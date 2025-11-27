using VaderData.Core.Interfaces;
using VaderData.Core.Models;

namespace VaderData.UI.Commands
{
    /// <summary>
    /// Command-klass för att visa och navigera genom väderdata med paginering
    /// 
    /// DESIGNMÖNSTER: Command Pattern
    /// - Inkapslar en specifik operation (datavisning med paginering)
    /// - Separerar UI-logik från business logic
    /// - Möjliggör enkel utökning och testning
    /// 
    /// ANSVARSOMRÅDEN:
    /// - Hanterar presentation av rådata med paginering
    /// - Implementerar navigeringslogik för stora dataset
    /// - Formaterar utdata för användarvänlig visning
    /// - Hanterar användarinput och felhantering
    /// 
    /// PAGINERINGSFÖRDELAR:
    /// - Minneseffektiv hantering av stora dataset (150,000+ rader)
    /// - Snabb respons även med mycket data
    /// - Användarvänlig navigation utan överväldigande information
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
        /// Executer commandot för att visa väderdata med full paginering
        /// 
        /// EXEKVERINGSFLÖDE:
        /// 1. Hämtar all data från service layer asynkront
        /// 2. Validerar att data finns att visa
        /// 3. Initierar pagineringssystem med 20 rader per sida
        /// 4. Visar data sida för sida med navigeringsalternativ
        /// 5. Hanterar användarens navigeringsval i realtid
        /// 
        /// PAGINERINGSALGORITM:
        /// - Sidstorlek: 20 rader (optimal för konsolvisning)
        /// - Sidberäkning: ceil(totalRader / sidStorlek)
        /// - Dataåtkomst: LINQ Skip() och Take() för effektiv minnesanvändning
        /// - Global indexering: Bevarar radnummer över sidor
        /// 
        /// ANVÄNDARUPPLEVELSE:
        /// - Omedelbar feedback även med stora dataset
        /// - Intuitiv tangentbordsnavigation
        /// - Tydlig progress-indikation (sida X av Y)
        /// - Smart felhantering för ogiltig input
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
            // PAGINERING KONFIGURATION
            // =============================================================================
            int pageSize = 20;        // Optimalt antal rader för konsolläsbarhet
            int currentPage = 0;      // Aktuell sida (0-indexed)
            int totalPages = (int)Math.Ceiling(data.Count / (double)pageSize);  // Totala antal sidor
            bool viewing = true;      // Kontrollvariabel för pagineringsloop

            // =============================================================================
            // PAGINERINGSLOOP - Huvudloop för datavisning och navigation
            // =============================================================================
            while (viewing)
            {
                // Rensa skärmen för ren visning av varje sida
                Console.Clear();
                
                // =========================================================================
                // SIDHUVUD - Visa sidinformation och progress
                // =========================================================================
                Console.WriteLine($"=== Väderdata - Sida {currentPage + 1} av {totalPages} ===");
                Console.WriteLine($"Visar rad {currentPage * pageSize + 1}-{Math.Min((currentPage + 1) * pageSize, data.Count)} av {data.Count} totalt");
                Console.WriteLine("=" .PadRight(50, '='));
                
                // =========================================================================
                // DATA VISNING - Hämta och visa data för aktuell sida
                // =========================================================================
                var pageData = data.Skip(currentPage * pageSize).Take(pageSize);
                
                // Formatera och visa varje rad på aktuell sida
                foreach (var item in pageData)
                {
                    // Formatering med konsekvent datetime och enhetsvisning
                    Console.WriteLine($"{item.DateTime:yyyy-MM-dd HH:mm}: {item.Temperature}°C, {item.Humidity}% ({item.Location})");
                }

                // =========================================================================
                // SIDFOT - Navigeringsalternativ och instruktioner
                // =========================================================================
                Console.WriteLine("\n" + "=".PadRight(50, '='));
                Console.WriteLine("Navigering:");
                Console.WriteLine("N - Nästa sida");
                Console.WriteLine("P - Föregående sida");
                Console.WriteLine("F - Första sidan");
                Console.WriteLine("S - Sista sidan");
                Console.WriteLine("G [sida] - Gå till specifik sida (t.ex. 'G 5')");
                Console.WriteLine("A - Avsluta visning");
                Console.Write("Val: ");

                // =========================================================================
                // ANVÄNDARINPUT - Läs och hantera navigeringskommandon
                // =========================================================================
                var input = Console.ReadLine()?.ToLower().Trim();
                
                // Switch-sats för navigeringslogik - O(1) lookup
                switch (input)
                {
                    case "n":  // Nästa sida
                        if (currentPage < totalPages - 1)
                            currentPage++;
                        else
                            Console.WriteLine("⚠️  Du är på sista sidan!");
                        break;
                    case "p":  // Föregående sida
                        if (currentPage > 0)
                            currentPage--;
                        else
                            Console.WriteLine("⚠️  Du är på första sidan!");
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
                    case string s when s.StartsWith("g "):  // Gå till specifik sida
                        if (int.TryParse(s.Substring(2), out int page) && page >= 1 && page <= totalPages)
                        {
                            currentPage = page - 1;  // Konvertera till 0-indexed
                        }
                        else
                        {
                            Console.WriteLine($"❌ Ogiltigt sidnummer. Använd 1-{totalPages}");
                        }
                        break;
                    default:  // Ogiltigt kommando
                        Console.WriteLine("❌ Ogiltigt val. Tryck på valfri tangent för att fortsätta...");
                        Console.ReadKey();
                        break;
                }
            }
            
            // =============================================================================
            // AVSLUTNING - Bekräfta att datavisning är klar
            // =============================================================================
            Console.WriteLine("✅ Data visning avslutad.");
        }
    }
}