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
                .Where(b => b.UserId == userId && b.IsActive)
                .ToListAsync();

            var response = new List<BudgetResponse>();
            foreach (var budget in budgets)
            {
                decimal usedAmount = await CalculateUsedAmount(budget);
                response.Add(new BudgetResponse
                {
                    Id = budget.Id,
                    CategoryId = budget.CategoryId,
                    CategoryName = budget.Category?.CategoryName,
                    Amount = budget.Amount,
                    UsedAmount = usedAmount,
                    Period = budget.Period,
                    StartDate = budget.StartDate,
                    IsActive = budget.IsActive
                });
            }

            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<Budget>> CreateBudget(BudgetRequest request)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var budget = new Budget
            {
                UserId = userId,
                CategoryId = request.CategoryId,
                Amount = request.Amount,
                Period = request.Period,
                StartDate = request.StartDate,
                CreatedAt = DateTime.UtcNow
            };

            _context.Budgets.Add(budget);
            await _context.SaveChangesAsync();

            // Cập nhật tiến độ nhiệm vụ thiết lập ngân sách
            await _gamificationService.OnBudgetSetup(userId);

            return Ok(budget);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBudget(int id, BudgetRequest request)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var budget = await _context.Budgets.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
            if (budget == null) return NotFound();

            budget.Amount = request.Amount;
            budget.CategoryId = request.CategoryId;
            budget.Period = request.Period;
            budget.StartDate = request.StartDate;

            await _context.SaveChangesAsync();

            // Cập nhật tiến độ nhiệm vụ cập nhật ngân sách
            await _gamificationService.OnBudgetSetup(userId);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBudget(int id)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var budget = await _context.Budgets.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
            if (budget == null) return NotFound();

            budget.IsActive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<decimal> CalculateUsedAmount(Budget budget)
        {
            DateTime endDate;
            switch (budget.Period)
            {
                case BudgetPeriod.Weekly:
                    endDate = budget.StartDate.AddDays(7);
                    break;
                case BudgetPeriod.Monthly:
                    endDate = budget.StartDate.AddMonths(1);
                    break;
                case BudgetPeriod.Yearly:
                    endDate = budget.StartDate.AddYears(1);
                    break;
                default:
                    endDate = budget.StartDate.AddMonths(1);
                    break;
            }

            var query = _context.Transactions
                .Where(t => t.Account!.UserId == budget.UserId
                         && t.TransactionDate >= budget.StartDate
                         && t.TransactionDate < endDate);

            if (budget.CategoryId.HasValue)
            {
                query = query.Where(t => t.CategoryId == budget.CategoryId.Value);
            }
            else
            {
                // Nếu là ngân sách tổng, chỉ tính các giao dịch Expense
                query = query.Include(t => t.Category).ThenInclude(c => c!.CategoryGroup)
                             .Where(t => t.Category!.CategoryGroup!.Type == CategoryType.Expense);
            }

            return await query.SumAsync(t => Math.Abs(t.BaseAmount));
        }
    }
}
