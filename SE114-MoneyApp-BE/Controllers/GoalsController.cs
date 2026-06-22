using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SE114_MoneyApp_BE.Controllers.Base;
using SE114_MoneyApp_BE.Data;
using SE114_MoneyApp_BE.DTOs.Goal;
using SE114_MoneyApp_BE.Models;

namespace SE114_MoneyApp_BE.Controllers
{
    [Route("api/[controller]")]
    public class GoalsController : AuthorizeControllerBase
    {
        public GoalsController(AppDbContext context, IMemoryCache cache) : base(context, cache) { }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<GoalResponse>>> GetGoals()
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var goals = await _context.Goals
                .Where(g => g.UserId == userId && g.IsActive)
                .Select(g => new GoalResponse
                {
                    Id = g.Id,
                    Name = g.Name,
                    TargetAmount = g.TargetAmount,
                    CurrentAmount = g.CurrentAmount,
                    Deadline = DateTime.SpecifyKind(g.Deadline, DateTimeKind.Utc), // Ép kiểu UTC khi trả về
                    IconId = g.IconId,
                    ColorId = g.ColorId,
                    IsActive = g.IsActive
                })
                .ToListAsync();

            return Ok(goals);
        }

        [HttpPost]
        public async Task<ActionResult<GoalResponse>> CreateGoal([FromBody] GoalRequest request)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var goal = new Goal
            {
                UserId = userId,
                Name = request.Name,
                TargetAmount = request.TargetAmount,
                CurrentAmount = 0,
                Deadline = request.Deadline.ToUniversalTime(),
                IconId = request.IconId,
                ColorId = request.ColorId,
                IsActive = true
            };

            _context.Goals.Add(goal);
            await _context.SaveChangesAsync();

            var response = new GoalResponse
            {
                Id = goal.Id,
                Name = goal.Name,
                TargetAmount = goal.TargetAmount,
                CurrentAmount = goal.CurrentAmount,
                Deadline = goal.Deadline,
                IconId = goal.IconId,
                ColorId = goal.ColorId,
                IsActive = goal.IsActive
            };

            return CreatedAtAction(nameof(GetGoals), new { id = goal.Id }, response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGoal(int id, [FromBody] GoalRequest request)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var goal = await _context.Goals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

            if (goal == null)
            {
                return NotFound(new { Message = "Không tìm thấy mục tiêu hoặc bạn không có quyền chỉnh sửa" });
            }

            goal.Name = request.Name;
            goal.TargetAmount = request.TargetAmount;
            goal.Deadline = request.Deadline.ToUniversalTime();
            goal.IconId = request.IconId;
            goal.ColorId = request.ColorId;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Cập nhật mục tiêu thành công" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGoal(int id)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var goal = await _context.Goals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

            if (goal == null)
            {
                return NotFound(new { Message = "Không tìm thấy mục tiêu hoặc bạn không có quyền xóa" });
            }

            // Soft delete
            goal.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Xóa mục tiêu thành công" });
        }

        [HttpPost("{id}/deposit")]
        public async Task<IActionResult> DepositToGoal(int id, [FromBody] DepositRequest request)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var goal = await _context.Goals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId && g.IsActive);

            if (goal == null)
            {
                return NotFound(new { Message = "Không tìm thấy mục tiêu hoặc mục tiêu đã bị đóng" });
            }

            // Mặc định nạp tiền là thêm số ảo. Nếu sau này có yêu cầu trừ ví thì viết thêm logic ở đây
            goal.CurrentAmount += request.Amount;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Nạp tiền vào mục tiêu thành công",
                CurrentAmount = goal.CurrentAmount,
                Progress = goal.TargetAmount > 0 ? (goal.CurrentAmount / goal.TargetAmount) * 100 : 0
            });
        }
    }
}