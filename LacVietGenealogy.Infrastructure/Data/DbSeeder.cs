using LacVietGenealogy.Core.Entities;
using LacVietGenealogy.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LacVietGenealogy.Infrastructure.Data
{
    public class DbSeeder
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public DbSeeder(AppDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task SeedAsync()
        {
            await _context.Database.EnsureCreatedAsync();

            var defaultPermissions = new List<Permission>
            {
                new() { Id = Guid.NewGuid(), Code = "member.view", Name = "Xem thành viên", Module = "Member" },
                new() { Id = Guid.NewGuid(), Code = "member.create", Name = "Tạo thành viên", Module = "Member" },
                new() { Id = Guid.NewGuid(), Code = "member.edit", Name = "Sửa thành viên", Module = "Member" },
                new() { Id = Guid.NewGuid(), Code = "member.delete", Name = "Xóa thành viên", Module = "Member" },
                new() { Id = Guid.NewGuid(), Code = "tree.view", Name = "Xem cây gia phả", Module = "FamilyTree" },
                new() { Id = Guid.NewGuid(), Code = "tree.edit", Name = "Sửa cây gia phả", Module = "FamilyTree" },
                new() { Id = Guid.NewGuid(), Code = "tree.export", Name = "Xuất cây gia phả", Module = "FamilyTree" },
                new() { Id = Guid.NewGuid(), Code = "data.import", Name = "Nhập dữ liệu gia phả", Module = "Data" },
                new() { Id = Guid.NewGuid(), Code = "data.approve", Name = "Phê duyệt đóng góp", Module = "Data" },
                new() { Id = Guid.NewGuid(), Code = "user.invite", Name = "Mời thành viên tham gia", Module = "Membership" },
                new() { Id = Guid.NewGuid(), Code = "user.remove", Name = "Xóa thành viên khỏi dòng họ", Module = "Membership" },
                new() { Id = Guid.NewGuid(), Code = "membership.view", Name = "Xem yêu cầu tham gia", Module = "Membership" },
                new() { Id = Guid.NewGuid(), Code = "membership.manage", Name = "Quản lý quyền tham gia", Module = "Membership" },
                new() { Id = Guid.NewGuid(), Code = "role.manage", Name = "Quản lý vai trò", Module = "Role" }
            };

            foreach (var perm in defaultPermissions)
            {
                if (!await _context.Permissions.AnyAsync(p => p.Code == perm.Code))
                {
                    await _context.Permissions.AddAsync(perm);
                }
            }
            await _context.SaveChangesAsync();

            var dbPermissions = await _context.Permissions.ToListAsync();

            var adminRoleId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var editorRoleId = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var viewerRoleId = Guid.Parse("00000000-0000-0000-0000-000000000003");

            if (!await _context.Roles.AnyAsync(r => r.Id == adminRoleId))
            {
                var adminRole = new Role { Id = adminRoleId, Name = "Admin", Description = "Quản trị viên dòng họ (Toàn quyền)" };
                await _context.Roles.AddAsync(adminRole);

                foreach (var perm in dbPermissions)
                {
                    await _context.RolePermissions.AddAsync(new RolePermission { RoleId = adminRoleId, PermissionId = perm.Id });
                }
            }

            if (!await _context.Roles.AnyAsync(r => r.Id == editorRoleId))
            {
                var editorRole = new Role { Id = editorRoleId, Name = "Editor", Description = "Biên soạn gia phả (Xem, Tạo, Sửa thành viên và quan hệ)" };
                await _context.Roles.AddAsync(editorRole);

                var editorCodes = new[] { "member.view", "member.create", "member.edit", "tree.view", "tree.export" };
                var editorPerms = dbPermissions.Where(p => editorCodes.Contains(p.Code));
                foreach (var perm in editorPerms)
                {
                    await _context.RolePermissions.AddAsync(new RolePermission { RoleId = editorRoleId, PermissionId = perm.Id });
                }
            }

            if (!await _context.Roles.AnyAsync(r => r.Id == viewerRoleId))
            {
                var viewerRole = new Role { Id = viewerRoleId, Name = "Viewer", Description = "Thành viên dòng họ (Chỉ xem)" };
                await _context.Roles.AddAsync(viewerRole);

                var viewerCodes = new[] { "member.view", "tree.view" };
                var viewerPerms = dbPermissions.Where(p => viewerCodes.Contains(p.Code));
                foreach (var perm in viewerPerms)
                {
                    await _context.RolePermissions.AddAsync(new RolePermission { RoleId = viewerRoleId, PermissionId = perm.Id });
                }
            }

            await _context.SaveChangesAsync();

            var defaultAdminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            if (!await _context.Users.AnyAsync(u => u.Id == defaultAdminId || u.Email == "admin@lacviet.local"))
            {
                var defaultAdmin = new User
                {
                    Id = defaultAdminId,
                    Username = "admin",
                    Email = "admin@lacviet.local",
                    PasswordHash = _passwordHasher.Hash("Admin@123"),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.Users.AddAsync(defaultAdmin);
                await _context.SaveChangesAsync();
            }
        }
    }
}