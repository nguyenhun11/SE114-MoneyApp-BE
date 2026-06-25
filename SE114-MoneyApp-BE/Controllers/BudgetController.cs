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
                    CurrentCycleIndex = cycleIndex
                });
            }

            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<BudgetResponse>> CreateBudget(BudgetRequest request)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var budget = new Budget
            {
                UserId = userId,
                Amount = request.Amount,
                Period = request.Period,
                StartDate = request.StartDate,
                CreatedAt = DateTime.UtcNow
            };

            // LOGIC ÉP CHUẨN HOÁ 3 CẤP (Tuyệt đối không để chứa cả 2 ID)
            if (request.CategoryId.HasValue)
            {
                budget.CategoryId = request.CategoryId;
                budget.CategoryGroupId = null;
            }
            else if (request.CategoryGroupId.HasValue)
            {
                budget.CategoryId = null;
                budget.CategoryGroupId = request.CategoryGroupId;
            }
            else
            {
                budget.CategoryId = null;
                budget.CategoryGroupId = null;
            }

            _context.Budgets.Add(budget);
            await _context.SaveChangesAsync();

            await _gamificationService.OnBudgetSetup(userId);

            // Truy vấn lấy tên để trả về DTO cho đẹp
            string categoryName = "Ngân sách tổng";
            if (budget.CategoryId.HasValue)
            {
                var cat = await _context.Categories.FindAsync(budget.CategoryId.Value);
                categoryName = cat?.CategoryName ?? "Hạng mục";
            }
            else if (budget.CategoryGroupId.HasValue)
            {
                var grp = await _context.CategoryGroups.FindAsync(budget.CategoryGroupId.Value);
                categoryName = grp?.GroupName ?? "Nhóm";
            }

            // TRẢ VỀ DTO (Tránh lỗi 500 vòng lặp vô tận của Entity)
            var responseDto = new BudgetResponse
            {
                Id = budget.Id,
                CategoryGroupId = budget.CategoryGroupId,
                CategoryId = budget.CategoryId,
                CategoryName = categoryName,
                Amount = budget.Amount,
                UsedAmount = 0, // Mới tạo thì chưa tính toán làm gì cho nặng
                Period = budget.Period,
                StartDate = budget.StartDate,
                IsActive = budget.IsActive,
                CurrentCycleIndex = 1
            };

            return Ok(responseDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBudget(int id, BudgetRequest request)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var budget = await _context.Budgets.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId && b.IsActive);
            if (budget == null) return NotFound(new { Message = "Không tìm thấy ngân sách" });

            budget.Amount = request.Amount;
            budget.Period = request.Period;
            budget.StartDate = request.StartDate;

            // LOGIC ÉP CHUẨN HOÁ 3 CẤP CHO UPDATE
            if (request.CategoryId.HasValue)
            {
                budget.CategoryId = request.CategoryId;
                budget.CategoryGroupId = null;
            }
            else if (request.CategoryGroupId.HasValue)
            {
                budget.CategoryId = null;
                budget.CategoryGroupId = request.CategoryGroupId;
            }
            else
            {
                budget.CategoryId = null;
                budget.CategoryGroupId = null;
            }

            await _context.SaveChangesAsync();

            await _gamificationService.OnBudgetSetup(userId);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBudget(int id)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var budget = await _context.Budgets.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId && b.IsActive);
            if (budget == null) return NotFound(new { Message = "Không tìm thấy ngân sách" });

            budget.IsActive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

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
            int cycleIndex = 0;

            if (now < anchorDate)
            {
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
                    cycleIndex = daysSinceAnchor / 7;
                    currentStart = anchorDate.AddDays(cycleIndex * 7);
                    currentEnd = currentStart.AddDays(7);
                    break;

                case BudgetPeriod.Monthly:
                    cycleIndex = ((now.Year - anchorDate.Year) * 12) + now.Month - anchorDate.Month;
                    currentStart = anchorDate.AddMonths(cycleIndex);

                    if (currentStart > now)
                    {
                        currentStart = currentStart.AddMonths(-1);
                        cycleIndex--;
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

            return (currentStart, currentEnd, cycleIndex + 1);
        }
    }
}