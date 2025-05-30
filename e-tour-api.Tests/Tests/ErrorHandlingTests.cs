using System;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;
using e_tour_api.Controllers;
using e_tour_api.Services;
using e_tour_api.Models.DTOs;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace e_tour_api.Tests.Tests
{
    public class ErrorHandlingTests : TestLoggerBase, IDisposable
    {
        private readonly Mock<IDocumentService> _mockDocumentService;
        private readonly Mock<IUserService> _mockUserService;
        private readonly ILogger<DocumentsController> _logger;
        private readonly DocumentsController _controller;
        private readonly ClaimsPrincipal _user;
        private readonly ITestOutputHelper _output;

        public ErrorHandlingTests(ITestOutputHelper output)
        {
            _output = output;
            _output.WriteLine("\n=== Starting ErrorHandlingTests Setup ===");
            
            _mockDocumentService = new Mock<IDocumentService>();
            _mockUserService = new Mock<IUserService>();
            _logger = new TestLogger<DocumentsController>(LogMessages);
            _controller = new DocumentsController(_mockDocumentService.Object, _logger);
            _output.WriteLine("Initialized controller with mock services and test logger");

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "User")
            };
            _user = new ClaimsPrincipal(new ClaimsIdentity(claims));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = _user }
            };
            _output.WriteLine("Configured test user context");
            
            _output.WriteLine("=== ErrorHandlingTests Setup Complete ===\n");
        }

        [Fact]
        public async Task UploadDocument_FileTooLarge_ReturnsBadRequest()
        {
            _output.WriteLine("\n=== Starting UploadDocument_FileTooLarge_ReturnsBadRequest ===");
            
            // Arrange
            var largeFileContent = new byte[11 * 1024 * 1024]; // 11MB
            var fileName = "large.pdf";
            var fileStream = new MemoryStream(largeFileContent);
            var formFile = new FormFile(fileStream, 0, largeFileContent.Length, "file", fileName);
            _output.WriteLine("Created large test file");

            // Act
            _output.WriteLine("Calling UploadDocument with large file...");
            var result = await _controller.UploadDocument(formFile, "Large Document");
            _output.WriteLine("UploadDocument call completed");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ServiceResult<object>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal(400, response.StatusCode);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Warning, "File too large");
            
            _output.WriteLine("=== Test UploadDocument_FileTooLarge_ReturnsBadRequest Completed ===\n");
        }

        [Fact]
        public async Task UploadDocument_InvalidFileType_ReturnsBadRequest()
        {
            _output.WriteLine("\n=== Starting UploadDocument_InvalidFileType_ReturnsBadRequest ===");
            
            // Arrange
            var fileContent = new byte[] { 1, 2, 3, 4 };
            var fileName = "test.exe"; // Invalid file type
            var fileStream = new MemoryStream(fileContent);
            var formFile = new FormFile(fileStream, 0, fileContent.Length, "file", fileName);
            _output.WriteLine("Created invalid file type");

            // Act
            _output.WriteLine("Calling UploadDocument with invalid file type...");
            var result = await _controller.UploadDocument(formFile, "Invalid Document");
            _output.WriteLine("UploadDocument call completed");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ServiceResult<object>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal(400, response.StatusCode);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Warning, "Invalid file type");
            
            _output.WriteLine("=== Test UploadDocument_InvalidFileType_ReturnsBadRequest Completed ===\n");
        }

        [Fact]
        public async Task DownloadDocument_UnauthorizedAccess_ReturnsForbidden()
        {
            _output.WriteLine("\n=== Starting DownloadDocument_UnauthorizedAccess_ReturnsForbidden ===");
            
            // Arrange
            _mockDocumentService
                .Setup(x => x.VerifyDocumentAccess(1, 1))
                .ReturnsAsync(new DocumentAccessResult { HasAccess = false, ErrorCode = 403, ErrorMessage = "Access denied" });
            _output.WriteLine("Configured mock service response");

            // Act
            _output.WriteLine("Calling DownloadDocument...");
            var result = await _controller.DownloadDocument(1);
            _output.WriteLine("DownloadDocument call completed");

            // Assert
            var forbiddenResult = Assert.IsType<ForbiddenObjectResult>(result);
            var response = Assert.IsType<ServiceResult<object>>(forbiddenResult.Value);
            Assert.False(response.Success);
            Assert.Equal(403, response.StatusCode);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Warning, "Unauthorized document access attempt");
            
            _output.WriteLine("=== Test DownloadDocument_UnauthorizedAccess_ReturnsForbidden Completed ===\n");
        }

        [Fact]
        public async Task DownloadDocument_FileNotFound_ReturnsNotFound()
        {
            _output.WriteLine("\n=== Starting DownloadDocument_FileNotFound_ReturnsNotFound ===");
            
            // Arrange
            _mockDocumentService
                .Setup(x => x.VerifyDocumentAccess(1, 1))
                .ReturnsAsync(new DocumentAccessResult { HasAccess = true });

            _mockDocumentService
                .Setup(x => x.GetDocumentFile(1))
                .ReturnsAsync(ServiceResult<DocumentFile>.Error("File not found", 404));
            _output.WriteLine("Configured mock service responses");

            // Act
            _output.WriteLine("Calling DownloadDocument...");
            var result = await _controller.DownloadDocument(1);
            _output.WriteLine("DownloadDocument call completed");

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ServiceResult<object>>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.Equal(404, response.StatusCode);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Warning, "Document file not found");
            
            _output.WriteLine("=== Test DownloadDocument_FileNotFound_ReturnsNotFound Completed ===\n");
        }

        [Fact]
        public async Task UploadDocument_ServiceThrowsException_ReturnsInternalServerError()
        {
            _output.WriteLine("\n=== Starting UploadDocument_ServiceThrowsException_ReturnsInternalServerError ===");
            
            // Arrange
            var fileContent = new byte[] { 1, 2, 3, 4 };
            var fileName = "test.pdf";
            var fileStream = new MemoryStream(fileContent);
            var formFile = new FormFile(fileStream, 0, fileContent.Length, "file", fileName);

            _mockDocumentService
                .Setup(x => x.UploadDocumentAsync(1, "Test Document", formFile))
                .ThrowsAsync(new Exception("Database connection failed"));
            _output.WriteLine("Configured mock service to throw exception");

            // Act
            _output.WriteLine("Calling UploadDocument...");
            var result = await _controller.UploadDocument(formFile, "Test Document");
            _output.WriteLine("UploadDocument call completed");

            // Assert
            var serverErrorResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverErrorResult.StatusCode);
            var response = Assert.IsType<ServiceResult<object>>(serverErrorResult.Value);
            Assert.False(response.Success);
            Assert.Equal(500, response.StatusCode);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Error, "Error uploading document");
            
            _output.WriteLine("=== Test UploadDocument_ServiceThrowsException_ReturnsInternalServerError Completed ===\n");
        }

        [Fact]
        public async Task UploadDocument_InvalidToken_ReturnsUnauthorized()
        {
            _output.WriteLine("\n=== Starting UploadDocument_InvalidToken_ReturnsUnauthorized ===");
            
            // Arrange
            var fileContent = new byte[] { 1, 2, 3, 4 };
            var fileName = "test.pdf";
            var fileStream = new MemoryStream(fileContent);
            var formFile = new FormFile(fileStream, 0, fileContent.Length, "file", fileName);

            // Set invalid user context
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };
            _output.WriteLine("Configured invalid user context");

            // Act
            _output.WriteLine("Calling UploadDocument with invalid token...");
            var result = await _controller.UploadDocument(formFile, "Test Document");
            _output.WriteLine("UploadDocument call completed");

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = Assert.IsType<ServiceResult<object>>(unauthorizedResult.Value);
            Assert.False(response.Success);
            Assert.Equal(401, response.StatusCode);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Warning, "Unauthorized document upload attempt");
            
            _output.WriteLine("=== Test UploadDocument_InvalidToken_ReturnsUnauthorized Completed ===\n");
        }

        public void Dispose()
        {
            _output.WriteLine("\n=== Starting ErrorHandlingTests Cleanup ===");
            // Cleanup if needed
            _output.WriteLine("=== ErrorHandlingTests Cleanup Complete ===\n");
        }
    }
} 