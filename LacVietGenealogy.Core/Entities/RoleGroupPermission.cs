namespace LacVietGenealogy.Core.Entities;

public class RoleGroupPermission
{
    public Guid RoleGroupId { get; set; }
    public RoleGroup RoleGroup { get; set; } = null!;

    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}
