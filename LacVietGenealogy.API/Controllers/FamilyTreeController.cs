using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LacVietGenealogy.Core.Entities;
using LacVietGenealogy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using LacVietGenealogy.Core.Interfaces;
using LacVietGenealogy.API.Services;
using Microsoft.Extensions.Logging;

namespace LacVietGenealogy.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FamilyTreeController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICurrentFamilyTreeService _currentFamilyTree;
        private readonly ILogger<FamilyTreeController> _logger;

        public FamilyTreeController(AppDbContext context, ICurrentFamilyTreeService currentFamilyTree, ILogger<FamilyTreeController> logger)
        {
            _context = context;
            _currentFamilyTree = currentFamilyTree;
            _logger = logger;
        }

        public class CreateTreeRequest
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> CreateFamilyTree([FromBody] CreateTreeRequest request)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var joinCode = string.Empty;
            do
            {
                joinCode = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            } while (await _context.FamilyTrees.AnyAsync(tree => tree.JoinCode == joinCode));

            var familyTree = new FamilyTree
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                OwnerUserId = userId,
                JoinCode = joinCode,
                CreatedAt = DateTime.UtcNow
            };

            await _context.FamilyTrees.AddAsync(familyTree);

            // Tự động tạo các nhóm quyền mặc định cho dòng họ này
            var allPermissions = await _context.Permissions.ToListAsync();

            var adminGroup = new RoleGroup { Id = Guid.NewGuid(), Name = "Quản trị viên", Description = "Toàn quyền quản trị dòng họ", FamilyTreeId = familyTree.Id };
            foreach (var p in allPermissions)
            {
                adminGroup.RoleGroupPermissions.Add(new RoleGroupPermission { RoleGroupId = adminGroup.Id, PermissionId = p.Id });
            }

            var editorGroup = new RoleGroup { Id = Guid.NewGuid(), Name = "Người biên tập", Description = "Thêm, sửa thành viên", FamilyTreeId = familyTree.Id };
            var editorCodes = new[] { "member_list.view", "member_list.create", "member_list.edit", "tree_view.view", "tree_view.export", "event.view", "event.manage", "kinship.view", "gallery.view", "gallery.manage", "relationship.view", "relationship.manage", "statistics.view" };
            foreach (var p in allPermissions.Where(x => editorCodes.Contains(x.Code)))
            {
                editorGroup.RoleGroupPermissions.Add(new RoleGroupPermission { RoleGroupId = editorGroup.Id, PermissionId = p.Id });
            }

            var viewerGroup = new RoleGroup { Id = Guid.NewGuid(), Name = "Thành viên thường", Description = "Chỉ xem thông tin", FamilyTreeId = familyTree.Id };
            var viewerCodes = new[] { "member_list.view", "tree_view.view", "event.view", "kinship.view", "gallery.view", "relationship.view", "statistics.view" };
            foreach (var p in allPermissions.Where(x => viewerCodes.Contains(x.Code)))
            {
                viewerGroup.RoleGroupPermissions.Add(new RoleGroupPermission { RoleGroupId = viewerGroup.Id, PermissionId = p.Id });
            }

            await _context.RoleGroups.AddRangeAsync(adminGroup, editorGroup, viewerGroup);

            // Gán role Admin mặc định cho người sáng lập dòng họ này
            var membership = new FamilyTreeMembership
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FamilyTreeId = familyTree.Id,
                RoleGroupId = adminGroup.Id,
                Status = MembershipStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            await _context.FamilyTreeMemberships.AddAsync(membership);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(CreateFamilyTree), new { id = familyTree.Id }, familyTree);
        }

        [HttpGet]
        public async Task<IActionResult> GetMyFamilyTrees()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var trees = await _context.FamilyTreeMemberships
                .IgnoreQueryFilters()
                .Where(m => m.UserId == userId && m.Status == MembershipStatus.Active)
                .Select(m => new
                {
                    m.FamilyTreeId,
                    m.FamilyTree.Name,
                    m.FamilyTree.Description,
                    m.FamilyTree.JoinCode,
                    Role = m.RoleGroup.Name,
                    m.FamilyTree.CreatedAt
                })
                .ToListAsync();

            return Ok(trees);
        }

        public class JoinTreeRequest
        {
            public string JoinCode { get; set; } = string.Empty;
        }

        [HttpPost("join")]
        public async Task<IActionResult> JoinFamilyTree([FromBody] JoinTreeRequest request)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var joinCode = request.JoinCode?.Trim().ToUpperInvariant();
            var tree = await _context.FamilyTrees.FirstOrDefaultAsync(t => t.JoinCode == joinCode);
            if (tree == null)
                return NotFound(new { message = "Mã tham gia không hợp lệ." });

            var existingMembership = await _context.FamilyTreeMemberships
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.UserId == userId && m.FamilyTreeId == tree.Id);

            var memberGroup = await _context.RoleGroups
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.FamilyTreeId == tree.Id && r.Name == "Thành viên thường")
                ?? await CreateDefaultMemberGroup(tree.Id);

            if (existingMembership != null)
            {
                if (existingMembership.Status == MembershipStatus.Active)
                    return BadRequest(new { message = "Bạn đã là thành viên của dòng họ này." });
                if (existingMembership.Status == MembershipStatus.Pending)
                    return BadRequest(new { message = "Yêu cầu tham gia của bạn đang chờ được duyệt." });

                existingMembership.RoleGroupId = memberGroup.Id;
                existingMembership.Status = MembershipStatus.Pending;
                existingMembership.LinkedMemberId = null;
                existingMembership.CreatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Đã gửi lại yêu cầu tham gia. Vui lòng chờ quản trị viên phê duyệt.", familyTreeId = tree.Id, status = existingMembership.Status.ToString() });
            }

            var membership = new FamilyTreeMembership
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FamilyTreeId = tree.Id,
                RoleGroupId = memberGroup.Id,
                Status = MembershipStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _context.FamilyTreeMemberships.AddAsync(membership);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã gửi yêu cầu tham gia. Vui lòng chờ quản trị viên phê duyệt.", familyTreeId = tree.Id, status = membership.Status.ToString() });
        }

        private async Task<RoleGroup> CreateDefaultMemberGroup(Guid familyTreeId)
        {
            var group = new RoleGroup
            {
                Id = Guid.NewGuid(),
                FamilyTreeId = familyTreeId,
                Name = "Thành viên thường",
                Description = "Quyền xem cơ bản, chờ quản trị viên phân quyền bổ sung"
            };
            var viewerPermissions = new[] { "member_list.view", "tree_view.view", "event.view", "kinship.view", "gallery.view", "relationship.view", "statistics.view" };
            var permissions = await _context.Permissions
                .Where(permission => viewerPermissions.Contains(permission.Code))
                .ToListAsync();
            foreach (var permission in permissions)
                group.RoleGroupPermissions.Add(new RoleGroupPermission { RoleGroupId = group.Id, PermissionId = permission.Id });
            await _context.RoleGroups.AddAsync(group);
            await _context.SaveChangesAsync();
            return group;
        }

        [HttpGet("current")]
        [Authorize(Policy = "membership.view")]
        public async Task<IActionResult> GetCurrentFamilyTree()
        {
            var treeId = _currentFamilyTree.FamilyTreeId;
            var tree = await _context.FamilyTrees.FirstOrDefaultAsync(item => item.Id == treeId);
            return tree == null ? NotFound() : Ok(new { tree.Id, tree.Name, tree.Description, tree.JoinCode, tree.CreatedAt });
        }

        [HttpGet("my-join-requests")]
        public async Task<IActionResult> GetMyJoinRequests()
        {
            var userId = GetUserId();
            var requests = await _context.FamilyTreeMemberships
                .IgnoreQueryFilters()
                .Where(item => item.UserId == userId && (item.Status == MembershipStatus.Pending || item.Status == MembershipStatus.Rejected))
                .Select(item => new
                {
                    item.FamilyTreeId,
                    FamilyTreeName = item.FamilyTree.Name,
                    item.Status,
                    StatusText = item.Status.ToString(),
                    item.CreatedAt
                })
                .OrderByDescending(item => item.CreatedAt)
                .ToListAsync();
            return Ok(requests);
        }

        [HttpGet("join-requests")]
        [Authorize(Policy = "membership.view")]
        public async Task<IActionResult> GetJoinRequests()
        {
            var treeId = _currentFamilyTree.FamilyTreeId;
            var requests = await _context.FamilyTreeMemberships
                .IgnoreQueryFilters()
                .Include(item => item.User)
                .Include(item => item.RoleGroup)
                .Include(item => item.LinkedMember)
                .Where(item => item.FamilyTreeId == treeId && item.Status == MembershipStatus.Pending)
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => new
                {
                    item.Id,
                    item.UserId,
                    UserName = item.User.FullName,
                    item.User.Email,
                    item.CreatedAt,
                    item.RoleGroupId,
                    RoleName = item.RoleGroup.Name,
                    item.LinkedMemberId,
                    LinkedMemberName = item.LinkedMember == null ? null : item.LinkedMember.FullName
                })
                .ToListAsync();
            return Ok(requests);
        }

        public class ReviewMembershipRequest
        {
            public Guid RoleGroupId { get; set; }
            public Guid? LinkedMemberId { get; set; }
        }

        [HttpPut("join-requests/{id:guid}/approve")]
        [Authorize(Policy = "membership.manage")]
        public async Task<IActionResult> ApproveJoinRequest(Guid id, ReviewMembershipRequest request)
        {
            var treeId = _currentFamilyTree.FamilyTreeId;
            var membership = await _context.FamilyTreeMemberships.IgnoreQueryFilters()
                .FirstOrDefaultAsync(item => item.Id == id && item.FamilyTreeId == treeId && item.Status == MembershipStatus.Pending);
            if (membership == null) return NotFound(new { message = "Không tìm thấy yêu cầu tham gia." });
            if (!await _context.RoleGroups.AnyAsync(group => group.Id == request.RoleGroupId && group.FamilyTreeId == treeId)) return BadRequest(new { message = "Nhóm quyền không thuộc gia tộc hiện tại." });
            if (request.LinkedMemberId.HasValue && !await _context.Members.AnyAsync(member => member.Id == request.LinkedMemberId && member.FamilyTreeId == treeId)) return BadRequest(new { message = "Thành viên liên kết không thuộc gia tộc hiện tại." });
            membership.RoleGroupId = request.RoleGroupId;
            membership.LinkedMemberId = request.LinkedMemberId;
            membership.Status = MembershipStatus.Active;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã phê duyệt thành viên." });
        }

        [HttpPut("join-requests/{id:guid}/reject")]
        [Authorize(Policy = "membership.manage")]
        public async Task<IActionResult> RejectJoinRequest(Guid id, [FromServices] IEmailService emailService)
        {
            var treeId = _currentFamilyTree.FamilyTreeId;
            var membership = await _context.FamilyTreeMemberships.IgnoreQueryFilters()
                .Include(item => item.User)
                .Include(item => item.FamilyTree)
                .FirstOrDefaultAsync(item => item.Id == id && item.FamilyTreeId == treeId && item.Status == MembershipStatus.Pending);
            if (membership == null) return NotFound(new { message = "Không tìm thấy yêu cầu tham gia." });
            membership.Status = MembershipStatus.Rejected;
            await _context.SaveChangesAsync();

            var subject = $"Lạc Việt Gia Phả - Yêu cầu tham gia {membership.FamilyTree.Name}";
            var body = $"<p>Chào {membership.User.FullName},</p><p>Yêu cầu tham gia gia phả <strong>{membership.FamilyTree.Name}</strong> của bạn đã bị từ chối.</p><p>Bạn có thể liên hệ quản trị viên để biết thêm chi tiết.</p>";
            var emailSent = await emailService.SendEmailAsync(membership.User.Email, subject, body);
            _logger.LogInformation("Join rejection email result for {Email}, FamilyTreeId={FamilyTreeId}: Sent={EmailSent}", membership.User.Email, treeId, emailSent);
            return Ok(new { message = "Đã từ chối yêu cầu." });
        }

        [HttpGet("role-groups")]
        [Authorize(Policy = "membership.manage")]
        public async Task<IActionResult> GetCurrentRoleGroups()
        {
            var treeId = _currentFamilyTree.FamilyTreeId;
            return Ok(await _context.RoleGroups.Where(group => group.FamilyTreeId == treeId).Select(group => new { group.Id, group.Name, group.Description }).ToListAsync());
        }

        private Guid GetUserId()
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(value, out var id) ? id : throw new UnauthorizedAccessException();
        }
    }
}