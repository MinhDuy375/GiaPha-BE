namespace LacVietGenealogy.Core.Entities;

public class FamilyEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FamilyTreeId { get; set; }
    public FamilyTree FamilyTree { get; set; } = null!;
    public Guid? MemberId { get; set; }
    public Member? Member { get; set; }
    public string Title { get; set; } = string.Empty;
    public string EventType { get; set; } = "custom";
    public DateTime EventDate { get; set; }
    public string? Description { get; set; }
    public bool IsRecurringYearly { get; set; }
}
