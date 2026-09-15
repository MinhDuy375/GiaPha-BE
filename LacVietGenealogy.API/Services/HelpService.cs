using LacVietGenealogy.API.Services.Interfaces;

namespace LacVietGenealogy.API.Services;

public class HelpService : IHelpService
{
    private readonly Dictionary<string, string> _helpMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["add_member"] = "Vào Thành viên → Thêm thành viên → nhập thông tin → nhấn Lưu.",
        ["view_tree"] = "Vào Cây gia phả để xem sơ đồ quan hệ của dòng họ.",
        ["edit_member"] = "Mở thông tin thành viên → chọn Chỉnh sửa → cập nhật thông tin → Lưu.",
        ["search_member"] = "Sử dụng ô tìm kiếm ở màn hình danh sách thành viên để tìm theo tên.",
        ["change_password"] = "Vào Tài khoản → Đổi mật khẩu → nhập mật khẩu cũ và mật khẩu mới → Xác nhận.",
        ["create_family_tree"] = "Vào Quản lý dòng họ → Tạo gia phả mới → nhập tên và lưu lại.",
        ["general"] = "Bạn có thể hỏi về: thêm thành viên, xem cây gia phả, sửa thành viên, tìm kiếm thành viên, đổi mật khẩu hoặc tạo gia phả."
    };

    public Task<string> GetHelpAsync(string topic, CancellationToken cancellationToken = default)
    {
        var normalized = (topic ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return Task.FromResult(_helpMap["general"]);

        if (_helpMap.TryGetValue(normalized, out var answer))
            return Task.FromResult(answer);

        if (normalized.Contains("thêm thành viên") || normalized.Contains("add member") || normalized.Contains("thêm con"))
            return Task.FromResult(_helpMap["add_member"]);

        if (normalized.Contains("xem cây") || normalized.Contains("tree"))
            return Task.FromResult(_helpMap["view_tree"]);

        if (normalized.Contains("sửa") || normalized.Contains("edit"))
            return Task.FromResult(_helpMap["edit_member"]);

        if (normalized.Contains("tìm kiếm") || normalized.Contains("search"))
            return Task.FromResult(_helpMap["search_member"]);

        if (normalized.Contains("mật khẩu") || normalized.Contains("password"))
            return Task.FromResult(_helpMap["change_password"]);

        if (normalized.Contains("gia phả") || normalized.Contains("family tree") || normalized.Contains("tạo gia phả"))
            return Task.FromResult(_helpMap["create_family_tree"]);

        return Task.FromResult(_helpMap["general"]);
    }
}
