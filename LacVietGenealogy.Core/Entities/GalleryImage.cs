namespace LacVietGenealogy.Core.Entities;

public class GalleryImage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FamilyTreeId { get; set; }
    public FamilyTree FamilyTree { get; set; } = null!;
    public Guid? MemberId { get; set; }
    public Member? Member { get; set; }
    public Guid? EventId { get; set; }
    public FamilyEvent? Event { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
