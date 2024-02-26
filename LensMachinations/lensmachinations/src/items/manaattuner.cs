
using System;
using System.Reflection.Metadata;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace LensstoryMod
{
    public class AttunementWandItem : Item
    {

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (byEntity.Controls.ShiftKey && api.Side == EnumAppSide.Client)
            {
                AttunementWandGui gui = new(api as ICoreClientAPI);
                gui.Toggle();
            }
            else
            {
                handling = EnumHandHandling.PreventDefault;
            }
        }
        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (api.Side == EnumAppSide.Server)
            {
                if (blockSel != null)
                {
                    BlockEntity mayhapMana = api.World.BlockAccessor.GetBlockEntity(blockSel.Position);
                    if (mayhapMana != null && mayhapMana.GetBehavior<Mana>() is { } behavior)
                    {
                        behavior.ManaID = slot.Itemstack.Attributes.GetInt("channel");
                        behavior.begin(true);
                    }
                }
            }
        }
        public override float OnBlockBreaking(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
        {
            if (api.Side == EnumAppSide.Server)
            {
                if (blockSel != null)
                {
                    BlockEntity mayhapMana = api.World.BlockAccessor.GetBlockEntity(blockSel.Position);
                    if (mayhapMana != null && mayhapMana.GetBehavior<Mana>() is { } behavior)
                    {
                        behavior.ManaID = itemslot.Itemstack.Attributes.GetInt("channel");
                        behavior.begin(true);
                    }
                }
            }
            return base.OnBlockBreaking(player, blockSel, itemslot, remainingResistance, dt, counter);
        }
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        }
    }

    public class AttunementWandGui : GuiDialog
    {
        int networkNum = 0;
        public AttunementWandGui(ICoreClientAPI capi) : base(capi)
        {
            SetUsUp();
        }

        public override string ToggleKeyCombinationCode => null;

        private void SetUsUp()
        {
            ElementBounds bounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            ElementBounds texttest = ElementBounds.Fixed(0, 20, 300, 75);

            ElementBounds inputbox = ElementBounds.Fixed(0, 50, 200, 100);

            ElementBounds closebutton = ElementBounds.Fixed(300, 80, 50, 50);

            ElementBounds confirmbutton = ElementBounds.Fixed(225, 80, 50, 50);

            ElementBounds bg = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bg.BothSizing = ElementSizing.FitToChildren;
            bg.WithChildren(texttest,inputbox,closebutton,confirmbutton);


            SingleComposer = capi.Gui.CreateCompo("AttunewandGui", bounds)
                .AddShadedDialogBG(bg)
                .AddStaticText("Insert Network Code", CairoFont.WhiteDetailText(), texttest)
                .AddNumberInput(inputbox, numbers => { networkNum = numbers.ToInt(); })
                .AddSmallButton("Close",() => {TryClose();return true; },closebutton)
                .AddSmallButton("Confirm",() => { capi.Network.GetChannel("manamessages").SendPacket(new LensMachinationsMod.ManaWandMessage() { message = networkNum});
                    TryClose();
                    capi.ShowChatMessage("You attune the wand to channel: " + networkNum);
                    return true; },confirmbutton)
                .Compose();
        }
    }
}
