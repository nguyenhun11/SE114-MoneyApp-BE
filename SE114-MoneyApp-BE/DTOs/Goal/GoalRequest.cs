using System.ComponentModel.DataAnnotations;

namespace SE114_MoneyApp_BE.DTOs.Goal
{
    public class GoalRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Target amount must be greater than 0")]
        public decimal TargetAmount { get; set; }

        public DateTime Deadline { get; set; }

        public int IconId { get; set; }

        public int ColorId { get; set; }
    }
}
