using LacVietGenealogy.Core.Entities;
using LacVietGenealogy.Core.Interfaces;
using LacVietGenealogy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LacVietGenealogy.API.Controllers;

[ApiController]
[Route("api/gallery")]
[Authorize]
public class GalleryController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICurrentFamilyTreeService _familyTreeService;
    private readonly IWebHostEnvironment _environment;

    public GalleryController(AppDbContext context, ICurrentFamilyTreeService familyTreeService, IWebHostEnvironment environment)
    {
        _context = context;
        _familyTreeService = familyTreeService;
        _environment = environment;
    }

    [HttpGet]
    [Authorize(Policy = "gallery.view")]
    public async Task<IActionResult> GetImages()
    {
        return Ok(await _context.GalleryImages.Include(image => image.Member).OrderByDescending(image => image.CreatedAt).Select(image => new
        {
            image.Id,
            image.FileUrl,
            image.FileName,
            image.Caption,
            image.Notes,
            image.CreatedAt,
            image.MemberId,
            MemberName = image.Member == null ? null : image.Member.FullName,
            image.EventId,
            EventName = image.Event == null ? null : image.Event.Title
        }).ToListAsync());
    }

    [HttpPost]
    [Authorize(Policy = "gallery.manage")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] string? caption, [FromForm] string? notes, [FromForm] Guid? memberId, [FromForm] Guid? eventId)
    {
        if (file == null || file.Length == 0) return BadRequest(new { message = "Vui lòng chọn ảnh." });
        if (!new[] { "image/jpeg", "image/png", "image/webp", "image/gif" }.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Định dạng ảnh không được hỗ trợ." });
        var treeId = _familyTreeService.FamilyTreeId;
        if (memberId.HasValue && !await _context.Members.AnyAsync(member => member.Id == memberId && member.FamilyTreeId == treeId)) return BadRequest(new { message = "Thành viên không thuộc cây hiện tại." });
        if (eventId.HasValue && !await _context.FamilyEvents.AnyAsync(item => item.Id == eventId && item.FamilyTreeId == treeId)) return BadRequest(new { message = "Sự kiện không thuộc cây hiện tại." });
        var folder = Path.Combine("uploads", "gallery", treeId.ToString());
        var absoluteFolder = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), folder);
        Directory.CreateDirectory(absoluteFolder);
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var id = Guid.NewGuid();
        var fileName = $"{id}{extension}";
        await using (var stream = System.IO.File.Create(Path.Combine(absoluteFolder, fileName))) await file.CopyToAsync(stream);
        var image = new GalleryImage { Id = id, FamilyTreeId = treeId, MemberId = memberId, EventId = eventId, FileName = file.FileName, FileUrl = $"/{folder.Replace('\\', '/')}/{fileName}", Caption = caption, Notes = notes };
        _context.GalleryImages.Add(image); await _context.SaveChangesAsync();
        return Ok(new { image.Id, image.FileUrl });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "gallery.manage")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var image = await _context.GalleryImages.FirstOrDefaultAsync(item => item.Id == id);
        if (image == null) return NotFound();
        _context.GalleryImages.Remove(image); await _context.SaveChangesAsync();
        return Ok(new { message = "Đã xóa ảnh." });
    }
}
