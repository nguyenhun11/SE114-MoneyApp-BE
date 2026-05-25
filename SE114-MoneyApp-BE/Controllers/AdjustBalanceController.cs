using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SE114_MoneyApp_BE.Data;
using SE114_MoneyApp_BE.DTOs.AdjustBalance;
using SE114_MoneyApp_BE.Models;

namespace SE114_MoneyApp_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdjustBalanceController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AdjustBalanceController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/AdjustBalance/{userId}?accountId={accountId}&startDate={startDate}&endDate={endDate}
        /// <summary>
        /// Danh sách các điều chỉnh số dư của một người dùng, có thể lọc theo tài khoản và khoảng thời gian
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="accountId"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        [HttpGet("{userId}")]
        public async Task<ActionResult<List<AdjustBalanceResponse>>> GetAdjustBalanceByUserId(
            [FromRoute] int userId,
            [FromQuery] Guid? accountId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
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
                query = query.Where(ab => ab.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(ab => ab.CreatedAt <= endDate.Value);
            }

            var adjustBalances = await query
                .OrderByDescending(ab => ab.CreatedAt)
                .Select(ab => new AdjustBalanceResponse
                {
                    Id = ab.Id,
                    AccountId = ab.AccountId,
                    AccountName = ab.Account!.AccountName,
                    Amount = ab.Amount,
                    CreatedAt = ab.CreatedAt
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
            var account = await _context.Accounts.FindAsync(request.AccountId);
            if (account == null || !account.IsActive)
            {
                return NotFound(new
                {
                    Message = "Không tìm thấy tài khoản hoặc tài khoản đã bị vô hiệu hóa."
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
