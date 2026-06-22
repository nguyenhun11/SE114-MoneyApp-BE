using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SE114_MoneyApp_BE.Controllers.Base;
using SE114_MoneyApp_BE.Data;
using SE114_MoneyApp_BE.DTOs.Statistic;
using SE114_MoneyApp_BE.Models;

namespace SE114_MoneyApp_BE.Controllers
{
    public enum GroupByPeriod
    {
        Day = 0,
        Week = 1,
        Month = 2,
        Year = 3
    }

    [Route("api/[controller]")]
    public class StatisticController : AuthorizeControllerBase
    {
        public StatisticController(AppDbContext context, IMemoryCache cache) : base(context, cache) { }

        #region Helpers
        private string GetPeriodLabel(DateTime date, GroupByPeriod groupBy)
        {
            switch (groupBy)
            {
                case GroupByPeriod.Day: return date.ToString("dd/MM/yyyy");
                case GroupByPeriod.Week:
                    int weekOfYear = System.Globalization.ISOWeek.GetWeekOfYear(date);
                    return $"W{weekOfYear}-{date.Year}";
                case GroupByPeriod.Month: return date.ToString("MM/yyyy");
                case GroupByPeriod.Year: return date.ToString("yyyy");
                default: return date.ToString("dd/MM/yyyy");
            }
        }
        private (DateTime utcStart, DateTime utcEnd) GetUtcTimeRange(DateTime startDate, DateTime endDate, int timeZoneOffset)
        {
            DateTime localStart = startDate.AddHours(timeZoneOffset).Date;
            DateTime localEnd = endDate.AddHours(timeZoneOffset).Date;

            DateTime utcStart = DateTime.SpecifyKind(localStart.AddHours(-timeZoneOffset), DateTimeKind.Utc);
            DateTime utcEnd = DateTime.SpecifyKind(localEnd.AddDays(1).AddTicks(-1).AddHours(-timeZoneOffset), DateTimeKind.Utc);

            return (utcStart, utcEnd);
        }

        private async Task<ActionResult<List<CategoryPieChartDto>>> GetPieChartInternal(
            DateTime startDate,
            DateTime endDate,
            CategoryType type,
            int timeZoneOffset)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var (utcStart, utcEnd) = GetUtcTimeRange(startDate, endDate, timeZoneOffset);

            var query = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Account!.UserId == userId
                         && t.TransactionDate >= utcStart
                         && t.TransactionDate <= utcEnd
                         && t.Category!.CategoryGroup!.Type == type)
                .GroupBy(t => new { t.CategoryId, t.Category!.CategoryName, t.Category.ColorId, t.Category.IconId })
                .Select(g => new CategoryPieChartDto
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    ColorId = g.Key.ColorId,
                    IconId = g.Key.IconId,
                    TotalAmount = Math.Abs(g.Sum(t => t.BaseAmount))
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToListAsync();

            var totalAmount = query.Sum(x => x.TotalAmount);
            if (totalAmount > 0)
            {
                foreach (var item in query)
                {
                    item.Percentage = (double)Math.Round((item.TotalAmount / totalAmount) * 100, 2);
                }
            }

            return Ok(query);
        }

        private async Task<ActionResult<List<StackedBarChartDto>>> GetStackedBarChartInternal(
            DateTime? startDate,
            DateTime endDate,
            GroupByPeriod groupBy,
            CategoryType type,
            int timeZoneOffset)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            if (!startDate.HasValue)
            {
                var oldestTx = await _context.Transactions
                    .Include(t => t.Category)
                    .ThenInclude(c => c!.CategoryGroup)
                    .Where(t => t.Account!.UserId == userId && t.Category!.CategoryGroup!.Type == type)
                    .OrderBy(t => t.TransactionDate)
                    .FirstOrDefaultAsync();

                if (oldestTx == null) return Ok(new List<StackedBarChartDto>());
                startDate = oldestTx.TransactionDate.AddHours(timeZoneOffset).Date;
            }

            var (utcStart, utcEnd) = GetUtcTimeRange(startDate.Value, endDate, timeZoneOffset);

            var transactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Account!.UserId == userId
                         && t.TransactionDate >= utcStart
                         && t.TransactionDate <= utcEnd
                         && t.Category!.CategoryGroup!.Type == type)
                .ToListAsync();

            var stackedData = transactions
                .GroupBy(t => GetPeriodLabel(t.TransactionDate.AddHours(timeZoneOffset), groupBy))
                .Select(gDay => new StackedBarChartDto
                {
                    Period = gDay.Key,
                    CategoryBreakdowns = gDay
                        .GroupBy(t => new { t.CategoryId, t.Category!.CategoryName, t.Category.ColorId, t.Category.IconId })
                        .Select(gCat => new CategoryPieChartDto
                        {
                            CategoryId = gCat.Key.CategoryId,
                            CategoryName = gCat.Key.CategoryName,
                            ColorId = gCat.Key.ColorId,
                            IconId = gCat.Key.IconId,
                            TotalAmount = Math.Abs(gCat.Sum(t => t.BaseAmount)) // Luôn dùng trị tuyệt đối
                        }).ToList()
                })
                .OrderBy(x => transactions.First(t => GetPeriodLabel(t.TransactionDate.AddHours(timeZoneOffset), groupBy) == x.Period).TransactionDate)
                .ToList();

            return Ok(stackedData);
        }
        #endregion

        #region API Endpoints
        [HttpGet("pie-chart/expense")]
        public async Task<ActionResult<List<CategoryPieChartDto>>> GetExpensePieChart([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] int timeZoneOffset = 7)
        {
            return await GetPieChartInternal(startDate, endDate, CategoryType.Expense, timeZoneOffset);
        }

        [HttpGet("pie-chart/income")]
        public async Task<ActionResult<List<CategoryPieChartDto>>> GetIncomePieChart([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] int timeZoneOffset = 7)
        {
            return await GetPieChartInternal(startDate, endDate, CategoryType.Income, timeZoneOffset);
        }

        [HttpGet("stacked-bar-chart/expense")]
        public async Task<ActionResult<List<StackedBarChartDto>>> GetExpenseStackedBarChart([FromQuery] DateTime? startDate, [FromQuery] DateTime endDate, [FromQuery] GroupByPeriod groupBy = GroupByPeriod.Month, [FromQuery] int timeZoneOffset = 7)
        {
            return await GetStackedBarChartInternal(startDate, endDate, groupBy, CategoryType.Expense, timeZoneOffset);
        }

        [HttpGet("stacked-bar-chart/income")]
        public async Task<ActionResult<List<StackedBarChartDto>>> GetIncomeStackedBarChart([FromQuery] DateTime? startDate, [FromQuery] DateTime endDate, [FromQuery] GroupByPeriod groupBy = GroupByPeriod.Month, [FromQuery] int timeZoneOffset = 7)
        {
            return await GetStackedBarChartInternal(startDate, endDate, groupBy, CategoryType.Income, timeZoneOffset);
        }

        [HttpGet("bar-chart/cashflow")]
        public async Task<ActionResult<List<CashFlowBarChartDto>>> GetCashFlowBarChart([FromQuery] DateTime? startDate, [FromQuery] DateTime endDate, [FromQuery] GroupByPeriod groupBy = GroupByPeriod.Month, [FromQuery] int timeZoneOffset = 7)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            if (!startDate.HasValue)
            {
                var oldestTx = await _context.Transactions
                    .Where(t => t.Account!.UserId == userId)
                    .OrderBy(t => t.TransactionDate)
                    .FirstOrDefaultAsync();

                if (oldestTx == null) return Ok(new List<CashFlowBarChartDto>());
                startDate = oldestTx.TransactionDate.AddHours(timeZoneOffset).Date;
            }

            var (utcStart, utcEnd) = GetUtcTimeRange(startDate.Value, endDate, timeZoneOffset);

            var transactions = await _context.Transactions
                .Include(t => t.Category)
                .ThenInclude(c => c!.CategoryGroup)
                .Where(t => t.Account!.UserId == userId
                         && t.TransactionDate >= utcStart
                         && t.TransactionDate <= utcEnd)
                .ToListAsync();

            var cashFlow = transactions
                .GroupBy(t => GetPeriodLabel(t.TransactionDate.AddHours(timeZoneOffset), groupBy))
                .Select(g => new CashFlowBarChartDto
                {
                    Period = g.Key,
                    TotalIncome = Math.Abs(g.Where(t => t.Category!.CategoryGroup!.Type == CategoryType.Income).Sum(t => t.BaseAmount)),
                    TotalExpense = Math.Abs(g.Where(t => t.Category!.CategoryGroup!.Type == CategoryType.Expense).Sum(t => t.BaseAmount))
                })
                .OrderBy(x => transactions.First(t => GetPeriodLabel(t.TransactionDate.AddHours(timeZoneOffset), groupBy) == x.Period).TransactionDate)
                .ToList();

            return Ok(cashFlow);
        }
        #endregion
    }
}