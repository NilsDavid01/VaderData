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
    /// Huvudprogramklass för VaderData konsolapplikation
    /// 
    /// ANSVAR: 
    /// - Starta och konfigurera applikationen
    /// - Hantera Dependency Injection
    /// - Köra huvudmenyn och användarinteraktion
    /// - Samordna alla UI-komponenter
    /// 
    /// DESIGNMÖNSTER:
    /// - HostBuilder Pattern för applikationskonfiguration
    /// - Dependency Injection för lösa kopplingar
    /// - Menu-driven Command Pattern för användarinteraktion
    /// - Pagination Pattern för hantering av stora dataset
    /// </summary>
    class Program
    {
        /// <summary>
        /// Applikationens startpunkt - huvudexekveringsflöde
        /// 
        /// PROGRAMFLÖDESSEKVENS:
        /// 1. Konfigurera Dependency Injection container
        /// 2. Initialisera databasen
        /// 3. Ladda väderdata från CSV-fil (valfritt)
        /// 4. Starta huvudmenyn för användarinteraktion
        /// 
        /// FELHANTERING: Global exception handling via HostBuilder
        /// </summary>
        static async Task Main(string[] args)
        {
            // Applikationsstartmeddelande
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
                })
                .Build();

            // =============================================================================
            // CSV-FIL SÖKVÄGSHANTERING OCH DATAINITIERING
            // =============================================================================
            
            // Dynamiskt bestäm sökväg till CSV-fil baserat på exekveringskontext
            var csvPath = GetCsvFilePath();
            Console.WriteLine($"📁 CSV file path: {csvPath}");

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
        /// - Data visualization commands med paginering
        /// - Analysalgoritmer (sortering, säsongsberäkning)
        /// - System operations (data reload, path info)
        /// 
        /// ALGORITM: O(1) per menyval med async/await för I/O operationer
        /// </summary>
        static async Task RunMainMenu(IHost host, string csvPath)
        {
            // Hämta services från DI container
            var weatherService = host.Services.GetRequiredService<IWeatherDataService>();
            
            bool running = true;  // Kontrollvariabel för huvudloop

            // =============================================================================
            // HUVUDLOOP FÖR MENYHANTERING
            // =============================================================================
            
            while (running)
            {
                // Visa menyalternativ
                Console.WriteLine("\n=== HUVUDMENY ===");
                Console.WriteLine("1. Visa data");                    // Raw data visualization med paginering
                Console.WriteLine("2. Ladda data från CSV på nytt");  // Data reimport
                Console.WriteLine("3. Sortera data efter temperatur"); // Algorithm: Temperature sorting med paginering
                Console.WriteLine("4. Sortera data efter luftfuktighet"); // Algorithm: Humidity sorting med paginering
                Console.WriteLine("5. Sortera data efter mögelrisk"); // Algorithm: Mold risk calculation & sorting med paginering
                Console.WriteLine("6. Beräkna säsonger");            // Algorithm: Meteorological season detection
                Console.WriteLine("7. Visa CSV sökväg");             // System information
                Console.WriteLine("0. Avsluta");                     // Exit application
                Console.Write("Val: ");

                // Läsa användarinput
                var input = Console.ReadLine();
                
                // Switch statement för menyval - O(1) lookup
                switch (input)
                {
                    case "1":  // Visa rådata med paginering
                        await ShowAllDataWithPagination(weatherService);
                        break;
                        
                    case "2":  // Omladda data från CSV
                        Console.WriteLine("📥 Laddar data från CSV...");
                        await weatherService.LoadDataFromCsvAsync(csvPath);
                        Console.WriteLine("✅ Data laddad successfully!");
                        break;
                        
                    case "3":  // Temperatursortering - varmaste dagar först med paginering
                        await ShowTemperatureSortedDataWithPagination(weatherService);
                        break;
                        
                    case "4":  // Luftfuktighetssortering - fuktigaste dagar först med paginering
                        await ShowHumiditySortedDataWithPagination(weatherService);
                        break;
                        
                    case "5":  // Mögelrisksortering - högst risk först med paginering
                        await ShowMoldRiskSortedDataWithPagination(weatherService);
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
        /// Visar all rådata från databasen med paginerad navigation
        /// 
        /// DATAFLÖDE:
        /// UI → Service Layer → Database → Paginerad visning
        /// 
        /// PAGINERING: 
        /// - 20 rader per sida för optimal läsbarhet
        /// - Global indexering över alla sidor
        /// - Navigering mellan sidor med tangentbords kommandon
        /// 
        /// ANVÄNDNING: Debugging, dataverifiering, och detaljerad analys
        /// </summary>
        static async Task ShowAllDataWithPagination(IWeatherDataService weatherService)
        {
            Console.WriteLine("=== Visa Väderdata ===");
            var data = await weatherService.GetRawDataAsync();
            
            if (!data.Any())
            {
                Console.WriteLine("Ingen data hittades.");
                Console.WriteLine("Tryck på valfri tangent för att fortsätta...");
                Console.ReadKey();
                return;
            }

            // Använd generisk pagineringsmetod för rådata
            await DisplayPagination(data, "All Väderdata", item => 
                $"{item.DateTime:yyyy-MM-dd HH:mm}: {item.Temperature}°C, {item.Humidity}% ({item.Location})");
        }

        /// <summary>
        /// Visar data sorterad efter temperatur (varmast först) med paginering
        /// 
        /// ALGORITM: LINQ OrderByDescending på dagliga medeltemperaturer
        /// DATABASQUERY: Gruppering till dagliga medelvärden + sortering
        /// 
        /// METEOROLOGISK ANVÄNDNING: 
        /// - Identifiera varma perioder och värmerekord
        /// - Analysera temperaturtrender över tid
        /// - Jämförelse mellan olika perioder
        /// </summary>
        static async Task ShowTemperatureSortedDataWithPagination(IWeatherDataService weatherService)
        {
            Console.WriteLine("\n=== SORTERING EFTER TEMPERATUR ===");
            Console.WriteLine("1. Utomhus");
            Console.WriteLine("2. Inomhus");
            Console.Write("Val: ");
            
            var choice = Console.ReadLine();
            var location = choice == "1" ? "Utomhus" : "Inomhus";
            
            var data = await weatherService.GetTemperatureSortedAsync(location);
            
            // Använd generisk pagineringsmetod för temperatursorterad data
            await DisplayPagination(data, $"Temperatur Sortering - {location}", item => 
                $"{item.Date:yyyy-MM-dd}: {item.AvgTemperature?.ToString("F1") ?? "N/A"}°C");
        }

        /// <summary>
        /// Visar data sorterad efter luftfuktighet (fuktigast först) med paginering
        /// 
        /// ALGORITM: LINQ OrderByDescending på dagliga medelluftfuktighet
        /// 
        /// BYGGNADSFYSIKALISK ANVÄNDNING:
        /// - Identifiera fuktperioder för mögelförebyggelse
        /// - Analysera luftfuktighetstrender för komfort
        /// - Planera ventilations- och avfuktningsbehov
        /// </summary>
        static async Task ShowHumiditySortedDataWithPagination(IWeatherDataService weatherService)
        {
            Console.WriteLine("\n=== SORTERING EFTER LUFTFUKTIGHET ===");
            Console.WriteLine("1. Utomhus");
            Console.WriteLine("2. Inomhus");
            Console.Write("Val: ");
            
            var choice = Console.ReadLine();
            var location = choice == "1" ? "Utomhus" : "Inomhus";
            
            var data = await weatherService.GetHumiditySortedAsync(location);
            
            // Använd generisk pagineringsmetod för luftfuktighetssorterad data
            await DisplayPagination(data, $"Luftfuktighet Sortering - {location}", item => 
                $"{item.Date:yyyy-MM-dd}: {item.AvgHumidity?.ToString("F1") ?? "N/A"}%");
        }

        /// <summary>
        /// Visar data sorterad efter mögelrisk (högst risk först) med paginering
        /// 
        /// ALGORITM: MoldRiskCalculator + sortering på beräknat riskindex
        /// BERÄKNING: f(T,H) = (H - 80) * (T / 15.0) där H > 80%
        /// 
        /// PREVENTIV ANVÄNDNING:
        /// - Proaktiv mögelförebyggelse och byggnadsskydd
        /// - Identifiera riskperioder för extra åtgärder
        /// - Underhållsplanering baserat på risknivå
        /// </summary>
        static async Task ShowMoldRiskSortedDataWithPagination(IWeatherDataService weatherService)
        {
            Console.WriteLine("\n=== SORTERING EFTER MÖGELRISK ===");
            Console.WriteLine("1. Utomhus");
            Console.WriteLine("2. Inomhus");
            Console.Write("Val: ");
            
            var choice = Console.ReadLine();
            var location = choice == "1" ? "Utomhus" : "Inomhus";
            
            var data = await weatherService.GetMoldRiskSortedAsync(location);
            
            // Använd generisk pagineringsmetod för mögelrisksorterad data
            await DisplayPagination(data, $"Mögelrisk Sortering - {location}", item => 
            {
                var riskLevel = item.MoldRisk.HasValue ? 
                    VaderData.Core.Algorithms.MoldRiskCalculator.GetMoldRiskLevel(item.MoldRisk.Value) : "N/A";
                return $"{item.Date:yyyy-MM-dd}: {item.MoldRisk?.ToString("F1") ?? "N/A"} index ({riskLevel})";
            });
        }

        /// <summary>
        /// Beräknar och visar meteorologiska säsonger baserat på temperaturdata
        /// 
        /// ALGORITM: SeasonCalculator med sliding window approach
        /// METEOROLOGISK DEFINITION (SMHI):
        /// - Höst: 5 på varandra följande dagar med T < 10°C
        /// - Vinter: 5 på varandra följande dagar med T < 0°C
        /// 
        /// KLIMAATOLOGISK ANALYS:
        /// - Identifierar säsongsovergångar
        /// - Analyserar klimattrender
        /// - Jämför med historiska normer
        /// </summary>
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
            Console.WriteLine($"🍂 Höst start: {result.AutumnStart?.ToString("yyyy-MM-dd") ?? "Kunde inte beräknas"}");
            Console.WriteLine($"❄️ Vinter start: {(result.WinterStart?.ToString("yyyy-MM-dd") ?? "För tidigt för vinter")}");
            Console.WriteLine($"💡 {result.Message}");
            Console.WriteLine("\nTryck på valfri tangent för att fortsätta...");
            Console.ReadKey();
        }

        /// <summary>
        /// Generisk pagineringsmetod för att visa stora dataset i hanterbara sidor
        /// 
        /// DESIGNMÖNSTER: Generic Programming med Func delegate
        /// 
        /// PAGINERING ALGORITM:
        /// - Beräkna totalt antal sidor: ceil(totalItems / pageSize)
        /// - Hämta aktuell sida: data.Skip(currentPage * pageSize).Take(pageSize)
        /// - Global indexering: currentPage * pageSize + localIndex
        /// 
        /// NAVIGERINGSKOMMANDON:
        /// N - Nästa sida
        /// P - Föregående sida
        /// F - Första sidan  
        /// S - Sista sidan
        /// G [sida] - Gå till specifik sida
        /// A - Avsluta visning
        /// 
        /// ANVÄNDNING: Återanvändbar komponent för alla datatyper och visningar
        /// </summary>
        /// <typeparam name="T">Typ av data att paginera (WeatherData, DailyAverage, etc.)</typeparam>
        /// <param name="data">Lista med data att visa</param>
        /// <param name="title">Titel för paginerad visning</param>
        /// <param name="formatter">Funktion för att formatera varje dataobjekt till sträng</param>
        static async Task DisplayPagination<T>(List<T> data, string title, Func<T, string> formatter)
        {
            // Validering - kontrollera att data finns
            if (!data.Any())
            {
                Console.WriteLine("Ingen data tillgänglig. Ladda först data från CSV.");
                Console.WriteLine("Tryck på valfri tangent för att fortsätta...");
                Console.ReadKey();
                return;
            }

            // =============================================================================
            // PAGINERING KONFIGURATION
            // =============================================================================
            
            int pageSize = 20;        // Antal rader per sida (optimal för konsolvisning)
            int currentPage = 0;      // Aktuell sida (0-indexed)
            int totalPages = (int)Math.Ceiling(data.Count / (double)pageSize);  // Totala antal sidor
            bool viewing = true;      // Kontrollvariabel för pagineringsloop

            // =============================================================================
            // PAGINERINGSLOOP
            // =============================================================================
            
            while (viewing)
            {
                // Rensa skärmen för ren visning
                Console.Clear();
                
                // Visa rubrik och sidinformation
                Console.WriteLine($"=== {title} ===");
                Console.WriteLine($"📊 Visar {data.Count} poster - Sida {currentPage + 1} av {totalPages}");
                Console.WriteLine("".PadRight(60, '='));
                
                // Hämta data för aktuell sida
                var pageData = data.Skip(currentPage * pageSize).Take(pageSize);
                int globalIndex = currentPage * pageSize;  // Globalt index för hela dataset
                
                // Visa alla poster på aktuell sida
                foreach (var item in pageData)
                {
                    globalIndex++;
                    // Använd användardefinierad formatteringsfunktion
                    Console.WriteLine($"{globalIndex}. {formatter(item)}");
                }

                // Visa sidfot med sammanfattning
                Console.WriteLine("".PadRight(60, '='));
                Console.WriteLine($"Visar {pageData.Count()} av {data.Count} totala poster");
                
                // Visa navigeringsalternativ endast om det finns flera sidor
                if (totalPages > 1)
                {
                    Console.WriteLine("\n📋 Navigering:");
                    Console.WriteLine("   N - Nästa sida");
                    Console.WriteLine("   P - Föregående sida");
                    Console.WriteLine("   F - Första sidan");
                    Console.WriteLine("   S - Sista sidan");
                    Console.WriteLine("   G [sida] - Gå till specifik sida (t.ex. 'G 5')");
                }
                Console.WriteLine("   A - Avsluta visning");
                Console.Write("Val: ");

                // Läs användarinput och trimma bort whitespace
                var input = Console.ReadLine()?.ToLower().Trim();
                
                // Hantera navigeringskommandon
                switch (input)
                {
                    case "n":  // Nästa sida
                        if (currentPage < totalPages - 1)
                            currentPage++;
                        else
                        {
                            Console.WriteLine("⚠️  Du är på sista sidan! Tryck på valfri tangent...");
                            Console.ReadKey();
                        }
                        break;
                    case "p":  // Föregående sida
                        if (currentPage > 0)
                            currentPage--;
                        else
                        {
                            Console.WriteLine("⚠️  Du är på första sidan! Tryck på valfri tangent...");
                            Console.ReadKey();
                        }
                        break;
                    case "f":  // Första sidan
                        currentPage = 0;
                        break;
                    case "s":  // Sista sidan
                        currentPage = totalPages - 1;
                        break;
                    case "a":  // Avsluta visning
                        viewing = false;
                        break;
                    case string s when s.StartsWith("g ") && totalPages > 1:  // Gå till specifik sida
                        if (int.TryParse(s.Substring(2), out int page) && page >= 1 && page <= totalPages)
                        {
                            currentPage = page - 1;  // Konvertera till 0-indexed
                        }
                        else
                        {
                            Console.WriteLine($"❌ Ogiltigt sidnummer. Använd 1-{totalPages}. Tryck på valfri tangent...");
                            Console.ReadKey();
                        }
                        break;
                    case string s when s.StartsWith("g ") && totalPages <= 1:  // G-kommando när endast en sida finns
                        Console.WriteLine("ℹ️  Endast en sida tillgänglig. Tryck på valfri tangent...");
                        Console.ReadKey();
                        break;
                    default:  // Ogiltigt kommando
                        Console.WriteLine("❌ Ogiltigt val. Tryck på valfri tangent...");
                        Console.ReadKey();
                        break;
                }
            }
        }
    }
}