using System.Text.Json.Serialization;
namespace thequenchiest.src
{
    public class quenchiestConfig
    {
        public float AdditivePercentPerQuenchSuccess { get; set; } = 0.07f;

        public float PercentStatsKeptOnOverstress { get; set; } = 0.4f;

        public float AvgStressTolerance { get; set; } = 35.0f;

        public float StressToleranceVariance { get; set; } = 35.0f;

        public quenchiestConfig()
        {
        }

        public quenchiestConfig(quenchiestConfig old)
        {
            AdditivePercentPerQuenchSuccess = old.AdditivePercentPerQuenchSuccess;
            PercentStatsKeptOnOverstress = old.PercentStatsKeptOnOverstress;
            AvgStressTolerance = old.AvgStressTolerance;
            StressToleranceVariance = old.StressToleranceVariance;
        }
    }
}
