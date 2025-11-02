using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace LensMiniTweaks.src.blocks
{
    public class WaterfillBlock : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is WaterfillBE ent) {
                return ent.OnPlayerInteract(world,byPlayer,blockSel);
            }
            return true;
        }
    }
    public class WaterfillBE : BlockEntity
    {

        public BlockPos LinkedPos;
        private BlockPos NullPos;
        public bool OnPlayerInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if(Api.Side != EnumAppSide.Server) { return true; }
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot.Itemstack != null && slot.Itemstack.Collectible.Code == AssetLocation.Create("lensminitweaks:waterfillblock"))
            {
                if (!byPlayer.Entity.Controls.ShiftKey)
                {
                    slot.Itemstack.Attributes.SetBlockPos("LinkedTo", Pos.Copy());
                    slot.MarkDirty();
                }
            }
            else if (byPlayer.Entity.Controls.ShiftKey && LinkedPos != NullPos)
            {
                TryWaterFill();
            }
            return false;
        }

        public void TryWaterFill()
        {
            if(LinkedPos == NullPos)
            {
                //What. How. We JUST CHECKED THIS.
                throw new ArgumentNullException("You cant water-fill an undefined area. How you did this I don't know. Pos: " + Pos.ToString());
            }
            int maxarea = Block.Attributes["maxArea"].AsInt(4096);
            if (maxarea < CalculateArea()) { return; }
            if(Api.World.BlockAccessor.GetBlock(LinkedPos).Id != Block.Id) { return; }
            (Api as ICoreServerAPI).World.BlockAccessor.WalkBlocks(Pos.Copy(), LinkedPos, (blocc,X, Y, Z) => {
                if(blocc.Id != 0 && blocc.Id != Block.Id) { return; }
                Api.World.BlockAccessor.SetBlock(Api.World.GetBlock(AssetLocation.Create("game:water-still-7")).Id,new (X,Y,Z));
            });
        }

        public int CalculateArea()
        {
            int Xdiff = Math.Abs(Math.Abs(Pos.X) - Math.Abs(LinkedPos.X)) + 1;
            int Ydiff = Math.Abs(Math.Abs(Pos.Y) - Math.Abs(LinkedPos.Y)) + 1;
            int Zdiff = Math.Abs(Math.Abs(Pos.Z) - Math.Abs(LinkedPos.Z)) + 1;
            return Xdiff * Ydiff * Zdiff;
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);
            NullPos = new(0, 0, 0);
            LinkedPos = new(0, 0, 0);
            if (byItemStack != null) {
                LinkedPos = byItemStack.Attributes.GetBlockPos("LinkedTo",NullPos);
                if(Api.World.BlockAccessor.GetBlockEntity(LinkedPos) is WaterfillBE wortor)
                {
                    wortor.LinkedPos = Pos.Copy();
                }
            }
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            dsc.AppendLine("Linked to: " + (LinkedPos != NullPos ? LinkedPos.ToString() : "R-click with another of this block to link!"));
            if (LinkedPos != NullPos) {
                dsc.AppendLine("Calculated size: " + CalculateArea());
            }

        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBlockPos("LinkedPos",LinkedPos);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            LinkedPos = tree.GetBlockPos("LinkedPos");
        }
    }
}
