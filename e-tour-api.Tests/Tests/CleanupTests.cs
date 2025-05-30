using System;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using e_tour_api.Controllers;
using e_tour_api.Services;
using e_tour_api.Models.DTOs;

namespace e_tour_api.Tests.Tests
{
    public class CleanupTests : IDisposable
    {
        private readonly Mock<ICleanupService> _mockCleanupService;
        private readonly Mock<ILogger<CleanupController>> _mockLogger;
        private readonly CleanupController _controller;

        public CleanupTests()
        {
            _mockCleanupService = new Mock<ICleanupService>();
            _mockLogger = new Mock<ILogger<CleanupController>>();
            _controller = new CleanupController(_mockCleanupService.Object, _mockLogger.Object);
        }

        [Fact]
               public async Task CleanupOrphanedFiles_Success_ReturnsOk()
        {
            // Arrange
            var cleanupResponse = new CleanupResponse
            {
                DeletedFilesCount = 5,
                Message = "Successfully cleaned up 5 orphaned files"
            };

            _mockCleanupService
                .Setup(x => x.CleanupOrphanedFilesAsync())
                .ReturnsAsync(ServiceResult<CleanupResponse>.Success(cleanupResponse));

            // Act
            var result = await _controller.CleanupOrphanedFiles();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServiceResult<CleanupResponse>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(5, response.Data.DeletedFilesCount);
            Assert.Equal("Successfully cleaned up 5 orphaned files", response.Data.Message);
        }

        [Fact]
        public async Task CleanupOrphanedFiles_NoFilesToClean_ReturnsOk()
        {
            // Arrange
            var cleanupResponse = new CleanupResponse
            {
                DeletedFilesCount = 0,
                Message = "No orphaned files found"
            };

            _mockCleanupService
                .Setup(x => x.CleanupOrphanedFilesAsync())
                .ReturnsAsync(ServiceResult<CleanupResponse>.Success(cleanupResponse));

            // Act
            var result = await _controller.CleanupOrphanedFiles();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServiceResult<CleanupResponse>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(0, response.Data.DeletedFilesCount);
            Assert.Equal("No orphaned files found", response.Data.Message);
        }

        [Fact]
        public async Task CleanupOrphanedFiles_ServiceError_ReturnsInternalServerError()
        {
            // Arrange
            _mockCleanupService
                .Setup(x => x.CleanupOrphanedFilesAsync())
                .ThrowsAsync(new IOException("Failed to access file system"));

            // Act
            var result = await _controller.CleanupOrphanedFiles();

            // Assert
            var serverErrorResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverErrorResult.StatusCode);
            var response = Assert.IsType<ServiceResult<object>>(serverErrorResult.Value);
            Assert.False(response.Success);
            Assert.Equal(500, response.StatusCode);
        }

        [Fact]
        public async Task CleanupOrphanedFiles_PartialSuccess_ReturnsOk()
        {
            // Arrange
            var cleanupResponse = new CleanupResponse
            {
                DeletedFilesCount = 3,
                Message = "Partially cleaned up files. Some files could not be deleted."
            };

            _mockCleanupService
                .Setup(x => x.CleanupOrphanedFilesAsync())
                .ReturnsAsync(ServiceResult<CleanupResponse>.Success(cleanupResponse));

            // Act
            var result = await _controller.CleanupOrphanedFiles();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServiceResult<CleanupResponse>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(3, response.Data.DeletedFilesCount);
            Assert.Contains("Partially cleaned up files", response.Data.Message);
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
} 