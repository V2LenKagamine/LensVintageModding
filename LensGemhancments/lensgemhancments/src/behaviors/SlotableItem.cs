using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using static LensGemhancments.LenGemConst;

namespace LensGemhancments
{
    public class SlotableItem : CollectibleBehavior
    {
        public SlotableItem(CollectibleObject collObj) : base(collObj)
        {
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            ItemStack stacc = inSlot.Itemstack;
            int maxGems = getMaxGems(stacc);
            if (maxGems >= 1)
            {
                dsc.AppendLine(Lang.Get("lengemhancements:socketable") + maxGems);
                dsc.AppendLine(Lang.Get("lengemhancements:contains"));
                if(stacc.Attributes.HasAttribute(GEM_SLOTTED))
                {
                    ITreeAttribute tree = stacc.Attributes.GetTreeAttribute(GEM_SLOTTED);
                    for (int i = 0; i < maxGems; i++)
                    {
                        ITreeAttribute gemSlot = tree.GetTreeAttribute("slot" + i);
                        if (!gemSlot.HasAttribute(GEM_STAT))
                        {
                            dsc.AppendLine(Lang.Get("lengemhancements:emptyslot"));
                        }
                        else
                        {
                            dsc.AppendLine((gemSlot.GetFloat(GEM_VALUE) > 0 ? ("+" + gemSlot.GetFloat(GEM_VALUE)) : gemSlot.GetFloat(GEM_VALUE)) + "% "+ Lang.Get("lengemhancements:" + gemSlot.GetString(GEM_STAT)));
                        }
                    }
                }
                else
                {
                    dsc.AppendLine("No Sockets.");
                }
            }
        }

        public static int getMaxGems(ItemStack stacc)
        {
            if(stacc == null) return -1;
            if(stacc.ItemAttributes!= null && stacc.ItemAttributes.KeyExists(MAX_GEMS) && stacc.ItemAttributes[MAX_GEMS].AsInt() >=1)
            {
                return stacc.ItemAttributes[MAX_GEMS].AsInt();
            }
            return -1;
        }
    }
}
