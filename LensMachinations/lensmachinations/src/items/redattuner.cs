
using System;
using System.Reflection.Metadata;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace LensstoryMod
{
    public class RedWandItem : Item
    {

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (byEntity.Controls.ShiftKey && api.Side == EnumAppSide.Client && blockSel == null)
            {
                RedWandGui gui = new(api as ICoreClientAPI);
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
                    if (byEntity.Controls.ShiftKey)
                    {
                        BlockEntity mayhapRed = api.World.BlockAccessor.GetBlockEntity(blockSel.Position);
                        if (mayhapRed != null && mayhapRed.GetBehavior<Redstone>() is { } behavior)
                        {
                            behavior.OutFrequency = slot.Itemstack.Attributes.GetString("channel");
                            behavior.begin(true);
                        }
                    }
                    else
                    {
                        BlockEntity mayhapRed = api.World.BlockAccessor.GetBlockEntity(blockSel.Position);
                        if (mayhapRed != null && mayhapRed.GetBehavior<Redstone>() is { } behavior)
                        {
                            behavior.Frequency = slot.Itemstack.Attributes.GetString("channel");
                            behavior.begin(true);
                        }
                    }
                }
            }
        }
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        }
    }

    public class RedWandGui : GuiDialog
    {
        string network = "";
        public RedWandGui(ICoreClientAPI capi) : base(capi)
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
            bg.WithChildren(texttest, inputbox, closebutton, confirmbutton);


            SingleComposer = capi.Gui.CreateCompo("RedWandGui", bounds)
                .AddShadedDialogBG(bg)
                .AddStaticText("Insert Network String", CairoFont.WhiteDetailText(), texttest)
                .AddTextInput(inputbox, numbers => { network = numbers; })
                .AddSmallButton("Close", () => { TryClose(); return true; }, closebutton)
                .AddSmallButton("Confirm", () => {
                    capi.Network.GetChannel("redmessages").SendPacket(new LensMachinationsMod.RedWandMessage() { Channel = network });
                    TryClose();
                    capi.ShowChatMessage("You attune the wand to channel: " + network);
                    return true;
                }, confirmbutton)
                .Compose();
        }
    }
}