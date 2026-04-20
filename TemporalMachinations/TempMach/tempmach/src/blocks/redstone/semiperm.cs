using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace TempMach
{
    public class SemiPermBlock : Block
    {
        public static Cuboidf[] sorrynothing = Array.Empty<Cuboidf>();
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is SemiPermBE entity && (byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack?.Block != null || byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack == null))
            {
                return entity.TryCamo(world, byPlayer, blockSel);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            if(blockAccessor.GetBlockEntity(pos) is SemiPermBE ent && (!ent.IsTangible))
            {
                return sorrynothing;
            }
            return base.GetCollisionBoxes(blockAccessor, pos);
        }
    }
    public class SemiPermBE : CamoableBE
    {
        public bool IsTangible = true;
        public void Toggle(bool isReal)
        {
            IsTangible = !isReal;
        }
    }
    public class SemiPermBhv : BlockEntityBehavior, IRedstoneTaker
    {
        public SemiPermBhv(BlockEntity blockentity) : base(blockentity)
        {
        }

        public void OnSignal(bool Activated)
        {
            if(Blockentity is SemiPermBE boi)
            {
                boi.Toggle(Activated);
            }
        }
    }
}
