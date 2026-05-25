using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SE114_MoneyApp_BE.Data;
using SE114_MoneyApp_BE.DTOs.Auth;
using SE114_MoneyApp_BE.Models;

namespace SE114_MoneyApp_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] RegisterRequest request)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
            {
                // A: Tài khoản cũ vẫn đang hoạt động bình thường
                if (existingUser.IsActive)
                {
                    return BadRequest(new { Message = "Email này đã được sử dụng trong hệ thống!" });
                }

                // B: Tài khoản cũ đã bị XÓA MỀM trước đó -> Tiến hành "Hồi sinh"
                existingUser.Name = request.Name;
                existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                existingUser.IsActive = true; // Kích hoạt lại tài khoản
                existingUser.CreatedAt = DateTime.UtcNow; // Cập nhật lại ngày kích hoạt mới nếu cần

                await _context.SaveChangesAsync();
                return Ok(new { Message = "Tài khoản cũ của bạn đã được khôi phục và kích hoạt thành công!" });
            }

            // C: Email hoàn toàn mới -> Tạo mới như bình thường
            var newUser = new User
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đăng ký tài khoản mới thành công!" });
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return BadRequest(new { Message = "Email hoặc mật khẩu không chính xác!" });
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return BadRequest(new { Message = "Email hoặc mật khẩu không chính xác!" });
            }

            var response = new AuthResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Token = "MOCK_JWT_TOKEN"
            };

            return Ok(response);
        }
    }
}
