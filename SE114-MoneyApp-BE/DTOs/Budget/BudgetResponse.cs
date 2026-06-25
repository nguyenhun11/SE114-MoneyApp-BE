using SE114_MoneyApp_BE.Models;

namespace SE114_MoneyApp_BE.DTOs.Budget
{
    public class BudgetResponse
    {
        public int Id { get; set; }
        public Guid? CategoryGroupId { get; set; }
        public Guid? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public decimal Amount { get; set; }
        public decimal UsedAmount { get; set; }
        public decimal RemainingAmount => Amount - UsedAmount;
        public double PercentageUsed => Amount > 0 ? (double)(UsedAmount / Amount * 100) : 0;
        public int CurrentCycleIndex { get; set; }
        public BudgetPeriod Period { get; set; }
        public DateTime StartDate { get; set; }
        public bool IsActive { get; set; }
    }
}
