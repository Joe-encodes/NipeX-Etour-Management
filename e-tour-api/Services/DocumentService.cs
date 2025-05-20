// Services/DocumentService.cs
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using e_tour_api.Data;  // For AppDbContext
using e_tour_api.Services; // For IPdfStampService

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
		private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;

		/// <summary>
		/// Initializes a new instance of the DocumentService.
		/// </summary>
		/// <param name="context">Database context for document operations</param>
		/// <param name="stampService">Service for PDF stamping operations</param>
		/// <param name="logger">Logger for tracking operations</param>
		/// <param name="env">Web hosting environment for file operations</param>
		public DocumentService(
			AppDbContext context, 
			IPdfStampService stampService,
			ILogger<DocumentService> logger,
			Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
		{
			_context = context;
			_stampService = stampService;
			_logger = logger;
			_env = env;
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
		public async Task<DocumentAccessResult> VerifyDocumentAccess(int documentId, int userId)
		{
			var document = await GetDocumentById(documentId);
			
			if (document == null || document.Status != DocumentStatus.Signed)
				return new DocumentAccessResult(false, "Document not found or not signed", 404);
			
			if (document.UserId != userId)
				return new DocumentAccessResult(false, "You can only download your own documents", 403);
			
			return new DocumentAccessResult(true);
		}

		/// <summary>
		/// Stamps a document with specified text.
		/// </summary>
		/// <param name="documentId">The ID of the document to stamp</param>
		/// <param name="stampText">The text to stamp on the document</param>
		/// <param name="uploadsPath">The path where uploaded files are stored</param>
		/// <param name="password">Optional password for the PDF file</param>
		/// <returns>Result of the stamping operation including the path to the stamped file</returns>
		public async Task<StampResult> StampDocument(int documentId, string stampText, string uploadsPath, string? password = null)
		{
			try
			{
				var document = await GetDocumentById(documentId);
				if (document == null)
					return new StampResult(false, "Document not found");
				
				if (!Directory.Exists(uploadsPath))
					Directory.CreateDirectory(uploadsPath);

				// Validate file type
				if (string.IsNullOrEmpty(document.FilePath) || !document.FilePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
					return new StampResult(false, "Invalid file type.");

				// Get the full path of the source file
				var sourceFilePath = Path.Combine(uploadsPath, document.FilePath);
				if (!File.Exists(sourceFilePath))
					return new StampResult(false, "Source file not found.");

				// Use Path.GetRandomFileName for temp file name
				var randomFileName = Path.GetRandomFileName();
				var signedName = Path.ChangeExtension(randomFileName, ".pdf");
				var signedPath = Path.Combine(uploadsPath, signedName);

				var resultPath = await _stampService.StampAsync(sourceFilePath, signedPath, stampText, password);

				document.Status = DocumentStatus.Signed;
				document.SignedFilePath = signedName;
				await _context.SaveChangesAsync();

				return new StampResult(true) { SignedFilePath = resultPath };
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error stamping document {DocumentId}", documentId);
				return new StampResult(false, ex.Message);
			}
		}

		/// <summary>
		/// Retrieves the file content of a document.
		/// </summary>
		/// <param name="documentId">The ID of the document to retrieve</param>
		/// <returns>File result containing the document contents and metadata</returns>
		public async Task<FileResult> GetDocumentFile(int documentId)
		{
			try
			{
				var document = await GetDocumentById(documentId);
				if (document == null || string.IsNullOrEmpty(document.SignedFilePath))
					return new FileResult(false, "Document file not found", 404);

				var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
				var filePath = Path.Combine(uploadsDir, document.SignedFilePath);

				if (!File.Exists(filePath))
					return new FileResult(false, "Document file not found", 404);

				// Retry logic for file read
				const int maxRetries = 3;
				const int delayMs = 200;
				byte[]? fileContents = null;
				for (int i = 0; i < maxRetries; i++)
				{
					try
					{
						fileContents = await File.ReadAllBytesAsync(filePath);
						break;
					}
					catch (IOException)
					{
						if (i == maxRetries - 1)
							throw;
						await Task.Delay(delayMs);
					}
				}

				return new FileResult(true) 
				{ 
					FileContents = fileContents,
					FileName = document.FileName ?? "document.pdf"
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting document file {DocumentId}", documentId);
				return new FileResult(false, ex.Message, 500);
			}
		}

		/// <summary>
		/// Updates a document in the database.
		/// </summary>
		/// <param name="document">The document to update</param>
		public async Task UpdateDocument(Document document)
		{
			_context.Documents.Update(document);
			await _context.SaveChangesAsync();
		}

		public async Task<List<Document>> GetUserDocumentsAsync(int userId)
		{
			return await _context.Documents
				.Where(d => d.UserId == userId)
				.ToListAsync();
		}

		public async Task<List<Document>> GetPendingDocumentsAsync()
		{
			return await _context.Documents
				.Where(d => d.Status == DocumentStatus.Pending)
				.ToListAsync();
		}
	}

	/// <summary>
	/// Result of a document access verification operation.
	/// </summary>
	public record DocumentAccessResult(
		bool HasAccess,
		string? ErrorMessage = null,
		int? ErrorCode = null
	);

	/// <summary>
	/// Result of a document stamping operation.
	/// </summary>
	public record StampResult(
		bool Success,
		string? ErrorMessage = null,
		string? SignedFilePath = null
	);

	/// <summary>
	/// Result of a document file retrieval operation.
	/// </summary>
	public record FileResult(
		bool Success,
		string? ErrorMessage = null,
		int? ErrorCode = null,
		byte[]? FileContents = null,
		string FileName = "document.pdf"
	);
}
