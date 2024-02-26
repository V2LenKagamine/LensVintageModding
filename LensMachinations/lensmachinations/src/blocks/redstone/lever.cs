using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace LensstoryMod
{
    public class LeverBlock : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is LeverBE entity)
            {
                return entity.OnPlayerInteract(byPlayer);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }

    public class LeverBE : BlockEntity
    {
        public bool toggled = false;
        Block OnBlock;
        Block Offblock;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            AssetLocation OnLoc = Block.CodeWithPart("on",1);
            AssetLocation offLoc = Block.CodeWithPart("off",1);
            OnBlock = api.World.GetBlock(OnLoc);
            Offblock = api.World.GetBlock(offLoc);
            GetBehavior<Redstone>().begin(true);
        }
        public bool OnPlayerInteract(IPlayer player)
        {
            if(player.InventoryManager.ActiveHotbarSlot.Itemstack != null) { return false; }
            toggled = !toggled;
            if (toggled && OnBlock != null)
            {
                Api.World.BlockAccessor.ExchangeBlock(OnBlock.BlockId, Pos);
            }else if (!toggled && Offblock != null)
            {
                Api.World.BlockAccessor.ExchangeBlock(Offblock.BlockId, Pos);
            }
            return true;
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            dsc.AppendLine("It's toggled " + (toggled == true ? "on." : "off."));
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("toggled",toggled);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            toggled = tree.GetBool("toggled");
        }
    }
    public class LeverBhv : BlockEntityBehavior, IRedstoneSender
    {
        public LeverBhv(BlockEntity blockentity) : base(blockentity)
        {
        }

        public bool Active()
        {
            if(Blockentity is LeverBE lever)
            {
                return lever.toggled;
            }
            return false;
        }
    }
}