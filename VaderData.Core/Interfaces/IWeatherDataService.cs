using VaderData.Core.Models;

namespace VaderData.Core.Interfaces
{
    /// <summary>
    /// Interface för väderdataservice - definierar kontrakt för business logic och dataåtkomst
    /// 
    /// DESIGNMÖNSTER: 
    /// - Repository Pattern för datalagringsabstraktion
    /// - Strategy Pattern för olika analysalgoritmer
    /// - Dependency Injection för lösa kopplingar
    /// 
    /// ARKITEKTURELL ROLL:
    /// - Separerar business logic från dataåtkomstimplementation
    /// - Möjliggör enhetstestning med mock implementations
    /// - Definierar klar API för UI-lagret
    /// - Implementerar Single Responsibility Principle
    /// 
    /// IMPLEMENTERINGSKRAV:
    /// - Thread-safe för samtidiga anrop
    /// - Async/await för icke-blockerande operationer
    /// - Felhantering och logging
    /// - Prestandaoptimering för stora dataset
    /// </summary>
    public interface IWeatherDataService
    {
        /// <summary>
        /// Initialiserar databasen och skapar nödvändiga tabeller
        /// 
        /// FUNKTIONALITET:
        /// - Skapar databasschema baserat på Entity Framework modeller
        /// - Konfigurerar index för optimerade queries
        /// - Säkerställer databasanslutning
        /// 
        /// ANVÄNDNING:
        /// - Körs vid applikationsstart
        /// - Krävs innan andra operationer kan utföras
        /// - Idempotent - kan köras flera gånger säkert
        /// 
        /// EXCEPTIONS:
        /// - DatabaseConnectionException vid anslutningsfel
        /// - SchemaCreationException vid tabellskapningsfel
        /// </summary>
        Task InitializeDatabaseAsync();

        /// <summary>
        /// Laddar och processar väderdata från CSV-fil till databasen
        /// 
        /// DATAFLÖDE:
        /// 1. Validerar CSV-filens existens och format
        /// 2. Parsar rad-för-rad med felhantering
        /// 3. Validerar och normaliserar data
        /// 4. Batch-inserter i databasen för prestanda
        /// 
        /// ALGORITMER:
        /// - Unicode-normalisering för nummerformat
        /// - Fysisk validering (-50°C till +50°C, 0-100% RH)
        /// - Batch processing med 1000 rader per batch
        /// 
        /// PERFORMANS:
        /// - O(n) tidskomplexitet för n rader
        /// - Progress reporting under processing
        /// - Minneseffektiv stream processing
        /// 
        /// @param filePath Sökväg till CSV-filen
        /// </summary>
        Task LoadDataFromCsvAsync(string filePath);

        /// <summary>
        /// Beräknar dagliga medelvärden för specifikt datum och plats
        /// 
        /// ANALYSALGORITM:
        /// - Gruppering per dag med SQL GROUP BY
        /// - Genomsnittsberäkning för temperatur och luftfuktighet
        /// - Mögelriskberäkning för varje dag
        /// 
        /// DATABASQUERY:
        /// SELECT Date, AVG(Temperature), AVG(Humidity)
        /// FROM WeatherData 
        /// WHERE Date = @date AND Location = @location AND IsValid = true
        /// GROUP BY Date
        /// 
        /// ANVÄNDNINGSFALL:
        /// - Daglig väderöversikt
        /// - Trendanalys för specifika dagar
        /// - Jämförelser mellan olika platser
        /// 
        /// @param date Datum för analys
        /// @param location Plats för analys ("Inomhus"/"Utomhus")
        /// @return Lista med dagliga medelvärden inklusive mögelrisk
        /// </summary>
        Task<List<DailyAverage>> GetDailyAveragesAsync(DateTime date, string location);

        /// <summary>
        /// Hämtar dagar sorterade efter temperatur (varmast först)
        /// 
        /// SORTERINGSALGORITM:
        /// 1. Beräkna dagliga medeltemperaturer
        /// 2. Sortera fallande efter temperatur
        /// 3. Returnera topp 10 varmaste dagarna
        /// 
        /// METEOROLOGISK ANVÄNDNING:
        /// - Identifiera värmeböljor
        /// - Analysera temperaturrekord
        /// - Planera kylbehov
        /// 
        /// @param location Plats för analys
        /// @return Topp 10 varmaste dagarna med temperatur och mögelrisk
        /// </summary>
        Task<List<DailyAverage>> GetTemperatureSortedAsync(string location);

        /// <summary>
        /// Hämtar dagar sorterade efter luftfuktighet (fuktigast först)
        /// 
        /// ANALYSALGORITM:
        /// - Gruppering till dagliga medelvärden
        /// - Sortering fallande efter luftfuktighet
        /// - Topp 10 presentation
        /// 
        /// BYGGNADSFYSIKALISK ANVÄNDNING:
        /// - Identifiera fuktperioder för mögelförebyggelse
        /// - Analysera luftfuktighetstrender
        /// - Planera ventilationsbehov
        /// 
        /// @param location Plats för analys
        /// @return Topp 10 fuktigaste dagarna med luftfuktighet och mögelrisk
        /// </summary>
        Task<List<DailyAverage>> GetHumiditySortedAsync(string location);

        /// <summary>
        /// Hämtar dagar sorterade efter mögelrisk (högst risk först)
        /// 
        /// BERÄKNINGSALGORITM:
        /// 1. Beräkna dagliga medeltemperaturer och luftfuktighet
        /// 2. Applicera MoldRiskCalculator: f(T,H) = (H-80) * (T/15)
        /// 3. Sortera fallande efter mögelriskindex
        /// 4. Returnera topp 10 dagar med högst risk
        /// 
        /// PREVENTIV ANVÄNDNING:
        /// - Proaktiv mögelförebyggelse
        /// - Byggnadshälsa och fuktskydd
        /// - Underhållsplanering
        /// 
        /// @param location Plats för analys
        /// @return Topp 10 dagar med högst mögelrisk
        /// </summary>
        Task<List<DailyAverage>> GetMoldRiskSortedAsync(string location);

        /// <summary>
        /// Beräknar meteorologiska säsonger baserat på temperaturdata
        /// 
        /// METEOROLOGISK ALGORITM:
        /// - Använder SeasonCalculator med sliding window approach
        /// - Höst: 5 på varandra följande dagar med T < 10°C
        /// - Vinter: 5 på varandra följande dagar med T < 0°C
        /// 
        /// KLIMAATOLOGISK ANALYS:
        /// - Identifierar säsongsovergångar
        /// - Analyserar klimattrender
        /// - Jämför med historiska normer
        /// 
        /// @param location Plats för säsongsberäkning
        /// @return SeasonResult med säsongsstartdatum och analysinformation
        /// </summary>
        Task<SeasonResult> GetSeasonsAsync(string location);

        /// <summary>
        /// Hämtar rådata från databasen med valfria datumfilter
        /// 
        /// DATABASQUERY:
        /// - Använder LINQ med conditional filtering
        /// - Sorterar kronologiskt
        /// - Begränsar till 50 poster för prestanda
        /// 
        /// ANVÄNDNINGSFALL:
        /// - Debugging och dataverifiering
        /// - Manuell dataanalys
        /// - Export av urval av data
        /// 
        /// @param startDate Startdatum för filter (valfritt)
        /// @param endDate Slutdatum för filter (valfritt)
        /// @return Lista av WeatherData objekt med rådata
        /// </summary>
        Task<List<WeatherData>> GetRawDataAsync(DateTime? startDate = null, DateTime? endDate = null);
    }
}