using System.Text.Json.Serialization;
namespace runestory.src.util
{
    public class runestoryConfig
    {
        public float GlobalMagicDamageMultiplier { get; set; } = 1.0f;

        public float GlobalMagicCoolDownMultiplier { get; set; } = 1.0f;

        public int LevelUnlockedByDefault { get; set; } = 1;



        public runestoryConfig()
        {
        }

        public runestoryConfig(runestoryConfig old)
        {
            GlobalMagicDamageMultiplier = old.GlobalMagicDamageMultiplier;
        }
    }
}
