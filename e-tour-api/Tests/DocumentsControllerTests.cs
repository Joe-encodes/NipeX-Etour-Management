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

    public class DocumentsControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly HttpClient _client;
        private readonly string _testUploadsPath;
        private readonly WebApplicationFactory<Program> _factory;
        private readonly string _dbName = "TestDb_" + Guid.NewGuid().ToString();
        private readonly AppDbContext _dbContext;
        private readonly IServiceScope _scope;
        private readonly ITestOutputHelper _output;

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
                        return new DocumentService(context, stampService, logger, env);
                    });
                    _output.WriteLine("Configured DocumentService with test DbContext");
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
            _output.WriteLine("Sending GET request to /api/documents...");
            var response = await _client.GetAsync("/api/documents");
            _output.WriteLine($"Response status code: {response.StatusCode}");

            // Assert
            _output.WriteLine("Starting assertions...");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _output.WriteLine("✓ Status code verified");

            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Response content: {content}");
            
            var result = JsonSerializer.Deserialize<List<DocumentDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.Equal(2, result?.Count);
            _output.WriteLine("✓ Document count verified");

            _output.WriteLine($"First document: Id={result?[0].Id}, FileName={result?[0].FileName}, Status={result?[0].Status}, SignedFilePath={result?[0].SignedFilePath}");
            _output.WriteLine($"Second document: Id={result?[1].Id}, FileName={result?[1].FileName}, Status={result?[1].Status}, SignedFilePath={result?[1].SignedFilePath}");

            // Verify file paths and status
            Assert.Equal("test1.pdf", result?[0].SignedFilePath);
            Assert.Equal("test2.pdf", result?[1].SignedFilePath);
            Assert.Equal("Pending", result?[0].Status);
            Assert.Equal("Signed", result?[1].Status);
            _output.WriteLine("✓ File paths and status verified");

            // Cleanup
            _output.WriteLine("Starting cleanup...");
            var user = await _dbContext.Users.FindAsync(userId);
            if (user != null)
            {
                _dbContext.Users.Remove(user);
            }
            await _dbContext.SaveChangesAsync();
            _output.WriteLine("Cleanup completed");
            
            _output.WriteLine("=== Test GetUserDocuments_ReturnsOkWithDocuments Completed ===\n");
        }

        [Fact]
        public async Task GetPendingDocuments_ReturnsOkWithDocuments()
        {
            _output.WriteLine("\n=== Starting GetPendingDocuments_ReturnsOkWithDocuments ===");
            
            // Arrange
            _output.WriteLine("Creating test users...");
            var userId1 = await CreateTestUser(_dbContext, "user1", "User");
            var userId2 = await CreateTestUser(_dbContext, "approver1", "Approver");
            _output.WriteLine($"Created test users with IDs: {userId1}, {userId2}");

            // Set approver role in request headers
            _client.DefaultRequestHeaders.Remove("X-Test-UserId");
            _client.DefaultRequestHeaders.Add("X-Test-UserId", userId2.ToString());
            _client.DefaultRequestHeaders.Remove("X-Test-Role");
            _client.DefaultRequestHeaders.Add("X-Test-Role", "Approver");
            _client.DefaultRequestHeaders.Remove("X-Test-Email");
            _client.DefaultRequestHeaders.Add("X-Test-Email", "approver1@example.com");
            _output.WriteLine("Set approver headers");

            // Create test files
            var filePath1 = Path.Combine(_testUploadsPath, "pending1.pdf");
            var filePath2 = Path.Combine(_testUploadsPath, "pending2.pdf");
            var filePath3 = Path.Combine(_testUploadsPath, "signed.pdf");
            await File.WriteAllBytesAsync(filePath1, new byte[] { 1, 2, 3, 4, 5 });
            await File.WriteAllBytesAsync(filePath2, new byte[] { 1, 2, 3, 4, 5 });
            await File.WriteAllBytesAsync(filePath3, new byte[] { 1, 2, 3, 4, 5 });
            _output.WriteLine("Created test files");

            // Add test documents
            _output.WriteLine("Adding test documents...");
            var documents = new List<Document>
            {
                new Document
                {
                    FileName = "Pending Document 1",
                    SignedFilePath = Path.GetFileName(filePath1),
                    Status = DocumentStatus.Pending,
                    UserId = userId1,
                    UploadedAt = DateTime.UtcNow
                },
                new Document
                {
                    FileName = "Pending Document 2",
                    SignedFilePath = Path.GetFileName(filePath2),
                    Status = DocumentStatus.Pending,
                    UserId = userId1,
                    UploadedAt = DateTime.UtcNow
                },
                new Document
                {
                    FileName = "Signed Document",
                    SignedFilePath = Path.GetFileName(filePath3),
                    Status = DocumentStatus.Signed,
                    UserId = userId1,
                    UploadedAt = DateTime.UtcNow
                }
            };
            _dbContext.Documents.AddRange(documents);
            await _dbContext.SaveChangesAsync();
            _output.WriteLine("Test documents added to database");

            // Act
            _output.WriteLine("Sending GET request to /api/documents/pending...");
            var response = await _client.GetAsync("/api/documents/pending");
            _output.WriteLine($"Response status code: {response.StatusCode}");

            // Assert
            _output.WriteLine("Starting assertions...");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _output.WriteLine("✓ Status code verified");

            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Response content: {content}");
            
            var result = JsonSerializer.Deserialize<List<DocumentDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.Equal(2, result?.Count);
            _output.WriteLine("✓ Document count verified");

            // Verify only pending documents are returned with correct file paths
            Assert.All(result!, doc => Assert.Equal("Pending", doc.Status));
            Assert.Equal("pending1.pdf", result?[0].SignedFilePath);
            Assert.Equal("pending2.pdf", result?[1].SignedFilePath);
            _output.WriteLine("✓ Document status and file paths verified");

            // Cleanup
            _output.WriteLine("Starting cleanup...");
            var users = await _dbContext.Users.Where(u => u.Id == userId1 || u.Id == userId2).ToListAsync();
            _dbContext.Users.RemoveRange(users);
            await _dbContext.SaveChangesAsync();
            _output.WriteLine("Cleanup completed");
            
            _output.WriteLine("=== Test GetPendingDocuments_ReturnsOkWithDocuments Completed ===\n");
        }

        private async Task<int> CreateTestUser(AppDbContext dbContext, string username = "testuser", string role = "User")
        {
            var user = new User
            {
                Username = username,
                Email = $"{username}@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!"),
                Role = role
            };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
            return user.Id;
        }

        public void Dispose()
        {
            _output.WriteLine("\n=== Starting DocumentsControllerTests Cleanup ===");
            
            // Clean up test directory
            if (Directory.Exists(_testUploadsPath))
            {
                Directory.Delete(_testUploadsPath, true);
                _output.WriteLine($"Deleted test uploads directory: {_testUploadsPath}");
            }
            _scope.Dispose();
            _output.WriteLine("Disposed service scope");
            
            _output.WriteLine("=== DocumentsControllerTests Cleanup Complete ===\n");
        }
    }
}