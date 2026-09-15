namespace LacVietGenealogy.API.Services.Interfaces;

public interface IHelpService
{
    Task<string> GetHelpAsync(string topic, CancellationToken cancellationToken = default);
}
