using System.ComponentModel.DataAnnotations;
using e_tour_api.Data;

namespace e_tour_api.Models
{
    public class User
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }
        
        [MaxLength(500)]
        public string? ProfilePicture { get; set; }
        
        [MaxLength(500)]
        public string? Bio { get; set; }
        
        public bool IsEmailVerified { get; set; }
        
        [MaxLength(50)]
        public string? EmailVerificationToken { get; set; }
        
        public DateTime? EmailVerificationExpiry { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
        public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();
        public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
        public virtual ICollection<DocumentApproval> AssignedApprovals { get; set; } = new List<DocumentApproval>();
    }
} 