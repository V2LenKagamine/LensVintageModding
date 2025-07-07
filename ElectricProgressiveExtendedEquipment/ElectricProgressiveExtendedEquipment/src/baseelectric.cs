using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace ElectricProgressiveExtendedEquipment.src
{
    public class BaseElectric : Item
    {
        int consperaction;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            consperaction = Attributes["perDurabilityDrain"] != null ? Attributes["perDurabilityDrain"].AsInt() : 20;
        }

        public override void DamageItem(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount = 1)
        {
            int dura = itemslot.Itemstack.Attributes.GetInt("durability");
            if (dura > amount)
            {
                dura -= amount;
                itemslot.Itemstack.Attributes.SetInt("durability", dura);
            }
            else
            {
                itemslot.Itemstack.Attributes.SetInt("durability", 1);
            }
            itemslot.MarkDirty();
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            dsc.AppendLine(inSlot.Itemstack.Attributes.GetInt("durability") * consperaction + "/" + inSlot.Itemstack.Collectible.GetMaxDurability(inSlot.Itemstack) * consperaction + " Power");
        }
    }
}
