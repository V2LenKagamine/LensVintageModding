using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace runestory.src.entity.spells
{
    public class MendItems : BaseRuneEnt
    {
        public override void OnEntitySpawn()
        {
            base.OnEntitySpawn();
            Mender();
            Die();
        }

        public void Mender()
        {
            if (Api.Side == EnumAppSide.Client || spawnedBy is null) { return; }
            EntityPlayer ply = (spawnedBy as EntityPlayer);
            ItemSlot slot = ply.ActiveHandItemSlot;
            if (slot.Itemstack?.Attributes?.GetInt("durability") < slot.Itemstack?.Collectible?.Durability)
            {
                int curdur = slot.Itemstack?.Attributes?.GetInt("durability", 1) ?? 1;
                slot.Itemstack.Collectible.SetDurability(slot.Itemstack, Math.Min(slot.Itemstack.Collectible.Durability, (int)Math.Round(curdur + slot.Itemstack.Collectible.Durability * 0.1f)));
                slot.MarkDirty();
            }
        }

        public override void OnTouchEntity(Entity entity)
        {
            //no.
        }
    }
}
