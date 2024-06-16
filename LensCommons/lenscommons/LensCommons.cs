using Vintagestory.API.Common;

namespace LensstoryMod {
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
            logger?.Error("(LensCommons):{0}",message);
        }
    }
}
