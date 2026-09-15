namespace LacVietGenealogy.API.DTOs;

public class AIIntentResponse
{
    public string Intent { get; set; } = "unknown";
    public string? PersonName { get; set; }
    public string? PersonName2 { get; set; }
    public string? Topic { get; set; }
}
