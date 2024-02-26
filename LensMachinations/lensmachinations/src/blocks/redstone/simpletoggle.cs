using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace LensstoryMod
{
    public class SimpleToggleBlockBe : BlockEntity
    {
        public bool toggled = false;
        Block OnBlock;
        Block Offblock;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            AssetLocation OnLoc = Block.CodeWithPart("on", 1);
            AssetLocation offLoc = Block.CodeWithPart("off", 1);
            OnBlock = api.World.GetBlock(OnLoc);
            Offblock = api.World.GetBlock(offLoc);
            GetBehavior<Redstone>().begin(true);
        }

        public void OnTriggered(bool Activated)
        {
            if (toggled == Activated) { return; }
            toggled = !toggled;
            if(toggled && OnBlock != null)
            {
                Api.World.BlockAccessor.ExchangeBlock(OnBlock.BlockId, Pos);
            }else if (!toggled && Offblock != null)
            {
                Api.World.BlockAccessor.ExchangeBlock(Offblock.BlockId, Pos);
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("toggled", toggled);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            toggled = tree.GetBool("toggled");
        }
    }

    public class SimpleToggleBlockBhv : BlockEntityBehavior, IRedstoneTaker
    {
        public SimpleToggleBlockBhv(BlockEntity blockentity) : base(blockentity)
        {
        }

        public void OnSignal(bool Activated)
        {
            if (Blockentity is SimpleToggleBlockBe toggle && Api.Side ==  EnumAppSide.Server)
            {
                toggle.OnTriggered(Activated);
            }
        }
    }
}
