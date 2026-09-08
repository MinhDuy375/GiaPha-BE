using LacVietGenealogy.Core.Entities;
using LacVietGenealogy.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LacVietGenealogy.Infrastructure.Data;

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

        // 1. Seed Admin User
        var defaultAdminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        if (!await _context.Users.AnyAsync(u => u.Id == defaultAdminId || u.Email == "admin@lacviet.local"))
        {
            var defaultAdmin = new User
            {
                Id = defaultAdminId,
                Username = "admin@lacviet.local",
                Email = "admin@lacviet.local",
                FullName = "System Admin",
                PasswordHash = _passwordHasher.Hash("Admin@123"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Users.AddAsync(defaultAdmin);
        }

        // 2. Seed System Modules
        var moduleMemberId = Guid.Parse("A0000000-0000-0000-0000-000000000001");
        var moduleTreeId = Guid.Parse("A0000000-0000-0000-0000-000000000002");
        var moduleSystemId = Guid.Parse("A0000000-0000-0000-0000-000000000003");

        var modules = new List<SystemModule>
        {
            new() { Id = moduleMemberId, Name = "Quản lý thành viên", Alias = "member" },
            new() { Id = moduleTreeId, Name = "Cây gia phả", Alias = "tree" },
            new() { Id = moduleSystemId, Name = "Hệ thống", Alias = "system" }
        };

        foreach (var m in modules)
        {
            if (!await _context.SystemModules.AnyAsync(x => x.Id == m.Id))
                await _context.SystemModules.AddAsync(m);
        }

        // 3. Seed System Menus
        var menuMemberListId = Guid.Parse("B0000000-0000-0000-0000-000000000001");
        var menuTreeViewId = Guid.Parse("B0000000-0000-0000-0000-000000000002");
        var menuRoleGroupId = Guid.Parse("B0000000-0000-0000-0000-000000000003");
        var menuMembershipId = Guid.Parse("B0000000-0000-0000-0000-000000000004");
        var menuEventId = Guid.Parse("B0000000-0000-0000-0000-000000000005");
        var menuKinshipId = Guid.Parse("B0000000-0000-0000-0000-000000000006");
        var menuGalleryId = Guid.Parse("B0000000-0000-0000-0000-000000000007");
        var menuRelationshipId = Guid.Parse("B0000000-0000-0000-0000-000000000008");
        var menuStatisticsId = Guid.Parse("B0000000-0000-0000-0000-000000000009");
        var menuUserManageId = Guid.Parse("B0000000-0000-0000-0000-000000000010");

        var menus = new List<SystemMenu>
        {
            new() { Id = menuMemberListId, ModuleId = moduleMemberId, Name = "Danh sách thành viên", Alias = "member_list", Icon = "users" },
            new() { Id = menuTreeViewId, ModuleId = moduleTreeId, Name = "Sơ đồ cây", Alias = "tree_view", Icon = "tree" },
            new() { Id = menuRoleGroupId, ModuleId = moduleSystemId, Name = "Nhóm quyền", Alias = "role_group", Icon = "shield" },
            new() { Id = menuMembershipId, ModuleId = moduleSystemId, Name = "Yêu cầu tham gia", Alias = "membership", Icon = "user-plus" },
            new() { Id = menuEventId, ModuleId = moduleSystemId, Name = "Sự kiện dòng họ", Alias = "events", Icon = "calendar" },
            new() { Id = menuKinshipId, ModuleId = moduleMemberId, Name = "Tra cứu danh xưng", Alias = "kinship", Icon = "link" },
            new() { Id = menuGalleryId, ModuleId = moduleMemberId, Name = "Thư viện dòng họ", Alias = "gallery", Icon = "images" },
            new() { Id = menuRelationshipId, ModuleId = moduleMemberId, Name = "Quản lý quan hệ", Alias = "relationships", Icon = "link" },
            new() { Id = menuStatisticsId, ModuleId = moduleTreeId, Name = "Thống kê dòng họ", Alias = "statistics", Icon = "chart" },
            new() { Id = menuUserManageId, ModuleId = moduleSystemId, Name = "Quản lý người dùng", Alias = "user_manage", Icon = "user-shield" }
        };

        foreach (var m in menus)
        {
            var existingMenu = await _context.SystemMenus.FirstOrDefaultAsync(x => x.Id == m.Id);
            if (existingMenu == null)
                await _context.SystemMenus.AddAsync(m);
            else
                existingMenu.Icon = m.Icon;
        }

        // 4. Seed Permissions
        var permissions = new List<Permission>
        {
            // member_list
            new() { Id = Guid.NewGuid(), MenuId = menuMemberListId, Code = "member_list.view", Name = "Xem danh sách" },
            new() { Id = Guid.NewGuid(), MenuId = menuMemberListId, Code = "member_list.create", Name = "Thêm thành viên" },
            new() { Id = Guid.NewGuid(), MenuId = menuMemberListId, Code = "member_list.edit", Name = "Sửa thành viên" },
            new() { Id = Guid.NewGuid(), MenuId = menuMemberListId, Code = "member_list.delete", Name = "Xóa thành viên" },
            // tree_view
            new() { Id = Guid.NewGuid(), MenuId = menuTreeViewId, Code = "tree_view.view", Name = "Xem sơ đồ" },
            new() { Id = Guid.NewGuid(), MenuId = menuTreeViewId, Code = "tree_view.export", Name = "Xuất sơ đồ" },
            // role_group
            new() { Id = Guid.NewGuid(), MenuId = menuRoleGroupId, Code = "role_group.view", Name = "Xem nhóm quyền" },
            new() { Id = Guid.NewGuid(), MenuId = menuRoleGroupId, Code = "role_group.manage", Name = "Quản lý phân quyền" },
            // membership
            new() { Id = Guid.NewGuid(), MenuId = menuMembershipId, Code = "membership.view", Name = "Xem yêu cầu" },
            new() { Id = Guid.NewGuid(), MenuId = menuMembershipId, Code = "membership.manage", Name = "Duyệt/Từ chối" },
            new() { Id = Guid.NewGuid(), MenuId = menuMembershipId, Code = "membership.code.view", Name = "Xem và sao chép mã gia tộc" }
            ,new() { Id = Guid.NewGuid(), MenuId = menuEventId, Code = "event.view", Name = "Xem sự kiện" }
            ,new() { Id = Guid.NewGuid(), MenuId = menuEventId, Code = "event.manage", Name = "Quản lý sự kiện" }
            ,new() { Id = Guid.NewGuid(), MenuId = menuKinshipId, Code = "kinship.view", Name = "Tra cứu danh xưng" }
            ,new() { Id = Guid.NewGuid(), MenuId = menuGalleryId, Code = "gallery.view", Name = "Xem thư viện" }
            ,new() { Id = Guid.NewGuid(), MenuId = menuGalleryId, Code = "gallery.manage", Name = "Quản lý thư viện" }
            ,new() { Id = Guid.NewGuid(), MenuId = menuRelationshipId, Code = "relationship.view", Name = "Xem quan hệ" }
            ,new() { Id = Guid.NewGuid(), MenuId = menuRelationshipId, Code = "relationship.manage", Name = "Quản lý quan hệ" }
            ,new() { Id = Guid.NewGuid(), MenuId = menuStatisticsId, Code = "statistics.view", Name = "Xem thống kê" }
            ,new() { Id = Guid.NewGuid(), MenuId = menuUserManageId, Code = "user.view", Name = "Xem danh sách người dùng" }
            ,new() { Id = Guid.NewGuid(), MenuId = menuUserManageId, Code = "user.manage", Name = "Quản lý người dùng" }
        };

        foreach (var p in permissions)
        {
            if (!await _context.Permissions.AnyAsync(x => x.Code == p.Code))
                await _context.Permissions.AddAsync(p);
        }

        await _context.SaveChangesAsync();

        var eventPermissions = await _context.Permissions
            .Where(permission => permission.Code == "event.view" || permission.Code == "event.manage" || permission.Code == "kinship.view" || permission.Code == "gallery.view" || permission.Code == "gallery.manage" || permission.Code == "membership.code.view" || permission.Code == "relationship.view" || permission.Code == "relationship.manage" || permission.Code == "statistics.view" || permission.Code == "user.view" || permission.Code == "user.manage")
            .ToListAsync();
        var existingGroups = await _context.RoleGroups
            .IgnoreQueryFilters()
            .Include(group => group.RoleGroupPermissions)
            .Where(group => group.Name == "Quản trị viên" || group.Name == "Người biên tập" || group.Name == "Thành viên thường")
            .ToListAsync();
        foreach (var group in existingGroups)
        {
            var allowedCodes = group.Name == "Thành viên thường"
                ? new[] { "event.view", "kinship.view", "gallery.view", "relationship.view", "statistics.view" }
                : group.Name == "Người biên tập"
                    ? new[] { "event.view", "event.manage", "kinship.view", "gallery.view", "gallery.manage", "relationship.view", "relationship.manage", "statistics.view" }
                    : eventPermissions.Select(permission => permission.Code).ToArray(); // Quản trị viên: tất cả
            foreach (var permission in eventPermissions.Where(permission => allowedCodes.Contains(permission.Code) && group.RoleGroupPermissions.All(link => link.PermissionId != permission.Id)))
                group.RoleGroupPermissions.Add(new RoleGroupPermission { RoleGroupId = group.Id, PermissionId = permission.Id });
        }
        await _context.SaveChangesAsync();
    }
}