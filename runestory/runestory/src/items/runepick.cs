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
            if (WildcardUtil.Match("rock-*", blockSel?.Block?.Code?.Path?.ToString()))
            {
                float chance = 0.025f;
                switch(Code.EndVariant())
                {
                    case "iron":
                        {
                            chance = 0.05f;
                            break;
                        }
                    case "gold":
                        {
                            chance = 0.075f;
                            break;
                        }
                    case "silver":
                        {
                            chance = 0.07f;
                            break;
                        }
                    case "blackbronze":
                        {
                            chance = 0.06f;
                            break;
                        }
                    case "copper":
                        {
                            chance = 0.0025f;
                            break;
                        }
                }
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
