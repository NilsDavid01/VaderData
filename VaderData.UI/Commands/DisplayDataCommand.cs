using VaderData.Core.Interfaces;
using VaderData.Core.Models;

namespace VaderData.UI.Commands
{
    public class DisplayDataCommand
    {
        private readonly IWeatherDataService _weatherService;

        public DisplayDataCommand(IWeatherDataService weatherService)
        {
            _weatherService = weatherService;
        }

        public async Task ExecuteAsync()
        {
            Console.WriteLine("=== Visa Väderdata ===");
            var data = await _weatherService.GetRawDataAsync();
            
            if (!data.Any())
            {
                Console.WriteLine("Ingen data hittades.");
                return;
            }

            int pageSize = 20;
            int currentPage = 0;
            int totalPages = (int)Math.Ceiling(data.Count / (double)pageSize);
            bool viewing = true;

            while (viewing)
            {
                Console.Clear();
                Console.WriteLine($"=== Väderdata - Sida {currentPage + 1} av {totalPages} ===");
                Console.WriteLine($"Visar rad {currentPage * pageSize + 1}-{Math.Min((currentPage + 1) * pageSize, data.Count)} av {data.Count} totalt");
                Console.WriteLine("=" .PadRight(50, '='));
                
                var pageData = data.Skip(currentPage * pageSize).Take(pageSize);
                
                foreach (var item in pageData)
                {
                    Console.WriteLine($"{item.DateTime:yyyy-MM-dd HH:mm}: {item.Temperature}°C, {item.Humidity}% ({item.Location})");
                }

                Console.WriteLine("\n" + "=".PadRight(50, '='));
                Console.WriteLine("Navigering:");
                Console.WriteLine("N - Nästa sida");
                Console.WriteLine("P - Föregående sida");
                Console.WriteLine("F - Första sidan");
                Console.WriteLine("S - Sista sidan");
                Console.WriteLine("G [sida] - Gå till specifik sida (t.ex. 'G 5')");
                Console.WriteLine("A - Avsluta visning");
                Console.Write("Val: ");

                var input = Console.ReadLine()?.ToLower().Trim();
                
                switch (input)
                {
                    case "n":
                        if (currentPage < totalPages - 1)
                            currentPage++;
                        else
                            Console.WriteLine("⚠️  Du är på sista sidan!");
                        break;
                    case "p":
                        if (currentPage > 0)
                            currentPage--;
                        else
                            Console.WriteLine("⚠️  Du är på första sidan!");
                        break;
                    case "f":
                        currentPage = 0;
                        break;
                    case "s":
                        currentPage = totalPages - 1;
                        break;
                    case "a":
                        viewing = false;
                        break;
                    case string s when s.StartsWith("g "):
                        if (int.TryParse(s.Substring(2), out int page) && page >= 1 && page <= totalPages)
                        {
                            currentPage = page - 1;
                        }
                        else
                        {
                            Console.WriteLine($"❌ Ogiltigt sidnummer. Använd 1-{totalPages}");
                        }
                        break;
                    default:
                        Console.WriteLine("❌ Ogiltigt val. Tryck på valfri tangent för att fortsätta...");
                        Console.ReadKey();
                        break;
                }
            }
            
            Console.WriteLine("✅ Data visning avslutad.");
        }
    }
}
