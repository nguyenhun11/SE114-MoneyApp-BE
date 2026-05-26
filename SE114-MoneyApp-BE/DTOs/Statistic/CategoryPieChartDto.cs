namespace SE114_MoneyApp_BE.DTOs.Statistic
{
    public class CategoryPieChartDto
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int ColorId { get; set; }
        public decimal TotalAmount { get; set; }
        public double Percentage { get; set; }
    }
}
