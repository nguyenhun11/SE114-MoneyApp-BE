using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SE114_MoneyApp_BE.Models
{
    public class Transaction
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AccountId { get; set; }
        public Guid CategoryId { get; set; }

        [Column("BaseAmount", TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime TransactionDate { get; set; }
        public string Note { get; set; } = string.Empty;
        public List<string> ImageUrls { get; set; } = new List<string>();
        public int MoodId { get; set; } = 0; // 0: Neutral, 1: Happy, 2: Sad, 3: Stressed, 4: Impulsive

        //
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        //
        [ForeignKey("AccountId")]
        public Account? Account { get; set; }
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }
    }
}
