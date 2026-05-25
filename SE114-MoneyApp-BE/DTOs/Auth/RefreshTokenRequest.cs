using System.ComponentModel.DataAnnotations;

namespace SE114_MoneyApp_BE.DTOs.Auth
{
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
