using LacVietGenealogy.API.Services.Interfaces;

namespace LacVietGenealogy.API.Services;

public class HelpService : IHelpService
{
    private readonly Dictionary<string, string> _helpMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["add_member"] = "Vào Sơ đồ cây → chọn Thêm thành viên mới → nhập thông tin → nhấn Lưu.",
        ["view_tree"] = "Vào Sơ đồ cây để xem sơ đồ quan hệ của dòng họ.",
        ["edit_member"] = "Vào Sơ đồ cây → chọn thành viên → chọn Chỉnh sửa thành viên → cập nhật thông tin → nhấn Lưu.",
        ["search_member"] = "Vào Danh sách thành viên → nhập tên vào ô Tìm tên thành viên để tra cứu.",
        ["change_password"] = "Vào Thông tin cá nhân → chọn Đổi mật khẩu → nhập Mật khẩu hiện tại, Mật khẩu mới và Xác nhận mật khẩu mới → nhấn Đổi mật khẩu.",
        ["create_family_tree"] = "Vào Chọn gia phả làm việc → chọn Tạo gia phả mới → nhập Tên gia phả / Dòng họ → nhấn Tạo gia phả.",
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
