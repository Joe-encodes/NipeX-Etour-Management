using System;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using e_tour_api.Controllers;
using e_tour_api.Services;
using e_tour_api.Models.DTOs;
using e_tour_api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.IO;
using Moq;

namespace e_tour_api.Tests.Tests.Integration
{
    public class DocumentApprovalFlowTests : TestLoggerBase, IDisposable
    {
        private readonly AppDbContext _context;
        private readonly IDocumentService _documentService;
        private readonly IUserService _userService;
        private readonly DocumentsController _documentsController;
        private readonly DocumentApprovalsController _approvalsController;
        private readonly UsersController _usersController;
        private readonly string _testUploadPath;
        private readonly ITestOutputHelper _output;
        private readonly ILogger<DocumentsController> _documentsLogger;
        private readonly ILogger<DocumentApprovalsController> _approvalsLogger;
        private readonly ILogger<UsersController> _usersLogger;

        public DocumentApprovalFlowTests(ITestOutputHelper output)
        {
            _output = output;
            _output.WriteLine("\n=== Starting DocumentApprovalFlowTests Setup ===");
            
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);
            _output.WriteLine("Initialized in-memory database");

            // Setup test upload directory
            _testUploadPath = Path.Combine(Path.GetTempPath(), "e-tour-tests");
            Directory.CreateDirectory(_testUploadPath);
            _output.WriteLine($"Created test upload directory: {_testUploadPath}");

            // Initialize loggers
            _documentsLogger = new TestLogger<DocumentsController>(LogMessages);
            _approvalsLogger = new TestLogger<DocumentApprovalsController>(LogMessages);
            _usersLogger = new TestLogger<UsersController>(LogMessages);
            _output.WriteLine("Initialized test loggers");

            // Initialize services
            _documentService = new DocumentService(_context, new PdfStampService(), _documentsLogger);
            _userService = new UserService(_context, _usersLogger);
            _output.WriteLine("Initialized services");

            // Initialize controllers
            _documentsController = new DocumentsController(_documentService, _documentsLogger);
            _approvalsController = new DocumentApprovalsController(_documentService, _approvalsLogger);
            _usersController = new UsersController(_userService, _usersLogger);
            _output.WriteLine("Initialized controllers");

            // Setup test users
            SetupTestUsers();
            _output.WriteLine("=== DocumentApprovalFlowTests Setup Complete ===\n");
        }

        private void SetupTestUsers()
        {
            _output.WriteLine("Setting up test users...");
            
            // Create regular user
            var user = new User
            {
                Username = "testuser",
                Email = "test@example.com",
                Role = "User"
            };
            _context.Users.Add(user);
            _output.WriteLine("Created regular user");

            // Create approver
            var approver = new User
            {
                Username = "approver",
                Email = "approver@example.com",
                Role = "Approver"
            };
            _context.Users.Add(approver);
            _output.WriteLine("Created approver user");

            _context.SaveChanges();
            _output.WriteLine("Test users saved to database");
        }

        [Fact]
        public async Task CompleteDocumentApprovalFlow_Success()
        {
            _output.WriteLine("\n=== Starting CompleteDocumentApprovalFlow_Success ===");
            
            // 1. User uploads a document
            _output.WriteLine("Step 1: User uploading document...");
            var fileContent = new byte[] { 1, 2, 3, 4 };
            var fileName = "test.pdf";
            var fileStream = new MemoryStream(fileContent);
            var formFile = new FormFile(fileStream, 0, fileContent.Length, "file", fileName);

            // Set user context
            var userClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "User")
            };
            _documentsController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(userClaims)) }
            };

            var uploadResult = await _documentsController.UploadDocument(formFile, "Test Document");
            var uploadResponse = Assert.IsType<OkObjectResult>(uploadResult).Value as ServiceResult<DocumentUploadResponse>;
            Assert.True(uploadResponse.Success);
            var documentId = uploadResponse.Data.DocumentId;
            _output.WriteLine($"Document uploaded successfully. ID: {documentId}");

            // Verify logging
            AssertLogMessage(LogLevel.Information, "Document uploaded successfully");

            // 2. Approver assigns themselves
            _output.WriteLine("Step 2: Approver assigning themselves...");
            var approverClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "2"),
                new Claim(ClaimTypes.Role, "Approver")
            };
            _approvalsController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(approverClaims)) }
            };

            var assignResult = await _approvalsController.AssignApprover(documentId, new AssignApproverRequest { ApproverId = 2 });
            var assignResponse = Assert.IsType<OkObjectResult>(assignResult).Value as ServiceResult<DocumentDto>;
            Assert.True(assignResponse.Success);
            Assert.Equal(2, assignResponse.Data.AssignedApproverId);
            _output.WriteLine("Approver assigned successfully");

            // Verify logging
            AssertLogMessage(LogLevel.Information, "Approver assigned successfully");

            // 3. Approver approves the document
            _output.WriteLine("Step 3: Approver approving document...");
            var approveResult = await _approvalsController.ApproveDocument(documentId, new ApproveDocumentRequest
            {
                Signature = "Test Approver",
                Password = "test123"
            });
            var approveResponse = Assert.IsType<OkObjectResult>(approveResult).Value as ServiceResult<DocumentDto>;
            Assert.True(approveResponse.Success);
            Assert.Equal(DocumentStatus.Approved, approveResponse.Data.Status);
            Assert.NotNull(approveResponse.Data.SignedFilePath);
            _output.WriteLine("Document approved successfully");

            // Verify logging
            AssertLogMessage(LogLevel.Information, "Document approved successfully");

            // 4. User downloads the approved document
            _output.WriteLine("Step 4: User downloading approved document...");
            var downloadResult = await _documentsController.DownloadDocument(documentId);
            Assert.IsType<FileStreamResult>(downloadResult);
            _output.WriteLine("Document downloaded successfully");

            // Verify logging
            AssertLogMessage(LogLevel.Information, "Document downloaded successfully");
            
            _output.WriteLine("=== Test CompleteDocumentApprovalFlow_Success Completed ===\n");
        }

        [Fact]
        public async Task DocumentRejectionFlow_Success()
        {
            _output.WriteLine("\n=== Starting DocumentRejectionFlow_Success ===");
            
            // 1. User uploads a document
            _output.WriteLine("Step 1: User uploading document...");
            var fileContent = new byte[] { 1, 2, 3, 4 };
            var fileName = "test.pdf";
            var fileStream = new MemoryStream(fileContent);
            var formFile = new FormFile(fileStream, 0, fileContent.Length, "file", fileName);

            // Set user context
            var userClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "User")
            };
            _documentsController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(userClaims)) }
            };

            var uploadResult = await _documentsController.UploadDocument(formFile, "Test Document");
            var uploadResponse = Assert.IsType<OkObjectResult>(uploadResult).Value as ServiceResult<DocumentUploadResponse>;
            Assert.True(uploadResponse.Success);
            var documentId = uploadResponse.Data.DocumentId;
            _output.WriteLine($"Document uploaded successfully. ID: {documentId}");

            // Verify logging
            AssertLogMessage(LogLevel.Information, "Document uploaded successfully");

            // 2. Approver rejects the document
            _output.WriteLine("Step 2: Approver rejecting document...");
            var approverClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "2"),
                new Claim(ClaimTypes.Role, "Approver")
            };
            _approvalsController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(approverClaims)) }
            };

            var rejectResult = await _approvalsController.RejectDocument(documentId, new RejectDocumentRequest
            {
                Reason = "Document needs revision"
            });
            var rejectResponse = Assert.IsType<OkObjectResult>(rejectResult).Value as ServiceResult<DocumentDto>;
            Assert.True(rejectResponse.Success);
            Assert.Equal(DocumentStatus.Rejected, rejectResponse.Data.Status);
            Assert.Equal("Document needs revision", rejectResponse.Data.RejectionReason);
            _output.WriteLine("Document rejected successfully");

            // Verify logging
            AssertLogMessage(LogLevel.Information, "Document rejected successfully");
            
            _output.WriteLine("=== Test DocumentRejectionFlow_Success Completed ===\n");
        }

        public void Dispose()
        {
            _output.WriteLine("\n=== Starting DocumentApprovalFlowTests Cleanup ===");
            
            _context.Database.EnsureDeleted();
            _output.WriteLine("Deleted test database");

            _context.Dispose();
            _output.WriteLine("Disposed database context");

            if (Directory.Exists(_testUploadPath))
            {
                Directory.Delete(_testUploadPath, true);
                _output.WriteLine($"Deleted test upload directory: {_testUploadPath}");
            }
            
            _output.WriteLine("=== DocumentApprovalFlowTests Cleanup Complete ===\n");
        }
    }
} 