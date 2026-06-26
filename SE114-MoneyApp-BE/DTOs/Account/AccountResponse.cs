using System.ComponentModel.DataAnnotations.Schema;

namespace SE114_MoneyApp_BE.DTOs.Account
{
    public class AccountResponse
    {
        public Guid Id { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public int ColorId { get; set; }
        public int IconId { get; set; }

        public decimal TotalBalance { get; set; }
        public decimal LockedBalance { get; set; }
        public decimal AvailableBalance { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IncludeInTotalBalance { get; set; } = true;
        public int SortingOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
    }
}
