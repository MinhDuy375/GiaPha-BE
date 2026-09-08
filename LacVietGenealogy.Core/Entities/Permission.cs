namespace LacVietGenealogy.Core.Entities;

public class Permission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid MenuId { get; set; }
    public SystemMenu Menu { get; set; } = null!;

    public ICollection<RoleGroupPermission> RoleGroupPermissions { get; set; } = new List<RoleGroupPermission>();
}