using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Xunit.Abstractions;
using e_tour_api.Services;
using e_tour_api.Data;

namespace e_tour_api.Tests
{
    public class DocumentServiceTests : TestLoggerBase, IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IPdfStampService> _stampServiceMock;
        private readonly ILogger<DocumentService> _logger;
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly DocumentService _service;
        private readonly ITestOutputHelper _output;

        public DocumentServiceTests(ITestOutputHelper output)
        {
            _output = output;
            _output.WriteLine("\n=== Starting DocumentServiceTests Setup ===");
            
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"DocumentServiceTestsDb_{Guid.NewGuid()}")
                .Options;
            _context = new AppDbContext(options);
            _output.WriteLine("Initialized in-memory database");

            _stampServiceMock = new Mock<IPdfStampService>();
            _logger = new TestLogger<DocumentService>(LogMessages);
            _envMock = new Mock<IWebHostEnvironment>();
            _envMock.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());
            _output.WriteLine("Configured mocks and logger");

            _service = new DocumentService(_context, _stampServiceMock.Object, _logger, _envMock.Object);
            _output.WriteLine("Created service instance");
            
            _output.WriteLine("=== DocumentServiceTests Setup Complete ===\n");
        }

        [Fact]
        public async Task GetDocumentById_ReturnsDocument()
        {
            _output.WriteLine("\n=== Starting GetDocumentById_ReturnsDocument ===");
            
            // Arrange
            var doc = new Document { FileName = "doc1.pdf" };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();
            _output.WriteLine("Added test document to database");

            // Act
            _output.WriteLine("Calling GetDocumentById...");
            var result = await _service.GetDocumentById(doc.Id);
            _output.WriteLine("GetDocumentById call completed");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(doc.FileName, result.FileName);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Information, "Document retrieved");
            
            _output.WriteLine("=== Test GetDocumentById_ReturnsDocument Completed ===\n");
        }

        [Fact]
        public async Task VerifyDocumentAccess_ReturnsAccessDenied_WhenDocumentNotFound()
        {
            _output.WriteLine("\n=== Starting VerifyDocumentAccess_ReturnsAccessDenied_WhenDocumentNotFound ===");
            
            // Act
            _output.WriteLine("Calling VerifyDocumentAccess with non-existent document...");
            var result = await _service.VerifyDocumentAccess(-1, 1);
            _output.WriteLine("VerifyDocumentAccess call completed");

            // Assert
            Assert.False(result.HasAccess);
            Assert.Equal(404, result.ErrorCode);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Warning, "Document not found");
            
            _output.WriteLine("=== Test VerifyDocumentAccess_ReturnsAccessDenied_WhenDocumentNotFound Completed ===\n");
        }

        [Fact]
        public async Task VerifyDocumentAccess_ReturnsAccessDenied_WhenDocumentNotSigned()
        {
            _output.WriteLine("\n=== Starting VerifyDocumentAccess_ReturnsAccessDenied_WhenDocumentNotSigned ===");
            
            // Arrange
            var doc = new Document { Status = DocumentStatus.Draft, UserId = 1 };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();
            _output.WriteLine("Added unsigned document to database");

            // Act
            _output.WriteLine("Calling VerifyDocumentAccess...");
            var result = await _service.VerifyDocumentAccess(doc.Id, 1);
            _output.WriteLine("VerifyDocumentAccess call completed");

            // Assert
            Assert.False(result.HasAccess);
            Assert.Equal(404, result.ErrorCode);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Warning, "Document not signed");
            
            _output.WriteLine("=== Test VerifyDocumentAccess_ReturnsAccessDenied_WhenDocumentNotSigned Completed ===\n");
        }

        [Fact]
        public async Task VerifyDocumentAccess_ReturnsAccessDenied_WhenUserMismatch()
        {
            _output.WriteLine("\n=== Starting VerifyDocumentAccess_ReturnsAccessDenied_WhenUserMismatch ===");
            
            // Arrange
            var doc = new Document { Status = DocumentStatus.Signed, UserId = 1 };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();
            _output.WriteLine("Added signed document to database");

            // Act
            _output.WriteLine("Calling VerifyDocumentAccess with different user...");
            var result = await _service.VerifyDocumentAccess(doc.Id, 2);
            _output.WriteLine("VerifyDocumentAccess call completed");

            // Assert
            Assert.False(result.HasAccess);
            Assert.Equal(403, result.ErrorCode);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Warning, "User access denied");
            
            _output.WriteLine("=== Test VerifyDocumentAccess_ReturnsAccessDenied_WhenUserMismatch Completed ===\n");
        }

        [Fact]
        public async Task VerifyDocumentAccess_ReturnsAccessGranted_WhenValid()
        {
            _output.WriteLine("\n=== Starting VerifyDocumentAccess_ReturnsAccessGranted_WhenValid ===");
            
            // Arrange
            var doc = new Document { Status = DocumentStatus.Signed, UserId = 1 };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();
            _output.WriteLine("Added signed document to database");

            // Act
            _output.WriteLine("Calling VerifyDocumentAccess with valid user...");
            var result = await _service.VerifyDocumentAccess(doc.Id, 1);
            _output.WriteLine("VerifyDocumentAccess call completed");

            // Assert
            Assert.True(result.HasAccess);
            Assert.Null(result.ErrorCode);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Information, "Document access granted");
            
            _output.WriteLine("=== Test VerifyDocumentAccess_ReturnsAccessGranted_WhenValid Completed ===\n");
        }

        [Fact]
        public async Task StampDocument_ReturnsFailure_WhenDocumentNotFound()
        {
            _output.WriteLine("\n=== Starting StampDocument_ReturnsFailure_WhenDocumentNotFound ===");
            
            // Act
            _output.WriteLine("Calling StampDocument with non-existent document...");
            var result = await _service.StampDocument(-1, "stamp", Path.GetTempPath());
            _output.WriteLine("StampDocument call completed");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Document not found", result.ErrorMessage);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Warning, "Document not found for stamping");
            
            _output.WriteLine("=== Test StampDocument_ReturnsFailure_WhenDocumentNotFound Completed ===\n");
        }

        [Fact]
        public async Task StampDocument_ReturnsFailure_WhenFilePathInvalid()
        {
            _output.WriteLine("\n=== Starting StampDocument_ReturnsFailure_WhenFilePathInvalid ===");
            
            // Arrange
            var doc = new Document { FilePath = "file.txt" };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();
            _output.WriteLine("Added document with invalid file type to database");

            // Act
            _output.WriteLine("Calling StampDocument...");
            var result = await _service.StampDocument(doc.Id, "stamp", Path.GetTempPath());
            _output.WriteLine("StampDocument call completed");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid file type.", result.ErrorMessage);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Warning, "Invalid file type for stamping");
            
            _output.WriteLine("=== Test StampDocument_ReturnsFailure_WhenFilePathInvalid Completed ===\n");
        }

        [Fact]
        public async Task StampDocument_ReturnsSuccess_WhenStampingSucceeds()
        {
            _output.WriteLine("\n=== Starting StampDocument_ReturnsSuccess_WhenStampingSucceeds ===");
            
            // Create test directory and file
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var fileName = "file.pdf";
            var filePath = Path.Combine(tempDir, fileName);
            await File.WriteAllBytesAsync(filePath, new byte[] { 1, 2, 3, 4, 5 });
            _output.WriteLine("Created test file");

            var doc = new Document { FilePath = fileName };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();
            _output.WriteLine("Added document to database");

            _stampServiceMock.Setup(s => s.StampAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
                .Returns((string source, string dest, string stamp, string? password) => Task.FromResult(dest));
            _output.WriteLine("Configured stamp service mock");

            // Act
            _output.WriteLine("Calling StampDocument...");
            var result = await _service.StampDocument(doc.Id, "stamp", tempDir);
            _output.WriteLine("StampDocument call completed");

            // Assert
            Assert.True(result.Success);
            var updatedDoc = await _context.Documents.FindAsync(doc.Id);
            Assert.NotNull(updatedDoc);
            Assert.Equal(DocumentStatus.Signed, updatedDoc.Status);
            Assert.Equal(updatedDoc.SignedFilePath, Path.GetFileName(result.SignedFilePath));
            Assert.EndsWith(".pdf", updatedDoc.SignedFilePath);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Information, "Document stamped successfully");

            // Cleanup
            Directory.Delete(tempDir, true);
            _output.WriteLine("Cleaned up test directory");
            
            _output.WriteLine("=== Test StampDocument_ReturnsSuccess_WhenStampingSucceeds Completed ===\n");
        }

        [Fact]
        public async Task StampDocument_ReturnsFailure_WhenSourceFileNotFound()
        {
            _output.WriteLine("\n=== Starting StampDocument_ReturnsFailure_WhenSourceFileNotFound ===");
            
            // Arrange
            var doc = new Document { FilePath = "nonexistent.pdf" };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();
            _output.WriteLine("Added document with non-existent file to database");

            // Act
            _output.WriteLine("Calling StampDocument...");
            var result = await _service.StampDocument(doc.Id, "stamp", Path.GetTempPath());
            _output.WriteLine("StampDocument call completed");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Source file not found.", result.ErrorMessage);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Warning, "Source file not found for stamping");
            
            _output.WriteLine("=== Test StampDocument_ReturnsFailure_WhenSourceFileNotFound Completed ===\n");
        }

        [Fact]
        public async Task StampDocument_ReturnsFailure_WhenStampingThrows()
        {
            _output.WriteLine("\n=== Starting StampDocument_ReturnsFailure_WhenStampingThrows ===");
            
            // Create test directory and file
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var fileName = "file.pdf";
            var filePath = Path.Combine(tempDir, fileName);
            await File.WriteAllBytesAsync(filePath, new byte[] { 1, 2, 3, 4, 5 });
            _output.WriteLine("Created test file");

            var doc = new Document { FilePath = fileName };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();
            _output.WriteLine("Added document to database");

            _stampServiceMock.Setup(s => s.StampAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
                .ThrowsAsync(new Exception("Stamp error"));
            _output.WriteLine("Configured stamp service mock to throw");

            // Act
            _output.WriteLine("Calling StampDocument...");
            var result = await _service.StampDocument(doc.Id, "stamp", tempDir);
            _output.WriteLine("StampDocument call completed");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Stamp error", result.ErrorMessage);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Error, "Error during document stamping");

            // Cleanup
            Directory.Delete(tempDir, true);
            _output.WriteLine("Cleaned up test directory");
            
            _output.WriteLine("=== Test StampDocument_ReturnsFailure_WhenStampingThrows Completed ===\n");
        }

        [Fact]
        public async Task GetDocumentFile_ReturnsFailure_WhenDocumentNotFound()
        {
            _output.WriteLine("\n=== Starting GetDocumentFile_ReturnsFailure_WhenDocumentNotFound ===");
            
            // Act
            _output.WriteLine("Calling GetDocumentFile with non-existent document...");
            var result = await _service.GetDocumentFile(-1);
            _output.WriteLine("GetDocumentFile call completed");

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.ErrorCode);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Warning, "Document not found for download");
            
            _output.WriteLine("=== Test GetDocumentFile_ReturnsFailure_WhenDocumentNotFound Completed ===\n");
        }

        [Fact]
        public async Task GetDocumentFile_ReturnsFailure_WhenSignedFilePathIsNull()
        {
            _output.WriteLine("\n=== Starting GetDocumentFile_ReturnsFailure_WhenSignedFilePathIsNull ===");
            
            // Arrange
            var doc = new Document { SignedFilePath = null };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();
            _output.WriteLine("Added document with null signed file path to database");

            // Act
            _output.WriteLine("Calling GetDocumentFile...");
            var result = await _service.GetDocumentFile(doc.Id);
            _output.WriteLine("GetDocumentFile call completed");

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.ErrorCode);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Warning, "Signed file path is null");
            
            _output.WriteLine("=== Test GetDocumentFile_ReturnsFailure_WhenSignedFilePathIsNull Completed ===\n");
        }

        [Fact]
        public async Task GetDocumentFile_ReturnsFailure_WhenFileDoesNotExist()
        {
            _output.WriteLine("\n=== Starting GetDocumentFile_ReturnsFailure_WhenFileDoesNotExist ===");
            
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var uploadsDir = Path.Combine(tempDir, "uploads");
            Directory.CreateDirectory(uploadsDir);
            var fileName = "test.pdf";
            var filePath = Path.Combine(uploadsDir, fileName);
            var fileContent = new byte[] { 1, 2, 3, 4, 5 };
            await File.WriteAllBytesAsync(filePath, fileContent);
            _output.WriteLine("Created test file");

            var doc = new Document { SignedFilePath = Path.Combine(tempDir, "nonexistent.pdf") };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();
            _output.WriteLine("Added document with non-existent file to database");

            _envMock.Setup(e => e.WebRootPath).Returns(tempDir);
            _output.WriteLine("Configured environment mock");

            // Act
            _output.WriteLine("Calling GetDocumentFile...");
            var result = await _service.GetDocumentFile(doc.Id);
            _output.WriteLine("GetDocumentFile call completed");

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.ErrorCode);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Warning, "File not found for download");

            // Cleanup
            Directory.Delete(tempDir, true);
            _output.WriteLine("Cleaned up test directory");
            
            _output.WriteLine("=== Test GetDocumentFile_ReturnsFailure_WhenFileDoesNotExist Completed ===\n");
        }

        [Fact]
        public async Task GetDocumentFile_ReturnsFileContents_WhenFileExists()
        {
            _output.WriteLine("\n=== Starting GetDocumentFile_ReturnsFileContents_WhenFileExists ===");
            
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var uploadsDir = Path.Combine(tempDir, "uploads");
            Directory.CreateDirectory(uploadsDir);
            var fileName = "test.pdf";
            var filePath = Path.Combine(uploadsDir, fileName);
            var fileContent = new byte[] { 1, 2, 3, 4, 5 };
            await File.WriteAllBytesAsync(filePath, fileContent);
            _output.WriteLine("Created test file");

            var doc = new Document { SignedFilePath = fileName, FileName = "file.pdf" };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();
            _output.WriteLine("Added document to database");

            _envMock.Setup(e => e.WebRootPath).Returns(tempDir);
            _output.WriteLine("Configured environment mock");

            // Act
            _output.WriteLine("Calling GetDocumentFile...");
            var result = await _service.GetDocumentFile(doc.Id);
            _output.WriteLine("GetDocumentFile call completed");

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.FileContents);
            Assert.Equal(fileContent, result.FileContents);
            Assert.Equal("file.pdf", result.FileName);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Information, "File retrieved successfully");

            // Cleanup
            Directory.Delete(tempDir, true);
            _output.WriteLine("Cleaned up test directory");
            
            _output.WriteLine("=== Test GetDocumentFile_ReturnsFileContents_WhenFileExists Completed ===\n");
        }

        [Fact]
        public async Task UpdateDocument_UpdatesDocumentSuccessfully()
        {
            _output.WriteLine("\n=== Starting UpdateDocument_UpdatesDocumentSuccessfully ===");
            
            // Arrange
            var doc = new Document { FileName = "old.pdf" };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();
            _output.WriteLine("Added document to database");

            // Act
            _output.WriteLine("Updating document...");
            doc.FileName = "new.pdf";
            await _service.UpdateDocument(doc);
            _output.WriteLine("Update completed");

            // Assert
            var updatedDoc = await _context.Documents.FindAsync(doc.Id);
            Assert.NotNull(updatedDoc);
            Assert.Equal("new.pdf", updatedDoc.FileName);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Information, "Document updated successfully");
            
            _output.WriteLine("=== Test UpdateDocument_UpdatesDocumentSuccessfully Completed ===\n");
        }

        public void Dispose()
        {
            _output.WriteLine("\n=== Starting DocumentServiceTests Cleanup ===");
            
            _context.Dispose();
            _output.WriteLine("Disposed database context");
            
            _output.WriteLine("=== DocumentServiceTests Cleanup Complete ===\n");
        }
    }
}
