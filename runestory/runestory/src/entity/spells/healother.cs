using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace runestory.src.entity.spells
{
    public class HealOther : BaseRuneEnt
    {
        public override void OnTouchEntity(Entity entity)
        {
            Heal(entity);
            Die();
        }

        public override void OnCollided()
        {
            Heal(Api.World.GetNearestEntity(Pos.XYZ, 1, 1));
            Die();
        }

        public void Heal(Entity entity)
        {
            if(Api.Side == EnumAppSide.Client) { return; }
            EntityBehaviorHealth? healthbhv = entity?.GetBehavior<EntityBehaviorHealth>();
            if (healthbhv != null)
            {
                try { 
                entity.ReceiveDamage(new DamageSource()
                {
                    Source = EnumDamageSource.Unknown,
                    Type = EnumDamageType.Heal,
                    TicksPerDuration = 10,
                    Duration = TimeSpan.FromSeconds(5)
                }, 3f); } catch(Exception e)
                {
                    //Fuck you why and how
                }
            }
        }
    }
}
