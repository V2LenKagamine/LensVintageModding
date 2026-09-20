using EpxVe.src;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace EpxVe
{
    public class EpxVeModSystem : ModSystem
    {

        public static string VEEP_ConfigName = "veep_common.json";

        public VEEPConfig LoadedConfig;

        public override void StartPre(ICoreAPI api)
        {
            if(api is ICoreServerAPI)
            {
                LoadedConfig = GetConfig(api);
            }
        }


        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockClass("BlockVeepConverter", typeof(BlockConverter));
            api.RegisterBlockEntityClass("BEVeepConverter", typeof(BEConverter));
            api.RegisterBlockEntityBehaviorClass("BEBhvVeepToEP",typeof(BEBhvConverterToEP));
            api.RegisterBlockEntityBehaviorClass("BEBhvVeepToVE", typeof(BEBhvConverterToVE));
        }

        public static VEEPConfig GetConfig(ICoreAPI api)
        {
            VEEPConfig tmp;

            try
            {
                tmp = api.LoadModConfig<VEEPConfig>(VEEP_ConfigName);
                if (tmp is null)
                {
                    tmp = new VEEPConfig();
                    api.StoreModConfig<VEEPConfig>(tmp, VEEP_ConfigName);
                }
                else
                {
                    api.StoreModConfig<VEEPConfig>(new VEEPConfig(tmp), VEEP_ConfigName);
                    tmp = api.LoadModConfig<VEEPConfig>(VEEP_ConfigName);
                }
            }
            catch (Exception e)
            {
                api.Logger.Error("VEEP: SOMEONE SCREWED UP THE CONFIG, REBUILDING FROM SCRATCH. Exception: " + e);
                tmp = new VEEPConfig();
                api.StoreModConfig<VEEPConfig>(tmp, VEEP_ConfigName);
            }
            return tmp;
        }
    }
}
