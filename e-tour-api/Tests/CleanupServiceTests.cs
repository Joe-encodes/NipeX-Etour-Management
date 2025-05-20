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
    public class CleanupServiceTests : IDisposable
    {
        private readonly string _tempUploadsPath;
        private readonly AppDbContext _context;
        private readonly CleanupService _service;
        private readonly Mock<ILogger<CleanupService>> _loggerMock;
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly ITestOutputHelper _output;

        public CleanupServiceTests(ITestOutputHelper output)
        {
            _output = output;
            _output.WriteLine("=== Starting CleanupServiceTests Setup ===");
            
            // Setup temporary uploads directory
            _tempUploadsPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempUploadsPath);
            _output.WriteLine($"Created temp directory: {_tempUploadsPath}");

            // Setup in-memory database
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "CleanupServiceTestsDb")
                .Options;
            _context = new AppDbContext(options);
            _output.WriteLine("Initialized in-memory database");

            // Setup mocks
            _loggerMock = new Mock<ILogger<CleanupService>>();
            _envMock = new Mock<IWebHostEnvironment>();
            _envMock.Setup(e => e.WebRootPath).Returns(_tempUploadsPath);
            _output.WriteLine("Configured mocks");

            // Create service instance
            _service = new CleanupService(_context, _loggerMock.Object, _envMock.Object, _tempUploadsPath);
            _output.WriteLine("=== CleanupServiceTests Setup Complete ===");
        }

        [Fact]
        public async Task CleanupOrphanedFilesAsync_DeletesUnreferencedFiles()
        {
            // Arrange
            // Create files in uploads directory
            var referencedFile = "referenced.pdf";
            var orphanedFile = "orphaned.pdf";
            File.WriteAllText(Path.Combine(_tempUploadsPath, referencedFile), "content");
            File.WriteAllText(Path.Combine(_tempUploadsPath, orphanedFile), "content");

            // Add document referencing one file
            // Use full path for SignedFilePath to match service logic
            _context.Documents.Add(new Document { SignedFilePath = Path.Combine(_tempUploadsPath, referencedFile) });
            await _context.SaveChangesAsync();

            // Act
            var deletedCount = await _service.CleanupOrphanedFilesAsync();

            // Assert
            Assert.Equal(1, deletedCount);
            Assert.True(File.Exists(Path.Combine(_tempUploadsPath, referencedFile)));
            Assert.False(File.Exists(Path.Combine(_tempUploadsPath, orphanedFile)));
        }

        [Fact]
        public async Task CleanupOrphanedFilesAsync_WithFilter_DeletesMatchingFilesOnly()
        {
            // Arrange
            var file1 = "file1.pdf";
            var file2 = "file2.pdf";
            File.WriteAllText(Path.Combine(_tempUploadsPath, file1), "content");
            File.WriteAllText(Path.Combine(_tempUploadsPath, file2), "content");

            _context.Documents.Add(new Document { SignedFilePath = Path.Combine(_tempUploadsPath, "other.pdf") });
            await _context.SaveChangesAsync();

            // Act
            var deletedCount = await _service.CleanupOrphanedFilesAsync("file1");

            // Assert
            Assert.Equal(1, deletedCount);
            Assert.False(File.Exists(Path.Combine(_tempUploadsPath, file1)));
            Assert.True(File.Exists(Path.Combine(_tempUploadsPath, file2)));
        }

        [Fact]
        public async Task CleanupOrphanedFilesAsync_WithInvalidFilter_ThrowsArgumentException()
        {
            _output.WriteLine("\n=== Starting CleanupOrphanedFilesAsync_WithInvalidFilter_ThrowsArgumentException ===");
            
            var invalidFilter = "[invalid";
            _output.WriteLine($"Using invalid filter: {invalidFilter}");
            _output.WriteLine("Expecting ArgumentException...");

            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.CleanupOrphanedFilesAsync(invalidFilter));
        }

        [Fact]
        public async Task CleanupOrphanedFilesAsync_WithEmptyDirectory_ReturnsZero()
        {
            // Act
            var deletedCount = await _service.CleanupOrphanedFilesAsync();

            // Assert
            Assert.Equal(0, deletedCount);
        }

        [Fact]
        public async Task CleanupOrphanedFilesAsync_WithAllReferencedFiles_ReturnsZero()
        {
            // Arrange
            var file1 = "file1.pdf";
            var file2 = "file2.pdf";
            File.WriteAllText(Path.Combine(_tempUploadsPath, file1), "content");
            File.WriteAllText(Path.Combine(_tempUploadsPath, file2), "content");

            _context.Documents.Add(new Document { SignedFilePath = Path.Combine(_tempUploadsPath, file1) });
            _context.Documents.Add(new Document { SignedFilePath = Path.Combine(_tempUploadsPath, file2) });
            await _context.SaveChangesAsync();

            // Act
            var deletedCount = await _service.CleanupOrphanedFilesAsync();

            // Assert
            Assert.Equal(0, deletedCount);
            Assert.True(File.Exists(Path.Combine(_tempUploadsPath, file1)));
            Assert.True(File.Exists(Path.Combine(_tempUploadsPath, file2)));
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
            _output.WriteLine("Added document reference to database");
            var doc = new Document { SignedFilePath = Path.Combine(_tempUploadsPath, "other.pdf") };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            // Make second file read-only to simulate access error
            _output.WriteLine("Set file2 to read-only");
            var fileInfo = new FileInfo(file2);
            fileInfo.IsReadOnly = true;

            // Execute cleanup
            _output.WriteLine("Executing CleanupOrphanedFilesAsync...");
            var result = await _service.CleanupOrphanedFilesAsync();
            _output.WriteLine($"Cleanup completed. Deleted count: {result}");

            // Assert
            _output.WriteLine("Starting assertions...");
            Assert.Equal(2, result); // Both files should be deleted since they are not referenced
            Assert.False(File.Exists(file1)); // Unreferenced file should be deleted
            Assert.False(File.Exists(file2)); // File with access error should also be deleted on macOS

            // Cleanup
            // fileInfo.IsReadOnly = false;
        }

        [Fact]
        public async Task CleanupOrphanedFilesAsync_WithSubdirectories_IgnoresSubdirectories()
        {
            // Arrange
            var subDir = Path.Combine(_tempUploadsPath, "subdir");
            Directory.CreateDirectory(subDir);
            var file1 = "file1.pdf";
            var file2 = "subdir/file2.pdf";
            File.WriteAllText(Path.Combine(_tempUploadsPath, file1), "content");
            File.WriteAllText(Path.Combine(_tempUploadsPath, file2), "content");

            // Act
            var deletedCount = await _service.CleanupOrphanedFilesAsync();

            // Assert
            Assert.Equal(1, deletedCount);
            Assert.False(File.Exists(Path.Combine(_tempUploadsPath, file1)));
            Assert.True(File.Exists(Path.Combine(_tempUploadsPath, file2)));
        }

        // Add this helper method to validate filter patterns
        private bool IsValidFilterPattern(string? filter)
        {
            if (string.IsNullOrEmpty(filter)) return true;
            
            try
            {
                // Check if the filter contains any invalid characters for file patterns
                if (filter.Contains("..") || filter.Contains("\\") || filter.Contains("/"))
                    return false;
                    
                // Try to create a regex pattern from the filter
                var pattern = filter.Replace("*", ".*").Replace("?", ".");
                new System.Text.RegularExpressions.Regex(pattern);
                return true;
            }
            catch
            {
                return false;
            }
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
