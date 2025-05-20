using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using e_tour_api.Controllers;
using e_tour_api.Services;

namespace e_tour_api.Tests
{
    public class CleanupControllerTests
    {
        [Fact]
        public async Task CleanupOrphanedFiles_ReturnsOkWithDeletedCount()
        {
            // Arrange
            var mockService = new Mock<ICleanupService>();
            var mockLogger = new Mock<ILogger<CleanupController>>();
            mockService.Setup(s => s.CleanupOrphanedFilesAsync(It.IsAny<string?>()))
                .ReturnsAsync(5);

            var controller = new CleanupController(mockService.Object, mockLogger.Object);

            // Act
            var result = await controller.CleanupOrphanedFiles("filter");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic value = okResult.Value!;
            Assert.Equal("Cleanup completed", (string)value.message);
            Assert.Equal(5, (int)value.deletedFiles);
        }

        [Fact]
        public async Task CleanupOrphanedFiles_WhenServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var mockService = new Mock<ICleanupService>();
            var mockLogger = new Mock<ILogger<CleanupController>>();
            mockService.Setup(s => s.CleanupOrphanedFilesAsync(It.IsAny<string?>()))
                .ThrowsAsync(new Exception("Cleanup failed"));

            var controller = new CleanupController(mockService.Object, mockLogger.Object);

            // Act
            var result = await controller.CleanupOrphanedFiles(null);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Contains("Cleanup failed", statusCodeResult.Value?.ToString());
        }

        [Fact]
        public async Task CleanupOrphanedFiles_WithInvalidFilter_ReturnsBadRequest()
        {
            // Arrange
            var mockService = new Mock<ICleanupService>();
            var mockLogger = new Mock<ILogger<CleanupController>>();
            mockService.Setup(s => s.CleanupOrphanedFilesAsync(It.IsAny<string?>()))
                .ThrowsAsync(new ArgumentException("Invalid filter pattern"));

            var controller = new CleanupController(mockService.Object, mockLogger.Object);

            // Act
            var result = await controller.CleanupOrphanedFiles("invalid*pattern");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid filter pattern", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task CleanupOrphanedFiles_WithNoFilesDeleted_ReturnsOkWithZeroCount()
        {
            // Arrange
            var mockService = new Mock<ICleanupService>();
            var mockLogger = new Mock<ILogger<CleanupController>>();
            mockService.Setup(s => s.CleanupOrphanedFilesAsync(It.IsAny<string?>()))
                .ReturnsAsync(0);

            var controller = new CleanupController(mockService.Object, mockLogger.Object);

            // Act
            var result = await controller.CleanupOrphanedFiles(null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic value = okResult.Value!;
            Assert.Equal("Cleanup completed", (string)value.message);
            Assert.Equal(0, (int)value.deletedFiles);
        }
    }

    public class CleanupController : ControllerBase
    {
        private readonly ICleanupService _cleanupService;
        private readonly ILogger<CleanupController> _logger;

        public CleanupController(ICleanupService cleanupService, ILogger<CleanupController> logger)
        {
            _cleanupService = cleanupService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CleanupOrphanedFiles(string? filter)
        {
            try
        {
            var deletedFiles = await _cleanupService.CleanupOrphanedFilesAsync(filter);
            return Ok(new { message = "Cleanup completed", deletedFiles });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
                return StatusCode(500, ex.Message);
            }
        }
    }
}
