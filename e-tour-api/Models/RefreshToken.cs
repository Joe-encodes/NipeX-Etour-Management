using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace e_tour_api.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        
        [Required]
        public string Token { get; set; } = string.Empty;
        
        [Required]
        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
        
        [Required]
        public DateTime ExpiryDate { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastUsedAt { get; set; }

        public bool IsRevoked { get; set; }
    }
} 