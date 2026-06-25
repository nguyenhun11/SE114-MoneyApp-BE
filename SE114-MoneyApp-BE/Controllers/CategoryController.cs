using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SE114_MoneyApp_BE.Controllers.Base;
using SE114_MoneyApp_BE.Data;
using SE114_MoneyApp_BE.DTOs.Category;
using SE114_MoneyApp_BE.DTOs.Budget;
using SE114_MoneyApp_BE.Models;
using System.Linq.Expressions;
using System.Security.Claims;

namespace SE114_MoneyApp_BE.Controllers
{
    [Route("api/[controller]")]
    public class CategoryController : AuthorizeControllerBase
    {
        public CategoryController(AppDbContext context, IMemoryCache cache) : base(context, cache) { }

        private static Expression<Func<Category, CategoryResponse>> MapToCategoryResponse = c => new CategoryResponse
        {
            Id = c.Id,
            CategoryName = c.CategoryName,
            Type = c.CategoryGroup!.Type,
            CategoryGroupId = c.CategoryGroupId,
            GroupName = c.CategoryGroup!.GroupName,
            ColorId = c.ColorId,
            IconId = c.IconId,
            SortingOrder = c.SortingOrder,
            CreatedAt = DateTime.SpecifyKind(c.CreatedAt, DateTimeKind.Utc),
            LastUpdatedAt = DateTime.SpecifyKind(c.LastUpdatedAt, DateTimeKind.Utc)
        };

        #region GET
        private async Task<ActionResult<List<CategoryResponse>>> GetCategories(CategoryType type)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success)
            {
                return Unauthorized(new { Message = message });
            }

            var categories = await _context.Categories
                .Where(c => c.CategoryGroup!.Type == type
                    && c.IsActive && c.CategoryGroup!.IsActive
                    && c.UserId == userId)
                .OrderBy(c => c.CategoryGroup!.SortingOrder)
                .ThenBy(c => c.SortingOrder)
                .Select(MapToCategoryResponse)
                .ToListAsync();

            return Ok(categories);
        }

        // GET: api/Category/expense/5
        /// <summary>
        /// Danh sách hạng mục chi tiêu
        /// </summary>
        /// <returns></returns>
        [HttpGet("expense")]
        public async Task<ActionResult<List<CategoryResponse>>> GetExpenseCategories()
        {
            return await GetCategories(CategoryType.Expense);
        }

        // GET: api/Category/income/5
        /// <summary>
        /// Danh sách hạng mục thu nhập
        /// </summary>
        /// <returns></returns>
        [HttpGet("income")]
        public async Task<ActionResult<List<CategoryResponse>>> GetIncomeCategories()
        {
            return await GetCategories(CategoryType.Income);
        }

        // GET: api/Category/...
        /// <summary>
        /// Chi tiết một hạng mục theo Id (Có đính kèm danh sách Ngân sách)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CategoryResponse>> GetCategoryById(Guid id)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            // 1. Lấy thông tin cơ bản của Category
            var categoryResponse = await _context.Categories
                .Where(c => c.Id == id && c.UserId == userId && c.IsActive)
                .Select(MapToCategoryResponse)
                .FirstOrDefaultAsync();

            if (categoryResponse == null) return NotFound(new { Message = "Không tìm thấy hạng mục" });

            // 2. Kéo raw data Ngân sách từ DB lên
            var rawBudgets = await _context.Budgets
                .Where(b => b.CategoryId == id && b.IsActive)
                .ToListAsync();

            // 3. Chạy vòng lặp để TÍNH TOÁN thực tế cho từng Ngân sách (Đã đồng bộ lịch chuẩn)
            var activeBudgets = new List<BudgetResponse>();
            foreach (var b in rawBudgets)
            {
                // ĐÃ SỬA: Dùng hàm tính toán trả về chuỗi tên
                var (usedAmount, cycleName) = await CalculateUsedAmountAndCycleName(b);

                activeBudgets.Add(new BudgetResponse
                {
                    Id = b.Id,
                    CategoryId = b.CategoryId,
                    CategoryName = categoryResponse.CategoryName,
                    Amount = b.Amount,
                    UsedAmount = usedAmount,
                    Period = b.Period,
                    IsActive = b.IsActive,
                    CycleName = cycleName // ĐÃ SỬA: Gán tên chu kỳ (VD: "Tháng 6/2026")
                });
            }

            // 4. Đính kèm vào kết quả trả về
            categoryResponse.ActiveBudgets = activeBudgets;

            return Ok(categoryResponse);
        }

        /// <summary>
        /// Lấy hạng mục trong một nhóm
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        [HttpGet("group/{groupId:guid}")]
        public async Task<ActionResult<List<CategoryResponse>>> GetCategoriesInGroup(Guid groupId)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var group = await _context.CategoryGroups
                .Where(g => g.Id == groupId
                           && g.IsActive
                           && g.UserId == userId)
                .FirstOrDefaultAsync();
            if (group == null)
            {
                return NotFound(new { Message = "Không tìm thấy nhóm" });
            }

            var categories = await _context.Categories
                .Where(c => c.CategoryGroupId == groupId
                           && c.IsActive
                           && c.UserId == userId)
                .OrderBy(c => c.SortingOrder)
                .Select(MapToCategoryResponse)
                .ToListAsync();

            return Ok(categories);
        }
        #endregion

        #region POST
        private async Task<ActionResult<CategoryResponse>> CreateCategory(CategoryRequest request, CategoryType type, string successMessage)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success)
            {
                return Unauthorized(new { Message = message });
            }

            var group = await _context.CategoryGroups
                .Where(g => g.Id == request.CategoryGroupId
                             && g.IsActive
                             && g.UserId == userId)
                .FirstOrDefaultAsync();
            if (group == null)
            {
                return NotFound(new { Message = "Không tìm thấy Nhóm hạng mục." });
            }
            if (group.Type != type)
            {
                return BadRequest(new { Message = "Nhóm này không thuộc loại Thu/Chi đang tạo." });
            }

            int nextOrder = await NormalizeAndGetNextSortingOrderAsync(userId, request.CategoryGroupId);

            var category = new Category
            {
                UserId = userId,
                CategoryGroupId = request.CategoryGroupId,
                CategoryGroup = group,
                CategoryName = request.CategoryName,
                ColorId = request.ColorId,
                IconId = request.IconId,
                SortingOrder = nextOrder
            };

            _context.Categories.Add(category);

            if (request.BudgetSetup != null && request.BudgetSetup.Amount > 0)
            {
                var budget = new Budget
                {
                    UserId = userId,
                    CategoryId = category.Id, // Ép cứng ID của hạng mục vừa tạo
                    CategoryGroupId = category.CategoryGroupId,
                    Amount = request.BudgetSetup.Amount,
                    Period = request.BudgetSetup.Period,
                    StartDate = DateTime.UtcNow, // Gán tạm để khớp CSDL, logic tính toán không dùng đến nữa
                    CreatedAt = DateTime.UtcNow
                };
                _context.Budgets.Add(budget);
            }

            await _context.SaveChangesAsync();

            var response = MapToCategoryResponse.Compile().Invoke(category);

            return Ok(response);
        }

        // POST: api/Category/expense
        /// <summary>
        /// Tạo hạng mục chi tiêu
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("expense")]
        public async Task<ActionResult<CategoryResponse>> CreateExpenseCategory([FromBody] CategoryRequest request)
        {
            return await CreateCategory(request, CategoryType.Expense, "Tạo hạng mục chi tiêu thành công");
        }

        // POST: api/Category/income
        /// <summary>
        /// Tạo hạng mục thu nhập
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("income")]
        public async Task<ActionResult<CategoryResponse>> CreateIncomeCategory([FromBody] CategoryRequest request)
        {
            return await CreateCategory(request, CategoryType.Income, "Tạo hạng mục thu nhập thành công");
        }
        #endregion

        #region PUT
        private async Task<IActionResult> UpdateCategoryInternal(Guid id, CategoryRequest request, CategoryType type)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success)
            {
                return Unauthorized(new { Message = message });
            }

            var category = await _context.Categories
                .Where(c => c.Id == id
                             && c.UserId == userId
                             && c.CategoryGroup!.Type == type
                             && c.IsActive)
                .FirstOrDefaultAsync();

            if (category == null)
            {
                return NotFound(new { Message = "Không tìm thấy danh mục hoặc sai loại danh mục." });
            }

            if (category.CategoryGroupId != request.CategoryGroupId)
            {
                var newGroup = await _context.CategoryGroups
                                    .FirstOrDefaultAsync(g => g.Id == request.CategoryGroupId
                                                            && g.UserId == userId
                                                            && g.IsActive
                                                            && g.Type == type);
                if (newGroup == null)
                {
                    return BadRequest(new
                    {
                        Message = "Nhóm không hợp lệ"
                    });
                }

                category.CategoryGroupId = request.CategoryGroupId;
                category.SortingOrder = await NormalizeAndGetNextSortingOrderAsync(userId, request.CategoryGroupId);

                var linkedBudgets = await _context.Budgets.Where(b => b.CategoryId == id && b.IsActive).ToListAsync();
                foreach (var budget in linkedBudgets)
                {
                    budget.CategoryGroupId = request.CategoryGroupId;
                }
            }

            category.CategoryName = request.CategoryName;
            category.ColorId = request.ColorId;
            category.IconId = request.IconId;
            category.LastUpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Cập nhật thông tin danh mục thành công" });
        }

        /// <summary>
        /// Đổi thông tin hạng mục chi tiêu
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("expense/{id:guid}")]
        [Authorize]
        public async Task<IActionResult> UpdateExpenseCategory(Guid id, [FromBody] CategoryRequest request)
        {
            return await UpdateCategoryInternal(id, request, CategoryType.Expense);
        }

        /// <summary>
        /// Đổi thông tin hạng mục thu nhập
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("income/{id:guid}")]
        [Authorize]
        public async Task<IActionResult> UpdateIncomeCategory(Guid id, [FromBody] CategoryRequest request)
        {
            return await UpdateCategoryInternal(id, request, CategoryType.Income);
        }

        private async Task<IActionResult> ReorderCategoryInternal(Guid id, ReorderCategoryRequest request, CategoryType type)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var targetCategory = await _context.Categories
                .Include(c => c.CategoryGroup)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && c.IsActive);

            if (targetCategory == null || targetCategory.CategoryGroup!.Type != type)
                return NotFound(new { Message = "Không tìm thấy danh mục hoặc sai loại." });

            int newOrder = request.NewOrder;

            var categories = await _context.Categories
                .Where(c => c.UserId == userId && c.CategoryGroupId == targetCategory.CategoryGroupId && c.IsActive)
                .OrderBy(c => c.SortingOrder)
                .ThenBy(c => c.CategoryName)
                .ToListAsync();

            if (newOrder < 0) newOrder = 0;
            if (newOrder >= categories.Count) newOrder = categories.Count - 1;

            categories.Remove(targetCategory);
            categories.Insert(newOrder, targetCategory);

            for (int i = 0; i < categories.Count; i++)
            {
                categories[i].SortingOrder = i;
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Cập nhật vị trí thành công" });
        }

        /// <summary>
        /// Đổi vị trí hạng mục chi tiêu
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("expense/reorder/{id:guid}")]
        [Authorize]
        public async Task<IActionResult> ReorderExpenseCategory(Guid id, [FromBody] ReorderCategoryRequest request)
        {
            return await ReorderCategoryInternal(id, request, CategoryType.Expense);
        }

        /// <summary>
        /// Đổi vị trí hạng mục thu nhập
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("income/reorder/{id:guid}")]
        [Authorize]
        public async Task<IActionResult> ReorderIncomeCategory(Guid id, [FromBody] ReorderCategoryRequest request)
        {
            return await ReorderCategoryInternal(id, request, CategoryType.Income);
        }
        #endregion

        #region DELETE
        // DELETE: api/Category/{id}
        /// <summary>
        /// Xóa danh mục với 3 tùy chọn xử lý giao dịch cũ
        /// </summary>
        /// <param name="id">Id của danh mục cần xóa</param>
        /// <param name="mode">"soft_delete" (mặc định), "delete_all", hoặc "move"</param>
        /// <param name="fallbackCategoryId">Id của danh mục dự phòng (bắt buộc nếu mode="move")</param>
        /// <returns></returns>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteCategory(
            Guid id,
            [FromQuery] string mode = "soft_delete",
            [FromQuery] Guid? fallbackCategoryId = null)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            // 1. TÌM DANH MỤC CẦN XÓA
            var categoryToDelete = await _context.Categories
                .Include(c => c.CategoryGroup)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && c.IsActive);

            if (categoryToDelete == null)
            {
                return NotFound(new { Message = "Không tìm thấy danh mục hoặc bạn không có quyền xóa." });
            }

            // 2. XỬ LÝ CÁC GIAO DỊCH LIÊN QUAN THEO TỪNG CHẾ ĐỘ
            var relatedTransactions = await _context.Transactions
                .Include(t => t.Account)
                .Where(t => t.CategoryId == id)
                .ToListAsync();

            switch (mode.ToLower())
            {
                case "move":
                    if (!fallbackCategoryId.HasValue)
                    {
                        return BadRequest(new { Message = "Vui lòng cung cấp ID danh mục dự phòng để chuyển giao dịch." });
                    }

                    var fallbackCategory = await _context.Categories
                        .FirstOrDefaultAsync(c => c.Id == fallbackCategoryId.Value
                                                && c.UserId == userId
                                                && c.IsActive);

                    if (fallbackCategory == null || fallbackCategory.CategoryGroup!.Type != categoryToDelete.CategoryGroup!.Type)
                    {
                        return BadRequest(new { Message = "Danh mục dự phòng không hợp lệ hoặc không cùng loại (Thu/Chi)." });
                    }

                    // Đổi CategoryId của tất cả giao dịch cũ sang cái mới
                    foreach (var t in relatedTransactions)
                    {
                        t.CategoryId = fallbackCategory.Id;
                        t.LastUpdatedAt = DateTime.UtcNow;
                    }
                    break;

                case "delete_all":
                    foreach (var t in relatedTransactions)
                    {
                        if (categoryToDelete.CategoryGroup!.Type == CategoryType.Expense)
                            t.Account!.Balance += t.BaseAmount;
                        else
                            t.Account!.Balance -= t.BaseAmount;
                    }
                    _context.Transactions.RemoveRange(relatedTransactions);
                    break;

                case "soft_delete":
                default:
                    break;
            }

            // Xóa mềm các Ngân sách liên kết với Hạng mục này
            var linkedBudgets = await _context.Budgets.Where(b => b.CategoryId == id && b.IsActive).ToListAsync();
            foreach (var budget in linkedBudgets)
            {
                budget.IsActive = false;
            }

            // Luôn xóa mềm hạng mục
            categoryToDelete.IsActive = false;
            categoryToDelete.LastUpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await NormalizeAndGetNextSortingOrderAsync(userId, categoryToDelete.CategoryGroupId);
            return Ok(new { Message = "Xóa danh mục thành công" });
        }
        #endregion

        private async Task<int> NormalizeAndGetNextSortingOrderAsync(int userId, Guid groupId)
        {
            var categories = await _context.Categories
                .Where(c => c.UserId == userId && c.CategoryGroupId == groupId && c.IsActive)
                .OrderBy(c => c.SortingOrder)
                .ThenBy(c => c.CategoryName)
                .ToListAsync();

            bool isChanged = false;
            for (int i = 0; i < categories.Count; i++)
            {
                if (categories[i].SortingOrder != i)
                {
                    categories[i].SortingOrder = i;
                    isChanged = true;
                }
            }

            if (isChanged) await _context.SaveChangesAsync();
            return categories.Count;
        }

        // =================================================================================
        // THUẬT TOÁN TÍNH TOÁN THEO LỊCH CHUẨN DÀNH CHO CATEGORY CONTROLLER
        // =================================================================================
        private async Task<(decimal usedAmount, string cycleName)> CalculateUsedAmountAndCycleName(Budget budget)
        {
            var (currentCycleStart, currentCycleEnd, cycleName) = GetCurrentCycle(budget.Period);

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
            return (usedAmount, cycleName);
        }

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