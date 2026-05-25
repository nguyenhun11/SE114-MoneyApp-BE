using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SE114_MoneyApp_BE.Data;
using SE114_MoneyApp_BE.DTOs.Transfer;
using SE114_MoneyApp_BE.Models;
using System.Linq.Expressions;

namespace SE114_MoneyApp_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransferController : ControllerBase
    {
        private readonly AppDbContext _context;
        public TransferController(AppDbContext context)
        {
            _context = context;
        }

        private static Expression<Func<Transfer, TransferResponse>> MapToTransferResponse = t => new TransferResponse
        {
            Id = t.Id,
            SourceAccount = t.SourceAccountId,
            SourceAccountName = t.Source!.AccountName,
            DestinationAccount = t.DestinationAccountId,
            DestinationAccountName = t.Destination!.AccountName,
            Amount = t.Amount,
            TransferDate = t.TransferDate,
            Description = t.Description,
            CreatedAt = t.CreatedAt,
            LastUpdatedAt = t.LastUpdatedAt
        };

        // GET: api/Transfer/{userId}?startDate=2024-01-01&endDate=2024-12-31&source=accountId&destination=accountId
        /// <summary>
        /// Danh sách các chuyển khoản của người dùng, có thể lọc theo ngày tháng, tài khoản nguồn và tài khoản đích
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        [HttpGet("{userId}")]
        public async Task<ActionResult<List<TransferResponse>>> GetTransfers(int userId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] Guid? source = null,
            [FromQuery] Guid? destination = null)
        {
            var query = _context.Transfers
                .Where(t => t.Source!.UserId == userId)
                .AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt <= endDate.Value);
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
                .OrderByDescending(t => t.CreatedAt)
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
            var transfer = await _context.Transfers
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

            var transfer = new Transfer
            {
                SourceAccountId = request.SourceAccountId,
                DestinationAccountId = request.DestinationAccountId,
                Amount = request.Amount,
                TransferDate = request.TransferDate,
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
            var transfer = await _context.Transfers.FindAsync(id);
            if (transfer == null)
            {
                return NotFound(new { Message = "Không tìm thấy chuyển khoản" });
            }

            // TÌM LẠI CÁC VÍ CŨ ĐỂ HOÀN TIỀN
            var oldSourceAccount = await _context.Accounts.FindAsync(transfer.SourceAccountId);
            var oldDestinationAccount = await _context.Accounts.FindAsync(transfer.DestinationAccountId);

            if (oldSourceAccount != null && oldDestinationAccount != null)
            {
                oldSourceAccount.Balance += transfer.Amount;
                oldDestinationAccount.Balance -= transfer.Amount;
            }

            // TÌM CÁC VÍ MỚI THEO REQUEST
            var newSourceAccount = await _context.Accounts.FindAsync(request.SourceAccountId);
            var newDestinationAccount = await _context.Accounts.FindAsync(request.DestinationAccountId);

            if (newSourceAccount == null || newDestinationAccount == null)
            {
                return BadRequest(new { Message = "Tài khoản nguồn hoặc tài khoản đích mới không tồn tại" });
            }
            if (newSourceAccount.UserId != newDestinationAccount.UserId)
            {
                return BadRequest(new { Message = "Tài khoản nguồn và tài khoản đích phải thuộc cùng một người dùng" });
            }
            if (newSourceAccount.Id == newDestinationAccount.Id)
            {
                return BadRequest(new { Message = "Tài khoản nguồn và đích không được trùng nhau" });
            }

            // ÁP DỤNG GIAO DỊCH MỚI LÊN VÍ MỚI
            newSourceAccount.Balance -= request.Amount;
            newDestinationAccount.Balance += request.Amount;

            // CẬP NHẬT THÔNG TIN LỊCH SỬ CHUYỂN KHOẢN
            transfer.SourceAccountId = request.SourceAccountId;
            transfer.DestinationAccountId = request.DestinationAccountId;
            transfer.Amount = request.Amount;
            transfer.TransferDate = request.TransferDate;
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
