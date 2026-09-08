using System.Text.Json.Serialization;

namespace LacVietGenealogy.Core.Entities
{
    public enum MembershipStatus { Pending, Active, Rejected, Suspended }

    public class FamilyTreeMembership
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid FamilyTreeId { get; set; }
        public FamilyTree FamilyTree { get; set; } = null!;

        public Guid RoleGroupId { get; set; }
        public RoleGroup RoleGroup { get; set; } = null!;

        public MembershipStatus Status { get; set; } = MembershipStatus.Pending;

        // Optional: Liên kết với bản ghi Member trong gia phả
        public Guid? LinkedMemberId { get; set; }
        
        [JsonIgnore]
        public Member? LinkedMember { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}