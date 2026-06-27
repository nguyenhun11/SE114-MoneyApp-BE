namespace SE114_MoneyApp_BE.DTOs.Account
{
    public class TotalBalanceDto
    {
        public decimal TotalBalance { get; set; }
        public decimal LockedBalance { get; set; }
        public decimal AvailableBalance { get; set; }
    }
}
