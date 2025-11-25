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
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🌤️  Välkommen till VaderData Applikationen!");
            Console.WriteLine("===========================================");

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddDbContext<WeatherContext>();
                    services.AddScoped<IWeatherDataService, WeatherDataService>();
                    services.AddTransient<DisplayDataCommand>();
                })
                .Build();

            var csvPath = GetCsvFilePath();
            Console.WriteLine($"📁 CSV file path: {csvPath}");

            var weatherService = host.Services.GetRequiredService<IWeatherDataService>();
            await weatherService.InitializeDatabaseAsync();

            if (File.Exists(csvPath))
            {
                Console.WriteLine($"✅ CSV file found at: {csvPath}");
                Console.Write("Vill du ladda data från CSV-filen? (j/n): ");
                var response = Console.ReadLine()?.ToLower();
                
                if (response == "j" || response == "ja")
                {
                    Console.WriteLine("📥 Laddar data från CSV...");
                    await weatherService.LoadDataFromCsvAsync(csvPath);
                    Console.WriteLine("✅ Data laddad successfully!");
                }
            }
            else
            {
                Console.WriteLine($"❌ CSV file not found at: {csvPath}");
                Console.WriteLine("Please make sure 'TempFuktData.csv' is located in the VaderData.UI folder.");
            }

            await RunMainMenu(host, csvPath);
        }

        static string GetCsvFilePath()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            if (currentDirectory.EndsWith("bin/Debug/net9.0") || currentDirectory.EndsWith("bin/Release/net9.0"))
            {
                return Path.Combine(currentDirectory, "../../..", "TempFuktData.csv");
            }
            return Path.Combine(currentDirectory, "TempFuktData.csv");
        }

        static async Task RunMainMenu(IHost host, string csvPath)
        {
            var weatherService = host.Services.GetRequiredService<IWeatherDataService>();
            var displayCmd = host.Services.GetRequiredService<DisplayDataCommand>();
            bool running = true;

            while (running)
            {
                Console.WriteLine("\n=== HUVUDMENY ===");
                Console.WriteLine("1. Visa data");
                Console.WriteLine("2. Ladda data från CSV på nytt");
                Console.WriteLine("3. Sortera data efter temperatur");
                Console.WriteLine("4. Sortera data efter luftfuktighet");
                Console.WriteLine("5. Sortera data efter mögelrisk");
                Console.WriteLine("6. Beräkna säsonger");
                Console.WriteLine("7. Visa CSV sökväg");
                Console.WriteLine("0. Avsluta");
                Console.Write("Val: ");

                var input = Console.ReadLine();
                switch (input)
                {
                    case "1":
                        await displayCmd.ExecuteAsync();
                        break;
                    case "2":
                        Console.WriteLine("📥 Laddar data från CSV...");
                        await weatherService.LoadDataFromCsvAsync(csvPath);
                        Console.WriteLine("✅ Data laddad successfully!");
                        break;
                    case "3":
                        await ShowTemperatureSortedData(weatherService);
                        break;
                    case "4":
                        await ShowHumiditySortedData(weatherService);
                        break;
                    case "5":
                        await ShowMoldRiskSortedData(weatherService);
                        break;
                    case "6":
                        await CalculateSeasons(weatherService);
                        break;
                    case "7":
                        Console.WriteLine($"📁 Aktuell CSV sökväg: {csvPath}");
                        Console.WriteLine($"📁 Fil finns: {(File.Exists(csvPath) ? "✅ JA" : "❌ NEJ")}");
                        break;
                    case "0":
                        running = false;
                        break;
                    default:
                        Console.WriteLine("Ogiltigt val. Försök igen.");
                        break;
                }
            }
            Console.WriteLine("Tack för att du använde VaderData!");
        }

        static async Task ShowTemperatureSortedData(IWeatherDataService weatherService)
        {
            Console.WriteLine("\n=== SORTERING EFTER TEMPERATUR ===");
            Console.WriteLine("1. Utomhus");
            Console.WriteLine("2. Inomhus");
            Console.Write("Val: ");
            
            var choice = Console.ReadLine();
            var location = choice == "1" ? "Utomhus" : "Inomhus";
            
            var data = await weatherService.GetTemperatureSortedAsync(location);
            DisplaySortedData(data, "Temperatur", "°C");
        }

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

        static async Task CalculateSeasons(IWeatherDataService weatherService)
        {
            Console.WriteLine("\n=== SÄSONGSBERÄKNING ===");
            Console.WriteLine("1. Utomhus");
            Console.WriteLine("2. Inomhus");
            Console.Write("Val: ");
            
            var choice = Console.ReadLine();
            var location = choice == "1" ? "Utomhus" : "Inomhus";
            
            var result = await weatherService.GetSeasonsAsync(location);
            
            Console.WriteLine($"\n📅 Säsongsberäkning för {location}:");
            Console.WriteLine($"🍂 Höst start: {(result.AutumnStart?.ToString("yyyy-MM-dd") ?? "Ej hittad")}");
            Console.WriteLine($"❄️ Vinter start: {(result.WinterStart?.ToString("yyyy-MM-dd") ?? "Ej hittad")}");
            Console.WriteLine($"💡 {result.Message}");
        }

        static void DisplaySortedData(List<DailyAverage> data, string metric, string unit)
        {
            if (!data.Any())
            {
                Console.WriteLine("Ingen data tillgänglig. Ladda först data från CSV.");
                return;
            }

            Console.WriteLine($"\n📊 Topp 10 dagar sorterade efter {metric}:");
            Console.WriteLine("=========================================");
            
            for (int i = 0; i < Math.Min(10, data.Count); i++)
            {
                var day = data[i];
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
