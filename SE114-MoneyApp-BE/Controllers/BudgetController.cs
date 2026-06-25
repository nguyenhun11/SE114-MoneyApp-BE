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
                // ĐÃ SỬA: Lấy cycleName (Chuỗi văn bản) thay vì index
                var (usedAmount, cycleName) = await CalculateUsedAmountAndCycleName(budget);

                response.Add(new BudgetResponse
                {
                    Id = budget.Id,
                    CategoryGroupId = budget.CategoryGroupId,
                    CategoryId = budget.CategoryId,
                    CategoryName = budget.Category?.CategoryName ?? budget.CategoryGroup?.GroupName ?? "Ngân sách tổng",
                    Amount = budget.Amount,
                    UsedAmount = usedAmount,
                    Period = budget.Period,
                    IsActive = budget.IsActive,
                    CycleName = cycleName // Gán chuỗi tên chu kỳ vào đây
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
                StartDate = DateTime.UtcNow, // Gán tạm để không lỗi DB, ta không dùng đến nó nữa
                CreatedAt = DateTime.UtcNow
            };

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

            var (_, _, cycleName) = GetCurrentCycle(budget.Period);

            var responseDto = new BudgetResponse
            {
                Id = budget.Id,
                CategoryGroupId = budget.CategoryGroupId,
                CategoryId = budget.CategoryId,
                CategoryName = categoryName,
                Amount = budget.Amount,
                UsedAmount = 0,
                Period = budget.Period,
                IsActive = budget.IsActive,
                CycleName = cycleName
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
        // THUẬT TOÁN TÍNH TOÁN THEO LỊCH CHUẨN
        // =================================================================================
        private async Task<(decimal usedAmount, string cycleName)> CalculateUsedAmountAndCycleName(Budget budget)
        {
            var (currentCycleStart, currentCycleEnd, cycleName) = GetCurrentCycle(budget.Period);

            var query = _context.Transactions
                .Where(t => t.Account!.UserId == budget.UserId
                         && t.TransactionDate >= currentCycleStart
                         && t.TransactionDate < currentCycleEnd);

            if (budget.CategoryId.HasValue)
                query = query.Where(t => t.CategoryId == budget.CategoryId.Value);
            else if (budget.CategoryGroupId.HasValue)
                query = query.Include(t => t.Category).Where(t => t.Category!.CategoryGroupId == budget.CategoryGroupId.Value);
            else
                query = query.Include(t => t.Category).ThenInclude(c => c!.CategoryGroup).Where(t => t.Category!.CategoryGroup!.Type == CategoryType.Expense);

            decimal usedAmount = await query.SumAsync(t => Math.Abs(t.BaseAmount));
            return (usedAmount, cycleName);
        }

        // =================================================================================
        // THUẬT TOÁN ĐỒNG BỘ LỊCH TỰ ĐỘNG
        // =================================================================================
        private (DateTime start, DateTime end, string cycleName) GetCurrentCycle(BudgetPeriod period)
        {
            // Lấy thời gian hiện tại theo múi giờ UTC+7
            var now = DateTime.UtcNow.AddHours(7);
            DateTime start;
            DateTime end;
            string cycleName;

            switch (period)
            {
                case BudgetPeriod.Weekly:
                    int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                    start = now.Date.AddDays(-diff);
                    end = start.AddDays(7);
                    cycleName = $"Tuần này ({start:dd/MM} - {end.AddDays(-1):dd/MM})";
                    break;

                case BudgetPeriod.Yearly:
                    start = new DateTime(now.Year, 1, 1);
                    end = start.AddYears(1);
                    cycleName = $"Năm {now.Year}";
                    break;

                case BudgetPeriod.Monthly:
                default:
                    start = new DateTime(now.Year, now.Month, 1);
                    end = start.AddMonths(1);
                    cycleName = $"Tháng {now.Month}/{now.Year}";
                    break;
            }

            // Trả về UTC để truy vấn DB chính xác
            return (start.AddHours(-7), end.AddHours(-7), cycleName);
        }
    }
}