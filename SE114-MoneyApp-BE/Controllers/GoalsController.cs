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
                    Deadline = DateTime.SpecifyKind(g.Deadline, DateTimeKind.Utc),
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
            if (goal == null) return NotFound(new { Message = "Không tìm thấy mục tiêu hoặc bạn không có quyền chỉnh sửa" });

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
            if (goal == null) return NotFound(new { Message = "Không tìm thấy mục tiêu hoặc bạn không có quyền xóa" });

            if (goal.CurrentAmount > 0)
            {
                return BadRequest(new { Message = "Vui lòng rút hết tiền trước khi xóa mục tiêu." });
            }

            goal.IsActive = false;
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Xóa mục tiêu thành công" });
        }

        [HttpPost("{id}/deposit")]
        public async Task<ActionResult<GoalTransactionResponse>> DepositToGoal(int id, [FromBody] DepositRequest request)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var goal = await _context.Goals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId && g.IsActive);
            if (goal == null) return NotFound(new { Message = "Không tìm thấy mục tiêu hoặc mục tiêu đã bị đóng" });

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

            return Ok(new GoalTransactionResponse
            {
                Message = "Nạp tiền vào mục tiêu thành công",
                CurrentAmount = goal.CurrentAmount,
                Progress = goal.TargetAmount > 0 ? (goal.CurrentAmount / goal.TargetAmount) * 100 : 0,
                AccountAvailableBalance = account.Balance - account.LockedBalance
            });
        }

        [HttpPost("{id}/withdraw")]
        public async Task<ActionResult<GoalTransactionResponse>> WithdrawFromGoal(int id, [FromBody] WithdrawRequest request)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var goal = await _context.Goals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId && g.IsActive);
            if (goal == null) return NotFound(new { Message = "Không tìm thấy mục tiêu hoặc mục tiêu đã bị đóng" });

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
            account.LastUpdatedAt = DateTime.UtcNow;

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

            return Ok(new GoalTransactionResponse
            {
                Message = "Rút tiền từ mục tiêu thành công",
                CurrentAmount = goal.CurrentAmount,
                Progress = goal.TargetAmount > 0 ? (goal.CurrentAmount / goal.TargetAmount) * 100 : 0,
                AccountAvailableBalance = account.Balance - account.LockedBalance
            });
        }

        [HttpGet("{id}/records")]
        public async Task<ActionResult<IEnumerable<GoalRecordResponse>>> GetGoalRecords(int id)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var isOwner = await _context.Goals.AnyAsync(g => g.Id == id && g.UserId == userId);
            if (!isOwner) return Forbid();

            var records = await _context.GoalRecords
                .Include(r => r.Account)
                .Where(r => r.GoalId == id)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new GoalRecordResponse
                {
                    Id = r.Id,
                    GoalId = r.GoalId,
                    AccountId = r.AccountId,
                    AccountName = r.Account != null ? r.Account.AccountName : string.Empty,
                    Amount = r.Amount,
                    Type = r.Type,
                    CreatedAt = DateTime.SpecifyKind(r.CreatedAt, DateTimeKind.Utc)
                })
                .ToListAsync();

            return Ok(records);
        }

        [HttpGet("records/{recordId}")]
        public async Task<ActionResult<GoalRecordResponse>> GetGoalRecordById(int recordId)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var record = await _context.GoalRecords
                .Include(r => r.Goal)
                .Include(r => r.Account)
                .FirstOrDefaultAsync(r => r.Id == recordId);

            if (record == null || record.Goal == null || record.Goal.UserId != userId)
            {
                return NotFound(new { Message = "Không tìm thấy lịch sử giao dịch hoặc bạn không có quyền truy cập" });
            }

            var response = new GoalRecordResponse
            {
                Id = record.Id,
                GoalId = record.GoalId,
                AccountId = record.AccountId,
                AccountName = record.Account != null ? record.Account.AccountName : string.Empty,
                Amount = record.Amount,
                Type = record.Type,
                CreatedAt = DateTime.SpecifyKind(record.CreatedAt, DateTimeKind.Utc)
            };

            return Ok(response);
        }

        [HttpDelete("records/{recordId}")]
        public async Task<ActionResult<GoalRecordDeleteResponse>> DeleteGoalRecord(int recordId)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var record = await _context.GoalRecords
                .Include(r => r.Goal)
                .FirstOrDefaultAsync(r => r.Id == recordId);

            if (record == null || record.Goal == null || record.Goal.UserId != userId)
            {
                return NotFound(new { Message = "Không tìm thấy lịch sử hoặc bạn không có quyền xóa" });
            }

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == record.AccountId);
            if (account == null) return NotFound(new { Message = "Không tìm thấy tài khoản liên quan" });

            if (record.Type == "Deposit")
            {
                if (record.Goal.CurrentAmount < record.Amount)
                {
                    return BadRequest(new { Message = "Không thể xóa lịch sử nạp này vì số dư mục tiêu sẽ bị âm." });
                }
                if (account.LockedBalance < record.Amount)
                {
                    return BadRequest(new { Message = "Không thể xóa lịch sử nạp vì số dư khóa của tài khoản không khớp." });
                }

                record.Goal.CurrentAmount -= record.Amount;
                account.LockedBalance -= record.Amount; // Hoàn tác trả lại tiền khả dụng cho ví
            }
            else if (record.Type == "Withdraw")
            {
                var availableBalance = account.Balance - account.LockedBalance;
                if (availableBalance < record.Amount)
                {
                    return BadRequest(new { Message = "Tài khoản nguồn không đủ số dư khả dụng để khóa lại khoản tiền này." });
                }

                record.Goal.CurrentAmount += record.Amount;
                account.LockedBalance += record.Amount; // Khóa tiền lại vào ví như cũ
            }

            account.LastUpdatedAt = DateTime.UtcNow;
            _context.GoalRecords.Remove(record);
            await _context.SaveChangesAsync();

            return Ok(new GoalRecordDeleteResponse
            {
                Message = "Xóa lịch sử thành công",
                NewGoalAmount = record.Goal.CurrentAmount
            });
        }
    }
}