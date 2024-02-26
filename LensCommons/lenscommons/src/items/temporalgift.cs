using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace LensstoryMod
{
    public class TemporalGiftItem : Item
    {
        public override void OnHeldUseStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumHandInteract useType, bool firstEvent, ref EnumHandHandling handling)
        {
            handling = EnumHandHandling.Handled;
            if(api.World.Side == EnumAppSide.Server)
            {
                if (!slot.Itemstack.Attributes.HasAttribute("gifts")) { return; }
                var itemattr = ((TreeArrayAttribute)slot.Itemstack.Attributes.GetTreeAttribute("gifts")["itemlist"])?.value;
                foreach (var thing in itemattr)
                {

                    var code = thing.GetString("type");
                    ItemStack yep;
                    switch (code)
                    {
                        case string x when x == "block" || x == "Block":
                            {
                                yep = new(api.World.GetBlock(new AssetLocation(thing.GetString("code"))), thing.GetAsInt("amount", 1));
                                break;
                            }
                        case string x when x == "item" || x == "Item":
                            {
                                yep = new(api.World.GetItem(new AssetLocation(thing.GetString("code"))), thing.GetAsInt("amount", 1));
                                break;
                            }

                        default:
                            { continue; }
                    }
                    var didgive = byEntity.TryGiveItemStack(yep);
                    if(!didgive)
                    {
                        api.World.SpawnItemEntity(yep,byEntity.Pos.Copy().Add(0.5f,0.5f,0.5f).AsBlockPos.ToVec3d());
                    }
                }
                slot.TakeOutWhole();
                slot.MarkDirty();
            }
        }

    }
}
