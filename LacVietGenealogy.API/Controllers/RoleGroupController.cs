using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LacVietGenealogy.Core.Entities;
using LacVietGenealogy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using LacVietGenealogy.Core.Interfaces;

namespace LacVietGenealogy.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RoleGroupController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICurrentFamilyTreeService _familyTreeService;

        public RoleGroupController(AppDbContext context, ICurrentFamilyTreeService familyTreeService)
        {
            _context = context;
            _familyTreeService = familyTreeService;
        }

        // Lấy danh sách nhóm quyền trong dòng họ hiện tại
        [HttpGet]
        [Authorize(Policy = "role_group.view")]
        public async Task<IActionResult> GetRoleGroups()
        {
            var familyTreeId = _familyTreeService.FamilyTreeId;
            if (familyTreeId == Guid.Empty) return BadRequest("FamilyTree context missing.");

            var groups = await _context.RoleGroups
                .Include(g => g.RoleGroupPermissions)
                .ThenInclude(rp => rp.Permission)
                .Select(g => new
                {
                    g.Id,
                    g.Name,
                    g.Description,
                    Permissions = g.RoleGroupPermissions.Select(rp => rp.Permission.Code)
                })
                .ToListAsync();

            return Ok(groups);
        }

        // Lấy cấu trúc ma trận (Modules -> Menus -> Permissions)
        [HttpGet("matrix")]
        [Authorize(Policy = "role_group.view")]
        public async Task<IActionResult> GetPermissionMatrix()
        {
            var modules = await _context.SystemModules
                .Include(m => m.Menus)
                .ThenInclude(menu => menu.Permissions)
                .Select(m => new
                {
                    m.Id,
                    m.Name,
                    m.Alias,
                    Menus = m.Menus.Select(menu => new
                    {
                        menu.Id,
                        menu.Name,
                        menu.Alias,
                        Permissions = menu.Permissions.Select(p => new
                        {
                            p.Id,
                            p.Name,
                            p.Code
                        })
                    })
                })
                .ToListAsync();

            return Ok(modules);
        }

        [HttpGet("navigation")]
        public async Task<IActionResult> GetNavigation()
        {
            var menus = await _context.SystemMenus
                .Include(menu => menu.Permissions)
                .OrderBy(menu => menu.ModuleId)
                .ThenBy(menu => menu.Name)
                .Select(menu => new
                {
                    menu.Id,
                    menu.Name,
                    menu.Alias,
                    menu.Icon,
                    Module = menu.Module.Name,
                    Permissions = menu.Permissions.Select(permission => permission.Code)
                })
                .ToListAsync();
            return Ok(menus);
        }

        public class CreateRoleGroupRequest
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public List<string> PermissionCodes { get; set; } = new();
        }

        [HttpPost]
        [Authorize(Policy = "role_group.manage")]
        public async Task<IActionResult> CreateRoleGroup([FromBody] CreateRoleGroupRequest request)
        {
            var familyTreeId = _familyTreeService.FamilyTreeId;
            if (familyTreeId == Guid.Empty) return BadRequest("FamilyTree context missing.");

            if (await _context.RoleGroups.AnyAsync(g => g.FamilyTreeId == familyTreeId && g.Name == request.Name))
                return BadRequest(new { message = "Tên nhóm quyền đã tồn tại." });

            var newGroup = new RoleGroup
            {
                Id = Guid.NewGuid(),
                FamilyTreeId = familyTreeId,
                Name = request.Name,
                Description = request.Description
            };

            if (request.PermissionCodes.Any())
            {
                var perms = await _context.Permissions
                    .Where(p => request.PermissionCodes.Contains(p.Code))
                    .ToListAsync();

                foreach (var p in perms)
                {
                    newGroup.RoleGroupPermissions.Add(new RoleGroupPermission
                    {
                        RoleGroupId = newGroup.Id,
                        PermissionId = p.Id
                    });
                }
            }

            await _context.RoleGroups.AddAsync(newGroup);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Tạo nhóm quyền thành công.", id = newGroup.Id });
        }

        public class UpdateRoleGroupRequest
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public List<string> PermissionCodes { get; set; } = new();
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "role_group.manage")]
        public async Task<IActionResult> UpdateRoleGroup(Guid id, [FromBody] UpdateRoleGroupRequest request)
        {
            var group = await _context.RoleGroups
                .Include(g => g.RoleGroupPermissions)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null) return NotFound();

            if (group.Name == "Quản trị viên")
                return BadRequest(new { message = "Không thể sửa nhóm quyền Quản trị viên hệ thống." });

            group.Name = request.Name;
            group.Description = request.Description;

            // Xóa quyền cũ
            _context.RoleGroupPermissions.RemoveRange(group.RoleGroupPermissions);

            // Thêm quyền mới
            if (request.PermissionCodes.Any())
            {
                var perms = await _context.Permissions
                    .Where(p => request.PermissionCodes.Contains(p.Code))
                    .ToListAsync();

                foreach (var p in perms)
                {
                    group.RoleGroupPermissions.Add(new RoleGroupPermission
                    {
                        RoleGroupId = group.Id,
                        PermissionId = p.Id
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thành công." });
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "role_group.manage")]
        public async Task<IActionResult> DeleteRoleGroup(Guid id)
        {
            var group = await _context.RoleGroups.FindAsync(id);
            if (group == null) return NotFound();

            if (group.Name == "Quản trị viên" || group.Name == "Thành viên thường")
                return BadRequest(new { message = "Không thể xóa nhóm quyền mặc định." });

            var hasMembers = await _context.FamilyTreeMemberships.AnyAsync(m => m.RoleGroupId == id);
            if (hasMembers)
                return BadRequest(new { message = "Không thể xóa nhóm quyền đang có thành viên. Hãy chuyển thành viên sang nhóm khác trước." });

            _context.RoleGroups.Remove(group);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa nhóm quyền." });
        }
    }
}
