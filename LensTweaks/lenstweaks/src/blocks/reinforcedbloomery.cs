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
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using static System.Formats.Asn1.AsnWriter;

namespace LensstoryMod
{
    public class ReinforcedBloomery : Block, IIgnitable
    {
        WorldInteraction[] interactions;
        //Stolen from BlockBloomery.cs
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            if (api.Side != EnumAppSide.Client) return;
            ICoreClientAPI capi = api as ICoreClientAPI;

            interactions = ObjectCacheUtil.GetOrCreate(api, "reinfbloomeryBlockInteractions", () =>
            {
                List<ItemStack> heatableStacklist = new List<ItemStack>();
                List<ItemStack> fuelStacklist = new List<ItemStack>();
                List<ItemStack> canIgniteStacks = BlockBehaviorCanIgnite.CanIgniteStacks(api, false);

                foreach (CollectibleObject obj in api.World.Collectibles)
                {
                    if (obj.CombustibleProps == null) continue;
                    if (obj.CombustibleProps.SmeltedStack != null && obj.CombustibleProps.MeltingPoint < 2000)
                    {
                        List<ItemStack> stacks = obj.GetHandBookStacks(capi);
                        if (stacks != null) heatableStacklist.AddRange(stacks);
                    }
                    else
                    {
                        if (obj.CombustibleProps.BurnTemperature >= 1200 && obj.CombustibleProps.BurnDuration > 30)
                        {
                            List<ItemStack> stacks = obj.GetHandBookStacks(capi);
                            if (stacks != null) fuelStacklist.AddRange(stacks);
                        }
                    }
                }

                return new WorldInteraction[] {
                    new WorldInteraction()
                    {
                        ActionLangCode = "Insert Burnable",
                        HotKeyCode = null,
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = heatableStacklist.ToArray(),
                        GetMatchingStacks = getMatchingStacks
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "Insert Burnable x5",
                        HotKeyCode = "ctrl",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = heatableStacklist.ToArray(),
                        GetMatchingStacks = getMatchingStacks
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "Insert Fuel",
                        HotKeyCode = null,
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = fuelStacklist.ToArray(),
                        GetMatchingStacks = getMatchingStacks
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "Ignite",
                        HotKeyCode = "shift",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = canIgniteStacks.ToArray(),
                        GetMatchingStacks = (wi, bs, es) => {
                            ReinforcedBloomeryBE beb = api.World.BlockAccessor.GetBlockEntity(bs.Position) as ReinforcedBloomeryBE;
                            if (beb!= null && beb.CanIgnite() == true && !beb.isBurning)
                            {
                                return wi.Itemstacks;
                            }
                            return null;
                        }
                    }
                };
            });
        }
        private ItemStack[] getMatchingStacks(WorldInteraction wi, BlockSelection blockSelection, EntitySelection entitySelection)
        {
            ReinforcedBloomeryBE beb = api.World.BlockAccessor.GetBlockEntity(blockSelection.Position) as ReinforcedBloomeryBE;
            if (beb == null || wi.Itemstacks.Length == 0) return null;

            List<ItemStack> matchStacks = new List<ItemStack>();
            foreach (ItemStack stack in wi.Itemstacks)
            {
                if (beb.CanAdd(stack)) matchStacks.Add(stack);
            }

            return matchStacks.ToArray();
        }
        public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
        {
            ReinforcedBloomeryBE babs = byEntity.World.BlockAccessor.GetBlockEntity(pos) as ReinforcedBloomeryBE;
            if(!babs.CanIgnite()) { return EnumIgniteState.NotIgnitablePreventDefault; }
            return secondsIgniting > 4 ? EnumIgniteState.IgniteNow : EnumIgniteState.Ignitable;
        }

        public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventDefault;

            ReinforcedBloomeryBE babs = byEntity.World.BlockAccessor.GetBlockEntity(pos) as ReinforcedBloomeryBE;
            babs?.TryIgnite();
        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is ReinforcedBloomeryBE entity)
            {
                return entity.OnInteract(world,byPlayer,blockSel);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public EnumIgniteState OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
        {
            return EnumIgniteState.NotIgnitable;
        }
        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
    }

    public class ReinforcedBloomeryBE : BlockEntity, IHeatSource
    {
        bool burning;
        internal InventoryGeneric contents;
        double burningUntil;
        double burningStarted;

        public bool isBurning { get {  return burning; }}

        int InputCap { get { if (InputSlot.Itemstack?.Collectible.CombustibleProps == null) { return 6; }
                return InputSlot.Itemstack.Collectible.CombustibleProps.SmeltedRatio * 4; } }
        float Fuel2Input { get
            {
                if (InputSlot.Itemstack?.Collectible.CombustibleProps == null || FuelSlot.Itemstack?.Collectible.CombustibleProps == null) { return 1; }
                return 4f / InputCap;
            } }

        ItemSlot FuelSlot { get { return contents[0]; } }
        ItemSlot InputSlot { get { return contents[1]; } }
        ItemSlot OutputSlot { get { return contents[2]; } }

        static SimpleParticleProperties smoke;

        public ReinforcedBloomeryBE()
        {
            contents = new InventoryGeneric(3,"reinforcedbloomery-1",null,null);
            smoke = new SimpleParticleProperties(
                1, 1, ColorUtil.ToRgba(128, 110, 110, 110), new Vec3d(), new Vec3d(),
                new Vec3f(-0.2f, 0.3f, -0.2f), new Vec3f(0.2f, 0.3f, 0.2f), 2, 0, 0.5f, 1f, EnumParticleModel.Quad
            );
            smoke.SelfPropelled = true;
            smoke.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -255);
            smoke.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, 2);
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            contents.LateInitialize("reinforcedbloomery-1", api);

            RegisterGameTickListener(OnCommonTick, 1000);
            RegisterGameTickListener(OnClientTick, 100);
        }

        private void OnCommonTick(float dt)
        {
            if (!burning) { return; }

            if (Api.Side == EnumAppSide.Server && burningUntil < Api.World.Calendar.TotalDays)
            {
                Smelt();
            }
        }

        private void OnClientTick(float dt)
        {
            if(!burning) { return; }
            if (Api.Side == EnumAppSide.Client)
            {
                if (Api.World.Rand.Next(2) != 0)
                {
                    smoke.MinPos.Set(Pos.X + 0.5 - 2 / 16.0, Pos.Y + 0.1 + 10 / 16f, Pos.Z + 0.5 - 2 / 16.0);
                    smoke.AddPos.Set(4 / 16.0, 0, 4 / 16.0);
                    Api.World.SpawnParticles(smoke, null);
                }
            }
        }

        public bool OnInteract(IWorldAccessor world,IPlayer player,BlockSelection blockSel)
        {
            ItemStack stacc = player.InventoryManager.ActiveHotbarSlot.Itemstack;
            if (OutputSlot.Itemstack == null && !player.Entity.Controls.ShiftKey)
            {
                if (stacc == null) { return true; }
                TryAdd(player, player.Entity.Controls.CtrlKey ? 5 : 1);
                return true;
            }
            if (player.Entity.Controls.ShiftKey && OutputSlot.Itemstack != null)
            {
                if(!player.InventoryManager.TryGiveItemstack(OutputSlot.Itemstack))
                {
                    world.SpawnItemEntity(OutputSlot.Itemstack, Pos.ToVec3d().Add(0.5, 1.5, 0.5));
                }
                OutputSlot.TakeOutWhole();
                MarkDirty();
                return true;
            }
            return true;
        }

        private void Smelt()
        {
            ItemStack input = InputSlot.Itemstack;
            if(input.Collectible.CombustibleProps == null) { return; }
            int amt = input.StackSize / input.Collectible.CombustibleProps.SmeltedRatio;
            if(input.Collectible.Attributes?.IsTrue("mergeUnitsInBloomery")== true)
            {
                OutputSlot.Itemstack = input.Collectible.CombustibleProps.SmeltedStack.ResolvedItemstack.Clone();
                OutputSlot.Itemstack.StackSize = 1;

                float floatamt = (float)input.StackSize / input.Collectible.CombustibleProps.SmeltedRatio;
                OutputSlot.Itemstack.Attributes.SetFloat("units", floatamt * 100);
            }else
            {
                OutputSlot.Itemstack = input.Collectible.CombustibleProps.SmeltedStack.ResolvedItemstack.Clone();
                OutputSlot.Itemstack.StackSize *= amt;
            }
            OutputSlot.Itemstack.Collectible.SetTemperature(Api.World, OutputSlot.Itemstack, 1100, true);
            FuelSlot.Itemstack = null;
            input.StackSize -= amt * input.Collectible.CombustibleProps.SmeltedRatio;
            if(input.StackSize <= 0) { InputSlot.Itemstack = null; }

            burning = false;
            burningUntil = 0;
            MarkDirty();
        }

        public bool CanAdd(ItemStack stacc,int amt = 1)
        {
            if(isBurning || OutputSlot.StackSize > 0 || stacc == null) { return false; }

            CollectibleObject coll = stacc.Collectible;

            if(coll.CombustibleProps?.SmeltedStack != null && coll.CombustibleProps.MeltingPoint < 2000 && coll.CombustibleProps.MeltingPoint >= 1000)
            {
                if(InputSlot.StackSize + amt > InputCap) { return false; }
                if(!InputSlot.Empty && FuelSlot.Itemstack?.Equals(Api.World,stacc,GlobalConstants.IgnoredStackAttributes) != true) { return false; }
                return true;
            }
            if(coll.CombustibleProps?.BurnTemperature >=1200 && coll.CombustibleProps.BurnDuration > 30)
            {
                if(FuelSlot.StackSize + amt > 4) { return false; }
                if(!FuelSlot.Empty && !FuelSlot.Itemstack.Equals(Api.World,stacc,GlobalConstants.IgnoredStackAttributes)) { return false; }
                return true;
            }
            return false;
        }

        public bool TryAdd(IPlayer player,int amt = 1) 
        {
            ItemSlot slot = player.InventoryManager.ActiveHotbarSlot;

            if(isBurning || OutputSlot.StackSize > 0 || slot == null) { return false; }

            CollectibleObject coll = slot.Itemstack.Collectible;

            if(coll.CombustibleProps?.SmeltedStack != null && coll.CombustibleProps.MeltingPoint < 2000 && coll.CombustibleProps.MeltingPoint >=1000)
            {
                if(InputSlot.StackSize >= InputCap) { return false;}
                int movable = Math.Min ( InputCap -  InputSlot.StackSize, amt);

                int storeamt = slot.StackSize;
                slot.TryPutInto(Api.World, InputSlot, movable);
                MarkDirty();

                return storeamt != slot.StackSize;
            }
            if (coll.CombustibleProps?.BurnTemperature >= 1200 && coll.CombustibleProps.BurnDuration > 30 && (float)FuelSlot.StackSize / InputSlot.StackSize < Fuel2Input)
            {
                if (FuelSlot.StackSize + amt > 16) {  return false; }
                int maxneeded = (int)Math.Ceiling(InputSlot.StackSize * Fuel2Input);
                int missing = maxneeded - FuelSlot.StackSize;
                int movable = Math.Min(missing, Math.Min(16 - FuelSlot.StackSize, amt));

                int lastsize = slot.StackSize;
                slot.TryPutInto(Api.World, FuelSlot, movable);
                MarkDirty();

                return lastsize != slot.StackSize;
            }
            return true;
        }

        public bool TryIgnite()
        {
            if (burning || !CanIgnite()) { return false; }

            burning = true;
            burningUntil = Api.World.Calendar.TotalDays + (10f/24f);
            burningStarted = Api.World.Calendar.TotalDays;
            MarkDirty();
            return true;

        }
        public bool CanIgnite() 
        {
            return !burning && FuelSlot.StackSize > 0 && InputSlot.StackSize > 0 && (float)FuelSlot.StackSize / InputSlot.StackSize >= Fuel2Input;
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            contents.ToTreeAttributes(tree);
            tree.SetBool("burning", burning);
            tree.SetDouble("burningUntil", burningUntil);
            tree.SetDouble("burningStarted", burningStarted);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            contents.FromTreeAttributes(tree);
            burning = tree.GetBool("burning");
            burningUntil = tree.GetDouble("burningUntil");
            burningStarted = tree.GetDouble("burningStarted");
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            if (Api.World.EntityDebugMode || forPlayer?.WorldData?.CurrentGameMode == EnumGameMode.Creative)
            {
                dsc.AppendLine(string.Format("Burning: {3}, Current total days: {0}, BurningStart total days: {1}, BurningUntil total days: {2}", Api.World.Calendar.TotalDays, burningStarted, burningUntil, burning));
            }
            else if(burning)
            {
                dsc.AppendLine("It appears to be on fire.");
            }
            dsc.AppendLine(Lang.Get("Contents:"));
            for (int i = 0; i < contents.Count; i++)
            {
                ItemStack stacc = contents[i].Itemstack;
                if(stacc != null)
                {
                    
                    dsc.AppendLine("  " + stacc.StackSize + "x "+stacc.GetName());
                }
            }
            base.GetBlockInfo(forPlayer, dsc);
        }

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            contents.DropAll(Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            base.OnBlockBroken(byPlayer);
        }

        public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos)
        {
            return isBurning ? 7 : 0 ;
        }
    }
}
