using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SE114_MoneyApp_BE.Models
{
    public class Category
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public int UserId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public Guid GroupId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyTarget { get; set; }
        public int ColorId { get; set; }
        public int IconId { get; set; }
        public bool IsDefault { get; set; } = false; //TODO: Create default category when register
        public int SortingOrder { get; set; }

        //
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        //
        [ForeignKey("UserId")]
        public User? User { get; set; }
        [ForeignKey("CategoryGroupId")]
        public CategoryGroup? CategoryGroup { get; set; }
    }


    public enum CategoryType
    {
        All = -1,
        Expense = 0,
        Income = 1
    }
}

