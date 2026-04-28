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
    public class WarCrySpell : BaseRuneEnt
    {
        public override void OnEntitySpawn()
        {
            base.OnEntitySpawn();
            Buff(spawnedBy);
            Die();
        }

        public void Buff(Entity entity)
        {
            if (Api.Side == EnumAppSide.Client || spawnedBy is null) { return; }
            Entity[] targets = Api.World.GetEntitiesAround(entity.Pos.XYZ, 6, 3, poss => (poss is EntityPlayer) && poss.Alive);
            targets.AddItem(entity);
            foreach (Entity target in targets)
            {
                PlayerTempBuffer buff = target.GetBehavior<PlayerTempBuffer>();
                if (buff != null)
                {
                    try
                    {
                        PlayerTempBuffer tmp = (target as EntityPlayer).GetBehavior<PlayerTempBuffer>();
                        tmp.AddTempBuff(target as EntityPlayer, "meleeWeaponsDamage", 0.2f, (2.5f * 60 * 1000),"warcry");
                        tmp.AddTempBuff(target as EntityPlayer, "armorDurabilityLoss", -0.2f, (2.5f * 60 * 1000), "warcry");
                        
                        ((target as EntityPlayer).Player as IServerPlayer).SendMessage(
                            GlobalConstants.InfoLogChatGroup,
                            Lang.Get("runestory:warcryon"),
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
