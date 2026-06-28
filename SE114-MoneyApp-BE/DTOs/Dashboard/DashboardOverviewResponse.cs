namespace SE114_MoneyApp_BE.DTOs.Dashboard
{
    public class DashboardOverviewResponse
    {
        public UserSummaryDto UserSummary { get; set; } = new();
        public CitySummaryDto CitySummary { get; set; } = new();
        public List<SmartInsightDto> SmartInsights { get; set; } = new();
        public List<BudgetAlertDto> BudgetAlerts { get; set; } = new();
        public List<GoalHighlightDto> GoalHighlights { get; set; } = new();
        public List<RecentTransactionDto> RecentTransactions { get; set; } = new();
        public List<PendingQuestDto> PendingQuests { get; set; } = new();
    }

    public class UserSummaryDto
    {
        public string Name { get; set; } = string.Empty;
        public int DailyStreak { get; set; }
        public bool TodayCheckedIn { get; set; }
    }

    public class CitySummaryDto
    {
        public int Level { get; set; }
        public int ProsperityPoints { get; set; }
        public int StabilityPoints { get; set; }
    }

    public class SmartInsightDto
    {
        public string Type { get; set; } = string.Empty; // INFO, SUCCESS, DANGER
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class BudgetAlertDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal UsedAmount { get; set; }
        public int Percent { get; set; }
        public string Status { get; set; } = string.Empty; // WARNING, OVER
    }

    public class GoalHighlightDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public int ProgressPercent { get; set; }
        public int IconId { get; set; }
        public int ColorId { get; set; }
    }

    public class RecentTransactionDto
    {
        public Guid Id { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public int IconId { get; set; }
        public int ColorId { get; set; }
        public DateTime Date { get; set; }
    }

    public class PendingQuestDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int CurrentProgress { get; set; }
        public int Target { get; set; }
    }
}
