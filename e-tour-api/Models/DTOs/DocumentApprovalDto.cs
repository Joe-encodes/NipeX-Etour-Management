namespace e_tour_api.Models.DTOs
{
    public class DocumentApprovalDto
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public int ApproverId { get; set; }
        public string ApproverName { get; set; } = string.Empty;
        public float SignaturePositionX { get; set; }
        public float SignaturePositionY { get; set; }
        public int SignaturePage { get; set; }
        public DocumentApprovalStatus Status { get; set; }
        public string? Comments { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
    }
} 