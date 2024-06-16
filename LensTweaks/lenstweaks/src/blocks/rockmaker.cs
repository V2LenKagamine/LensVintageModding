using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace LensstoryMod
{
    public class RockmakerBlock : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is RockmakerBE entity)
            {
                return entity.OnPlayerInteract(world,byPlayer,blockSel);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
    public class RockmakerBE : BlockEntity
    {
        public ItemStack? contents { get; private set; }
        public double LastTickTotalHours;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            contents?.ResolveBlockOrItem(api.World);
           
            RegisterGameTickListener(OnCommonTick, 1000);
        }
        internal void OnCommonTick(float dt)
        {
            if (contents != null)
            {
                IBlockAccessor ba = Api.World.BlockAccessor;
                if (ba.GetBlock(Pos.UpCopy()).Id == 0 && ba.GetBlock(Pos.DownCopy()).FirstCodePart(1) == "basalt")
                {
                    ba.SetBlock(contents.Id, Pos.UpCopy());
                    ba.SetBlock(0, Pos.DownCopy());
                }
            }
        }

        internal bool OnPlayerInteract(IWorldAccessor world,IPlayer player,BlockSelection blocksel) 
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
            var maybeblock = slot.Itemstack.Collectible;
            var type = maybeblock.FirstCodePart();
            if (maybeblock != null && (type == "rock" || type == "gravel" || type == "sand" || type == "soil" || type == "cobblestone" || type == "rockpolished") && contents == null) 
            {
                contents = slot.Itemstack.Clone();
                contents.StackSize = 1;
                slot.TakeOut(1);
                slot.MarkDirty();
                MarkDirty();
                return true;
            }
            return false;
        }
        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            if(contents!=null)
            {
                Api.World.SpawnItemEntity(contents, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }
            base.OnBlockBroken(byPlayer);
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (contents != null)
            {
                dsc.AppendLine($"\nContents: {contents?.GetName()}");
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            contents = tree.GetItemstack("contents");
            contents?.ResolveBlockOrItem(worldAccessForResolve);
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetItemstack("contents", contents);
        }

    }
}
