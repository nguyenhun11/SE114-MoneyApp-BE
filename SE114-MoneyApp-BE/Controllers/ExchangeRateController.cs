using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SE114_MoneyApp_BE.Controllers.Base;
using SE114_MoneyApp_BE.Data;
using System;
using System.Collections.Generic;

namespace SE114_MoneyApp_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExchangeRateController : AuthorizeControllerBase
    {
        public ExchangeRateController(AppDbContext context, IMemoryCache cache) : base(context, cache) { }

        // GET: api/ExchangeRate
        /// <summary>
        /// Lấy toàn bộ 160+ mã đơn vị tiền tệ và tỷ giá động trực tiếp từ API thế giới trong RAM
        /// </summary>
        [HttpGet]
        public IActionResult GetSupportedCurrencies()
        {
            if (_cache.TryGetValue("LatestExchangeRates", out Dictionary<string, double>? globalRates) && globalRates != null)
            {
                return Ok(new
                {
                    Message = "Success",
                    BaseCurrency = "VND", // Trạm trung chuyển toán học cố định để FE tính chéo
                    Timestamp = DateTime.UtcNow,

                    Currencies = globalRates.Keys,
                    Rates = globalRates
                });
            }

            // Fallback tối thiểu khi server vừa bật lên, service ngầm chưa kịp sync xong lượt đầu
            var fallbackRates = new Dictionary<string, double>
            {
                { "VND", 1.0 },
                { "USD", 0.00004 },
                { "EUR", 0.000037 },
                { "JPY", 0.0062 }
            };

            return Ok(new
            {
                Message = "Using Fallback Data",
                BaseCurrency = "VND",
                Timestamp = DateTime.UtcNow,
                Currencies = fallbackRates.Keys,
                Rates = fallbackRates
            });
        }
    }
}