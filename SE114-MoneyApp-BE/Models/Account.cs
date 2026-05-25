using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SE114_MoneyApp_BE.Models
{
    public class Account
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public int UserId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public int ColorId { get; set; }
        public int IconId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IncludeInTotalBalance { get; set; } = true;

        //
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

    }
}
