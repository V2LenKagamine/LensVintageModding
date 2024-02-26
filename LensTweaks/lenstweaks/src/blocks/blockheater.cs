using System;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using LensstoryMod.HarmonyEx;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace LensstoryMod
{
    public class HeaterBlock : Block, IIgnitable
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is HeaterBE entity)
            {
                return entity.OnInteract(world, byPlayer, blockSel);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
        public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
        {
            HeaterBE babs = byEntity.World.BlockAccessor.GetBlockEntity(pos) as HeaterBE;
            if (!babs.CanIgnite()) { return EnumIgniteState.NotIgnitablePreventDefault; }
            return secondsIgniting > 2 ? EnumIgniteState.IgniteNow : EnumIgniteState.Ignitable;
        }

        public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventDefault;

            HeaterBE babs = byEntity.World.BlockAccessor.GetBlockEntity(pos) as HeaterBE;
            babs?.TryIgnite();
        }

        public EnumIgniteState OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
        {
            return EnumIgniteState.NotIgnitable;
        }
    }

    public class HeaterBE : BlockEntity, IHeatSource
    {
        bool burning;

        internal InventoryGeneric contents;

        ItemSlot FuelSlot { get { return contents[0]; } }
        int maxtemp { get { return FuelSlot?.Itemstack?.Collectible?.CombustibleProps?.BurnTemperature != null ? Math.Min(350,FuelSlot.Itemstack.Collectible.CombustibleProps.BurnTemperature) : GetEnvTemp(); } }
        float temperature;
        float fuelleft;
        public bool isBurning { get { return burning; } }

        public HeaterBE()
        {
            contents = new InventoryGeneric(1, "lensheatblock-1", null, null);
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            contents.LateInitialize("lensheatblock-1", api);

            RegisterGameTickListener(OnCommonTick, 1000);
        }

        public void OnCommonTick(float dt)
        {
            if (Api.Side == EnumAppSide.Server)
            {
                if(fuelleft>0)
                {
                    fuelleft -= dt;
                    if(fuelleft <=0)
                    {
                        fuelleft = 0;
                        burning = false;
                        FuelSlot.TakeOutWhole();
                        MarkDirty();
                    }
                }
                if (isBurning) { temperature = ChangeTemp(temperature, maxtemp, dt / 24); }
                else
                {
                    int envTemp = GetEnvTemp();
                    if (temperature > envTemp)
                    {
                        temperature = ChangeTemp(temperature, envTemp, dt / 24);
                    }
                }
                if (temperature > 0)
                {
                    HeatAbove(dt);
                }
            }
        }

        public bool OnInteract(IWorldAccessor world, IPlayer player, BlockSelection blockSel)
        {
            if(Api.Side == EnumAppSide.Server)
            {
                ItemSlot slot = player.InventoryManager.ActiveHotbarSlot;
                if ((FuelSlot.Empty || FuelSlot.StackSize < FuelSlot.Itemstack?.Collectible.MaxStackSize) && slot.Itemstack?.Collectible?.CombustibleProps?.BurnDuration != null && !isBurning)
                {
                    int moved = 0;
                    if (FuelSlot.Empty)
                    {
                        moved = slot.TryPutInto(Api.World, FuelSlot);
                    }
                    else if (FuelSlot.Itemstack.Collectible == slot.Itemstack?.Collectible)
                    {
                        moved = slot.TryPutInto(Api.World,FuelSlot);
                    }
                    if(moved>0)
                    {
                        MarkDirty();
                    }
                    return moved > 0;
                }
                if (!burning && player.Entity.Controls.ShiftKey && slot.Itemstack == null && !FuelSlot.Empty)
                {
                    int moved = FuelSlot.TryPutInto(Api.World, slot, FuelSlot.StackSize);
                    if(moved > 0) { MarkDirty(); }
                    return moved > 0;
                }
            }
            return true;
        }

        protected void HeatAbove(float dt)
        {
            if(Api.World.BlockAccessor.GetBlockEntity(Pos.UpCopy()) is BlockEntityOven oven)
            {
                oven.ovenTemperature = oven.ChangeTemperature(oven.ovenTemperature, temperature, dt);
            }
            if (Api.ModLoader.IsModEnabled("aculinaryartillery"))
            {
                BlockEntity maybeoven = Api.World.BlockAccessor.GetBlockEntity(Pos.UpCopy());
                Assembly assembly = Assembly.Load("ACulinaryArtillery");
                if (assembly != null && maybeoven != null)
                {
                    Type oventype = assembly.GetClassType("BlockEntityExpandedOven");
                    if(maybeoven.GetType() == oventype)
                    {
                        var temp = AccessTools.Method(oventype, "ChangeTemperature").Invoke(maybeoven, new float[] { maybeoven.GetField<float>("ovenTemperature"),temperature,dt }.Cast<object>().ToArray());
                        maybeoven.SetField("ovenTemperature", temp);
                    }
                }
            }
        }
        protected virtual int GetEnvTemp()
        {
            float temperature = Api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, Api.World.Calendar.TotalDays).Temperature;
            return (int)temperature;
        }
        public virtual float ChangeTemp(float fromTemp, float toTemp, float dt)
        {
            float diff = Math.Abs(fromTemp - toTemp);
            diff *= GameMath.Sqrt(diff);
            dt += dt * (diff / 45);
            if (diff < dt)
            {
                return toTemp;
            }
            if (fromTemp > toTemp)
            {
                dt = -dt / 5f;
            }
            if (Math.Abs(fromTemp - toTemp) < 1)
            {
                return toTemp;
            }
            MarkDirty();
            return fromTemp + dt;
        }
        public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos)
        {
            return isBurning ? 7 : 0;
        }

        public bool TryIgnite()
        {
            if (burning || !CanIgnite()) { return false; }

            burning = true;
            fuelleft = FuelSlot.Itemstack.Collectible.CombustibleProps.BurnDuration * FuelSlot.Itemstack.StackSize;
            MarkDirty();
            return true;

        }
        public bool CanIgnite()
        {
            return !burning && !FuelSlot.Empty;
        }
        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            base.OnBlockBroken(byPlayer);
            if(!contents.Empty) { Api.World.SpawnItemEntity(FuelSlot.Itemstack,Pos.ToVec3d().Add(0.5,0.5,0.5)); }
            
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            burning = tree.GetBool("burning");
            contents.FromTreeAttributes(tree);
            temperature = tree.GetFloat("temperature");
            fuelleft = tree.GetFloat("fuelleft");
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("burning", burning);
            contents.ToTreeAttributes(tree);
            tree.SetFloat("temperature", temperature);
            tree.SetFloat("fuelleft", fuelleft);
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if(burning) { dsc.AppendLine("A fire burns inside."); }
            if(temperature <=25)
            {
                dsc.AppendLine("Temperature: Cold");
            }else
            {
                dsc.AppendLine("Temperature: " + (int)temperature + "C");
                if(temperature < 150 && !burning)
                {
                    dsc.AppendLine("It's getting colder.");
                }
            }
            if (!FuelSlot.Empty)
            {
                dsc.AppendLine("Contains: "+FuelSlot.Itemstack?.StackSize + "x " + FuelSlot.Itemstack?.GetName()); 
            }
        }

    }

}
