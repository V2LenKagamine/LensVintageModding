using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace runestory.src.items
{
    public class  GoodBerryItem : Item
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            handling = EnumHandHandling.Handled;
            return;
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            return secondsUsed <= 0.96f;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {

            if (byEntity.World is IServerWorldAccessor && secondsUsed >= 0.95f)
            {
                byEntity.ReceiveSaturation(300, EnumFoodCategory.Fruit);
                byEntity.ReceiveSaturation(300, EnumFoodCategory.Vegetable);
                byEntity.ReceiveSaturation(300, EnumFoodCategory.Protein);
                byEntity.ReceiveSaturation(300, EnumFoodCategory.Grain);
                byEntity.ReceiveSaturation(300, EnumFoodCategory.Dairy);

                slot.TakeOutWhole();
                slot.MarkDirty();

            }
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
        }
    }
}
