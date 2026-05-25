using System.ComponentModel.DataAnnotations.Schema;

namespace SE114_MoneyApp_BE.DTOs.Transfer
{
    public class TransferResponse
    {
        public Guid Id { get; set; }
        public Guid SourceAccount { get; set; }
        public string SourceAccountName { get; set; } = string.Empty;
        public Guid DestinationAccount { get; set; }
        public string DestinationAccountName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime TransferDate { get; set; }
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
    }
}
