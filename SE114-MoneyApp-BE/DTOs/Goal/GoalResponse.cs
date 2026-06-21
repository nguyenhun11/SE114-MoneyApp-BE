namespace SE114_MoneyApp_BE.DTOs.Goal
{
    public class GoalResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public DateTime Deadline { get; set; }
        public int IconId { get; set; }
        public int ColorId { get; set; }
        public bool IsActive { get; set; }
    }
}
