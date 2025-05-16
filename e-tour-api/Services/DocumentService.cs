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
	public class DocumentService : IDocumentService
	{
		private readonly AppDbContext _context;
		private readonly IPdfStampService _stampService;
		private readonly ILogger<DocumentService> _logger;

		public DocumentService(
			AppDbContext context, 
			IPdfStampService stampService,
			ILogger<DocumentService> logger)
		{
			_context = context;
			_stampService = stampService;
			_logger = logger;
		}

		public async Task<Document?> GetDocumentById(int documentId)
		{
			return await _context.Documents.FindAsync(documentId);
		}

		public async Task<DocumentAccessResult> VerifyDocumentAccess(int documentId, int userId)
		{
			var document = await GetDocumentById(documentId);
			
			if (document == null || document.Status != "Signed")
				return new DocumentAccessResult(false, "Document not found or not signed", 404);
			
			if (document.UserId != userId)
				return new DocumentAccessResult(false, "You can only download your own documents", 403);
			
			return new DocumentAccessResult(true);
		}

		public async Task<StampResult> StampDocument(int documentId, string stampText, string uploadsPath)
		{
			try
			{
				var document = await GetDocumentById(documentId);
				if (document == null)
					return new StampResult(false, "Document not found");
				
				if (!Directory.Exists(uploadsPath))
					Directory.CreateDirectory(uploadsPath);

				var signedName = $"{document.FileName}_signed.pdf";
				var signedPath = Path.Combine(uploadsPath, signedName);
				
				if (string.IsNullOrEmpty(document.FilePath))
					return new StampResult(false, "Document path is invalid");
				var resultPath = await _stampService.StampAsync(document.FilePath, signedPath, stampText);

				document.Status = "Signed";
				document.SignedFilePath = signedPath;
				await _context.SaveChangesAsync();

				return new StampResult(true) { SignedFilePath = resultPath };
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error stamping document {DocumentId}", documentId);
				return new StampResult(false, ex.Message);
			}
		}

		public async Task<FileResult> GetDocumentFile(int documentId)
		{
			try
			{
				var document = await GetDocumentById(documentId);
				if (document == null || string.IsNullOrEmpty(document.SignedFilePath))
					return new FileResult(false, "Document file not found", 404);

				var fileContents = await System.IO.File.ReadAllBytesAsync(document.SignedFilePath);
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
	}


	// Supporting DTOs
	public record DocumentAccessResult(
		bool HasAccess,
		string? ErrorMessage = null,
		int? ErrorCode = null
	);

	public record StampResult(
		bool Success,
		string? ErrorMessage = null,
		string? SignedFilePath = null
	);

	public record FileResult(
		bool Success,
		string? ErrorMessage = null,
		int? ErrorCode = null,
		byte[]? FileContents = null,
		string FileName = "document.pdf"
	);
}
