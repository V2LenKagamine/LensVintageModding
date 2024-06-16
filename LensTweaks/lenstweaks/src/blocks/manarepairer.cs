using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace LensstoryMod
{
    public class ManaRepairBlock : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is ManaRepairBE entity)
            {
                return entity.OnPlayerInteract(world,byPlayer,blockSel);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
    public class ManaRepairBE : BlockEntity
    {
        public ItemStack? contents { get; private set; }

        private double LastTickTotalHours;
        private int fuel;
        public int Fuel
        {
            get => fuel; set
            {
                if (fuel != value)
                {
                    fuel = value;
                    MarkDirty();
                };
            }
        }
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            contents?.ResolveBlockOrItem(api.World);

            RegisterGameTickListener(OnCommonTick, 750);
        }

        internal void OnCommonTick(float dt)
        {
            if (Fuel > 0 && contents != null)
            {
                var hourspast = Api.World.Calendar.TotalHours - LastTickTotalHours;
                int? itemdura = contents.Attributes?.TryGetInt("durability");
                if ( itemdura != null && itemdura < contents.Collectible?.Durability)
                {
                    var dura = contents.Collectible?.Durability;
                    if (dura != null)
                    {
                        int torepair = Math.Min((int)Math.Floor(150 * hourspast),Fuel);
                        var newdura = Math.Min(contents.Attributes.GetInt("durability") + torepair, contents.Collectible.Durability);
                        contents.Attributes.SetInt("durability", newdura);
                        Fuel -= torepair;
                        MarkDirty();

                    }
                }
            }
            LastTickTotalHours = Api.World.Calendar.TotalHours;
        }
        internal bool OnPlayerInteract(IWorldAccessor world, IPlayer player, BlockSelection blockSel)
        {
            var slot = player.InventoryManager.ActiveHotbarSlot;
            if (player.Entity.Controls.ShiftKey)
            {

                if (contents == null) { return false; }
                var split = contents.Clone();
                split.StackSize = 1;
                contents.StackSize--;
                if (contents.StackSize <= 0)
                {
                    contents = null;
                }
                if (!player.InventoryManager.TryGiveItemstack(split))
                {
                    world.SpawnItemEntity(split, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }
                MarkDirty();
                return true;
            }
            if (slot.Itemstack == null)
            { return false; }
            if (slot.Itemstack.Item.FirstCodePart() == "gear")
            {
                int fueltoadd = 0;
                switch (slot.Itemstack.Item.LastCodePart())
                {
                    case "temporal":
                        {
                            fueltoadd = 1500;
                            break;
                        }
                    case "rusty":
                        {
                            fueltoadd = 50;
                            break;
                        }
                    default:
                        {
                            return false;
                        }
                }
                if (Fuel + fueltoadd > 3000) { return false; }
                Fuel += fueltoadd;
                slot.TakeOut(1);
                slot.MarkDirty();
                return true;
            }
            var maybeitem = slot.Itemstack.Collectible;
            if (maybeitem != null)
            {
                int? slotdura = slot.Itemstack.Attributes.TryGetInt("durability");
                if (slotdura != null && slotdura < maybeitem.Durability && contents == null)
                {
                    contents = slot.Itemstack.Clone();
                    contents.StackSize = 1;

                    slot.TakeOut(1);
                    slot.MarkDirty();
                    MarkDirty();

                    return true;
                }
            }
            return false;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (contents != null)
            {
                var durabilityleft = contents?.Collectible?.Durability - contents?.Attributes?.GetInt("durability");
                dsc.AppendLine($"\nContents: {contents?.GetName()},missing {durabilityleft} durability.");
            }
            dsc.AppendLine($"\nFuel: " + Fuel);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            Fuel = tree.GetInt("Fuel");
            LastTickTotalHours = tree.GetDouble("LastTick");
            contents = tree.GetItemstack("contents");
            contents?.ResolveBlockOrItem(worldAccessForResolve);

        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("Fuel", Fuel);
            tree.SetDouble("LastTick", LastTickTotalHours);
            tree.SetItemstack("contents", contents);
        }
        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            if (contents != null)
            {
                Api.World.SpawnItemEntity(contents, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }
            base.OnBlockBroken(byPlayer);
        }
    }
}
