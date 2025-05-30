// Services/DocumentService.cs
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using e_tour_api.Data;  // For AppDbContext
using e_tour_api.Services; // For IPdfStampService
using e_tour_api.Models;
using e_tour_api.Models.DTOs; // For DocumentDto and UserProfileDto
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using DocumentApprovalDto = e_tour_api.Models.DTOs.DocumentApprovalDto;
using Document = e_tour_api.Models.Document;

namespace e_tour_api.Services
{
	/// <summary>
	/// Service for managing document operations including retrieval, access verification, and PDF stamping.
	/// </summary>
	public class DocumentService : IDocumentService
	{
		private readonly AppDbContext _context;
		private readonly IPdfStampService _stampService;
		private readonly ILogger<DocumentService> _logger;
		private readonly IWebHostEnvironment _environment;
		private readonly IMemoryCache _cache;
		private const string DocumentCacheKeyPrefix = "Document_";
		private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

		/// <summary>
		/// Initializes a new instance of the DocumentService.
		/// </summary>
		/// <param name="context">Database context for document operations</param>
		/// <param name="stampService">Service for PDF stamping operations</param>
		/// <param name="logger">Logger for tracking operations</param>
		/// <param name="environment">Web hosting environment for file operations</param>
		/// <param name="cache">Memory cache for caching document operations</param>
		public DocumentService(
			AppDbContext context, 
			IPdfStampService stampService,
			ILogger<DocumentService> logger,
			IWebHostEnvironment environment,
			IMemoryCache cache)
		{
			_context = context;
			_stampService = stampService;
			_logger = logger;
			_environment = environment;
			_cache = cache;
		}

		/// <summary>
		/// Retrieves a document by its ID with caching.
		/// </summary>
		/// <param name="documentId">The ID of the document to retrieve</param>
		/// <param name="userId">The ID of the user requesting the document</param>
		/// <returns>The document if found, otherwise null</returns>
		public async Task<ServiceResult<DocumentDto>> GetDocumentWithCacheAsync(int documentId, int userId)
		{
			try
			{
				var cacheKey = $"{DocumentCacheKeyPrefix}{documentId}";

				if (_cache.TryGetValue(cacheKey, out DocumentDto? cachedDocument))
				{
					_logger.LogInformation("Cache hit for document {DocumentId}", documentId);
					return ServiceResult<DocumentDto>.Ok(cachedDocument!);
				}

				var document = await GetDocumentById(documentId);
				if (document == null)
				{
					_logger.LogWarning("Document not found: {DocumentId}", documentId);
					return ServiceResult<DocumentDto>.Error("Document not found", 404);
				}

				// Verify user has access to the document
				if (document.UserId != userId)
				{
					_logger.LogWarning("Unauthorized access attempt: User {UserId} tried to access document {DocumentId}", userId, documentId);
					return ServiceResult<DocumentDto>.Error("You do not have access to this document", 403);
				}

				var documentDto = MapToDto(document);
				var cacheOptions = new MemoryCacheEntryOptions()
					.SetAbsoluteExpiration(CacheDuration)
					.SetSlidingExpiration(TimeSpan.FromMinutes(10));

				_cache.Set(cacheKey, documentDto, cacheOptions);
				_logger.LogInformation("Document cached: {DocumentId}", documentId);

				return ServiceResult<DocumentDto>.Ok(documentDto);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving document {DocumentId}", documentId);
				return ServiceResult<DocumentDto>.Error("An error occurred while retrieving the document", 500);
			}
		}

		/// <summary>
		/// Invalidates the cache for a specific document.
		/// </summary>
		/// <param name="documentId">The ID of the document to invalidate</param>
		public Task InvalidateDocumentCacheAsync(int documentId)
		{
			var cacheKey = $"{DocumentCacheKeyPrefix}{documentId}";
			_cache.Remove(cacheKey);
			_logger.LogInformation("Invalidated cache for document {DocumentId}", documentId);
			return Task.CompletedTask;
		}

		/// <summary>
		/// Retrieves a document by its ID.
		/// </summary>
		/// <param name="documentId">The ID of the document to retrieve</param>
		/// <returns>The document if found, otherwise null</returns>
		public async Task<Document?> GetDocumentById(int documentId)
		{
			return await _context.Documents.FindAsync(documentId);
		}

		/// <summary>
		/// Verifies if a user has access to a specific document.
		/// </summary>
		/// <param name="documentId">The ID of the document to verify access for</param>
		/// <param name="userId">The ID of the user requesting access</param>
		/// <returns>Result indicating whether access is granted and any error messages</returns>
		public async Task<ServiceResult> VerifyDocumentAccess(int documentId, int userId)
		{
			try
		{
			var document = await GetDocumentById(documentId);
			
				if (document == null)
				{
					_logger.LogWarning("Document not found: {DocumentId}", documentId);
					return ServiceResult.Error("Document not found", 404);
				}
				
				if (document.Status != DocumentStatus.Signed)
				{
					_logger.LogWarning("Document not signed: {DocumentId}", documentId);
					return ServiceResult.Error("Document is not signed", 400);
				}
			
			if (document.UserId != userId)
				{
					_logger.LogWarning("Unauthorized access attempt: User {UserId} tried to access document {DocumentId}", userId, documentId);
					return ServiceResult.Error("You can only download your own documents", 403);
				}
				
				_logger.LogInformation("Document access verified: User {UserId} accessed document {DocumentId}", userId, documentId);
				return ServiceResult.Ok();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error verifying document access for document {DocumentId} and user {UserId}", documentId, userId);
				return ServiceResult.Error("An error occurred while verifying document access", 500);
			}
		}

		/// <summary>
		/// Stamps a document with specified text.
		/// </summary>
		/// <param name="documentId">The ID of the document to stamp</param>
		/// <param name="stampText">The text to stamp on the document</param>
		/// <param name="uploadsPath">The path where uploaded files are stored</param>
		/// <param name="password">Optional password for the PDF file</param>
		/// <returns>Result of the stamping operation including the path to the stamped file</returns>
		public async Task<ServiceResult<string>> StampDocument(int documentId, string stampText, string uploadsPath, string? password = null)
		{
			try
			{
				var document = await GetDocumentById(documentId);
				if (document == null)
				{
					_logger.LogWarning("Document not found for stamping: {DocumentId}", documentId);
					return ServiceResult<string>.Error("Document not found", 404);
				}
				
				if (!Directory.Exists(uploadsPath))
				{
					_logger.LogInformation("Creating uploads directory: {Path}", uploadsPath);
					Directory.CreateDirectory(uploadsPath);
				}

				if (string.IsNullOrEmpty(document.FilePath) || !document.FilePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
				{
					_logger.LogWarning("Invalid file type for document {DocumentId}: {FilePath}", documentId, document.FilePath);
					return ServiceResult<string>.Error("Invalid file type", 400);
				}

				var sourceFilePath = Path.Combine(uploadsPath, document.FilePath);
				if (!File.Exists(sourceFilePath))
				{
					_logger.LogWarning("Source file not found: {FilePath}", sourceFilePath);
					return ServiceResult<string>.Error("Source file not found", 404);
				}

				var randomFileName = Path.GetRandomFileName();
				var signedName = Path.ChangeExtension(randomFileName, ".pdf");
				var signedPath = Path.Combine(uploadsPath, signedName);

				_logger.LogInformation("Stamping document {DocumentId} with text: {StampText}", documentId, stampText);
				var resultPath = await _stampService.StampAsync(sourceFilePath, signedPath, stampText, password);

				document.Status = DocumentStatus.Signed;
				document.SignedFilePath = signedName;
				await _context.SaveChangesAsync();

				_logger.LogInformation("Document {DocumentId} successfully stamped", documentId);
				return ServiceResult<string>.Ok(resultPath);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error stamping document {DocumentId}", documentId);
				return ServiceResult<string>.Error("An error occurred while stamping the document", 500);
			}
		}

		/// <summary>
		/// Retrieves the file content of a document.
		/// </summary>
		/// <param name="documentId">The ID of the document to retrieve</param>
		/// <returns>File result containing the document contents and metadata</returns>
		public async Task<ServiceResult<DocumentFile>> GetDocumentFile(int documentId)
		{
			try
			{
				var document = await _context.Documents.FindAsync(documentId);
				if (document == null)
					return ServiceResult<DocumentFile>.Error("Document not found", 404);

				var filePath = Path.Combine(_environment.WebRootPath, "uploads", document.FilePath ?? string.Empty);
				if (!File.Exists(filePath))
					return ServiceResult<DocumentFile>.Error("File not found", 404);

				var fileBytes = await File.ReadAllBytesAsync(filePath);
				return ServiceResult<DocumentFile>.Ok(new DocumentFile
				{
					FileName = document.FileName ?? string.Empty,
					ContentType = "application/pdf",
					Contents = fileBytes
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving document file {DocumentId}", documentId);
				return ServiceResult<DocumentFile>.Error("An error occurred while retrieving the document file", 500);
			}
		}

		/// <summary>
		/// Updates a document in the database.
		/// </summary>
		/// <param name="document">The document to update</param>
		public async Task<ServiceResult> UpdateDocument(Document document)
		{
			try
			{
				_context.Documents.Update(document);
				await _context.SaveChangesAsync();
				await InvalidateDocumentCacheAsync(document.Id);
				return ServiceResult.Ok();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating document {DocumentId}", document.Id);
				return ServiceResult.Error("An error occurred while updating the document", 500);
			}
		}

		/// <summary>
		/// Retrieves all documents for a specific user.
		/// </summary>
		/// <param name="userId">The ID of the user</param>
		/// <returns>List of user's documents</returns>
		public async Task<ServiceResult<List<DocumentDto>>> GetUserDocumentsAsync(int userId)
		{
			try
			{
				var documents = await _context.Documents
				.Where(d => d.UserId == userId)
					.Select(d => MapToDto(d))
				.ToListAsync();

				return ServiceResult<List<DocumentDto>>.Ok(documents);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving documents for user {UserId}", userId);
				return ServiceResult<List<DocumentDto>>.Error("An error occurred while retrieving documents", 500);
			}
		}

		/// <summary>
		/// Retrieves all pending documents.
		/// </summary>
		/// <returns>List of pending documents</returns>
		public async Task<ServiceResult<List<DocumentDto>>> GetPendingDocumentsAsync()
		{
			try
			{
				var documents = await _context.Documents
				.Where(d => d.Status == DocumentStatus.Pending)
					.Select(d => MapToDto(d))
				.ToListAsync();

				return ServiceResult<List<DocumentDto>>.Ok(documents);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving pending documents");
				return ServiceResult<List<DocumentDto>>.Error("An error occurred while retrieving pending documents", 500);
		}
	}

	/// <summary>
		/// Retrieves all documents.
	/// </summary>
		/// <returns>List of all documents</returns>
		public async Task<ServiceResult<List<DocumentDto>>> GetAllDocumentsAsync()
		{
			try
			{
				var documents = await _context.Documents
					.Select(d => MapToDto(d))
					.ToListAsync();

				return ServiceResult<List<DocumentDto>>.Ok(documents);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving all documents");
				return ServiceResult<List<DocumentDto>>.Error("An error occurred while retrieving documents", 500);
			}
		}

	/// <summary>
		/// Uploads a document for a specific user.
	/// </summary>
		/// <param name="userId">The ID of the user</param>
		/// <param name="title">The title of the document</param>
		/// <param name="file">The file to upload</param>
		/// <returns>The uploaded document</returns>
		public async Task<ServiceResult<DocumentDto>> UploadDocumentAsync(int userId, string title, IFormFile file)
		{
			try
			{
				if (file == null || file.Length == 0)
					return ServiceResult<DocumentDto>.Error("No file uploaded", 400);

				if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
					return ServiceResult<DocumentDto>.Error("Only PDF files are allowed", 400);

				var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads");
				if (!Directory.Exists(uploadsDir))
					Directory.CreateDirectory(uploadsDir);

				var fileName = $"{Guid.NewGuid()}.pdf";
				var filePath = Path.Combine(uploadsDir, fileName);

				using (var stream = new FileStream(filePath, FileMode.Create))
				{
					await file.CopyToAsync(stream);
				}

				var document = new Document
				{
					FileName = title,
					FilePath = fileName,
					Status = DocumentStatus.Pending,
					UserId = userId,
					UploadedAt = DateTime.UtcNow
				};

				_context.Documents.Add(document);
				await _context.SaveChangesAsync();

				return ServiceResult<DocumentDto>.Ok(MapToDto(document));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error uploading document for user {UserId}", userId);
				return ServiceResult<DocumentDto>.Error("An error occurred while uploading the document", 500);
			}
		}

	/// <summary>
		/// Retrieves all users.
	/// </summary>
		/// <returns>List of all users</returns>
		public async Task<ServiceResult<List<UserProfileDto>>> GetAllUsersAsync()
		{
			try
			{
				var users = await _context.Users
					.Select(u => new UserProfileDto
					{
						Id = u.Id,
						Username = u.Username,
						Email = u.Email,
						Role = u.Role,
						CreatedAt = u.CreatedAt,
						LastLoginAt = u.LastLoginAt
					})
					.ToListAsync();

				return ServiceResult<List<UserProfileDto>>.Ok(users);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving all users");
				return ServiceResult<List<UserProfileDto>>.Error("An error occurred while retrieving users", 500);
			}
		}

		public async Task<ServiceResult> DeleteDocumentAsync(int documentId)
		{
			try
			{
				var document = await _context.Documents.FindAsync(documentId);
				if (document == null)
					return ServiceResult.Error("Document not found", 404);

				var filePath = Path.Combine(_environment.WebRootPath, "uploads", document.FilePath ?? string.Empty);
				if (File.Exists(filePath))
					File.Delete(filePath);

				_context.Documents.Remove(document);
				await _context.SaveChangesAsync();

				return ServiceResult.Ok();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
				return ServiceResult.Error("An error occurred while deleting the document", 500);
			}
		}

		public async Task<ServiceResult<DocumentApprovalDto>> AssignApproverAsync(int documentId, int approverId, float signaturePositionX, float signaturePositionY, int signaturePage, string? comments)
		{
			try
			{
				var document = await _context.Documents.FindAsync(documentId);
				if (document == null)
					return ServiceResult<DocumentApprovalDto>.Error("Document not found", 404);

				var approver = await _context.Users.FindAsync(approverId);
				if (approver == null)
					return ServiceResult<DocumentApprovalDto>.Error("Approver not found", 404);

				var approval = new DocumentApproval
				{
					DocumentId = documentId,
					ApproverId = approverId,
					SignaturePositionX = (int)signaturePositionX,
					SignaturePositionY = (int)signaturePositionY,
					SignaturePage = signaturePage,
					Comments = comments,
					Status = DocumentApprovalStatus.Pending,
					AssignedAt = DateTime.UtcNow
				};

				_context.DocumentApprovals.Add(approval);
				await _context.SaveChangesAsync();

				var dto = new DocumentApprovalDto
				{
					Id = approval.Id,
					DocumentId = approval.DocumentId,
					ApproverId = approval.ApproverId,
					ApproverName = approver.Username,
					SignaturePositionX = signaturePositionX,
					SignaturePositionY = signaturePositionY,
					SignaturePage = signaturePage,
					Comments = comments,
					AssignedAt = approval.AssignedAt,
					ApprovedAt = approval.ApprovedAt
				};

				return ServiceResult<DocumentApprovalDto>.Ok(dto);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error assigning approver for document {DocumentId}", documentId);
				return ServiceResult<DocumentApprovalDto>.Error("An error occurred while assigning the approver", 500);
			}
		}

		public async Task<ServiceResult<List<DocumentApprovalDto>>> GetPendingApprovalsAsync(int userId)
		{
			try
			{
				var approvals = await _context.DocumentApprovals
					.Include(da => da.Approver)
					.Where(da => da.ApproverId == userId && da.Status == DocumentApprovalStatus.Pending)
					.Select(da => new DocumentApprovalDto
					{
						Id = da.Id,
						DocumentId = da.DocumentId,
						ApproverId = da.ApproverId,
						ApproverName = da.Approver.Username,
						SignaturePositionX = da.SignaturePositionX ?? 0,
						SignaturePositionY = da.SignaturePositionY ?? 0,
						SignaturePage = da.SignaturePage ?? 0,
						Comments = da.Comments,
						AssignedAt = da.AssignedAt,
						ApprovedAt = da.ApprovedAt
					})
					.ToListAsync();

				return ServiceResult<List<DocumentApprovalDto>>.Ok(approvals);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving pending approvals for user {UserId}", userId);
				return ServiceResult<List<DocumentApprovalDto>>.Error("An error occurred while retrieving pending approvals", 500);
			}
		}

		public async Task<ServiceResult<DocumentDto>> ApproveDocumentAsync(int documentId, int userId, string? signature, string? password)
		{
			var document = await _context.Documents.FindAsync(documentId);
			if (document == null)
				return ServiceResult<DocumentDto>.Error("Document not found", 404);

			var approval = await _context.DocumentApprovals
				.Include(da => da.Approver)
				.FirstOrDefaultAsync(da => da.DocumentId == documentId && da.ApproverId == userId && da.Status == DocumentApprovalStatus.Pending);
			if (approval == null)
				return ServiceResult<DocumentDto>.Error("You are not assigned as an approver for this document or it is not pending your approval", 403);

			approval.Status = DocumentApprovalStatus.Approved;
			approval.ApprovedAt = DateTime.UtcNow;
			approval.Comments = $"Approved by {approval.Approver.Username} at {approval.ApprovedAt:yyyy-MM-dd HH:mm}";

			var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads");
			if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);
			var stampText = $"Approved by {approval.Approver.Username} on {approval.ApprovedAt:yyyy-MM-dd}";
			var stampResult = await StampDocument(documentId, stampText, uploadsDir, password);
			if (!stampResult.Success)
				return ServiceResult<DocumentDto>.Error(stampResult.Message ?? "Failed to stamp document", stampResult.StatusCode);

			document.Status = DocumentStatus.Signed;
			document.Signature = signature;
			document.SignedFilePath = stampResult.Data;
			await _context.SaveChangesAsync();

			_logger.LogInformation("Document {DocumentId} approved by user {UserId}", documentId, userId);
			return ServiceResult<DocumentDto>.Ok(MapToDto(document));
		}

		public async Task<ServiceResult<DocumentDto>> RejectDocumentAsync(int documentId, int userId, string? reason)
		{
			var document = await _context.Documents.FindAsync(documentId);
			if (document == null)
				return ServiceResult<DocumentDto>.Error("Document not found", 404);

			var approval = await _context.DocumentApprovals
				.Include(da => da.Approver)
				.FirstOrDefaultAsync(da => da.DocumentId == documentId && da.ApproverId == userId && da.Status == DocumentApprovalStatus.Pending);
			if (approval == null)
				return ServiceResult<DocumentDto>.Error("You are not assigned as an approver for this document or it is not pending your approval", 403);

			approval.Status = DocumentApprovalStatus.Rejected;
			approval.RejectedAt = DateTime.UtcNow;
			approval.Comments = reason;
			document.Status = DocumentStatus.Pending;
			await _context.SaveChangesAsync();

			_logger.LogInformation("Document {DocumentId} rejected by user {UserId}", documentId, userId);
			return ServiceResult<DocumentDto>.Ok(MapToDto(document));
		}

		private static DocumentDto MapToDto(Document document)
		{
			return new DocumentDto
			{
				Id = document.Id,
				FileName = document.FileName ?? string.Empty,
				FilePath = document.FilePath ?? string.Empty,
				Status = document.Status,
				Signature = document.Signature,
				SignedFilePath = document.SignedFilePath,
				UserId = document.UserId,
				UploadedAt = document.UploadedAt
			};
		}
	}
}
