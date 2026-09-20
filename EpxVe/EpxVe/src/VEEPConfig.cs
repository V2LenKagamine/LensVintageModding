using System.Text.Json.Serialization;
namespace EpxVe.src
{
    public class VEEPConfig
    {

        public float PowerInMultiplier { get; set; } = 1f;

        public float EPPowerPerOneVEPower { get; set; } = 13/16f;


        public VEEPConfig() { }

        public VEEPConfig(VEEPConfig old)
        {
            PowerInMultiplier = old.PowerInMultiplier;
            EPPowerPerOneVEPower = old.EPPowerPerOneVEPower;
        }
    }
}
