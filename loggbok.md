# Dag 1

Skapade en 3-lagersarkitektur för bättre separation av concerns. Core-projektet innehåller domänmodeller och affärslogik. DataAccess hanterar Entity Framework och databaskommunikation. UI-projektet är konsolapplikationen. Valde SQLite för enkel installation och cross-platform support. Denna struktur gör projektet lättunderhållet och testbart.

# Dag 2

Designade WeatherData-klassen med nullable properties för temperatur och fuktighet för att hantera felaktig data. Implementerade DailyAverage för aggregerad daglig data. Använde DbSet i WeatherContext för Entity Framework operations. Valde DateTime för tidsstämpling och string för platsinformation. Denna datamodell hanterar både rådata och aggregerad analysdata.

# Dag 3

Implementerade CSV-parser som använder string.Split() för att dela rader och double.TryParse() för numerisk validering. La till hantering av olika minus-tecken (U+2212 etc.) genom karakter-normalisering. Använde List<WeatherData> för att samla validerad data innan batch-insättning. Valideringslogiken filtrerar bort data utanför rimliga intervall (-50 till 50°C).

# Dag 4

Utvecklade MoldRiskCalculator som använder formeln (RH-80)*(T/15) när RH > 80%. Denna algoritm baseras på vetenskapliga studier om mögelväxt. För säsongsberäkning använde jag en algoritm som letar efter 5 konsekutiva dagar under tröskelvärden. Använde LINQ för gruppering och genomsnittsberäkningar. IEnumerable<T> användes för att effektivt bearbeta stora datamängder.

# Dag 5

Byggde ett meny-baserat konsolgränssnitt med switch-sats för användarinteraktion. Implementerade dependency injection med ServiceCollection för loose coupling. Använde IWeatherDataService interface för att abstrahera dataåtkomst. DisplayDataCommand-klassen skapades för att separera visningslogik. Denna design gör det enkelt att byta ut gränssnitt eller lägga till nya kommandon.

# Dag 6

Löste namespace-problem genom att lägga till using VaderData.UI.Commands. Använde async/await för att hantera databasoperationer utan att frysa gränssnittet. Implementerade proper error handling med try-catch blocks. Använde ILogger för strukturerad logging. Dessa val gör applikationen robust och lätt att felsöka.

# Dag 7

Optiminerade databasprestanda genom att implementera batch-insättning med 1000 rader per batch. Använde RemoveRange() och AddRangeAsync() för effektiva bulk-operationer. För aggregeringar använde jag LINQ GroupBy och Average med Entity Framework. Valde att köra komplexa beräkningar (mögelrisk) i minnet efter databashämtning för bättre prestanda.

# Dag 8

Testade alla analysfunktioner med riktig data. Temperatursortering använder OrderByDescending på dagliga genomsnitt. Fuktighetssortering fungerar liknande men på luftfuktighet. Mögelrisksortering kombinerar båda faktorerna. Säsongsberäkningen använder en sliding window-algoritm för att hitta säsongsförändringar. Alla funktioner returnerar top 10 resultat för överskådlighet.

# Dag 9

Implementerade automatisk databasskapelse med EnsureCreatedAsync(). La till progress reporting under CSV-läsning för användarvänlighet. Skapade en flexibel sökvägshanterare som fungerar både under utveckling och efter publicering. Använde Path.Combine för cross-platform sökvägshantering. Dessa features gör applikationen användarvänlig och pålitlig.

# Dag 10

Projektet uppfyller alla krav: Entity Framework Code First, CSV-läsning med validering, kompletta analysalgoritmer. Valet av 3-lagersarkitektur har visat sig lyckat för underhållbarhet. Algoritmerna för mögelrisk och säsongsberäkning ger meningsfulla resultat. Datastrukturerna hanterar effektivt stora datamängder. Applikationen är klar. 