using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SE114_MoneyApp_BE.Controllers.Base;
using SE114_MoneyApp_BE.Data;
using SE114_MoneyApp_BE.DTOs.Transfer;
using SE114_MoneyApp_BE.Models;
using System.Linq.Expressions;

namespace SE114_MoneyApp_BE.Controllers
{
    [Route("api/[controller]")]
    public class TransferController : AuthorizeControllerBase
    {
        public TransferController(AppDbContext context, IMemoryCache cache) : base(context, cache) { }

        private static Expression<Func<Transfer, TransferResponse>> MapToTransferResponse = t => new TransferResponse
        {
            Id = t.Id,
            SourceAccount = t.SourceAccountId,
            SourceAccountName = t.Source!.AccountName,
            SourceAccountColorId = t.Source!.ColorId,
            SourceAccountIconId = t.Source!.IconId,

            DestinationAccount = t.DestinationAccountId,
            DestinationAccountName = t.Destination!.AccountName,
            DestinationAccountColorId = t.Destination!.ColorId,
            DestinationAccountIconId = t.Destination!.IconId,

            SourceAmount = t.SourceAmount,
            DestinationAmount = t.DestinationAmount,
            BaseAmount = t.BaseAmount,
            SourceExchangeRate = t.SourceExchangeRate,
            DestinationExchangeRate = t.DestinationExchangeRate,

            TransferDate = DateTime.SpecifyKind(t.TransferDate, DateTimeKind.Utc),
            Description = t.Description,
            CreatedAt = DateTime.SpecifyKind(t.CreatedAt, DateTimeKind.Utc),
            LastUpdatedAt = DateTime.SpecifyKind(t.LastUpdatedAt, DateTimeKind.Utc)
        };

        [HttpGet]
        public async Task<ActionResult<List<TransferResponse>>> GetTransfers(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] Guid? source = null,
            [FromQuery] Guid? destination = null)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var query = _context.Transfers
                .Include(t => t.Source)
                .Include(t => t.Destination)
                .Where(t => t.Source!.UserId == userId)
                .AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(t => t.TransferDate >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.TransferDate <= endOfDay);
            }

            if (source.HasValue)
            {
                query = query.Where(t => t.SourceAccountId == source.Value);
            }

            if (destination.HasValue)
            {
                query = query.Where(t => t.DestinationAccountId == destination.Value);
            }

            var transfers = await query
                .OrderByDescending(t => t.TransferDate)
                .Select(MapToTransferResponse)
                .ToListAsync();

            return Ok(transfers);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<TransferResponse>> GetTransferById(Guid id)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var transfer = await _context.Transfers
                .Include(t => t.Source)
                .Include(t => t.Destination)
                .Where(t => t.Id == id)
                .Select(MapToTransferResponse)
                .FirstOrDefaultAsync();

            if (transfer == null) return NotFound(new { Message = "Không tìm thấy chuyển khoản" });
            return Ok(transfer);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTransfer([FromBody] TransferRequest request)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            if (request.SourceAccountId == request.DestinationAccountId)
                return BadRequest(new { Message = "Tài khoản trùng nhau" });

            var sourceAccount = await _context.Accounts.FindAsync(request.SourceAccountId);
            var destinationAccount = await _context.Accounts.FindAsync(request.DestinationAccountId);

            if (sourceAccount == null || destinationAccount == null)
                return BadRequest(new { Message = "Tài khoản không tồn tại" });

            if (sourceAccount.UserId != userId || destinationAccount.UserId != userId)
                return BadRequest(new { Message = "Tài khoản không hợp lệ" });

            // Lấy thông tin user để quy đổi BaseAmount theo DefaultCurrency
            var currentUser = await _context.Users.FindAsync(userId);

            // 1. CHUẨN BỊ DỮ LIỆU TÍNH TOÁN
            double absSourceAmount = Math.Abs((double)request.SourceAmount);
            string sourceCurrency = sourceAccount.CurrencyCode.ToUpper();
            string destCurrency = destinationAccount.CurrencyCode.ToUpper();
            string systemCurrency = !string.IsNullOrEmpty(currentUser?.DefaultCurrency) ? currentUser.DefaultCurrency.ToUpper() : "VND";

            _cache.TryGetValue("LatestExchangeRates", out Dictionary<string, double>? rates);

            // 2. BACKEND TỰ TÍNH TOÁN
            double calculatedDestAmount = ConvertCurrency(absSourceAmount, sourceCurrency, destCurrency, rates);
            double calculatedBaseAmount = ConvertCurrency(absSourceAmount, sourceCurrency, systemCurrency, rates);

            double destExchangeRate = absSourceAmount > 0 ? calculatedDestAmount / absSourceAmount : 1.0;

            var transfer = new Transfer
            {
                SourceAccountId = request.SourceAccountId,
                DestinationAccountId = request.DestinationAccountId,

                SourceAmount = (decimal)absSourceAmount,
                DestinationAmount = (decimal)calculatedDestAmount, // Lấy số Backend tính
                BaseAmount = (decimal)calculatedBaseAmount,        // Lấy số Backend tính
                SourceExchangeRate = 1.0,
                DestinationExchangeRate = destExchangeRate,        // Lấy số Backend tính

                TransferDate = request.TransferDate.Date.ToUniversalTime(),
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };

            // 3. TRỪ/CỘNG VÍ BẰNG CON SỐ TỰ TÍNH
            sourceAccount.Balance -= (decimal)absSourceAmount;
            destinationAccount.Balance += (decimal)calculatedDestAmount;

            _context.Transfers.Add(transfer);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Chuyển khoản thành công", TransferId = transfer.Id });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateTransfer(Guid id, [FromBody] TransferRequest request)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var transfer = await _context.Transfers
                .Include(t => t.Source)
                .Include(t => t.Destination)
                .FirstOrDefaultAsync(t => t.Id == id && t.Source!.UserId == userId);

            if (transfer == null) return NotFound(new { Message = "Không tìm thấy chuyển khoản" });

            var newSourceAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.SourceAccountId && a.UserId == userId);
            var newDestinationAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.DestinationAccountId && a.UserId == userId);

            if (newSourceAccount == null || newDestinationAccount == null)
                return BadRequest(new { Message = "Tài khoản không hợp lệ" });

            if (newSourceAccount.Id == newDestinationAccount.Id)
                return BadRequest(new { Message = "Tài khoản không được trùng nhau" });

            // 1. HOÀN TIỀN LẠI CHO VÍ CŨ (Dùng số tiền lưu lúc trước)
            transfer.Source!.Balance += transfer.SourceAmount;
            transfer.Destination!.Balance -= transfer.DestinationAmount;

            var currentUser = await _context.Users.FindAsync(userId);

            // 2. TÍNH TOÁN LẠI TỪ ĐẦU DỰA TRÊN REQUEST MỚI
            double newAbsSourceAmount = Math.Abs((double)request.SourceAmount);
            string newSourceCurrency = newSourceAccount.CurrencyCode.ToUpper();
            string newDestCurrency = newDestinationAccount.CurrencyCode.ToUpper();
            string systemCurrency = !string.IsNullOrEmpty(currentUser?.DefaultCurrency) ? currentUser.DefaultCurrency.ToUpper() : "VND";

            _cache.TryGetValue("LatestExchangeRates", out Dictionary<string, double>? rates);

            double newCalculatedDestAmount = ConvertCurrency(newAbsSourceAmount, newSourceCurrency, newDestCurrency, rates);
            double newCalculatedBaseAmount = ConvertCurrency(newAbsSourceAmount, newSourceCurrency, systemCurrency, rates);
            double newDestExchangeRate = newAbsSourceAmount > 0 ? newCalculatedDestAmount / newAbsSourceAmount : 1.0;

            // 3. CẬP NHẬT DỮ LIỆU CHUYỂN KHOẢN
            transfer.SourceAccountId = request.SourceAccountId;
            transfer.DestinationAccountId = request.DestinationAccountId;

            transfer.SourceAmount = (decimal)newAbsSourceAmount;
            transfer.DestinationAmount = (decimal)newCalculatedDestAmount;
            transfer.BaseAmount = (decimal)newCalculatedBaseAmount;
            transfer.SourceExchangeRate = 1.0;
            transfer.DestinationExchangeRate = newDestExchangeRate;

            transfer.TransferDate = request.TransferDate.Date.ToUniversalTime();
            transfer.Description = request.Description;
            transfer.LastUpdatedAt = DateTime.UtcNow;

            // 4. ÁP DỤNG TRỪ/CỘNG CHO VÍ MỚI BẰNG CON SỐ VỪA TÍNH
            newSourceAccount.Balance -= (decimal)newAbsSourceAmount;
            newDestinationAccount.Balance += (decimal)newCalculatedDestAmount;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Cập nhật thành công" });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteTransfer(Guid id)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var transfer = await _context.Transfers
                .Include(t => t.Source)
                .Include(t => t.Destination)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transfer == null) return NotFound(new { Message = "Không tìm thấy chuyển khoản" });
            if (transfer.Source!.UserId != userId) return BadRequest(new { Message = "Không có quyền xóa." });

            // Hoàn tác chuyển khoản bằng đúng hệ tiền của từng ví đã lưu, không cần tính lại
            transfer.Source.Balance += transfer.SourceAmount;
            transfer.Destination!.Balance -= transfer.DestinationAmount;

            _context.Transfers.Remove(transfer);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Xóa chuyển khoản thành công" });
        }
    }
}