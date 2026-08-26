using LacVietGenealogy.Core.Interfaces;

namespace LacVietGenealogy.API.Services;

public class CurrentFamilyTreeService : ICurrentFamilyTreeService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentFamilyTreeService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid FamilyTreeId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst("family_tree_id");
            if (claim == null || !Guid.TryParse(claim.Value, out var id))
                throw new UnauthorizedAccessException("Thiếu hoặc không hợp lệ family_tree_id claim trong token.");
            return id;
        }
    }
}