namespace VaderData.Core.Algorithms
{
    public static class MoldRiskCalculator
    {
        public static double CalculateMoldRisk(double temperature, double humidity)
        {
            if (humidity <= 80) return 0;
            return (humidity - 80) * (temperature / 15.0);
        }

        public static string GetMoldRiskLevel(double moldRisk)
        {
            return moldRisk switch
            {
                < 1 => "Försumbar",
                < 5 => "Låg",
                < 10 => "Måttlig",
                < 20 => "Hög",
                _ => "Mycket hög"
            };
        }
    }
}
