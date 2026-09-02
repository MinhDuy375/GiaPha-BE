namespace LacVietGenealogy.Core.Entities;

public class Member
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FamilyTreeId { get; set; }
    public FamilyTree FamilyTree { get; set; } = null!;

    public string FullName { get; set; } = string.Empty;
    public string? TabooName { get; set; }
    public string? CourtesyName { get; set; }
    public string? OtherNames { get; set; }
    public int Gender { get; set; }
    public int GenerationLevel { get; set; }
    public int? BirthOrder { get; set; }
    public bool IsInLaw { get; set; }

    public DateTime? BirthDateSolar { get; set; }
    public int? BirthMonth { get; set; }
    public int? BirthDay { get; set; }
    public string? BirthDateLunar { get; set; }
    public int? BirthLunarYear { get; set; }
    public int? BirthLunarMonth { get; set; }
    public int? BirthLunarDay { get; set; }
    public DateTime? DeathDateSolar { get; set; }
    public int? DeathMonth { get; set; }
    public int? DeathDay { get; set; }
    public int? DeathLunarYear { get; set; }
    public int? DeathLunarMonth { get; set; }
    public int? DeathLunarDay { get; set; }
    public string? DeathDateLunar { get; set; }

    public bool IsAlive { get; set; } = true;
    public string? AvatarUrl { get; set; }
    public string? Biography { get; set; }
    public string? Note { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Occupation { get; set; }
    public string? CurrentResidence { get; set; }

    public ICollection<ParentChildRelationship> ParentEdges { get; set; } = new List<ParentChildRelationship>();
    public ICollection<ParentChildRelationship> ChildEdges { get; set; } = new List<ParentChildRelationship>();
    public ICollection<SpouseRelationship> HusbandEdges { get; set; } = new List<SpouseRelationship>();
    public ICollection<SpouseRelationship> WifeEdges { get; set; } = new List<SpouseRelationship>();
}