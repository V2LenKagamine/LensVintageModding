using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace runestory.src.entity.spells
{
    public class WaterCloak : BaseRuneEnt
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
            Entity[] targets = Api.World.GetEntitiesAround(entity.Pos.XYZ, 6, 3, poss => (poss is EntityPlayer) && poss.Alive);
            targets.AddItem(entity);
            foreach (Entity target in targets)
            {
                EntityBehaviorHealth? healthy = target.GetBehavior<EntityBehaviorHealth>();
                if (healthy != null)
                {
                    try
                    {
                        target.ReceiveDamage(new DamageSource()
                        {
                            Source = EnumDamageSource.Unknown,
                            Type = EnumDamageType.Heal,
                            TicksPerDuration = 600,
                            Duration = TimeSpan.FromMinutes(5)
                        }, 25f);
                        ((target as EntityPlayer).Player as IServerPlayer).SendMessage(
                            GlobalConstants.InfoLogChatGroup,
                            Lang.Get("runestory:mendingmist"),
                            EnumChatType.Notification
                        );
                    }
                    catch (Exception e)
                    {
                        //Fuck you why and how
                    }
                }
            }
        }

        public override void OnTouchEntity(Entity entity)
        {
            //no.
        }
    }
}
