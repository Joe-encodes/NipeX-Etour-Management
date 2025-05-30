using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace e_tour_api.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int SenderId { get; set; }
        
        [Required]
        public int ReceiverId { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;
        
        [Required]
        public string EncryptedContent { get; set; } = string.Empty;
        
        public bool IsRead { get; set; }
        
        public DateTime? ReadAt { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        [ForeignKey("SenderId")]
        public virtual User Sender { get; set; } = null!;
        
        [ForeignKey("ReceiverId")]
        public virtual User Receiver { get; set; } = null!;
    }
} 