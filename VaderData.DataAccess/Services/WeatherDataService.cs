using VaderData.Core.Interfaces;
using VaderData.Core.Models;
using VaderData.Core.Algorithms;
using Microsoft.Extensions.Logging;
using System.Globalization;
using VaderData.DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace VaderData.DataAccess.Services
{
    public class WeatherDataService : IWeatherDataService
    {
        private readonly ILogger<WeatherDataService> _logger;
        private readonly WeatherContext _context;

        public WeatherDataService(ILogger<WeatherDataService> logger, WeatherContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                await _context.Database.EnsureCreatedAsync();
                _logger.LogInformation("Database initialized successfully");
                Console.WriteLine("✅ Databas initialiserad!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing database");
                Console.WriteLine($"❌ Fel vid databasinitialisering: {ex.Message}");
            }
        }

        public async Task LoadDataFromCsvAsync(string filePath)
        {
            try
            {
                _logger.LogInformation($"Loading data from CSV: {filePath}");
                
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"❌ Filen hittades inte: {filePath}");
                    return;
                }

                var lines = await File.ReadAllLinesAsync(filePath);
                Console.WriteLine($"📖 Läser {lines.Length} rader från CSV...");

                var weatherData = new List<WeatherData>();
                int validRows = 0;
                int invalidRows = 0;

                for (int i = 1; i < lines.Length; i++)
                {
                    try
                    {
                        var line = lines[i];
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var data = ParseCsvLine(line, i);
                        if (data != null && data.IsValid)
                        {
                            weatherData.Add(data);
                            validRows++;
                        }
                        else
                        {
                            invalidRows++;
                            if (invalidRows <= 5)
                            {
                                Console.WriteLine($"❌ Rad {i}: {data?.ErrorMessage}");
                            }
                        }

                        if (i % 10000 == 0)
                        {
                            Console.WriteLine($"📊 Processed {i} rows...");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Error parsing line {i}");
                        invalidRows++;
                    }
                }

                if (weatherData.Any())
                {
                    Console.WriteLine($"💾 Sparar {weatherData.Count} rader till databasen...");
                    
                    _context.WeatherData.RemoveRange(_context.WeatherData);
                    await _context.SaveChangesAsync();
                    
                    const int batchSize = 1000;
                    for (int i = 0; i < weatherData.Count; i += batchSize)
                    {
                        var batch = weatherData.Skip(i).Take(batchSize).ToList();
                        await _context.WeatherData.AddRangeAsync(batch);
                        await _context.SaveChangesAsync();
                        Console.WriteLine($"💾 Batch sparad: {Math.Min(i + batchSize, weatherData.Count)} / {weatherData.Count}");
                    }
                    
                    Console.WriteLine($"✅ Data laddad successfully!");
                    Console.WriteLine($"📈 Valida rader: {validRows}, Ogiltiga rader: {invalidRows}");
                }
                else
                {
                    Console.WriteLine("❌ Ingen giltig data hittades i CSV-filen.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fel vid inläsning av CSV: {ex.Message}");
                _logger.LogError(ex, "Error loading CSV data");
            }
        }

        private WeatherData ParseCsvLine(string line, int lineNumber)
        {
            try
            {
                var fields = line.Split(',');
                
                if (fields.Length < 4)
                {
                    return new WeatherData 
                    { 
                        IsValid = false, 
                        ErrorMessage = $"Otillräckligt med fält: {fields.Length}. Förväntade 4 fält." 
                    };
                }

                var dateString = fields[0].Trim();
                if (!DateTime.TryParse(dateString, new CultureInfo("sv-SE"), DateTimeStyles.None, out DateTime dateTime))
                {
                    return new WeatherData 
                    { 
                        IsValid = false, 
                        ErrorMessage = $"Ogiltigt datetime-format: '{dateString}'" 
                    };
                }

                var locationString = fields[1].Trim().ToLower();
                string location = locationString switch
                {
                    "ute" => "Utomhus",
                    "inne" => "Inomhus",
                    _ => locationString
                };

                var tempString = NormalizeNumberString(fields[2].Trim());
                if (!double.TryParse(tempString, NumberStyles.Any, CultureInfo.InvariantCulture, out double temperature))
                {
                    return new WeatherData 
                    { 
                        IsValid = false, 
                        ErrorMessage = $"Ogiltigt temperaturvärde: '{fields[2]}' (normalized: '{tempString}')" 
                    };
                }

                var humidityString = NormalizeNumberString(fields[3].Trim());
                if (!double.TryParse(humidityString, NumberStyles.Any, CultureInfo.InvariantCulture, out double humidity))
                {
                    return new WeatherData 
                    { 
                        IsValid = false, 
                        ErrorMessage = $"Ogiltigt luftfuktighetsvärde: '{fields[3]}' (normalized: '{humidityString}')" 
                    };
                }

                if (temperature < -50 || temperature > 50)
                {
                    return new WeatherData 
                    { 
                        IsValid = false, 
                        ErrorMessage = $"Temperatur utanför rimligt intervall: {temperature}" 
                    };
                }

                if (humidity < 0 || humidity > 100)
                {
                    return new WeatherData 
                    { 
                        IsValid = false, 
                        ErrorMessage = $"Luftfuktighet utanför rimligt intervall: {humidity}" 
                    };
                }

                return new WeatherData
                {
                    DateTime = dateTime,
                    Location = location,
                    Temperature = temperature,
                    Humidity = humidity,
                    IsValid = true
                };
            }
            catch (Exception ex)
            {
                return new WeatherData 
                { 
                    IsValid = false, 
                    ErrorMessage = $"Parse error: {ex.Message}" 
                };
            }
        }

        private string NormalizeNumberString(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var result = new StringBuilder();
            foreach (char c in input)
            {
                switch (c)
                {
                    case '−': // U+2212 MINUS SIGN
                    case '‐': // U+2010 HYPHEN
                    case '‑': // U+2011 NON-BREAKING HYPHEN
                    case '‒': // U+2012 FIGURE DASH
                    case '–': // U+2013 EN DASH
                    case '—': // U+2014 EM DASH
                    case '―': // U+2015 HORIZONTAL BAR
                        result.Append('-');
                        break;
                    case ',':
                        result.Append('.');
                        break;
                    default:
                        result.Append(c);
                        break;
                }
            }
            return result.ToString();
        }

        public async Task<List<WeatherData>> GetRawDataAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            _logger.LogInformation("Getting raw data");
            
            try
            {
                var query = _context.WeatherData.Where(w => w.IsValid);
                
                if (startDate.HasValue)
                    query = query.Where(w => w.DateTime >= startDate.Value);
                    
                if (endDate.HasValue)
                    query = query.Where(w => w.DateTime <= endDate.Value);

                var data = await query.OrderBy(w => w.DateTime).Take(50).ToListAsync();
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting raw data");
                Console.WriteLine($"❌ Fel vid hämtning av data: {ex.Message}");
                return new List<WeatherData>();
            }
        }

        public async Task<List<DailyAverage>> GetDailyAveragesAsync(DateTime date, string location)
        {
            try
            {
                var data = await _context.WeatherData
                    .Where(w => w.DateTime.Date == date.Date && w.Location == location && w.IsValid)
                    .GroupBy(w => w.DateTime.Date)
                    .Select(g => new DailyAverage
                    {
                        Date = g.Key,
                        AvgTemperature = g.Average(w => w.Temperature),
                        AvgHumidity = g.Average(w => w.Humidity)
                    })
                    .ToListAsync();

                foreach (var day in data)
                {
                    if (day.AvgTemperature.HasValue && day.AvgHumidity.HasValue)
                    {
                        day.MoldRisk = MoldRiskCalculator.CalculateMoldRisk(day.AvgTemperature.Value, day.AvgHumidity.Value);
                    }
                }
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily averages");
                return new List<DailyAverage>();
            }
        }

        public async Task<List<DailyAverage>> GetTemperatureSortedAsync(string location)
        {
            try
            {
                var dailyAverages = await _context.WeatherData
                    .Where(w => w.Location == location && w.IsValid)
                    .GroupBy(w => w.DateTime.Date)
                    .Select(g => new DailyAverage
                    {
                        Date = g.Key,
                        AvgTemperature = g.Average(w => w.Temperature),
                        AvgHumidity = g.Average(w => w.Humidity)
                    })
                    .ToListAsync();

                foreach (var day in dailyAverages)
                {
                    if (day.AvgTemperature.HasValue && day.AvgHumidity.HasValue)
                    {
                        day.MoldRisk = MoldRiskCalculator.CalculateMoldRisk(day.AvgTemperature.Value, day.AvgHumidity.Value);
                    }
                }
                return dailyAverages.OrderByDescending(d => d.AvgTemperature).Take(10).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting temperature sorted data");
                return new List<DailyAverage>();
            }
        }

        public async Task<List<DailyAverage>> GetHumiditySortedAsync(string location)
        {
            try
            {
                var dailyAverages = await _context.WeatherData
                    .Where(w => w.Location == location && w.IsValid)
                    .GroupBy(w => w.DateTime.Date)
                    .Select(g => new DailyAverage
                    {
                        Date = g.Key,
                        AvgTemperature = g.Average(w => w.Temperature),
                        AvgHumidity = g.Average(w => w.Humidity)
                    })
                    .ToListAsync();

                foreach (var day in dailyAverages)
                {
                    if (day.AvgTemperature.HasValue && day.AvgHumidity.HasValue)
                    {
                        day.MoldRisk = MoldRiskCalculator.CalculateMoldRisk(day.AvgTemperature.Value, day.AvgHumidity.Value);
                    }
                }
                return dailyAverages.OrderByDescending(d => d.AvgHumidity).Take(10).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting humidity sorted data");
                return new List<DailyAverage>();
            }
        }

        public async Task<List<DailyAverage>> GetMoldRiskSortedAsync(string location)
        {
            try
            {
                var dailyAverages = await _context.WeatherData
                    .Where(w => w.Location == location && w.IsValid && w.Temperature.HasValue && w.Humidity.HasValue)
                    .GroupBy(w => w.DateTime.Date)
                    .Select(g => new DailyAverage
                    {
                        Date = g.Key,
                        AvgTemperature = g.Average(w => w.Temperature),
                        AvgHumidity = g.Average(w => w.Humidity)
                    })
                    .ToListAsync();

                foreach (var day in dailyAverages)
                {
                    if (day.AvgTemperature.HasValue && day.AvgHumidity.HasValue)
                    {
                        day.MoldRisk = MoldRiskCalculator.CalculateMoldRisk(day.AvgTemperature.Value, day.AvgHumidity.Value);
                    }
                }
                return dailyAverages.OrderByDescending(d => d.MoldRisk).Take(10).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting mold risk sorted data");
                return new List<DailyAverage>();
            }
        }

        public async Task<SeasonResult> GetSeasonsAsync(string location)
        {
            try
            {
                var dailyAverages = await _context.WeatherData
                    .Where(w => w.Location == location && w.IsValid && w.Temperature.HasValue)
                    .GroupBy(w => w.DateTime.Date)
                    .Select(g => new DailyAverage
                    {
                        Date = g.Key,
                        AvgTemperature = g.Average(w => w.Temperature)
                    })
                    .OrderBy(d => d.Date)
                    .ToListAsync();

                var result = SeasonCalculator.CalculateSeasonsFromDailyAverages(dailyAverages, location);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating seasons");
                return new SeasonResult { Message = $"Fel vid säsongsberäkning: {ex.Message}" };
            }
        }
    }
}
