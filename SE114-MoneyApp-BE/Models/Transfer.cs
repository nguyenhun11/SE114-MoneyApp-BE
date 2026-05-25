using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SE114_MoneyApp_BE.Models
{
    public class Transfer
    {
        [Key]
        public Guid Id { get; set; } = new Guid();
        public Guid SourceAccountId { get; set; }
        public Guid DestinationAccountId { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
        public DateTime TransferDate { get; set; }
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("SourceAccountId")]
        public virtual Account? Source { get; set; }

        [ForeignKey("DestinationAccountId")]
        public virtual Account? Destination { get; set; }
    }
}
