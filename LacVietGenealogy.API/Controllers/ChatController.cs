using LacVietGenealogy.API.DTOs;
using LacVietGenealogy.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LacVietGenealogy.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Policy = "chatbot.view")]
    public async Task<IActionResult> Post([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { message = "Tin nhắn không được để trống." });

        try
        {
            var result = await _chatService.ProcessAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ hoặc thiếu thông tin dòng họ." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý chatbot request");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Xin lỗi, hiện tại tôi không thể xử lý câu hỏi này. Vui lòng thử lại sau."
            });
        }
    }
}
