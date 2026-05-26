using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SE114_MoneyApp_BE.Data;
using SE114_MoneyApp_BE.DTOs.Auth;
using SE114_MoneyApp_BE.Models;
using SE114_MoneyApp_BE.Services;

namespace SE114_MoneyApp_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly TokenService _tokenService;
        public AuthController(AppDbContext context, 
            IConfiguration configuration,
            TokenService tokenService)
        {
            _context = context;
            _configuration = configuration;
            _tokenService = tokenService;
        }

        // POST: api/auth/register
        /// <summary>
        /// Đăng ký bằng email
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Đăng nhập bằng email
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            // Kiểm tra tồn tại
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

            // Tạo token
            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshTokenString = _tokenService.GenerateRefreshToken();
            var newRefreshToken = new RefreshToken
            {
                Token = refreshTokenString,
                ExpiresAt = DateTime.UtcNow.AddDays(double.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7")),
                UserId = user.Id
            };

            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync();

            var response = new AuthResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Token = accessToken,
                RefreshToken = refreshTokenString
            };

            return Ok(response);
        }

        // POST: api/auth/google-login
        /// <summary>
        /// Đăng nhập - đăng ký tài khoản bằng Google
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("google-login")]
        public async Task<ActionResult<AuthResponse>> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            GoogleJsonWebSignature.Payload payload;

            try
            {
                // Xác thực ID Token gửi từ Android với server Google
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _configuration["GoogleAuth:ClientId"] }
                };

                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
            }
            catch (InvalidJwtException)
            {
                return BadRequest(new { Message = "Google ID Token không hợp lệ hoặc đã hết hạn!" });
            }
            catch (Exception)
            {
                return BadRequest(new { Message = "Xác thực tài khoản Google thất bại!" });
            }

            string googleId = payload.Subject; // ID duy nhất của user trên hệ thống Google
            string email = payload.Email;
            string name = payload.Name;
            string? imageUrl = payload.Picture;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user != null)
            {

                if (!user.IsActive)
                {
                    user.Name = name;
                    user.GoogleId = googleId;
                    user.ImageUrl = imageUrl ?? user.ImageUrl;
                    user.IsActive = true; // Kích hoạt lại
                    user.CreatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                }

                else if (string.IsNullOrEmpty(user.GoogleId))
                {
                    user.GoogleId = googleId;
                    if (string.IsNullOrEmpty(user.ImageUrl)) user.ImageUrl = imageUrl;

                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                user = new User
                {
                    Name = name,
                    Email = email,
                    GoogleId = googleId,
                    ImageUrl = imageUrl,
                    PasswordHash = string.Empty
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            // Sinh token
            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshTokenString = _tokenService.GenerateRefreshToken();

            var newRefreshToken = new RefreshToken
            {
                Token = refreshTokenString,
                ExpiresAt = DateTime.UtcNow.AddDays(double.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7")),
                UserId = user.Id
            };

            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync();

            var response = new AuthResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Token = accessToken,
                RefreshToken = refreshTokenString
            };

            return Ok(response);
        }

        // POST: api/auth/refresh-token
        /// <summary>
        /// Cấp lại Access Token và Refresh Token mới (Cơ chế xoay vòng - Token Rotation)
        /// </summary>
        /// <remarks>
        /// API này không được gọi qua thao tác bấm nút của người dùng. Nó được Frontend gọi ngầm (âm thầm) dưới nền khi Access Token cũ hết hạn.
        /// 
        /// Kịch bản tích hợp tại Frontend:
        /// 1. Frontend gắn Access Token vào Header và gọi các API nghiệp vụ (ví dụ: Lấy danh sách giao dịch).
        /// 2. Nếu Access Token hết hạn, Server trả về lỗi HTTP 401 (Unauthorized).
        /// 3. Bộ đánh chặn (Interceptor/Authenticator) của Frontend bắt được lỗi 401 -> Dừng request cũ lại.
        /// 4. Frontend tự động gọi API này, truyền Refresh Token đang lưu trong máy lên.
        /// 5. Nhận cặp Token mới -> Cập nhật lại vào bộ nhớ cục bộ.
        /// 6. Gắn Access Token mới vào Request bị lỗi ở Bước 1 và tự động gọi lại lần 2.
        /// </remarks>
        // POST: api/auth/refresh-token
        [HttpPost("refresh-token")]
        public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            // 1. Tìm Refresh Token này trong cơ sở dữ liệu kèm thông tin người dùng
            var savedToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            // 2. Kiểm tra tính hợp lệ của Token
            if (savedToken == null || savedToken.IsExpired || !savedToken.User.IsActive)
            {
                return Unauthorized(new { Message = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ. Vui lòng đăng nhập lại!" });
            }

            // 3. Cơ chế xoay vòng Token (Token Rotation): Xóa hoặc hủy token cũ để tránh bị tái sử dụng bừa bãi
            _context.RefreshTokens.Remove(savedToken);

            // 4. Sinh cặp mã mới tinh (Access Token & Refresh Token)
            var newAccessToken = _tokenService.GenerateAccessToken(savedToken.User);
            var newRefreshTokenString = _tokenService.GenerateRefreshToken();

            // 5. Lưu Refresh Token mới vào database gắn với user
            var newRefreshToken = new RefreshToken
            {
                Token = newRefreshTokenString,
                ExpiresAt = DateTime.UtcNow.AddDays(double.Parse(_configuration["Jwt:RefreshTokenExpirationDays"]!)),
                UserId = savedToken.UserId
            };

            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync();

            // 6. Trả về thông tin cặp mã mới cho phía App di động cập nhật bộ nhớ cục bộ
            var response = new AuthResponse
            {
                Id = savedToken.User.Id,
                Name = savedToken.User.Name,
                Email = savedToken.User.Email,
                Token = newAccessToken,
                RefreshToken = newRefreshTokenString
            };

            return Ok(response);
        }

        // POST: api/auth/logout
        /// <summary>
        /// Đăng xuất và hủy Refresh Token hiện tại
        /// </summary>
        [HttpPost("logout")]
        [Microsoft.AspNetCore.Authorization.Authorize] 
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            var tokenRecord = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (tokenRecord != null)
            {
                _context.RefreshTokens.Remove(tokenRecord);
                await _context.SaveChangesAsync();
            }

            return Ok(new { Message = "Đăng xuất thành công!" });
        }

        // POST: api/auth/change-password
        /// <summary>
        /// Đổi mật khẩu
        /// </summary>
        [HttpPost("change-password")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { Message = "Không thể xác định danh tính người dùng" });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
            {
                return NotFound(new { Message = "Tài khoản không tồn tại hoặc đã bị khóa" });
            }

            // Người dùng đăng nhập bằng Google không có mật khẩu, không cho đổi
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                return BadRequest(new { Message = "Tài khoản đăng nhập bằng Google không thể đổi mật khẩu qua chức năng này" });
            }

            // Kiểm tra mật khẩu cũ
            bool isOldPasswordValid = BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash);
            if (!isOldPasswordValid)
            {
                return BadRequest(new { Message = "Mật khẩu cũ không chính xác!" });
            }

            // Mã hóa và lưu mật khẩu mới
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đổi mật khẩu thành công!" });
        }
    }
}
