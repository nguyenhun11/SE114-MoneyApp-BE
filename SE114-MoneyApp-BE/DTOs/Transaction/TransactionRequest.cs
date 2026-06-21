using System.ComponentModel.DataAnnotations.Schema;

namespace SE114_MoneyApp_BE.DTOs.Transaction
{
    public class TransactionRequest
    {
        public Guid AccountId { get; set; }
        public Guid CategoryId { get; set; }
        public decimal OriginalAmount { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public decimal AccountAmount { get; set; }
        public decimal BaseAmount { get; set; }
        public double ExchangeRate { get; set; } = 1.0;
        public DateTime Date { get; set; }
        public string Note { get; set; } = string.Empty;
        public List<string> ImageUrls { get; set; } = new List<string>();
        public int MoodId { get; set; }
    }
}
