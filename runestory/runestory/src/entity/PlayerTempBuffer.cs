
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace runestory
{
    public class PlayerTempBuffer : EntityBehavior
    {

        public static string RunetempBuffKey => "runestorytempbuff";
        public PlayerTempBuffer(Entity entity) : base(entity)
        {
        }

        public override void OnEntityDeath(DamageSource damageSourceForDeath)
        {
            IServerPlayer P = entity.World.PlayerByUid((entity as EntityPlayer).PlayerUID) as IServerPlayer;

            TempBuff scroll = new TempBuff();
            scroll.RemoveAll((P.Entity), RunetempBuffKey);
            scroll = null; //Goodbye sweet summer child.

            base.OnEntityDeath(damageSourceForDeath);
        }

        public override string PropertyName()
        {
            return "runestorytempbuffremover";
        }
    }

    public class TempBuff
    {
        EntityPlayer affected;

        List<EffectPowerDuration> EffPowDurList = new();

        string sourceCodeString;

        string effectID;


        public void DoStats(EntityPlayer entity, List<EffectPowerDuration> stats, string code, string sourceId)
        {
            affected = entity;
            EffPowDurList = stats;
            sourceCodeString = code;
            effectID = sourceId;
            if (EffPowDurList.Count >= 1)
            {
                ApplyStats();
            }
        }
        public void DoStats(EntityPlayer entity, EffectPowerDuration stats, string code, string sourceId)
        {
            affected = entity;
            EffPowDurList = [stats];
            sourceCodeString = code;
            effectID = sourceId;
            if (EffPowDurList.Count >= 1)
            {
                ApplyStats();
            }
        }
        public void DoStats(EntityPlayer entity, string effect,float change, float durationTicks, string code, string sourceId)
        {
            affected = entity;
            EffPowDurList = [new(effect,change,durationTicks)];
            sourceCodeString = code;
            effectID = sourceId;
            if (EffPowDurList.Count >= 1)
            {
                ApplyStats();
            }
        }

        public void DissapateEffect(float dt)
        {
            Dissapate();
        }

        public void Dissapate() //Experimental Code, may crash, must check.
        {
            foreach (EffectPowerDuration trio in EffPowDurList)
            {
                affected.Stats.Remove(trio.Effect, sourceCodeString);
                affected.WatchedAttributes.RemoveAttribute(effectID);
            }
            IServerPlayer player = (
               affected.World.PlayerByUid(affected.PlayerUID)
               as IServerPlayer
           );
            player.SendMessage(
                GlobalConstants.InfoLogChatGroup,
                Lang.Get("runestory:runedissipatebuff"),
                EnumChatType.Notification
            );
        }

        public void ApplyStats()
        {
            for (int i = 0; i < EffPowDurList.Count; i++)
            {
                
                var statslist = affected.Stats.Where(stat => stat.Key == EffPowDurList.ElementAt(i).Effect);
                var nobuff = true;
                for (int j =0;j < statslist.Count(); j++)
                {
                    var stat = statslist.ElementAt(j);
                    if(stat.Value.ValuesByKey.ContainsKey(sourceCodeString)) { nobuff = false; break; }
                }
                if(!nobuff) { continue; }
                affected.Stats.Set(EffPowDurList.ElementAt(i).Effect, sourceCodeString, EffPowDurList.ElementAt(i).Power, false);

                long discallback = affected.World.RegisterCallback(DissapateEffect, (int)Math.Floor(EffPowDurList.ElementAt(i).Duration));// in minutes
                //affected.WatchedAttributes.SetLong(effectID, discallback);

            }
        }

        public void RemoveAll(EntityPlayer player, string code)
        {
            foreach (KeyValuePair<string,EntityFloatStats> stat in player.Stats)
            {
                player.Stats.Remove(stat.Key, code);
            }
            player.GetBehavior<EntityBehaviorHealth>().MarkDirty();
        }
    }
    public struct EffectPowerDuration
    {
        public string Effect;
        public float Power;
        public float Duration;
        public EffectPowerDuration(string effect, float power, float duration)
        {
            Effect = effect; Power = power; Duration = duration;
        }
    }
}