using System.ComponentModel.DataAnnotations;

namespace SE114_MoneyApp_BE.Models
{
    public enum RewardType
    {
        SP = 0,
        PP = 1
    }

    public class Quest
    {
        [Key]
        public string Id { get; set; } = string.Empty;
        [Required]
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Target { get; set; }
        public int RewardPoints { get; set; }
        public RewardType RewardType { get; set; }
        public string ActionType { get; set; } = string.Empty; // e.g., "AddTransaction", "DepositGoal"
    }

    public class UserQuest
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public string QuestId { get; set; } = string.Empty;
        public int CurrentProgress { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsClaimed { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
        public Quest? Quest { get; set; }
    }
}
