using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using e_tour_api.Controllers;
using e_tour_api.Data;
using e_tour_api.Services;
using e_tour_api.Configuration;
using e_tour_api.Models;

namespace e_tour_api.Tests
{
    public class AuthControllerTests : IDisposable, TestLoggerBase
    {
        private readonly IUserRepository _repo;
        private readonly AppSettings _settings;
        private readonly AppDbContext _context;
        private readonly ITestOutputHelper _output;
        private readonly ILogger<AuthController> _logger;

        public AuthControllerTests(ITestOutputHelper output)
        {
            _output = output;
            _output.WriteLine("\n=== Starting AuthControllerTests Setup ===");
            
            // Use a unique database name for each test run
            var dbName = $"AuthControllerTestsDb_{Guid.NewGuid()}";
            _output.WriteLine($"Using database: {dbName}");
            
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            _context = new AppDbContext(options);
            _repo = new UserRepository(_context);
            _output.WriteLine("Initialized database and repository");

            _settings = new AppSettings
            {
                Jwt = new JwtSettings
                {
                    Key = "test-key-that-is-long-enough-for-testing-purposes-only",
                    Issuer = "https://test.com",
                    Audience = "https://test.com",
                    ExpiryMinutes = 15
                }
            };
            _output.WriteLine("Configured JWT settings");

            // Initialize test logger
            _logger = new TestLogger<AuthController>(LogMessages);
            _output.WriteLine("Initialized test logger");
            
            _output.WriteLine("=== AuthControllerTests Setup Complete ===\n");
        }

        [Fact]
        public async Task Register_ValidUser_ReturnsOk()
        {
            _output.WriteLine("\n=== Starting Register_ValidUser_ReturnsOk ===");
            
            // Arrange
            var controller = new AuthController(_repo, _settings, _logger);
            var model = new RegisterModel
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "Test123!",
                Role = "User"
            };
            _output.WriteLine("Created test user model");

            // Act
            _output.WriteLine("Calling Register...");
            var result = await controller.Register(model);
            _output.WriteLine("Register call completed");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServiceResult<object>>(okResult.Value);
            Assert.True(response.Success);
            _output.WriteLine("✓ Assertions passed");

            // Verify user was created in database
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            Assert.NotNull(user);
            Assert.Equal(model.Email, user.Email);
            Assert.Equal(model.Username, user.Username);
            Assert.Equal(model.Role, user.Role);
            _output.WriteLine("✓ User verified in database");

            // Verify logging
            AssertLogMessage(LogLevel.Information, "User registered successfully");
        }

        [Fact]
        public async Task Register_UserDoesNotExist_ShouldReturnOk()
        {
            // Arrange
            var controller = new AuthController(_repo, _settings, _logger);

            var registerModel = new RegisterModel
            {
                Username = "newuser123", // Changed to a unique username
                Password = "TestPassword123!",
                Email = "newuser123@example.com", // Changed to match username
                Role = "User"
            };

            // Act
            var result = await controller.Register(registerModel);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("User registered successfully", okResult.Value);
        }

        [Fact]
        public async Task Register_UserAlreadyExists_ShouldReturnBadRequest()
        {
            // Arrange
            var controller = new AuthController(_repo, _settings, _logger);

            // First registration
            var registerModel = new RegisterModel
            {
                Username = "duplicateuser",
                Password = "TestPassword123!",
                Email = "duplicateuser@example.com",
                Role = "User"
            };
            await controller.Register(registerModel);

            // Act - Try to register the same user again
            var result = await controller.Register(registerModel);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ServiceResult<object>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("already exists", response.Message);

            // Verify logging
            AssertLogMessage(LogLevel.Warning, "User registration failed");
        }

        [Fact]
        public async Task Register_WithInvalidEmail_ShouldReturnBadRequest()
        {
            // Arrange
            var controller = new AuthController(_repo, _settings, _logger);

            var registerModel = new RegisterModel
            {
                Username = "invalidemailuser",
                Password = "TestPassword123!",
                Email = "invalid-email",
                Role = "User"
            };

            // Act
            var result = await controller.Register(registerModel);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ServiceResult<object>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Invalid email format", response.Message);

            // Verify logging
            AssertLogMessage(LogLevel.Warning, "Invalid email format");
        }

        [Fact]
        public async Task Register_WithWeakPassword_ShouldReturnBadRequest()
        {
            // Arrange
            var controller = new AuthController(_repo, _settings, _logger);

            var registerModel = new RegisterModel
            {
                Username = "weakpassuser",
                Password = "weak",
                Email = "weakpassuser@example.com",
                Role = "User"
            };

            // Act
            var result = await controller.Register(registerModel);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ServiceResult<object>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Password must be at least 8 characters", response.Message);

            // Verify logging
            AssertLogMessage(LogLevel.Warning, "Weak password");
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnToken()
        {
            _output.WriteLine("\n=== Starting Login_WithValidCredentials_ShouldReturnToken ===");
            
            // Arrange
            var controller = new AuthController(_repo, _settings, _logger);
            _output.WriteLine("Created AuthController instance");

            // Create test user
            _output.WriteLine("Attempting to register test user...");
            var registerResult = await controller.Register(new RegisterModel
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "Test123!",
                Role = "User"
            });
            _output.WriteLine("User registration successful");

            // Act
            _output.WriteLine("Attempting login...");
            var result = await controller.Login(new LoginModel
            {
                Username = "testuser",
                Password = "Test123!"
            });
            _output.WriteLine("Login attempt completed");

            // Assert
            _output.WriteLine("Starting assertions...");
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServiceResult<LoginResponse>>(okResult.Value);
            Assert.True(response.Success);
            Assert.NotNull(response.Data.Token);
            Assert.Equal("testuser", response.Data.Username);
            Assert.Equal("User", response.Data.Role);
            _output.WriteLine("✓ All assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Information, "User logged in successfully");

            _output.WriteLine("=== Test Login_WithValidCredentials_ShouldReturnToken Completed ===\n");
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
        {
            // Arrange
            var controller = new AuthController(_repo, _settings, _logger);

            // Register a user first
            var registerModel = new RegisterModel
            {
                Username = "invalidcreduser",
                Password = "TestPassword123!",
                Email = "invalidcreduser@example.com",
                Role = "User"
            };
            await controller.Register(registerModel);

            // Act
            var result = await controller.Login(new LoginModel
            {
                Username = "invalidcreduser",
                Password = "WrongPassword123!"
            });

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = Assert.IsType<ServiceResult<object>>(unauthorizedResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Invalid credentials", response.Message);

            // Verify logging
            AssertLogMessage(LogLevel.Warning, "Invalid login attempt");
        }

        [Fact]
        public async Task Login_WithNonExistentUser_ShouldReturnUnauthorized()
        {
            // Arrange
            var controller = new AuthController(_repo, _settings, _logger);

            // Act
            var result = await controller.Login(new LoginModel
            {
                Username = "nonexistentuser",
                Password = "TestPassword123!"
            });

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = Assert.IsType<ServiceResult<object>>(unauthorizedResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Invalid credentials", response.Message);

            // Verify logging
            AssertLogMessage(LogLevel.Warning, "Login attempt for non-existent user");
        }

        [Fact]
        public async Task Register_WithInvalidRole_ShouldReturnBadRequest()
        {
            // Arrange
            var controller = new AuthController(_repo, _settings, _logger);

            var registerModel = new RegisterModel
            {
                Username = "invalidroleuser",
                Password = "TestPassword123!",
                Email = "invalidroleuser@example.com",
                Role = "InvalidRole"
            };

            // Act
            var result = await controller.Register(registerModel);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ServiceResult<object>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Invalid role", response.Message);

            // Verify logging
            AssertLogMessage(LogLevel.Warning, "Invalid role specified");
        }

        public void Dispose()
        {
            _output.WriteLine("\n=== Starting AuthControllerTests Cleanup ===");
            
            // Clean up database
            _context.Database.EnsureDeleted();
            _output.WriteLine("Deleted test database");

            _context.Dispose();
            _output.WriteLine("Disposed database context");
            
            _output.WriteLine("=== AuthControllerTests Cleanup Complete ===\n");
        }
    }
}
