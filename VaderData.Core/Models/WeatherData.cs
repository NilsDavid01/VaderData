namespace VaderData.Core.Models
{
    public class WeatherData
    {
        public int Id { get; set; }
        public DateTime DateTime { get; set; }
        public string Location { get; set; } = string.Empty;
        public double? Temperature { get; set; }
        public double? Humidity { get; set; }
        public bool IsValid { get; set; } = true;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class DailyAverage
    {
        public DateTime Date { get; set; }
        public double? AvgTemperature { get; set; }
        public double? AvgHumidity { get; set; }
        public double? MoldRisk { get; set; }
    }

    public class SeasonResult
    {
        public DateTime? AutumnStart { get; set; }
        public DateTime? WinterStart { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
