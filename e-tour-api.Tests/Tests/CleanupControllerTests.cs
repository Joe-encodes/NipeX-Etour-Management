using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Xunit.Abstractions;
using e_tour_api.Controllers;
using e_tour_api.Services;

namespace e_tour_api.Tests
{
    public class CleanupControllerTests : TestLoggerBase
    {
        private readonly ITestOutputHelper _output;
        private readonly ILogger<CleanupController> _logger;

        public CleanupControllerTests(ITestOutputHelper output)
        {
            _output = output;
            _output.WriteLine("\n=== Starting CleanupControllerTests Setup ===");
            
            // Initialize test logger
            _logger = new TestLogger<CleanupController>(LogMessages);
            _output.WriteLine("Initialized test logger");
            
            _output.WriteLine("=== CleanupControllerTests Setup Complete ===\n");
        }

        [Fact]
        public async Task CleanupOrphanedFiles_ReturnsOkWithDeletedCount()
        {
            _output.WriteLine("\n=== Starting CleanupOrphanedFiles_ReturnsOkWithDeletedCount ===");
            
            // Arrange
            var mockService = new Mock<ICleanupService>();
            mockService.Setup(s => s.CleanupOrphanedFilesAsync(It.IsAny<string?>()))
                .ReturnsAsync(5);

            var controller = new CleanupController(mockService.Object, _logger);
            _output.WriteLine("Created controller with mock service");

            // Act
            _output.WriteLine("Calling CleanupOrphanedFiles...");
            var result = await controller.CleanupOrphanedFiles("filter");
            _output.WriteLine("Cleanup call completed");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServiceResult<CleanupResponse>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(5, response.Data.DeletedFiles);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Information, "Cleanup completed successfully");
            
            _output.WriteLine("=== Test CleanupOrphanedFiles_ReturnsOkWithDeletedCount Completed ===\n");
        }

        [Fact]
        public async Task CleanupOrphanedFiles_WhenServiceThrowsException_ReturnsInternalServerError()
        {
            _output.WriteLine("\n=== Starting CleanupOrphanedFiles_WhenServiceThrowsException_ReturnsInternalServerError ===");
            
            // Arrange
            var mockService = new Mock<ICleanupService>();
            mockService.Setup(s => s.CleanupOrphanedFilesAsync(It.IsAny<string?>()))
                .ThrowsAsync(new Exception("Cleanup failed"));

            var controller = new CleanupController(mockService.Object, _logger);
            _output.WriteLine("Created controller with mock service that throws exception");

            // Act
            _output.WriteLine("Calling CleanupOrphanedFiles...");
            var result = await controller.CleanupOrphanedFiles(null);
            _output.WriteLine("Cleanup call completed");

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var response = Assert.IsType<ServiceResult<object>>(statusCodeResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Cleanup failed", response.Message);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Error, "Error during cleanup");
            
            _output.WriteLine("=== Test CleanupOrphanedFiles_WhenServiceThrowsException_ReturnsInternalServerError Completed ===\n");
        }

        [Fact]
        public async Task CleanupOrphanedFiles_WithInvalidFilter_ReturnsBadRequest()
        {
            _output.WriteLine("\n=== Starting CleanupOrphanedFiles_WithInvalidFilter_ReturnsBadRequest ===");
            
            // Arrange
            var mockService = new Mock<ICleanupService>();
            mockService.Setup(s => s.CleanupOrphanedFilesAsync(It.IsAny<string?>()))
                .ThrowsAsync(new ArgumentException("Invalid filter pattern"));

            var controller = new CleanupController(mockService.Object, _logger);
            _output.WriteLine("Created controller with mock service that throws ArgumentException");

            // Act
            _output.WriteLine("Calling CleanupOrphanedFiles with invalid filter...");
            var result = await controller.CleanupOrphanedFiles("invalid*pattern");
            _output.WriteLine("Cleanup call completed");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ServiceResult<object>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Invalid filter pattern", response.Message);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Warning, "Invalid filter pattern");
            
            _output.WriteLine("=== Test CleanupOrphanedFiles_WithInvalidFilter_ReturnsBadRequest Completed ===\n");
        }

        [Fact]
        public async Task CleanupOrphanedFiles_WithNoFilesDeleted_ReturnsOkWithZeroCount()
        {
            _output.WriteLine("\n=== Starting CleanupOrphanedFiles_WithNoFilesDeleted_ReturnsOkWithZeroCount ===");
            
            // Arrange
            var mockService = new Mock<ICleanupService>();
            mockService.Setup(s => s.CleanupOrphanedFilesAsync(It.IsAny<string?>()))
                .ReturnsAsync(0);

            var controller = new CleanupController(mockService.Object, _logger);
            _output.WriteLine("Created controller with mock service that returns zero deleted files");

            // Act
            _output.WriteLine("Calling CleanupOrphanedFiles...");
            var result = await controller.CleanupOrphanedFiles(null);
            _output.WriteLine("Cleanup call completed");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServiceResult<CleanupResponse>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(0, response.Data.DeletedFiles);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Information, "Cleanup completed successfully");
            
            _output.WriteLine("=== Test CleanupOrphanedFiles_WithNoFilesDeleted_ReturnsOkWithZeroCount Completed ===\n");
        }
    }

    public class CleanupResponse
    {
        public int DeletedFiles { get; set; }
    }
}
