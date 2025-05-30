using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using e_tour_api.Data;
using e_tour_api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Xunit;
using Moq;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using BCrypt.Net;
using Xunit.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace e_tour_api.Tests
{
    // Minimal test authentication handler
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var role = Context.Request.Headers["X-Test-Role"].FirstOrDefault() ?? "User";
            var userId = Context.Request.Headers["X-Test-UserId"].FirstOrDefault() ?? "1";
            var email = Context.Request.Headers["X-Test-Email"].FirstOrDefault() ?? "testuser@example.com";
            var claims = new[] {
                new Claim(ClaimTypes.Name, "TestUser"),
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Email, email)
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    public class DocumentsControllerTests : TestLoggerBase, IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly HttpClient _client;
        private readonly string _testUploadsPath;
        private readonly WebApplicationFactory<Program> _factory;
        private readonly string _dbName = "TestDb_" + Guid.NewGuid().ToString();
        private readonly AppDbContext _dbContext;
        private readonly IServiceScope _scope;
        private readonly ITestOutputHelper _output;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsControllerTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
        {
            _output = output;
            _output.WriteLine("\n=== Starting DocumentsControllerTests Setup ===");
            
            _factory = factory;
            // Create a temporary directory for test uploads
            _testUploadsPath = Path.Combine(Path.GetTempPath(), "TestUploads_" + Guid.NewGuid());
            Directory.CreateDirectory(_testUploadsPath);
            _output.WriteLine($"Created test uploads directory: {_testUploadsPath}");

            var webAppFactory = factory.WithWebHostBuilder(builder =>
            {
                _output.WriteLine("Configuring web host...");
                builder.ConfigureServices(services =>
                {
                    // Replace DB with in-memory
                    services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
                    services.AddDbContext<AppDbContext>(opts =>
                        opts.UseInMemoryDatabase(_dbName));
                    _output.WriteLine("Configured in-memory database");

                    // Replace auth with test scheme
                    services.AddAuthentication("TestScheme")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });
                    _output.WriteLine("Configured test authentication");

                    services.AddAuthorization(options =>
                    {
                        options.AddPolicy("Approver", policy =>
                            policy.RequireRole("Approver"));
                    });
                    _output.WriteLine("Configured authorization policies");

                    // Mock IWebHostEnvironment as singleton
                    var envMock = new Mock<IWebHostEnvironment>();
                    envMock.Setup(e => e.WebRootPath).Returns(_testUploadsPath);
                    envMock.Setup(e => e.WebRootFileProvider).Returns(new PhysicalFileProvider(_testUploadsPath));
                    services.AddSingleton(_ => envMock.Object);
                    _output.WriteLine("Configured web host environment mock");

                    services.PostConfigureAll<AuthenticationOptions>(options =>
                    {
                        options.DefaultAuthenticateScheme = "TestScheme";
                        options.DefaultChallengeScheme = "TestScheme";
                    });
                    _output.WriteLine("Configured authentication options");

                    // Register DocumentService with the same DbContext
                    services.AddScoped<IDocumentService>(sp =>
                    {
                        var context = sp.GetRequiredService<AppDbContext>();
                        var stampService = sp.GetRequiredService<IPdfStampService>();
                        var logger = sp.GetRequiredService<ILogger<DocumentService>>();
                        var env = sp.GetRequiredService<IWebHostEnvironment>();
                        var cache = sp.GetRequiredService<IMemoryCache>();
                        return new DocumentService(context, stampService, logger, env, cache);
                    });
                    _output.WriteLine("Configured DocumentService with test DbContext");

                    // Add test logger
                    _logger = new TestLogger<DocumentsController>(LogMessages);
                    services.AddSingleton(_logger);
                    _output.WriteLine("Configured test logger");
                });
            });
            _output.WriteLine("Created test client");

            // Create a scope to get the DbContext
            _scope = webAppFactory.Services.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _client = webAppFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("http://localhost")
            });
            _output.WriteLine($"Initialized database context with name: {_dbName}");

            // Use test auth header
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("TestScheme");
            _client.DefaultRequestHeaders.Add("X-Test-Role", "User");
            _output.WriteLine("Configured test authentication headers");
            
            _output.WriteLine("=== DocumentsControllerTests Setup Complete ===\n");
        }

        [Fact]
        public async Task GetUserDocuments_ReturnsOkWithDocuments()
        {
            _output.WriteLine("\n=== Starting GetUserDocuments_ReturnsOkWithDocuments ===");
            
            // Arrange
            _output.WriteLine("Creating test user...");
            var userId = await CreateTestUser(_dbContext);
            _output.WriteLine($"Created test user with ID: {userId}");

            // Create test files
            var filePath1 = Path.Combine(_testUploadsPath, "test1.pdf");
            var filePath2 = Path.Combine(_testUploadsPath, "test2.pdf");
            await File.WriteAllBytesAsync(filePath1, new byte[] { 1, 2, 3, 4, 5 });
            await File.WriteAllBytesAsync(filePath2, new byte[] { 1, 2, 3, 4, 5 });
            _output.WriteLine("Created test files");

            // Add test documents
            _output.WriteLine("Adding test documents...");
            var documents = new List<Document>
            {
                new Document
                {
                    FileName = "Test Document 1",
                    SignedFilePath = Path.GetFileName(filePath1),
                    Status = DocumentStatus.Pending,
                    UserId = userId,
                    UploadedAt = DateTime.UtcNow
                },
                new Document
                {
                    FileName = "Test Document 2",
                    SignedFilePath = Path.GetFileName(filePath2),
                    Status = DocumentStatus.Signed,
                    UserId = userId,
                    UploadedAt = DateTime.UtcNow
                }
            };
            _dbContext.Documents.AddRange(documents);
            await _dbContext.SaveChangesAsync();
            _output.WriteLine("Test documents added to database");

            // Set user ID and role in request headers
            _client.DefaultRequestHeaders.Remove("X-Test-UserId");
            _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
            _client.DefaultRequestHeaders.Remove("X-Test-Role");
            _client.DefaultRequestHeaders.Add("X-Test-Role", "User");
            _client.DefaultRequestHeaders.Remove("X-Test-Email");
            _client.DefaultRequestHeaders.Add("X-Test-Email", "testuser@example.com");
            _output.WriteLine("Set test user headers");

            // Act
            _output.WriteLine("Sending request to get user documents...");
            var response = await _client.GetAsync("/api/Documents/user");
            _output.WriteLine($"Response status code: {response.StatusCode}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Response content: {content}");
            var result = JsonSerializer.Deserialize<ServiceResult<List<DocumentDto>>>(content);
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(2, result.Data.Count);
            
            // Verify logging
            AssertLogMessage(LogLevel.Information, "Retrieved user documents");
        }

        [Fact]
        public async Task UploadDocument_ValidFile_ReturnsOk()
        {
            _output.WriteLine("\n=== Starting UploadDocument_ValidFile_ReturnsOk ===");
            
            // Arrange
            var userId = await CreateTestUser(_dbContext);
            _client.DefaultRequestHeaders.Remove("X-Test-UserId");
            _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

            var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4, 5 });
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(fileContent, "file", "test.pdf");

            // Act
            _output.WriteLine("Sending document upload request...");
            var response = await _client.PostAsync("/api/Documents/upload", content);
            _output.WriteLine($"Response status code: {response.StatusCode}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Response content: {responseContent}");
            var result = JsonSerializer.Deserialize<ServiceResult<DocumentDto>>(responseContent);
            Assert.NotNull(result);
            Assert.True(result.Success);
            
            // Verify logging
            AssertLogMessage(LogLevel.Information, "Document uploaded successfully");
        }

        [Fact]
        public async Task UploadDocument_InvalidFile_ReturnsBadRequest()
        {
            _output.WriteLine("\n=== Starting UploadDocument_InvalidFile_ReturnsBadRequest ===");
            
            // Arrange
            var userId = await CreateTestUser(_dbContext);
            _client.DefaultRequestHeaders.Remove("X-Test-UserId");
            _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

            var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4, 5 });
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/exe");
            content.Add(fileContent, "file", "test.exe");

            // Act
            _output.WriteLine("Sending invalid document upload request...");
            var response = await _client.PostAsync("/api/Documents/upload", content);
            _output.WriteLine($"Response status code: {response.StatusCode}");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Response content: {responseContent}");
            var result = JsonSerializer.Deserialize<ServiceResult<object>>(responseContent);
            Assert.NotNull(result);
            Assert.False(result.Success);
            
            // Verify logging
            AssertLogMessage(LogLevel.Warning, "Invalid file type");
        }

        private async Task<int> CreateTestUser(AppDbContext dbContext, string username = "testuser", string role = "User")
        {
            var user = new User
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Email = $"{username}@example.com",
                Role = role
            };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
            return user.Id;
        }

        public void Dispose()
        {
            _output.WriteLine("\n=== Starting DocumentsControllerTests Cleanup ===");
            
            // Clean up test files
            if (Directory.Exists(_testUploadsPath))
            {
                Directory.Delete(_testUploadsPath, true);
                _output.WriteLine($"Deleted test uploads directory: {_testUploadsPath}");
            }

            // Clean up database
            _dbContext.Database.EnsureDeleted();
            _output.WriteLine("Deleted test database");

            _scope.Dispose();
            _output.WriteLine("Disposed service scope");
            
            _output.WriteLine("=== DocumentsControllerTests Cleanup Complete ===\n");
        }
    }
}