using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using e_tour_api.Services;

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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CleanupOrphanedFiles([FromQuery] string? fileNameFilter = null)
        {
            var deletedCount = await _cleanupService.CleanupOrphanedFilesAsync(fileNameFilter);
            _logger.LogInformation("Cleanup completed. Deleted {Count} orphaned files.", deletedCount);
            return Ok(new { message = "Cleanup completed", deletedFiles = deletedCount });
        }
    }
}
