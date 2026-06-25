using SE114_MoneyApp_BE.DTOs.Budget;
using SE114_MoneyApp_BE.Models;

namespace SE114_MoneyApp_BE.DTOs.Category
{
    public class CategoryGroupRequest
    {
        public String GroupName { get; set; } = string.Empty;
        public BudgetRequest? BudgetSetup { get; set; }
    }
}
