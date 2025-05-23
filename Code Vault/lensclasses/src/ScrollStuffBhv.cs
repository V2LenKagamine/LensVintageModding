
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

namespace LensstoryMod
{
    public class ScrollStuffBhv : EntityBehavior
    {
        public ScrollStuffBhv(Entity entity) : base(entity)
        {
        }

        public override void OnEntityDeath(DamageSource damageSourceForDeath)
        {
            IServerPlayer P = this.entity.World.PlayerByUid((this.entity as EntityPlayer).PlayerUID) as IServerPlayer;

            ScrollEffect scroll = new ScrollEffect();
            scroll.removeAll((P.Entity), "lensmod");
            scroll.removeAll((P.Entity), "lensmodtemp");

            base.OnEntityDeath(damageSourceForDeath);
        }

        public override string PropertyName()
        {
            return "ScrollRemoveBhv";
        }
    }

    public class ScrollEffect
    {
        EntityPlayer affected;

        Dictionary<string, float> effectPowerList;

        Dictionary<string, float> effectTimeList;

        List<EffectPowerDuration> EPDL =  new();

        string effectCode;

        string effectID;


        public void ScrollStats(EntityPlayer entity,Dictionary<string,float> effectlist,string code,string id,Dictionary<string,float> durdic = null)
        {
            affected = entity;
            effectPowerList = effectlist;
            effectCode = code;
            effectID = id;
            effectTimeList = durdic;
            if(effectlist.Count >= 1)
            {
                applyStats();
            }
        }

        public void DissapateEffect(float dt) 
        {
            Dissapate();
        }

        public void Dissapate() //Experimental Code, may crash, must check.
        {
            var takefrom = EPDL.Where(trio => effectPowerList.ContainsKey(trio.Effect) && effectTimeList.ContainsKey(trio.Effect));
            foreach (EffectPowerDuration trio in takefrom)
            {
                affected.Stats.Remove(trio.Effect, "lensmodtemp");
                affected.WatchedAttributes.RemoveAttribute(effectID);
            }
            IServerPlayer player = (
               affected.World.PlayerByUid((affected).PlayerUID)
               as IServerPlayer
           );
            player.SendMessage(
                GlobalConstants.InfoLogChatGroup,
                "You feel your body shift, as a temporary effect dissapates.",
                EnumChatType.Notification
            );
        }

        public void applyStats()
        {
            foreach(KeyValuePair<string,float> stat in effectPowerList)
            {
                var statslist = affected.Stats.Where(stat2 => stat2.Key == stat.Key);
                var foundstatbonus = 0f;
                statslist.Foreach(stat =>
                {
                    stat.Value.ValuesByKey.TryGetValue(effectCode,out var possiblestats);
                    foundstatbonus += possiblestats != null ? possiblestats.Value : 0;
                });
                affected.Stats.Set(stat.Key,effectCode,stat.Value + foundstatbonus,true);
                if(effectTimeList == null) { continue; }
                if(effectTimeList.ContainsKey(stat.Key))
                {
                    EPDL.Add(new(stat.Key,stat.Value, effectTimeList[stat.Key]));
                    long discallback = affected.World.RegisterCallback(DissapateEffect, (int)Math.Floor(effectTimeList[stat.Key]) * 1000 * 60);// in minutes
                    affected.WatchedAttributes.SetLong(effectID, discallback);
                }
            }
        }

        public void removeAll(EntityPlayer player,string code)
        {
            foreach(var stat in player.Stats)
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
