using LensGemology;
using Vintagestory.API.Common;

namespace LensGemology
{
    public class LensGemology : ModSystem
    {
        public static ILogger logger;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterBlockClass("OreCrystalsCrystal", typeof(OreCrystalsCrystal));
            api.RegisterBlockEntityClass("OreCrystalsCrystalBE", typeof(OreCrystalsCrystalBE));
            logger = api.Logger;
        }
    }
}
