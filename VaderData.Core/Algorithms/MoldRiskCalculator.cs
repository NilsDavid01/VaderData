namespace VaderData.Core.Algorithms
{
    /// <summary>
    /// Statisk klass för beräkning och klassificering av mögelrisk baserat på temperatur och luftfuktighet
    /// 
    /// VETENSKAPLIGT UNDERLAG: 
    /// - Mögelsporer börjar växa när relativ luftfuktighet överstiger 80%
    /// - Optimal temperatur för många mögelsvampar ligger mellan 15-25°C
    /// - Riskindexet kombinerar både fuktighet och temperatur för en mer precis bedömning
    /// 
    /// ANVÄNDNINGSFALL:
    /// - Byggnadshälsa och fuktskydd
    /// - Preventiv underhållsplanering
    /// - Inomhusmiljöanalys
    /// </summary>
    public static class MoldRiskCalculator
    {
        /// <summary>
        /// Beräknar mögelriskindex baserat på temperatur och luftfuktighet
        /// 
        /// MATEMATISK MODELL:
        /// f(T, H) = { 0,                   om H ≤ 80
        ///           { (H - 80) × (T / 15), om H > 80
        /// 
        /// Där:
        ///   T = temperatur i Celsius (°C)
        ///   H = relativ luftfuktighet i procent (%)
        /// 
        /// ALGORITMISK BESKRIVNING:
        /// 1. Grundvillkor: Ingen risk vid luftfuktighet under 80%
        /// 2. Riskberäkning: Linjär ökning med luftfuktighet över tröskelvärdet
        /// 3. Temperaturkorrigering: Normaliserat till 15°C som referenspunkt
        /// 
        /// KOMPLEXITET: O(1) - Konstant tid, oberoende av inputstorlek
        /// 
        /// EXEMPELBERÄKNINGAR:
        /// - T=20°C, H=85% → (85-80) × (20/15) = 5 × 1.33 = 6.67 (Måttlig risk)
        /// - T=10°C, H=90% → (90-80) × (10/15) = 10 × 0.67 = 6.67 (Måttlig risk)  
        /// - T=25°C, H=95% → (95-80) × (25/15) = 15 × 1.67 = 25.0 (Mycket hög risk)
        /// 
        /// FYSISK TOLKNING:
        /// - Högre luftfuktighet → högre risk (linjärt över 80%)
        /// - Högre temperatur → högre risk (proportionellt mot T/15)
        /// - 15°C fungerar som normaliseringsfaktor för temperaturpåverkan
        /// </summary>
        /// <param name="temperature">Temperatur i Celsius (°C)</param>
        /// <param name="humidity">Relativ luftfuktighet i procent (%)</param>
        /// <returns>Mögelriskindex (0 = ingen risk, högre värden = högre risk)</returns>
        public static double CalculateMoldRisk(double temperature, double humidity)
        {
            // Grundvillkor: Ingen mögelrisk vid luftfuktighet under 80%
            // Mögelsporer kräver minst 80% RH för att börja växa
            if (humidity <= 80) return 0;
            
            // Riskberäkning: Kombinerar både fuktighet och temperatur
            // (humidity - 80) = Överskott av luftfuktighet över tröskelvärdet
            // (temperature / 15.0) = Temperaturkorrigering med 15°C som baseline
            return (humidity - 80) * (temperature / 15.0);
        }

        /// <summary>
        /// Klassificerar mögelriskindex i kategorier för användarvänlig presentation
        /// 
        /// KLASSIFICERINGSKRITERIER:
        /// - Baserad på praktisk erfarenhet och byggnadshälsoriktlinjer
        /// - Progressiv skala som reflekterar ökande åtgärdsbehov
        /// 
        /// RISKKATEGORIER OCH ÅTGÄRDSREKOMMENDATIONER:
        /// 
        /// █ Försumbar (index < 1)   - Ingen åtgärd krävs
        ///   • Normal luftfuktighet, ingen omedelbar risk
        ///   
        /// █ Låg (index 1-5)         - Övervakning rekommenderas  
        ///   • Måttlig fuktighet, regelbunden kontroll
        ///   • Se till för god ventilation
        ///   
        /// █ Måttlig (index 5-10)    - Åtgärder rekommenderas
        ///   • Högre fuktighet, risk för mögelutveckling
        ///   • Förbättrad ventilation, avfuktning vid behov
        ///   • Kontrollera fuktiga ytor regelbundet
        ///   
        /// █ Hög (index 10-20)       - Omedelbara åtgärder krävs
        ///   • Mycket hög fuktighet, stor mögelrisk
        ///   • Professionell avfuktning, fuktsanering
        ///   • Undersök byggnaden för fuktskador
        ///   
        /// █ Mycket hög (index ≥ 20) - Akuta åtgärder krävs
        ///   • Extrem fuktighet, mycket stor mögelrisk
        ///   • Omedelbar fuktsanering, expertutredning
        ///   • Evakuering vid omfattande kontamination
        /// 
        /// ALGORITM: Pattern matching med C# switch expression
        /// KOMPLEXITET: O(1) - Konstant tid för klassificering
        /// </summary>
        /// <param name="moldRisk">Beräknat mögelriskindex från CalculateMoldRisk</param>
        /// <returns>Textbeskrivning av risknivå enligt klassificeringsskalan</returns>
        public static string GetMoldRiskLevel(double moldRisk)
        {
            return moldRisk switch
            {
                // Försumbar risk - under gränsvärdet för mätbar påverkan
                < 1 => "Försumbar",
                
                // Låg risk - början på riskzonen, kräver uppmärksamhet
                < 5 => "Låg",
                
                // Måttlig risk - tydlig risk som kräver åtgärder
                < 10 => "Måttlig",
                
                // Hög risk - allvarlig situation som kräver omedelbara åtgärder
                < 20 => "Hög",
                
                // Mycket hög risk - akut situation med stor hälsorisk
                _ => "Mycket hög"
            };
        }
    }
}