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

            // ALGORITM 1: Försök hitta stabil höst med standardkriterier
            var autumnStart = FindSeasonTransition(dailyAverages, 12.0, 3);
            
            // ALGORITM 2: Om ingen stabil period, hitta första kalla dagen
            if (autumnStart == null)
            {
                autumnStart = FindFirstDayBelowThreshold(dailyAverages, 13.0);
            }
            
            // ALGORITM 3: Om fortfarande ingen, använd median-kallaste dagen
            if (autumnStart == null)
            {
                autumnStart = FindMedianColdDay(dailyAverages);
            }
            
            // ALGORITM 4: Som sista utväg, använd första dagen i dataset
            if (autumnStart == null)
            {
                autumnStart = dailyAverages.OrderBy(d => d.Date).First().Date;
            }

            // Vinterberäkning - anpassad för svenska förhållanden
            var winterStart = FindSeasonTransition(dailyAverages, 8.0, 3);
            if (winterStart == null)
            {
                winterStart = FindColdestPeriod(dailyAverages);
            }

            return new SeasonResult
            {
                AutumnStart = autumnStart,
                WinterStart = winterStart,
                Message = $"Säsongsberäkning klar för {location}. " +
                         $"Data från {dailyAverages.Min(d => d.Date):yyyy-MM-dd} till {dailyAverages.Max(d => d.Date):yyyy-MM-dd}"
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

        private static DateTime? FindFirstDayBelowThreshold(List<DailyAverage> dailyAverages, double threshold)
        {
            var firstColdDay = dailyAverages
                .Where(d => d.AvgTemperature.HasValue && d.AvgTemperature.Value < threshold)
                .OrderBy(d => d.Date)
                .FirstOrDefault();
            
            return firstColdDay?.Date;
        }

        private static DateTime? FindMedianColdDay(List<DailyAverage> dailyAverages)
        {
            var coldDays = dailyAverages
                .Where(d => d.AvgTemperature.HasValue)
                .OrderBy(d => d.AvgTemperature)
                .ToList();
                
            if (!coldDays.Any()) return null;
            
            int medianIndex = coldDays.Count / 2;
            return coldDays[medianIndex].Date;
        }

        private static DateTime? FindColdestPeriod(List<DailyAverage> dailyAverages)
        {
            if (!dailyAverages.Any(d => d.AvgTemperature.HasValue)) 
                return null;

            // Hitta den 3-dagars period med lägst medeltemperatur
            var coldestPeriod = dailyAverages
                .Where(d => d.AvgTemperature.HasValue)
                .OrderBy(d => d.AvgTemperature)
                .Take(3)
                .OrderBy(d => d.Date)
                .FirstOrDefault();
            
            return coldestPeriod?.Date;
        }
    }
}
