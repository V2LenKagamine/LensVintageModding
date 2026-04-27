using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace runestory.src.entity.spells
{
    public class SuperHeat : BaseRuneEnt
    {
        public override void OnEntitySpawn()
        {
            base.OnEntitySpawn();
            Heat();
            Die();
        }

        public void Heat()
        {
            if (Api.Side == EnumAppSide.Client || spawnedBy is null) { return; }
            ItemStack boi = (spawnedBy as EntityPlayer).ActiveHandItemSlot?.Itemstack;
            if (boi is not null)
            {
                boi.Collectible.SetTemperature(World, boi, (350 + (750 * boi.StackSize)) / boi.StackSize);
            }
            (spawnedBy as EntityPlayer).ActiveHandItemSlot.MarkDirty();
        }

        public override void OnTouchEntity(Entity entity)
        {
            //no.
        }
    }
}
