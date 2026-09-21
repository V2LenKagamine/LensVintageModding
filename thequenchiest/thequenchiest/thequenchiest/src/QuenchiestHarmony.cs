using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace thequenchiest.src
{
    public static class QuenchiestHarmony
    {
        public static bool Patchgood { get; private set; } = false;

        public static void ApplyPatch(Harmony harmony, ILogger logger)
        {
            try
            {
                MethodInfo methodInfo = AccessTools.Method(typeof(CollectibleBehaviorQuenchable), "CoolToTemperature", null, null);
                if (methodInfo == null)
                {
                    logger.Warning("Quenchiest: Can\'t find \'CoolToTemperature\',No quench ticks for you.");
                }
                else
                {
                    MethodInfo ourPatch = AccessTools.Method(typeof(QuenchiestHarmony), "CoolToTemperaturePrefix", null, null);
                    harmony.Patch(methodInfo, new HarmonyMethod(ourPatch), null, null, null);
                    Patchgood = true;
                }
            }
            catch (Exception e)
            {
                logger.Warning("Quenchiest: Can\'t patch for some reason, No quench ticks for you. Error: {0}", e );
            }
        }
        private static void CoolToTemperaturePrefix(IWorldAccessor world, ItemSlot slot, Vec3d pos, float targetTemperature)
        {
            if (world is null || pos is null)
            {
                return;
            }
            ItemStack stacc = slot is null ? slot.Itemstack : null;
            if (stacc is null)
            {
                return;
            }
            CollectibleObject collectible = stacc.Collectible;
            QuenchierQuenchBHV stressQuenchBehavior = (collectible is not null ? collectible.GetBehavior<QuenchierQuenchBHV>() : null);
            if (stressQuenchBehavior is null)
            {
                return;
            }
            try
            {
                stressQuenchBehavior.OnCoolingTick(world, stacc, pos, targetTemperature);
            }
            catch (Exception)
            {
                //fuck don't let them know
            }
        }
    }
}
