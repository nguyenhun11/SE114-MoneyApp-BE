using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SE114_MoneyApp_BE.Controllers.Base;
using SE114_MoneyApp_BE.Data;
using SE114_MoneyApp_BE.DTOs.Transaction;
using SE114_MoneyApp_BE.Models;
using SE114_MoneyApp_BE.Services;
using System.Diagnostics;
using System.Linq.Expressions;

namespace SE114_MoneyApp_BE.Controllers
{
    [Route("api/[controller]")]
    public class TransactionController : AuthorizeControllerBase
    {
        private readonly GamificationService _gamificationService;

        public TransactionController(AppDbContext context, IMemoryCache cache, GamificationService gamificationService) : base(context, cache)
        {
            _gamificationService = gamificationService;
        }

        private Expression<Func<Transaction, TransactionResponse>> MapToTransactionResponse = t => new TransactionResponse
        {
            Id = t.Id,
            AccountId = t.AccountId,
            AccountName = t.Account != null ? t.Account.AccountName : string.Empty,
            CategoryId = t.CategoryId,
            CategoryName = t.Category != null ? t.Category.CategoryName : string.Empty,
            Type = t.Category != null && t.Category.CategoryGroup != null ? t.Category.CategoryGroup.Type : CategoryType.Expense,

            OriginalAmount = t.OriginalAmount,
            CurrencyCode = t.CurrencyCode,
            BaseAmount = t.BaseAmount,
            AccountAmount = t.AccountAmount,
            ExchangeRate = t.ExchangeRate,

            Date = DateTime.SpecifyKind(t.TransactionDate, DateTimeKind.Utc),
            Note = t.Note,
            categoryColorId = t.Category!.ColorId,
            categoryIconId = t.Category!.IconId,
            accountColorId = t.Account!.ColorId,
            accountIconId = t.Account!.IconId,
            ImageUrls = t.ImageUrls,
            MoodId = t.MoodId,
            CreatedAt = DateTime.SpecifyKind(t.CreatedAt, DateTimeKind.Utc),
            LastUpdatedAt = DateTime.SpecifyKind(t.LastUpdatedAt, DateTimeKind.Utc)
        };

        [HttpGet]
        public async Task<ActionResult<List<TransactionResponse>>> GetTransactions(
    [FromQuery] DateTime? startDate,
    [FromQuery] DateTime? endDate,
    [FromQuery] CategoryType? categoryType,
    [FromQuery] Guid? accountId,
    [FromQuery] Guid? categoryId)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(message);

            var query = _context.Transactions
                .Include(t => t.Account)
                .Include(t => t.Category)
                .Where(t => t.Account!.UserId == userId)
                .AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(t => t.TransactionDate >= startDate.Value.Date);
            }
            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.TransactionDate <= endOfDay);
            }
            if (categoryType.HasValue && categoryType.Value != CategoryType.All)
            {
                query = query.Where(t => t.Category!.CategoryGroup!.Type == categoryType.Value);
            }
            if (accountId.HasValue)
            {
                query = query.Where(t => t.AccountId == accountId.Value);
            }
            if (categoryId.HasValue)
            {
                query = query.Where(t => t.CategoryId == categoryId.Value);
            }

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .Select(MapToTransactionResponse)
                .ToListAsync();

            return Ok(transactions);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TransactionResponse>> GetTransactionById(Guid id)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(message);

            var transaciton = await _context.Transactions
                .Include(t => t.Account)
                .Include(t => t.Category)
                .Where(t => t.Id == id && t.Account!.UserId == userId)
                .Select(MapToTransactionResponse)
                .FirstOrDefaultAsync();

            if (transaciton == null) return NotFound("Transaction not found");
            return Ok(transaciton);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTransaction([FromBody] TransactionRequest request)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(message);

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId);
            if (account == null) return BadRequest("Invalid account");

            var category = await _context.Categories
                .Include(c => c.CategoryGroup)
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.UserId == userId);
            if (category == null) return BadRequest("Invalid category");

            // Lấy thông tin user để quy đổi BaseAmount theo đúng đồng tiền họ đang xài
            var currentUser = await _context.Users.FindAsync(userId);

            // 1. CHUẨN BỊ DỮ LIỆU TÍNH TOÁN
            double absOriginalAmount = Math.Abs((double)request.OriginalAmount);
            string transactionCurrency = !string.IsNullOrEmpty(request.CurrencyCode) ? request.CurrencyCode.ToUpper() : account.CurrencyCode.ToUpper();
            string accountCurrency = account.CurrencyCode.ToUpper();

            // ĐÃ SỬA: Lấy DefaultCurrency của user (Fallback về VND nếu lỗi data)
            string systemCurrency = !string.IsNullOrEmpty(currentUser?.DefaultCurrency) ? currentUser.DefaultCurrency.ToUpper() : "VND";

            _cache.TryGetValue("LatestExchangeRates", out Dictionary<string, double>? rates);

            // 2. BACKEND TỰ TÍNH TOÁN
            double calculatedAccountAmount = ConvertCurrency(absOriginalAmount, transactionCurrency, accountCurrency, rates);
            double calculatedBaseAmount = ConvertCurrency(absOriginalAmount, transactionCurrency, systemCurrency, rates);
            double exchangeRate = absOriginalAmount > 0 ? calculatedAccountAmount / absOriginalAmount : 1.0;

            var transaction = new Transaction
            {
                AccountId = request.AccountId,
                CategoryId = request.CategoryId,
                TransactionDate = DateTime.SpecifyKind(request.Date.Date, DateTimeKind.Utc),
                Note = request.Note,
                ImageUrls = request.ImageUrls,
                Account = account,
                Category = category,

                OriginalAmount = (decimal)absOriginalAmount,
                CurrencyCode = transactionCurrency,
                AccountAmount = (decimal)calculatedAccountAmount,
                BaseAmount = (decimal)calculatedBaseAmount,
                ExchangeRate = exchangeRate,

                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };

            // 3. TRỪ TIỀN BẰNG CON SỐ ĐÃ TỰ TÍNH
            switch (category.CategoryGroup!.Type)
            {
                case CategoryType.Expense:
                    var availableBalance = account.Balance - account.LockedBalance;
                    if (availableBalance < (decimal)calculatedAccountAmount)
                    {
                        return BadRequest("Số dư khả dụng trong tài khoản này không đủ để thực hiện giao dịch.");
                    }
                    account.Balance -= (decimal)calculatedAccountAmount;
                    break;
                case CategoryType.Income:
                    account.Balance += (decimal)calculatedAccountAmount;
                    break;
                default:
                    return BadRequest("Invalid category type");
            }

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Cập nhật điểm cho MoneyCity dựa trên ngày của giao dịch
            var (baseSP, bonusPP, totalPP) = await _gamificationService.OnTransactionAdded(userId, transaction.TransactionDate);

            var response = MapToTransactionResponse.Compile().Invoke(transaction);
            return Ok(new
            {
                transactionId = response.Id,
                baseSP = baseSP,
                bonusPP = bonusPP,
                totalPP = totalPP,
                transaction = response
            });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateTransaction(Guid id, [FromBody] TransactionRequest request)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(message);

            var transaction = await _context.Transactions
                .Where(t => t.Id == id && t.Account!.UserId == userId)
                .FirstOrDefaultAsync();
            if (transaction == null) return NotFound("Không tìm thấy giao dịch");

            var oldAccount = await _context.Accounts.FindAsync(transaction.AccountId);
            var oldCategory = await _context.Categories.Include(c => c.CategoryGroup).FirstOrDefaultAsync(c => c.Id == transaction.CategoryId);

            var newAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId);
            if (newAccount == null) return BadRequest("Tài khoản mới không hợp lệ");

            var newCategory = await _context.Categories.Include(c => c.CategoryGroup).FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.UserId == userId);
            if (newCategory == null) return BadRequest("Hạng mục mới không hợp lệ");

            var currentUser = await _context.Users.FindAsync(userId);

            // 1. TÍNH TOÁN LẠI TỪ ĐẦU DỰA TRÊN REQUEST MỚI
            double newAbsOriginalAmount = Math.Abs((double)request.OriginalAmount);
            string newTransactionCurrency = !string.IsNullOrEmpty(request.CurrencyCode) ? request.CurrencyCode.ToUpper() : newAccount.CurrencyCode.ToUpper();
            string newAccountCurrency = newAccount.CurrencyCode.ToUpper();
            string systemCurrency = !string.IsNullOrEmpty(currentUser?.DefaultCurrency) ? currentUser.DefaultCurrency.ToUpper() : "VND";

            _cache.TryGetValue("LatestExchangeRates", out Dictionary<string, double>? rates);

            double newCalculatedAccountAmount = ConvertCurrency(newAbsOriginalAmount, newTransactionCurrency, newAccountCurrency, rates);
            double newCalculatedBaseAmount = ConvertCurrency(newAbsOriginalAmount, newTransactionCurrency, systemCurrency, rates);
            double newExchangeRate = newAbsOriginalAmount > 0 ? newCalculatedAccountAmount / newAbsOriginalAmount : 1.0;

            // 2. MÔ PHỎNG VÀ KIỂM TRA AN TOÀN SỐ DƯ
            decimal tempOldAccountBalance = oldAccount!.Balance;
            decimal tempNewAccountBalance = newAccount.Balance;
            bool isSameAccount = oldAccount.Id == newAccount.Id;

            // BƯỚC A: HOÀN TÁC GIAO DỊCH CŨ (Trong bộ nhớ tạm)
            if (oldCategory!.CategoryGroup!.Type == CategoryType.Expense)
            {
                tempOldAccountBalance += transaction.AccountAmount;
            }
            else if (oldCategory.CategoryGroup.Type == CategoryType.Income)
            {
                tempOldAccountBalance -= transaction.AccountAmount;
            }

            // Đồng bộ biến tạm nếu không đổi tài khoản
            if (isSameAccount) tempNewAccountBalance = tempOldAccountBalance;

            // BƯỚC B: ÁP DỤNG GIAO DỊCH MỚI (Trong bộ nhớ tạm)
            if (newCategory!.CategoryGroup!.Type == CategoryType.Expense)
            {
                tempNewAccountBalance -= (decimal)newCalculatedAccountAmount;
            }
            else if (newCategory.CategoryGroup.Type == CategoryType.Income)
            {
                tempNewAccountBalance += (decimal)newCalculatedAccountAmount;
            }

            // BƯỚC C: KIỂM TRA KHẢ DỤNG TỪNG TÀI KHOẢN VÀ CHỐT SỐ
            if (isSameAccount)
            {
                if (tempNewAccountBalance - oldAccount.LockedBalance < 0)
                    return BadRequest("Sau khi cập nhật, số dư khả dụng không đủ để thực hiện thao tác này.");

                // An toàn -> Áp dụng số dư thực
                oldAccount.Balance = tempNewAccountBalance;
            }
            else
            {
                if (tempOldAccountBalance - oldAccount.LockedBalance < 0)
                    return BadRequest("Không thể hoàn tác giao dịch ở tài khoản CŨ vì số dư khả dụng sẽ bị âm.");

                if (tempNewAccountBalance - newAccount.LockedBalance < 0)
                    return BadRequest("Số dư khả dụng của tài khoản MỚI không đủ để chứa giao dịch này.");

                // An toàn -> Áp dụng số dư thực
                oldAccount.Balance = tempOldAccountBalance;
                newAccount.Balance = tempNewAccountBalance;
            }

            // 3. CẬP NHẬT DỮ LIỆU ENTITY GIAO DỊCH (Chỉ viết 1 lần duy nhất)
            transaction.AccountId = request.AccountId;
            transaction.CategoryId = request.CategoryId;
            transaction.TransactionDate = DateTime.SpecifyKind(request.Date.Date, DateTimeKind.Utc);
            transaction.Note = request.Note;
            transaction.ImageUrls = request.ImageUrls;
            transaction.LastUpdatedAt = DateTime.UtcNow;
            transaction.Account = newAccount;
            transaction.Category = newCategory;

            transaction.OriginalAmount = (decimal)newAbsOriginalAmount;
            transaction.CurrencyCode = newTransactionCurrency;
            transaction.AccountAmount = (decimal)newCalculatedAccountAmount;
            transaction.BaseAmount = (decimal)newCalculatedBaseAmount;
            transaction.ExchangeRate = newExchangeRate;

            await _context.SaveChangesAsync();

            var response = MapToTransactionResponse.Compile().Invoke(transaction);
            return Ok(response);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteTransaction(Guid id)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(message);

            var transaction = await _context.Transactions
                .Include(t => t.Account)
                .Include(t => t.Category)
                    .ThenInclude(c => c.CategoryGroup)
                .FirstOrDefaultAsync(t => t.Id == id && t.Account!.UserId == userId);

            if (transaction == null) return NotFound("Không tìm thấy giao dịch hoặc không có quyền truy cập");

            switch (transaction.Category!.CategoryGroup!.Type)
            {
                case CategoryType.Expense:
                    transaction.Account!.Balance += transaction.AccountAmount;
                    break;
                case CategoryType.Income:
                    var availableBalance = transaction.Account!.Balance - transaction.Account!.LockedBalance;
                    if (availableBalance < transaction.AccountAmount)
                    {
                        return BadRequest("Không thể xóa giao dịch thu nhập này vì số dư khả dụng hiện tại sẽ bị âm.");
                    }
                    transaction.Account!.Balance -= transaction.AccountAmount;
                    break;
            }

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Xóa thành công" });
        }
    }
}
