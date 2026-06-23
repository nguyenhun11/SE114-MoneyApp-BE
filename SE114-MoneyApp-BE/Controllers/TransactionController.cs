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
            await _gamificationService.OnTransactionAdded(userId, transaction.TransactionDate);

            var response = MapToTransactionResponse.Compile().Invoke(transaction);
            return Ok(response);
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
            var oldCategory = await _context.Categories
                .Include(c => c.CategoryGroup)
                .FirstOrDefaultAsync(c => c.Id == transaction.CategoryId);

            // HOÀN TIỀN CŨ
            if (oldAccount != null && oldCategory != null)
            {
                var oldAmount = transaction.AccountAmount;
                switch (oldCategory.CategoryGroup!.Type)
                {
                    case CategoryType.Expense:
                        oldAccount.Balance += oldAmount;
                        break;
                    case CategoryType.Income:
                        oldAccount.Balance -= oldAmount;
                        break;
                }
            }

            var newAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId);
            if (newAccount == null) return BadRequest("Invalid account");

            var newCategory = await _context.Categories
                .Include(c => c.CategoryGroup)
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.UserId == userId);
            if (newCategory == null) return BadRequest("Invalid category");

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

            // 2. CẬP NHẬT DỮ LIỆU
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

            // 3. TRỪ/CỘNG TIỀN MỚI
            switch (newCategory.CategoryGroup!.Type)
            {
                case CategoryType.Expense:
                    newAccount.Balance -= (decimal)newCalculatedAccountAmount;
                    break;
                case CategoryType.Income:
                    newAccount.Balance += (decimal)newCalculatedAccountAmount;
                    break;
            }

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
                    transaction.Account!.Balance -= transaction.AccountAmount;
                    break;
            }

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Xóa thành công" });
        }
    }
}
