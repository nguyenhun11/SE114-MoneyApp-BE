using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SE114_MoneyApp_BE.Controllers.Base;
using SE114_MoneyApp_BE.Data;
using SE114_MoneyApp_BE.DTOs.User;
using SE114_MoneyApp_BE.Models;
using System.Linq.Expressions;

namespace SE114_MoneyApp_BE.Controllers
{
    [Route("api/[controller]")]
    public class UserController : AuthorizeControllerBase
    {      
        public UserController(AppDbContext context, IMemoryCache cache) : base(context, cache) { }

        private static Expression<Func<User, UserProfileResponse>> MapToUserProfileResponse = user => new UserProfileResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            ImageUrl = user.ImageUrl,
            PhoneNumber = user.PhoneNumber,
            DailyStreak = user.DailyStreak,
            DefaultCurrency = user.DefaultCurrency,
            CreatedAt = user.CreatedAt,
            LastUpdatedAt = user.LastUpdatedAt
        };

        // GET: api/User
        /// <summary>
        /// Lấy thông tin người dùng hiện tại
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<UserProfileResponse>> GetUser([FromQuery] DateTime? clientToday)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive == true);

            if (user == null) return NotFound(new { message = "Không tìm thấy người dùng" });

            int displayStreak = user.DailyStreak;

            // Nếu Android có truyền ngày lên, ta mới check đứt chuỗi
            if (user.LastActiveDate.HasValue)
            {
                // Lấy ngày hiện tại chuẩn UTC (để tránh lệch múi giờ)
                DateTime serverToday = DateTime.UtcNow.Date;
                DateTime lastActiveDay = user.LastActiveDate.Value.Date;

                // Nếu ngày cuối cùng hoạt động nhỏ hơn NGÀY HÔM QUA (tức là cách đây >= 2 ngày)
                // Thì hiển thị cho người dùng là 0 (Đứt chuỗi rồi)
                if (lastActiveDay < serverToday.AddDays(-1))
                {
                    displayStreak = 0;
                }
            }

            var response = new UserProfileResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                ImageUrl = user.ImageUrl,
                PhoneNumber = user.PhoneNumber,
                DailyStreak = displayStreak,
                TodayCheckedIn = user.LastActiveDate.HasValue && user.LastActiveDate.Value.Date == DateTime.UtcNow.Date,
                CreatedAt = user.CreatedAt,
                LastUpdatedAt = user.LastUpdatedAt
            };

            return Ok(response);
        }

        // GET: api/User/search?email=... hoặc api/user/search?phone=...
        /// <summary>
        /// (*) Tìm thông tin người dùng theo id, email hoặc số điện thoại
        /// </summary>
        /// <param name="id"></param>
        /// <param name="email"></param>
        /// <param name="phone"></param>
        /// <returns></returns>
        [HttpGet("search")]
        public async Task<ActionResult<UserProfileResponse>> SearchUser(
            [FromQuery] int? id,
            [FromQuery] string? email,
            [FromQuery] string? phone)
        {
            if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(phone))
            {
                return BadRequest(new { Message = "Vui lòng cung cấp Email hoặc Số điện thoại để tìm kiếm." });
            }

            var query = _context.Users.Where(u => u.IsActive);
            if (!id.HasValue && string.IsNullOrEmpty(email) && string.IsNullOrEmpty(phone))
            {
                return BadRequest(new { Message = "Vui lòng cung cấp ít nhất ID, Email hoặc Số điện thoại." });
            }
            if (id.HasValue)
            {
                query = query.Where(u => u.Id == id.Value);
            }
            if (!string.IsNullOrEmpty(email))
            {
                query = query.Where(u => u.Email == email);
            }

            if (!string.IsNullOrEmpty(phone))
            {
                query = query.Where(u => u.PhoneNumber == phone);
            }

            var userProfile = await query
                .Select(MapToUserProfileResponse)
                .FirstOrDefaultAsync();

            if (userProfile == null)
            {
                return NotFound(new { Message = "Không tìm thấy người dùng thỏa mãn điều kiện." });
            }

            return Ok(userProfile);
        }

        //GET: api/User/all
        /// <summary>
        /// (*) Lấy mã và email tất cả người dùng hiện tại
        /// </summary>
        /// <returns></returns>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllUsers()
        {
            var totalActive = await _context.Users.CountAsync(u => u.IsActive);
            var totalDeactive = await _context.Users.CountAsync(u => !u.IsActive);

            var users = await _context.Users
                .Where(u => u.IsActive)
                .Select(u => new UserProfileResponse 
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email
                })
                .ToListAsync();

            var result = new
            {
                TotalActive = totalActive,
                TotalDeactive = totalDeactive,
                Users = users
            };

            return Ok(result);
        }

        // PUT: api/user
        /// <summary>
        /// Sửa đổi thông tin người dùng hiện tại
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<IActionResult> UpdateUser([FromBody] UserProfileRequest request)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success)
            {
                return Unauthorized(new { Message = message });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null)
            {
                return NotFound(new { Message = "Không tìm thấy người dùng hoặc tài khoản đã bị khóa." });
            }

            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == request.Email && u.Id != userId);
            if (emailExists)
            {
                return BadRequest(new { Message = "Email này đã được sử dụng bởi một tài khoản khác!" });
            }

            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                var phoneExists = await _context.Users
                    .AnyAsync(u => u.PhoneNumber == request.PhoneNumber && u.Id != userId);
                if (phoneExists)
                {
                    return BadRequest(new { Message = "Số điện thoại này đã được sử dụng bởi một tài khoản khác!" });
                }
            }

            user.Name = request.Name;
            user.Email = request.Email;
            user.ImageUrl = request.ImageUrl;
            user.DefaultCurrency = request.DefaultCurrency;
            user.PhoneNumber = request.PhoneNumber;
            user.LastUpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Cập nhật thông tin thành công!" });
        }

        // DELETE: api/user
        /// <summary>
        /// Xóa người dùng (Tùy chọn: soft_delete để khóa tài khoản, hoặc wipe_data để xóa vĩnh viễn toàn bộ dữ liệu)
        /// </summary>
        /// <param name="mode">"soft_delete" (mặc định) hoặc "wipe_data"</param>
        /// <returns></returns>
        [HttpDelete]
        public async Task<IActionResult> DeleteUser([FromQuery] string mode = "soft_delete")
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null)
            {
                return NotFound(new { Message = "Không tìm thấy người dùng hoặc tài khoản đã bị khóa trước đó." });
            }

            if (mode.ToLower() == "wipe_data")
            {
                // KỊCH BẢN 1: XÓA VĨNH VIỄN (RIGHT TO BE FORGOTTEN)
                // Phải xóa theo thứ tự TỪ CON ĐẾN CHA để không bị lỗi Khóa ngoại (Foreign Key)

                // 1. Lấy danh sách ID các ví của User này
                var accountIds = await _context.Accounts
                    .Where(a => a.UserId == userId)
                    .Select(a => a.Id)
                    .ToListAsync();

                // 2. Xóa toàn bộ Transfers liên quan đến các ví này
                var transfers = await _context.Transfers
                    .Where(t => accountIds.Contains(t.SourceAccountId) || accountIds.Contains(t.DestinationAccountId))
                    .ToListAsync();
                _context.Transfers.RemoveRange(transfers);

                // 3. Xóa toàn bộ AdjustBalances
                var adjustBalances = await _context.AdjustBalances
                    .Where(ab => accountIds.Contains(ab.AccountId))
                    .ToListAsync();
                _context.AdjustBalances.RemoveRange(adjustBalances);

                // 4. Xóa toàn bộ Transactions
                var transactions = await _context.Transactions
                    .Where(t => accountIds.Contains(t.AccountId))
                    .ToListAsync();
                _context.Transactions.RemoveRange(transactions);

                // 5. Xóa toàn bộ Accounts
                var accounts = await _context.Accounts.Where(a => a.UserId == userId).ToListAsync();
                _context.Accounts.RemoveRange(accounts);

                // 6. Xóa toàn bộ Categories
                var categories = await _context.Categories.Where(c => c.UserId == userId).ToListAsync();
                _context.Categories.RemoveRange(categories);

                // 7. Cuối cùng, búng tay bay màu User
                _context.Users.Remove(user);
            }
            else
            {
                // KỊCH BẢN 2: SOFT DELETE (CHỈ KHÓA TÀI KHOẢN)
                user.IsActive = false;
                user.LastUpdatedAt = DateTime.UtcNow;

                // THỦ THUẬT AN TOÀN: Ẩn luôn toàn bộ Ví và Danh mục để vô hiệu hóa hoàn toàn dữ liệu
                var accounts = await _context.Accounts.Where(a => a.UserId == userId && a.IsActive).ToListAsync();
                foreach (var acc in accounts)
                {
                    acc.IsActive = false;
                    acc.IncludeInTotalBalance = false; // Ngắt khỏi thống kê
                    acc.LastUpdatedAt = DateTime.UtcNow;
                }

                var categories = await _context.Categories.Where(c => c.UserId == userId && c.IsActive).ToListAsync();
                foreach (var cat in categories)
                {
                    cat.IsActive = false;
                    cat.LastUpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đã xử lý xóa tài khoản thành công!" });
        }

        // POST: api/user/checkin
        /// <summary>
        /// Ghi nhận hoạt động trong ngày để tăng chuỗi (Streak)
        /// </summary>
        [HttpPost("checkin")]
        public async Task<IActionResult> DailyCheckIn([FromBody] CheckInRequest request)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user == null) return NotFound(new { Message = "Không tìm thấy người dùng." });

            DateTime clientToday = request.ClientToday.Date;
            bool isStreakIncreased = false;
            string responseMessage = "";

            if (!user.LastActiveDate.HasValue)
            {
                user.DailyStreak = 1;
                user.LastActiveDate = clientToday;
                isStreakIncreased = true;
                responseMessage = "Khởi đầu tuyệt vời!";
            }
            else
            {
                DateTime lastActiveDay = user.LastActiveDate.Value.Date;

                if (lastActiveDay == clientToday)
                {
                    return Ok(new { Message = "Bạn đã điểm danh hôm nay rồi!", CurrentStreak = user.DailyStreak });
                }
                else if (lastActiveDay == clientToday.AddDays(-1))
                {
                    user.DailyStreak += 1;
                    user.LastActiveDate = clientToday;
                    isStreakIncreased = true;
                    responseMessage = $"Tuyệt vời! Chuỗi {user.DailyStreak} ngày.";
                }
                else
                {
                    user.DailyStreak = 1;
                    user.LastActiveDate = clientToday;
                    isStreakIncreased = true;
                    responseMessage = "Bắt đầu hành trình mới!";
                }
            }

            user.LastUpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { Message = responseMessage, CurrentStreak = user.DailyStreak, IsIncreased = isStreakIncreased });
        }

        // POST: api/user/restore-streak
        /// <summary>
        /// Khôi phục chuỗi
        /// </summary>
        [HttpPost("restore-streak")]
        public async Task<IActionResult> RestoreStreak([FromBody] CheckInRequest request)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user == null) return NotFound(new { Message = "Không tìm thấy người dùng." });

            DateTime clientToday = request.ClientToday.Date;

            // Kiểm tra xem có thực sự bị đứt chuỗi không (cách > 1 ngày)
            if (user.LastActiveDate.HasValue && user.LastActiveDate.Value.Date < clientToday.AddDays(-1))
            {
                user.DailyStreak += 1;
                user.LastActiveDate = clientToday;
                user.LastUpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Chuỗi của bạn đã được khôi phục.",
                    CurrentStreak = user.DailyStreak,
                    IsIncreased = true
                });
            }

            return BadRequest(new { Message = "Chuỗi của bạn chưa bị đứt, không cần khôi phục!" });
        }
    }
}
