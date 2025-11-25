using VaderData.Core.Models;

namespace VaderData.Core.Algorithms
{
    public static class SeasonCalculator
    {
        public static SeasonResult CalculateSeasonsFromDailyAverages(List<DailyAverage> dailyAverages, string location)
        {
            if (dailyAverages == null || !dailyAverages.Any())
            {
                return new SeasonResult 
                { 
                    Message = "Ingen data tillgänglig för säsongsberäkning" 
                };
            }

            var autumnStart = FindSeasonTransition(dailyAverages, 10.0, 5);
            var winterStart = FindSeasonTransition(dailyAverages, 0.0, 5);

            return new SeasonResult
            {
                AutumnStart = autumnStart,
                WinterStart = winterStart,
                Message = $"Säsongsberäkning klar för {location}. Data från {dailyAverages.Min(d => d.Date):yyyy-MM-dd} till {dailyAverages.Max(d => d.Date):yyyy-MM-dd}"
            };
        }

        private static DateTime? FindSeasonTransition(List<DailyAverage> dailyAverages, double threshold, int consecutiveDays)
        {
            for (int i = 0; i <= dailyAverages.Count - consecutiveDays; i++)
            {
                var consecutive = dailyAverages.Skip(i).Take(consecutiveDays);
                if (consecutive.All(d => d.AvgTemperature.HasValue && d.AvgTemperature.Value < threshold))
                {
                    return consecutive.First().Date;
                }
            }
            return null;
        }
    }
}
