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

        // GET: api/documents
        [HttpGet]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> GetUserDocuments()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Unauthorized("Invalid user ID in token.");

                var documents = await _context.Documents
                    .Where(d => d.UserId == userId)
                    .Select(d => new
                    {
                        d.Id,
                        d.FileName,
                        d.FilePath,
                        d.Status,
                        d.Signature,
                        d.SignedFilePath
                    })
                    .ToListAsync();

                return Ok(documents);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/documents/pending
        [HttpGet("pending")]
        [Authorize(Roles = "Approver")]
        public async Task<IActionResult> GetPendingDocuments()
        {
            try
            {
                var documents = await _context.Documents
                    .Where(d => d.Status == "Pending")
                    .Select(d => new
                    {
                        d.Id,
                        d.FileName,
                        d.FilePath,
                        d.Status,
                        d.Signature,
                        d.SignedFilePath
                    })
                    .ToListAsync();
                return Ok(documents);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/documents/all-documents
        [HttpGet("all-documents")]
        [Authorize(Roles = "Admin")]
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

        // GET: api/documents/users
        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
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

        // POST: api/documents/upload
        [HttpPost("upload")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> UploadDocument([FromForm] IFormFile file, [FromForm] string title)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded.");
                if (string.IsNullOrEmpty(title))
                    return BadRequest("Document title is required.");

                // Validate file type
                if (!file.FileName.ToLower().EndsWith(".pdf"))
                    return BadRequest("Only PDF files are allowed.");

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Unauthorized("Invalid user ID in token.");

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
                    FilePath = filePath,
                    Status = "Pending",
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
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

       // POST: api/documents/approve/{id}
        [HttpPost("approve/{id}")]
        [Authorize(Roles = "Approver")]
        public async Task<IActionResult> ApproveDocument(int id, [FromBody] ApprovalRequest req)
        {
            try {
                var document = await _documentService.GetDocumentById(id);
                if (document == null)
                    return NotFound();

                // Id is plain int, no HasValue needed
                int docId = document.Id;

                var result = await _documentService.StampDocument(
                    documentId: docId,
                    stampText: $"Approved by {req.Signature ?? "Unknown"} on {DateTime.UtcNow:yyyy-MM-dd}",
                    uploadsPath: Path.Combine(_environment.WebRootPath, "uploads")
                );

                if (!result.Success)
                    return BadRequest(result.ErrorMessage);

                return Ok(new {
                    message = "Stamped successfully",
                    path = Path.GetFileName(result.SignedFilePath!)
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

        // GET: api/documents/download/{id}
        [HttpGet("download/{id}")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            try
            {
                // Verify permissions through service
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Unauthorized("Invalid user ID");

                // Now use 'userId' (non-nullable int) in your service calls:
                var accessCheck = await _documentService.VerifyDocumentAccess(id, userId);              
                if (!accessCheck.HasAccess)
                    return StatusCode(accessCheck.ErrorCode ?? 500, accessCheck.ErrorMessage);

                // Get file through service
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
    public class ApprovalRequest
    {
        public string? Signature { get; set; }
    }
}