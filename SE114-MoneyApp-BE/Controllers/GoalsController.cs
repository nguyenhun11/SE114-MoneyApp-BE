using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SE114_MoneyApp_BE.Controllers.Base;
using SE114_MoneyApp_BE.Data;
using SE114_MoneyApp_BE.DTOs.Goal;
using SE114_MoneyApp_BE.Models;
using SE114_MoneyApp_BE.Services;

namespace SE114_MoneyApp_BE.Controllers
{
    [Route("api/[controller]")]
    public class GoalsController : AuthorizeControllerBase
    {
        private readonly GamificationService _gamificationService;

        public GoalsController(AppDbContext context, IMemoryCache cache, GamificationService gamificationService) : base(context, cache)
        {
            _gamificationService = gamificationService;
        }

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
            if (goal.CurrentAmount > 0)
            {
                return BadRequest(new { Message = "Vui lòng rút hết tiền trước khi xóa mục tiêu." });
            }

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

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId && a.IsActive);
            if (account == null) return NotFound(new { Message = "Không tìm thấy tài khoản nguồn" });

            var availableBalance = account.Balance - account.LockedBalance;
            if (availableBalance < request.Amount)
            {
                return BadRequest(new { Message = "Số dư khả dụng trong tài khoản này không đủ để nạp." });
            }

            goal.CurrentAmount += request.Amount;
            account.LockedBalance += request.Amount;
            account.LastUpdatedAt = DateTime.UtcNow;

            var record = new GoalRecord
            {
                GoalId = goal.Id,
                AccountId = account.Id,
                Amount = request.Amount,
                Type = "Deposit",
                CreatedAt = DateTime.UtcNow
            };
            _context.GoalRecords.Add(record);

            await _context.SaveChangesAsync();
            await _gamificationService.OnGoalDeposited(userId);
            if (goal.CurrentAmount >= goal.TargetAmount)
            {
                await _gamificationService.OnGoalCompleted(userId);
            }

            return Ok(new
            {
                Message = "Nạp tiền vào mục tiêu thành công",
                CurrentAmount = goal.CurrentAmount,
                Progress = goal.TargetAmount > 0 ? (goal.CurrentAmount / goal.TargetAmount) * 100 : 0
            });
        }

        [HttpPost("{id}/withdraw")]
        public async Task<IActionResult> WithdrawFromGoal(int id, [FromBody] WithdrawRequest request)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var goal = await _context.Goals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId && g.IsActive);
            if (goal == null)
            {
                return NotFound(new { Message = "Không tìm thấy mục tiêu hoặc mục tiêu đã bị đóng" });
            }
            if (goal.CurrentAmount < request.Amount)
            {
                return BadRequest(new { Message = "Số dư của mục tiêu không đủ để rút." });
            }

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId && a.IsActive);
            if (account == null) return NotFound(new { Message = "Không tìm thấy tài khoản nguồn" });
            if (account.LockedBalance < request.Amount)
            {
                return BadRequest(new { Message = "Tài khoản được chọn không có đủ số dư đang khóa để thực hiện rút." });
            }

            goal.CurrentAmount -= request.Amount;
            account.LockedBalance -= request.Amount;

            var record = new GoalRecord
            {
                GoalId = goal.Id,
                AccountId = account.Id,
                Amount = request.Amount,
                Type = "Withdraw",
                CreatedAt = DateTime.UtcNow
            };
            _context.GoalRecords.Add(record);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Rút tiền từ mục tiêu thành công",
                CurrentAmount = goal.CurrentAmount,
                Progress = goal.TargetAmount > 0 ? (goal.CurrentAmount / goal.TargetAmount) * 100 : 0
            });
        }

        [HttpGet("{id}/records")]
        public async Task<IActionResult> GetGoalRecords(int id)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            // Kiểm tra quyền sở hữu mục tiêu trước khi cho xem lịch sử
            var isOwner = await _context.Goals.AnyAsync(g => g.Id == id && g.UserId == userId);
            if (!isOwner) return Forbid();

            var records = await _context.GoalRecords
                .Where(r => r.GoalId == id)
                .OrderByDescending(r => r.CreatedAt) // Sắp xếp mới nhất lên đầu
                .Select(r => new
                {
                    r.Id,
                    r.Amount,
                    r.Type, // "Deposit" hoặc "Withdraw"
                    CreatedAt = DateTime.SpecifyKind(r.CreatedAt, DateTimeKind.Utc)
                })
                .ToListAsync();

            return Ok(records);
        }

        // ĐÃ THÊM: Xóa một lịch sử nạp/rút cụ thể
        [HttpDelete("records/{recordId}")]
        public async Task<IActionResult> DeleteGoalRecord(int recordId)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            // 1. Tìm record, bao gồm cả thông tin Goal chứa nó
            var record = await _context.GoalRecords
                .Include(r => r.Goal)
                .FirstOrDefaultAsync(r => r.Id == recordId);

            // Kiểm tra xem record có tồn tại và thuộc về user đang đăng nhập không
            if (record == null || record.Goal == null || record.Goal.UserId != userId)
            {
                return NotFound(new { Message = "Không tìm thấy lịch sử hoặc bạn không có quyền xóa" });
            }

            // 2. HOÀN TÁC SỐ DƯ (Rollback TotalBalance)
            if (record.Type == "Deposit")
            {
                // Nếu xóa lịch sử Nạp -> Trừ tiền khỏi mục tiêu
                // Phải kiểm tra xem nếu trừ thì có bị âm tiền không
                if (record.Goal.CurrentAmount < record.Amount)
                {
                    return BadRequest(new { Message = "Không thể xóa lịch sử nạp này vì số dư mục tiêu sẽ bị âm." });
                }
                record.Goal.CurrentAmount -= record.Amount;
            }
            else if (record.Type == "Withdraw")
            {
                // Nếu xóa lịch sử Rút -> Cộng tiền trả lại mục tiêu
                record.Goal.CurrentAmount += record.Amount;
            }

            _context.GoalRecords.Remove(record);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Xóa lịch sử thành công",
                NewGoalAmount = record.Goal.CurrentAmount
            });
        }
    }
}