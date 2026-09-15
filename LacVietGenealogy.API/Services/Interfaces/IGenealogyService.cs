using LacVietGenealogy.Core.Entities;

namespace LacVietGenealogy.API.Services.Interfaces;

public interface IGenealogyService
{
    Task<List<Member>> FindPeopleAsync(string personName, CancellationToken cancellationToken = default);
    Task<Member?> GetPersonAsync(string personName, CancellationToken cancellationToken = default);
    Task<Member?> GetFatherAsync(string personName, CancellationToken cancellationToken = default);
    Task<Member?> GetMotherAsync(string personName, CancellationToken cancellationToken = default);
    Task<Member?> GetGrandfatherAsync(string personName, CancellationToken cancellationToken = default);
    Task<Member?> GetGrandmotherAsync(string personName, CancellationToken cancellationToken = default);
    Task<List<Member>> GetChildrenAsync(string personName, CancellationToken cancellationToken = default);
    Task<List<Member>> GetSiblingsAsync(string personName, CancellationToken cancellationToken = default);
    Task<Member?> GetSpouseAsync(string personName, CancellationToken cancellationToken = default);
    Task<int?> GetGenerationAsync(string personName, CancellationToken cancellationToken = default);
    Task<string?> GetRelationshipAsync(string personName1, string personName2, CancellationToken cancellationToken = default);
}
