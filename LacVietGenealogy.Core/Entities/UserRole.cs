namespace LacVietGenealogy.Core.Entities;

/// <summary>
/// Gán role cho user trong phạm vi một FamilyTree cụ thể.
/// Composite PK: (UserId, RoleId, FamilyTreeId).
/// </summary>
public class UserRole
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public Guid FamilyTreeId { get; set; }
    public FamilyTree FamilyTree { get; set; } = null!;
}