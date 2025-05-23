using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace LensstoryMod
{
    public class LensCommonsMod : ModSystem
    {
        public static ILogger logger;
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterItemClass("lenstempgiftitem", typeof(TemporalGiftItem));
            logger = api.Logger;
        }
        internal static void LogError(string message)
        {
            logger?.Error("(LensClasses):{0}", message);
        }
    }
}
