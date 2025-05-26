using Vintagestory.API.Common;

namespace LensMiniTweaks
{
    public class LensMiniTweaksModSystem : ModSystem
    {
        public static ILogger logger;
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterBlockClass("lensshroomstrateblock", typeof(MushroomSubBlock));
            api.RegisterBlockEntityClass("lensshroomstrate", typeof(MushroomSubBE));

            logger = api.Logger;
        }
        internal static void LogError(string message)
        {
            logger?.Error("(LensMiniTweaks):{0}", message);
        }
    }
}