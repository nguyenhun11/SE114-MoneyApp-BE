using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SE114_MoneyApp_BE.Models
{
    public class Transfer
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SourceAccountId { get; set; }
        public Guid DestinationAccountId { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal SourceAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal DestinationAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal BaseAmount { get; set; }
        public double SourceExchangeRate { get; set; } = 1.0;
        public double DestinationExchangeRate { get; set; } = 1.0;
        public DateTime TransferDate { get; set; }
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("SourceAccountId")]
        public Account? Source { get; set; }

        [ForeignKey("DestinationAccountId")]
        public Account? Destination { get; set; }
    }
}
