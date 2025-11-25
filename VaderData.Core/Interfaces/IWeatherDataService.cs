using VaderData.Core.Models;

namespace VaderData.Core.Interfaces
{
    public interface IWeatherDataService
    {
        Task InitializeDatabaseAsync();
        Task LoadDataFromCsvAsync(string filePath);
        Task<List<DailyAverage>> GetDailyAveragesAsync(DateTime date, string location);
        Task<List<DailyAverage>> GetTemperatureSortedAsync(string location);
        Task<List<DailyAverage>> GetHumiditySortedAsync(string location);
        Task<List<DailyAverage>> GetMoldRiskSortedAsync(string location);
        Task<SeasonResult> GetSeasonsAsync(string location);
        Task<List<WeatherData>> GetRawDataAsync(DateTime? startDate = null, DateTime? endDate = null);
    }
}
