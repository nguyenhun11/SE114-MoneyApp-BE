 using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SE114_MoneyApp_BE.Data;
using SE114_MoneyApp_BE.DTOs.Account;
using SE114_MoneyApp_BE.Models;
using System.Linq.Expressions;
using System.Security.Claims;

namespace SE114_MoneyApp_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        private static Expression<Func<Account, AccountResponse>> MapToAccountResponse = account => new AccountResponse
        {
            Id = account.Id,
            AccountName = account.AccountName,
            ColorId = account.ColorId,
            IconId = account.IconId,
            Balance = account.Balance,
            Description = account.Description,
            IncludeInTotalBalance = account.IncludeInTotalBalance   
        };

        // GET: api/account/5
        /// <summary>
        /// Lấy tất cả tài khoản người dùng
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("{userId}")]
        public async Task<ActionResult<List<AccountResponse>>> GetAccountsByUserId([FromRoute] int userId)
        {
            var accounts = await _context.Accounts
                .Where(a => a.UserId == userId && a.IsActive == true)
                .Select(MapToAccountResponse)
                .ToListAsync();

            return Ok(accounts);
        }

        // GET: api/account/....
        /// <summary>
        /// 
        /// Lấy chi tiết 1 tài khoản
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<AccountResponse>> GetAccountById([FromRoute] Guid id)
        {
            var account = await _context.Accounts
                .Where(a => a.Id == id && a.IsActive)
                .Select(MapToAccountResponse)
                .FirstOrDefaultAsync();
            if (account == null)
            {
                return NotFound(new
                {
                    Message = "Không tìm thấy tài khoản"
                });
            }
            return Ok(account);
        }

        // GET: api/account/5/total-balance
        /// <summary>
        /// Tổng số dư của các tài khoản của người dùng
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("{userId}/total-balance")]
        public async Task<ActionResult<decimal>> GetTotalBalanceByUserId([FromRoute] int userId)
        {
            var totalBalance = await _context.Accounts
                .Where(a => a.UserId == userId && a.IsActive == true && a.IncludeInTotalBalance == true)
                .SumAsync(a => a.Balance);
            return Ok(totalBalance);
        }

        // POST: api/account
        /// <summary>
        /// Tạo tài khoản mới
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateAccount([FromBody] AccountRequest request)
        {
            // Tự động lấy UserId từ trong JWT Token của người đang gọi API
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { Message = "Phiên đăng nhập không hợp lệ." });
            }
            int userId = int.Parse(userIdClaim);

            var newAccount = new Account
            {
                UserId = userId,
                AccountName = request.AccountName,
                ColorId = request.ColorId,
                IconId = request.IconId,
                Balance = request.Balance,
                Description = request.Description,
                IncludeInTotalBalance = request.IncludeInTotalBalance,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Accounts.Add(newAccount);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Tạo tài khoản thành công!", AccountId = newAccount.Id });
        }

        // PUT: api/Account/5
        /// <summary>
        /// Cập nhật tài khoản
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateAccount(Guid id, [FromBody] AccountRequest request)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == id && a.IsActive);

            if (account == null)
            {
                return NotFound(new { Message = "Tài khoản không tồn tại hoặc đã bị xóa" });
            }

            account.AccountName = request.AccountName;
            account.ColorId = request.ColorId;
            account.IconId = request.IconId;
            account.Description = request.Description;
            account.IncludeInTotalBalance = request.IncludeInTotalBalance;
            account.LastUpdatedAt = DateTime.UtcNow; 

            if (account.Balance != request.Balance)
            {
                decimal amountDifference = request.Balance - account.Balance;

                var adjustBalance = new AdjustBalance
                {
                    AccountId = account.Id,
                    Amount = amountDifference
                };

                _context.AdjustBalances.Add(adjustBalance);
                account.Balance = request.Balance;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Cập nhật tài khoản thành công"
            });
        }

        // DELETE
        /// <summary>
        /// Xóa mềm (ngừng kích hoạt tài khoản)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> SoftDeleteAccount(Guid id)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == id && a.IsActive);

            if (account == null)
            {
                return NotFound(new
                {
                    Message = "Không tìm thấy tài khoản hoặc đã xóa"
                });
            }

            account.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Đã xóa"
            });
        }
        /*TODO
         * Các lựa chọn khi xóa tài khoản:
         * 1. Xóa các giao dịch liên quan đã phát sinh
         * 2. Xóa tài khoản nhưng giữ giao dịch
         *      a. Chuyển tài khoản cần xóa sang một tài khoản khác
         *      b. Xóa mềm, vẫn hiển thị giao dịch dùng tài khoản đã xóa
         */
    }
}
