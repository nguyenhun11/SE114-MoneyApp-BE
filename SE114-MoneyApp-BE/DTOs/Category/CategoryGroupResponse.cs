using SE114_MoneyApp_BE.Models;

namespace SE114_MoneyApp_BE.DTOs.Category
{
    public class CategoryGroupResponse
    {
        public Guid Id { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public CategoryType Type { get; set; }
        public int SortingOrder { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
