using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LacVietGenealogy.Core.Entities;
using LacVietGenealogy.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LacVietGenealogy.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    // ── JWT#1: chỉ có user_id ──────────────────────────────────────────────
    public string CreateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("token_type", "user")
        };
        return BuildToken(claims, GetAccessExpireMinutes());
    }

    // ── JWT#2: user_id + family_tree_id + role + permissions ─────────────
    public string CreateFamilyTreeToken(User user, Guid familyTreeId, string roleName, IEnumerable<string> permissions)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("token_type", "family_tree"),
            new("family_tree_id", familyTreeId.ToString()),
            new("role", roleName)
        };

        // Thêm từng permission code vào claims
        foreach (var perm in permissions)
            claims.Add(new Claim("permissions", perm));

        return BuildToken(claims, GetAccessExpireMinutes());
    }

    // ── RefreshToken: 64 bytes ngẫu nhiên ────────────────────────────────
    public string CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private string BuildToken(IEnumerable<Claim> claims, int expireMinutes)
    {
        var secret = _config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret chưa được cấu hình.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private int GetAccessExpireMinutes() =>
        int.TryParse(_config["Jwt:AccessTokenExpireMinutes"], out var m) ? m : 60;
}
