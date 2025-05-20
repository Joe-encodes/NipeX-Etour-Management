using System;

namespace e_tour_api.Data
{
    /// <summary>
    /// Enumeration of possible document statuses.
    /// </summary>
    public enum DocumentStatus
    {
        /// <summary>
        /// Document is in draft state.
        /// </summary>
        Draft,

        /// <summary>
        /// Document is pending approval.
        /// </summary>
        Pending,

        /// <summary>
        /// Document has been signed.
        /// </summary>
        Signed
    }

    /// <summary>
    /// Represents a document uploaded by a user.
    /// </summary>
    public class Document
    {
        /// <summary>
        /// Document identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name of the file.
        /// </summary>
        public string? FileName { get; set; } // Nullable, set in UploadDocument

        /// <summary>
        /// Path to the file on disk.
        /// </summary>
        public string? FilePath { get; set; } // Nullable, set in UploadDocument

        /// <summary>
        /// Foreign key to the user who uploaded the document.
        /// </summary>
        public int UserId { get; set; } // Non-nullable foreign key

        /// <summary>
        /// Navigation property to the user.
        /// </summary>
        public User? User { get; set; } // Nullable navigation property

        /// <summary>
        /// Current status of the document.
        /// </summary>
        public DocumentStatus Status { get; set; } = DocumentStatus.Pending; // Default value

        /// <summary>
        /// Signature of the approver.
        /// </summary>
        public string? Signature { get; set; } // Nullable, for approver signature

        /// <summary>
        /// Date and time when the document was uploaded.
        /// </summary>
        public DateTime UploadedAt { get; set; }

        /// <summary>
        /// Path to the signed document file.
        /// </summary>
        public string? SignedFilePath { get; set; } // Added for signed document path
    }
}
