using SE114_MoneyApp_BE.Models;

namespace SE114_MoneyApp_BE.DTOs.Quest
{
    public class QuestResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Target { get; set; }
        public int CurrentProgress { get; set; }
        public int RewardPoints { get; set; }
        public RewardType RewardType { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsClaimed { get; set; }
    }
}
