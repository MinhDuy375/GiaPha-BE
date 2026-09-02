namespace LacVietGenealogy.Core.Entities;

public class SystemModule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SystemMenu> Menus { get; set; } = new List<SystemMenu>();
}
