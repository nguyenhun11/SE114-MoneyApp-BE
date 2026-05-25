using System.ComponentModel.DataAnnotations;

namespace SE114_MoneyApp_BE.DTOs.Auth
{
    public class GoogleLoginRequest
    {
        [Required]
        public string IdToken { get; set; } = string.Empty;
    }
}
