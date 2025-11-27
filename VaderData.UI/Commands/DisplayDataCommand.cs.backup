using VaderData.Core.Interfaces;
using VaderData.Core.Models;

namespace VaderData.UI.Commands
{
    /// <summary>
    /// Command-klass för att visa rå väderdata i konsolgränssnittet
    /// 
    /// DESIGNMÖNSTER: Command Pattern
    /// - Inkapslar en specifik operation (data visning)
    /// - Separerar UI-logik från business logic
    /// - Möjliggör enkel utökning och testning
    /// 
    /// ANSVARSOMRÅDEN:
    /// - Hanterar presentation av rådata
    /// - Formaterar utdata för användarvänlig visning
    /// - Implementerar felhantering för tomma dataset
    /// - Begränsar antal visade rader för prestanda
    /// </summary>
    public class DisplayDataCommand
    {
        private readonly IWeatherDataService _weatherService;

        /// <summary>
        /// Constructor med Dependency Injection för väderdataservice
        /// 
        /// DI-PRINCIP: Constructor Injection
        /// - Lösa kopplingar mellan lager
        /// - Enkel mockning vid enhetstestning
        /// - Tydliga beroenden
        /// 
        /// @param weatherService Service för dataåtkomst och business logic
        /// </summary>
        public DisplayDataCommand(IWeatherDataService weatherService)
        {
            _weatherService = weatherService;
        }

        /// <summary>
        /// Executer commandot för att visa väderdata i konsolen
        /// 
        /// EXEKVERINGSFLÖDE:
        /// 1. Visar rubrik för operationen
        /// 2. Hämtar rådata från service layer
        /// 3. Validerar att data finns
        /// 4. Formaterar och visar data rad för rad
        /// 5. Hanterar tomma dataset gracefullt
        /// 
        /// DATAFLÖDE:
        /// UI → Command → Service Layer → Database → Formaterad utdata
        /// 
        /// PRESTANDAÖVERVÄGNINGAR:
        /// - Begränsar till 10 rader för snabb respons
        /// - Async/await för icke-blockerande databasanrop
        /// - Minimal minnesanvändning under visning
        /// 
        /// ANVÄNDARUPPLEVELSE:
        /// - Omedelbar feedback även med stora dataset
        /// - Tydlig indikation på tomma dataset
        /// - Lättläst datum- och tidsformat
        /// - Konsekvent enhetsvisning (°C, %)
        /// </summary>
        public async Task ExecuteAsync()
        {
            // =============================================================================
            // OPERATIONS RUBRIK - Tydlig indikation på vad som händer
            // =============================================================================
            Console.WriteLine("=== Visa Väderdata ===");
            
            // =============================================================================
            // DATAHÄMTAING - Asynkron hämtning från service layer
            // =============================================================================
            var data = await _weatherService.GetRawDataAsync();
            
            // =============================================================================
            // VALIDERING - Kontrollera att data finns att visa
            // =============================================================================
            if (!data.Any())
            {
                Console.WriteLine("Ingen data hittades.");
                return;
            }

            // =============================================================================
            // DATAVISNING - Formaterad presentation av rådata
            // =============================================================================
            foreach (var item in data.Take(10))
            {
                // Formatering av varje datarad:
                // - DateTime: Standard ToString() för lokalt format
                // - Temperature: Visas med °C enhet
                // - Humidity: Visas med % enhet  
                // - Location: Visas i parentes för kontext
                Console.WriteLine($"{item.DateTime}: {item.Temperature}°C, {item.Humidity}% ({item.Location})");
            }
            
            // =============================================================================
            // ANVÄNDARINDIKATION - Visar att endast del av data visas
            // =============================================================================
            if (data.Count > 10)
            {
                Console.WriteLine($"... och {data.Count - 10} fler rader. Använd export för fullständig data.");
            }
        }
    }
}