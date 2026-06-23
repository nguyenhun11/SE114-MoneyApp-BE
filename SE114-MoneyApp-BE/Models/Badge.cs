using System.ComponentModel.DataAnnotations;

namespace SE114_MoneyApp_BE.Models
{
    public class Badge
    {
        [Key]
        public string Id { get; set; } = string.Empty;
        [Required]
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconKey { get; set; } = string.Empty;
        public string ConditionType { get; set; } = string.Empty; // e.g., "Streak", "GoalCompleted"
        public int ConditionValue { get; set; }
    }

    public class UserBadge
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public string BadgeId { get; set; } = string.Empty;
        public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
        public Badge? Badge { get; set; }
    }
}
