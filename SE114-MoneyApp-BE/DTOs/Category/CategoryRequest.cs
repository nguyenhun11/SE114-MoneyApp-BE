using System.ComponentModel.DataAnnotations.Schema;

namespace SE114_MoneyApp_BE.DTOs.Category
{
    public class CategoryRequest
    {
        public string CategoryName { get; set; } = string.Empty;
        public Guid GroupId { get; set; }
        public decimal MonthlyTarget { get; set; }
        public int ColorId { get; set; }
        public int IconId { get; set; }
    }
}
