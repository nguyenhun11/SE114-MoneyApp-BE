using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SE114_MoneyApp_BE.Data;
using SE114_MoneyApp_BE.DTOs.User;
using SE114_MoneyApp_BE.Models;

namespace SE114_MoneyApp_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        
        public UserController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserProfileResponse>> GetUserById([FromRoute] int id)
        {
            var userProfile = await _context.Users
                .Where(u => u.Id == id && u.IsActive == true)
                .Select(u => new UserProfileResponse
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    ImageUrl = u.ImageUrl,
                    PhoneNumber = u.PhoneNumber,
                    CreatedAt = u.CreatedAt
                })
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

        // GET: api/user/search?email=... hoặc api/user/search?phone=...
        [HttpGet("search")]
        public async Task<ActionResult<UserProfileResponse>> SearchUser(
            [FromQuery] string? email,
            [FromQuery] string? phone)
        {
            if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(phone))
            {
                return BadRequest(new { Message = "Vui lòng cung cấp Email hoặc Số điện thoại để tìm kiếm." });
            }

            var query = _context.Users.Where(u => u.IsActive);

            if (!string.IsNullOrEmpty(email))
            {
                query = query.Where(u => u.Email == email);
            }

            if (!string.IsNullOrEmpty(phone))
            {
                query = query.Where(u => u.PhoneNumber == phone);
            }

            var userProfile = await query
                .Select(u => new UserProfileResponse
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    ImageUrl = u.ImageUrl,
                    PhoneNumber = u.PhoneNumber,
                    CreatedAt = u.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (userProfile == null)
            {
                return NotFound(new { Message = "Không tìm thấy người dùng thỏa mãn điều kiện." });
            }

            return Ok(userProfile);
        }

        // PUT: api/user/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserProfileRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

            if (user == null)
            {
                return NotFound(new { Message = "Không tìm thấy người dùng hoặc tài khoản đã bị khóa." });
            }

            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == request.Email && u.Id != id);
            if (emailExists)
            {
                return BadRequest(new { Message = "Email này đã được sử dụng bởi một tài khoản khác!" });
            }

            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                var phoneExists = await _context.Users
                    .AnyAsync(u => u.PhoneNumber == request.PhoneNumber && u.Id != id);
                if (phoneExists)
                {
                    return BadRequest(new { Message = "Số điện thoại này đã được sử dụng bởi một tài khoản khác!" });
                }
            }

            user.Name = request.Name;
            user.Email = request.Email;
            user.ImageUrl = request.ImageUrl;
            user.PhoneNumber = request.PhoneNumber;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Cập nhật thông tin thành công!" });
        }

        // DELETE: api/user/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeactiveUser(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

            if (user == null)
            {
                return NotFound(new { Message = "Không tìm thấy người dùng hoặc tài khoản đã bị xóa trước đó." });
            }

            user.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đã xóa người dùng thành công!" });
        }
    }
}
