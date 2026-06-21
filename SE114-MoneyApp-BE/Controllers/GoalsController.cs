using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SE114_MoneyApp_BE.Data;
using SE114_MoneyApp_BE.DTOs.Goal;
using SE114_MoneyApp_BE.Models;
using System.Security.Claims;

namespace SE114_MoneyApp_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GoalsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public GoalsController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                throw new UnauthorizedAccessException("Không thể xác định danh tính người dùng");
            }
            return userId;
        }

        // GET: api/goals
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GoalResponse>>> GetGoals()
        {
            int userId = GetUserId();
            var goals = await _context.Goals
                .Where(g => g.UserId == userId && g.IsActive)
                .Select(g => new GoalResponse
                {
                    Id = g.Id,
                    Name = g.Name,
                    TargetAmount = g.TargetAmount,
                    CurrentAmount = g.CurrentAmount,
                    Deadline = g.Deadline,
                    IconId = g.IconId,
                    ColorId = g.ColorId,
                    IsActive = g.IsActive
                })
                .ToListAsync();

            return Ok(goals);
        }

        // POST: api/goals
        [HttpPost]
        public async Task<ActionResult<GoalResponse>> CreateGoal([FromBody] GoalRequest request)
        {
            int userId = GetUserId();

            var goal = new Goal
            {
                UserId = userId,
                Name = request.Name,
                TargetAmount = request.TargetAmount,
                CurrentAmount = 0,
                Deadline = request.Deadline,
                IconId = request.IconId,
                ColorId = request.ColorId
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

        // PUT: api/goals/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGoal(int id, [FromBody] GoalRequest request)
        {
            int userId = GetUserId();
            var goal = await _context.Goals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

            if (goal == null)
            {
                return NotFound(new { Message = "Không tìm thấy mục tiêu hoặc bạn không có quyền chỉnh sửa" });
            }

            goal.Name = request.Name;
            goal.TargetAmount = request.TargetAmount;
            goal.Deadline = request.Deadline;
            goal.IconId = request.IconId;
            goal.ColorId = request.ColorId;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Cập nhật mục tiêu thành công" });
        }

        // DELETE: api/goals/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGoal(int id)
        {
            int userId = GetUserId();
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

        // POST: api/goals/{id}/deposit
        [HttpPost("{id}/deposit")]
        public async Task<IActionResult> DepositToGoal(int id, [FromBody] DepositRequest request)
        {
            int userId = GetUserId();
            var goal = await _context.Goals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId && g.IsActive);

            if (goal == null)
            {
                return NotFound(new { Message = "Không tìm thấy mục tiêu hoặc mục tiêu đã bị đóng" });
            }

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
