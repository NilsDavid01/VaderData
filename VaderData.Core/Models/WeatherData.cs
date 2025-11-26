namespace VaderData.Core.Models
{
    /// <summary>
    /// Representerar en individuell väderobservation med temperatur och luftfuktighet
    /// 
    /// ENTITETSBESKRIVNING:
    /// - Huvudentitet i domänmodellen för väderdata
    /// - Mappas direkt till databastabell via Entity Framework
    /// - Används för både rådata och bearbetad analys
    /// 
    /// DATAMODELLERING:
    /// - Normaliserad struktur för effektiv lagring
    /// - Nullable properties för hantering av saknad data
    /// - Valideringsflaggor för datakvalitetshantering
    /// 
    /// ANVÄNDNINGSFALL:
    /// - Direkt lagring av CSV-import data
    /// - Grunddata för alla analyser och beräkningar
    /// - Historisk datalagring för trendanalys
    /// </summary>
    public class WeatherData
    {
        /// <summary>
        /// Primärnyckel för databasentiteten
        /// 
        /// DATABASMAPPING:
        /// - Auto-increment identity column
        /// - Unik identifierare för varje observation
        /// - Används för relationer och indexering
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Tidpunkt för väderobservationen
        /// 
        /// TEMPORAL ANALYS:
        /// - Nyckel för tidsserieanalys
        /// - Används för säsongsberäkningar
        /// - Grund för kronologisk sortering
        /// 
        /// DATAFORMAT:
        /// - DateTime inklusive tidpunkt
        /// - Svensk tidszon (UTC+1/UTC+2)
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// Plats för väderobservationen
        /// 
        /// PLATSKODNING:
        /// - "Inomhus" för inomhusmätningar
        /// - "Utomhus" för utomhusmätningar
        /// - Normaliserad från CSV ("ute" → "Utomhus", "inne" → "Inomhus")
        /// 
        /// ANALYSANVÄNDNING:
        /// - Dimension för platsbaserad analys
        /// - Jämförelser mellan inomhus/utomhus
        /// - Geografisk segmentering
        /// </summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// Temperatur i Celsius grader
        /// 
        /// FYSISK VALIDERING:
        /// - Acceptabelt intervall: -50°C till +50°C
        /// - Nullable för hantering av saknad eller ogiltig data
        /// - Precision: double för hög noggrannhet
        /// 
        /// METEOROLOGISK BETYDELSE:
        /// - Primär parameter för väderanalys
        /// - Ingår i säsongsberäkningar
        /// - Påverkar mögelriskberäkningar
        /// </summary>
        public double? Temperature { get; set; }

        /// <summary>
        /// Relativ luftfuktighet i procent
        /// 
        /// FYSISK VALIDERING:
        /// - Acceptabelt intervall: 0% till 100%
        /// - Nullable för hantering av saknad eller ogiltig data
        /// - Precision: double för hög noggrannhet
        /// 
        /// BYGGNADSFYSIKALISK BETYDELSE:
        /// - Avgörande för komfort och hälsa
        /// - Huvudparameter för mögelriskberäkning
        /// - Indikator för ventilationsbehov
        /// </summary>
        public double? Humidity { get; set; }

        /// <summary>
        /// Flagga som indikerar om observationen är giltig för analys
        /// 
        /// DATAQUALITY MANAGEMENT:
        /// - True: Data uppfyller alla valideringskriterier
        /// - False: Data har fel och ska exkluderas från analyser
        /// - Används för att filtrera bort ogiltiga observationer
        /// 
        /// VALIDERINGSKRITERIER:
        /// - Temperatur mellan -50°C och +50°C
        /// - Luftfuktighet mellan 0% och 100%
        /// - Korrekt datetime-format
        /// - Korrekt platskodning
        /// </summary>
        public bool IsValid { get; set; } = true;

        /// <summary>
        /// Beskrivning av valideringsfel för ogiltiga observationer
        /// 
        /// DEBUGGING OCH FELSPÅRNING:
        /// - Tom sträng för giltiga observationer
        /// - Beskrivande felmeddelande för ogiltiga
        /// - Används för att identifiera dataquality problem
        /// 
        /// FELTYPER:
        /// - Parsing errors från CSV
        /// - Value out of range
        /// - Missing required fields
        /// - Format violations
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Representerar aggregerade dagliga medelvärden för väderdataanalys
    /// 
    /// ANALYSENTITET:
    /// - Beräknad entitet för trendanalys och visualisering
    /// - Reducerar brus i data genom genomsnittsbildning
    /// - Grund för långsiktiga klimatstudier
    /// 
    /// BERÄKNINGSALGORITM:
    /// - Gruppering av WeatherData per datum och plats
    /// - Genomsnittsberäkning för temperatur och luftfuktighet
    /// - Mögelriskberäkning baserat på medelvärden
    /// </summary>
    public class DailyAverage
    {
        /// <summary>
        /// Datum för den aggregerade analysen
        /// 
        /// TEMPORAL STRUKTUR:
        /// - Representerar ett helt dygn (00:00-23:59)
        /// - Används för dagliga jämförelser
        /// - Kronologisk sorteringsnyckel
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Genomsnittlig dagstemperatur i Celsius
        /// 
        /// BERÄKNING:
        /// - Genomsnitt av alla giltiga observationer under dygnet
        /// - Nullable om inga giltiga observationer finns
        /// - Används för säsongsberäkningar och trendanalys
        /// 
        /// KLIMAATOLOGISK BETYDELSE:
        /// - Grund för väderstatistik
        /// - Identifiering av värmeböljor/köldknäppar
        /// - Jämförelse med klimatologiska normer
        /// </summary>
        public double? AvgTemperature { get; set; }

        /// <summary>
        /// Genomsnittlig daglig luftfuktighet i procent
        /// 
        /// BERÄKNING:
        /// - Genomsnitt av alla giltiga observationer under dygnet
        /// - Nullable om inga giltiga observationer finns
        /// - Används för komfortanalys och fuktproblem
        /// 
        /// BYGGNADSHÄLSOMÅSSIG BETYDELSE:
        /// - Indikator för inomhusklimat
        /// - Grund för ventilationsrekommendationer
        /// - Varning för mögeltillväxt
        /// </summary>
        public double? AvgHumidity { get; set; }

        /// <summary>
        /// Beräknad mögelrisk baserad på dagliga medelvärden
        /// 
        /// ALGORITMISK BERÄKNING:
        /// - MoldRiskCalculator.CalculateMoldRisk(AvgTemperature, AvgHumidity)
        /// - f(T,H) = (H - 80) × (T / 15) för H > 80%
        /// - Nullable om temperatur eller luftfuktighet saknas
        /// 
        /// RISKKLASSIFICERING:
        /// - 0-1: Försumbar risk
        /// - 1-5: Låg risk  
        /// - 5-10: Måttlig risk
        /// - 10-20: Hög risk
        /// - 20+: Mycket hög risk
        /// 
        /// PREVENTIV ANVÄNDNING:
        /// - Tidig varning för fuktproblem
        /// - Underhållsplanering
        /// - Hälsoriskbedömning
        /// </summary>
        public double? MoldRisk { get; set; }
    }

    /// <summary>
    /// Resultatentitet för meteorologiska säsongsberäkningar
    /// 
    /// KLIMAATOLOGISK ANALYS:
    /// - Sammanställning av säsongsidentifiering
    /// - Innehåller både data och beskrivande information
    /// - Används för rapporter och visualisering
    /// 
    /// METEOROLOGISK STANDARD:
    /// - Baserat på SMHI:s definitioner för säsonger
    /// - Använder 5-dagars regel för stabila övergångar
    /// - Följer internationella klimatologiska normer
    /// </summary>
    public class SeasonResult
    {
        /// <summary>
        /// Startdatum för meteorologisk höst
        /// 
        /// DEFINITION:
        /// - Första dagen i en sekvens av 5 på varandra följande dagar
        ///   med dygnsmedeltemperatur under 10°C
        /// - Nullable om ingen höst kunde identifieras i datasetet
        /// 
        /// FENOLOGISK KORRELATION:
        /// - Sammanfaller ofta med lövens färgförändring
        /// - Relaterad till skördetider i jordbruk
        /// - Påverkar djurs beteenden och migration
        /// </summary>
        public DateTime? AutumnStart { get; set; }

        /// <summary>
        /// Startdatum för meteorologisk vinter
        /// 
        /// DEFINITION:
        /// - Första dagen i en sekvens av 5 på varandra följande dagar
        ///   med dygnsmedeltemperatur under 0°C  
        /// - Nullable om ingen vinter kunde identifieras i datasetet
        /// 
        /// PRAKTISK BETYDELSE:
        /// - Start för väghållningssäsong
        /// - Energiförbrukningsplanering
        /// - Vinterturism och friluftsliv
        /// </summary>
        public DateTime? WinterStart { get; set; }

        /// <summary>
        /// Beskrivande meddelande om säsongsberäkningen
        /// 
        /// ANVÄNDARKOMMUNIKATION:
        /// - Sammanfattar analysresultatet
        /// - Inkluderar plats och datumintervall
        /// - Förklarar eventuella begränsningar eller oklarheter
        /// 
        /// INNEHÅLLSEXEMPEL:
        /// - "Säsongsberäkning klar för Utomhus. Data från 2024-01-01 till 2024-12-31"
        /// - "Ingen vinter kunde identifieras under observationsperioden"
        /// - "Begränsad datatillgänglighet kan påverka resultatets noggrannhet"
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}