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
public class FamilyEventController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICurrentFamilyTreeService _familyTreeService;

    public FamilyEventController(AppDbContext context, ICurrentFamilyTreeService familyTreeService)
    {
        _context = context;
        _familyTreeService = familyTreeService;
    }

    [HttpGet]
    [Authorize(Policy = "event.view")]
    public async Task<IActionResult> GetEvents()
    {
        var events = await _context.FamilyEvents
            .Include(item => item.Member)
            .OrderBy(item => item.EventDate)
            .Select(item => new
            {
                item.Id,
                item.Title,
                item.EventType,
                item.EventDate,
                item.Description,
                item.IsRecurringYearly,
                item.MemberId,
                MemberName = item.Member == null ? null : item.Member.FullName,
                Images = _context.GalleryImages.Where(image => image.EventId == item.Id).Select(image => new { image.Id, image.FileUrl, image.FileName, image.Caption, image.Notes }).ToList()
            })
            .ToListAsync();
        return Ok(events);
    }

    public class EventRequest
    {
        public Guid? MemberId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string EventType { get; set; } = "custom";
        public DateTime EventDate { get; set; }
        public string? Description { get; set; }
        public bool IsRecurringYearly { get; set; }
    }

    [HttpPost]
    [Authorize(Policy = "event.manage")]
    public async Task<IActionResult> CreateEvent(EventRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest(new { message = "Tên sự kiện không được để trống." });
        if (request.EventDate == default) return BadRequest(new { message = "Ngày sự kiện không hợp lệ." });
        var familyTreeId = _familyTreeService.FamilyTreeId;
        if (request.MemberId.HasValue && !await _context.Members.AnyAsync(member => member.Id == request.MemberId && member.FamilyTreeId == familyTreeId))
            return BadRequest(new { message = "Thành viên không thuộc cây gia phả hiện tại." });
        var item = new FamilyEvent
        {
            FamilyTreeId = familyTreeId,
            MemberId = request.MemberId,
            Title = request.Title.Trim(),
            EventType = request.EventType,
            EventDate = request.EventDate.Date,
            Description = request.Description,
            IsRecurringYearly = request.IsRecurringYearly
        };
        _context.FamilyEvents.Add(item);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã thêm sự kiện.", id = item.Id });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "event.manage")]
    public async Task<IActionResult> UpdateEvent(Guid id, EventRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.EventDate == default)
            return BadRequest(new { message = "Tên và ngày sự kiện không được để trống." });
        var familyTreeId = _familyTreeService.FamilyTreeId;
        var item = await _context.FamilyEvents.FirstOrDefaultAsync(eventItem => eventItem.Id == id);
        if (item == null) return NotFound();
        if (request.MemberId.HasValue && !await _context.Members.AnyAsync(member => member.Id == request.MemberId && member.FamilyTreeId == familyTreeId))
            return BadRequest(new { message = "Thành viên không thuộc cây gia phả hiện tại." });
        item.Title = request.Title.Trim(); item.EventType = request.EventType; item.EventDate = request.EventDate.Date;
        item.Description = request.Description; item.IsRecurringYearly = request.IsRecurringYearly; item.MemberId = request.MemberId;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã cập nhật sự kiện." });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "event.manage")]
    public async Task<IActionResult> DeleteEvent(Guid id)
    {
        var item = await _context.FamilyEvents.FirstOrDefaultAsync(eventItem => eventItem.Id == id);
        if (item == null) return NotFound();
        _context.FamilyEvents.Remove(item);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã xóa sự kiện." });
    }
}
