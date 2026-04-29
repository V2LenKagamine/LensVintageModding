using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace runestory.src.entity.spells
{
    public class SproutCutting : BaseRuneEnt
    {
        public override void OnTouchEntity(Entity entity)
        {
            Cuttings();
            Die();
        }

        public override void OnCollided()
        {
            Cuttings();
            Die();
        }
        public void Cuttings()
        {
            if (Api.Side == EnumAppSide.Client) { return; }
            bool coerced = false;
            World.BlockAccessor.WalkBlocks(Pos.XYZ.AddCopy(1, 1, 1).AsBlockPos, Pos.XYZ.AddCopy(-1, -1, -1).AsBlockPos, (blocc, ex, why, zee) =>
            {
                if (!coerced)
                {
                    BlockPos bloc = new(ex, why, zee);
                    if (World.BlockAccessor.GetBlockEntity(bloc)?.GetBehavior<BEBehaviorFruitingBush>() is BEBehaviorFruitingBush bush)
                    {
                        string type = blocc.FirstCodePart(2);
                        ItemStack boi = new ItemStack(World.GetBlock($"game:fruitingbushcutting-{type}-free")); //Todo: FUCK TRAITS.
                        World.SpawnItemEntity(boi, bloc);
                        coerced = true;
                    }
                }
            });
        }
    }
}
