namespace SE114_MoneyApp_BE.DTOs.User
{
    public class UserProfileResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? PhoneNumber { get; set; }
        public int DailyStreak { get; set; }
        public bool TodayCheckedIn { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
    }
}
