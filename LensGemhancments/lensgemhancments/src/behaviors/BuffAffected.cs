using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;
using static LensGemhancments.LenGemConst;

namespace LensGemhancments
{
    public class BuffAffected : EntityBehavior
    {

        public Dictionary<int,Dictionary<string,float>> SlotStatValDic = new();

        public BuffAffected(Entity entity) : base(entity)
        {
            this.getBuffs();
            IServerPlayer ply = (this.entity as EntityPlayer).Player as IServerPlayer;
            if (ply != null)
            {
                IInventory inv = ply.InventoryManager.GetOwnInventory("character");
                if (inv != null)
                {
                    inv.SlotModified += onModified;
                }
            }
        }
        public override void OnEntityDespawn(EntityDespawnData despawn)
        {
            IServerPlayer ply = (this.entity as EntityPlayer).Player as IServerPlayer;

            if (ply != null)
            {
                IInventory inv = ply.InventoryManager.GetOwnInventory("character");
                if (inv != null)
                {
                    inv.SlotModified -= onModified;
                }
            }
            saveBuffs();
            base.OnEntityDespawn(despawn);
        }
        public override string PropertyName()
        {
            return GEM_BUFFAFFECTED;
        }

        private void onModified(int slot) { //Todo: Does this work

            ItemStack stacc = ((entity as EntityPlayer).Player as IServerPlayer).InventoryManager.GetOwnInventory("character")[slot].Itemstack;
            Dictionary<string,float> itemStats = getStackBuffs(stacc);
            if(SlotStatValDic.TryGetValue(slot,out Dictionary<string,float> found))
            {
                if(!found.Equals(itemStats)) {
                    if(itemStats.Count >=1)
                    {
                        applyFromStack(found, entity as EntityPlayer,false);
                        SlotStatValDic[slot] = itemStats;
                    }
                    else
                    {
                        applyFromStack(found, entity as EntityPlayer,true);
                        SlotStatValDic.Remove(slot);
                    }
                }
            } else
            {
                if (itemStats.Count >= 1)
                {
                    applyFromStack(itemStats, entity as EntityPlayer,false);
                    SlotStatValDic[slot] = itemStats;
                }
                else
                {
                    applyFromStack(itemStats, entity as EntityPlayer,true);
                    SlotStatValDic.Remove(slot);
                }
            }
        }
        private void onModifiedHotbar(int toSlot,int fromSlot) //Todo: Test
        {
            if(toSlot != ((entity as EntityPlayer).Player as IServerPlayer).InventoryManager.ActiveHotbarSlotNumber)
            {
                return;
            }
            ItemStack stacc = ((entity as EntityPlayer).Player as IServerPlayer).InventoryManager.GetOwnInventory("hotbar")[toSlot].Itemstack;
            if (SlotStatValDic.GetValueOrDefault(fromSlot, null)!=null)
            {
                applyFromStack(SlotStatValDic.GetValueOrDefault(fromSlot,null), entity as EntityPlayer, true);
                SlotStatValDic.Remove(toSlot);
                return;
            }if (stacc == null) { return; }
            Dictionary<string, float> itemStats = getStackBuffs(stacc);
            if (SlotStatValDic.TryGetValue(toSlot, out Dictionary<string, float> found))
            {
                if (!found.Equals(itemStats))
                {
                    if (itemStats.Count >= 1)
                    {
                        applyFromStack(found, entity as EntityPlayer, false);
                        SlotStatValDic[toSlot] = itemStats;
                    }
                    else
                    {
                        applyFromStack(SlotStatValDic.GetValueOrDefault(fromSlot, null), entity as EntityPlayer, true);
                        SlotStatValDic.Remove(toSlot);
                    }
                }
            }
            else 
            {
                if (itemStats.Count >= 1)
                {
                    applyFromStack(itemStats, entity as EntityPlayer,false);
                    SlotStatValDic[toSlot] = itemStats;
                }
                else
                {
                    applyFromStack(SlotStatValDic.GetValueOrDefault(fromSlot, null), entity as EntityPlayer, true);
                    SlotStatValDic.Remove(toSlot);
                }
            }
        }
        public void onSlotSwapped(IServerPlayer ply, int fromSlot,int toSlot)
        {
            onModifiedHotbar(toSlot,fromSlot);
        }

        private static void applyFromStack(Dictionary<string,float> theDic,EntityPlayer ply,bool remove) //Todo: Ensure works
        {
            if (theDic == null) { return; }
            foreach(var buff in theDic)
            {
                if (!ply.Stats[buff.Key].ValuesByKey.ContainsKey(GEMS_BUFFS))
                {
                    ply.Stats.Set(buff.Key, GEMS_BUFFS, 0);
                }

                float foundstat = 0f;
                var stats = ply.Stats.Where(statkey => statkey.Key == buff.Key);
                foreach (var stat in stats)
                {
                    stat.Value.ValuesByKey.TryGetValue(GEMS_BUFFS, out var possible);
                    foundstat += possible != null ? possible.Value : 0f;
                }

                ply.Stats.Set(buff.Key, GEMS_BUFFS, (remove? -buff.Value/100f:buff.Value/100) + foundstat);
            }
        }

        private Dictionary<string,float> getStackBuffs(ItemStack stacc)
        {
            Dictionary<string,float> result = new();
            if (stacc != null && stacc.Attributes.HasAttribute(GEM_SLOTTED))
            {
                ITreeAttribute tree = stacc.Attributes.GetTreeAttribute(GEM_SLOTTED);
                for (int i = 0; i < SlotableItem.getMaxGems(stacc); i++)
                {
                    ITreeAttribute gemSlot = tree.GetTreeAttribute("slot" + i.ToString());
                    if(!gemSlot.HasAttribute(GEM_STAT)) { continue; }
                    if (gemSlot != null)
                    {
                        float val = gemSlot.GetFloat(GEM_VALUE);
                        string buffType = gemSlot.GetString(GEM_STAT);
                        if (result.TryGetValue(buffType,out float found))
                        {
                            result[buffType] =  found + val;
                        }else
                        {
                            result[buffType] = val;
                        }
                    }
                }
            }
            return result;
        }

        private void saveBuffs()
        {
            (entity as EntityPlayer).Player.WorldData.SetModdata("lengembuffs", SerializerUtil.Serialize(this.SlotStatValDic));
        }

        private void getBuffs()
        {
            var loadedBuffs = (this.entity as EntityPlayer).Player.WorldData.GetModdata("lengembuffs");
            if (loadedBuffs != null)
            {
                SlotStatValDic = SerializerUtil.Deserialize<Dictionary<int,Dictionary<string,float>>>(loadedBuffs);
            }
        }
    }
}
