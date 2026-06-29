namespace SE114_MoneyApp_BE.DTOs.City
{
    public class RankingResponse
    {
        public int Rank { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int ProsperityPoints { get; set; }
        public int StabilityPoints { get; set; }
        public int CityLevel { get; set; }
    }
}
