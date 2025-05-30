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
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet("profile")]
        [ProducesResponseType(typeof(ServiceResult<UserProfileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserProfile()
        {
            try
            {
                var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
                var result = await _userService.GetUserProfileAsync(userId);
                
                if (!result.Success)
                    return StatusCode(result.StatusCode, new { error = result.Message });

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile");
                return StatusCode(500, ServiceResult<object>.Error("An error occurred while retrieving the profile", 500));
            }
        }

        [HttpPut("profile")]
        [ProducesResponseType(typeof(ServiceResult<UserProfileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UserProfileDto profile)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
                var result = await _userService.UpdateUserProfileAsync(userId, profile.PhoneNumber, profile.Bio);
                
                if (!result.Success)
                    return StatusCode(result.StatusCode, new { error = result.Message });

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return StatusCode(500, ServiceResult<object>.Error("An error occurred while updating the profile", 500));
            }
        }

        [HttpPost("profile/picture")]
        [ProducesResponseType(typeof(ServiceResult<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded");

                var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
                var result = await _userService.UploadProfilePictureAsync(userId, file);
                
                if (!result.Success)
                    return StatusCode(result.StatusCode, new { error = result.Message });

                return Ok(new { url = result.Data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading profile picture");
                return StatusCode(500, ServiceResult<object>.Error("An error occurred while uploading the profile picture", 500));
            }
        }

        [HttpDelete("profile/picture")]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteProfilePicture()
        {
            try
            {
                var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
                var result = await _userService.DeleteProfilePictureAsync(userId);
                
                if (!result.Success)
                    return StatusCode(result.StatusCode, new { error = result.Message });

                return Ok(new { message = "Profile picture deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting profile picture");
                return StatusCode(500, ServiceResult<object>.Error("An error occurred while deleting the profile picture", 500));
            }
        }

        private bool IsValidImageFile(IFormFile file)
        {
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
            return allowedTypes.Contains(file.ContentType.ToLower());
        }
    }

    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
    }
} 