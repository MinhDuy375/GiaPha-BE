using LacVietGenealogy.API.Services;
using LacVietGenealogy.Core.Entities;
using LacVietGenealogy.Core.Interfaces;
using LacVietGenealogy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LacVietGenealogy.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RelationshipController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICurrentFamilyTreeService _familyTreeService;

    public RelationshipController(AppDbContext context, ICurrentFamilyTreeService familyTreeService)
    {
        _context = context;
        _familyTreeService = familyTreeService;
    }

    [HttpGet]
    [Authorize(Policy = "relationship.view")]
    public async Task<IActionResult> GetRelationships()
    {
        var familyTreeId = _familyTreeService.FamilyTreeId;
        var members = await _context.Members
            .Where(member => member.FamilyTreeId == familyTreeId)
            .ToDictionaryAsync(member => member.Id, member => new { member.Id, member.FullName, member.Gender });

        var parentRelations = await _context.ParentChildRelationships
            .Where(relation => relation.FamilyTreeId == familyTreeId)
            .Select(relation => new
            {
                id = relation.Id,
                type = relation.RelationshipType == 1 ? "adopted_child" : "parent_child",
                parentId = relation.ParentId,
                childId = relation.ChildId,
                order = relation.ChildOrder
            })
            .ToListAsync();
        var spouseRelations = await _context.SpouseRelationships
            .Where(relation => relation.FamilyTreeId == familyTreeId)
            .Select(relation => new
            {
                id = relation.Id,
                type = "spouse",
                husbandId = relation.HusbandId,
                wifeId = relation.WifeId,
                order = relation.MarriageOrder
            })
            .ToListAsync();

        return Ok(new
        {
            parents = parentRelations.Select(relation => new
            {
                relation.id,
                relation.type,
                relation.order,
                personA = members.TryGetValue(relation.parentId, out var parent) ? parent : null,
                personB = members.TryGetValue(relation.childId, out var child) ? child : null
            }),
            spouses = spouseRelations.Select(relation => new
            {
                relation.id,
                relation.type,
                relation.order,
                personA = members.TryGetValue(relation.husbandId, out var husband) ? husband : null,
                personB = members.TryGetValue(relation.wifeId, out var wife) ? wife : null
            })
        });
    }

    public class CreateRelationshipRequest
    {
        public string Type { get; set; } = "parent_child";
        public Guid PersonAId { get; set; }
        public Guid PersonBId { get; set; }
        public int Order { get; set; } = 1;
    }

    [HttpPost]
    [Authorize(Policy = "relationship.manage")]
    public async Task<IActionResult> CreateRelationship(CreateRelationshipRequest request)
    {
        var familyTreeId = _familyTreeService.FamilyTreeId;
        if (request.PersonAId == request.PersonBId)
            return BadRequest(new { message = "Hai thành viên phải khác nhau." });
        if (request.Order < 1)
            return BadRequest(new { message = "Thứ tự phải lớn hơn 0." });

        var memberIds = await _context.Members
            .Where(member => member.FamilyTreeId == familyTreeId && (member.Id == request.PersonAId || member.Id == request.PersonBId))
            .Select(member => member.Id)
            .ToListAsync();
        if (memberIds.Count != 2)
            return BadRequest(new { message = "Thành viên không thuộc cây gia phả hiện tại." });

        if (request.Type is "parent_child" or "adopted_child")
        {
            var exists = await _context.ParentChildRelationships.AnyAsync(relation => relation.FamilyTreeId == familyTreeId && relation.ParentId == request.PersonAId && relation.ChildId == request.PersonBId);
            if (exists) return BadRequest(new { message = "Quan hệ cha/mẹ - con đã tồn tại." });
            _context.ParentChildRelationships.Add(new ParentChildRelationship
            {
                FamilyTreeId = familyTreeId,
                ParentId = request.PersonAId,
                ChildId = request.PersonBId,
                ChildOrder = request.Order,
                RelationshipType = request.Type == "adopted_child" ? 1 : 0
            });
        }
        else if (request.Type == "spouse")
        {
            var exists = await _context.SpouseRelationships.AnyAsync(relation => relation.FamilyTreeId == familyTreeId && ((relation.HusbandId == request.PersonAId && relation.WifeId == request.PersonBId) || (relation.HusbandId == request.PersonBId && relation.WifeId == request.PersonAId)));
            if (exists) return BadRequest(new { message = "Quan hệ vợ/chồng đã tồn tại." });
            var first = await _context.Members.FirstAsync(member => member.Id == request.PersonAId);
            var second = await _context.Members.FirstAsync(member => member.Id == request.PersonBId);
            _context.SpouseRelationships.Add(new SpouseRelationship
            {
                FamilyTreeId = familyTreeId,
                HusbandId = first.Gender == 0 ? first.Id : second.Id,
                WifeId = first.Gender == 0 ? second.Id : first.Id,
                MarriageOrder = request.Order
            });
        }
        else return BadRequest(new { message = "Loại quan hệ không hợp lệ." });

        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã thêm quan hệ." });
    }

    [HttpDelete("{type}/{id:guid}")]
    [Authorize(Policy = "relationship.manage")]
    public async Task<IActionResult> DeleteRelationship(string type, Guid id)
    {
        var familyTreeId = _familyTreeService.FamilyTreeId;
        if (type == "spouse")
        {
            var relation = await _context.SpouseRelationships.FirstOrDefaultAsync(item => item.Id == id && item.FamilyTreeId == familyTreeId);
            if (relation == null) return NotFound();
            _context.SpouseRelationships.Remove(relation);
        }
        else
        {
            var relation = await _context.ParentChildRelationships.FirstOrDefaultAsync(item => item.Id == id && item.FamilyTreeId == familyTreeId);
            if (relation == null) return NotFound();
            _context.ParentChildRelationships.Remove(relation);
        }
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã xóa quan hệ." });
    }
}
