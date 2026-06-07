using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public StatisticController(AppDbContext context) : base(context) { }

        #region Helpers
        private string GetPeriodLabel(DateTime date, GroupByPeriod groupBy)
        {
            switch (groupBy)
            {
                case GroupByPeriod.Day:
                    return date.ToString("dd/MM/yyyy");
                case GroupByPeriod.Week:
                    int weekOfYear = System.Globalization.ISOWeek.GetWeekOfYear(date);
                    return $"W{weekOfYear}-{date.Year}";
                case GroupByPeriod.Month:
                    return date.ToString("MM/yyyy");
                case GroupByPeriod.Year:
                    return date.ToString("yyyy");
                default:
                    return date.ToString("dd/MM/yyyy");
            }
        }

        private async Task<ActionResult<List<CategoryPieChartDto>>> GetPieChartInternal(
            DateTime startDate,
            DateTime endDate,
            CategoryType type,
            int timeZoneOffset)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            // BƯỚC 1: Dịch khoảng thời gian sang chuẩn UTC để quét sạch giao dịch trong ngày
            var utcStart = startDate.Date.AddHours(-timeZoneOffset);
            var utcEnd = endDate.Date.AddDays(1).AddTicks(-1).AddHours(-timeZoneOffset);

            var query = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Account!.UserId == userId
                         && t.TransactionDate >= utcStart
                         && t.TransactionDate <= utcEnd
                         && t.Category!.CategoryGroup!.Type == type)
                .GroupBy(t => new { t.CategoryId, t.Category!.CategoryName, t.Category.ColorId })
                .Select(g => new CategoryPieChartDto
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    ColorId = g.Key.ColorId,
                    TotalAmount = g.Sum(t => t.Amount)
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
            DateTime startDate,
            DateTime endDate,
            GroupByPeriod groupBy,
            CategoryType type,
            int timeZoneOffset)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            // BƯỚC 1: Dịch khoảng thời gian sang chuẩn UTC
            var utcStart = startDate.Date.AddHours(-timeZoneOffset);
            var utcEnd = endDate.Date.AddDays(1).AddTicks(-1).AddHours(-timeZoneOffset);

            var transactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Account!.UserId == userId
                         && t.TransactionDate >= utcStart
                         && t.TransactionDate <= utcEnd
                         && t.Category!.CategoryGroup!.Type == type)
                .ToListAsync();

            // BƯỚC 2: Cộng lại giờ Local khi gom nhóm để hiện biểu đồ chuẩn
            var stackedData = transactions
                .GroupBy(t => GetPeriodLabel(t.TransactionDate.AddHours(timeZoneOffset), groupBy))
                .Select(gDay => new StackedBarChartDto
                {
                    Period = gDay.Key,
                    CategoryBreakdowns = gDay
                        .GroupBy(t => new { t.CategoryId, t.Category!.CategoryName, t.Category.ColorId })
                        .Select(gCat => new CategoryPieChartDto
                        {
                            CategoryId = gCat.Key.CategoryId,
                            CategoryName = gCat.Key.CategoryName,
                            ColorId = gCat.Key.ColorId,
                            TotalAmount = gCat.Sum(t => t.Amount)
                        }).ToList()
                })
                .OrderBy(x => transactions.First(t => GetPeriodLabel(t.TransactionDate.AddHours(timeZoneOffset), groupBy) == x.Period).TransactionDate)
                .ToList();

            return Ok(stackedData);
        }
        #endregion


        #region API Endpoints

        /// <summary>
        /// Biểu đồ tròn các hạng mục chi tiêu
        /// </summary>
        [HttpGet("pie-chart/expense")]
        public async Task<ActionResult<List<CategoryPieChartDto>>> GetExpensePieChart(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] int timeZoneOffset = 7) // Thêm biến nhận múi giờ từ Android
        {
            return await GetPieChartInternal(startDate, endDate, CategoryType.Expense, timeZoneOffset);
        }

        /// <summary>
        /// Biểu đồ tròn các hạng mục thu nhập
        /// </summary>
        [HttpGet("pie-chart/income")]
        public async Task<ActionResult<List<CategoryPieChartDto>>> GetIncomePieChart(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] int timeZoneOffset = 7)
        {
            return await GetPieChartInternal(startDate, endDate, CategoryType.Income, timeZoneOffset);
        }

        /// <summary>
        /// Biểu đồ cột chồng các chi tiêu
        /// </summary>
        [HttpGet("stacked-bar-chart/expense")]
        public async Task<ActionResult<List<StackedBarChartDto>>> GetExpenseStackedBarChart(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] GroupByPeriod groupBy = GroupByPeriod.Month,
            [FromQuery] int timeZoneOffset = 7)
        {
            return await GetStackedBarChartInternal(startDate, endDate, groupBy, CategoryType.Expense, timeZoneOffset);
        }

        /// <summary>
        /// Biểu đồ cột chồng các thu nhập
        /// </summary>
        [HttpGet("stacked-bar-chart/income")]
        public async Task<ActionResult<List<StackedBarChartDto>>> GetIncomeStackedBarChart(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] GroupByPeriod groupBy = GroupByPeriod.Month,
            [FromQuery] int timeZoneOffset = 7)
        {
            return await GetStackedBarChartInternal(startDate, endDate, groupBy, CategoryType.Income, timeZoneOffset);
        }

        /// <summary>
        /// Biểu đồ dòng tiền (Cơ cấu Thu/Chi và Hiệu số)
        /// </summary>
        [HttpGet("bar-chart/cashflow")]
        public async Task<ActionResult<List<CashFlowBarChartDto>>> GetCashFlowBarChart(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] GroupByPeriod groupBy = GroupByPeriod.Month,
            [FromQuery] int timeZoneOffset = 7) // Áp dụng đồng bộ cho Dòng tiền
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            // BƯỚC 1: Dịch sang UTC
            var utcStart = startDate.Date.AddHours(-timeZoneOffset);
            var utcEnd = endDate.Date.AddDays(1).AddTicks(-1).AddHours(-timeZoneOffset);

            var transactions = await _context.Transactions
                .Include(t => t.Category)
                .ThenInclude(c => c!.CategoryGroup)
                .Where(t => t.Account!.UserId == userId
                         && t.TransactionDate >= utcStart
                         && t.TransactionDate <= utcEnd)
                .ToListAsync();

            // BƯỚC 2: Cộng bù giờ Local khi gom nhóm
            var cashFlow = transactions
                .GroupBy(t => GetPeriodLabel(t.TransactionDate.AddHours(timeZoneOffset), groupBy))
                .Select(g => new CashFlowBarChartDto
                {
                    Period = g.Key,
                    TotalIncome = g.Where(t => t.Category!.CategoryGroup!.Type == CategoryType.Income).Sum(t => t.Amount),
                    TotalExpense = g.Where(t => t.Category!.CategoryGroup!.Type == CategoryType.Expense).Sum(t => t.Amount)
                })
                .OrderBy(x => transactions.First(t => GetPeriodLabel(t.TransactionDate.AddHours(timeZoneOffset), groupBy) == x.Period).TransactionDate)
                .ToList();

            return Ok(cashFlow);
        }

        #endregion
    }
}