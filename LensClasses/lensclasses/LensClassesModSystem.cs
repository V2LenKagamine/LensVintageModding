using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace LensstoryMod
{
    public class LensClassesMod : ModSystem
    {
        public static ILogger logger;
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterItemClass("scrollitem", typeof(ScrollItem));
            logger = api.Logger;
        }
        internal static void LogError(string message)
        {
            logger?.Error("(LensStory):{0}", message);
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Event.PlayerNowPlaying += (IServerPlayer player) =>
            {
                if (player.Entity is EntityPlayer)
                {
                    Entity e = player.Entity;
                    e.AddBehavior(new ScrollStuffBhv(e));
                }
            };
        }
    }
}
