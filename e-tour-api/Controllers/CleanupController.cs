using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using e_tour_api.Services;
using e_tour_api.Models;
using Microsoft.Extensions.Logging;

namespace e_tour_api.Controllers
{
    /// <summary>
    /// Controller for cleanup operations such as removing orphaned files.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CleanupController : ControllerBase
    {
        private readonly CleanupService _cleanupService;
        private readonly ILogger<CleanupController> _logger;

        public CleanupController(CleanupService cleanupService, ILogger<CleanupController> logger)
        {
            _cleanupService = cleanupService;
            _logger = logger;
        }

        /// <summary>
        /// Cleans up orphaned files optionally filtered by file name.
        /// </summary>
        /// <param name="fileNameFilter">Optional filter for file names</param>
        /// <returns>Count of deleted orphaned files</returns>
        [HttpPost("orphaned-files")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ServiceResult<CleanupResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CleanupOrphanedFiles([FromQuery] string? fileNameFilter = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(fileNameFilter))
                {
                    _logger.LogInformation("Starting cleanup with filter: {Filter}", fileNameFilter);
                }
                else
                {
                    _logger.LogInformation("Starting cleanup of all orphaned files");
                }

                var deletedCount = await _cleanupService.CleanupOrphanedFilesAsync(fileNameFilter);
                _logger.LogInformation("Cleanup completed. Deleted {Count} orphaned files.", deletedCount);

                var response = new CleanupResponse
                {
                    DeletedFiles = deletedCount,
                    Message = "Cleanup completed successfully"
                };

                return Ok(ServiceResult<CleanupResponse>.Ok(response, "Cleanup completed successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid cleanup filter: {Filter}", fileNameFilter);
                return BadRequest(ServiceResult<object>.Error(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup operation");
                return StatusCode(500, ServiceResult<object>.Error("An error occurred during cleanup operation", 500));
            }
        }
    }

    /// <summary>
    /// Cleanup response model
    /// </summary>
    public class CleanupResponse
    {
        public int DeletedFiles { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
