using VaderData.UI.Commands;
using VaderData.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VaderData.Core.Interfaces;
using VaderData.DataAccess.Services;
using VaderData.DataAccess.Context;
using Microsoft.EntityFrameworkCore;

namespace VaderData.UI
{
    /// <summary>
    /// Huvudprogramklass för VaderData applikationen
    /// 
    /// ARKITEKTURPRINCIPER:
    /// - Dependency Injection för lösa kopplingar
    /// - HostBuilder pattern för konfiguration
    /// - Repository pattern för dataåtkomst
    /// - Separation of Concerns (UI, Business Logic, Data Access)
    /// </summary>
    class Program
    {
        /// <summary>
        /// Applikationens startpunkt - huvudexekveringsflöde
        /// 
        /// PROGRAMFLÖDESSEKVENS:
        /// 1. Konfigurera Dependency Injection container
        /// 2. Initialisera databasen
        /// 3. Ladda väderdata från CSV-fil
        /// 4. Starta huvudmenyn för användarinteraktion
        /// 
        /// DESIGNMÖNSTER: HostBuilder pattern med Service Collection
        /// </summary>
        /// <param name="args">Kommando-radsargument (används ej i denna implementation)</param>
        static async Task Main(string[] args)
        {
            // Applikationens startmeddelande
            Console.WriteLine("🌤️  Välkommen till VaderData Applikationen!");
            Console.WriteLine("===========================================");

            // =============================================================================
            // KONFIGURATION AV DEPENDENCY INJECTION CONTAINER
            // =============================================================================
            
            // Skapa och konfigurera .NET Generic Host med Service Collection
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    // Registrera Entity Framework DbContext med SQLite
                    // LIFECYCLE: Scoped (en instans per request)
                    services.AddDbContext<WeatherContext>();
                    
                    // Registrera väderdataservice för business logic
                    // LIFECYCLE: Scoped - delas inom samma scope
                    services.AddScoped<IWeatherDataService, WeatherDataService>();
                    
                    // Registrera display command för UI-operationer
                    // LIFECYCLE: Transient - ny instans varje gång
                    services.AddTransient<DisplayDataCommand>();
                })
                .Build();

            // =============================================================================
            // CSV-FIL SÖKVÄGSHANTERING
            // =============================================================================
            
            // Dynamiskt bestäm sökväg till CSV-fil baserat på exekveringskontext
            var csvPath = GetCsvFilePath();
            Console.WriteLine($"📁 CSV file path: {csvPath}");

            // =============================================================================
            // DATABASINITIERING OCH DATAIMPORT
            // =============================================================================
            
            // Hämta service instance från DI container
            var weatherService = host.Services.GetRequiredService<IWeatherDataService>();
            
            // Skapa databasen och tabellerna (Code-First approach)
            await weatherService.InitializeDatabaseAsync();

            // Kontrollera om CSV-filen finns och erbjud dataimport
            if (File.Exists(csvPath))
            {
                Console.WriteLine($"✅ CSV file found at: {csvPath}");
                Console.Write("Vill du ladda data från CSV-filen? (j/n): ");
                var response = Console.ReadLine()?.ToLower();
                
                // Användarval för dataimport - batch processing av CSV-data
                if (response == "j" || response == "ja")
                {
                    Console.WriteLine("📥 Laddar data från CSV...");
                    
                    // ALGORITM: Batch processing med felhantering och validering
                    await weatherService.LoadDataFromCsvAsync(csvPath);
                    
                    Console.WriteLine("✅ Data laddad successfully!");
                }
            }
            else
            {
                // Felhantering för saknad CSV-fil
                Console.WriteLine($"❌ CSV file not found at: {csvPath}");
                Console.WriteLine("Please make sure 'TempFuktData.csv' is located in the VaderData.UI folder.");
            }

            // =============================================================================
            // STARTA HUVUDMENY OCH ANVÄNDARINTERAKTION
            // =============================================================================
            
            await RunMainMenu(host, csvPath);
        }

        /// <summary>
        /// Bestämmer sökväg till CSV-fil baserat på exekveringsmiljö
        /// 
        /// ALGORITM: Directory context detection
        /// - Identifierar om applikationen körs från bin/Debug eller bin/Release
        /// - Justerar sökväg relativt till projektrot i development
        /// - Använder current directory i production
        /// 
        /// ANVÄNDNING: Hanterar olika filstrukturer mellan development och deployment
        /// </summary>
        /// <returns>Sökväg till CSV-filen</returns>
        static string GetCsvFilePath()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            
            // Development environment detection
            if (currentDirectory.EndsWith("bin/Debug/net9.0") || currentDirectory.EndsWith("bin/Release/net9.0"))
            {
                // Navigera upp till projektroten från bin-katalogen
                return Path.Combine(currentDirectory, "../../..", "TempFuktData.csv");
            }
            
            // Production environment - fil i samma katalog som exe
            return Path.Combine(currentDirectory, "TempFuktData.csv");
        }

        /// <summary>
        /// Huvudmeny-loop för användarinteraktion och dataanalys
        /// 
        /// DESIGNMÖNSTER: Command Loop med Switch Statement
        /// 
        /// MENYSTRUKTUR:
        /// - Data visualization commands
        /// - Analysalgoritmer (sortering, säsongsberäkning)
        /// - System operations (data reload, path info)
        /// 
        /// ALGORITM: O(1) per menyval med async/await för I/O operationer
        /// </summary>
        /// <param name="host">DI container host</param>
        /// <param name="csvPath">Sökväg till CSV-fil för reload operation</param>
        static async Task RunMainMenu(IHost host, string csvPath)
        {
            // Hämta services från DI container
            var weatherService = host.Services.GetRequiredService<IWeatherDataService>();
            var displayCmd = host.Services.GetRequiredService<DisplayDataCommand>();
            
            bool running = true;  // Kontrollvariabel för huvudloop

            // =============================================================================
            // HUVUDLOOP FÖR MENYHANTERING
            // =============================================================================
            
            while (running)
            {
                // Visa menyalternativ
                Console.WriteLine("\n=== HUVUDMENY ===");
                Console.WriteLine("1. Visa data");                    // Raw data visualization
                Console.WriteLine("2. Ladda data från CSV på nytt");  // Data reimport
                Console.WriteLine("3. Sortera data efter temperatur"); // Algorithm: Temperature sorting
                Console.WriteLine("4. Sortera data efter luftfuktighet"); // Algorithm: Humidity sorting
                Console.WriteLine("5. Sortera data efter mögelrisk"); // Algorithm: Mold risk calculation & sorting
                Console.WriteLine("6. Beräkna säsonger");            // Algorithm: Meteorological season detection
                Console.WriteLine("7. Visa CSV sökväg");             // System information
                Console.WriteLine("0. Avsluta");                     // Exit application
                Console.Write("Val: ");

                // Läsa användarinput
                var input = Console.ReadLine();
                
                // Switch statement för menyval - O(1) lookup
                switch (input)
                {
                    case "1":  // Visa rådata
                        await displayCmd.ExecuteAsync();
                        break;
                        
                    case "2":  // Omladda data från CSV
                        Console.WriteLine("📥 Laddar data från CSV...");
                        await weatherService.LoadDataFromCsvAsync(csvPath);
                        Console.WriteLine("✅ Data laddad successfully!");
                        break;
                        
                    case "3":  // Temperatursortering - varmaste dagar först
                        await ShowTemperatureSortedData(weatherService);
                        break;
                        
                    case "4":  // Luftfuktighetssortering - fuktigaste dagar först
                        await ShowHumiditySortedData(weatherService);
                        break;
                        
                    case "5":  // Mögelrisksortering - högst risk först
                        await ShowMoldRiskSortedData(weatherService);
                        break;
                        
                    case "6":  // Säsongsberäkning - meteorologiska definitioner
                        await CalculateSeasons(weatherService);
                        break;
                        
                    case "7":  // Systeminformation - CSV sökväg
                        Console.WriteLine($"📁 Aktuell CSV sökväg: {csvPath}");
                        Console.WriteLine($"📁 Fil finns: {(File.Exists(csvPath) ? "✅ JA" : "❌ NEJ")}");
                        break;
                        
                    case "0":  // Avsluta applikationen
                        running = false;
                        break;
                        
                    default:   // Ogiltigt input - felhantering
                        Console.WriteLine("Ogiltigt val. Försök igen.");
                        break;
                }
            }
            
            // Avslutsmeddelande
            Console.WriteLine("Tack för att du använde VaderData!");
        }

        /// <summary>
        /// Visar data sorterad efter temperatur (varmast först)
        /// 
        /// ALGORITM: LINQ OrderByDescending med Take(10)
        /// DATABASQUERY: Gruppering till dagliga medelvärden + sortering
        /// 
        /// METEOROLOGISK ANVÄNDNING: Identifiera varma perioder och värmerekord
        /// </summary>
        /// <param name="weatherService">Service för dataåtkomst</param>
        static async Task ShowTemperatureSortedData(IWeatherDataService weatherService)
        {
            Console.WriteLine("\n=== SORTERING EFTER TEMPERATUR ===");
            Console.WriteLine("1. Utomhus");
            Console.WriteLine("2. Inomhus");
            Console.Write("Val: ");
            
            var choice = Console.ReadLine();
            var location = choice == "1" ? "Utomhus" : "Inomhus";
            
            // Hämta data sorterad efter temperatur
            var data = await weatherService.GetTemperatureSortedAsync(location);
            
            // Visualisera resultat
            DisplaySortedData(data, "Temperatur", "°C");
        }

        /// <summary>
        /// Visar data sorterad efter luftfuktighet (fuktigast först)
        /// 
        /// ALGORITM: LINQ OrderByDescending med Take(10)
        /// 
        /// METEOROLOGISK ANVÄNDNING: Identifiera fuktiga perioder för 
        /// mögelprevention och komfortanalys
        /// </summary>
        /// <param name="weatherService">Service för dataåtkomst</param>
        static async Task ShowHumiditySortedData(IWeatherDataService weatherService)
        {
            Console.WriteLine("\n=== SORTERING EFTER LUFTFUKTIGHET ===");
            Console.WriteLine("1. Utomhus");
            Console.WriteLine("2. Inomhus");
            Console.Write("Val: ");
            
            var choice = Console.ReadLine();
            var location = choice == "1" ? "Utomhus" : "Inomhus";
            
            var data = await weatherService.GetHumiditySortedAsync(location);
            DisplaySortedData(data, "Luftfuktighet", "%");
        }

        /// <summary>
        /// Visar data sorterad efter mögelrisk (högst risk först)
        /// 
        /// ALGORITM: MoldRiskCalculator.CalculateMoldRisk() + sortering
        /// BERÄKNING: f(T,H) = (H - 80) * (T / 15.0) där H > 80%
        /// 
        /// BYGGNADSFYSIKALISK ANVÄNDNING: Proaktiv mögelförebyggelse
        /// </summary>
        /// <param name="weatherService">Service för dataåtkomst</param>
        static async Task ShowMoldRiskSortedData(IWeatherDataService weatherService)
        {
            Console.WriteLine("\n=== SORTERING EFTER MÖGELRISK ===");
            Console.WriteLine("1. Utomhus");
            Console.WriteLine("2. Inomhus");
            Console.Write("Val: ");
            
            var choice = Console.ReadLine();
            var location = choice == "1" ? "Utomhus" : "Inomhus";
            
            var data = await weatherService.GetMoldRiskSortedAsync(location);
            DisplaySortedData(data, "Mögelrisk", "index");
        }

        /// <summary>
        /// Beräknar och visar meteorologiska säsonger
        /// 
        /// ALGORITM: SeasonCalculator med sliding window approach
        /// METEOROLOGISK DEFINITION (SMHI):
        /// - Höst: 5 på varandra följande dagar med T < 10°C
        /// - Vinter: 5 på varandra följande dagar med T < 0°C
        /// 
        /// KOMPLEXITET: O(n) för säsongsidentifiering
        /// </summary>
        /// <param name="weatherService">Service för dataåtkomst</param>
        static async Task CalculateSeasons(IWeatherDataService weatherService)
        {
            Console.WriteLine("\n=== SÄSONGSBERÄKNING ===");
            Console.WriteLine("1. Utomhus");
            Console.WriteLine("2. Inomhus");
            Console.Write("Val: ");
            
            var choice = Console.ReadLine();
            var location = choice == "1" ? "Utomhus" : "Inomhus";
            
            // ALGORITM: Sliding window season detection
            var result = await weatherService.GetSeasonsAsync(location);
            
            // Presentera resultat
            Console.WriteLine($"\n📅 Säsongsberäkning för {location}:");
            Console.WriteLine($"🍂 Höst start: {(result.AutumnStart?.ToString("yyyy-MM-dd") ?? "Ej hittad")}");
            Console.WriteLine($"❄️ Vinter start: {(result.WinterStart?.ToString("yyyy-MM-dd") ?? "Ej hittad")}");
            Console.WriteLine($"💡 {result.Message}");
        }

        /// <summary>
        /// Generisk metod för att visa sorterad data i konsolen
        /// 
        /// ALGORITM: Iterativ presentation med formatering
        /// VISUALISERING: Topp 10 poster med rangordning
        /// 
        /// ANVÄNDNING: Återanvändbar komponent för alla sorteringsoperationer
        /// </summary>
        /// <param name="data">Lista med DailyAverage objekt</param>
        /// <param name="metric">Typ av metric (Temperatur/Luftfuktighet/Mögelrisk)</param>
        /// <param name="unit">Enhet för metric (°C/%/index)</param>
        static void DisplaySortedData(List<DailyAverage> data, string metric, string unit)
        {
            // Validering - kontrollera att data finns
            if (!data.Any())
            {
                Console.WriteLine("Ingen data tillgänglig. Ladda först data från CSV.");
                return;
            }

            // Visa rubrik för datatyp
            Console.WriteLine($"\n📊 Topp 10 dagar sorterade efter {metric}:");
            Console.WriteLine("=========================================");
            
            // Iterera genom topp 10 poster
            for (int i = 0; i < Math.Min(10, data.Count); i++)
            {
                var day = data[i];
                
                // Dynamisk formatering baserat på metric typ
                if (metric == "Temperatur")
                    Console.WriteLine($"{i + 1}. {day.Date:yyyy-MM-dd}: {day.AvgTemperature?.ToString("F1") ?? "N/A"}{unit}");
                else if (metric == "Luftfuktighet")
                    Console.WriteLine($"{i + 1}. {day.Date:yyyy-MM-dd}: {day.AvgHumidity?.ToString("F1") ?? "N/A"}{unit}");
                else if (metric == "Mögelrisk")
                    Console.WriteLine($"{i + 1}. {day.Date:yyyy-MM-dd}: {day.MoldRisk?.ToString("F1") ?? "N/A"}{unit}");
            }
        }
    }
}