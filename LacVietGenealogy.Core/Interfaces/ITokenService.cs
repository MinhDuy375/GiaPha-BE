using LacVietGenealogy.Core.Entities;

namespace LacVietGenealogy.Core.Interfaces;

public interface ITokenService
{
    /// <summary>
    /// Tạo JWT#1 — chỉ chứa user_id. Dùng sau khi login, trước khi chọn dòng họ.
    /// </summary>
    string CreateAccessToken(User user);

    /// <summary>
    /// Tạo JWT#2 — chứa user_id + family_tree_id + role + permissions.
    /// Dùng sau khi user chọn dòng họ (select-tree).
    /// </summary>
    string CreateFamilyTreeToken(User user, Guid familyTreeId, string roleName, IEnumerable<string> permissions);

    /// <summary>Tạo refresh token ngẫu nhiên (64 bytes base64).</summary>
    string CreateRefreshToken();
}
