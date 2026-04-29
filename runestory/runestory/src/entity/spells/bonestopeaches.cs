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
    public class BonePeaches : BaseRuneEnt
    {
        public override void OnEntitySpawn()
        {
            base.OnEntitySpawn();
            ScapeRune();
            Die();
        }

        public void ScapeRune()
        {
            if (Api.Side == EnumAppSide.Client || spawnedBy is null) { return; }
            EntityPlayer ply = (spawnedBy as EntityPlayer);
            ply.WalkInventory(slot =>
            {
                if( slot.Itemstack?.Collectible?.Code?.ToString() == "game:bone")
                {
                    int ofpeaches = Math.Min(8, slot.Itemstack.StackSize);
                    slot.TakeOut(ofpeaches);
                    slot.MarkDirty();
                    ItemStack millions = new(World.GetItem("game:fruit-peach"), ofpeaches);
                    if (!ply.TryGiveItemStack(millions)) {
                        Api.World.SpawnItemEntity((millions), ply.Pos.AsBlockPos);
                    }
                    return false;
                }
                return true;
            });
            
        }

        public override void OnTouchEntity(Entity entity)
        {
            //no.
        }
    }
}
