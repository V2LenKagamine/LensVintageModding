using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace runestory.src.entity.spells
{
    public class Ignite : BaseRuneEnt
    {
        public override void OnTouchEntity(Entity entity)
        {
            Fire(entity);
            Die();
        }

        public override void OnCollided()
        {
            if ((spawnedBy as EntityPlayer)?.BlockSelection?.HitPosition is null) { return; }
            FireB((spawnedBy as EntityPlayer).BlockSelection.Position);
            Die();
        }

        public void Fire(Entity entity)
        {
            entity.Ignite();
        }
        public void FireB(BlockPos bloc)
        {

            if(Api.World.BlockAccessor.GetBlockEntity(bloc) is BlockEntityPitKiln pit) 
            {
                pit.TryIgnite(spawnedBy as IPlayer);
            }
            else if(Api.World.BlockAccessor.GetBlockEntity(bloc) is BlockEntityCharcoalPit cha)
            {
                cha.IgniteNow();
            }
        }
    }
}
