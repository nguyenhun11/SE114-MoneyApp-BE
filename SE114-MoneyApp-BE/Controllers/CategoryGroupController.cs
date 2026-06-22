using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SE114_MoneyApp_BE.Controllers.Base;
using SE114_MoneyApp_BE.Data;
using SE114_MoneyApp_BE.DTOs.Category;
using SE114_MoneyApp_BE.Models;
using System.Diagnostics;
using System.Linq.Expressions;

namespace SE114_MoneyApp_BE.Controllers
{
    [Route("api/[controller]")]
    public class CategoryGroupController : AuthorizeControllerBase
    {
        public CategoryGroupController(AppDbContext context, IMemoryCache cache) : base(context, cache) { }

        private static Expression<Func<CategoryGroup, CategoryGroupResponse>> MapToCategoryGroupResponse = g => new CategoryGroupResponse
        {
            Id = g.Id,
            GroupName = g.GroupName,
            Type = g.Type,
            SortingOrder = g.SortingOrder,
            CreatedAt = DateTime.SpecifyKind(g.CreatedAt, DateTimeKind.Utc),
            LastUpdatedAt = DateTime.SpecifyKind(g.LastUpdatedAt, DateTimeKind.Utc)
        };

        private async Task<ActionResult<List<CategoryGroupResponse>>> GetCategoryGroups(CategoryType? type)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success)
            {
                return Unauthorized(new { message });
            }

            var categoryGroups = await _context.CategoryGroups
                .Where(g => g.UserId == userId && g.IsActive && (!type.HasValue || g.Type == type.Value))
                .OrderBy(g => g.SortingOrder)
                .Select(MapToCategoryGroupResponse)
                .ToListAsync();

            return Ok(categoryGroups);
        }
        /// <summary>
        /// Danh sách nhóm hạng mục chi tiêu
        /// </summary>
        /// <returns></returns>
        [HttpGet("expense")]
        public async Task<ActionResult<List<CategoryGroupResponse>>> GetExpenseCategoryGroups()
        {
            return await GetCategoryGroups(CategoryType.Expense);
        }
        /// <summary>
        /// Danh sách nhóm hạng mục thu nhập
        /// </summary>
        /// <returns></returns>
        [HttpGet("income")]
        public async Task<ActionResult<List<CategoryGroupResponse>>> GetIncomeCategoryGroups()
        {
            return await GetCategoryGroups(CategoryType.Income);
        }

        private async Task<int> NormalizeAndGetNextSortingOrderAsync(int userId, CategoryType type)
        {
            var groups = await _context.CategoryGroups
                .Where(a => a.UserId == userId && a.Type == type && a.IsActive)
                .OrderBy(a => a.SortingOrder)
                .ThenBy(a => a.GroupName)
                .ToListAsync();

            bool isChanged = false;
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].SortingOrder != i)
                {
                    groups[i].SortingOrder = i;
                    isChanged = true;
                }
            }

            if (isChanged) { await _context.SaveChangesAsync(); }
            return groups.Count;
        }

        private async Task<ActionResult<CategoryGroupResponse>> CreateCategoryGroup(CategoryType? type, CategoryGroupRequest request)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success)
            {
                return Unauthorized(new { message });
            }

            CategoryGroup newGroup = new CategoryGroup
            {
                UserId = userId,
                GroupName = request.GroupName,
                Type = type ?? CategoryType.Expense,
                SortingOrder = await NormalizeAndGetNextSortingOrderAsync(userId, type ?? CategoryType.Expense),
            };

            _context.CategoryGroups.Add(newGroup);
            await _context.SaveChangesAsync();

            var response = new CategoryGroupResponse
            {
                Id = newGroup.Id,
                GroupName = newGroup.GroupName,
                Type = newGroup.Type,
                SortingOrder = newGroup.SortingOrder,
                CreatedAt = DateTime.SpecifyKind(newGroup.CreatedAt, DateTimeKind.Utc),
                LastUpdatedAt = DateTime.SpecifyKind(newGroup.LastUpdatedAt, DateTimeKind.Utc)
            };
            return Ok(response);
        }

        /// <summary>
        /// Tạo nhóm hạng mục chi tiêu
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("expense")]
        public async Task<ActionResult<CategoryGroupResponse>> CreateExpenseCategoryGroup(CategoryGroupRequest request)
        {
            return await CreateCategoryGroup(CategoryType.Expense, request);
        }
        /// <summary>
        /// Tạo nhóm hạng mục thu nhập
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("income")]
        public async Task<ActionResult<CategoryGroupResponse>> CreateIncomeCategoryGroup(CategoryGroupRequest request)
        {
            return await CreateCategoryGroup(CategoryType.Income, request);
        }

        /// <summary>
        /// Đổi tên nhóm hạng mục
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateCategoryGroupName(Guid id, [FromBody] CategoryGroupRequest request)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) { return Unauthorized(new { message }); }

            var group = await _context.CategoryGroups.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId && g.IsActive);
            if (group == null)
            {
                return NotFound(new { message = "Category group not found" });
            }

            group.GroupName = request.GroupName;
            group.LastUpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Category group updated successfully" });
        }

        /// <summary>
        /// Đổi thứ tự nhóm hạng mục
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("reorder/{id:guid}")]
        public async Task<IActionResult> ReorderCategoryGroup(Guid id, [FromBody] ReorderCategoryGroupRequest request)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) { return Unauthorized(new { message }); }
            var group = await _context.CategoryGroups.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId && g.IsActive);
            if (group == null)
            {
                return NotFound(new { message = "Category group not found" });
            }
            var groups = await _context.CategoryGroups
                .Where(g => g.UserId == userId && g.Type == group.Type && g.IsActive)
                .OrderBy(g => g.SortingOrder)
                .ToListAsync();

            int newOrder = request.NewOrder;
            if (newOrder < 0 || newOrder >= groups.Count)
            {
                return BadRequest(new { message = "Invalid new order index" });
            }
            groups.Remove(group);
            groups.Insert(newOrder, group);
            for (int i = 0; i < groups.Count; i++)
            {
                groups[i].SortingOrder = i;
            }
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Category group reordered successfully" });
        }

        /// <summary>
        /// Xóa nhóm hạng mục
        /// </summary>
        /// <param name="id"></param>
        /// <param name="mode">delete, move</param>
        /// <param name="fallbackGroupId"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategoryGroup(Guid id,
            [FromQuery] string? mode = "delete",
            [FromQuery] Guid? fallbackGroupId = null)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) { return Unauthorized(new { message }); }

            var group = await _context.CategoryGroups.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId && g.IsActive);
            if (group == null)
            {
                return NotFound(new { message = "Category group not found" });
            }

            var categories = await _context.Categories.Where(c => c.CategoryGroupId == id && c.UserId == userId && c.IsActive).ToListAsync();

            if (mode == "move")
            {
                if (!fallbackGroupId.HasValue || fallbackGroupId == group.Id)
                {
                    return BadRequest(new { message = "Fallback group ID is required and must be different from current group" });
                }

                var fallbackCategoryGroup = await _context.CategoryGroups.FirstOrDefaultAsync(g => g.Id == fallbackGroupId.Value && g.UserId == userId && g.IsActive);
                if (fallbackCategoryGroup == null)
                {
                    return NotFound(new { message = "Fallback category group not found" });
                }

                if (fallbackCategoryGroup.Type != group.Type)
                {
                    return BadRequest(new { message = "Cannot move categories to a group of a different type (Income/Expense)" });
                }

                foreach (var category in categories)
                {
                    category.CategoryGroupId = fallbackGroupId.Value;
                    category.LastUpdatedAt = DateTime.UtcNow; // Nhớ update giờ
                }
            }
            else
            {
                foreach (var category in categories)
                {
                    category.IsActive = false;
                    category.LastUpdatedAt = DateTime.UtcNow;
                }
            }

            // Xóa mềm nhóm cha
            group.IsActive = false;
            group.LastUpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await NormalizeAndGetNextSortingOrderAsync(userId, group.Type);

            return Ok(new { Message = "Category group deleted successfully" });
        }
    }
}
