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
    public DbSet<SystemModule> SystemModules => Set<SystemModule>();
    public DbSet<SystemMenu> SystemMenus => Set<SystemMenu>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RoleGroup> RoleGroups => Set<RoleGroup>();
    public DbSet<RoleGroupPermission> RoleGroupPermissions => Set<RoleGroupPermission>();
    public DbSet<FamilyTree> FamilyTrees => Set<FamilyTree>();
    public DbSet<FamilyTreeMembership> FamilyTreeMemberships => Set<FamilyTreeMembership>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<ParentChildRelationship> ParentChildRelationships => Set<ParentChildRelationship>();
    public DbSet<SpouseRelationship> SpouseRelationships => Set<SpouseRelationship>();
    public DbSet<FamilyEvent> FamilyEvents => Set<FamilyEvent>();
    public DbSet<GalleryImage> GalleryImages => Set<GalleryImage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── RoleGroupPermission (composite PK) ────────────────────────────────
        modelBuilder.Entity<RoleGroupPermission>()
            .HasKey(rp => new { rp.RoleGroupId, rp.PermissionId });

        modelBuilder.Entity<RoleGroupPermission>()
            .HasOne(rp => rp.RoleGroup)
            .WithMany(rg => rg.RoleGroupPermissions)
            .HasForeignKey(rp => rp.RoleGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RoleGroupPermission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RoleGroupPermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── SystemMenu & SystemModule ─────────────────────────────────────────
        modelBuilder.Entity<SystemMenu>()
            .HasOne(m => m.Module)
            .WithMany(md => md.Menus)
            .HasForeignKey(m => m.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Permission>()
            .HasOne(p => p.Menu)
            .WithMany(m => m.Permissions)
            .HasForeignKey(p => p.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── RoleGroup ────────────────────────────────────────────────────────
        modelBuilder.Entity<RoleGroup>()
            .HasOne(rg => rg.FamilyTree)
            .WithMany()
            .HasForeignKey(rg => rg.FamilyTreeId)
            .OnDelete(DeleteBehavior.Cascade);

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
            .WithMany(u => u.Memberships)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FamilyTreeMembership>()
            .HasOne(m => m.FamilyTree)
            .WithMany()
            .HasForeignKey(m => m.FamilyTreeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FamilyTreeMembership>()
            .HasOne(m => m.RoleGroup)
            .WithMany(rg => rg.Memberships)
            .HasForeignKey(m => m.RoleGroupId)
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

        modelBuilder.Entity<FamilyEvent>()
            .HasOne(e => e.FamilyTree)
            .WithMany()
            .HasForeignKey(e => e.FamilyTreeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FamilyEvent>()
            .HasOne(e => e.Member)
            .WithMany()
            .HasForeignKey(e => e.MemberId)
            .OnDelete(DeleteBehavior.SetNull);

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
        modelBuilder.Entity<Member>()
            .HasQueryFilter(m => m.FamilyTreeId == _currentFamilyTreeService.FamilyTreeId);

        modelBuilder.Entity<ParentChildRelationship>()
            .HasQueryFilter(pc => pc.FamilyTreeId == _currentFamilyTreeService.FamilyTreeId);

        modelBuilder.Entity<SpouseRelationship>()
            .HasQueryFilter(s => s.FamilyTreeId == _currentFamilyTreeService.FamilyTreeId);

        modelBuilder.Entity<RoleGroup>()
            .HasQueryFilter(rg => rg.FamilyTreeId == _currentFamilyTreeService.FamilyTreeId);

        modelBuilder.Entity<FamilyEvent>()
            .HasQueryFilter(e => e.FamilyTreeId == _currentFamilyTreeService.FamilyTreeId);

        modelBuilder.Entity<GalleryImage>()
            .HasOne(image => image.FamilyTree)
            .WithMany()
            .HasForeignKey(image => image.FamilyTreeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GalleryImage>()
            .HasOne(image => image.Member)
            .WithMany()
            .HasForeignKey(image => image.MemberId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<GalleryImage>()
            .HasQueryFilter(image => image.FamilyTreeId == _currentFamilyTreeService.FamilyTreeId);
    }
}