namespace LacVietGenealogy.Core.Interfaces;

/// <summary>
/// Cung cấp FamilyTreeId hiện tại của phiên làm việc (đọc từ JWT claim "family_tree_id").
/// Chỉ hợp lệ sau khi user đã thực hiện bước "chọn dòng họ" (nhận JWT#2).
/// </summary>
public interface ICurrentFamilyTreeService
{
    Guid FamilyTreeId { get; }
}