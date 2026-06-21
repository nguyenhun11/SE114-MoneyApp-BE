using System.ComponentModel.DataAnnotations.Schema;

namespace SE114_MoneyApp_BE.DTOs.Transfer
{
    public class TransferRequest
    {
        public Guid SourceAccountId { get; set; }
        public Guid DestinationAccountId { get; set; }
        public decimal SourceAmount { get; set; }
        public decimal DestinationAmount { get; set; }
        public decimal BaseAmount { get; set; }
        public double SourceExchangeRate { get; set; } = 1.0;
        public double DestinationExchangeRate { get; set; } = 1.0;
        public DateTime TransferDate { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
