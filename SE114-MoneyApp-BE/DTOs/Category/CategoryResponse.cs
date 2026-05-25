using System.ComponentModel.DataAnnotations.Schema;

namespace SE114_MoneyApp_BE.DTOs.Category
{
    public class CategoryResponse
    {
        public Guid Id { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int Type { get; set; } // 0: Expense 1: Income
        public decimal MonthlyTarget { get; set; }
        public int ColorId { get; set; }
        public int IconId { get; set; }
        public bool IsDefault { get; set; }
        public int SortingOrder { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
