using Microsoft.EntityFrameworkCore;
using LacVietGenealogy.Core.Entities;
using LacVietGenealogy.Core.Interfaces;

namespace LacVietGenealogy.Infrastructure.Data;

public class AppDbContext : DbContext
{
    private readonly ICurrentFamilyTreeService _currentFamilyTreeService;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentFamilyTreeService currentFamilyTreeService)
        : base(options)
    {
        _currentFamilyTreeService = currentFamilyTreeService;
    }

    // ── DbSets ──────────────────────────────────────────────────────────────
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<FamilyTree> FamilyTrees => Set<FamilyTree>();
    public DbSet<FamilyTreeMembership> FamilyTreeMemberships => Set<FamilyTreeMembership>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<ParentChildRelationship> ParentChildRelationships => Set<ParentChildRelationship>();
    public DbSet<SpouseRelationship> SpouseRelationships => Set<SpouseRelationship>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── UserRole (composite PK) ──────────────────────────────────────────
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId, ur.FamilyTreeId });

        // ── RolePermission (composite PK) ───────────────────────────────────
        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        // ── FamilyTree ───────────────────────────────────────────────────────
        modelBuilder.Entity<FamilyTree>()
            .HasOne(ft => ft.Owner)
            .WithMany()
            .HasForeignKey(ft => ft.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── FamilyTreeMembership ─────────────────────────────────────────────
        modelBuilder.Entity<FamilyTreeMembership>()
            .HasIndex(m => new { m.UserId, m.FamilyTreeId })
            .IsUnique(); // 1 user chỉ có 1 membership / 1 dòng họ

        modelBuilder.Entity<FamilyTreeMembership>()
            .HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FamilyTreeMembership>()
            .HasOne(m => m.FamilyTree)
            .WithMany()
            .HasForeignKey(m => m.FamilyTreeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FamilyTreeMembership>()
            .HasOne(m => m.Role)
            .WithMany()
            .HasForeignKey(m => m.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FamilyTreeMembership>()
            .HasOne(m => m.LinkedMember)
            .WithMany()
            .HasForeignKey(m => m.LinkedMemberId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── RefreshToken ─────────────────────────────────────────────────────
        modelBuilder.Entity<RefreshToken>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Member ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Member>()
            .HasOne(m => m.FamilyTree)
            .WithMany(ft => ft.Members)
            .HasForeignKey(m => m.FamilyTreeId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── ParentChildRelationship ──────────────────────────────────────────
        modelBuilder.Entity<ParentChildRelationship>()
            .HasOne(pc => pc.Parent)
            .WithMany(m => m.ParentEdges)
            .HasForeignKey(pc => pc.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ParentChildRelationship>()
            .HasOne(pc => pc.Child)
            .WithMany(m => m.ChildEdges)
            .HasForeignKey(pc => pc.ChildId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ParentChildRelationship>()
            .HasOne(pc => pc.FamilyTree)
            .WithMany()
            .HasForeignKey(pc => pc.FamilyTreeId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── SpouseRelationship ───────────────────────────────────────────────
        modelBuilder.Entity<SpouseRelationship>()
            .HasOne(s => s.Husband)
            .WithMany(m => m.HusbandEdges)
            .HasForeignKey(s => s.HusbandId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SpouseRelationship>()
            .HasOne(s => s.Wife)
            .WithMany(m => m.WifeEdges)
            .HasForeignKey(s => s.WifeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SpouseRelationship>()
            .HasOne(s => s.FamilyTree)
            .WithMany()
            .HasForeignKey(s => s.FamilyTreeId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Global Query Filters (Bảo mật multi-tenant) ─────────────────────
        // Dùng lambda capture để EF Core evaluate lazily mỗi request
        modelBuilder.Entity<Member>()
            .HasQueryFilter(m => m.FamilyTreeId == _currentFamilyTreeService.FamilyTreeId);

        modelBuilder.Entity<ParentChildRelationship>()
            .HasQueryFilter(pc => pc.FamilyTreeId == _currentFamilyTreeService.FamilyTreeId);

        modelBuilder.Entity<SpouseRelationship>()
            .HasQueryFilter(s => s.FamilyTreeId == _currentFamilyTreeService.FamilyTreeId);
    }
}