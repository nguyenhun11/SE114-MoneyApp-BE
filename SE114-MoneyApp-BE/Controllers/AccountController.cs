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
    }
}
