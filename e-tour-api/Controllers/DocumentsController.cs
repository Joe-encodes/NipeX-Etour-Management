using e_tour_api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.IO;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Font;
namespace e_tour_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public DocumentsController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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
        // POST: api/documents/approve/{id}
        [HttpPost("approve/{id}")]
        [Authorize(Roles = "Approver")]
        public async Task<IActionResult> ApproveDocument(int id, [FromBody] ApprovalRequest request)
        {
            try
            {
                var document = await _context.Documents.FindAsync(id);
                if (document == null || document.Status != "Pending")
                    return NotFound("Document not found or already processed.");

                if (string.IsNullOrEmpty(request.Signature))
                    return BadRequest("Signature is required.");

                // Validate file existence and type
                if (string.IsNullOrEmpty(document.FilePath))
                    return BadRequest("Document file path is missing.");
                if (!System.IO.File.Exists(document.FilePath))
                    return BadRequest($"File not found at path: {document.FilePath}");
                if (!document.FilePath.ToLower().EndsWith(".pdf"))
                    return BadRequest("Only PDF files can be signed.");

                // Define the signed file path
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                var signedFileName = $"signed_{Path.GetFileName(document.FilePath)}";
                var signedFilePath = Path.Combine(uploadsFolder, signedFileName);

                // Log the file path for debugging
                Console.WriteLine($"Attempting to write signed PDF to: {signedFilePath}");

                // Check if the directory is writable
                try
                {
                    var directoryInfo = new DirectoryInfo(uploadsFolder);
                    if (!directoryInfo.Exists)
                    {
                        directoryInfo.Create();
                    }

                    // Test write access
                    var testFile = Path.Combine(uploadsFolder, "test.txt");
                    System.IO.File.WriteAllText(testFile, "test");
                    System.IO.File.Delete(testFile);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Failed to verify write access to uploads directory: {ex.Message}\nStackTrace: {ex.StackTrace}");
                }

                // Add signature to the PDF using iText7
                try
                {
                    using (var reader = new iText.Kernel.Pdf.PdfReader(document.FilePath))
                    using (var writer = new iText.Kernel.Pdf.PdfWriter(signedFilePath))
                    using (var pdf = new iText.Kernel.Pdf.PdfDocument(reader, writer))
                    {
                        var page = pdf.GetPage(1); // 1-based index in iText7
                        var canvas = new iText.Kernel.Pdf.Canvas.PdfCanvas(page);
                        canvas.BeginText();
                        canvas.SetFontAndSize(iText.Kernel.Font.PdfFontFactory.CreateFont(iText.Kernel.Font.StandardFonts.HELVETICA), 12);
                        canvas.MoveText(50, 50); // X, Y (from bottom-left)
                        canvas.ShowText($"Approved by: {request.Signature}");
                        canvas.EndText();
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"PDF processing error: {ex.GetType().FullName} - {ex.Message}\nStackTrace: {ex.StackTrace}");
                }

                // Update document record
                document.Status = "Signed";
                document.Signature = request.Signature;
                document.SignedFilePath = signedFilePath;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Document approved and signed successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        // GET: api/documents/download/{id}
        [HttpGet("download/{id}")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            try
            {
                var document = await _context.Documents.FindAsync(id);
                if (document == null || document.Status != "Signed")
                    return NotFound("Document not found or not signed.");

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (document.UserId != userId)
                    return Unauthorized("You can only download your own documents.");

                if (string.IsNullOrEmpty(document.SignedFilePath))
                    return BadRequest("Signed file path is not available.");

                var fileStream = System.IO.File.OpenRead(document.SignedFilePath);
                return File(fileStream, "application/pdf", document.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }

    public class ApprovalRequest
    {
        public string? Signature { get; set; }
    }
}