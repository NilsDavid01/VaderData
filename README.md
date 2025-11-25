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

## Kommandon

```bash
# Bygg projekt
dotnet build

# Kör applikation
dotnet run --project VaderData.UI

# Skapa migration
dotnet ef migrations add InitialCreate --project VaderData.DataAccess

# Uppdatera databas
dotnet ef database update --project VaderData.DataAccess
