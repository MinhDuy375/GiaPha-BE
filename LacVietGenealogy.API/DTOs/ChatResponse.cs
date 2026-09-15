namespace LacVietGenealogy.API.DTOs;

public class ChatResponse
{
    public Guid ConversationId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Intent { get; set; } = "unknown";
}
