using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using e_tour_api.Controllers;
using e_tour_api.Models;
using e_tour_api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace e_tour_api.Tests.Tests
{
    public class MessagesControllerTests : TestLoggerBase
    {
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly ILogger<MessagesController> _logger;
        private readonly MessagesController _controller;

        public MessagesControllerTests()
        {
            _mockMessageService = new Mock<IMessageService>();
            _logger = new TestLogger<MessagesController>(LogMessages);
            _controller = new MessagesController(_mockMessageService.Object, _logger);

            // Setup test user claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Role, "User")
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [Fact]
        public async Task SendMessage_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new SendMessageRequest
            {
                RecipientId = 2,
                Subject = "Test Subject",
                Content = "Test Content"
            };

            _mockMessageService
                .Setup(x => x.SendMessageAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<MessageDto>.Ok(new MessageDto(), "Message sent successfully"));

            // Act
            var result = await _controller.SendMessage(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServiceResult<MessageDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Message sent successfully", response.Message);
            
            // Verify logging
            AssertLogMessage(LogLevel.Information, "Message sent");
        }

        [Fact]
        public async Task SendMessage_InvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var request = new SendMessageRequest
            {
                RecipientId = 2,
                Subject = "", // Invalid empty subject
                Content = "Test Content"
            };

            _mockMessageService
                .Setup(x => x.SendMessageAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<MessageDto>.Error("Subject cannot be empty", 400));

            // Act
            var result = await _controller.SendMessage(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ServiceResult<MessageDto>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Subject cannot be empty", response.ErrorMessage);
            
            // Verify logging
            AssertLogMessage(LogLevel.Warning, "Invalid message request");
        }

        [Fact]
        public async Task GetInbox_ReturnsOk()
        {
            // Arrange
            var messages = new List<MessageDto>
            {
                new MessageDto { Id = 1, Subject = "Test 1" },
                new MessageDto { Id = 2, Subject = "Test 2" }
            };

            _mockMessageService
                .Setup(x => x.GetInboxAsync(It.IsAny<int>()))
                .ReturnsAsync(ServiceResult<List<MessageDto>>.Ok(messages, "Inbox retrieved successfully"));

            // Act
            var result = await _controller.GetInbox();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServiceResult<List<MessageDto>>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(2, response.Data.Count);
            
            // Verify logging
            AssertLogMessage(LogLevel.Information, "Retrieved inbox");
        }

        [Fact]
        public async Task GetSentMessages_ReturnsOk()
        {
            // Arrange
            var messages = new List<MessageDto>
            {
                new MessageDto { Id = 1, Subject = "Sent 1" },
                new MessageDto { Id = 2, Subject = "Sent 2" }
            };

            _mockMessageService
                .Setup(x => x.GetSentMessagesAsync(It.IsAny<int>()))
                .ReturnsAsync(ServiceResult<List<MessageDto>>.Ok(messages, "Sent messages retrieved successfully"));

            // Act
            var result = await _controller.GetSentMessages();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServiceResult<List<MessageDto>>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(2, response.Data.Count);
        }

        [Fact]
        public async Task GetMessage_ValidId_ReturnsOk()
        {
            // Arrange
            var message = new MessageDto { Id = 1, Subject = "Test Message" };

            _mockMessageService
                .Setup(x => x.GetMessageAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(ServiceResult<MessageDto>.Ok(message, "Message retrieved successfully"));

            // Act
            var result = await _controller.GetMessage(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServiceResult<MessageDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(1, response.Data.Id);
        }

        [Fact]
        public async Task GetMessage_InvalidId_ReturnsNotFound()
        {
            // Arrange
            _mockMessageService
                .Setup(x => x.GetMessageAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(ServiceResult<MessageDto>.Error("Message not found", 404));

            // Act
            var result = await _controller.GetMessage(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ServiceResult<MessageDto>>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Message not found", response.ErrorMessage);
            
            // Verify logging
            AssertLogMessage(LogLevel.Warning, "Message not found");
        }

        [Fact]
        public async Task GetMessage_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var exception = new Exception("Database error");
            _mockMessageService
                .Setup(x => x.GetMessageAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _controller.GetMessage(1);

            // Assert
            var serverErrorResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverErrorResult.StatusCode);
            
            // Verify logging
            AssertLogMessage(LogLevel.Error, "Error retrieving message", exception);
        }

        [Fact]
        public async Task MarkAsRead_ValidId_ReturnsOk()
        {
            // Arrange
            _mockMessageService
                .Setup(x => x.MarkAsReadAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(ServiceResult.Ok());

            // Act
            var result = await _controller.MarkAsRead(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServiceResult>(okResult.Value);
            Assert.True(response.Success);
            
            // Verify logging
            AssertLogMessage(LogLevel.Information, "Message marked as read");
        }

        [Fact]
        public async Task DeleteMessage_ValidId_ReturnsOk()
        {
            // Arrange
            _mockMessageService
                .Setup(x => x.DeleteMessageAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(ServiceResult.Ok());

            // Act
            var result = await _controller.DeleteMessage(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServiceResult>(okResult.Value);
            Assert.True(response.Success);
            
            // Verify logging
            AssertLogMessage(LogLevel.Information, "Message deleted");
        }
    }
} 