using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LacVietGenealogy.Core.Entities;
using LacVietGenealogy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using LacVietGenealogy.Core.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LacVietGenealogy.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICurrentFamilyTreeService _familyTreeService;

        public UserController(AppDbContext context, ICurrentFamilyTreeService familyTreeService)
        {
            _context = context;
            _familyTreeService = familyTreeService;
        }

        // Lấy thông tin người dùng hiện tại
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found.");

            return Ok(new { user.Id, user.Username, user.Email, user.FullName, user.CreatedAt });
        }

        // Cập nhật hồ sơ cá nhân
        public class UpdateProfileRequest { public string FullName { get; set; } = string.Empty; }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found.");

            if (string.IsNullOrWhiteSpace(request.FullName))
                return BadRequest(new { message = "Họ và tên không được để trống." });

            user.FullName = request.FullName;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật hồ sơ thành công.",
                user = new { user.Id, user.Username, user.Email, user.FullName, user.CreatedAt }
            });
        }

        // Lấy danh sách người dùng trong dòng họ hiện tại
        [HttpGet]
        [Authorize(Policy = "user.view")]
        public async Task<IActionResult> GetUsers()
        {
            var familyTreeId = _familyTreeService.FamilyTreeId;
            if (familyTreeId == Guid.Empty) return BadRequest("FamilyTree context missing.");

            var users = await _context.FamilyTreeMemberships
                .Where(m => m.FamilyTreeId == familyTreeId && m.Status == MembershipStatus.Active)
                .Include(m => m.User)
                .Include(m => m.RoleGroup)
                .Select(m => new
                {
                    m.Id,
                    UserId = m.UserId,
                    m.User.Username,
                    m.User.Email,
                    m.User.FullName,
                    m.User.IsActive,
                    m.User.CreatedAt,
                    RoleGroupId = m.RoleGroupId,
                    RoleGroupName = m.RoleGroup.Name,
                    LinkedMemberId = m.LinkedMemberId,
                    JoinedAt = m.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        // Đổi nhóm quyền của người dùng trong dòng họ
        public class UpdateRoleRequest { public Guid RoleGroupId { get; set; } }

        [HttpPut("{membershipId:guid}/role")]
        [Authorize(Policy = "user.manage")]
        public async Task<IActionResult> UpdateUserRole(Guid membershipId, [FromBody] UpdateRoleRequest request)
        {
            var familyTreeId = _familyTreeService.FamilyTreeId;
            if (familyTreeId == Guid.Empty) return BadRequest("FamilyTree context missing.");

            var membership = await _context.FamilyTreeMemberships
                .FirstOrDefaultAsync(m => m.Id == membershipId && m.FamilyTreeId == familyTreeId);
            if (membership == null) return NotFound(new { message = "Không tìm thấy thành viên." });

            var roleExists = await _context.RoleGroups.AnyAsync(r => r.Id == request.RoleGroupId);
            if (!roleExists) return BadRequest(new { message = "Nhóm quyền không hợp lệ." });

            membership.RoleGroupId = request.RoleGroupId;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật vai trò thành công." });
        }

        // Kích hoạt / Khóa tài khoản người dùng
        public class UpdateStatusRequest { public bool IsActive { get; set; } }

        [HttpPut("{userId:guid}/status")]
        [Authorize(Policy = "user.manage")]
        public async Task<IActionResult> UpdateUserStatus(Guid userId, [FromBody] UpdateStatusRequest request)
        {
            var familyTreeId = _familyTreeService.FamilyTreeId;
            if (familyTreeId == Guid.Empty) return BadRequest("FamilyTree context missing.");

            // Đảm bảo user thuộc dòng họ này
            var inTree = await _context.FamilyTreeMemberships
                .AnyAsync(m => m.UserId == userId && m.FamilyTreeId == familyTreeId && m.Status == MembershipStatus.Active);
            if (!inTree) return NotFound(new { message = "Không tìm thấy người dùng trong dòng họ." });

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { message = "Người dùng không tồn tại." });

            // Không cho phép khóa chính mình
            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(currentUserIdStr, out var currentUserId) && currentUserId == userId)
                return BadRequest(new { message = "Không thể thay đổi trạng thái tài khoản của chính mình." });

            user.IsActive = request.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { message = request.IsActive ? "Đã kích hoạt tài khoản." : "Đã khóa tài khoản." });
        }

        // Xóa người dùng khỏi dòng họ (xóa membership, không xóa tài khoản)
        [HttpDelete("{membershipId:guid}")]
        [Authorize(Policy = "user.manage")]
        public async Task<IActionResult> RemoveUserFromTree(Guid membershipId)
        {
            var familyTreeId = _familyTreeService.FamilyTreeId;
            if (familyTreeId == Guid.Empty) return BadRequest("FamilyTree context missing.");

            var membership = await _context.FamilyTreeMemberships
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == membershipId && m.FamilyTreeId == familyTreeId);
            if (membership == null) return NotFound(new { message = "Không tìm thấy thành viên." });

            // Không cho phép tự xóa mình
            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(currentUserIdStr, out var currentUserId) && currentUserId == membership.UserId)
                return BadRequest(new { message = "Không thể xóa chính mình khỏi dòng họ." });

            _context.FamilyTreeMemberships.Remove(membership);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã xóa {membership.User.FullName} khỏi dòng họ." });
        }
    }
}

