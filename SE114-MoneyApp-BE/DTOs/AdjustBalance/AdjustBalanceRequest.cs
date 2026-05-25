namespace SE114_MoneyApp_BE.DTOs.AdjustBalance
{
    public class AdjustBalanceRequest
    {
        public Guid AccountId { get; set; }
        public decimal Amount { get; set; }
    }
}
