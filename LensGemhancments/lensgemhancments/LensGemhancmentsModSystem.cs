using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Vintagestory.GameContent;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
namespace LensGemhancments
{
    public class LensGemhancmentsModSystem : ModSystem
    {
        /* Quick reference to all attributes that change the characters Stats:
        healingeffectivness, maxhealthExtraPoints, walkspeed, hungerrate, rangedWeaponsAcc, rangedWeaponsSpeed
        rangedWeaponsDamage, meleeWeaponsDamage, mechanicalsDamage, animalLootDropRate, forageDropRate, wildCropDropRate
        vesselContentsDropRate, oreDropRate, rustyGearDropRate, miningSpeedMul, animalSeekingRange, armorDurabilityLoss,
        bowDrawingStrength, wholeVesselLootChance, temporalGearTLRepairCost, animalHarvestingTime*/
        public static ILogger logger;
        public Harmony harmInst;
        public const string harmID = "lensgemhance.Patches";
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            logger = api.Logger;
            harmInst = new Harmony(harmID);


            api.RegisterItemClass("gemcore", typeof(PowerCore));

            api.RegisterCollectibleBehaviorClass("lengemslotable",typeof(SlotableItem));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.RegisterEntityBehaviorClass(LenGemConst.GEM_BUFFAFFECTED, typeof(BuffAffected));
            harmInst.Patch(typeof(Vintagestory.Server.CoreServerEventManager).GetMethod("TriggerAfterActiveSlotChanged"),postfix:new HarmonyMethod(Postfix_TriggerAfterActiveSlotChanged));
        }
        public static void Postfix_TriggerAfterActiveSlotChanged(Vintagestory.Server.CoreServerEventManager __instance, IServerPlayer player,int fromSlot,int toSlot)
        {
            if (player.Entity.HasBehavior<BuffAffected>())
            {
                player.Entity.GetBehavior<BuffAffected>().onSlotSwapped(player, fromSlot, toSlot);
            }
        }
    }
}
