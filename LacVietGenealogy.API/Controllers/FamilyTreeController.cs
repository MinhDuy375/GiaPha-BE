using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LacVietGenealogy.Core.Entities;
using LacVietGenealogy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace LacVietGenealogy.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FamilyTreeController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FamilyTreeController(AppDbContext context)
        {
            _context = context;
        }

        public class CreateTreeRequest
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> CreateFamilyTree([FromBody] CreateTreeRequest request)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var familyTree = new FamilyTree
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                OwnerUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _context.FamilyTrees.AddAsync(familyTree);

            // Gán role Admin mặc định cho người sáng lập dòng họ này
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin")
                ?? throw new InvalidOperationException("Default Admin role not found. Run DbSeeder first.");

            var membership = new FamilyTreeMembership
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FamilyTreeId = familyTree.Id,
                RoleId = adminRole.Id,
                Status = MembershipStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            await _context.FamilyTreeMemberships.AddAsync(membership);

            // Thêm bản ghi UserRole để đồng bộ quyền
            var userRole = new UserRole
            {
                UserId = userId,
                RoleId = adminRole.Id,
                FamilyTreeId = familyTree.Id
            };
            await _context.UserRoles.AddAsync(userRole);

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(CreateFamilyTree), new { id = familyTree.Id }, familyTree);
        }

        [HttpGet]
        public async Task<IActionResult> GetMyFamilyTrees()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var trees = await _context.FamilyTreeMemberships
                .Where(m => m.UserId == userId && m.Status == MembershipStatus.Active)
                .Select(m => new
                {
                    m.FamilyTreeId,
                    m.FamilyTree.Name,
                    m.FamilyTree.Description,
                    Role = m.Role.Name,
                    m.FamilyTree.CreatedAt
                })
                .ToListAsync();

            return Ok(trees);
        }
    }
}