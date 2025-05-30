using FluentValidation;
using Microsoft.AspNetCore.Http;
using e_tour_api.Models;

namespace e_tour_api.Validators
{
    /// <summary>
    /// Validator for document upload requests
    /// </summary>
    public class DocumentUploadValidator : AbstractValidator<DocumentUploadRequest>
    {
        public DocumentUploadValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Document title is required")
                .MaximumLength(255).WithMessage("Document title cannot exceed 255 characters");

            RuleFor(x => x.File)
                .NotNull().WithMessage("File is required")
                .Must(f => f.ContentType == "application/pdf").WithMessage("Only PDF files are allowed")
                .Must(f => f.Length <= 10 * 1024 * 1024).WithMessage("File size cannot exceed 10MB");
        }
    }

    /// <summary>
    /// Validator for document approval requests
    /// </summary>
    public class DocumentApprovalValidator : AbstractValidator<DocumentApprovalRequest>
    {
        public DocumentApprovalValidator()
        {
            RuleFor(x => x.Signature)
                .NotEmpty().WithMessage("Signature is required")
                .MaximumLength(100).WithMessage("Signature cannot exceed 100 characters");

            RuleFor(x => x.Password)
                .MaximumLength(50).WithMessage("Password cannot exceed 50 characters")
                .When(x => !string.IsNullOrEmpty(x.Password));
        }
    }
}

namespace e_tour_api.Models
{
    /// <summary>
    /// Document upload request model
    /// </summary>
    public class DocumentUploadRequest
    {
        /// <summary>
        /// Title of the document
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// PDF file to upload
        /// </summary>
        public IFormFile File { get; set; } = null!;
    }

    /// <summary>
    /// Document approval request model
    /// </summary>
    public class DocumentApprovalRequest
    {
        /// <summary>
        /// Signature of the approver
        /// </summary>
        public string Signature { get; set; } = string.Empty;

        /// <summary>
        /// Optional password for the PDF file
        /// </summary>
        public string? Password { get; set; }
    }
} 