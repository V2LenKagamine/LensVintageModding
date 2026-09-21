using System.Text.Json.Serialization;
namespace EpxVe.src
{
    public class VEEPConfig
    {

        public float VEPowerPerOneEPPower { get; set; } = 3/13f;
         
        public float EPPowerPerOneVEPower { get; set; } = 13/3f;


        public VEEPConfig() { }

        public VEEPConfig(VEEPConfig old)
        {
            VEPowerPerOneEPPower = old.VEPowerPerOneEPPower;
            EPPowerPerOneVEPower = old.EPPowerPerOneVEPower;
        }
    }
}
