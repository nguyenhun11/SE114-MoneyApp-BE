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
        [Column(TypeName = "decimal(18,2)")]  public decimal OriginalAmount { get; set; }
        public string CurrencyCode { get; set; } = "VND";
        [Column(TypeName = "decimal(18,2)")] public decimal AccountAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal BaseAmount { get; set; }
        public double ExchangeRate { get; set; } = 1.0;

        public DateTime TransactionDate { get; set; }
        public string Note { get; set; } = string.Empty;
        public List<string> ImageUrls { get; set; } = new List<string>();

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
