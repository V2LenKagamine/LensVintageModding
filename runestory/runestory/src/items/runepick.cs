using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;

namespace runestory.src.items
{
    public class RunePickaxe : Item
    {
        public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1)
        {
            if (blockSel?.Block?.GetBlockMaterial(world.BlockAccessor,blockSel?.Position) == EnumBlockMaterial.Stone)
            {
                float chance = Attributes["runeChance"]?.AsFloat() ?? 0.025f;
                if (world.Rand.NextDouble() < chance)
                {
                    Item[] nice = world.SearchItems("runestory:rune-*");
                    world.SpawnItemEntity(new(nice.ElementAt(world.Rand.Next(0,nice.Length)), 1),blockSel?.Position ?? byEntity.Pos.AsBlockPos);
                }

                return base.OnBlockBrokenWith(world, byEntity, itemslot,blockSel,dropQuantityMultiplier);
            }
            return false;
        }
    }
}
