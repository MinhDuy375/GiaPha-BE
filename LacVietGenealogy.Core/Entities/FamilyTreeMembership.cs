namespace LacVietGenealogy.Core.Entities
{
    public enum MembershipStatus { Pending, Active, Blocked }

    public class FamilyTreeMembership
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid FamilyTreeId { get; set; }
        public FamilyTree FamilyTree { get; set; } = null!;

        public Guid RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public MembershipStatus Status { get; set; } = MembershipStatus.Pending;

        /// <summary>
        /// Liên kết với node Member trong cây (dùng cho tính năng "claim node").
        /// Nullable: user có thể chưa được gắn với node nào.
        /// </summary>
        public Guid? LinkedMemberId { get; set; }
        public Member? LinkedMember { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}