using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE114_MoneyApp_BE.Data;
using System.Security.Claims;

namespace SE114_MoneyApp_BE.Controllers.Base
{
    [ApiController]
    [Authorize]
    public abstract class AuthorizeControllerBase : ControllerBase
    {
        protected readonly AppDbContext _context;
        public AuthorizeControllerBase(AppDbContext context)
        {
            _context = context;
        }
        protected (int userId, bool success, string message) GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return (0, false, "Đăng nhập không hợp lệ");
            }
            return (int.Parse(userIdClaim), true, string.Empty);
        }

    }
}
