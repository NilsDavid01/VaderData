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

            foreach (var item in data.Take(10))
            {
                Console.WriteLine($"{item.DateTime}: {item.Temperature}°C, {item.Humidity}% ({item.Location})");
            }
        }
    }
}
