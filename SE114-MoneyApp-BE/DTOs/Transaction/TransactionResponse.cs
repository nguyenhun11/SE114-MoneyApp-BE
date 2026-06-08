using SE114_MoneyApp_BE.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace SE114_MoneyApp_BE.DTOs.Transaction
{
    public class TransactionResponse
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public CategoryType Type { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Note { get; set; } = string.Empty;
        public List<string> ImageUrls { get; set; } = new List<string>();

        //
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
