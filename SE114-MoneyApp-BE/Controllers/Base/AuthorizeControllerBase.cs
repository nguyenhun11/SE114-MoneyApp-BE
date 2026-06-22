using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory; // Thêm thư viện Cache
using SE114_MoneyApp_BE.Data;
using System.Security.Claims;

namespace SE114_MoneyApp_BE.Controllers.Base
{
    [ApiController]
    [Authorize]
    public abstract class AuthorizeControllerBase : ControllerBase
    {
        protected readonly AppDbContext _context;
        protected readonly IMemoryCache _cache;

        public AuthorizeControllerBase(AppDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
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

        protected double ConvertCurrency(double amount, string fromCurrency, string toCurrency, Dictionary<string, double>? rates)
        {
            fromCurrency = fromCurrency.ToUpper();
            toCurrency = toCurrency.ToUpper();

            if (fromCurrency == toCurrency) return amount;

            if (rates == null || !rates.ContainsKey(fromCurrency) || !rates.ContainsKey(toCurrency))
            {
                if (fromCurrency == "USD" && toCurrency == "VND") return amount * 25000;
                if (fromCurrency == "VND" && toCurrency == "USD") return amount / 25000;
                return amount;
            }

            double amountInVND = amount / rates[fromCurrency];
            return amountInVND * rates[toCurrency];
        }
    }
}