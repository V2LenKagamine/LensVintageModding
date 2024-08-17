using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using static LensGemhancments.LenGemConst;

namespace LensGemhancments
{ 
    public class PowerCore : Item
    {
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            if (inSlot.Itemstack.ItemAttributes["armor"].Exists)
            {
                dsc.AppendLine(Lang.Get("Gives {1}% {0} on armor.", Lang.Get("lengemhancements:"+inSlot.Itemstack.ItemAttributes["armor"][GEM_STAT].AsString()), inSlot.Itemstack.ItemAttributes["armor"][GEM_VALUE].AsFloat()));
            }
            if (inSlot.Itemstack.ItemAttributes["tool"].Exists)
            {
                dsc.AppendLine(Lang.Get("Gives {1}% {0} on a tool.", Lang.Get("lengemhancements:"+inSlot.Itemstack.ItemAttributes["tool"][GEM_STAT].AsString()), inSlot.Itemstack.ItemAttributes["tool"][GEM_VALUE].AsFloat()));
            }
        }
        public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null)
        {
            if (extractedStack == null) { return; }
            string searchingfor = extractedStack.Item.FirstCodePart() == "armor" ? "armor" : "tool";
            if (!slot.Itemstack.ItemAttributes[searchingfor].Exists) { return; }
            var selected = slot.Itemstack.ItemAttributes[searchingfor];
            if (!(selected[GEM_STAT].Exists || selected[GEM_VALUE].Exists)) { return; }
            int maxGems = SlotableItem.getMaxGems(extractedStack);
            if(!extractedStack.Attributes.HasAttribute(GEM_SLOTTED))
            {
                extractedStack.Attributes.GetOrAddTreeAttribute(GEM_SLOTTED);
                for (int i = 0; i < maxGems; i++)
                {
                    extractedStack.Attributes.GetTreeAttribute(GEM_SLOTTED).GetOrAddTreeAttribute("slot" + i.ToString());
                }
            }
            if (maxGems >= 1 && extractedStack.Attributes.HasAttribute(GEM_SLOTTED))
            {
                ITreeAttribute tree = extractedStack.Attributes.GetTreeAttribute(GEM_SLOTTED);
                for (int i = 0; i < maxGems; i++)
                {
                    ITreeAttribute gemSlot = tree.GetTreeAttribute("slot" + i.ToString());
                    if (gemSlot.HasAttribute(GEM_STAT)) { continue; }
                    gemSlot.SetString(GEM_STAT, selected[GEM_STAT].AsString());
                    gemSlot.SetFloat(GEM_VALUE, selected[GEM_VALUE].AsFloat());
                    slot.TakeOut(1);
                    slot.MarkDirty();
                    return;
                }
            }
        }
    }
}
