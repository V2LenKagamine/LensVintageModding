
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

        public List<TempBuff> TempBuffList = [];
        public static string RunetempBuffKey => "runestorytempbuff";
        public PlayerTempBuffer(Entity entity) : base(entity)
        {
        }


        public void AddTempBuff(TempBuff tempBuff)
        {
            tempBuff.ApplyStats();
            TempBuffList.Add(tempBuff);
        }
        public void AddTempBuff(List<TempBuff> tempBuffs)
        {
            foreach (TempBuff tempBuff in tempBuffs)
            {
                tempBuff.ApplyStats();
                TempBuffList.Add(tempBuff);
            }
        }

        public void AddTempBuff(EntityPlayer entity, List<EffectPowerDuration> stats, string code, string sourceId)
        {

            TempBuff boi = new TempBuff
            {
                affected = entity,
                EffPowDurList = stats,
                effectID = sourceId,
            };
            boi.ApplyStats();
            TempBuffList.Add(boi);

        }
        public void AddTempBuff(EntityPlayer entity, string effect, float change, float durationTicks, string sourceId)
        {

            TempBuff boi = new TempBuff
            {
                affected = entity,
                EffPowDurList = [new(effect, change, durationTicks)],
                effectID = sourceId,
            };
            boi.ApplyStats();
            TempBuffList.Add(boi);

        }

        public override void OnEntityDeath(DamageSource damageSourceForDeath)
        {
            IServerPlayer P = entity.World.PlayerByUid((entity as EntityPlayer).PlayerUID) as IServerPlayer;

            foreach(TempBuff t in TempBuffList)
            {
                t.Dissapate();
                TempBuffList.Remove(t);
            }
            TempBuffList = [];

            base.OnEntityDeath(damageSourceForDeath);
        }

        public override void OnEntityDespawn(EntityDespawnData despawn)
        {
            foreach (TempBuff t in TempBuffList)
            {
                t.Dissapate();
            }
            TempBuffList = null;

            base.OnEntityDespawn(despawn);
        }

        public override string PropertyName()
        {
            return "runestorytempbuffremover";
        }
    }

    public class TempBuff
    {
        public EntityPlayer affected;

        public List<EffectPowerDuration> EffPowDurList;

        public static string RunetempBuffKey => PlayerTempBuffer.RunetempBuffKey;

        public string effectID;

        public void DissapateEffect(float dt)
        {
            Dissapate();
        }

        public void Dissapate() //Experimental Code, may crash, must check.
        {
            if (EffPowDurList is null) { return; }
             IServerPlayer player = (
                    affected.World.PlayerByUid(affected.PlayerUID)
                    as IServerPlayer);
            foreach (EffectPowerDuration trio in EffPowDurList)
            {
                affected.Stats.Remove(trio.Effect, RunetempBuffKey);
                affected.WatchedAttributes.RemoveAttribute(effectID);
                RunestoryMS.runeSApi.Network.GetChannel(RunestoryMS.RMS_Net_Channel).SendPacket(new STC_BuffSync
                {
                    effect = trio.Effect,
                    duration = -1,
                }, player);
            }


            player?.SendMessage(
                GlobalConstants.InfoLogChatGroup,
                Lang.Get("runestory:runedissipatebuff"),
                EnumChatType.Notification
            );

            affected = null;
            EffPowDurList = null;
            effectID = null;
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
                    if(stat.Value.ValuesByKey.ContainsKey(RunetempBuffKey)) { nobuff = false; break; }
                }
                if(!nobuff) { continue; }
                affected.Stats.Set(EffPowDurList.ElementAt(i).Effect, RunetempBuffKey, EffPowDurList.ElementAt(i).Power, false);

                long discallback = affected.World.RegisterCallback(DissapateEffect, (int)Math.Floor(EffPowDurList.ElementAt(i).Duration));// in minutes
                RunestoryMS.runeSApi.Network.GetChannel(RunestoryMS.RMS_Net_Channel).SendPacket(new STC_BuffSync
                {
                    effect = EffPowDurList.ElementAt(i).Effect,
                    duration = EffPowDurList.ElementAt(i).Duration,
                }, affected.Player as IServerPlayer);
            }
        }

        public void RemoveAll(EntityPlayer player, string code)
        {
            foreach (KeyValuePair<string,EntityFloatStats> stat in player.Stats)
            {
                player.Stats.Remove(stat.Key, code);
            }
            player.GetBehavior<EntityBehaviorHealth>().MarkDirty();


            affected = null;
            EffPowDurList = null;
            effectID = null;
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