namespace SE114_MoneyApp_BE.DTOs.User
{
    public class UserProfileRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DefaultCurrency { get; set; } = "VND";
        public string? ImageUrl { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
