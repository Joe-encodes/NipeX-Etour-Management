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
using Xunit.Abstractions;

namespace e_tour_api.Tests
{
    public class CleanupServiceTests : IDisposable, TestLoggerBase
    {
        private readonly string _tempUploadsPath;
        private readonly AppDbContext _context;
        private readonly CleanupService _service;
        private readonly ILogger<CleanupService> _logger;
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly ITestOutputHelper _output;

        public CleanupServiceTests(ITestOutputHelper output)
        {
            _output = output;
            _output.WriteLine("\n=== Starting CleanupServiceTests Setup ===");
            
            // Setup temporary uploads directory
            _tempUploadsPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempUploadsPath);
            _output.WriteLine($"Created temp directory: {_tempUploadsPath}");

            // Setup in-memory database
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"CleanupServiceTestsDb_{Guid.NewGuid()}")
                .Options;
            _context = new AppDbContext(options);
            _output.WriteLine("Initialized in-memory database");

            // Setup environment mock
            _envMock = new Mock<IWebHostEnvironment>();
            _envMock.Setup(e => e.WebRootPath).Returns(_tempUploadsPath);
            _output.WriteLine("Configured environment mock");

            // Initialize test logger
            _logger = new TestLogger<CleanupService>(LogMessages);
            _output.WriteLine("Initialized test logger");

            // Create service instance
            _service = new CleanupService(_context, _logger, _envMock.Object, _tempUploadsPath);
            _output.WriteLine("Created service instance");
            
            _output.WriteLine("=== CleanupServiceTests Setup Complete ===\n");
        }

        [Fact]
        public async Task CleanupOrphanedFilesAsync_DeletesUnreferencedFiles()
        {
            _output.WriteLine("\n=== Starting CleanupOrphanedFilesAsync_DeletesUnreferencedFiles ===");
            
            // Arrange
            _output.WriteLine("Creating test files...");
            var referencedFile = "referenced.pdf";
            var orphanedFile = "orphaned.pdf";
            File.WriteAllText(Path.Combine(_tempUploadsPath, referencedFile), "content");
            File.WriteAllText(Path.Combine(_tempUploadsPath, orphanedFile), "content");
            _output.WriteLine("Test files created");

            // Add document referencing one file
            _output.WriteLine("Adding document reference to database...");
            _context.Documents.Add(new Document { SignedFilePath = Path.Combine(_tempUploadsPath, referencedFile) });
            await _context.SaveChangesAsync();
            _output.WriteLine("Document reference added");

            // Act
            _output.WriteLine("Executing cleanup...");
            var deletedCount = await _service.CleanupOrphanedFilesAsync();
            _output.WriteLine($"Cleanup completed. Deleted count: {deletedCount}");

            // Assert
            Assert.Equal(1, deletedCount);
            Assert.True(File.Exists(Path.Combine(_tempUploadsPath, referencedFile)));
            Assert.False(File.Exists(Path.Combine(_tempUploadsPath, orphanedFile)));
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Information, "Cleanup completed successfully");
            
            _output.WriteLine("=== Test CleanupOrphanedFilesAsync_DeletesUnreferencedFiles Completed ===\n");
        }

        [Fact]
        public async Task CleanupOrphanedFilesAsync_WithFilter_DeletesMatchingFilesOnly()
        {
            _output.WriteLine("\n=== Starting CleanupOrphanedFilesAsync_WithFilter_DeletesMatchingFilesOnly ===");
            
            // Arrange
            _output.WriteLine("Creating test files...");
            var file1 = "file1.pdf";
            var file2 = "file2.pdf";
            File.WriteAllText(Path.Combine(_tempUploadsPath, file1), "content");
            File.WriteAllText(Path.Combine(_tempUploadsPath, file2), "content");
            _output.WriteLine("Test files created");

            _output.WriteLine("Adding document reference to database...");
            _context.Documents.Add(new Document { SignedFilePath = Path.Combine(_tempUploadsPath, "other.pdf") });
            await _context.SaveChangesAsync();
            _output.WriteLine("Document reference added");

            // Act
            _output.WriteLine("Executing cleanup with filter...");
            var deletedCount = await _service.CleanupOrphanedFilesAsync("file1");
            _output.WriteLine($"Cleanup completed. Deleted count: {deletedCount}");

            // Assert
            Assert.Equal(1, deletedCount);
            Assert.False(File.Exists(Path.Combine(_tempUploadsPath, file1)));
            Assert.True(File.Exists(Path.Combine(_tempUploadsPath, file2)));
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Information, "Cleanup completed successfully");
            
            _output.WriteLine("=== Test CleanupOrphanedFilesAsync_WithFilter_DeletesMatchingFilesOnly Completed ===\n");
        }

        [Fact]
        public async Task CleanupOrphanedFilesAsync_WithInvalidFilter_ThrowsArgumentException()
        {
            _output.WriteLine("\n=== Starting CleanupOrphanedFilesAsync_WithInvalidFilter_ThrowsArgumentException ===");
            
            var invalidFilter = "[invalid";
            _output.WriteLine($"Using invalid filter: {invalidFilter}");
            _output.WriteLine("Expecting ArgumentException...");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.CleanupOrphanedFilesAsync(invalidFilter));
            _output.WriteLine($"Exception thrown: {exception.Message}");

            // Verify logging
            AssertLogMessage(LogLevel.Warning, "Invalid filter pattern");
            
            _output.WriteLine("=== Test CleanupOrphanedFilesAsync_WithInvalidFilter_ThrowsArgumentException Completed ===\n");
        }

        [Fact]
        public async Task CleanupOrphanedFilesAsync_WithEmptyDirectory_ReturnsZero()
        {
            _output.WriteLine("\n=== Starting CleanupOrphanedFilesAsync_WithEmptyDirectory_ReturnsZero ===");
            
            // Act
            _output.WriteLine("Executing cleanup on empty directory...");
            var deletedCount = await _service.CleanupOrphanedFilesAsync();
            _output.WriteLine($"Cleanup completed. Deleted count: {deletedCount}");

            // Assert
            Assert.Equal(0, deletedCount);
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Information, "Cleanup completed successfully");
            
            _output.WriteLine("=== Test CleanupOrphanedFilesAsync_WithEmptyDirectory_ReturnsZero Completed ===\n");
        }

        [Fact]
        public async Task CleanupOrphanedFilesAsync_WithAllReferencedFiles_ReturnsZero()
        {
            _output.WriteLine("\n=== Starting CleanupOrphanedFilesAsync_WithAllReferencedFiles_ReturnsZero ===");
            
            // Arrange
            _output.WriteLine("Creating test files...");
            var file1 = "file1.pdf";
            var file2 = "file2.pdf";
            File.WriteAllText(Path.Combine(_tempUploadsPath, file1), "content");
            File.WriteAllText(Path.Combine(_tempUploadsPath, file2), "content");
            _output.WriteLine("Test files created");

            _output.WriteLine("Adding document references to database...");
            _context.Documents.Add(new Document { SignedFilePath = Path.Combine(_tempUploadsPath, file1) });
            _context.Documents.Add(new Document { SignedFilePath = Path.Combine(_tempUploadsPath, file2) });
            await _context.SaveChangesAsync();
            _output.WriteLine("Document references added");

            // Act
            _output.WriteLine("Executing cleanup...");
            var deletedCount = await _service.CleanupOrphanedFilesAsync();
            _output.WriteLine($"Cleanup completed. Deleted count: {deletedCount}");

            // Assert
            Assert.Equal(0, deletedCount);
            Assert.True(File.Exists(Path.Combine(_tempUploadsPath, file1)));
            Assert.True(File.Exists(Path.Combine(_tempUploadsPath, file2)));
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Information, "Cleanup completed successfully");
            
            _output.WriteLine("=== Test CleanupOrphanedFilesAsync_WithAllReferencedFiles_ReturnsZero Completed ===\n");
        }

        [Fact]
        public async Task CleanupOrphanedFilesAsync_WithFileAccessError_LogsErrorAndContinues()
        {
            _output.WriteLine("\n=== Starting CleanupOrphanedFilesAsync_WithFileAccessError_LogsErrorAndContinues ===");
            
            // Create test files
            _output.WriteLine("Creating test files: file1.pdf, file2.pdf");
            var file1 = Path.Combine(_tempUploadsPath, "file1.pdf");
            var file2 = Path.Combine(_tempUploadsPath, "file2.pdf");
            await File.WriteAllBytesAsync(file1, new byte[] { 1, 2, 3, 4, 5 });
            await File.WriteAllBytesAsync(file2, new byte[] { 1, 2, 3, 4, 5 });
            _output.WriteLine("Test files created successfully");

            // Add one file to database
            _output.WriteLine("Adding document reference to database...");
            var doc = new Document { SignedFilePath = Path.Combine(_tempUploadsPath, "other.pdf") };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();
            _output.WriteLine("Document reference added");

            // Make second file read-only to simulate access error
            _output.WriteLine("Setting file2 to read-only...");
            var fileInfo = new FileInfo(file2);
            fileInfo.IsReadOnly = true;
            _output.WriteLine("File2 is now read-only");

            // Execute cleanup
            _output.WriteLine("Executing cleanup...");
            var result = await _service.CleanupOrphanedFilesAsync();
            _output.WriteLine($"Cleanup completed. Deleted count: {result}");

            // Assert
            _output.WriteLine("Starting assertions...");
            Assert.Equal(2, result); // Both files should be deleted since they are not referenced
            Assert.False(File.Exists(file1)); // Unreferenced file should be deleted
            Assert.False(File.Exists(file2)); // File with access error should also be deleted on macOS
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Warning, "Error accessing file");
            AssertLogMessage(LogLevel.Information, "Cleanup completed successfully");
            
            _output.WriteLine("=== Test CleanupOrphanedFilesAsync_WithFileAccessError_LogsErrorAndContinues Completed ===\n");
        }

        [Fact]
        public async Task CleanupOrphanedFilesAsync_WithSubdirectories_IgnoresSubdirectories()
        {
            _output.WriteLine("\n=== Starting CleanupOrphanedFilesAsync_WithSubdirectories_IgnoresSubdirectories ===");
            
            // Arrange
            _output.WriteLine("Creating test directory structure...");
            var subDir = Path.Combine(_tempUploadsPath, "subdir");
            Directory.CreateDirectory(subDir);
            var file1 = "file1.pdf";
            var file2 = "subdir/file2.pdf";
            File.WriteAllText(Path.Combine(_tempUploadsPath, file1), "content");
            File.WriteAllText(Path.Combine(_tempUploadsPath, file2), "content");
            _output.WriteLine("Test directory structure created");

            // Act
            _output.WriteLine("Executing cleanup...");
            var deletedCount = await _service.CleanupOrphanedFilesAsync();
            _output.WriteLine($"Cleanup completed. Deleted count: {deletedCount}");

            // Assert
            Assert.Equal(1, deletedCount);
            Assert.False(File.Exists(Path.Combine(_tempUploadsPath, file1)));
            Assert.True(File.Exists(Path.Combine(_tempUploadsPath, file2)));
            _output.WriteLine("✓ Assertions passed");

            // Verify logging
            AssertLogMessage(LogLevel.Information, "Cleanup completed successfully");
            
            _output.WriteLine("=== Test CleanupOrphanedFilesAsync_WithSubdirectories_IgnoresSubdirectories Completed ===\n");
        }

        public void Dispose()
        {
            _output.WriteLine("\n=== Starting CleanupServiceTests Cleanup ===");
            
            // Cleanup temp directory and dispose context
            if (Directory.Exists(_tempUploadsPath))
            {
                Directory.Delete(_tempUploadsPath, true);
                _output.WriteLine($"Deleted temp directory: {_tempUploadsPath}");
            }
            _context.Dispose();
            _output.WriteLine("Disposed database context");
            
            _output.WriteLine("=== CleanupServiceTests Cleanup Complete ===\n");
        }
    }
}
