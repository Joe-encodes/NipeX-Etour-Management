using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using e_tour_api.Services;
using e_tour_api.Data;

namespace e_tour_api.Tests
{
    public class DocumentServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IPdfStampService> _stampServiceMock;
        private readonly Mock<ILogger<DocumentService>> _loggerMock;
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly DocumentService _service;

        public DocumentServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "DocumentServiceTestsDb")
                .Options;
            _context = new AppDbContext(options);

            _stampServiceMock = new Mock<IPdfStampService>();
            _loggerMock = new Mock<ILogger<DocumentService>>();
            _envMock = new Mock<IWebHostEnvironment>();
            _envMock.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());

            _service = new DocumentService(_context, _stampServiceMock.Object, _loggerMock.Object, _envMock.Object);
        }

        [Fact]
        public async Task GetDocumentById_ReturnsDocument()
        {
            var doc = new Document { FileName = "doc1.pdf" };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            var result = await _service.GetDocumentById(doc.Id);

            Assert.NotNull(result);
            Assert.Equal(doc.FileName, result.FileName);
        }

        [Fact]
        public async Task VerifyDocumentAccess_ReturnsAccessDenied_WhenDocumentNotFound()
        {
            var result = await _service.VerifyDocumentAccess(-1, 1);
            Assert.False(result.HasAccess);
            Assert.Equal(404, result.ErrorCode);
        }

        [Fact]
        public async Task VerifyDocumentAccess_ReturnsAccessDenied_WhenDocumentNotSigned()
        {
            var doc = new Document { Status = DocumentStatus.Draft, UserId = 1 };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            var result = await _service.VerifyDocumentAccess(doc.Id, 1);
            Assert.False(result.HasAccess);
            Assert.Equal(404, result.ErrorCode);
        }

        [Fact]
        public async Task VerifyDocumentAccess_ReturnsAccessDenied_WhenUserMismatch()
        {
            var doc = new Document { Status = DocumentStatus.Signed, UserId = 1 };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            var result = await _service.VerifyDocumentAccess(doc.Id, 2);
            Assert.False(result.HasAccess);
            Assert.Equal(403, result.ErrorCode);
        }

        [Fact]
        public async Task VerifyDocumentAccess_ReturnsAccessGranted_WhenValid()
        {
            var doc = new Document { Status = DocumentStatus.Signed, UserId = 1 };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            var result = await _service.VerifyDocumentAccess(doc.Id, 1);
            Assert.True(result.HasAccess);
            Assert.Null(result.ErrorCode);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task StampDocument_ReturnsFailure_WhenDocumentNotFound()
        {
            var result = await _service.StampDocument(-1, "stamp", Path.GetTempPath());
            Assert.False(result.Success);
            Assert.Equal("Document not found", result.ErrorMessage);
        }

        [Fact]
        public async Task StampDocument_ReturnsFailure_WhenFilePathInvalid()
        {
            var doc = new Document { FilePath = "file.txt" };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            var result = await _service.StampDocument(doc.Id, "stamp", Path.GetTempPath());
            Assert.False(result.Success);
            Assert.Equal("Invalid file type.", result.ErrorMessage);
        }

        [Fact]
        public async Task StampDocument_ReturnsSuccess_WhenStampingSucceeds()
        {
            // Create test directory and file
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var fileName = "file.pdf";
            var filePath = Path.Combine(tempDir, fileName);
            await File.WriteAllBytesAsync(filePath, new byte[] { 1, 2, 3, 4, 5 });

            var doc = new Document { FilePath = fileName };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            _stampServiceMock.Setup(s => s.StampAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
                .Returns((string source, string dest, string stamp, string? password) => Task.FromResult(dest));

            var result = await _service.StampDocument(doc.Id, "stamp", tempDir);

            Assert.True(result.Success);
            var updatedDoc = await _context.Documents.FindAsync(doc.Id);
            Assert.NotNull(updatedDoc);
            Assert.Equal(DocumentStatus.Signed, updatedDoc.Status);
            Assert.Equal(updatedDoc.SignedFilePath, Path.GetFileName(result.SignedFilePath));
            Assert.EndsWith(".pdf", updatedDoc.SignedFilePath);

            // Cleanup
            Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task StampDocument_ReturnsFailure_WhenSourceFileNotFound()
        {
            var doc = new Document { FilePath = "nonexistent.pdf" };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            var result = await _service.StampDocument(doc.Id, "stamp", Path.GetTempPath());

            Assert.False(result.Success);
            Assert.Equal("Source file not found.", result.ErrorMessage);
        }

        [Fact]
        public async Task StampDocument_ReturnsFailure_WhenStampingThrows()
        {
            // Create test directory and file
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var fileName = "file.pdf";
            var filePath = Path.Combine(tempDir, fileName);
            await File.WriteAllBytesAsync(filePath, new byte[] { 1, 2, 3, 4, 5 });

            var doc = new Document { FilePath = fileName };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            _stampServiceMock.Setup(s => s.StampAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
                .ThrowsAsync(new Exception("Stamp error"));

            var result = await _service.StampDocument(doc.Id, "stamp", tempDir);

            Assert.False(result.Success);
            Assert.Contains("Stamp error", result.ErrorMessage);

            // Cleanup
            Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task GetDocumentFile_ReturnsFailure_WhenDocumentNotFound()
        {
            var result = await _service.GetDocumentFile(-1);
            Assert.False(result.Success);
            Assert.Equal(404, result.ErrorCode);
        }

        [Fact]
        public async Task GetDocumentFile_ReturnsFailure_WhenSignedFilePathIsNull()
        {
            var doc = new Document { SignedFilePath = null };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            var result = await _service.GetDocumentFile(doc.Id);
            Assert.False(result.Success);
            Assert.Equal(404, result.ErrorCode);
        }

        [Fact]
        public async Task GetDocumentFile_ReturnsFailure_WhenFileDoesNotExist()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var uploadsDir = Path.Combine(tempDir, "uploads");
            Directory.CreateDirectory(uploadsDir);
            var fileName = "test.pdf";
            var filePath = Path.Combine(uploadsDir, fileName);
            var fileContent = new byte[] { 1, 2, 3, 4, 5 };
            await File.WriteAllBytesAsync(filePath, fileContent);

            var doc = new Document { SignedFilePath = Path.Combine(tempDir, "nonexistent.pdf") };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            _envMock.Setup(e => e.WebRootPath).Returns(tempDir);

            var result = await _service.GetDocumentFile(doc.Id);

            Assert.False(result.Success);
            Assert.Equal(404, result.ErrorCode);

            // Cleanup
            Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task GetDocumentFile_ReturnsFileContents_WhenFileExists()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var uploadsDir = Path.Combine(tempDir, "uploads");
            Directory.CreateDirectory(uploadsDir);
            var fileName = "test.pdf";
            var filePath = Path.Combine(uploadsDir, fileName);
            var fileContent = new byte[] { 1, 2, 3, 4, 5 };
            await File.WriteAllBytesAsync(filePath, fileContent);

            var doc = new Document { SignedFilePath = fileName, FileName = "file.pdf" };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            _envMock.Setup(e => e.WebRootPath).Returns(tempDir);

            var result = await _service.GetDocumentFile(doc.Id);

            Assert.True(result.Success);
            Assert.NotNull(result.FileContents);
            Assert.Equal(fileContent, result.FileContents);
            Assert.Equal("file.pdf", result.FileName);

            // Cleanup
            Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task UpdateDocument_UpdatesDocumentSuccessfully()
        {
            var doc = new Document { FileName = "old.pdf" };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            doc.FileName = "new.pdf";
            await _service.UpdateDocument(doc);

            var updatedDoc = await _context.Documents.FindAsync(doc.Id);
            if (updatedDoc != null)
            {
                Assert.Equal("new.pdf", updatedDoc.FileName);
            }
            else
            {
               throw new InvalidOperationException("updatedDoc is null");
            }
        }
    }
}
