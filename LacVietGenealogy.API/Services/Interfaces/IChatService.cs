using LacVietGenealogy.API.DTOs;

namespace LacVietGenealogy.API.Services.Interfaces;

public interface IChatService
{
    Task<ChatResponse> ProcessAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
