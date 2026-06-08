 using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using SE114_MoneyApp_BE.Controllers.Base;
using SE114_MoneyApp_BE.Data;
using SE114_MoneyApp_BE.DTOs.Account;
using SE114_MoneyApp_BE.Models;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Security.Claims;

namespace SE114_MoneyApp_BE.Controllers
{
    [Route("api/[controller]")]
    public class AccountController : AuthorizeControllerBase
    {
        public AccountController(AppDbContext context) : base(context) { }

        private static Expression<Func<Account, AccountResponse>> MapToAccountResponse = account => new AccountResponse
        {
            Id = account.Id,
            AccountName = account.AccountName,
            ColorId = account.ColorId,
            IconId = account.IconId,
            Balance = account.Balance,
            Description = account.Description,
            IncludeInTotalBalance = account.IncludeInTotalBalance,
            SortingOrder = account.SortingOrder,
            CreatedAt = account.CreatedAt,
            LastUpdatedAt = account.LastUpdatedAt
        };


        // GET: api/account/5
        /// <summary>
        /// Lấy tất cả tài khoản người dùng
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<List<AccountResponse>>> GetAccounts()
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success)
            {
                return Unauthorized(new { Message = message });
            }

            var accounts = await _context.Accounts
                .Where(a => a.UserId == userId && a.IsActive)
                .Select(MapToAccountResponse)
                .ToListAsync();

            return Ok(accounts);
        }

        // GET: api/account/....
        /// <summary>
        /// Lấy chi tiết 1 tài khoản
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<AccountResponse>> GetAccountById([FromRoute] Guid id)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success)
            {
                return Unauthorized(new { Message = message });
            }

            var account = await _context.Accounts
                .Where(a => a.Id == id && a.UserId == userId && a.IsActive)
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
        /// <returns></returns>
        [HttpGet("total-balance")]
        public async Task<ActionResult<decimal>> GetTotalBalance()
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success)
            {
                return Unauthorized(new { Message = message });
            }

            var totalBalance = await _context.Accounts
                .Where(a => a.UserId == userId && a.IsActive && a.IncludeInTotalBalance)
                .SumAsync(a => a.Balance);
            return Ok(totalBalance);
        }


        private async Task<int> NormalizeAndGetNextSortingOrderAsync(int userId)
        {
            var accounts = await _context.Accounts
                .Where(a => a.UserId == userId && a.IsActive)
                .OrderBy(a => a.SortingOrder)
                .ThenBy(a => a.AccountName)
                .ToListAsync();

            bool isChanged = false;

            for (int i = 0; i < accounts.Count; i++)
            {
                if (accounts[i].SortingOrder != i)
                {
                    accounts[i].SortingOrder = i;
                    isChanged = true;
                }
            }

            if (isChanged)
            {
                await _context.SaveChangesAsync();
            }

            return accounts.Count;
        }

        // POST: api/account
        /// <summary>
        /// Tạo tài khoản mới
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<AccountResponse>> CreateAccount([FromBody] AccountRequest request)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success)
            {
                return Unauthorized(new { Message = message });
            }

            int nextOrder = await NormalizeAndGetNextSortingOrderAsync(userId);

            var newAccount = new Account
            {
                UserId = userId,
                AccountName = request.AccountName,
                ColorId = request.ColorId,
                IconId = request.IconId,
                Balance = request.Balance,
                Description = request.Description,
                IncludeInTotalBalance = request.IncludeInTotalBalance,
                SortingOrder = nextOrder
            };

            _context.Accounts.Add(newAccount);
            await _context.SaveChangesAsync();

            var response = MapToAccountResponse.Compile().Invoke(newAccount);

            return Ok(response);
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
            var (userId, success, message) = GetCurrentUserId();
            if (!success)
            {
                return Unauthorized(new { Message = message });
            }

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == id 
                    && a.UserId == userId 
                    && a.IsActive);

            if (account == null)
            {
                return NotFound(new { Message = "Tài khoản không tồn tại hoặc bạn không có quyền chỉnh sửa" });
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

        /// <summary>
        /// Thay đổi thứ tự tài khoản
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("reorder/{id:guid}")]
        public async Task<IActionResult> ReorderAccount(Guid id, [FromBody] ReorderAccountRequest request)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            int newOrder = request.NewOrder;

            var accounts = await _context.Accounts
                .Where(a => a.UserId == userId && a.IsActive)
                .OrderBy(a => a.SortingOrder)
                .ThenBy(a => a.AccountName)
                .ToListAsync();

            if (accounts.Count == 0) return NotFound(new { Message = "Không có tài khoản." });

            var targetAccount = accounts.FirstOrDefault(a => a.Id == id);
            if (targetAccount == null) return NotFound(new { Message = "Không tìm thấy tài khoản." });

            if (newOrder < 0) newOrder = 0;
            if (newOrder >= accounts.Count) newOrder = accounts.Count - 1;

            accounts.Remove(targetAccount);
            accounts.Insert(newOrder, targetAccount);

            for (int i = 0; i < accounts.Count; i++)
            {
                accounts[i].SortingOrder = i;
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Cập nhật vị trí thành công!" });
        }

        // DELETE: api/Account/{id}
        /// <summary>
        /// Xóa tài khoản với 3 tùy chọn xử lý dữ liệu: soft_delete, delete_all, move
        /// </summary>
        /// <param name="id"></param>
        /// <param name="mode">"soft_delete", "delete_all", "move"</param>
        /// <param name="fallbackAccountId">ID ví dự phòng khi chọn mode = "move"</param>
        /// <returns></returns>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteAccount(
            Guid id,
            [FromQuery] string mode = "soft_delete",
            [FromQuery] Guid? fallbackAccountId = null)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            // 1. TÌM VÍ CẦN XÓA
            var accountToDelete = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId && a.IsActive);

            if (accountToDelete == null)
            {
                return NotFound(new { Message = "Không tìm thấy tài khoản hoặc bạn không có quyền xóa" });
            }

            // 2. LẤY TOÀN BỘ LỊCH SỬ LIÊN QUAN
            var transactions = await _context.Transactions.Where(t => t.AccountId == id).ToListAsync();
            var adjustBalances = await _context.AdjustBalances.Where(ab => ab.AccountId == id).ToListAsync();

            // Lấy Transfer và nạp sẵn ví đối tác (Source/Destination) để tiện hoàn tiền
            var transfersAsSource = await _context.Transfers
                .Include(t => t.Destination)
                .Where(t => t.SourceAccountId == id).ToListAsync();

            var transfersAsDest = await _context.Transfers
                .Include(t => t.Source)
                .Where(t => t.DestinationAccountId == id).ToListAsync();

            // 3. XỬ LÝ THEO CHẾ ĐỘ
            switch (mode.ToLower())
            {
                case "move":
                    if (!fallbackAccountId.HasValue || fallbackAccountId.Value == id)
                    {
                        return BadRequest(new { Message = "Vui lòng chọn một ví dự phòng hợp lệ để chuyển dữ liệu." });
                    }

                    var fallbackAccount = await _context.Accounts
                        .FirstOrDefaultAsync(a => a.Id == fallbackAccountId.Value && a.UserId == userId && a.IsActive);

                    if (fallbackAccount == null)
                    {
                        return BadRequest(new { Message = "Ví dự phòng không tồn tại." });
                    }

                    // A. Chuyển Transactions và AdjustBalances
                    foreach (var t in transactions) t.AccountId = fallbackAccount.Id;
                    foreach (var ab in adjustBalances) ab.AccountId = fallbackAccount.Id;

                    // B. Chuyển Transfers và triệt tiêu giao dịch "Tự chuyển cho chính mình"
                    foreach (var t in transfersAsSource)
                    {
                        if (t.DestinationAccountId == fallbackAccount.Id)
                            _context.Transfers.Remove(t); // Ví A -> Ví B, giờ A nhập vào B => Xóa
                        else
                            t.SourceAccountId = fallbackAccount.Id;
                    }
                    foreach (var t in transfersAsDest)
                    {
                        if (t.SourceAccountId == fallbackAccount.Id)
                            _context.Transfers.Remove(t); // Ví B -> Ví A, giờ A nhập vào B => Xóa
                        else
                            t.DestinationAccountId = fallbackAccount.Id;
                    }

                    // C. Cộng dồn số dư của ví bị xóa vào ví dự phòng
                    fallbackAccount.Balance += accountToDelete.Balance;
                    break;

                case "delete_all":
                    // A. Xóa sạch Transactions và AdjustBalances
                    _context.Transactions.RemoveRange(transactions);
                    _context.AdjustBalances.RemoveRange(adjustBalances);

                    // B. Hoàn tiền cho các ví đối tác trong Transfer trước khi xóa
                    foreach (var t in transfersAsSource)
                    {
                        // Tiền đi từ Ví A (bị xóa) đến Ví B. Giờ xóa giao dịch -> B mất tiền
                        t.Destination!.Balance -= t.Amount;
                    }
                    _context.Transfers.RemoveRange(transfersAsSource);

                    foreach (var t in transfersAsDest)
                    {
                        // Tiền đi từ Ví B đến Ví A (bị xóa). Giờ xóa giao dịch -> B lấy lại tiền
                        t.Source!.Balance += t.Amount;
                    }
                    _context.Transfers.RemoveRange(transfersAsDest);
                    break;

                case "soft_delete":
                default:
                    break;
            }

            // 4. Xóa mềm
            accountToDelete.IsActive = false;
            accountToDelete.LastUpdatedAt = DateTime.UtcNow;

            accountToDelete.Balance = 0;
            accountToDelete.IncludeInTotalBalance = false;

            await _context.SaveChangesAsync();
            await NormalizeAndGetNextSortingOrderAsync(userId);

            return Ok(new { Message = "Đã xóa tài khoản thành công" });
        }
    }
}
