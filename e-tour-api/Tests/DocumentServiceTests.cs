using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using e_tour_api.Data;
using e_tour_api.Services;
using Xunit.Abstractions;

namespace e_tour_api.Tests
{
    public class DocumentServiceTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task GetDocumentById_ReturnsDocument_WhenExists()
        {
            var context = GetInMemoryDbContext();
            var stampServiceMock = new Mock<IPdfStampService>();
            var loggerMock = new Mock<ILogger<DocumentService>>();

            var document = new Document { Id = 1, FileName = "TestDoc" };
            context.Documents.Add(document);
            await context.SaveChangesAsync();

            var service = new DocumentService(context, stampServiceMock.Object, loggerMock.Object);
            var result = await service.GetDocumentById(1);

            Assert.NotNull(result);
            Assert.Equal("TestDoc", result.FileName);
        }

        [Fact]
        public async Task VerifyDocumentAccess_ReturnsTrue_WhenUserHasAccess()
        {
            var context = GetInMemoryDbContext();
            var stampServiceMock = new Mock<IPdfStampService>();
            var loggerMock = new Mock<ILogger<DocumentService>>();

            var document = new Document { Id = 1, UserId = 1, Status = "Signed" };
            context.Documents.Add(document);
            await context.SaveChangesAsync();

            var service = new DocumentService(context, stampServiceMock.Object, loggerMock.Object);
            var result = await service.VerifyDocumentAccess(1, 1);

            Assert.True(result.HasAccess);
        }

        [Fact]
        public async Task VerifyDocumentAccess_ReturnsFalse_WhenUserHasNoAccess()
        {
            var context = GetInMemoryDbContext();
            var stampServiceMock = new Mock<IPdfStampService>();
            var loggerMock = new Mock<ILogger<DocumentService>>();

            var document = new Document { Id = 1, UserId = 2, Status = "Signed" };
            context.Documents.Add(document);
            await context.SaveChangesAsync();

            var service = new DocumentService(context, stampServiceMock.Object, loggerMock.Object);
            var result = await service.VerifyDocumentAccess(1, 1);

            Assert.False(result.HasAccess);
        }

        [Fact]
        public async Task StampDocument_ReturnsSuccess_WhenStampingSucceeds()
        {
            var context = GetInMemoryDbContext();
            var stampServiceMock = new Mock<IPdfStampService>();
            var loggerMock = new Mock<ILogger<DocumentService>>();

            var document = new Document { Id = 1, FilePath = "source.pdf", Status = "Pending" };
            context.Documents.Add(document);
            await context.SaveChangesAsync();

            stampServiceMock.Setup(s => s.StampAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("signed.pdf");

            var service = new DocumentService(context, stampServiceMock.Object, loggerMock.Object);
            var result = await service.StampDocument(1, "Approved", "uploads");

            Assert.True(result.Success);
            Assert.Equal("signed.pdf", result.SignedFilePath);
        }

        [Fact]
        public async Task GetDocumentFile_ReturnsFile_WhenDocumentExists()
        {
            var context = GetInMemoryDbContext();
            var stampServiceMock = new Mock<IPdfStampService>();
            var loggerMock = new Mock<ILogger<DocumentService>>();

            var document = new Document { Id = 1, SignedFilePath = "signed.pdf", FileName = "doc.pdf" };
            context.Documents.Add(document);
            await context.SaveChangesAsync();

            // Create dummy file
            System.IO.File.WriteAllText("signed.pdf", "dummy content");

            var service = new DocumentService(context, stampServiceMock.Object, loggerMock.Object);
            var result = await service.GetDocumentFile(1);

            Assert.True(result.Success);
            Assert.Equal("doc.pdf", result.FileName);

            // Cleanup
            System.IO.File.Delete("signed.pdf");
        }
    }
}
