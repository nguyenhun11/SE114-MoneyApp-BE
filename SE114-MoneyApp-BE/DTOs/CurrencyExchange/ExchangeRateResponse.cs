using System.Text.Json.Serialization;

namespace SE114_MoneyApp_BE.DTOs.CurrencyExchange
{
    public class ExchangeRateResponse
    {
        [JsonPropertyName("result")]
        public string Result { get; set; }

        [JsonPropertyName("base_code")]
        public string BaseCode { get; set; }

        [JsonPropertyName("conversion_rates")]
        public Dictionary<string, double> ConversionRates { get; set; }
    }
}
