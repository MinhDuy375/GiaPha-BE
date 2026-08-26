namespace LacVietGenealogy.Core.Entities;

public class FamilyTree
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;       // VD: "DÃ²ng há» Nguyá»…n Láº¡c Viá»‡t"
    public string? Description { get; set; }
    public Guid OwnerUserId { get; set; }                  // User Ä‘Ã£ táº¡o ra cÃ¢y nÃ y
    public User Owner { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Member> Members { get; set; } = new List<Member>();
}