# VaderData Solution

En applikation för analys av väderdata med Entity Framework Core.

## Projektstruktur

- **VaderData.Core** - Business logic, modeller och algoritmer
- **VaderData.DataAccess** - Dataåtkomst med Entity Framework
- **VaderData.UI** - Konsolapplikation för användargränssnitt

## Funktioner

- Läsning av CSV-data med validering
- Beräkning av medeltemperatur och luftfuktighet
- Sortering av data efter olika kriterier
- Beräkning av mögelrisk
- Meteorologiska säsongsberäkningar

## Kommandon för att köra applikationen

```bash
# Hämta applikationen
git clone https://github.com/NilsDavid01/VaderData.git

# Navigera till projektmappen
cd VaderData/

# Bygg applikationen
dotnet build

# Kör applikationen
dotnet run --project VaderData.UI
