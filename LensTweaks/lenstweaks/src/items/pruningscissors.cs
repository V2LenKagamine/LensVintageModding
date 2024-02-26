using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace LensstoryMod
{
    public class PruningScissors : Item
    {
        public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1)
        {
            if(api.Side == EnumAppSide.Client) { return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier); }
            if (blockSel.Block.FirstCodePart() == "leavesbranchy" || blockSel.Block.FirstCodePart() == "leaves")
            {
                var treetype = blockSel.Block.FirstCodePart(2);
                var maybeseed = world.GetBlock(new AssetLocation("game:sapling-" + treetype + "-free"));
                if (maybeseed != null)
                {
                    api.World.SpawnItemEntity(new(maybeseed, 1),blockSel.Position.ToVec3d().Add(0.5f,0.5f,0.5f));
                    DamageItem(world,byEntity,itemslot);
                }
            }
            return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);
        }
    }
}
