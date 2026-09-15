using LacVietGenealogy.API.DTOs;

namespace LacVietGenealogy.API.Services.Interfaces;

public interface IAIService
{
    Task<AIIntentResponse> ExtractIntentAsync(string userMessage, CancellationToken cancellationToken = default);
    Task<string> GenerateNaturalResponseAsync(string question, string verifiedResult, CancellationToken cancellationToken = default);
}
