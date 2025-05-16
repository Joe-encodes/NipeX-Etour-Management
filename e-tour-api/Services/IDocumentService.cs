// Services/IDocumentService.cs
using e_tour_api.Data;

namespace e_tour_api.Services
{
    public interface IDocumentService
    {
        Task<Document?> GetDocumentById(int documentId);
        Task<DocumentAccessResult> VerifyDocumentAccess(int documentId, int userId);
        Task<StampResult> StampDocument(int documentId, string stampText, string uploadsPath);
        Task<FileResult> GetDocumentFile(int documentId);
    }
}
