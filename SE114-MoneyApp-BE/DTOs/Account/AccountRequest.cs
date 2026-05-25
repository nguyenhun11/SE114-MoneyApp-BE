using System.ComponentModel.DataAnnotations.Schema;

namespace SE114_MoneyApp_BE.DTOs.Account
{
    public class AccountRequest
    {
        public string AccountName { get; set; } = string.Empty;
        public int ColorId { get; set; }
        public int IconId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public bool IncludeInTotalBalance { get; set; } = true;
    }
}
