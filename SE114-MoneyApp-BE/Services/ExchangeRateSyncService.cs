using Microsoft.Extensions.Caching.Memory;
using SE114_MoneyApp_BE.DTOs.CurrencyExchange;
using System.Text.Json;

namespace SE114_MoneyApp_BE.Services
{
    public class ExchangeRateSyncService : BackgroundService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ExchangeRateSyncService> _logger;

        public ExchangeRateSyncService(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            IConfiguration configuration,
            ILogger<ExchangeRateSyncService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Đang đồng bộ tỷ giá tiền tệ lúc: {time}", DateTimeOffset.Now);
                    await FetchAndCacheRatesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi đồng bộ tỷ giá");
                }

                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
            }
        }

        private async Task FetchAndCacheRatesAsync()
        {
            var baseUrl = _configuration["ExchangeRateApi:BaseUrl"];
            var apiKey = _configuration["ExchangeRateApi:ApiKey"];
            var baseCurrency = _configuration["ExchangeRateApi:BaseCurrency"]; // VD: VND

            var requestUrl = $"{baseUrl}{apiKey}/latest/{baseCurrency}";

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(requestUrl);

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<ExchangeRateResponse>(jsonString);

                if (data != null && data.Result == "success")
                {
                    _cache.Set("LatestExchangeRates", data.ConversionRates, TimeSpan.FromHours(24));
                    _logger.LogInformation("Đồng bộ tỷ giá thành công. Số lượng đồng tiền: {count}", data.ConversionRates.Count);
                }
            }
            else
            {
                _logger.LogWarning("API trả về mã lỗi: {statusCode}", response.StatusCode);
            }
        }
    }
}
