using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SE114_MoneyApp_BE.Controllers.Base;
using SE114_MoneyApp_BE.Data;
using SE114_MoneyApp_BE.DTOs.AdjustBalance;
using SE114_MoneyApp_BE.Models;

namespace SE114_MoneyApp_BE.Controllers
{
    [Route("api/[controller]")]
    public class AdjustBalanceController : AuthorizeControllerBase
    {
        public AdjustBalanceController(AppDbContext context) : base(context) { }


        /// <summary>
        /// Danh sách các điều chỉnh số dư của một người dùng, có thể lọc theo tài khoản và khoảng thời gian
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="accountId"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<List<AdjustBalanceResponse>>> GetAdjustBalances(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] Guid? accountId)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success)
            {
                return Unauthorized(new { Message = message });
            }

            var query = _context.AdjustBalances
                .Include(ab => ab.Account)
                .Where(ab => ab.Account!.UserId == userId)
                .AsQueryable();

            if (accountId.HasValue)
            {
                query = query.Where(ab => ab.AccountId == accountId.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(ab => ab.CreatedAt >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(ab => ab.CreatedAt <= endOfDay);
            }

            var adjustBalances = await query
                .OrderByDescending(ab => ab.CreatedAt)
                .Select(ab => new AdjustBalanceResponse
                {
                    Id = ab.Id,
                    AccountId = ab.AccountId,
                    AccountName = ab.Account!.AccountName,
                    Amount = ab.Amount,
                    CreatedAt = DateTime.SpecifyKind(ab.CreatedAt, DateTimeKind.Utc)
                })
                .ToListAsync();

            return Ok(adjustBalances);
        }

        // POST: api/AdjustBalance
        /// <summary>
        /// Thêm thay đổi số dư
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> CreateAdjustBalance([FromBody] AdjustBalanceRequest request)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success)
            {
                return Unauthorized(new { Message = message });
            }

            var account = await _context.Accounts
                .Where(a => a.Id == request.AccountId && a.UserId == userId && a.IsActive)
                .FirstOrDefaultAsync();

            if (account == null)
            {
                return NotFound(new
                {
                    Message = "Không tìm thấy tài khoản."
                });
            }

            var newAdjustBalance = new AdjustBalance
            {
                AccountId = request.AccountId,
                Amount = request.Amount
            };
            _context.AdjustBalances.Add(newAdjustBalance);
            account.Balance += request.Amount;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Điều chỉnh số dư thành công.",
                AdjustBalanceId = newAdjustBalance.Id,
                NewBalance = account.Balance
            });
        }
    }
}
