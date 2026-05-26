namespace SE114_MoneyApp_BE.DTOs.Statistic
{
    public class StackedBarChartDto
    {
        public string Period { get; set; } = string.Empty;
        public List<CategoryPieChartDto> CategoryBreakdowns { get; set; } = new();
    }
}
