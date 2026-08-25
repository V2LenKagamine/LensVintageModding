using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace runestory.src.MiscHarmony
{
    public static class AddRMSstats
    {
        public static void UpdateEquips(IInventory inv, IServerPlayer player)
        {
            float magicdmg = 0f;
            float runechance = 0f;
            if (player is null || inv is null) { return; }
            foreach (var slot in inv)
            {
                if (slot.Empty || slot.Itemstack?.ItemAttributes is null) { continue; }
                if (slot.Itemstack.ItemAttributes["magicAttributes"] is null) { continue; }
                magicdmg += slot.Itemstack.ItemAttributes["magicAttributes"][RunestoryMS.RMS_Stat_MagicDamage]?.AsFloat() ?? 0f;
                runechance += slot.Itemstack.ItemAttributes["magicAttributes"][RunestoryMS.RMS_Stat_CDTime]?.AsFloat() ?? 0f;
            }
            EntityPlayer plyent = player.Entity;
            plyent.Stats.Set(RunestoryMS.RMS_Stat_MagicDamage, "wearablemod",magicdmg, true)
                .Set(RunestoryMS.RMS_Stat_CDTime,"wearablemod",runechance,true);
        }
        public static void UpdateHotbarEquips(IInventory inv, IServerPlayer player)
        {
            float magicdmg = 0f;
            float runechance = 0f;
            bool wandequipped = false;
            if(player is null || inv is null){ return; }
            foreach (var slot in inv)
            {
                if (slot.Empty || slot.Itemstack?.ItemAttributes is null) { continue; }
                if (slot.Itemstack != player.InventoryManager.OffhandHotbarSlot.Itemstack || slot.Itemstack.ItemAttributes["magicAttributes"] is null) { continue; }

                magicdmg += slot.Itemstack.ItemAttributes["magicAttributes"][RunestoryMS.RMS_Stat_MagicDamage]?.AsFloat() ?? 0;
                runechance += slot.Itemstack.ItemAttributes["magicAttributes"][RunestoryMS.RMS_Stat_CDTime]?.AsFloat() ?? 0;
                wandequipped = true;

            }
            EntityPlayer plyent = player.Entity;
            plyent.Stats.Set(RunestoryMS.RMS_Stat_MagicDamage, "hotbarmod", magicdmg, true);
            plyent.Stats.Set(RunestoryMS.RMS_Stat_CDTime, "hotbarmod", runechance, true);
            plyent.Stats.Set("hungerrate", "hotbarmod", wandequipped? -0.2f : 0f, true);
        }
    }
}
