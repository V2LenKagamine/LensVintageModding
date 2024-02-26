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
        private bool Powered;
        private double storedDura;
        public bool Working
        {
            get => Powered; set
            {
                if (Powered != value)
                {
                    if (value && !Powered)
                    {
                        MarkDirty();
                    }
                    Powered = value;
                };
            }
        }
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            contents?.ResolveBlockOrItem(api.World);

            RegisterGameTickListener(OnCommonTick, 1000);

            GetBehavior<Mana>().begin(true);
        }

        internal void OnCommonTick(float dt)
        {
            if (Working)
            {
                var hourspast = Api.World.Calendar.TotalHours - LastTickTotalHours;
                storedDura = storedDura < 10 ? storedDura + hourspast * 5 : storedDura = 10;
                if(storedDura >=1 && contents != null)
                {
                    if (contents.Attributes?.GetInt("durability") < contents.Collectible?.Durability)
                    {
                        var dura = contents.Collectible?.Durability;
                        if (dura != null)
                        {
                            int percentDura = (int)Math.Floor((double)dura * 0.01);
                            int torepair = (int)Math.Floor(storedDura);

                            var newdura = Math.Min(contents.Attributes.GetInt("durability") + (torepair * percentDura) + 1, contents.Collectible.Durability);
                            contents.Attributes.SetInt("durability", newdura);
                            storedDura -= torepair;
                            MarkDirty();

                        }
                    }
                }
            }
            LastTickTotalHours = Api.World.Calendar.TotalHours;
        }
        internal bool OnPlayerInteract(IWorldAccessor world, IPlayer player, BlockSelection blockSel)
        {

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
            var slot = player.InventoryManager.ActiveHotbarSlot;
            if (slot.Itemstack == null)
            { return false; }
            var maybeitem = slot.Itemstack.Collectible;
            if (maybeitem != null)
            {
                if (slot.Itemstack.Attributes.GetInt("durability") < maybeitem.Durability && contents == null)
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
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            LastTickTotalHours = tree.GetDouble("LastTick");
            Working = tree.GetBool("powered");
            storedDura = tree.GetDouble("storeddura");
            contents = tree.GetItemstack("contents");
            contents?.ResolveBlockOrItem(worldAccessForResolve) ;

        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetDouble("LastTick", LastTickTotalHours);
            tree.SetBool("powered", Powered);
            tree.SetDouble("storeddura", storedDura);
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
    public class ManaRepairBhv : BlockEntityBehavior, IManaConsumer
    {
        public ManaRepairBhv(BlockEntity blockentity) : base(blockentity)
        {
        }

        public int ToVoid() {
            if (Blockentity is ManaRepairBE entity)
            {
                return entity.contents != null ? entity.contents.StackSize : 0;
            } return 0;
        }

        public void EatMana(int mana)
        {
            if(Blockentity is ManaRepairBE entity)
            {
                entity.Working = mana == ToVoid();
            }
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);

            dsc.AppendLine("MP: ")
                .AppendLine("Consumes: " + ToVoid());
        }
    }
}
