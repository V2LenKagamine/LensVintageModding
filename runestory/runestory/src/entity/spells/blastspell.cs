using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace runestory.src.entity.spells
{
    public class BlastSpell : BaseRuneEnt
    {
        public override void OnTouchEntity(Entity entity)
        {
            if (entity != spawnedBy && entity is not null)
            {
                Boom();
                Die();
            }
        }

        public override void OnCollided()
        {
            Boom();
            Die();
        }

        public void Boom()
        {
            if(Api.Side == EnumAppSide.Client) { return; }
            (Api.World as IServerWorldAccessor).CreateExplosion(Pos.AsBlockPos, EnumBlastType.RockBlast, 5, 5, 1, (spawnedBy as EntityPlayer)?.PlayerUID ?? null);
        }
    }
}
