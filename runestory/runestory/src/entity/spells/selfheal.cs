using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace runestory.src.entity.spells
{
    public class HealSelf : BaseRuneEnt
    {
        public override void OnEntitySpawn()
        {
            base.OnEntitySpawn();
            Heal(spawnedBy);
            Die();
        }

        public void Heal(Entity entity)
        {
            if (Api.Side == EnumAppSide.Client) { return; }
            EntityBehaviorHealth? healthbhv = entity?.GetBehavior<EntityBehaviorHealth>();
            if (healthbhv != null)
            {
                try
                {
                    entity.ReceiveDamage(new DamageSource()
                    {
                        Source = EnumDamageSource.Unknown,
                        Type = EnumDamageType.Heal,
                        TicksPerDuration = 10,
                        Duration = TimeSpan.FromSeconds(5)
                    }, 2.5f);
                }
                catch (Exception e)
                {
                    //Fuck you why and how
                }
            }
        }

        public override void OnTouchEntity(Entity entity)
        {
            //no.
        }
    }
}
