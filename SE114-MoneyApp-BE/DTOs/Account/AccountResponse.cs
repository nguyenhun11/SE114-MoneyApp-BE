using System.ComponentModel.DataAnnotations.Schema;

namespace SE114_MoneyApp_BE.DTOs.Account
{
    public class AccountResponse
    {
        public Guid Id { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public int ColorId { get; set; }
        public int IconId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IncludeInTotalBalance { get; set; } = true;
        public int SortingOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
    }
}
