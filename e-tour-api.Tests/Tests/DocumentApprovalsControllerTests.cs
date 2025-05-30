using System;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using e_tour_api.Controllers;
using e_tour_api.Services;
using e_tour_api.Models.DTOs;

namespace e_tour_api.Tests.Tests
{
    public class DocumentApprovalsControllerTests : IDisposable
    {
        private readonly Mock<IDocumentService> _mockDocumentService;
        private readonly Mock<ILogger<DocumentApprovalsController>> _mockLogger;
        private readonly DocumentApprovalsController _controller;
        private readonly ClaimsPrincipal _approver;

        public DocumentApprovalsControllerTests()
        {
            _mockDocumentService = new Mock<IDocumentService>();
            _mockLogger = new Mock<ILogger<DocumentApprovalsController>>();
            _controller = new DocumentApprovalsController(_mockDocumentService.Object, _mockLogger.Object);

            // Setup test approver
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Approver")
            };
            _approver = new ClaimsPrincipal(new ClaimsIdentity(claims));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = _approver }
            };
        }

        [Fact]
        public async Task AssignApprover_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new AssignApproverRequest
            {
                ApproverId = 2
            };

            var expectedDocument = new DocumentDto
            {
                Id = 1,
                Status = DocumentStatus.Pending,
                AssignedApproverId = 2
            };

            _mockDocumentService
                .Setup(x => x.AssignApproverAsync(1, request.ApproverId))
                .ReturnsAsync(ServiceResult<DocumentDto>.Ok(expectedDocument));

            // Act
            var result = await _controller.AssignApprover(1, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServiceResult<DocumentDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(expectedDocument.AssignedApproverId, response.Data.AssignedApproverId);
        }

        [Fact]
        public async Task AssignApprover_InvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var request = new AssignApproverRequest
            {
                ApproverId = -1 // Invalid approver ID
            };

            _mockDocumentService
                .Setup(x => x.AssignApproverAsync(1, request.ApproverId))
                .ReturnsAsync(ServiceResult<DocumentDto>.Error("Invalid approver ID", 400));

            // Act
            var result = await _controller.AssignApprover(1, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ServiceResult<DocumentDto>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal(400, response.StatusCode);
        }

        [Fact]
        public async Task ApproveDocument_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new ApproveDocumentRequest
            {
                Signature = "John Doe",
                Password = "password123"
            };

            var expectedDocument = new DocumentDto
            {
                Id = 1,
                Status = DocumentStatus.Approved,
                Signature = "John Doe",
                SignedFilePath = "/uploads/signed/doc1.pdf"
            };

            _mockDocumentService
                .Setup(x => x.ApproveDocumentAsync(1, 1, request.Signature, request.Password))
                .ReturnsAsync(ServiceResult<DocumentDto>.Ok(expectedDocument));

            // Act
            var result = await _controller.ApproveDocument(1, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServiceResult<DocumentDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(DocumentStatus.Approved, response.Data.Status);
            Assert.Equal(expectedDocument.Signature, response.Data.Signature);
        }

        [Fact]
        public async Task ApproveDocument_InvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var request = new ApproveDocumentRequest
            {
                Signature = "", // Invalid empty signature
                Password = "password123"
            };

            _mockDocumentService
                .Setup(x => x.ApproveDocumentAsync(1, 1, request.Signature, request.Password))
                .ReturnsAsync(ServiceResult<DocumentDto>.Error("Signature is required", 400));

            // Act
            var result = await _controller.ApproveDocument(1, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ServiceResult<DocumentDto>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal(400, response.StatusCode);
        }

        [Fact]
        public async Task RejectDocument_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new RejectDocumentRequest
            {
                Reason = "Document needs revision"
            };

            var expectedDocument = new DocumentDto
            {
                Id = 1,
                Status = DocumentStatus.Rejected,
                RejectionReason = "Document needs revision"
            };

            _mockDocumentService
                .Setup(x => x.RejectDocumentAsync(1, 1, request.Reason))
                .ReturnsAsync(ServiceResult<DocumentDto>.Ok(expectedDocument));

            // Act
            var result = await _controller.RejectDocument(1, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServiceResult<DocumentDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(DocumentStatus.Rejected, response.Data.Status);
            Assert.Equal(expectedDocument.RejectionReason, response.Data.RejectionReason);
        }

        [Fact]
        public async Task RejectDocument_InvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var request = new RejectDocumentRequest
            {
                Reason = "" // Invalid empty reason
            };

            _mockDocumentService
                .Setup(x => x.RejectDocumentAsync(1, 1, request.Reason))
                .ReturnsAsync(ServiceResult<DocumentDto>.Error("Rejection reason is required", 400));

            // Act
            var result = await _controller.RejectDocument(1, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ServiceResult<DocumentDto>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal(400, response.StatusCode);
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
} 