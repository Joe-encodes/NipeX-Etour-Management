using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace e_tour_api.Models
{
    public class Document
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? FilePath { get; set; }
        
        [MaxLength(100)]
        public string? Signature { get; set; }
        
        [MaxLength(500)]
        public string? SignedFilePath { get; set; }
        
        public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
        
        [Required]
        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
        
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }

    public enum DocumentStatus
    {
        Pending,
        Signed,
        Rejected
    }
} 