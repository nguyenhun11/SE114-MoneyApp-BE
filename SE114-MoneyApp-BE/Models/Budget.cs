using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SE114_MoneyApp_BE.Models
{
    public enum BudgetPeriod
    {
        Weekly = 0,
        Monthly = 1,
        Yearly = 2
    }

    public class Budget
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public Guid? CategoryGroupId { get; set; } // Ngân sách cho nhóm hạng mục

        public Guid? CategoryId { get; set; } // Nếu null thì là ngân sách tổng

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public BudgetPeriod Period { get; set; }

        public DateTime StartDate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }
        [ForeignKey("CategoryGroupId")]
        public CategoryGroup? CategoryGroup { get; set; }
    }
}
