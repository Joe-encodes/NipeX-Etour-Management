using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using e_tour_api.Data;

namespace e_tour_api.Models
{
    public class DocumentApproval
    {
        public int Id { get; set; }
        
        [Required]
        public int DocumentId { get; set; }
        
        [ForeignKey("DocumentId")]
        public virtual Document Document { get; set; } = null!;
        
        [Required]
        public int ApproverId { get; set; }
        
        [ForeignKey("ApproverId")]
        public virtual User Approver { get; set; } = null!;
        
        public int? SignaturePositionX { get; set; }
        public int? SignaturePositionY { get; set; }
        public int? SignaturePage { get; set; }
        
        public string? Comments { get; set; }
        
        public DocumentApprovalStatus Status { get; set; } = DocumentApprovalStatus.Pending;
        
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
    }

    public enum DocumentApprovalStatus
    {
        Pending,
        Approved,
        Rejected
    }
} 