namespace LacVietGenealogy.Core.Entities;

public class SpouseRelationship
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>FamilyTreeId trực tiếp để Global Query Filter filter nhanh, không cần join.</summary>
    public Guid FamilyTreeId { get; set; }
    public FamilyTree FamilyTree { get; set; } = null!;

    public Guid HusbandId { get; set; }
    public Member Husband { get; set; } = null!;

    public Guid WifeId { get; set; }
    public Member Wife { get; set; } = null!;

    public int MarriageOrder { get; set; } = 1;
}