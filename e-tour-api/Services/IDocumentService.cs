// Services/IDocumentService.cs
using e_tour_api.Data;
using Microsoft.AspNetCore.Mvc;
using e_tour_api.Models;
using Microsoft.AspNetCore.Http;
using e_tour_api.Models.DTOs;

namespace e_tour_api.Services
{
    /// <summary>
    /// Interface for document-related operations.
    /// </summary>
    public interface IDocumentService
    {
        // Document Retrieval Methods
        Task<ServiceResult<DocumentDto>> GetDocumentWithCacheAsync(int documentId, int userId);
        Task<Document?> GetDocumentById(int documentId);
        Task<ServiceResult<List<DocumentDto>>> GetUserDocumentsAsync(int userId);
        Task<ServiceResult<List<DocumentDto>>> GetPendingDocumentsAsync();
        Task<ServiceResult<List<DocumentDto>>> GetAllDocumentsAsync();
        Task<ServiceResult<DocumentFile>> GetDocumentFile(int documentId);

        // Document Management Methods
        Task<ServiceResult<DocumentDto>> UploadDocumentAsync(int userId, string title, IFormFile file);
        Task<ServiceResult> UpdateDocument(Document document);
        Task<ServiceResult> DeleteDocumentAsync(int documentId);
        Task InvalidateDocumentCacheAsync(int documentId);

        // Document Approval Methods
        Task<ServiceResult<DocumentApprovalDto>> AssignApproverAsync(int documentId, int approverId, float signaturePositionX, float signaturePositionY, int signaturePage, string? comments);
        Task<ServiceResult<List<DocumentApprovalDto>>> GetPendingApprovalsAsync(int userId);
        Task<ServiceResult<DocumentDto>> ApproveDocumentAsync(int documentId, int userId, string? signature, string? password);
        Task<ServiceResult<DocumentDto>> RejectDocumentAsync(int documentId, int userId, string? reason);

        // Document Access and Stamping
        Task<ServiceResult> VerifyDocumentAccess(int documentId, int userId);
        Task<ServiceResult<string>> StampDocument(int documentId, string stampText, string uploadsPath, string? password = null);

        // User Management
        Task<ServiceResult<List<UserProfileDto>>> GetAllUsersAsync();
    }
}
