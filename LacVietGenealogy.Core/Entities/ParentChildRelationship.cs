namespace LacVietGenealogy.Core.Entities;

public class ParentChildRelationship
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>FamilyTreeId trực tiếp để Global Query Filter filter nhanh, không cần join.</summary>
    public Guid FamilyTreeId { get; set; }
    public FamilyTree FamilyTree { get; set; } = null!;

    public Guid ParentId { get; set; }
    public Member Parent { get; set; } = null!;

    public Guid ChildId { get; set; }
    public Member Child { get; set; } = null!;

    public int ChildOrder { get; set; } = 1;

    /// <summary>0 = sinh đẻ, 1 = nuôi, 2 = khác</summary>
    public int RelationshipType { get; set; } = 0;
}