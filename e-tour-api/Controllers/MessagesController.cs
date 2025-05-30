using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using e_tour_api.Services;
using e_tour_api.Models.DTOs;
using e_tour_api.Models;

namespace e_tour_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(IMessageService messageService, ILogger<MessagesController> logger)
        {
            _messageService = messageService;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ServiceResult<MessageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _messageService.SendMessageAsync(userId, request.ReceiverId, request.Content, request.Subject);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return StatusCode(500, ServiceResult<object>.Error("An error occurred while sending the message", 500));
            }
        }

        [HttpGet]
        [ProducesResponseType(typeof(ServiceResult<List<MessageDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMessages([FromQuery] bool includeRead = true)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _messageService.GetUserMessagesAsync(userId, includeRead);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving messages");
                return StatusCode(500, ServiceResult<object>.Error("An error occurred while retrieving messages", 500));
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ServiceResult<MessageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMessage(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _messageService.GetMessageAsync(id, userId);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving message {MessageId}", id);
                return StatusCode(500, ServiceResult<object>.Error("An error occurred while retrieving the message", 500));
            }
        }

        [HttpPost("{id}/read")]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _messageService.MarkMessageAsReadAsync(id, userId);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, result);
                }

                return Ok(ServiceResult<object>.Ok(new object(), "Message marked as read"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message {MessageId} as read", id);
                return StatusCode(500, ServiceResult<object>.Error("An error occurred while marking the message as read", 500));
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _messageService.DeleteMessageAsync(id, userId);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, result);
                }

                return Ok(ServiceResult<object>.Ok(new object(), "Message deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message {MessageId}", id);
                return StatusCode(500, ServiceResult<object>.Error("An error occurred while deleting the message", 500));
            }
        }
    }

    public class SendMessageRequest
    {
        public int ReceiverId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
} 