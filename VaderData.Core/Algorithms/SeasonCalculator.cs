using VaderData.Core.Models;

namespace VaderData.Core.Algorithms
{
    /// <summary>
    /// Statisk klass för meteorologisk säsongsberäkning baserad på temperaturdata
    /// 
    /// METEOROLOGISKA DEFINITIONER (ENLIGT SMHI STANDARD):
    /// - Höst: Första dagen i en sekvens av minst 5 på varandra följande dagar 
    ///         med dygnsmedeltemperatur under 10°C
    /// - Vinter: Första dagen i en sekvens av minst 5 på varandra följande dagar 
    ///           med dygnsmedeltemperatur under 0°C
    /// 
    /// VETENSKAPLIGT UNDERLAG:
    /// - Definitionerna baseras på klimatologiska normer för Norden
    /// - 5-dagars sekvenser används för att filtrera bort tillfälliga temperaturfluktuationer
    /// - Systemet följer internationella meteorologiska standarder för säsongsdefinition
    /// 
    /// ANVÄNDNINGSFALL:
    /// - Klimatologisk analys och trendidentifiering
    /// - Jordbruksplanering och skördetidpunkter
    /// - Energiförbrukningsprognoser
    /// - Fenologiska studier (naturens säsongsförlopp)
    /// </summary>
    public static class SeasonCalculator
    {
        /// <summary>
        /// Beräknar säsongsovergångar baserat på dagliga medeltemperaturer
        /// 
        /// ALGORITMISKT FLÖDE:
        /// 1. Validering av indata - kontrollerar att data finns och är relevant
        /// 2. Höstberäkning - identifierar första 5-dagars sekvens under 10°C
        /// 3. Vinterberäkning - identifierar första 5-dagars sekvens under 0°C
        /// 4. Resultatsammansättning - skapar sammanfattning med metadata
        /// 
        /// DATABEHANDLING:
        /// - Input: Lista av dagliga medeltemperaturer (kronologiskt sorterade)
        /// - Output: SeasonResult med säsongsstartdatum och beskrivande meddelande
        /// 
        /// FELHANTERING:
        /// - Returnerar informativt meddelande vid ogiltig indata
        /// - Hanterar null och tomma listor gracefullt
        /// </summary>
        /// <param name="dailyAverages">Lista av dagliga medeltemperaturer, förväntas vara kronologiskt sorterad</param>
        /// <param name="location">Geografisk plats för analys (används i resultatmeddelande)</param>
        /// <returns>SeasonResult objekt med säsongsstartdatum och analysinformation</returns>
        public static SeasonResult CalculateSeasonsFromDailyAverages(List<DailyAverage> dailyAverages, string location)
        {
            // Validering av indata - försvarar mot ogiltiga tillstånd
            if (dailyAverages == null || !dailyAverages.Any())
            {
                return new SeasonResult 
                { 
                    Message = "Ingen data tillgänglig för säsongsberäkning" 
                };
            }

            // =============================================================================
            // SÄSONGSBERÄKNING - Använder sliding window algoritm
            // =============================================================================
            
            // Höststart: Första 5-dagars sekvensen med medeltemperatur < 10°C
            var autumnStart = FindSeasonTransition(dailyAverages, 10.0, 5);
            
            // Vinterstart: Första 5-dagars sekvensen med medeltemperatur < 0°C
            var winterStart = FindSeasonTransition(dailyAverages, 0.0, 5);

            // =============================================================================
            // RESULTATSKOMPILERING - Skapar användarvänligt resultat
            // =============================================================================
            
            return new SeasonResult
            {
                AutumnStart = autumnStart,
                WinterStart = winterStart,
                Message = $"Säsongsberäkning klar för {location}. " +
                         $"Data från {dailyAverages.Min(d => d.Date):yyyy-MM-dd} " +
                         $"till {dailyAverages.Max(d => d.Date):yyyy-MM-dd}"
            };
        }

        /// <summary>
        /// Sliding window algoritm för att identifiera säsongsovergångar
        /// 
        /// ALGORITM: 
        /// 1. Iterera genom listan av dagliga medeltemperaturer
        /// 2. För varje startposition, undersök ett fönster av 'consecutiveDays' storlek
        /// 3. Kontrollera om alla dagar i fönstret uppfyller temperaturkriteriet
        /// 4. Returnera första dagen i sekvensen om kriteriet uppfylls
        /// 
        /// KOMPLEXITETSANALYS:
        /// - Bästa fall: O(1) - säsong hittas direkt i början av dataset
        /// - Värsta fall: O(n) - måste skanna hela datasetet
        /// - Genomsnittligt: O(n) för stora dataset
        /// 
        /// OPTIMERING: 
        /// - Avbryter vid första träff (first-match semantics)
        /// - Använder LINQ för läsbarhet med behållande av prestanda
        /// 
        /// METEOROLOGISK LOGIK:
        /// - 5-dagars sekvenser eliminerar tillfälliga väderomslag
        /// - Representerar stabila klimatförhållanden
        /// - Följer etablerade meteorologiska praxis
        /// </summary>
        /// <param name="dailyAverages">Kronologiskt sorterad lista av dagliga medeltemperaturer</param>
        /// <param name="threshold">Temperaturtröskel för säsongsdefinition (°C)</param>
        /// <param name="consecutiveDays">Antal på varandra följande dagar som krävs</param>
        /// <returns>Startdatum för säsongsovergång, eller null om ingen sekvens hittades</returns>
        private static DateTime? FindSeasonTransition(
            List<DailyAverage> dailyAverages, 
            double threshold, 
            int consecutiveDays)
        {
            // Sliding window implementation
            for (int i = 0; i <= dailyAverages.Count - consecutiveDays; i++)
            {
                // Extrahera sekvens av på varandra följande dagar
                var consecutive = dailyAverages.Skip(i).Take(consecutiveDays);
                
                // Kontrollera om alla dagar i sekvensen uppfyller kriteriet:
                // - Temperaturen måste ha ett värde (inte null)
                // - Temperaturen måste vara under tröskelvärdet
                if (consecutive.All(d => 
                    d.AvgTemperature.HasValue && 
                    d.AvgTemperature.Value < threshold))
                {
                    // Returnera första dagen i sekvensen som säsongsstart
                    // Detta följer meteorologisk konvention där hela perioden
                    // tilldelas den säsong som inleds
                    return consecutive.First().Date;
                }
            }
            
            // Ingen säsongsovergång hittades som uppfyllde kriterierna
            return null;
        }
    }
}