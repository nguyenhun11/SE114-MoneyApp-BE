using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public TransferController(AppDbContext context) : base(context) { }

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
            Amount = t.Amount,
            TransferDate = DateTime.SpecifyKind(t.TransferDate, DateTimeKind.Utc),
            Description = t.Description,
            CreatedAt = DateTime.SpecifyKind(t.CreatedAt, DateTimeKind.Utc),
            LastUpdatedAt = DateTime.SpecifyKind(t.LastUpdatedAt, DateTimeKind.Utc)
        };

        // GET: api/Transfer/{userId}?startDate=2024-01-01&endDate=2024-12-31&source=accountId&destination=accountId
        /// <summary>
        /// Danh sách các chuyển khoản của người dùng, có thể lọc theo ngày tháng, tài khoản nguồn và tài khoản đích
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<List<TransferResponse>>> GetTransfers(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] Guid? source = null,
            [FromQuery] Guid? destination = null)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success)
            {
                return Unauthorized(new
                {
                    Message = message
                });
            }

            var query = _context.Transfers
                .Include(t => t.Source)
                .Include(t => t.Destination)
                .Where(t => t.Source!.UserId == userId)
                .AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= startDate.Value.Date);
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

        // GET: api/Transfer/{id}
        /// <summary>
        /// Chi tiết chuyển khoản theo Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<TransferResponse>> GetTransferById(Guid id)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success)
            {
                return Unauthorized(new
                {
                    Message = message
                });
            }
            var transfer = await _context.Transfers
                .Include(t => t.Source)
                .Include(t => t.Destination)
                .Where(t => t.Id == id)
                .Select(MapToTransferResponse)
                .FirstOrDefaultAsync();
            if (transfer == null)
            {
                return NotFound(new
                {
                    Message = "Không tìm thấy chuyển khoản"
                });
            }
            return Ok(transfer);
        }

        // POST: api/Transfer
        /// <summary>
        /// Tạo chuyển khoản mới
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> CreateTransfer([FromBody] TransferRequest request)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success)
            {
                return Unauthorized(new
                {
                    Message = message
                });
            }
            
            // Kiểm tra tài khoản nguồn và đích
            if (request.SourceAccountId == request.DestinationAccountId)
            {
                return BadRequest(new
                {
                    Message = "Tài khoản trùng nhau"
                });
            }
            var sourceAccount = await _context.Accounts.FindAsync(request.SourceAccountId);
            var destinationAccount = await _context.Accounts.FindAsync(request.DestinationAccountId);
            if (sourceAccount == null || destinationAccount == null)
            {
                return BadRequest(new
                {
                    Message = "Tài khoản nguồn hoặc tài khoản đích không tồn tại"
                });
            }
            if (sourceAccount.UserId != destinationAccount.UserId)
            {
                return BadRequest(new
                {
                    Message = "Tài khoản nguồn và tài khoản đích phải thuộc cùng một người dùng"
                });
            }
            if (sourceAccount.UserId != userId)
            {
                return BadRequest(new
                {
                    Message = "Bạn không có quyền sử dụng tài khoản nguồn hoặc tài khoản đích"
                });
            }

            var transfer = new Transfer
            {
                SourceAccountId = request.SourceAccountId,
                DestinationAccountId = request.DestinationAccountId,
                Amount = request.Amount,
                TransferDate = request.TransferDate.Date.ToUniversalTime(),
                Description = request.Description
            };

            sourceAccount.Balance -= request.Amount;
            destinationAccount.Balance += request.Amount;

            _context.Transfers.Add(transfer);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Chuyển khoản thành công",
                TransferId = transfer.Id
            });
        }

        // PUT: api/Transfer/{id}
        /// <summary>
        /// Cập nhật chuyển khoản
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateTransfer(Guid id, [FromBody] TransferRequest request)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            // 1. Lấy giao dịch kèm sẵn 2 ví cũ
            var transfer = await _context.Transfers
                .Include(t => t.Source)
                .Include(t => t.Destination)
                .FirstOrDefaultAsync(t => t.Id == id && t.Source!.UserId == userId);

            if (transfer == null) return NotFound(new { Message = "Không tìm thấy chuyển khoản" });

            // 2. TÌM CÁC VÍ MỚI
            var newSourceAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.SourceAccountId && a.UserId == userId);
            var newDestinationAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.DestinationAccountId && a.UserId == userId);

            if (newSourceAccount == null || newDestinationAccount == null)
            {
                return BadRequest(new { Message = "Tài khoản nguồn hoặc đích không hợp lệ/không thuộc quyền sở hữu" });
            }
            if (newSourceAccount.Id == newDestinationAccount.Id)
            {
                return BadRequest(new { Message = "Tài khoản nguồn và đích không được trùng nhau" });
            }

            // 3. HOÀN TIỀN LẠI CHO VÍ CŨ (Dùng thẳng object đã Include)
            transfer.Source!.Balance += transfer.Amount;
            transfer.Destination!.Balance -= transfer.Amount;

            // 4. ÁP DỤNG TRỪ TIỀN CHO VÍ MỚI
            newSourceAccount.Balance -= request.Amount;
            newDestinationAccount.Balance += request.Amount;

            // 5. CẬP NHẬT THÔNG TIN LỊCH SỬ CHUYỂN KHOẢN
            transfer.SourceAccountId = request.SourceAccountId;
            transfer.DestinationAccountId = request.DestinationAccountId;
            transfer.Amount = request.Amount;
            transfer.TransferDate = request.TransferDate.Date.ToUniversalTime();
            transfer.Description = request.Description;
            transfer.LastUpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Cập nhật thành công" });
        }


        // DELETE: api/Transfer/{id}
        /// <summary>
        /// Xóa chuyển khoản
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteTransfer(Guid id)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success)
            {
                return Unauthorized(new
                {
                    Message = message
                });
            }

            var transfer = await _context.Transfers.FindAsync(id);
            if (transfer == null)
            {
                return NotFound(new
                {
                    Message = "Không tìm thấy chuyển khoản"
                });
            }

            var sourceAccount = await _context.Accounts.FindAsync(transfer.SourceAccountId);
            var destinationAccount = await _context.Accounts.FindAsync(transfer.DestinationAccountId);
            if (sourceAccount == null || destinationAccount == null)
            {
                return BadRequest(new
                {
                    Message = "Tài khoản nguồn hoặc tài khoản đích không tồn tại"
                });
            }
            if (sourceAccount.UserId != userId)
            {
                return BadRequest(new
                {
                    Message = "Không có quyền xóa tài khoản."
                });
            }

            // Hoàn tác chuyển khoản
            sourceAccount.Balance += transfer.Amount;
            destinationAccount.Balance -= transfer.Amount;

            _context.Transfers.Remove(transfer);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Xóa chuyển khoản thành công"
            });
        }
    }
}
