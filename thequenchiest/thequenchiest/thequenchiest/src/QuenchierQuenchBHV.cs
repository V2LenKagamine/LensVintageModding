using Cairo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace thequenchiest.src
{
    public class QuenchierQuenchBHV : CollectibleBehaviorQuenchable
    {
        #region const stuff

        private static readonly AssetLocation SnapSound = new AssetLocation("thequenchiest:sounds/effect/anvilsnap");

        public const string PowerValue = "powervalue";
        public const string DurationBonus = "durationbonus";
        public const string State = "metalworkingstate";
        public const string StateChangeHours = "statechangetotalhours";
        public const string LastInQuenchH = "lastinquenchrangetotalhours";
        public const string LastInTemperH = "lastintemperrangetotalhours";
        public const string QuenchIteration = "quenchiteration";
        public const string TemperIteration = "temperIteration";
        public const string ShatterChance = "shatterchance";
        public const string ClayCovered = "clayCovered";
        public const string WillBreak = "willbreak";
        public const string LastCool = "tq:lastCoolMs";
        public const string LastRing = "tq:lastRingMs";
        public const string StressTolerance = "tq:stressTolerance";
        public const string CurrentStress = "tq:currentStress";
        public const string QuenchCount = "tq:quenchCount";
        public const string OverStressed = "tq:overstressed";
        public const string CoolX = "tq:coolX";
        public const string CoolY = "tq:coolY";
        public const string CoolZ = "tq:coolZ";
        public const string ChainBeatMS = "tq:chainBeatMs";
        public const string StateSettled = "settled";
        public const string StateTemper = "temper";
        public const string StateOverheat = "overheat";
        public const string StateQuench = "quench";
        #endregion

        private class TickChain
        {
            public IWorldAccessor World;
            public ItemStack Stack;
            public float StartTemp;
            public float From;
            public float To;
            public bool First;
        }

        private string metalVariantGroupCode;
        private MetalPropertyVariant worldQuenchProps;
        public QuenchierQuenchBHV(CollectibleObject collObj) : base(collObj)
        {
        }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            metalVariantGroupCode = properties?["metalVariantgroupCode"].AsString("metal");
            BreakChancePerQuench = 0f;
            TemperShatterMultiplier = 1f;
            TemperPowerMultiplier = 1f;
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            collObj.Variant.TryGetValue(metalVariantGroupCode, out string text);
            if (text is null)
            {
                api.Logger.Warning("[TheQuenchiest] {0} is quenchable but has no metal code in variant group \"{1}\"", collObj.Code, metalVariantGroupCode);
                return;
            }
            worldQuenchProps = api.Assets.Get("worldproperties/block/metal.json").ToObject<MetalWorldProperties>().Variants.FirstOrDefault((MetalPropertyVariant p) => p.Code.ToShortString().Equals(text));
            if (worldQuenchProps is null)
            {
                api.Logger.Warning("[TheQuenchiest] {0}: metal \"{1}\" has no quench temperatures in worldproperties/block/metal.json, heat treatment disabled for it.", collObj.Code, text);
            }
        }

        public override void SetTemperature(IWorldAccessor world, ItemStack itemstack, float temperature, bool delayCooldown, ref EnumHandling handling)
        {
            if (worldQuenchProps is null || itemstack is null)
            {
                return;
            }
            EnsureMigrated(world, itemstack);
            UpdateQuenchTimestamp(world, itemstack, temperature);
            string state = GetState(itemstack);
            if (state == StateOverheat)
            {
                if (temperature < worldQuenchProps.settledTemperature)
                {
                    SetState(world, itemstack, StateSettled);
                }
                return;
            }
            if (temperature > worldQuenchProps.quenchMaxTemp)
            {
                SetState(world, itemstack, StateOverheat);
            }
            else if (temperature > worldQuenchProps.quenchMinTemp)
            {
                SetState(world, itemstack, StateQuench);
            }
            TrySettle(world, itemstack, temperature, state);
        }

        public override void AfterGetTemperature(IWorldAccessor world, ItemStack itemstack, float temperature, ref EnumHandling handling)
        {
            if (worldQuenchProps is null || itemstack is null)
            {
                return;
            }
            EnsureMigrated(world, itemstack);
            UpdateQuenchTimestamp(world, itemstack, temperature);
            TrySettle(world, itemstack, temperature, GetState(itemstack));
        }
        private void UpdateQuenchTimestamp(IWorldAccessor world, ItemStack stack, float temperature)
        {
            if (temperature > worldQuenchProps.quenchMinTemp && temperature < worldQuenchProps.quenchMaxTemp)
            {
                stack.Attributes.SetDouble(LastInQuenchH, world.Calendar.ElapsedHours);
            }
        }
        private void TrySettle(IWorldAccessor world, ItemStack stack, float temperature, string currentState)
        {
            if (currentState != StateQuench)
            {
                return;
            }
            if (temperature > worldQuenchProps.settledTemperature)
            {
                return;
            }
            if (world.Calendar.ElapsedHours - stack.Attributes.GetDouble(LastInQuenchH, 0) < 0.25)
            {
                ApplyQuench(world, stack);
            }
            SetState(world, stack, StateSettled);
            stack.TempAttributes.RemoveAttribute(WillBreak);
        }
        public void OnCoolingTick(IWorldAccessor world, ItemStack stack, Vec3d pos, float targetTemperature)
        {
            if (worldQuenchProps is null || stack is null || world.Side != EnumAppSide.Server)
            {
                return;
            }
            if (GetState(stack) != StateQuench)
            {
                return;
            }
            long elapsed = world.ElapsedMilliseconds;
            stack.TempAttributes.SetDouble(CoolX, pos.X);
            stack.TempAttributes.SetDouble(CoolY, pos.Y);
            stack.TempAttributes.SetDouble(CoolZ, pos.Z);
            stack.TempAttributes.SetLong(LastCool, elapsed);
            float temperature = collObj.GetTemperature(world, stack);
            if (temperature <= worldQuenchProps.settledTemperature || targetTemperature >= temperature)
            {
                return;
            }
            if (elapsed - stack.TempAttributes.GetLong(ChainBeatMS, 0L) < 1200L)
            {
                return;
            }
            EnsureMigrated(world, stack);
            if (stack.Attributes.GetBool(OverStressed))
            {
                return;
            }
            int ourRollTolerance = GetRollQuality(world, stack);
            int stress = stack.Attributes.GetInt(CurrentStress);
            int newStress = stress + 10;
            TickChain chain = new TickChain
            {
                World = world,
                Stack = stack,
                StartTemp = temperature,
                From = Math.Min(1f, stress / (float)ourRollTolerance),
                To = Math.Min(1f, newStress / (float)ourRollTolerance),
                First = true
            };
            stack.TempAttributes.SetLong(ChainBeatMS, elapsed);
        }
        public void DebugForceQuench(IWorldAccessor world, ItemStack stack)
        {
            if (stack is null)
            {
                return;
            }
            EnsureMigrated(world, stack);
            ApplyQuench(world, stack);
            SetState(world, stack, StateSettled);
        }
        private void ApplyQuench(IWorldAccessor world, ItemStack stack)
        {
            if (world.Side != EnumAppSide.Server)
            {
                return;
            }
            quenchiestConfig conf = world.Api.ModLoader.GetModSystem<thequenchiestModSystem>().TQ_LoadedConfig;

            bool clayed = stack.Attributes.GetBool(ClayCovered);

            stack.Attributes.SetBool(ClayCovered, false);

            if (stack.Attributes.GetBool(OverStressed))
            {
                return;
            }
            int ourRollTolerance = GetRollQuality(world, stack);
            int newstress = stack.Attributes.GetInt(CurrentStress) + world.Rand.Next(0,6) + 7;
            int newquench = stack.Attributes.GetInt(QuenchCount) + 1;
            float statBonus = conf.AdditivePercentPerQuenchSuccess + ((conf.AdditivePercentPerQuenchSuccess * (newquench - 1)) / 2f);
            stack.Attributes.SetInt(CurrentStress, newstress);
            stack.Attributes.SetInt(QuenchCount, newquench);
            stack.Attributes.SetInt(QuenchIteration, newquench);
            if (clayed)
            {
                float DurBonus = statBonus * 1.6f;
                stack.Attributes.SetFloat(DurationBonus, stack.Attributes.GetFloat(DurationBonus) + DurBonus);
            }
            else
            {
                stack.Attributes.SetFloat(PowerValue, stack.Attributes.GetFloat(PowerValue) + statBonus);
            }
            if (newstress > ourRollTolerance)
            {
                Overstress(stack, conf.PercentStatsKeptOnOverstress);
                Vec3d pos = CoolingPos(stack);
                if (world is not null && world.Side == EnumAppSide.Server && pos != null)
                {
                    world.PlaySoundAt(SnapSound, pos.X, pos.Y, pos.Z, null, 1f, 42f, 1f);
                    world.SpawnCubeParticles(pos, stack, 1f, 38, 1.15f, null, new Vec3f(0f, 0.55f, 0f));
                }
                return;
            }
            ReapplyBuffs(stack);
        }
        private static Vec3d CoolingPos(ItemStack stack)
        {
            if (!stack.TempAttributes.HasAttribute(CoolX))
            {
                return null;
            }
            return new Vec3d(stack.TempAttributes.GetDouble(CoolX, 0.0), stack.TempAttributes.GetDouble(CoolY, 0.0), stack.TempAttributes.GetDouble(CoolZ, 0.0));
        }
        private void Overstress(ItemStack stack, float penalty)
        {
            stack.Attributes.SetFloat(PowerValue, stack.Attributes.GetFloat(PowerValue) * penalty);
            stack.Attributes.SetFloat(DurationBonus, stack.Attributes.GetFloat(DurationBonus) * penalty);
            stack.Attributes.SetBool(OverStressed, true);
            ReapplyBuffs(stack);
        }
        private int GetRollQuality(IWorldAccessor world, ItemStack stack)
        {
            int num = stack.Attributes.GetInt(StressTolerance);
            if (num > 0)
            {
                return num;
            }
            if (world.Side == EnumAppSide.Client) { return num; }
            quenchiestConfig conf = world.Api.ModLoader.GetModSystem<thequenchiestModSystem>().TQ_LoadedConfig;
            num = (int)Math.Floor(NatFloat.createGauss(conf.AvgStressTolerance, conf.StressToleranceVariance).nextFloat());
            stack.Attributes.SetInt(StressTolerance, num);
            return num;
        }
        public float GetStressRatio(IWorldAccessor world, ItemStack stacc)
        {
            //decides it likes returning 0, fuck you you're a FLOAT DIVIDE DAMMIT
            return (float)((float)stacc.Attributes.GetInt(CurrentStress) / (float)GetRollQuality(world, stacc));
        }
        private void ReapplyBuffs(ItemStack stack)
        {
            CollectibleBehaviorBuffable behavior = collObj.GetBehavior<CollectibleBehaviorBuffable>();
            if (behavior is null)
            {
                return;
            }
            float power = stack.Attributes.GetFloat(PowerValue);
            float duration = stack.Attributes.GetFloat(DurationBonus);
            List<AppliedCollectibleBuff> list = new List<AppliedCollectibleBuff>();
            if (power != 0f)
            {
                list.Add(new AppliedCollectibleBuff
                {
                    Code = "hardened",
                    Multiplier = 1f + power,
                    StatCode = "attackpower"
                });
                list.Add(new AppliedCollectibleBuff
                {
                    Code = "hardened",
                    Multiplier = 1f + power,
                    StatCode = "miningspeed"
                });
            }
            bool hasdura = duration != 0f;
            float num3 = 0f;
            if (hasdura)
            {
                num3 = (float)collObj.GetRemainingDurability(stack) / (float)collObj.GetMaxDurability(stack);
                list.Add(new AppliedCollectibleBuff
                {
                    Code = "hardened",
                    Multiplier = 1f + duration,
                    StatCode = "maxdurability"
                });
            }
            if (list.Count == 0)
            {
                return;
            }
            behavior.ApplyBuffs(stack, list, EnumBuffAddType.ReplaceOnDuplicate);
            if (hasdura)
            {
                collObj.SetDurability(stack, (int)(num3 * collObj.GetMaxDurability(stack)));
            }
        }
        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            if (!TryRingOnAnvil(slot, byEntity, blockSel))
            {
                return;
            }
            handHandling = EnumHandHandling.PreventDefault;
            handling = EnumHandling.Handled;
        }
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            if (!firstEvent)
            {
                return;
            }
            if (!TryRingOnAnvil(slot, byEntity, blockSel))
            {
                return;
            }
            handHandling = EnumHandHandling.PreventDefault;
            handling = EnumHandling.Handled;
        }

        private bool TryRingOnAnvil(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel)
        {
            if (worldQuenchProps == null || blockSel == null)
            {
                return false;
            }
            if (slot == null || slot.Itemstack == null)
            {
                return false;
            }
            if (byEntity == null || byEntity.World == null)
            {
                return false;
            }
            IWorldAccessor world = byEntity.World;
            if (!(world.BlockAccessor.GetBlock(blockSel.Position) is BlockAnvil))
            {
                return false;
            }
            long elapsedMilliseconds = world.ElapsedMilliseconds;
            long last = slot.Itemstack.TempAttributes.GetLong(LastRing);
            if (elapsedMilliseconds - last < 800L)
            {
                return true;
            }
            slot.Itemstack.TempAttributes.SetLong(LastRing, elapsedMilliseconds);
            if (world.Side == EnumAppSide.Server)
            {
                EnsureMigrated(world, slot.Itemstack);
                StolenRingSounds.PlayRing(world, slot.Itemstack, blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5));
            }
            return true;
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
        {
            if (worldQuenchProps is null)
            {
                return null;
            }
            return new WorldInteraction[]
            {
                new WorldInteraction
                {
                    ActionLangCode = "thequenchiest:heldhelp-ring",
                    MouseButton = EnumMouseButton.Right,
                    ShouldApply = delegate(WorldInteraction wi, BlockSelection blockSel, EntitySelection entitySel)
                    {
                        if (blockSel != null)
                        {
                            return blockSel.Block is BlockAnvil;
                        }
                        return false;
                    }
                }
            };
        }
        private void EnsureMigrated(IWorldAccessor world, ItemStack stack)
        {
            if (world.Side != EnumAppSide.Server)
            {
                return;
            }
            if (stack.Attributes.HasAttribute(QuenchCount))
            {
                return;
            }
            ITreeAttribute atts = stack.Attributes;
            if (atts is null)
            {
                return;
            }
            if ((atts.GetInt(QuenchIteration, 0) > 0 || atts.HasAttribute("temperIteration") || atts.HasAttribute(ShatterChance)) && collObj.GetBehavior<CollectibleBehaviorBuffable>() is null)
            {
                atts.RemoveAttribute(PowerValue);
                atts.RemoveAttribute(DurationBonus);
                atts.RemoveAttribute(QuenchIteration);
            }
            if (atts.GetString(State, null) == StateTemper)
            {
                atts.RemoveAttribute(State);
            }
            atts.RemoveAttribute("temperIteration");
            atts.RemoveAttribute(ShatterChance);
            atts.RemoveAttribute(LastInTemperH);
            atts.SetInt(QuenchCount, 0);
            atts.SetInt(CurrentStress, 0);
        }
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            if (worldQuenchProps is null)
            {
                return;
            }
            ItemStack itemstack = inSlot.Itemstack;
            if (itemstack is null)
            {
                return;
            }
            bool overstressed = itemstack.Attributes.GetBool(OverStressed);
            int num = itemstack.Attributes.GetInt(QuenchCount);
            float num2 = itemstack.Attributes.GetFloat(PowerValue);
            float num3 = itemstack.Attributes.GetFloat(DurationBonus);
            if (!overstressed)
            {
                dsc.AppendLine(Lang.Get("game:itemstack-quenchable", worldQuenchProps.quenchMinTemp, worldQuenchProps.quenchMaxTemp));
            }
            if (itemstack.Attributes.GetBool(ClayCovered))
            {
                dsc.AppendLine(Lang.Get("game:itemstack-claycovered", Array.Empty<object>()));
            }
            if (num > 0)
            {
                dsc.AppendLine(Lang.Get("game:quenchable-quenched-amount", new object[] { num }));
            }
            if (num2 != 0f)
            {
                dsc.AppendLine(Lang.Get("game:quenchable-power-gain", new object[] { num2 }));
            }
            if (num3 != 0f)
            {
                dsc.AppendLine(Lang.Get("game:quenchable-durability-gain", new object[] { num3 }));
            }
            if (overstressed)
            {
                dsc.AppendLine(Lang.Get("thequenchiest:overstressed", Array.Empty<object>()));
            }
            else if (GetState(itemstack) == StateQuench)
            {
                dsc.AppendLine(Lang.Get("quenchable-reached-quenching-temperature", Array.Empty<object>()));
            }
            /*
            if (withDebugInfo)
            {
                int num4 = GetRollQuality(world, itemstack);
                dsc.AppendLine(string.Concat(
                    "<font color=\"#8ab4d8\">[Debug] Stress ",
                    itemstack.Attributes.GetInt(CurrentStress),
                    " / ",
                    (num4 > 0) ? num4.ToString() : "unrolled",
                    (num4 > 0) ? ("  (" + Math.Floor(num4 / 10f).ToString() + " safe quenches)") : "",
                    "</font>"
                ));
            }
            */
        }
    }
}
