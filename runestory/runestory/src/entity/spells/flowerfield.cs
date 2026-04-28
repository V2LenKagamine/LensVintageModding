using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace runestory.src.entity.spells
{
    public class FlowerFieldSpell : BaseRuneEnt
    {
        public override void OnEntitySpawn()
        {
            base.OnEntitySpawn();
            Flowers(spawnedBy);
            Die();
        }


        public void Flowers(Entity entity)
        {
            if (Api.Side == EnumAppSide.Client) { return; }
            BlockPos starter = spawnedBy.Pos.AsBlockPos.Copy();
            IEnumerable <Block>  flowers = Api.World.Blocks.Where(flower => WildcardUtil.Match("game:flower-*-free", flower.Code.ToString()));
            Block decidedflower = Api.World.GetBlock(flowers.ElementAt(World.Rand.Next(0,flowers.Count())).Id);
            Api.World.BlockAccessor.WalkBlocks(starter.AddCopy(4, 2, 4), starter.AddCopy(-4, -2, -4), (blck, x, y, z) =>
            {
                BlockPos curr = new BlockPos(x, y, z);
                if (World.BlockAccessor.GetBlock(curr).Id == 0 || World.BlockAccessor.GetBlock(curr).Replaceable >=6000)
                {
                    if (World.Rand.NextDouble() > 0.35f && World.BlockAccessor.GetBlock(curr.DownCopy()).SideSolid.OnSide(BlockFacing.UP))
                    {
                        World.BlockAccessor.SetBlock(decidedflower.Id,curr);
                    }
                }
            });
        }

        public override void OnTouchEntity(Entity entity)
        {
            //no.
        }
    }
}
