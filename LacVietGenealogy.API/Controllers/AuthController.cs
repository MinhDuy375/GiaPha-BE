using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LacVietGenealogy.Core.Entities;
using LacVietGenealogy.Core.Interfaces;
using LacVietGenealogy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace LacVietGenealogy.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;

        public AuthController(AppDbContext context, IPasswordHasher passwordHasher, ITokenService tokenService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
        }

        public class RegisterRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string DefaultFamilyTreeName { get; set; } = string.Empty;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest(new { message = "Email đã tồn tại." });

            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                return BadRequest(new { message = "Username đã tồn tại." });

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                PasswordHash = _passwordHasher.Hash(request.Password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user);

            // Tự động tạo 1 gia phả mặc định nếu có tên gia phả trong request
            var treeName = string.IsNullOrWhiteSpace(request.DefaultFamilyTreeName) 
                ? $"Gia phả họ {request.Username}" 
                : request.DefaultFamilyTreeName;

            var familyTree = new FamilyTree
            {
                Id = Guid.NewGuid(),
                Name = treeName,
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            await _context.FamilyTrees.AddAsync(familyTree);

            // Gán quyền Admin mặc định cho Owner trong dòng họ mới tạo này thông qua FamilyTreeMembership
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin")
                ?? throw new InvalidOperationException("Default Admin role not found. Run DbSeeder first.");

            var membership = new FamilyTreeMembership
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                FamilyTreeId = familyTree.Id,
                RoleId = adminRole.Id,
                Status = MembershipStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            await _context.FamilyTreeMemberships.AddAsync(membership);

            // Thêm bản ghi UserRole để đồng bộ quyền
            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = adminRole.Id,
                FamilyTreeId = familyTree.Id
            };
            await _context.UserRoles.AddAsync(userRole);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Đăng ký thành công.", userId = user.Id });
        }

        public class LoginRequest
        {
            public string EmailOrUsername { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => 
                u.Email == request.EmailOrUsername || u.Username == request.EmailOrUsername);

            if (user == null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
                return Unauthorized(new { message = "Tài khoản hoặc mật khẩu không chính xác." });

            if (!user.IsActive)
                return BadRequest(new { message = "Tài khoản đã bị vô hiệu hóa." });

            // JWT#1: Chỉ có User info
            var accessToken = _tokenService.CreateAccessToken(user);
            var refreshTokenStr = _tokenService.CreateRefreshToken();

            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = refreshTokenStr,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

            // Lấy danh sách dòng họ mà user thuộc về để FE hiển thị lựa chọn
            var memberships = await _context.FamilyTreeMemberships
                .Where(m => m.UserId == user.Id && m.Status == MembershipStatus.Active)
                .Select(m => new { m.FamilyTreeId, m.FamilyTree.Name })
                .ToListAsync();

            return Ok(new
            {
                accessToken,
                refreshToken = refreshTokenStr,
                user = new { user.Id, user.Username, user.Email },
                familyTrees = memberships
            });
        }

        public class SelectTreeRequest
        {
            public Guid FamilyTreeId { get; set; }
        }

        [Authorize]
        [HttpPost("select-tree")]
        public async Task<IActionResult> SelectTree([FromBody] SelectTreeRequest request)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Unauthorized();

            // Kiểm tra membership của user trong dòng họ này
            var membership = await _context.FamilyTreeMemberships
                .Include(m => m.Role)
                .FirstOrDefaultAsync(m => m.UserId == userId && m.FamilyTreeId == request.FamilyTreeId && m.Status == MembershipStatus.Active);

            if (membership == null)
                return Forbid("Bạn không có quyền truy cập vào dòng họ này.");

            // Lấy danh sách các permissions gán cho Role của user trong dòng họ này
            var permissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == membership.RoleId)
                .Select(rp => rp.Permission.Code)
                .ToListAsync();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            // JWT#2: Token có chứa thông tin phân quyền và tenant dòng họ
            var token = _tokenService.CreateFamilyTreeToken(user, request.FamilyTreeId, membership.Role.Name, permissions);

            return Ok(new
            {
                accessToken = token,
                role = membership.Role.Name,
                permissions = permissions
            });
        }

        public class RefreshRequest
        {
            public string RefreshToken { get; set; } = string.Empty;
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            var storedToken = await _context.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

            if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
                return Unauthorized(new { message = "Refresh token không hợp lệ hoặc đã hết hạn." });

            // Tạo Token mới
            var newAccessToken = _tokenService.CreateAccessToken(storedToken.User);
            var newRefreshTokenStr = _tokenService.CreateRefreshToken();

            // Hủy token cũ
            storedToken.IsRevoked = true;

            // Lưu token mới
            var newRefreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = newRefreshTokenStr,
                UserId = storedToken.UserId,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.RefreshTokens.AddAsync(newRefreshToken);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                accessToken = newAccessToken,
                refreshToken = newRefreshTokenStr
            });
        }
    }
}