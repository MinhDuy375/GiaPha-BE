using System.Security.Claims;
using LacVietGenealogy.API.DTOs;
using LacVietGenealogy.API.Services.Interfaces;
using LacVietGenealogy.Core.Entities;
using LacVietGenealogy.Core.Interfaces;
using LacVietGenealogy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LacVietGenealogy.API.Services;

public class ChatService : IChatService
{
    private readonly AppDbContext _context;
    private readonly IAIService _aiService;
    private readonly IGenealogyService _genealogyService;
    private readonly IHelpService _helpService;
    private readonly ICurrentFamilyTreeService _familyTreeService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        AppDbContext context,
        IAIService aiService,
        IGenealogyService genealogyService,
        IHelpService helpService,
        ICurrentFamilyTreeService familyTreeService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ChatService> logger)
    {
        _context = context;
        _aiService = aiService;
        _genealogyService = genealogyService;
        _helpService = helpService;
        _familyTreeService = familyTreeService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<ChatResponse> ProcessAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var message = (request.Message ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Nội dung tin nhắn không được để trống.", nameof(request));

        var userId = GetCurrentUserId();
        var conversation = await GetOrCreateConversationAsync(userId, request.ConversationId, message, cancellationToken);

        await SaveMessageAsync(conversation.Id, "user", message, cancellationToken);

        var intent = await _aiService.ExtractIntentAsync(message, cancellationToken);
        var resultText = await HandleIntentAsync(intent, message, cancellationToken);
        var finalMessage = await _aiService.GenerateNaturalResponseAsync(message, resultText, cancellationToken);

        await SaveMessageAsync(conversation.Id, "assistant", finalMessage, cancellationToken);
        conversation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return new ChatResponse
        {
            ConversationId = conversation.Id,
            Message = finalMessage,
            Intent = intent.Intent
        };
    }

    private async Task<ChatConversation> GetOrCreateConversationAsync(Guid userId, Guid? conversationId, string message, CancellationToken cancellationToken)
    {
        if (conversationId.HasValue)
        {
            var existing = await _context.ChatConversations
                .FirstOrDefaultAsync(c => c.Id == conversationId.Value && c.UserId == userId, cancellationToken);

            if (existing != null)
            {
                existing.UpdatedAt = DateTime.UtcNow;
                return existing;
            }
        }

        var conversation = new ChatConversation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FamilyTreeId = _familyTreeService.FamilyTreeId,
            Title = BuildConversationTitle(message),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ChatConversations.Add(conversation);
        await _context.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    private async Task SaveMessageAsync(Guid conversationId, string role, string content, CancellationToken cancellationToken)
    {
        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = role,
            Content = content,
            TokensUsed = 0,
            CreatedAt = DateTime.UtcNow
        };

        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> HandleIntentAsync(DTOs.AIIntentResponse intent, string originalMessage, CancellationToken cancellationToken)
    {
        var normalizedIntent = (intent.Intent ?? "unknown").Trim();

        if (string.Equals(normalizedIntent, "help", StringComparison.OrdinalIgnoreCase))
        {
            var topic = string.IsNullOrWhiteSpace(intent.Topic) ? "general" : intent.Topic;
            return await _helpService.GetHelpAsync(topic, cancellationToken);
        }

        if (string.Equals(normalizedIntent, "unknown", StringComparison.OrdinalIgnoreCase))
            return "Xin lỗi, tôi chưa hiểu rõ câu hỏi này. Hãy thử hỏi về bố/mẹ/ông/bà, con cái, vợ/chồng, quan hệ giữa hai người, hoặc cách sử dụng hệ thống.";

        var personName1 = intent.PersonName;
        var personName2 = intent.PersonName2;

        if (string.IsNullOrWhiteSpace(personName1) && normalizedIntent is not "help" and not "unknown")
            return "Tôi chưa xác định được tên người trong câu hỏi. Vui lòng nói rõ tên thành viên.";

        switch (normalizedIntent)
        {
            case "get_father":
                return await ResolveSinglePersonResultAsync(personName1, async name => await _genealogyService.GetFatherAsync(name, cancellationToken), "bố", "bố của");
            case "get_mother":
                return await ResolveSinglePersonResultAsync(personName1, async name => await _genealogyService.GetMotherAsync(name, cancellationToken), "mẹ", "mẹ của");
            case "get_grandfather":
                return await ResolveSinglePersonResultAsync(personName1, async name => await _genealogyService.GetGrandfatherAsync(name, cancellationToken), "ông nội/ông ngoại", "ông của");
            case "get_grandmother":
                return await ResolveSinglePersonResultAsync(personName1, async name => await _genealogyService.GetGrandmotherAsync(name, cancellationToken), "bà nội/bà ngoại", "bà của");
            case "get_children":
                return await ResolveCollectionResultAsync(personName1, async name => await _genealogyService.GetChildrenAsync(name, cancellationToken), "người con", "con của");
            case "get_spouse":
                return await ResolveSinglePersonResultAsync(personName1, async name => await _genealogyService.GetSpouseAsync(name, cancellationToken), "vợ/chồng", "vợ/chồng của");
            case "get_siblings":
                return "Chức năng anh/chị/em chưa được triển khai đầy đủ trong phiên bản này. Bạn có thể yêu cầu tìm thông tin về từng thành viên hoặc quan hệ giữa hai người.";
            case "get_relationship":
                if (string.IsNullOrWhiteSpace(personName1) || string.IsNullOrWhiteSpace(personName2))
                    return "Tôi cần xác định rõ tên của hai người để so sánh quan hệ.";

                var relationship = await _genealogyService.GetRelationshipAsync(personName1, personName2, cancellationToken);
                if (relationship is not null)
                    return relationship;

                return $"Hiện tại gia phả chưa có thông tin về quan hệ giữa {personName1} và {personName2}.";
            case "get_person":
                return await ResolveSinglePersonResultAsync(personName1, async name => await _genealogyService.GetPersonAsync(name, cancellationToken), "thành viên", "thành viên");
            case "get_generation":
                if (string.IsNullOrWhiteSpace(personName1))
                    return "Tôi chưa xác định được tên người cần tra cứu đời thứ.";

                var matches = await _genealogyService.FindPeopleAsync(personName1, cancellationToken);
                if (matches.Count > 1)
                    return $"Tìm thấy nhiều thành viên có tên {personName1}. Vui lòng chọn người phù hợp.";

                var person = await _genealogyService.GetPersonAsync(personName1, cancellationToken);
                if (person is null)
                    return $"Hiện tại gia phả chưa có thông tin về {personName1}.";

                return $"{person.FullName} thuộc đời thứ {person.GenerationLevel}.";
            default:
                return "Xin lỗi, hiện tại tôi chưa hỗ trợ loại câu hỏi này.";
        }
    }

    private async Task<string> ResolveSinglePersonResultAsync(string? personName, Func<string, Task<object?>> lookup, string relationshipLabel, string textPrefix)
    {
        if (string.IsNullOrWhiteSpace(personName))
            return $"Tôi chưa xác định được tên người cần tra cứu {relationshipLabel}.";

        var matches = await _genealogyService.FindPeopleAsync(personName);
        if (matches.Count > 1)
            return $"Tìm thấy nhiều thành viên có tên {personName}. Vui lòng chọn người phù hợp.";

        var result = await lookup(personName);
        if (result is null)
            return $"Hiện tại gia phả chưa có thông tin về {relationshipLabel} của {personName}.";

        var member = (Core.Entities.Member)result;
        return $"{textPrefix} {personName} là {member.FullName}.";
    }

    private async Task<string> ResolveCollectionResultAsync(string? personName, Func<string, Task<object?>> lookup, string collectionLabel, string textPrefix)
    {
        if (string.IsNullOrWhiteSpace(personName))
            return $"Tôi chưa xác định được tên người cần tra cứu {collectionLabel}.";

        var matches = await _genealogyService.FindPeopleAsync(personName);
        if (matches.Count > 1)
            return $"Tìm thấy nhiều thành viên có tên {personName}. Vui lòng chọn người phù hợp.";

        var result = await lookup(personName);
        if (result is null)
            return $"Hiện tại gia phả chưa có thông tin về {collectionLabel} của {personName}.";

        var members = (List<Core.Entities.Member>)result;
        if (members.Count == 0)
            return $"{personName} hiện chưa có {collectionLabel} nào trong gia phả.";

        var names = string.Join(", ", members.Select(m => m.FullName));
        return $"{textPrefix} {personName} có những {collectionLabel}: {names}.";
    }

    private Guid GetCurrentUserId()
    {
        var claim = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(claim) || !Guid.TryParse(claim, out var userId))
            throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng hợp lệ.");

        return userId;
    }

    private static string BuildConversationTitle(string message)
    {
        var clean = message.Trim();
        if (clean.Length <= 40)
            return clean;

        return clean[..37].TrimEnd() + "...";
    }
}
