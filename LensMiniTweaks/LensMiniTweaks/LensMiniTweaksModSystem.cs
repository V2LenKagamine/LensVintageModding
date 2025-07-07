using LensMiniTweaks.src.blocks;
using LensstoryMod;
using Vintagestory.API.Common;

namespace LensMiniTweaks
{
    public class LensMiniTweaksModSystem : ModSystem
    {
        public static ILogger logger;
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterItemClass("lenspruningscissors", typeof(PruningScissors));
            api.RegisterItemClass("lentreemeal", typeof(TreegrowItem));

            api.RegisterBlockClass("lensshroomstrateblock", typeof(MushroomSubBlock));
            api.RegisterBlockEntityClass("lensshroomstrate", typeof(MushroomSubBE));

            api.RegisterBlockClass("lenswaterfillblock", typeof(WaterfillBlock));
            api.RegisterBlockEntityClass("lenswaterfillbe", typeof(WaterfillBE));

            api.RegisterBlockClass("lensreinforcedbloomeryblock", typeof(ReinforcedBloomery));
            api.RegisterBlockEntityClass("lensreinforcedbloomery", typeof(ReinforcedBloomeryBE));

            logger = api.Logger;
        }
        internal static void LogError(string message)
        {
            logger?.Error("(LensMiniTweaks):{0}", message);
        }
    }
}