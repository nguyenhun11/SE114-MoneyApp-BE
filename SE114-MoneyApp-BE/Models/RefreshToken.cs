using System.ComponentModel.DataAnnotations;

namespace SE114_MoneyApp_BE.Models
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
