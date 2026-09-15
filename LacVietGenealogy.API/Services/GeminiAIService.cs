using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using LacVietGenealogy.API.DTOs;
using LacVietGenealogy.API.Services.Interfaces;

namespace LacVietGenealogy.API.Services;

public class GeminiAIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiAIService> _logger;

    public GeminiAIService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<GeminiAIService> logger)
    {
        _httpClient = httpClientFactory.CreateClient(nameof(GeminiAIService));
        _httpClient.Timeout = TimeSpan.FromSeconds(20);
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AIIntentResponse> ExtractIntentAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        var message = (userMessage ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(message))
            return new AIIntentResponse();

        var apiKey = _configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return ExtractIntentWithHeuristics(message);

        try
        {
            var prompt = $@"Bạn là bộ phân tích ý định cho chatbot gia phả Việt Nam.
Trả về JSON thuần theo định dạng sau:
{{""intent"":""get_father"",""personName"":""Nguyễn Văn A"",""personName2"":null,""topic"":null}}
Chỉ hỗ trợ các intent: get_father, get_mother, get_grandfather, get_grandmother, get_children, get_spouse, get_siblings, get_relationship, get_person, get_generation, help, unknown.
Nếu hỏi cách sử dụng hệ thống thì intent='help' và topic là một trong: add_member, view_tree, edit_member, search_member, change_password, create_family_tree. Khi hướng dẫn, phải dùng đúng tên page/nút: 'Sơ đồ cây', 'Danh sách thành viên', 'Thông tin cá nhân', 'Đổi mật khẩu', 'Chọn gia phả làm việc', 'Tạo gia phả mới', 'Tạo gia phả', 'Thêm thành viên mới', 'Chỉnh sửa thành viên'.
Bạn chỉ giải đáp và hướng dẫn người dùng. Không thực hiện, không yêu cầu thực hiện và không mô phỏng thao tác thêm, sửa, xóa thành viên, quan hệ, gia phả hoặc bất kỳ dữ liệu nào.
Không suy đoán dữ liệu gia phả.
Chỉ trả về JSON, không markdown, không giải thích thêm.
Câu hỏi: {message}";

            var requestPayload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}");
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(requestPayload), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini intent extraction failed: {StatusCode} {Response}", response.StatusCode, responseText);
                return ExtractIntentWithHeuristics(message);
            }

            var candidateText = TryExtractGeminiText(responseText);
            if (string.IsNullOrWhiteSpace(candidateText))
                return ExtractIntentWithHeuristics(message);

            var intent = JsonSerializer.Deserialize<AIIntentResponse>(candidateText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return intent ?? ExtractIntentWithHeuristics(message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi gọi Gemini cho intent extraction. Dùng heuristic fallback.");
            return ExtractIntentWithHeuristics(message);
        }
    }

    public async Task<string> GenerateNaturalResponseAsync(string question, string verifiedResult, CancellationToken cancellationToken = default)
    {
        var cleanQuestion = (question ?? string.Empty).Trim();
        var cleanVerifiedResult = (verifiedResult ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(cleanQuestion) || string.IsNullOrWhiteSpace(cleanVerifiedResult))
            return cleanVerifiedResult;

        var apiKey = _configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return cleanVerifiedResult;

        try
        {
            var prompt = $@"Bạn là trợ lý viết lại câu trả lời cho chatbot gia phả.
Chỉ trả lời dựa trên dữ liệu do backend xác minh.
Question:
{cleanQuestion}
Verified database result:
{cleanVerifiedResult}
Instruction:
- Không được suy đoán thêm thông tin.
- Chỉ giải đáp hoặc hướng dẫn; không thực hiện thao tác thêm, sửa, xóa hay thay đổi dữ liệu.
- Chỉ trả lời ngắn gọn, tự nhiên bằng tiếng Việt.
- Không dùng markdown.
- Nếu không có dữ liệu, hãy trả lời theo kiểu: 'Hiện tại gia phả chưa có thông tin ...'";

            var requestPayload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}");
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(requestPayload), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return cleanVerifiedResult;

            var text = TryExtractGeminiText(responseText);
            return string.IsNullOrWhiteSpace(text) ? cleanVerifiedResult : text.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi tạo câu trả lời bằng Gemini. Dùng kết quả xác thực gốc.");
            return cleanVerifiedResult;
        }
    }

    private static AIIntentResponse ExtractIntentWithHeuristics(string message)
    {
        var normalized = message.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return new AIIntentResponse();

        var lower = normalized.ToLowerInvariant();
        var person1 = ExtractPersonName(normalized);
        var person2 = ExtractSecondPersonName(normalized);

        if (lower.Contains("thêm thành viên") || lower.Contains("thêm con") || lower.Contains("thêm bố mẹ") || lower.Contains("cách thêm thành viên"))
            return new AIIntentResponse { Intent = "help", Topic = "add_member" };

        if (lower.Contains("xem cây") || lower.Contains("cây gia phả") || lower.Contains("xem gia phả"))
            return new AIIntentResponse { Intent = "help", Topic = "view_tree" };

        if (lower.Contains("sửa thông tin") || lower.Contains("chỉnh sửa thành viên") || lower.Contains("sửa thành viên"))
            return new AIIntentResponse { Intent = "help", Topic = "edit_member" };

        if (lower.Contains("tìm kiếm thành viên") || lower.Contains("tìm thành viên") || lower.Contains("tìm kiếm"))
            return new AIIntentResponse { Intent = "help", Topic = "search_member" };

        if (lower.Contains("đổi mật khẩu") || lower.Contains("mật khẩu"))
            return new AIIntentResponse { Intent = "help", Topic = "change_password" };

        if (lower.Contains("tạo gia phả") || lower.Contains("tao gia pha") || lower.Contains("tạo dòng họ"))
            return new AIIntentResponse { Intent = "help", Topic = "create_family_tree" };

        if (lower.Contains("vợ") || lower.Contains("chồng") || lower.Contains("vợ chồng") || lower.Contains("spouse"))
            return new AIIntentResponse { Intent = "get_spouse", PersonName = person1 };

        if (lower.Contains("ông nội") || lower.Contains("ông") || lower.Contains("grandfather") || lower.Contains("ông ngoại") || lower.Contains("ông của"))
            return new AIIntentResponse { Intent = "get_grandfather", PersonName = person1 };

        if (lower.Contains("bà nội") || lower.Contains("bà ngoại") || lower.Contains("bà") || lower.Contains("grandmother"))
            return new AIIntentResponse { Intent = "get_grandmother", PersonName = person1 };

        if (lower.Contains("bố") || lower.Contains("cha") || lower.Contains("father") || lower.Contains("cha của"))
            return new AIIntentResponse { Intent = "get_father", PersonName = person1 };

        if (lower.Contains("mẹ") || lower.Contains("me") || lower.Contains("mother") || lower.Contains("mẹ của"))
            return new AIIntentResponse { Intent = "get_mother", PersonName = person1 };

        if (lower.Contains("có những người con") || lower.Contains("người con") || lower.Contains("con của") || lower.Contains("có con") || lower.Contains("con nào"))
            return new AIIntentResponse { Intent = "get_children", PersonName = person1 };

        if (lower.Contains("anh") || lower.Contains("chị") || lower.Contains("em") || lower.Contains("sibling") || lower.Contains("anh em"))
            return new AIIntentResponse { Intent = "get_siblings", PersonName = person1 };

        if (lower.Contains("đời thứ") || lower.Contains("thuộc đời") || lower.Contains("generation"))
            return new AIIntentResponse { Intent = "get_generation", PersonName = person1 };

        if (lower.Contains("quan hệ") || lower.Contains("liên quan") || lower.Contains("là gì của nhau") || lower.Contains("có quan hệ gì") || lower.Contains("quan hệ giữa"))
            return new AIIntentResponse { Intent = "get_relationship", PersonName = person1, PersonName2 = person2 };

        if (lower.Contains("thông tin về") || lower.Contains("thông tin ca nhân") || lower.Contains("tìm thông tin"))
            return new AIIntentResponse { Intent = "get_person", PersonName = person1 };

        if (person1 != null && person2 != null && !string.Equals(person1, person2, StringComparison.OrdinalIgnoreCase))
            return new AIIntentResponse { Intent = "get_relationship", PersonName = person1, PersonName2 = person2 };

        return new AIIntentResponse { Intent = "unknown" };
    }

    private static string? ExtractPersonName(string input)
    {
        var match = Regex.Match(input, @"(?:bố|cha|mẹ|me|ông|bà|vợ|chồng|con|thông tin về|tìm thông tin về|của|về)\s+(?:của\s+)?([A-ZÀ-Ỹ][A-Za-zÀ-ỹà-ỹ'\-\s]+?)(?:\?|\.|,|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        if (match.Success)
        {
            var value = match.Groups[1].Value.Trim();
            return CleanPersonName(value);
        }

        var names = ExtractCapitalizedNames(input);
        return names.Count > 0 ? names[0] : null;
    }

    private static string? ExtractSecondPersonName(string input)
    {
        var names = ExtractCapitalizedNames(input);
        if (names.Count >= 2)
            return names[1];

        var andIndex = input.IndexOf(" và ", StringComparison.OrdinalIgnoreCase);
        if (andIndex > 0)
        {
            var substring = input[(andIndex + 3)..];
            var second = ExtractCapitalizedNames(substring);
            return second.Count > 0 ? second[0] : null;
        }

        return null;
    }

    private static List<string> ExtractCapitalizedNames(string input)
    {
        var matches = Regex.Matches(input, @"\b[A-ZÀ-Ý][a-zà-ý]+(?:\s+[A-ZÀ-Ý][a-zà-ý]+)+\b");
        var names = new List<string>();

        foreach (Match match in matches)
        {
            var name = CleanPersonName(match.Value);
            if (!string.IsNullOrWhiteSpace(name))
                names.Add(name);
        }

        return names;
    }

    private static string CleanPersonName(string input)
    {
        var value = input.Trim();
        value = Regex.Replace(value, @"\s+", " ");
        value = value.TrimEnd('.', ',', '?', '!', ':', ';');
        return value;
    }

    private static string TryExtractGeminiText(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
            return string.Empty;

        var cleaned = responseText.Trim();
        var jsonMatch = Regex.Match(cleaned, @"\{.*\}", RegexOptions.Singleline);
        if (jsonMatch.Success)
            return jsonMatch.Value;

        var codeFenceMatch = Regex.Match(cleaned, @"```json\s*(.*)\s*```", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (codeFenceMatch.Success)
            return codeFenceMatch.Groups[1].Value.Trim();

        var codeFencePlain = Regex.Match(cleaned, @"```\s*(.*)\s*```", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (codeFencePlain.Success)
            return codeFencePlain.Groups[1].Value.Trim();

        return cleaned;
    }
}
