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
            Type = c.Type,
            MonthlyTarget = c.MonthlyTarget,
            ColorId = c.ColorId,
            IconId = c.IconId,
            IsDefault = c.IsDefault,
            SortingOrder = c.SortingOrder,
            CreatedAt = c.CreatedAt,
            LastUpdatedAt = c.LastUpdatedAt
        };

        #region GET
        private async Task<ActionResult<List<CategoryResponse>>> GetCategories(Category.CategoryType type)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success)
            {
                return Unauthorized(new { Message = message });
            }

            var categories = await _context.Categories
                .Where(c => c.Type == type && c.UserId == userId && c.IsActive)
                .OrderByDescending(c => c.SortingOrder)
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
            var expenseCategories = await GetCategories(Category.CategoryType.Expense);

            return Ok(expenseCategories);
        }

        // GET: api/Category/income/5
        /// <summary>
        /// Danh sách hạng mục thu nhập
        /// </summary>
        /// <returns></returns>
        [HttpGet("income")]
        public async Task<ActionResult<List<CategoryResponse>>> GetIncomeCategories()
        {
            var expenseCategories = await GetCategories(Category.CategoryType.Income);

            return Ok(expenseCategories);
        }

        // GET: api/Category/...
        /// <summary>
        /// (*) Chi tiết một hạng mục theo Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CategoryResponse>> GetCategoryById(Guid id)
        {
            var category = await _context.Categories
                .Where(c => c.Id == id && c.IsActive)
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
        #endregion

        #region POST
        private async Task<IActionResult> CreateCategory(CategoryRequest request, Category.CategoryType type, string successMessage)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success)
            {
                return Unauthorized(new { Message = message });
            }

            var maxSortingOrder = await _context.Categories
                .Where(c => c.UserId == userId && c.Type == type && c.IsActive)
                .MaxAsync(c => (int?)c.SortingOrder) ?? 0;

            var category = new Category
            {
                CategoryName = request.CategoryName,
                MonthlyTarget = request.MonthlyTarget,
                ColorId = request.ColorId,
                IconId = request.IconId,

                UserId = userId,
                Type = type,
                SortingOrder = maxSortingOrder + 1
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = successMessage,
                Id = category.Id,
                SortingOrder = category.SortingOrder
            });
        }

        // POST: api/Category/expense
        /// <summary>
        /// Tạo hạng mục chi tiêu
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("expense")]
        [Authorize]
        public async Task<IActionResult> CreateExpenseCategory([FromBody] CategoryRequest request)
        {
            return await CreateCategory(request, Category.CategoryType.Expense, "Tạo hạng mục chi tiêu thành công");
        }

        // POST: api/Category/income
        /// <summary>
        /// Tạo hạng mục thu nhập
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("income")]
        [Authorize]
        public async Task<IActionResult> CreateIncomeCategory([FromBody] CategoryRequest request)
        {
            return await CreateCategory(request, Category.CategoryType.Income, "Tạo hạng mục thu nhập thành công");
        }
        #endregion

        #region PUT
        private async Task<IActionResult> UpdateCategoryInternal(Guid id, CategoryRequest request, Category.CategoryType type)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success)
            {
                return Unauthorized(new { Message = message });
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && c.Type == type && c.IsActive);

            if (category == null)
            {
                return NotFound(new { Message = "Không tìm thấy danh mục hoặc sai loại danh mục." });
            }
            if (category.IsDefault)
            {
                return BadRequest(new
                {
                    Message = "Không được sửa hạng mục mặc định"
                });
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
            return await UpdateCategoryInternal(id, request, Category.CategoryType.Expense);
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
            return await UpdateCategoryInternal(id, request, Category.CategoryType.Income);
        }

        private async Task<IActionResult> ReorderCategoryInternal(Guid id, ReorderCategoryRequest request, Category.CategoryType type)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success)
            {
                return Unauthorized(new { Message = message });
            }

            var categoryToMove = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && c.Type == type && c.IsActive);

            if (categoryToMove == null)
            {
                return NotFound(new { Message = "Không tìm thấy danh mục hoặc sai loại." });
            }

            int oldOrder = categoryToMove.SortingOrder;
            int newOrder = request.NewOrder;

            if (oldOrder == newOrder)
            {
                return Ok(new { Message = "Vị trí không thay đổi" });
            }

            // Lọc ra tập hợp cùng loại để chuẩn bị dồn
            var query = _context.Categories
                .Where(c => c.UserId == userId && c.Type == type && c.IsActive);

            if (newOrder < oldOrder)
            {
                // Kéo lên trên
                var itemsToShift = await query
                    .Where(c => c.SortingOrder >= newOrder && c.SortingOrder < oldOrder)
                    .ToListAsync();
                foreach (var item in itemsToShift) item.SortingOrder += 1;
            }
            else
            {
                // Kéo xuống dưới
                var itemsToShift = await query
                    .Where(c => c.SortingOrder > oldOrder && c.SortingOrder <= newOrder)
                    .ToListAsync();
                foreach (var item in itemsToShift) item.SortingOrder -= 1;
            }

            categoryToMove.SortingOrder = newOrder;
            categoryToMove.LastUpdatedAt = DateTime.UtcNow;

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
            return await ReorderCategoryInternal(id, request, Category.CategoryType.Expense);
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
            return await ReorderCategoryInternal(id, request, Category.CategoryType.Income);
        }
        #endregion

        #region DELETE
        //TODO: Xử lý xóa sau ki có giao dịch
        #endregion

    }
}
