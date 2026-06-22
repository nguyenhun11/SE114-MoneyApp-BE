namespace SE114_MoneyApp_BE.DTOs.City
{
    public class CityResponse
    {
        public int Level { get; set; }
        public int ProsperityPoints { get; set; }
        public int StabilityPoints { get; set; }
        public int CurrentStreak { get; set; }
        public List<BuildingDto> Buildings { get; set; } = new();
    }

    public class BuildingDto
    {
        public int Id { get; set; }
        public string BuildingType { get; set; } = string.Empty;
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public int Level { get; set; }
    }
}
