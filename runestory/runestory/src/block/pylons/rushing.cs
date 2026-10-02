using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace runestory.src.block.pylons
{
    public class RushingPylonBe : BlockEntity
    {
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            Block below = Api.World.BlockAccessor.GetBlock(Pos.DownCopy());
            if (below.FirstCodePart() == "water")
            {
                Api.World.BlockAccessor.SetBlock(Api.World.GetBlock(below.Code.WildCardReplace("water-*","rapidwater-*")).Id,Pos.DownCopy());
            }
            base.OnBlockPlaced(byItemStack);
        }
        public override void OnBlockRemoved()
        {
            Block below = Api.World.BlockAccessor.GetBlock(Pos.DownCopy());
            if (below.FirstCodePart() == "rapidwater")
            {
                Api.World.BlockAccessor.SetBlock(Api.World.GetBlock(below.Code.WildCardReplace("rapidwater-*", "water-*")).Id, Pos.DownCopy());
            }
            base.OnBlockRemoved();
        }
    }
}
