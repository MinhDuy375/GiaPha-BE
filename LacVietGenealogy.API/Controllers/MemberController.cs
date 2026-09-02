using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LacVietGenealogy.Core.Entities;
using LacVietGenealogy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using LacVietGenealogy.Core.Interfaces;
using LacVietGenealogy.API.Services;

namespace LacVietGenealogy.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MemberController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICurrentFamilyTreeService _familyTreeService;

        public MemberController(AppDbContext context, ICurrentFamilyTreeService familyTreeService)
        {
            _context = context;
            _familyTreeService = familyTreeService;
        }

        // GET /api/member - Lấy tất cả thành viên trong dòng họ
        [HttpGet]
        [Authorize(Policy = "member_list.view")]
        public async Task<IActionResult> GetMembers()
        {
            var familyTreeId = _familyTreeService.FamilyTreeId;

            var members = await _context.Members
                .Where(m => m.FamilyTreeId == familyTreeId)
                .Select(m => new
                {
                    m.Id,
                    m.FullName,
                    m.TabooName,
                    m.CourtesyName,
                    m.OtherNames,
                    m.Gender,
                    m.GenerationLevel,
                    m.BirthOrder,
                    m.IsInLaw,
                    BirthYear = m.BirthDateSolar.HasValue ? (int?)m.BirthDateSolar.Value.Year : null,
                    m.BirthMonth,
                    m.BirthDay,
                    DeathYear = m.DeathDateSolar.HasValue ? (int?)m.DeathDateSolar.Value.Year : null,
                    m.DeathMonth,
                    m.DeathDay,
                    m.BirthDateLunar,
                    m.BirthLunarYear,
                    m.BirthLunarMonth,
                    m.BirthLunarDay,
                    m.DeathDateLunar,
                    m.DeathLunarYear,
                    m.DeathLunarMonth,
                    m.DeathLunarDay,
                    m.IsAlive,
                    m.AvatarUrl,
                    m.Biography,
                    m.Note,
                    m.PhoneNumber,
                    m.Occupation,
                    m.CurrentResidence
                })
                .ToListAsync();

            return Ok(members);
        }

        // GET /api/member/tree-data - Trả về dữ liệu đầy đủ cho UI cây gia phả
        [HttpGet("tree-data")]
        [Authorize(Policy = "tree_view.view")]
        public async Task<IActionResult> GetTreeData()
        {
            var familyTreeId = _familyTreeService.FamilyTreeId;

            var members = await _context.Members
                .Where(m => m.FamilyTreeId == familyTreeId)
                .Select(m => new
                {
                    id = m.Id.ToString(),
                    full_name = m.FullName,
                    taboo_name = m.TabooName,
                    courtesy_name = m.CourtesyName,
                    other_names = m.OtherNames,
                    gender = m.Gender == 0 ? "male" : m.Gender == 1 ? "female" : "other",
                    birth_year = m.BirthDateSolar.HasValue ? (int?)m.BirthDateSolar.Value.Year : null,
                    birth_month = m.BirthMonth,
                    birth_day = m.BirthDay,
                    death_year = m.DeathDateSolar.HasValue ? (int?)m.DeathDateSolar.Value.Year : null,
                    death_month = m.DeathMonth,
                    death_day = m.DeathDay,
                    birth_date_lunar = m.BirthDateLunar,
                    birth_lunar_year = m.BirthLunarYear,
                    birth_lunar_month = m.BirthLunarMonth,
                    birth_lunar_day = m.BirthLunarDay,
                    death_date_lunar = m.DeathDateLunar,
                    birth_order = m.BirthOrder,
                    is_in_law = m.IsInLaw,
                    m.IsAlive,
                    avatar_url = m.AvatarUrl,
                    generation = m.GenerationLevel,
                    note = m.Note,
                    phone_number = m.PhoneNumber,
                    occupation = m.Occupation,
                    current_residence = m.CurrentResidence
                })
                .ToListAsync();

            var parentChildRels = await _context.ParentChildRelationships
                .IgnoreQueryFilters()
                .Where(r => r.FamilyTreeId == familyTreeId)
                .Select(r => new
                {
                    person_a = r.ParentId.ToString(),
                    person_b = r.ChildId.ToString(),
                    type = r.RelationshipType == 1 ? "adopted_child" : "biological_child",
                    order = r.ChildOrder
                })
                .ToListAsync();

            var spouseRels = await _context.SpouseRelationships
                .IgnoreQueryFilters()
                .Where(r => r.FamilyTreeId == familyTreeId)
                .Select(r => new
                {
                    person_a = r.HusbandId.ToString(),
                    person_b = r.WifeId.ToString(),
                    type = "marriage",
                    order = r.MarriageOrder
                })
                .ToListAsync();

            var relationships = parentChildRels.Cast<object>().Concat(spouseRels).ToList();

            return Ok(new { members, relationships });
        }

        public class CreateMemberRequest
        {
            public string FullName { get; set; } = string.Empty;
            public string? TabooName { get; set; }
            public string? CourtesyName { get; set; }
            public string? OtherNames { get; set; }
            public int Gender { get; set; } = 0; // 0: male, 1: female, 2: other
            public int? BirthYear { get; set; }
            public int? BirthMonth { get; set; }
            public int? BirthDay { get; set; }
            public int? DeathYear { get; set; }
            public int? DeathMonth { get; set; }
            public int? DeathDay { get; set; }
            public string? BirthDateLunar { get; set; }
            public int? BirthLunarYear { get; set; }
            public int? BirthLunarMonth { get; set; }
            public int? BirthLunarDay { get; set; }
            public string? DeathDateLunar { get; set; }
            public int? DeathLunarYear { get; set; }
            public int? DeathLunarMonth { get; set; }
            public int? DeathLunarDay { get; set; }
            public bool IsAlive { get; set; } = true;
            public int? BirthOrder { get; set; }
            public bool IsInLaw { get; set; }
            public string? Biography { get; set; }
            public string? Note { get; set; }
            public string? PhoneNumber { get; set; }
            public string? Occupation { get; set; }
            public string? CurrentResidence { get; set; }
            public string? AvatarUrl { get; set; }
            public int GenerationLevel { get; set; } = 1;
            // Quan hệ cha-mẹ
            public Guid? FatherId { get; set; }
            public Guid? MotherId { get; set; }
            // Quan hệ vợ chồng  
            public Guid? SpouseId { get; set; }
        }

        [HttpPost]
        [Authorize(Policy = "member_list.create")]
        public async Task<IActionResult> CreateMember([FromBody] CreateMemberRequest request)
        {
            var familyTreeId = _familyTreeService.FamilyTreeId;

            var validationError = ValidateMemberRequest(request);
            if (validationError != null)
                return BadRequest(new { message = validationError });

            var relationshipError = await ValidateRelationships(request, familyTreeId);
            if (relationshipError != null)
                return BadRequest(new { message = relationshipError });

            var member = new Member
            {
                Id = Guid.NewGuid(),
                FamilyTreeId = familyTreeId,
                FullName = request.FullName,
                TabooName = request.TabooName,
                CourtesyName = request.CourtesyName,
                OtherNames = request.OtherNames,
                Gender = request.Gender,
                GenerationLevel = request.GenerationLevel,
                BirthOrder = request.BirthOrder,
                IsInLaw = request.IsInLaw,
                IsAlive = request.IsAlive,
                Biography = request.Biography,
                BirthDateSolar = CreateDate(request.BirthYear, request.BirthMonth, request.BirthDay),
                BirthMonth = request.BirthMonth,
                BirthDay = request.BirthDay,
                DeathDateSolar = CreateDate(request.DeathYear, request.DeathMonth, request.DeathDay),
                DeathMonth = request.DeathMonth,
                DeathDay = request.DeathDay,
                BirthDateLunar = request.BirthDateLunar,
                BirthLunarYear = request.BirthLunarYear,
                BirthLunarMonth = request.BirthLunarMonth,
                BirthLunarDay = request.BirthLunarDay,
                DeathDateLunar = request.DeathDateLunar,
                DeathLunarYear = request.DeathLunarYear,
                DeathLunarMonth = request.DeathLunarMonth,
                DeathLunarDay = request.DeathLunarDay,
                Note = request.Note,
                PhoneNumber = request.PhoneNumber,
                Occupation = request.Occupation,
                CurrentResidence = request.CurrentResidence,
                AvatarUrl = request.AvatarUrl,
            };

            await _context.Members.AddAsync(member);

            // Tạo mối quan hệ cha-mẹ nếu có
            if (request.FatherId.HasValue)
            {
                _context.ParentChildRelationships.Add(new ParentChildRelationship
                {
                    ParentId = request.FatherId.Value,
                    ChildId = member.Id,
                    FamilyTreeId = familyTreeId
                });
            }

            if (request.MotherId.HasValue)
            {
                _context.ParentChildRelationships.Add(new ParentChildRelationship
                {
                    ParentId = request.MotherId.Value,
                    ChildId = member.Id,
                    FamilyTreeId = familyTreeId
                });
            }

            // Tạo quan hệ vợ chồng nếu có
            if (request.SpouseId.HasValue)
            {
                var spouse = await _context.Members.FindAsync(request.SpouseId.Value);
                if (spouse != null)
                {
                    var (husbandId, wifeId) = request.Gender == 0
                        ? (member.Id, request.SpouseId.Value)
                        : (request.SpouseId.Value, member.Id);

                    _context.SpouseRelationships.Add(new SpouseRelationship
                    {
                        HusbandId = husbandId,
                        WifeId = wifeId,
                        FamilyTreeId = familyTreeId
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Thêm thành viên thành công.", id = member.Id });
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "member_list.edit")]
        public async Task<IActionResult> UpdateMember(Guid id, [FromBody] CreateMemberRequest request)
        {
            var member = await _context.Members.FindAsync(id);
            if (member == null) return NotFound();

            var validationError = ValidateMemberRequest(request);
            if (validationError != null)
                return BadRequest(new { message = validationError });

            member.FullName = request.FullName;
            member.TabooName = request.TabooName;
            member.CourtesyName = request.CourtesyName;
            member.OtherNames = request.OtherNames;
            member.Gender = request.Gender;
            member.GenerationLevel = request.GenerationLevel;
            member.BirthOrder = request.BirthOrder;
            member.IsInLaw = request.IsInLaw;
            member.IsAlive = request.IsAlive;
            member.Biography = request.Biography;
            member.BirthDateSolar = CreateDate(request.BirthYear, request.BirthMonth, request.BirthDay);
            member.BirthMonth = request.BirthMonth;
            member.BirthDay = request.BirthDay;
            member.DeathDateSolar = CreateDate(request.DeathYear, request.DeathMonth, request.DeathDay);
            member.DeathMonth = request.DeathMonth;
            member.DeathDay = request.DeathDay;
            member.BirthDateLunar = request.BirthDateLunar;
            member.BirthLunarYear = request.BirthLunarYear;
            member.BirthLunarMonth = request.BirthLunarMonth;
            member.BirthLunarDay = request.BirthLunarDay;
            member.DeathDateLunar = request.DeathDateLunar;
            member.DeathLunarYear = request.DeathLunarYear;
            member.DeathLunarMonth = request.DeathLunarMonth;
            member.DeathLunarDay = request.DeathLunarDay;
            member.Note = request.Note;
            member.PhoneNumber = request.PhoneNumber;
            member.Occupation = request.Occupation;
            member.CurrentResidence = request.CurrentResidence;
            member.AvatarUrl = request.AvatarUrl;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thành viên thành công." });
        }

        private static string? ValidateMemberRequest(CreateMemberRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
                return "Tên thành viên không được để trống.";

            if (request.Gender < 0 || request.Gender > 2)
                return "Giới tính không hợp lệ.";

            if (request.GenerationLevel < 1 || request.GenerationLevel > 30)
                return "Đời thứ phải nằm trong khoảng từ 1 đến 30.";

            if (request.BirthYear is < 1400 or > 2100)
                return "Năm sinh phải nằm trong khoảng từ 1400 đến 2100.";

            if (request.BirthMonth is < 1 or > 12 || request.BirthDay is < 1 or > 31)
                return "Ngày sinh dương lịch không hợp lệ.";

            if (!IsValidDate(request.BirthYear, request.BirthMonth, request.BirthDay))
                return "Ngày sinh dương lịch không tồn tại.";

            if (request.BirthLunarMonth is < 1 or > 12 || request.BirthLunarDay is < 1 or > 30)
                return "Ngày sinh âm lịch không hợp lệ.";

            if (request.DeathYear is < 1400 or > 2100)
                return "Năm mất phải nằm trong khoảng từ 1400 đến 2100.";

            if (request.DeathMonth is < 1 or > 12 || request.DeathDay is < 1 or > 31)
                return "Ngày mất dương lịch không hợp lệ.";

            if (!IsValidDate(request.DeathYear, request.DeathMonth, request.DeathDay))
                return "Ngày mất dương lịch không tồn tại.";

            if (request.DeathLunarMonth is < 1 or > 12 || request.DeathLunarDay is < 1 or > 30)
                return "Ngày mất âm lịch không hợp lệ.";

            if (request.IsAlive && request.DeathYear.HasValue)
                return "Thành viên còn sống không được có năm mất.";

            if (request.BirthYear.HasValue && request.DeathYear.HasValue && request.DeathYear < request.BirthYear)
                return "Năm mất không thể trước năm sinh.";

            return null;
        }

        private static DateTime? CreateDate(int? year, int? month, int? day)
        {
            if (!year.HasValue) return null;
            return new DateTime(year.Value, month ?? 1, day ?? 1);
        }

        private static bool IsValidDate(int? year, int? month, int? day)
        {
            if (!year.HasValue || !month.HasValue || !day.HasValue) return true;
            return DateTime.TryParse($"{year.Value:D4}-{month.Value:D2}-{day.Value:D2}", out _);
        }

        private async Task<string?> ValidateRelationships(CreateMemberRequest request, Guid familyTreeId)
        {
            var relationshipIds = new[] { request.FatherId, request.MotherId, request.SpouseId }
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            if (relationshipIds.Count == 0)
                return null;

            var existingIds = await _context.Members
                .Where(member => member.FamilyTreeId == familyTreeId && relationshipIds.Contains(member.Id))
                .Select(member => member.Id)
                .ToListAsync();

            if (existingIds.Count != relationshipIds.Count)
                return "Một hoặc nhiều thành viên trong quan hệ không thuộc cây gia phả hiện tại.";

            if (request.FatherId.HasValue && request.MotherId.HasValue && request.FatherId == request.MotherId)
                return "Cha và mẹ không thể là cùng một thành viên.";

            if (request.SpouseId.HasValue && request.SpouseId == request.FatherId)
                return "Vợ/chồng không thể đồng thời là cha của thành viên.";

            if (request.SpouseId.HasValue && request.SpouseId == request.MotherId)
                return "Vợ/chồng không thể đồng thời là mẹ của thành viên.";

            return null;
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "member_list.delete")]
        public async Task<IActionResult> DeleteMember(Guid id)
        {
            var member = await _context.Members.FindAsync(id);
            if (member == null) return NotFound();

            _context.Members.Remove(member);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa thành viên." });
        }

        [HttpPost("{id}/avatar")]
        [Authorize(Policy = "member_list.edit")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadAvatar(Guid id, IFormFile file, IWebHostEnvironment environment)
        {
            var familyTreeId = _familyTreeService.FamilyTreeId;
            var member = await _context.Members.FirstOrDefaultAsync(item => item.Id == id && item.FamilyTreeId == familyTreeId);
            if (member == null) return NotFound(new { message = "Không tìm thấy thành viên." });
            if (file == null || file.Length == 0) return BadRequest(new { message = "Vui lòng chọn ảnh." });

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
                return BadRequest(new { message = "Chỉ hỗ trợ ảnh JPG, PNG hoặc WEBP." });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var relativeFolder = Path.Combine("uploads", "avatars", familyTreeId.ToString());
            var absoluteFolder = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), relativeFolder);
            Directory.CreateDirectory(absoluteFolder);
            var fileName = $"{id}{extension}";
            var absolutePath = Path.Combine(absoluteFolder, fileName);
            await using (var stream = System.IO.File.Create(absolutePath))
                await file.CopyToAsync(stream);

            member.AvatarUrl = $"/{relativeFolder.Replace('\\', '/')}/{fileName}";
            await _context.SaveChangesAsync();
            return Ok(new { avatarUrl = member.AvatarUrl });
        }

        // GET /api/member/kinship?fromId=...&toId=... - Tính danh xưng
        [HttpGet("kinship")]
        [Authorize(Policy = "member_list.view")]
        public async Task<IActionResult> GetKinship([FromQuery] Guid fromId, [FromQuery] Guid toId)
        {
            var familyTreeId = _familyTreeService.FamilyTreeId;

            if (fromId == toId)
                return Ok(new { aCallsB = "Chính mình", bCallsA = "Chính mình", description = "Cùng một người" });

            // Lấy dữ liệu thô
            var members = await _context.Members
                .Where(m => m.FamilyTreeId == familyTreeId)
                .ToListAsync();

            var parentChildRels = await _context.ParentChildRelationships
                .IgnoreQueryFilters()
                .Where(r => r.FamilyTreeId == familyTreeId)
                .ToListAsync();

            var spouseRels = await _context.SpouseRelationships
                .IgnoreQueryFilters()
                .Where(r => r.FamilyTreeId == familyTreeId)
                .ToListAsync();

            // Tính danh xưng bằng thuật toán BFS tìm LCA
            var result = KinshipCalculator.Calculate(fromId, toId, members, parentChildRels, spouseRels);
            return Ok(result);
        }
    }
}
