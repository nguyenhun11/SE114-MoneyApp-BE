using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SE114_MoneyApp_BE.Controllers.Base;
using SE114_MoneyApp_BE.Data;
using SE114_MoneyApp_BE.DTOs.Dashboard;
using SE114_MoneyApp_BE.Models;

namespace SE114_MoneyApp_BE.Controllers
{
    [Route("api/[controller]")]
    public class DashboardController : AuthorizeControllerBase
    {
        public DashboardController(AppDbContext context, IMemoryCache cache) : base(context, cache)
        {
        }

        [HttpGet("overview")]
        public async Task<ActionResult<DashboardOverviewResponse>> GetOverview()
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var now = DateTime.UtcNow;
            var today = now.Date;
            var startOfCurrentMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var startOfLastMonth = startOfCurrentMonth.AddMonths(-1);

            var response = new DashboardOverviewResponse();

            // ==========================================
            // 1. THÔNG TIN USER (Streak & Check-in)
            // ==========================================
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                int displayStreak = user.DailyStreak;
                if (user.LastActiveDate.HasValue && user.LastActiveDate.Value.Date < today.AddDays(-1))
                {
                    displayStreak = 0; // Đứt chuỗi
                }

                response.UserSummary = new UserSummaryDto
                {
                    Name = user.Name,
                    ImageUrl = user.ImageUrl,
                    DailyStreak = displayStreak,
                    TodayCheckedIn = user.LastActiveDate.HasValue && user.LastActiveDate.Value.Date == today
                };
            }

            // ==========================================
            // 2. THÔNG TIN MONEY CITY
            // ==========================================
            var city = await _context.CityStates.FirstOrDefaultAsync(c => c.UserId == userId);
            if (city != null)
            {
                response.CitySummary = new CitySummaryDto
                {
                    Level = city.Level,
                    ProsperityPoints = city.ProsperityPoints,
                    StabilityPoints = city.StabilityPoints
                };
            }

            // ==========================================
            // 3. GIAO DỊCH GẦN ĐÂY NHẤT (Top 3)
            // ==========================================
            response.RecentTransactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Account!.UserId == userId)
                .OrderByDescending(t => t.TransactionDate)
                .Take(3)
                .Select(t => new RecentTransactionDto
                {
                    Id = t.Id,
                    CategoryName = t.Category!.CategoryName,
                    Amount = t.BaseAmount,
                    Type = t.Category.CategoryGroup!.Type.ToString(),
                    IconId = t.Category.IconId,
                    ColorId = t.Category.ColorId,
                    Date = t.TransactionDate
                })
                .ToListAsync();

            // ==========================================
            // 4. MỤC TIÊU SẮP VỀ ĐÍCH (Top 2 có % cao nhất)
            // ==========================================
            var activeGoals = await _context.Goals
                .Where(g => g.UserId == userId && g.IsActive && g.CurrentAmount < g.TargetAmount)
                .ToListAsync(); // Kéo về RAM vì công thức tính toán chia % khó parse trong SQL

            response.GoalHighlights = activeGoals
                .Select(g => new GoalHighlightDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    TargetAmount = g.TargetAmount,
                    CurrentAmount = g.CurrentAmount,
                    ProgressPercent = g.TargetAmount > 0 ? (int)((g.CurrentAmount / g.TargetAmount) * 100) : 0,
                    IconId = g.IconId,
                    ColorId = g.ColorId
                })
                .OrderByDescending(g => g.ProgressPercent)
                .Take(2)
                .ToList();

            // ==========================================
            // 5. NHIỆM VỤ ƯU TIÊN TRONG NGÀY (Top 2 chưa làm)
            // ==========================================
            response.PendingQuests = await _context.UserQuests
                .Include(uq => uq.Quest)
                .Where(uq => uq.UserId == userId && uq.CreatedAt.Date == today && !uq.IsCompleted)
                .Take(2)
                .Select(uq => new PendingQuestDto
                {
                    Id = uq.QuestId,
                    Title = uq.Quest!.Title,
                    CurrentProgress = uq.CurrentProgress,
                    Target = uq.Quest.Target
                })
                .ToListAsync();

            // ==========================================
            // 6. SMART INSIGHTS (So sánh Tháng này vs Tháng trước)
            // ==========================================
            var currentMonthExpense = await _context.Transactions
                .Where(t => t.Account!.UserId == userId && t.Category!.CategoryGroup!.Type == CategoryType.Expense && t.TransactionDate >= startOfCurrentMonth)
                .SumAsync(t => t.BaseAmount);

            var lastMonthExpense = await _context.Transactions
                .Where(t => t.Account!.UserId == userId && t.Category!.CategoryGroup!.Type == CategoryType.Expense && t.TransactionDate >= startOfLastMonth && t.TransactionDate < startOfCurrentMonth)
                .SumAsync(t => t.BaseAmount);

            var insights = new List<SmartInsightDto>();

            if (lastMonthExpense > 0)
            {
                decimal diffPercent = ((currentMonthExpense - lastMonthExpense) / lastMonthExpense) * 100;

                if (diffPercent > 20) // Tiêu nhiều hơn 20%
                {
                    insights.Add(new SmartInsightDto
                    {
                        Type = "DANGER",
                        Title = "Cảnh báo chi tiêu",
                        Message = $"Tháng này bạn đã tiêu nhiều hơn {Math.Round(diffPercent, 1)}% so với tháng trước. Hãy chú ý nhé!"
                    });
                }
                else if (diffPercent < -10) // Tiết kiệm hơn 10%
                {
                    insights.Add(new SmartInsightDto
                    {
                        Type = "SUCCESS",
                        Title = "Quản lý tuyệt vời",
                        Message = $"Bạn đang chi tiêu ít hơn {Math.Round(Math.Abs(diffPercent), 1)}% so với tháng trước. Tiếp tục phát huy nhé!"
                    });
                }
            }

            // Nếu không có insight nào nổi bật, thêm 1 câu khích lệ mặc định
            if (!insights.Any())
            {
                insights.Add(new SmartInsightDto
                {
                    Type = "INFO",
                    Title = "MoneyApp",
                    Message = "Hôm nay là một ngày tuyệt vời để ghi chép lại các khoản chi tiêu của bạn!"
                });
            }
            response.SmartInsights = insights;

            // ==========================================
            // 7. CẢNH BÁO NGÂN SÁCH (Chỉ lấy những cái > 80%)
            // ==========================================
            // Lấy danh sách ngân sách (Giả lập tính số tiền đã dùng trong tháng này)
            var activeBudgets = await _context.Budgets
                .Include(b => b.Category)
                .Include(b => b.CategoryGroup)
                .Where(b => b.UserId == userId && b.IsActive)
                .ToListAsync();

            var budgetAlerts = new List<BudgetAlertDto>();

            foreach (var budget in activeBudgets)
            {
                // Truy vấn tính tổng tiền đã tiêu cho ngân sách này trong tháng hiện tại
                var query = _context.Transactions
                    .Where(t => t.Account!.UserId == userId && t.TransactionDate >= startOfCurrentMonth);

                if (budget.CategoryId.HasValue)
                    query = query.Where(t => t.CategoryId == budget.CategoryId.Value);
                else if (budget.CategoryGroupId.HasValue)
                    query = query.Where(t => t.Category!.CategoryGroupId == budget.CategoryGroupId.Value);

                var usedAmount = await query.SumAsync(t => t.BaseAmount);
                decimal percent = budget.Amount > 0 ? (usedAmount / budget.Amount) * 100 : 0;

                if (percent >= 80) // Vượt ngưỡng báo động
                {
                    budgetAlerts.Add(new BudgetAlertDto
                    {
                        Id = budget.Id,
                        Name = budget.Category?.CategoryName ?? budget.CategoryGroup?.GroupName ?? "Tổng",
                        Amount = budget.Amount,
                        UsedAmount = usedAmount,
                        Percent = (int)percent,
                        Status = percent >= 100 ? "OVER" : "WARNING"
                    });
                }
            }

            // Chỉ lấy top 2 ngân sách nguy hiểm nhất đẩy ra trang chủ
            response.BudgetAlerts = budgetAlerts.OrderByDescending(b => b.Percent).Take(2).ToList();

            return Ok(response);
        }
    }
}
