//DocumentsController.cs
// <summary>
// This controller handles document upload, approval, and download functionalities.
// It includes methods for users to upload documents, approvers to approve them,
// and admins to view all documents and users.
// </summary>

using System;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using e_tour_api.Services; // For IDocumentService
using e_tour_api.Data; // For AppDbContext and Document
using e_tour_api.Models;
using e_tour_api.Models.DTOs;

namespace e_tour_api.Controllers
{
    /// <summary>
    /// Controller for managing documents including upload, approval, and download.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly ILogger<DocumentsController> _logger;
        private readonly IDocumentService _documentService;

        public DocumentsController(
            ILogger<DocumentsController> logger, 
            IDocumentService documentService)
        {
            _logger = logger;
            _documentService = documentService;
        }

        /// <summary>
        /// Gets documents uploaded by the authenticated user.
        /// </summary>
        /// <returns>List of user's documents</returns>
        [HttpGet]
        [Authorize(Roles = "User")]
        [ProducesResponseType(typeof(ServiceResult<List<DocumentDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserDocuments()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("Invalid user ID in token");
                    return Unauthorized(ServiceResult<object>.Error("Invalid user ID in token", 401));
                }

                var documents = await _documentService.GetUserDocumentsAsync(userId);
                _logger.LogInformation("Retrieved {Count} documents for user {UserId}", documents.Data?.Count ?? 0, userId);
                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents for user ID: {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, ServiceResult<object>.Error("An error occurred while retrieving documents", 500));
            }
        }

        /// <summary>
        /// Gets documents pending approval.
        /// </summary>
        /// <returns>List of pending documents</returns>
        [HttpGet("pending")]
        [Authorize(Roles = "Approver")]
        [ProducesResponseType(typeof(ServiceResult<List<DocumentDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPendingDocuments()
        {
            try
            {
                var documents = await _documentService.GetPendingDocumentsAsync();
                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending documents");
                return StatusCode(500, ServiceResult<object>.Error("An error occurred while retrieving pending documents", 500));
            }
        }

        /// <summary>
        /// Gets all documents (admin only).
        /// </summary>
        /// <returns>List of all documents</returns>
        [HttpGet("all-documents")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ServiceResult<List<DocumentDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllDocuments()
        {
            try
            {
                var documents = await _documentService.GetAllDocumentsAsync();
                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all documents");
                return StatusCode(500, ServiceResult<object>.Error("An error occurred while retrieving all documents", 500));
            }
        }

        /// <summary>
        /// Gets all users (admin only).
        /// </summary>
        /// <returns>List of all users</returns>
        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ServiceResult<List<UserProfileDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var result = await _documentService.GetAllUsersAsync();
                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all users");
                return StatusCode(500, ServiceResult<object>.Error("An error occurred while retrieving all users", 500));
            }
        }

        /// <summary>
        /// Uploads a document.
        /// </summary>
        /// <param name="file">PDF file to upload</param>
        /// <param name="title">Title of the document</param>
        /// <returns>Upload result message and document ID</returns>
        [HttpPost("upload")]
        [Authorize(Roles = "User")]
        [ProducesResponseType(typeof(ServiceResult<DocumentUploadResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadDocument([FromForm] IFormFile file, [FromForm] string title)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(ServiceResult<object>.Error("Invalid user ID in token", 401));
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(ServiceResult<object>.Error("No file uploaded", 400));
                }

                if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(ServiceResult<object>.Error("Only PDF files are allowed", 400));
                }

                var result = await _documentService.UploadDocumentAsync(userId, title, file);
                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, result);
                }

                var response = new DocumentUploadResponse
                {
                    DocumentId = result.Data?.Id ?? 0,
                    Message = "Document uploaded successfully"
                };

                return Ok(ServiceResult<DocumentUploadResponse>.Ok(response, "Document uploaded successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document");
                return StatusCode(500, ServiceResult<object>.Error("An error occurred while uploading the document", 500));
            }
        }

        /// <summary>
        /// Approves a document
        /// </summary>
        /// <param name="documentId">The ID of the document to approve</param>
        /// <param name="request">The approval request containing signature and password</param>
        /// <returns>The approved document</returns>
        [HttpPost("{documentId}/approve")]
        [Authorize(Roles = "Approver")]
        [ProducesResponseType(typeof(ServiceResult<DocumentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ApproveDocument(int documentId, [FromBody] ApprovalRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(ServiceResult<object>.Error("Invalid user ID in token", 401));
                }

                var result = await _documentService.ApproveDocumentAsync(documentId, userId, request.Signature, request.Password);
                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving document {DocumentId}", documentId);
                return StatusCode(500, ServiceResult<object>.Error("An error occurred while approving the document", 500));
            }
        }

        /// <summary>
        /// Downloads a document
        /// </summary>
        /// <param name="documentId">The ID of the document to download</param>
        /// <returns>The document file</returns>
        [HttpGet("{documentId}/download")]
        [Authorize(Roles = "User")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DownloadDocument(int documentId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Unauthorized(ServiceResult<object>.Error("Invalid user ID in token", 401));

                var accessResult = await _documentService.VerifyDocumentAccess(documentId, userId);
                if (!accessResult.Success)
                    return StatusCode(accessResult.StatusCode, ServiceResult<object>.Error(accessResult.Message ?? "Access denied", accessResult.StatusCode));

                var result = await _documentService.GetDocumentFile(documentId);
                if (!result.Success)
                    return StatusCode(result.StatusCode, ServiceResult<object>.Error(result.Message ?? "Failed to retrieve document", result.StatusCode));

                if (result.Data == null)
                    return NotFound(ServiceResult<object>.Error("File not found", 404));

                return File(result.Data.Contents, "application/pdf", result.Data.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document {DocumentId}", documentId);
                return StatusCode(500, ServiceResult<object>.Error("An error occurred while downloading the document", 500));
            }
        }

        /// <summary>
        /// Approval request model
        /// </summary>
        public class ApprovalRequest
        {
            /// <example>John Doe</example>
            public string? Signature { get; set; }

            /// <example>password123</example>
            public string? Password { get; set; }
        }
    }
}
