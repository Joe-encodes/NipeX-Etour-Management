using System;
using e_tour_api.Data;
using e_tour_api.Models;

namespace e_tour_api.Models.DTOs
{
    /// <summary>
    /// Document DTO for transferring document data
    /// </summary>
    public class DocumentDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public DocumentStatus Status { get; set; }
        public string? Signature { get; set; }
        public string? SignedFilePath { get; set; }
        public int UserId { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    /// <summary>
    /// Document upload response
    /// </summary>
    public class DocumentUploadResponse
    {
        public int DocumentId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
} 