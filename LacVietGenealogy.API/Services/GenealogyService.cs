using LacVietGenealogy.API.Services.Interfaces;
using LacVietGenealogy.Core.Entities;
using LacVietGenealogy.Core.Interfaces;
using LacVietGenealogy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LacVietGenealogy.API.Services;

public class GenealogyService : IGenealogyService
{
    private readonly AppDbContext _context;
    private readonly ICurrentFamilyTreeService _familyTreeService;

    public GenealogyService(AppDbContext context, ICurrentFamilyTreeService familyTreeService)
    {
        _context = context;
        _familyTreeService = familyTreeService;
    }

    public async Task<List<Member>> FindPeopleAsync(string personName, CancellationToken cancellationToken = default)
    {
        var normalized = personName?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return new List<Member>();

        var familyTreeId = _familyTreeService.FamilyTreeId;

        var candidates = await _context.Members
            .AsNoTracking()
            .Where(m => m.FamilyTreeId == familyTreeId)
            .ToListAsync(cancellationToken);

        var exactMatches = candidates
            .Where(m =>
                string.Equals(m.FullName, normalized, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(m.OtherNames) && m.OtherNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Any(name => string.Equals(name, normalized, StringComparison.OrdinalIgnoreCase))) ||
                (!string.IsNullOrWhiteSpace(m.CourtesyName) && string.Equals(m.CourtesyName, normalized, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(m.TabooName) && string.Equals(m.TabooName, normalized, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(m => m.FullName)
            .ToList();

        if (exactMatches.Count > 0)
            return exactMatches;

        return candidates
            .Where(m =>
                m.FullName.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(m.OtherNames) && m.OtherNames.Contains(normalized, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(m.CourtesyName) && m.CourtesyName.Contains(normalized, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(m.TabooName) && m.TabooName.Contains(normalized, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(m => m.FullName)
            .ToList();
    }

    public async Task<Member?> GetPersonAsync(string personName, CancellationToken cancellationToken = default)
    {
        var matches = await FindPeopleAsync(personName, cancellationToken);
        return matches.Count == 1 ? matches[0] : null;
    }

    public async Task<Member?> GetFatherAsync(string personName, CancellationToken cancellationToken = default)
    {
        var member = await GetPersonAsync(personName, cancellationToken);
        if (member == null)
            return null;

        return await _context.ParentChildRelationships
            .AsNoTracking()
            .Where(r => r.FamilyTreeId == _familyTreeService.FamilyTreeId && r.ChildId == member.Id)
            .Select(r => r.Parent)
            .FirstOrDefaultAsync(m => m.Gender == 0, cancellationToken);
    }

    public async Task<Member?> GetMotherAsync(string personName, CancellationToken cancellationToken = default)
    {
        var member = await GetPersonAsync(personName, cancellationToken);
        if (member == null)
            return null;

        return await _context.ParentChildRelationships
            .AsNoTracking()
            .Where(r => r.FamilyTreeId == _familyTreeService.FamilyTreeId && r.ChildId == member.Id)
            .Select(r => r.Parent)
            .FirstOrDefaultAsync(m => m.Gender == 1, cancellationToken);
    }

    public async Task<Member?> GetGrandfatherAsync(string personName, CancellationToken cancellationToken = default)
    {
        var father = await GetFatherAsync(personName, cancellationToken);
        if (father == null)
            return null;

        return await GetFatherAsync(father.FullName, cancellationToken);
    }

    public async Task<Member?> GetGrandmotherAsync(string personName, CancellationToken cancellationToken = default)
    {
        var mother = await GetMotherAsync(personName, cancellationToken);
        if (mother == null)
            return null;

        return await GetMotherAsync(mother.FullName, cancellationToken);
    }

    public async Task<List<Member>> GetChildrenAsync(string personName, CancellationToken cancellationToken = default)
    {
        var parent = await GetPersonAsync(personName, cancellationToken);
        if (parent == null)
            return new List<Member>();

        return await _context.ParentChildRelationships
            .AsNoTracking()
            .Where(r => r.FamilyTreeId == _familyTreeService.FamilyTreeId && r.ParentId == parent.Id)
            .OrderBy(r => r.ChildOrder)
            .Select(r => r.Child)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Member>> GetSiblingsAsync(string personName, CancellationToken cancellationToken = default)
    {
        var member = await GetPersonAsync(personName, cancellationToken);
        if (member == null)
            return new List<Member>();

        var parentIds = await _context.ParentChildRelationships
            .AsNoTracking()
            .Where(r => r.FamilyTreeId == _familyTreeService.FamilyTreeId && r.ChildId == member.Id)
            .Select(r => r.ParentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (parentIds.Count == 0)
            return new List<Member>();

        var siblingIds = await _context.ParentChildRelationships
            .AsNoTracking()
            .Where(r => r.FamilyTreeId == _familyTreeService.FamilyTreeId && parentIds.Contains(r.ParentId) && r.ChildId != member.Id)
            .Select(r => r.ChildId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (siblingIds.Count == 0)
            return new List<Member>();

        return await _context.Members
            .AsNoTracking()
            .Where(m => m.FamilyTreeId == _familyTreeService.FamilyTreeId && siblingIds.Contains(m.Id))
            .OrderBy(m => m.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Member?> GetSpouseAsync(string personName, CancellationToken cancellationToken = default)
    {
        var member = await GetPersonAsync(personName, cancellationToken);
        if (member == null)
            return null;

        var spouse = await _context.SpouseRelationships
            .AsNoTracking()
            .Where(r => r.FamilyTreeId == _familyTreeService.FamilyTreeId &&
                        (r.HusbandId == member.Id || r.WifeId == member.Id))
            .Select(r => r.HusbandId == member.Id ? r.Wife : r.Husband)
            .FirstOrDefaultAsync(cancellationToken);

        return spouse;
    }

    public async Task<int?> GetGenerationAsync(string personName, CancellationToken cancellationToken = default)
    {
        var member = await GetPersonAsync(personName, cancellationToken);
        return member?.GenerationLevel;
    }

    public async Task<string?> GetRelationshipAsync(string personName1, string personName2, CancellationToken cancellationToken = default)
    {
        var person1 = await GetPersonAsync(personName1, cancellationToken);
        var person2 = await GetPersonAsync(personName2, cancellationToken);

        if (person1 == null || person2 == null)
            return null;

        var familyTreeId = _familyTreeService.FamilyTreeId;

        var members = await _context.Members
            .AsNoTracking()
            .Where(m => m.FamilyTreeId == familyTreeId)
            .ToListAsync(cancellationToken);

        var parentChildRelationships = await _context.ParentChildRelationships
            .AsNoTracking()
            .Where(r => r.FamilyTreeId == familyTreeId)
            .ToListAsync(cancellationToken);

        var spouseRelationships = await _context.SpouseRelationships
            .AsNoTracking()
            .Where(r => r.FamilyTreeId == familyTreeId)
            .ToListAsync(cancellationToken);

        var result = KinshipCalculator.Calculate(person1.Id, person2.Id, members, parentChildRelationships, spouseRelationships);

        if (string.Equals(result.Description, "Cùng một người", StringComparison.OrdinalIgnoreCase))
            return "Cùng một người.";

        if (string.Equals(result.Description, "Quan hệ vợ chồng", StringComparison.OrdinalIgnoreCase))
            return $"{person1.FullName} và {person2.FullName} là vợ chồng.";

        if (!string.IsNullOrWhiteSpace(result.ACallsB) && !string.Equals(result.ACallsB, "Không rõ", StringComparison.OrdinalIgnoreCase))
            return $"{person1.FullName} gọi {person2.FullName} là {result.ACallsB.ToLowerInvariant()}.";

        if (!string.IsNullOrWhiteSpace(result.BCallsA) && !string.Equals(result.BCallsA, "Không rõ", StringComparison.OrdinalIgnoreCase))
            return $"{person2.FullName} gọi {person1.FullName} là {result.BCallsA.ToLowerInvariant()}.";

        return $"{person1.FullName} và {person2.FullName} có mối quan hệ họ hàng trong gia phả.";
    }
}
