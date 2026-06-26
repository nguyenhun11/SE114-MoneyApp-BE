namespace SE114_MoneyApp_BE.DTOs.Statistic
{
    public class CashFlowBarChartDto
    {
        public string Period { get; set; } = string.Empty; //?
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal TotalSaved { get; set; } 
        public decimal TotalWithdrawn { get; set; }
        public decimal NetBalance => TotalIncome - TotalExpense;
    }
}
