using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SE114_MoneyApp_BE.Models
{
    public class AdjustBalance
    {
        [Key]
        public Guid Id { get; set; } = new Guid();
        public Guid AccountId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime CreatedAt = DateTime.UtcNow;
        public DateTime LaseUpdatedAt = DateTime.UtcNow;
    }
}
