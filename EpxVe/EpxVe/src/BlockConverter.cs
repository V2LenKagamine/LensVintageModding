using ElectricalProgressive.Utils;
using VintageEngineering.Electrical;
using Vintagestory.API.Common;

namespace EpxVe.src
{
    public class BlockConverter : ElectricBlock
    {
        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            if (
                base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack) &&
                world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEConverter entity
            )
            {
                Selection sel = new(blockSel);
                entity.epfacing = FacingHelper.From(sel.Face,sel.Direction);
            }

            return base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
        }
    }
}
