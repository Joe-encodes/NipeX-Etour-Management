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

namespace e_tour_api.Tests
{
    public class AuthControllerTests : IDisposable
    {
        private readonly IUserRepository _repo;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly ITestOutputHelper _output;

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

            var inMemorySettings = new Dictionary<string, string?> {
                {"Jwt:Key", "ThisIsASecretKeyForJwtToken"},
                {"Jwt:Issuer", "test-issuer"},
                {"Jwt:Audience", "test-audience"},
                {"Jwt:ExpiryMinutes", "60"}
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            _output.WriteLine("Configured JWT settings");
            
            _output.WriteLine("=== AuthControllerTests Setup Complete ===\n");
        }

        [Fact]
        public async Task Register_ValidUser_ReturnsOk()
        {
            _output.WriteLine("\n=== Starting Register_ValidUser_ReturnsOk ===");
            
            // Arrange
            var controller = new AuthController(_repo, _configuration);
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
            // Assert.Equal("User registered successfully", okResult.Value);
            _output.WriteLine("✓ Assertions passed");

            // Verify user was created in database
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            Assert.NotNull(user);
            Assert.Equal(model.Email, user.Email);
            Assert.Equal(model.Username, user.Username);
            Assert.Equal(model.Role, user.Role);
            _output.WriteLine("✓ User verified in database");
        }

        [Fact]
        public async Task Register_UserDoesNotExist_ShouldReturnOk()
        {
            // Arrange
            var controller = new AuthController(_repo, _configuration);

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
            var controller = new AuthController(_repo, _configuration);

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
            var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
            Assert.Contains("already exists", problemDetails.Detail);
        }

        [Fact]
        public async Task Register_WithInvalidEmail_ShouldReturnBadRequest()
        {
            // Arrange
            var controller = new AuthController(_repo, _configuration);

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
            var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
            Assert.Contains("Invalid email format", problemDetails.Detail);
        }

        [Fact]
        public async Task Register_WithWeakPassword_ShouldReturnBadRequest()
        {
            // Arrange
            var controller = new AuthController(_repo, _configuration);

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
            var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
            Assert.Contains("Password must be at least 8 characters", problemDetails.Detail);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnToken()
        {
            _output.WriteLine("\n=== Starting Login_WithValidCredentials_ShouldReturnToken ===");
            
            // Arrange
            var inMemorySettings = new Dictionary<string, string?> {
                {"Jwt:Key", "0123456789ABCDEF0123456789ABCDEF"}, // 32 chars
                {"Jwt:Issuer", "TestIssuer"},
                {"Jwt:Audience", "TestAudience"},
                {"Jwt:ExpiryMinutes", "60"}
            };
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            _output.WriteLine("Created test configuration");

            var controller = new AuthController(_repo, config);
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
            var response = Assert.IsType<Dictionary<string, object>>(okResult.Value);
            Assert.NotNull(response["token"]);
            Assert.Equal("testuser", response["username"]);
            Assert.Equal("User", response["role"]);
            _output.WriteLine("✓ All assertions passed");

            _output.WriteLine("=== Test Login_WithValidCredentials_ShouldReturnToken Completed ===\n");
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
        {
            // Arrange
            var controller = new AuthController(_repo, _configuration);

            // Register a user first
            var registerModel = new RegisterModel
            {
                Username = "invalidcreduser",
                Password = "TestPassword123!",
                Email = "invalidcreduser@example.com",
                Role = "User"
            };
            await controller.Register(registerModel);

            // Act - Try to login with wrong password
            var result = await controller.Login(new LoginModel
            {
                Username = "invalidcreduser",
                Password = "WrongPassword123!"
            });

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(unauthorizedResult.Value);
            Assert.Contains("Invalid username or password", problemDetails.Detail);
        }

        [Fact]
        public async Task Login_WithNonExistentUser_ShouldReturnUnauthorized()
        {
            // Arrange
            var controller = new AuthController(_repo, _configuration);

            // Act - Try to login with non-existent user
            var result = await controller.Login(new LoginModel
            {
                Username = "nonexistentuser",
                Password = "TestPassword123!"
            });

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(unauthorizedResult.Value);
            Assert.Contains("Invalid username or password", problemDetails.Detail);
        }

        [Fact]
        public async Task Register_WithInvalidRole_ShouldReturnBadRequest()
        {
            // Arrange
            var controller = new AuthController(_repo, _configuration);

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
            var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
            Assert.Contains("Invalid role", problemDetails.Detail);
        }

        public void Dispose()
        {
            _output.WriteLine("\n=== Starting AuthControllerTests Cleanup ===");
            _context.Dispose();
            _output.WriteLine("Disposed database context");
            _output.WriteLine("=== AuthControllerTests Cleanup Complete ===\n");
        }
    }
}
