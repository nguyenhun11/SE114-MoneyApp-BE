using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public UserController(AppDbContext context) : base(context) { }

        private static Expression<Func<User, UserProfileResponse>> MapToUserProfileResponse = user => new UserProfileResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            ImageUrl = user.ImageUrl,
            PhoneNumber = user.PhoneNumber,
            CreatedAt = user.CreatedAt,
            LastUpdatedAt = user.LastUpdatedAt
        };

        // GET: api/User
        /// <summary>
        /// Lấy thông tin người dùng hiện tại
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<UserProfileResponse>> GetUser()
        {
            var (userId, sucess, message) = GetCurrentUserId();
            if (!sucess)
            {
                return Unauthorized(new { Message = message });
            }

            var userProfile = await _context.Users
                .Where(u => u.Id == userId && u.IsActive == true)
                .Select(MapToUserProfileResponse)
                .FirstOrDefaultAsync();
            
            if (userProfile == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy người dùng hoặc tài khoản đã bị vô hiệu hóa."
                });
            }
            else
            {
                return Ok(userProfile);
            }
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
            user.PhoneNumber = request.PhoneNumber;
            user.LastUpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Cập nhật thông tin thành công!" });
        }

        // DELETE: api/user/{id}
        /// <summary>
        /// Hủy kích hoạt (xóa mềm) người dùng hiện tại
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public async Task<IActionResult> DeactiveUser()
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success)
            {
                return Unauthorized(new { Message = message });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null)
            {
                return NotFound(new { Message = "Không tìm thấy người dùng hoặc tài khoản đã bị xóa trước đó." });
            }

            user.IsActive = false;
            user.LastUpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đã xóa người dùng thành công!" });
        }
    }
}
