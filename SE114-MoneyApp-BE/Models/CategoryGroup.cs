using System.ComponentModel.DataAnnotations.Schema;

namespace SE114_MoneyApp_BE.Models
{
    public class CategoryGroup
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int UserId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public CategoryType Type { get; set; }
        public int SortingOrder { get; set; }

        //
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Category> Categories { get; set; } = new List<Category>();
        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}
