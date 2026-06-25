using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SE114_MoneyApp_BE.Controllers.Base;
using SE114_MoneyApp_BE.Data;
using SE114_MoneyApp_BE.DTOs.Budget;
using SE114_MoneyApp_BE.Models;
using SE114_MoneyApp_BE.Services;

namespace SE114_MoneyApp_BE.Controllers
{
    [Route("api/[controller]")]
    public class BudgetController : AuthorizeControllerBase
    {
        private readonly GamificationService _gamificationService;

        public BudgetController(AppDbContext context, IMemoryCache cache, GamificationService gamificationService)
            : base(context, cache)
        {
            _gamificationService = gamificationService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BudgetResponse>>> GetBudgets()
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var budgets = await _context.Budgets
                .Include(b => b.Category)
                .Include(b => b.CategoryGroup)
                .Where(b => b.UserId == userId && b.IsActive)
                .ToListAsync();

            var response = new List<BudgetResponse>();
            foreach (var budget in budgets)
            {
                // ĐÃ SỬA: Lấy thêm index của chu kỳ hiện tại
                var (usedAmount, cycleIndex) = await CalculateUsedAmountAndCycleIndex(budget);

                response.Add(new BudgetResponse
                {
                    Id = budget.Id,
                    CategoryGroupId = budget.CategoryGroupId,
                    CategoryId = budget.CategoryId,
                    CategoryName = budget.Category?.CategoryName ?? budget.CategoryGroup?.GroupName ?? "Ngân sách tổng",
                    Amount = budget.Amount,
                    UsedAmount = usedAmount,
                    Period = budget.Period,
                    StartDate = budget.StartDate,
                    IsActive = budget.IsActive,
                    CurrentCycleIndex = cycleIndex // Gán vào Response
                });
            }

            return Ok(response);
        }

        // ... (Các hàm POST, PUT, DELETE giữ nguyên) ...

        // =================================================================================
        // THUẬT TOÁN TÍNH TOÁN (Trả về lượng tiền đã dùng + Số thứ tự chu kỳ)
        // =================================================================================
        private async Task<(decimal usedAmount, int cycleIndex)> CalculateUsedAmountAndCycleIndex(Budget budget)
        {
            var (currentCycleStart, currentCycleEnd, cycleIndex) = GetCurrentCycle(budget.StartDate, budget.Period);

            var query = _context.Transactions
                .Where(t => t.Account!.UserId == budget.UserId
                         && t.TransactionDate >= currentCycleStart
                         && t.TransactionDate < currentCycleEnd);

            if (budget.CategoryId.HasValue)
            {
                query = query.Where(t => t.CategoryId == budget.CategoryId.Value);
            }
            else if (budget.CategoryGroupId.HasValue)
            {
                query = query.Include(t => t.Category)
                             .Where(t => t.Category!.CategoryGroupId == budget.CategoryGroupId.Value);
            }
            else
            {
                query = query.Include(t => t.Category).ThenInclude(c => c!.CategoryGroup)
                             .Where(t => t.Category!.CategoryGroup!.Type == CategoryType.Expense);
            }

            decimal usedAmount = await query.SumAsync(t => Math.Abs(t.BaseAmount));
            return (usedAmount, cycleIndex);
        }

        // =================================================================================
        // THUẬT TOÁN TÍNH CHU KỲ (DỊCH MỐC MỎ NEO)
        // =================================================================================
        private (DateTime start, DateTime end, int cycleIndex) GetCurrentCycle(DateTime anchorDate, BudgetPeriod period)
        {
            var now = DateTime.UtcNow;
            DateTime currentStart = anchorDate;
            DateTime currentEnd;
            int cycleIndex = 0; // Khởi tạo số đếm

            if (now < anchorDate)
            {
                // Chưa tới ngày bắt đầu -> Chu kỳ 0
                switch (period)
                {
                    case BudgetPeriod.Weekly: return (anchorDate, anchorDate.AddDays(7), 0);
                    case BudgetPeriod.Yearly: return (anchorDate, anchorDate.AddYears(1), 0);
                    default: return (anchorDate, anchorDate.AddMonths(1), 0);
                }
            }

            switch (period)
            {
                case BudgetPeriod.Weekly:
                    int daysSinceAnchor = (now - anchorDate).Days;
                    cycleIndex = daysSinceAnchor / 7; // Số tuần đã trôi qua
                    currentStart = anchorDate.AddDays(cycleIndex * 7);
                    currentEnd = currentStart.AddDays(7);
                    break;

                case BudgetPeriod.Monthly:
                    cycleIndex = ((now.Year - anchorDate.Year) * 12) + now.Month - anchorDate.Month;
                    currentStart = anchorDate.AddMonths(cycleIndex);

                    if (currentStart > now)
                    {
                        currentStart = currentStart.AddMonths(-1);
                        cycleIndex--; // Lùi lại 1 chu kỳ
                    }
                    currentEnd = currentStart.AddMonths(1);
                    break;

                case BudgetPeriod.Yearly:
                    cycleIndex = now.Year - anchorDate.Year;
                    currentStart = anchorDate.AddYears(cycleIndex);

                    if (currentStart > now)
                    {
                        currentStart = currentStart.AddYears(-1);
                        cycleIndex--;
                    }
                    currentEnd = currentStart.AddYears(1);
                    break;

                default:
                    currentEnd = currentStart.AddMonths(1);
                    break;
            }

            // cycleIndex + 1 vì nếu vừa tạo thì tính là chu kỳ thứ 1 đang chạy
            return (currentStart, currentEnd, cycleIndex + 1);
        }
    }
}