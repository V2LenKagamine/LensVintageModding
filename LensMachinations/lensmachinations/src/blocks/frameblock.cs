using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace LensstoryMod
{
    public class WoodFrame : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if(slot.Itemstack != null && slot.Itemstack.Collectible is BlockLiquidContainerBase container)
            {
                ItemStack fluid = container.GetContent(slot.Itemstack);
                if (fluid!=null && fluid.Collectible?.Code == AssetLocation.Create("lensstory:concreteportion"))
                {
                    if (fluid.StackSize >= 10)
                    {
                        if (world.Side == EnumAppSide.Client) { return true; }
                        container.TryTakeLiquid(slot.Itemstack, 0.1f);
                        world.BlockAccessor.SetBlock(api.World.GetBlock(AssetLocation.Create("lensstory:concretepath-free")).Id,blockSel.Position);
                        slot.MarkDirty();
                    }
                }
            }
            else if (byPlayer.Entity.Controls.Sneak)
            {
                if (!byPlayer.InventoryManager.TryGiveItemstack(new ItemStack(api.World.GetBlock(AssetLocation.Create("lensstory:frame"))))) 
                {
                    api.World.SpawnItemEntity(new ItemStack(api.World.GetBlock(AssetLocation.Create("lensstory:frame"))),blockSel.Position.ToVec3d().Add(0.5,0.5,0.5));
                }
                world.BlockAccessor.SetBlock(0, blockSel.Position);
            }
            return true;
        }
    }
}
