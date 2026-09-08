namespace LacVietGenealogy.Core.Entities;

public class SystemMenu
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string Icon { get; set; } = "menu";
    public Guid ModuleId { get; set; }
    public SystemModule Module { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}
