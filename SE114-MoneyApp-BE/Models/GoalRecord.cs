using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SE114_MoneyApp_BE.Models
{
    public class GoalRecord
    {
        [Key] public int Id { get; set; }
        public int GoalId { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
        public Guid AccountId { get; set; }
        [Required] public string Type { get; set; } = string.Empty; //Deposit / Withdraw
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [ForeignKey("GoalId")] public Goal? Goal { get; set; }
        [ForeignKey("AccountId")] public Account? Account { get; set; }
    }
}