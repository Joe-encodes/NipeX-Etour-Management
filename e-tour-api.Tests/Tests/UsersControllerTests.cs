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
    public class UsersControllerTests : TestLoggerBase, IDisposable
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly ILogger<UsersController> _logger;
        private readonly UsersController _controller;
        private readonly ClaimsPrincipal _user;
        private readonly ITestOutputHelper _output;

        public UsersControllerTests(ITestOutputHelper output)
        {
            _output = output;
            _output.WriteLine("\n=== Starting UsersControllerTests Setup ===");
            
            _mockUserService = new Mock<IUserService>();
            _logger = new TestLogger<UsersController>(LogMessages);
            _controller = new UsersController(_mockUserService.Object, _logger);
            _output.WriteLine("Initialized controller with mock service and test logger");

            // Setup test user
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
            
            _output.WriteLine("=== UsersControllerTests Setup Complete ===\n");
        }

        [Fact]
        public async Task GetProfile_ReturnsOk()
        {
            _output.WriteLine("\n=== Starting GetProfile_ReturnsOk ===");
            
            // Arrange
            var expectedProfile = new UserProfileDto
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                Role = "User"
            };

            _mockUserService
                .Setup(x => x.GetUserProfileAsync(1))
                .ReturnsAsync(ServiceResult<UserProfileDto>.Ok(expectedProfile));
            _output.WriteLine("Configured mock service response");

            // Act
            _output.WriteLine("Calling GetProfile...");
            var result = await _controller.GetProfile();
            _output.WriteLine("GetProfile call completed");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServiceResult<UserProfileDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(expectedProfile.Username, response.Data.Username);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Information, "User profile retrieved");
            
            _output.WriteLine("=== Test GetProfile_ReturnsOk Completed ===\n");
        }

        [Fact]
        public async Task UpdateProfile_ValidRequest_ReturnsOk()
        {
            _output.WriteLine("\n=== Starting UpdateProfile_ValidRequest_ReturnsOk ===");
            
            // Arrange
            var request = new UpdateProfileRequest
            {
                Email = "updated@example.com",
                CurrentPassword = "oldpass",
                NewPassword = "newpass"
            };

            var expectedProfile = new UserProfileDto
            {
                Id = 1,
                Username = "testuser",
                Email = "updated@example.com",
                Role = "User"
            };

            _mockUserService
                .Setup(x => x.UpdateProfileAsync(1, request))
                .ReturnsAsync(ServiceResult<UserProfileDto>.Ok(expectedProfile));
            _output.WriteLine("Configured mock service response");

            // Act
            _output.WriteLine("Calling UpdateProfile...");
            var result = await _controller.UpdateProfile(request);
            _output.WriteLine("UpdateProfile call completed");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServiceResult<UserProfileDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(expectedProfile.Email, response.Data.Email);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Information, "User profile updated");
            
            _output.WriteLine("=== Test UpdateProfile_ValidRequest_ReturnsOk Completed ===\n");
        }

        [Fact]
        public async Task UpdateProfile_InvalidRequest_ReturnsBadRequest()
        {
            _output.WriteLine("\n=== Starting UpdateProfile_InvalidRequest_ReturnsBadRequest ===");
            
            // Arrange
            var request = new UpdateProfileRequest
            {
                Email = "invalid-email",
                CurrentPassword = "oldpass",
                NewPassword = "newpass"
            };

            _mockUserService
                .Setup(x => x.UpdateProfileAsync(1, request))
                .ReturnsAsync(ServiceResult<UserProfileDto>.Error("Invalid email format", 400));
            _output.WriteLine("Configured mock service response");

            // Act
            _output.WriteLine("Calling UpdateProfile with invalid request...");
            var result = await _controller.UpdateProfile(request);
            _output.WriteLine("UpdateProfile call completed");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ServiceResult<UserProfileDto>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal(400, response.StatusCode);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Warning, "Invalid profile update request");
            
            _output.WriteLine("=== Test UpdateProfile_InvalidRequest_ReturnsBadRequest Completed ===\n");
        }

        [Fact]
        public async Task UploadProfilePicture_ValidFile_ReturnsOk()
        {
            _output.WriteLine("\n=== Starting UploadProfilePicture_ValidFile_ReturnsOk ===");
            
            // Arrange
            var fileContent = new byte[] { 1, 2, 3, 4 };
            var fileName = "test.jpg";
            var fileStream = new MemoryStream(fileContent);
            var formFile = new FormFile(fileStream, 0, fileContent.Length, "file", fileName);

            var expectedProfile = new UserProfileDto
            {
                Id = 1,
                Username = "testuser",
                ProfilePictureUrl = "/uploads/profile/test.jpg"
            };

            _mockUserService
                .Setup(x => x.UploadProfilePictureAsync(1, formFile))
                .ReturnsAsync(ServiceResult<UserProfileDto>.Ok(expectedProfile));
            _output.WriteLine("Configured mock service response");

            // Act
            _output.WriteLine("Calling UploadProfilePicture...");
            var result = await _controller.UploadProfilePicture(formFile);
            _output.WriteLine("UploadProfilePicture call completed");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServiceResult<UserProfileDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(expectedProfile.ProfilePictureUrl, response.Data.ProfilePictureUrl);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Information, "Profile picture uploaded");
            
            _output.WriteLine("=== Test UploadProfilePicture_ValidFile_ReturnsOk Completed ===\n");
        }

        [Fact]
        public async Task UploadProfilePicture_InvalidFile_ReturnsBadRequest()
        {
            _output.WriteLine("\n=== Starting UploadProfilePicture_InvalidFile_ReturnsBadRequest ===");
            
            // Arrange
            var fileContent = new byte[] { 1, 2, 3, 4 };
            var fileName = "test.txt"; // Invalid file type
            var fileStream = new MemoryStream(fileContent);
            var formFile = new FormFile(fileStream, 0, fileContent.Length, "file", fileName);

            _mockUserService
                .Setup(x => x.UploadProfilePictureAsync(1, formFile))
                .ReturnsAsync(ServiceResult<UserProfileDto>.Error("Invalid file type. Only images are allowed.", 400));
            _output.WriteLine("Configured mock service response");

            // Act
            _output.WriteLine("Calling UploadProfilePicture with invalid file...");
            var result = await _controller.UploadProfilePicture(formFile);
            _output.WriteLine("UploadProfilePicture call completed");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ServiceResult<UserProfileDto>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal(400, response.StatusCode);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Warning, "Invalid profile picture file type");
            
            _output.WriteLine("=== Test UploadProfilePicture_InvalidFile_ReturnsBadRequest Completed ===\n");
        }

        [Fact]
        public async Task DeleteProfilePicture_ReturnsOk()
        {
            _output.WriteLine("\n=== Starting DeleteProfilePicture_ReturnsOk ===");
            
            // Arrange
            var expectedProfile = new UserProfileDto
            {
                Id = 1,
                Username = "testuser",
                ProfilePictureUrl = null
            };

            _mockUserService
                .Setup(x => x.DeleteProfilePictureAsync(1))
                .ReturnsAsync(ServiceResult<UserProfileDto>.Ok(expectedProfile));
            _output.WriteLine("Configured mock service response");

            // Act
            _output.WriteLine("Calling DeleteProfilePicture...");
            var result = await _controller.DeleteProfilePicture();
            _output.WriteLine("DeleteProfilePicture call completed");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServiceResult<UserProfileDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Null(response.Data.ProfilePictureUrl);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Information, "Profile picture deleted");
            
            _output.WriteLine("=== Test DeleteProfilePicture_ReturnsOk Completed ===\n");
        }

        public void Dispose()
        {
            _output.WriteLine("\n=== Starting UsersControllerTests Cleanup ===");
            // Cleanup if needed
            _output.WriteLine("=== UsersControllerTests Cleanup Complete ===\n");
        }
    }
} 