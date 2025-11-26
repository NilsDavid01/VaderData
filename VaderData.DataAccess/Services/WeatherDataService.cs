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
    /// <summary>
    /// Implementering av IWeatherDataService för hantering av väderdata
    /// 
    /// DESIGNMÖNSTER: Repository Pattern med Entity Framework
    /// ANSVAR: Dataåtkomst, CSV-processing, och affärslogik
    /// 
    /// ALGORITMISKA KOMPONENTER:
    /// - CSV parsing med felhantering
    /// - Batch processing för prestanda
    /// - Databasaggregation för analyser
    /// - Meteorologiska beräkningar
    /// </summary>
    public class WeatherDataService : IWeatherDataService
    {
        private readonly ILogger<WeatherDataService> _logger;
        private readonly WeatherContext _context;

        /// <summary>
        /// Constructor med Dependency Injection för logger och database context
        /// 
        /// DI-PRINCIP: Constructor injection för lösa kopplingar
        /// </summary>
        /// <param name="logger">Logger för felspårning och monitoring</param>
        /// <param name="context">Entity Framework database context</param>
        public WeatherDataService(ILogger<WeatherDataService> logger, WeatherContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Initialiserar databasen med Entity Framework Code-First approach
        /// 
        /// ALGORITM: EnsureCreatedAsync skapar databas och tabeller automatiskt
        /// DATABASSTRATEGI: SQLite med automatiska migrationer
        /// 
        /// FELHANTERING: Try-catch med logging och användarvänliga meddelanden
        /// </summary>
        public async Task InitializeDatabaseAsync()
        {
            try
            {
                // Skapar databasen och schemat baserat på DbContext
                await _context.Database.EnsureCreatedAsync();
                _logger.LogInformation("Database initialized successfully");
                Console.WriteLine("✅ Databas initialiserad!");
            }
            catch (Exception ex)
            {
                // Felhantering med både logging och användarfeedback
                _logger.LogError(ex, "Error initializing database");
                Console.WriteLine($"❌ Fel vid databasinitialisering: {ex.Message}");
            }
        }

        /// <summary>
        /// Laddar och processar väderdata från CSV-fil till databasen
        /// 
        /// ALGORITMISK PROCESS:
        /// 1. Filvalidering och läsning
        /// 2. Linje-för-linje parsing med felhantering
        /// 3. Datavalidering och normalisering
        /// 4. Batch insertion för prestanda
        /// 
        /// KOMPLEXITET: O(n) där n = antal rader i CSV
        /// MINNESANVÄNDNING: Batch processing för att undvika minnesläckor
        /// </summary>
        /// <param name="filePath">Sökväg till CSV-filen</param>
        public async Task LoadDataFromCsvAsync(string filePath)
        {
            try
            {
                _logger.LogInformation($"Loading data from CSV: {filePath}");
                
                // Validera att filen finns
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"❌ Filen hittades inte: {filePath}");
                    return;
                }

                // Läs alla rader från CSV-filen asynkront
                var lines = await File.ReadAllLinesAsync(filePath);
                Console.WriteLine($"📖 Läser {lines.Length} rader från CSV...");

                // Data structures för processing
                var weatherData = new List<WeatherData>();
                int validRows = 0;
                int invalidRows = 0;

                // =============================================================================
                // CSV PROCESSING ALGORITM - Linje-för-linje parsing
                // =============================================================================
                
                // Start från rad 1 (hoppa över header-raden)
                for (int i = 1; i < lines.Length; i++)
                {
                    try
                    {
                        var line = lines[i];
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        // Parse CSV-rad till WeatherData objekt
                        var data = ParseCsvLine(line, i);
                        if (data != null && data.IsValid)
                        {
                            weatherData.Add(data);
                            validRows++;
                        }
                        else
                        {
                            invalidRows++;
                            // Visa första 5 felen för debugging
                            if (invalidRows <= 5)
                            {
                                Console.WriteLine($"❌ Rad {i}: {data?.ErrorMessage}");
                            }
                        }

                        // Progress indicator för stora filer
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

                // =============================================================================
                // BATCH INSERTION ALGORITM - Optimal databasprestanda
                // =============================================================================
                
                if (weatherData.Any())
                {
                    Console.WriteLine($"💾 Sparar {weatherData.Count} rader till databasen...");
                    
                    // Rensa befintlig data för fresh import
                    _context.WeatherData.RemoveRange(_context.WeatherData);
                    await _context.SaveChangesAsync();
                    
                    // Batch processing med 1000 rader per batch
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

        /// <summary>
        /// Parser för individuella CSV-rader till WeatherData objekt
        /// 
        /// ALGORITM: Field splitting + typkonvertering + validering
        /// 
        /// DATAFLÖDE:
        /// 1. Split på kommatecken → 4 fält
        /// 2. Datum parsing med svensk kultur
        /// 3. Plats-normalisering ("ute" → "Utomhus", "inne" → "Inomhus")
        /// 4. Temperatur/luftfuktighet parsing med normalisering
        /// 5. Fysisk validering av värden
        /// 
        /// FELHANTERING: Returnerar ogiltiga WeatherData objekt med felmeddelanden
        /// </summary>
        /// <param name="line">CSV-rad att pars</param>
        /// <param name="lineNumber">Radnummer för felrapportering</param>
        /// <returns>WeatherData objekt eller null vid fel</returns>
        private WeatherData ParseCsvLine(string line, int lineNumber)
        {
            try
            {
                var fields = line.Split(',');
                
                // Validera antal fält
                if (fields.Length < 4)
                {
                    return new WeatherData 
                    { 
                        IsValid = false, 
                        ErrorMessage = $"Otillräckligt med fält: {fields.Length}. Förväntade 4 fält." 
                    };
                }

                // =============================================================================
                // DATUM PARSING - Svensk kultur för datumformat
                // =============================================================================
                
                var dateString = fields[0].Trim();
                if (!DateTime.TryParse(dateString, new CultureInfo("sv-SE"), DateTimeStyles.None, out DateTime dateTime))
                {
                    return new WeatherData 
                    { 
                        IsValid = false, 
                        ErrorMessage = $"Ogiltigt datetime-format: '{dateString}'" 
                    };
                }

                // =============================================================================
                // PLATS-NORMALISERING - Konvertera till konsekventa värden
                // =============================================================================
                
                var locationString = fields[1].Trim().ToLower();
                string location = locationString switch
                {
                    "ute" => "Utomhus",
                    "inne" => "Inomhus",
                    _ => locationString
                };

                // =============================================================================
                // TEMPERATUR PARSING - Med Unicode-normalisering
                // =============================================================================
                
                var tempString = NormalizeNumberString(fields[2].Trim());
                if (!double.TryParse(tempString, NumberStyles.Any, CultureInfo.InvariantCulture, out double temperature))
                {
                    return new WeatherData 
                    { 
                        IsValid = false, 
                        ErrorMessage = $"Ogiltigt temperaturvärde: '{fields[2]}' (normalized: '{tempString}')" 
                    };
                }

                // =============================================================================
                // LUFTFUKTIGHET PARSING - Med Unicode-normalisering
                // =============================================================================
                
                var humidityString = NormalizeNumberString(fields[3].Trim());
                if (!double.TryParse(humidityString, NumberStyles.Any, CultureInfo.InvariantCulture, out double humidity))
                {
                    return new WeatherData 
                    { 
                        IsValid = false, 
                        ErrorMessage = $"Ogiltigt luftfuktighetsvärde: '{fields[3]}' (normalized: '{humidityString}')" 
                    };
                }

                // =============================================================================
                // FYSISK VALIDERING - Kontrollera rimliga värden
                // =============================================================================
                
                // Temperaturvalidering: -50°C till +50°C (jordens extrema temperaturer)
                if (temperature < -50 || temperature > 50)
                {
                    return new WeatherData 
                    { 
                        IsValid = false, 
                        ErrorMessage = $"Temperatur utanför rimligt intervall: {temperature}" 
                    };
                }

                // Luftfuktighetsvalidering: 0% till 100% (fysiskt möjligt)
                if (humidity < 0 || humidity > 100)
                {
                    return new WeatherData 
                    { 
                        IsValid = false, 
                        ErrorMessage = $"Luftfuktighet utanför rimligt intervall: {humidity}" 
                    };
                }

                // Returnera giltigt WeatherData objekt
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
                // Allmän felhantering för oväntade exceptions
                return new WeatherData 
                { 
                    IsValid = false, 
                    ErrorMessage = $"Parse error: {ex.Message}" 
                };
            }
        }

        /// <summary>
        /// Normaliserar nummersträngar för att hantera olika teckenkodningar
        /// 
        /// ALGORITM: Character replacement för Unicode-normalisering
        /// PROBLEMLÖSNING: Hanterar olika minus-tecken och decimalseparatorer
        /// 
        /// UNICODE-HANTERING:
        /// - 7 olika minus-tecken konverteras till standard '-'
        /// - Komma ',' konverteras till punkt '.' för decimaltal
        /// - Övriga tecken behålls oförändrade
        /// </summary>
        /// <param name="input">Original nummersträng</param>
        /// <returns>Normaliserad nummersträng</returns>
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

        /// <summary>
        /// Hämtar rådata från databasen med valfritt datumfilter
        /// 
        /// DATABASQUERY: LINQ med conditional filtering
        /// PRESTANDA: Take(50) för att begränsa resultatstorlek
        /// 
        /// ANVÄNDNING: Debugging, dataverifiering, och grundläggande visning
        /// </summary>
        /// <param name="startDate">Startdatum för filter (valfritt)</param>
        /// <param name="endDate">Slutdatum för filter (valfritt)</param>
        /// <returns>Lista av WeatherData objekt</returns>
        public async Task<List<WeatherData>> GetRawDataAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            _logger.LogInformation("Getting raw data");
            
            try
            {
                // Basquery med endast giltig data
                var query = _context.WeatherData.Where(w => w.IsValid);
                
                // Lägg till datumfilter om angivna
                if (startDate.HasValue)
                    query = query.Where(w => w.DateTime >= startDate.Value);
                    
                if (endDate.HasValue)
                    query = query.Where(w => w.DateTime <= endDate.Value);

                // Exekvera query med sortering och begränsning
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

        /// <summary>
        /// Beräknar dagliga medelvärden för specifikt datum och plats
        /// 
        /// ALGORITM: Entity Framework GROUP BY med AVG aggregation
        /// DATABASOPERATION: Gruppering per dag + genomsnittsberäkning
        /// 
        /// MÖGELRISK: Beräknar även mögelrisk för varje dag
        /// </summary>
        /// <param name="date">Datum för analys</param>
        /// <param name="location">Plats för analys</param>
        /// <returns>Lista med dagliga medelvärden</returns>
        public async Task<List<DailyAverage>> GetDailyAveragesAsync(DateTime date, string location)
        {
            try
            {
                // DATABASE QUERY: Gruppering och genomsnittsberäkning
                var data = await _context.WeatherData
                    .Where(w => w.DateTime.Date == date.Date && w.Location == location && w.IsValid)
                    .GroupBy(w => w.DateTime.Date)  // Gruppera per dag
                    .Select(g => new DailyAverage
                    {
                        Date = g.Key,
                        AvgTemperature = g.Average(w => w.Temperature),
                        AvgHumidity = g.Average(w => w.Humidity)
                    })
                    .ToListAsync();

                // BERÄKNA MÖGELRISK för varje dag
                foreach (var day in data)
                {
                    if (day.AvgTemperature.HasValue && day.AvgHumidity.HasValue)
                    {
                        // Använd MoldRiskCalculator algoritm
                        day.MoldRisk = MoldRiskCalculator.CalculateMoldRisk(
                            day.AvgTemperature.Value, 
                            day.AvgHumidity.Value);
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

        /// <summary>
        /// Hämtar dagar sorterade efter temperatur (varmast först)
        /// 
        /// ALGORITM: Gruppering → Genomsnitt → Sortering → Topp 10
        /// 
        /// METEOROLOGISK ANVÄNDNING: Identifiera värmeböljor och rekordvarma dagar
        /// </summary>
        /// <param name="location">Plats för analys</param>
        /// <returns>Topp 10 varmaste dagar</returns>
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

                // Beräkna mögelrisk och sortera efter temperatur
                foreach (var day in dailyAverages)
                {
                    if (day.AvgTemperature.HasValue && day.AvgHumidity.HasValue)
                    {
                        day.MoldRisk = MoldRiskCalculator.CalculateMoldRisk(
                            day.AvgTemperature.Value, 
                            day.AvgHumidity.Value);
                    }
                }
                
                // Sortera fallande efter temperatur och ta topp 10
                return dailyAverages.OrderByDescending(d => d.AvgTemperature).Take(10).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting temperature sorted data");
                return new List<DailyAverage>();
            }
        }

        /// <summary>
        /// Hämtar dagar sorterade efter luftfuktighet (fuktigast först)
        /// 
        /// ALGORITM: Samma som temperatur men sorterar på luftfuktighet
        /// 
        /// BYGGNADSFYSIKALISK ANVÄNDNING: Identifiera fuktproblem och möglerisker
        /// </summary>
        /// <param name="location">Plats för analys</param>
        /// <returns>Topp 10 fuktigaste dagar</returns>
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
                        day.MoldRisk = MoldRiskCalculator.CalculateMoldRisk(
                            day.AvgTemperature.Value, 
                            day.AvgHumidity.Value);
                    }
                }
                
                // Sortera fallande efter luftfuktighet
                return dailyAverages.OrderByDescending(d => d.AvgHumidity).Take(10).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting humidity sorted data");
                return new List<DailyAverage>();
            }
        }

        /// <summary>
        /// Hämtar dagar sorterade efter mögelrisk (högst risk först)
        /// 
        /// ALGORITM: MoldRiskCalculator + sortering på beräknat riskindex
        /// 
        /// PREVENTIV ANVÄNDNING: Proaktiv mögelförebyggelse och byggnadsskydd
        /// </summary>
        /// <param name="location">Plats för analys</param>
        /// <returns>Topp 10 dagar med högst mögelrisk</returns>
        public async Task<List<DailyAverage>> GetMoldRiskSortedAsync(string location)
        {
            try
            {
                // Extra filtrering - kräver både temperatur och luftfuktighet
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

                // Beräkna mögelrisk för varje dag
                foreach (var day in dailyAverages)
                {
                    if (day.AvgTemperature.HasValue && day.AvgHumidity.HasValue)
                    {
                        day.MoldRisk = MoldRiskCalculator.CalculateMoldRisk(
                            day.AvgTemperature.Value, 
                            day.AvgHumidity.Value);
                    }
                }
                
                // Sortera fallande efter mögelrisk
                return dailyAverages.OrderByDescending(d => d.MoldRisk).Take(10).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting mold risk sorted data");
                return new List<DailyAverage>();
            }
        }

        /// <summary>
        /// Beräknar meteorologiska säsonger baserat på temperaturdata
        /// 
        /// ALGORITM: SeasonCalculator med sliding window approach
        /// METEOROLOGISK DEFINITION (SMHI):
        /// - Höst: 5 på varandra följande dagar med T < 10°C
        /// - Vinter: 5 på varandra följande dagar med T < 0°C
        /// 
        /// DATABASQUERY: Dagliga medeltemperaturer sorterade kronologiskt
        /// </summary>
        /// <param name="location">Plats för säsongsberäkning</param>
        /// <returns>SeasonResult med säsongsstartdatum</returns>
        public async Task<SeasonResult> GetSeasonsAsync(string location)
        {
            try
            {
                // Hämta dagliga medeltemperaturer kronologiskt sorterade
                var dailyAverages = await _context.WeatherData
                    .Where(w => w.Location == location && w.IsValid && w.Temperature.HasValue)
                    .GroupBy(w => w.DateTime.Date)
                    .Select(g => new DailyAverage
                    {
                        Date = g.Key,
                        AvgTemperature = g.Average(w => w.Temperature)
                    })
                    .OrderBy(d => d.Date)  // Viktigt för kronologisk analys
                    .ToListAsync();

                // Använd SeasonCalculator för säsongsidentifiering
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