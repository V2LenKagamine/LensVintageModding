using HarmonyLib;
using System;
using thequenchiest.src;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace thequenchiest
{
    public class thequenchiestModSystem : ModSystem
    {

        public static string TQ_ConfigName => "quenchiest_common.json";

        public quenchiestConfig TQ_LoadedConfig;

        private Harmony quenchHarm;

        public override void StartPre(ICoreAPI api)
        {
            TQ_LoadedConfig = GetConfig(api);
        }
        public override void Start(ICoreAPI api)
        {
            api.RegisterCollectibleBehaviorClass("Quenchable", typeof(QuenchierQuenchBHV));
            api.Logger.Notification("Nothing Quenchier, it\'s the Quenchiest!");
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            if(!Harmony.HasAnyPatches("thequenchiest"))
            {
                quenchHarm = new Harmony("thequenchiest");
                QuenchiestHarmony.ApplyPatch(quenchHarm, api.Logger);
            }
        }

        public override void Dispose()
        {
            if (quenchHarm != null)
            {
                quenchHarm.UnpatchAll("thequenchiest");
                quenchHarm = null;
            }
            base.Dispose();
        }


        public static quenchiestConfig GetConfig(ICoreAPI api)
        {
            quenchiestConfig tmp;

            try
            {
                tmp = api.LoadModConfig<quenchiestConfig>(TQ_ConfigName);
                if (tmp is null)
                {
                    tmp = new quenchiestConfig();
                    api.StoreModConfig<quenchiestConfig>(tmp, TQ_ConfigName);
                }
                else
                {
                    api.StoreModConfig<quenchiestConfig>(new quenchiestConfig(tmp), TQ_ConfigName);
                    tmp = api.LoadModConfig<quenchiestConfig>(TQ_ConfigName);
                }
            }
            catch (Exception e)
            {
                api.Logger.Error("QuenchiestConfig: SOMEONE SCREWED UP THE CONFIG, REBUILDING FROM SCRATCH. Exception: " + e);
                tmp = new quenchiestConfig();
                api.StoreModConfig<quenchiestConfig>(tmp, TQ_ConfigName);
            }
            return tmp;
        }
    }
}
