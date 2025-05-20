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

namespace e_tour_api.Controllers
{
    /// <summary>
    /// Controller for managing documents including upload, approval, and download.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IPdfStampService _stampService;
        private readonly ILogger<DocumentsController> _logger;
        private readonly IDocumentService _documentService;


        public DocumentsController(AppDbContext context, IWebHostEnvironment environment, IPdfStampService stampService, ILogger<DocumentsController> logger, IDocumentService documentService)
        {
            _stampService = stampService;
            _environment = environment;
            _context = context;
            _logger = logger;
            _documentService = documentService;
        }

        /// <summary>
        /// Gets documents uploaded by the authenticated user.
        /// </summary>
        /// <returns>List of user's documents</returns>
        [HttpGet]
        [Authorize(Roles = "User")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserDocuments()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Unauthorized(new ProblemDetails { Detail = "Invalid user ID in token." });

                var documents = await _documentService.GetUserDocumentsAsync(userId);
                return Ok(documents);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ProblemDetails { Detail = $"Internal server error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Gets documents pending approval.
        /// </summary>
        /// <returns>List of pending documents</returns>
        [HttpGet("pending")]
        [Authorize(Roles = "Approver")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPendingDocuments()
        {
            try
            {
                var documents = await _documentService.GetPendingDocumentsAsync();
                return Ok(documents);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ProblemDetails { Detail = $"Internal server error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Gets all documents (admin only).
        /// </summary>
        /// <returns>List of all documents</returns>
        [HttpGet("all-documents")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllDocuments()
        {
            try
            {
                var documents = await _context.Documents
                    .Select(d => new
                    {
                        d.Id,
                        d.FileName,
                        d.FilePath,
                        d.Status,
                        d.Signature,
                        d.SignedFilePath,
                        d.UserId,
                        d.UploadedAt
                    })
                    .ToListAsync();
                return Ok(documents);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Gets all users (admin only).
        /// </summary>
        /// <returns>List of all users</returns>
        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _context.Users
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.Role
                    })
                    .ToListAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}\nStackTrace: {ex.StackTrace}");
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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadDocument([FromForm] IFormFile file, [FromForm] string title)
        {
            try
            {
                const long maxFileSize = 10 * 1024 * 1024; // 10MB
                if (file == null || file.Length == 0)
                    return BadRequest(new ProblemDetails { Detail = "No file uploaded." });
                if (string.IsNullOrEmpty(title))
                    return BadRequest(new ProblemDetails { Detail = "Document title is required." });
                if (file.Length > maxFileSize)
                    return BadRequest(new ProblemDetails { Detail = "File size exceeds the maximum allowed size (10MB)." });

                // Validate file type
                if (!file.FileName.ToLower().EndsWith(".pdf"))
                    return BadRequest(new ProblemDetails { Detail = "Only PDF files are allowed." });

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Unauthorized(new ProblemDetails { Detail = "Invalid user ID in token." });

                // Check for duplicate title for this user
                var duplicate = await _context.Documents.AnyAsync(d => d.UserId == userId && d.FileName == title);
                if (duplicate)
                    return BadRequest(new ProblemDetails { Detail = "A document with this title already exists" });

                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!System.IO.Directory.Exists(uploadsFolder))
                    System.IO.Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid().ToString() + ".pdf";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var document = new e_tour_api.Data.Document
                {
                    FileName = title,
                    FilePath = fileName,
                    Status = DocumentStatus.Pending,
                    Signature = null,
                    UserId = userId,
                    UploadedAt = DateTime.UtcNow,
                    SignedFilePath = null
                };

                _context.Documents.Add(document);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Document uploaded successfully", documentId = document.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ProblemDetails { Detail = $"Internal server error: {ex.Message}" });
            }
        }

       /// <summary>
       /// Approves a document by stamping it.
       /// </summary>
       /// <param name="id">Document ID</param>
       /// <param name="req">Approval request containing signature and password</param>
       /// <returns>Approval result message and signed file path</returns>
        [HttpPost("approve/{id}")]
        [Authorize(Policy = "Approver")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ApproveDocument(int id, [FromBody] ApprovalRequest req)
        {
            try {
                var document = await _documentService.GetDocumentById(id);
                if (document == null)
                    return NotFound();

                var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads");
                if (!System.IO.Directory.Exists(uploadsDir))
                    System.IO.Directory.CreateDirectory(uploadsDir);

                var result = await _documentService.StampDocument(
                    documentId: document.Id,
                    stampText: $"Approved by {req.Signature ?? "Unknown"} on {DateTime.UtcNow:yyyy-MM-dd}",
                    uploadsPath: uploadsDir,
                    password: req.Password
                );

                if (!result.Success)
                    return BadRequest(result.ErrorMessage);

                document.SignedFilePath = result.SignedFilePath;
                document.Status = DocumentStatus.Signed;
                await _documentService.UpdateDocument(document);

                return Ok(new {
                    message = "Stamped successfully",
                    path = result.SignedFilePath
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving document {DocumentId}", id);
                return StatusCode(500, new {
                    error = "Failed to approve document",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Downloads a document file.
        /// </summary>
        /// <param name="id">Document ID</param>
        /// <returns>PDF file stream</returns>
        [HttpGet("download/{id}")]
        [Authorize(Roles = "User")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Unauthorized("Invalid user ID");

                var accessCheck = await _documentService.VerifyDocumentAccess(id, userId);              
                if (!accessCheck.HasAccess)
                    return StatusCode(accessCheck.ErrorCode ?? 500, accessCheck.ErrorMessage);

                var fileResult = await _documentService.GetDocumentFile(id);
                if (!fileResult.Success)
                    return StatusCode(fileResult.ErrorCode ?? 500, fileResult.ErrorMessage);

                if (fileResult.FileContents == null)
                    return StatusCode(404, "File not found");
                return File(fileResult.FileContents, "application/pdf", fileResult.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document {DocumentId}", id);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
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
