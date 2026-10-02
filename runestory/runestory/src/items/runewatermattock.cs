using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;

namespace runestory.src.items
{
    public class RuneWaterMattock : Item
    {
        public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1)
        {
            if (blockSel?.Position?.Y <= world.SeaLevel)
            {
                EnumBlockMaterial? broken = blockSel?.Block?.GetBlockMaterial(world.BlockAccessor,blockSel?.Position);
                if (broken == EnumBlockMaterial.Soil || broken == EnumBlockMaterial.Stone || broken == EnumBlockMaterial.Sand || broken == EnumBlockMaterial.Gravel)
                {
                    world.BlockAccessor.SetBlock(world.GetBlock("game:water-still-7").Id,blockSel.Position,BlockLayersAccess.Fluid);
                }
            }
            return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);
        }
    }
}
