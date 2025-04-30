using System;

namespace e_tour_api.Data
{
    public class Document
    {
        public int Id { get; set; }
        public string? FileName { get; set; } // Nullable, set in UploadDocument
        public string? FilePath { get; set; } // Nullable, set in UploadDocument
        public int UserId { get; set; } // Non-nullable foreign key
        public User? User { get; set; } // Nullable navigation property
        public string Status { get; set; } = "Pending"; // Default value
        public string? Signature { get; set; } // Nullable, for approver signature
        public DateTime UploadedAt { get; set; }
        public string? SignedFilePath { get; set; } // Added for signed document path
    }
}