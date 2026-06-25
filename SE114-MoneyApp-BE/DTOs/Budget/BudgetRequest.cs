using SE114_MoneyApp_BE.Models;

namespace SE114_MoneyApp_BE.DTOs.Budget
{
    public class BudgetRequest
    {
        public Guid? CategoryId { get; set; }
        public decimal Amount { get; set; }
        public BudgetPeriod Period { get; set; }
        public DateTime StartDate { get; set; }
        public Guid? CategoryGroupId { get; set; }
    }
}
