using Vintagestory.API.Common;

namespace LensstoryMod {
    public class LensTweaks : ModSystem
    {
        public static ILogger logger;
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterItemClass("lenspruningscissors", typeof(PruningScissors));

            api.RegisterBlockClass("lensreinforcedbloomeryblock", typeof(ReinforcedBloomery));
            api.RegisterBlockEntityClass("lensreinforcedbloomery", typeof(ReinforcedBloomeryBE));

            api.RegisterBlockClass("lensheaterblock", typeof(HeaterBlock));
            api.RegisterBlockEntityClass("lensheater",typeof(HeaterBE));

            api.RegisterBlockClass("lensmagmaforgeblock", typeof(MagmaForgeBlock));
            api.RegisterBlockEntityClass("lensmagmaforge",typeof (MagmaForgeBe));
            api.RegisterBlockClass("lensbeehiveblock", typeof(WoodenHiveBlock));
            api.RegisterBlockEntityClass("lensbeehive", typeof(WoodenHiveBE));
            api.RegisterItemClass("lensblowdartgun", typeof(Blowdartgun));
            api.RegisterEntity("lenssimpleprojectile", typeof(EntitySimpleProjectile));
            logger = api.Logger;
        }
        internal static void LogError(string message)
        {
            logger?.Error("(LensStory):{0}",message);
        }
    }
}
