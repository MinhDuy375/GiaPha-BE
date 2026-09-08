namespace LacVietGenealogy.Core.Entities;

public class RoleGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid FamilyTreeId { get; set; }
    public FamilyTree FamilyTree { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RoleGroupPermission> RoleGroupPermissions { get; set; } = new List<RoleGroupPermission>();
    public ICollection<FamilyTreeMembership> Memberships { get; set; } = new List<FamilyTreeMembership>();
}
