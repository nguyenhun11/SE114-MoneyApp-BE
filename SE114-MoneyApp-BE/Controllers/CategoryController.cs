using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SE114_MoneyApp_BE.Controllers.Base;
using SE114_MoneyApp_BE.Data;
using SE114_MoneyApp_BE.DTOs.Category;
using SE114_MoneyApp_BE.Models;
using System.Linq.Expressions;
using System.Security.Claims;

namespace SE114_MoneyApp_BE.Controllers
{
    [Route("api/[controller]")]
    public class CategoryController : AuthorizeControllerBase
    {
        public CategoryController(AppDbContext context) : base(context) { }

        private static Expression<Func<Category, CategoryResponse>> MapToCategoryResponse = c => new CategoryResponse
        {
            Id = c.Id,
            CategoryName = c.CategoryName,
            Type = c.CategoryGroup!.Type,
            MonthlyTarget = c.MonthlyTarget,
            GroupId = c.GroupId,
            GroupName = c.CategoryGroup!.GroupName,
            ColorId = c.ColorId,
            IconId = c.IconId,
            SortingOrder = c.SortingOrder,
            CreatedAt = c.CreatedAt,
            LastUpdatedAt = c.LastUpdatedAt
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
        /// Chi tiết một hạng mục theo Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CategoryResponse>> GetCategoryById(Guid id)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success)
            {
                return Unauthorized(new { Message = message });
            }
            var category = await _context.Categories
                .Where(c => c.Id == id && c.UserId == userId && c.IsActive)
                .Select(MapToCategoryResponse)
                .FirstOrDefaultAsync();

            if (category == null)
            {
                return NotFound(new
                {
                    Message = "Không tìm thấy hạng mục"
                });
            }
            return Ok(category);
        }

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
                .Where(c => c.GroupId == groupId
                           && c.IsActive
                           && c.UserId == userId)
                .OrderBy(c => c.SortingOrder)
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
                .Where(g => g.Id == request.GroupId
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

            int nextOrder = await NormalizeAndGetNextSortingOrderAsync(userId, request.GroupId);

            var category = new Category
            {
                UserId = userId,
                GroupId = request.GroupId,
                CategoryName = request.CategoryName,
                MonthlyTarget = request.MonthlyTarget,
                ColorId = request.ColorId,
                IconId = request.IconId,
                SortingOrder = nextOrder
            };

            _context.Categories.Add(category);
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
                .Include(c => c.CategoryGroup) // Cần không?
                .Where(c => c.Id == id
                            && c.UserId == userId
                            && c.CategoryGroup!.Type == type
                            && c.IsActive)
                .FirstOrDefaultAsync();

            if (category == null)
            {
                return NotFound(new { Message = "Không tìm thấy danh mục hoặc sai loại danh mục." });
            }

            if (category.GroupId != request.GroupId)
            {
                var newGroup = await _context.CategoryGroups
                                    .FirstOrDefaultAsync(g => g.Id == request.GroupId 
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

                category.GroupId = request.GroupId;
                category.SortingOrder = await NormalizeAndGetNextSortingOrderAsync(userId, request.GroupId);
            }

            category.CategoryName = request.CategoryName;
            category.MonthlyTarget = request.MonthlyTarget;
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
                .Where(c => c.UserId == userId && c.GroupId == targetCategory.GroupId && c.IsActive)
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
                            t.Account!.Balance += t.Amount;
                        else
                            t.Account!.Balance -= t.Amount;
                    }
                    _context.Transactions.RemoveRange(relatedTransactions);
                    break;

                case "soft_delete":
                default:
                    break;
            }
            
            // Luôn xóa mềm
            categoryToDelete.IsActive = false;
            categoryToDelete.LastUpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await NormalizeAndGetNextSortingOrderAsync(userId, categoryToDelete.GroupId);
            return Ok(new { Message = "Xóa danh mục thành công" });
        }
        #endregion

        private async Task<int> NormalizeAndGetNextSortingOrderAsync(int userId, Guid groupId)
        {
            var categories = await _context.Categories
                .Where(c => c.UserId == userId && c.GroupId == groupId && c.IsActive)
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

    }
}
