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
                .Include(b => b.CategoryGroup) // Kéo theo Group
                .Where(b => b.UserId == userId && b.IsActive)
                .ToListAsync();

            var response = new List<BudgetResponse>();
            foreach (var budget in budgets)
            {
                // Tự động tính toán số tiền đã dùng trong chu kỳ HIỆN TẠI
                decimal usedAmount = await CalculateUsedAmount(budget);

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
                // ĐÃ SỬA: Hỗ trợ tạo cả 3 loại (Tổng, Nhóm, Hạng mục lẻ) từ API này
                CategoryGroupId = request.CategoryGroupId,
                CategoryId = request.CategoryId,
                Amount = request.Amount,
                Period = request.Period,
                StartDate = request.StartDate,
                CreatedAt = DateTime.UtcNow
            };

            _context.Budgets.Add(budget);
            await _context.SaveChangesAsync();

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
            budget.CategoryGroupId = request.CategoryGroupId;
            budget.CategoryId = request.CategoryId;
            budget.Period = request.Period;
            budget.StartDate = request.StartDate;

            await _context.SaveChangesAsync();

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

        // =================================================================================
        // THUẬT TOÁN TÍNH TOÁN LƯỢNG TIỀN ĐÃ DÙNG (CHUẨN XÁC THEO CHU KỲ HIỆN TẠI)
        // =================================================================================
        private async Task<decimal> CalculateUsedAmount(Budget budget)
        {
            // 1. Dùng hàm mới để tìm ra khoảng thời gian (Start -> End) của chu kỳ HIỆN TẠI
            var (currentCycleStart, currentCycleEnd) = GetCurrentCycle(budget.StartDate, budget.Period);

            var query = _context.Transactions
                .Where(t => t.Account!.UserId == budget.UserId
                         && t.TransactionDate >= currentCycleStart
                         && t.TransactionDate < currentCycleEnd);

            // 2. Lọc theo cấp bậc của Ngân sách
            if (budget.CategoryId.HasValue)
            {
                // Cấp 3: Ngân sách Hạng mục lẻ
                query = query.Where(t => t.CategoryId == budget.CategoryId.Value);
            }
            else if (budget.CategoryGroupId.HasValue)
            {
                // Cấp 2: Ngân sách Nhóm (Tính tất cả các Hạng mục con trong nhóm đó)
                query = query.Include(t => t.Category)
                             .Where(t => t.Category!.CategoryGroupId == budget.CategoryGroupId.Value);
            }
            else
            {
                // Cấp 1: Ngân sách Tổng (Tính toàn bộ các giao dịch Chi tiêu)
                query = query.Include(t => t.Category).ThenInclude(c => c!.CategoryGroup)
                             .Where(t => t.Category!.CategoryGroup!.Type == CategoryType.Expense);
            }

            return await query.SumAsync(t => Math.Abs(t.BaseAmount));
        }

        // =================================================================================
        // THUẬT TOÁN TÍNH CHU KỲ (DỊCH MỐC MỎ NEO)
        // =================================================================================
        private (DateTime start, DateTime end) GetCurrentCycle(DateTime anchorDate, BudgetPeriod period)
        {
            var now = DateTime.UtcNow;
            DateTime currentStart = anchorDate;
            DateTime currentEnd;

            // Nếu thời điểm hiện tại còn chưa đến ngày kích hoạt ngân sách, 
            // coi như chu kỳ đầu tiên chính là mốc anchorDate.
            if (now < anchorDate)
            {
                switch (period)
                {
                    case BudgetPeriod.Weekly: return (anchorDate, anchorDate.AddDays(7));
                    case BudgetPeriod.Yearly: return (anchorDate, anchorDate.AddYears(1));
                    default: return (anchorDate, anchorDate.AddMonths(1));
                }
            }

            switch (period)
            {
                case BudgetPeriod.Weekly:
                    int daysSinceAnchor = (now - anchorDate).Days;
                    int weeksPassed = daysSinceAnchor / 7;
                    currentStart = anchorDate.AddDays(weeksPassed * 7);
                    currentEnd = currentStart.AddDays(7);
                    break;

                case BudgetPeriod.Monthly:
                    // Tính số tháng đã trôi qua kể từ ngày tạo
                    int monthsPassed = ((now.Year - anchorDate.Year) * 12) + now.Month - anchorDate.Month;
                    currentStart = anchorDate.AddMonths(monthsPassed);

                    // Nếu ngày mỏ neo lớn hơn ngày hiện tại trong tháng (Vd: Mỏ neo 25, hôm nay mới 15)
                    // -> Nghĩa là chu kỳ hiện tại phải lùi lại 1 tháng
                    if (currentStart > now)
                    {
                        currentStart = currentStart.AddMonths(-1);
                    }
                    currentEnd = currentStart.AddMonths(1);
                    break;

                case BudgetPeriod.Yearly:
                    int yearsPassed = now.Year - anchorDate.Year;
                    currentStart = anchorDate.AddYears(yearsPassed);
                    if (currentStart > now) currentStart = currentStart.AddYears(-1);
                    currentEnd = currentStart.AddYears(1);
                    break;

                default:
                    currentEnd = currentStart.AddMonths(1);
                    break;
            }

            return (currentStart, currentEnd);
        }
    }
}