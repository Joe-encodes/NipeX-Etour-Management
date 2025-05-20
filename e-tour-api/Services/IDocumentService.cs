// Services/IDocumentService.cs
using e_tour_api.Data;
using Microsoft.AspNetCore.Mvc;

namespace e_tour_api.Services
{
    /// <summary>
    /// Interface for document-related operations.
    /// </summary>
    public interface IDocumentService
    {
        /// <summary>
        /// Retrieves a document by its ID.
        /// </summary>
        /// <param name="documentId">The ID of the document</param>
        /// <returns>The document if found, otherwise null</returns>
        Task<Document?> GetDocumentById(int documentId);

        /// <summary>
        /// Retrieves all documents for a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <returns>List of user's documents</returns>
        Task<List<Document>> GetUserDocumentsAsync(int userId);

        /// <summary>
        /// Retrieves all pending documents.
        /// </summary>
        /// <returns>List of pending documents</returns>
        Task<List<Document>> GetPendingDocumentsAsync();

        /// <summary>
        /// Verifies if a user has access to a document.
        /// </summary>
        /// <param name="documentId">The ID of the document</param>
        /// <param name="userId">The ID of the user</param>
        /// <returns>Result indicating access status</returns>
        Task<DocumentAccessResult> VerifyDocumentAccess(int documentId, int userId);

        /// <summary>
        /// Stamps a document with specified text.
        /// </summary>
        /// <param name="documentId">The ID of the document</param>
        /// <param name="stampText">Text to stamp on the document</param>
        /// <param name="uploadsPath">Path to uploads directory</param>
        /// <param name="password">Optional password for stamping</param>
        /// <returns>Result of the stamping operation</returns>
        Task<StampResult> StampDocument(int documentId, string stampText, string uploadsPath, string? password = null);

        /// <summary>
        /// Retrieves the file content of a document.
        /// </summary>
        /// <param name="documentId">The ID of the document</param>
        /// <returns>File result containing the document file</returns>
        Task<FileResult> GetDocumentFile(int documentId);

        /// <summary>
        /// Updates a document entity.
        /// </summary>
        /// <param name="document">The document to update</param>
        Task UpdateDocument(Document document);
    }
}
