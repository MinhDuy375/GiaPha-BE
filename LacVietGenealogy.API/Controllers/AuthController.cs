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
        private readonly ILogger<AuthController> _logger;

        public AuthController(AppDbContext context, IPasswordHasher passwordHasher, ITokenService tokenService, ILogger<AuthController> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _logger = logger;
        }

        public class RegisterRequest
        {
            public string Email { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string DefaultFamilyTreeName { get; set; } = string.Empty;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest(new { message = "Email đã tồn tại." });

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Email, // Use Email as Username
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = _passwordHasher.Hash(request.Password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user);
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
                .IgnoreQueryFilters()
                .Where(m => m.UserId == user.Id && m.Status == MembershipStatus.Active)
                .Select(m => new { m.FamilyTreeId, m.FamilyTree.Name })
                .ToListAsync();

            var joinRequests = await _context.FamilyTreeMemberships
                .IgnoreQueryFilters()
                .Where(m => m.UserId == user.Id && (m.Status == MembershipStatus.Pending || m.Status == MembershipStatus.Rejected))
                .Select(m => new
                {
                    m.FamilyTreeId,
                    FamilyTreeName = m.FamilyTree.Name,
                    JoinCode = m.FamilyTree.JoinCode,
                    Status = m.Status.ToString(),
                    m.CreatedAt
                })
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return Ok(new
            {
                accessToken,
                refreshToken = refreshTokenStr,
                user = new { user.Id, user.Username, user.Email, user.FullName, user.MustChangePassword },
                familyTrees = memberships,
                joinRequests
            });
        }

        public class ForgotPasswordRequest
        {
            public string Email { get; set; } = string.Empty;
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, [FromServices] IEmailService emailService)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                // Vẫn trả về thành công để tránh dò rỉ email
                return Ok(new { message = "Nếu email hợp lệ, một mật khẩu tạm thời sẽ được gửi đến bạn." });
            }

            // Generate temp password
            var tempPassword = Guid.NewGuid().ToString().Substring(0, 8);

            // Send email
            var subject = "Lạc Việt Gia Phả - Khôi phục mật khẩu";
            var body = $"<p>Chào {user.FullName},</p><p>Mật khẩu tạm thời của bạn là: <strong>{tempPassword}</strong></p><p>Vui lòng đăng nhập và đổi mật khẩu ngay lập tức.</p>";
            var emailSent = await emailService.SendEmailAsync(user.Email, subject, body);
            _logger.LogInformation("Password recovery email result for {Email}: Sent={EmailSent}", user.Email, emailSent);

            if (!emailSent)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Không thể gửi email khôi phục. Máy chủ email có thể đã vượt giới hạn gửi, mật khẩu của bạn chưa bị thay đổi." });

            user.PasswordHash = _passwordHasher.Hash(tempPassword);
            user.MustChangePassword = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Nếu email hợp lệ, một mật khẩu tạm thời sẽ được gửi đến bạn." });
        }

        public class ChangePasswordRequest
        {
            public string OldPassword { get; set; } = string.Empty;
            public string NewPassword { get; set; } = string.Empty;
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (!_passwordHasher.Verify(request.OldPassword, user.PasswordHash))
                return BadRequest(new { message = "Mật khẩu cũ không chính xác." });

            user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
            user.MustChangePassword = false;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Đổi mật khẩu thành công." });
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
                .IgnoreQueryFilters()
                .Include(m => m.RoleGroup)
                .FirstOrDefaultAsync(m => m.UserId == userId && m.FamilyTreeId == request.FamilyTreeId && m.Status == MembershipStatus.Active);

            if (membership == null)
                return Forbid("Bạn không có quyền truy cập vào dòng họ này.");

            // Lấy danh sách các permissions gán cho Role của user trong dòng họ này
            var permissions = await _context.RoleGroupPermissions
                .Where(rp => rp.RoleGroupId == membership.RoleGroupId)
                .Select(rp => rp.Permission.Code)
                .ToListAsync();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            // JWT#2: Token có chứa thông tin phân quyền và tenant dòng họ
            var token = _tokenService.CreateFamilyTreeToken(user, request.FamilyTreeId, membership.RoleGroup.Name, permissions);

            return Ok(new
            {
                accessToken = token,
                role = membership.RoleGroup.Name,
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