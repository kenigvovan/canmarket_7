using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace canmarket.src.GUI
{
    public class GUIDialogCANWareHouse: GuiDialogBlockEntity
    {
        public GUIDialogCANWareHouse(string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, ICoreClientAPI capi) : base(dialogTitle, inventory, blockEntityPos, capi)
        {
            if (IsDuplicate)
            {
                return;
            }
            capi.World.Player.InventoryManager.OpenInventory((IInventory)inventory);
            SetupDialog();
        }
        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }
        public void SetupDialog()
        {


            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            // Just a simple 300x300 pixel box
            ElementBounds textBounds = ElementBounds.Fixed(0, 40, 250, 75);

            // Background boundaries. Again, just make it fit it's child elements, then add the text as a child element
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(textBounds);

            // Lastly, create the dialog
            SingleComposer = capi.Gui.CreateCompo("wareHouseCompo", dialogBounds);
            SingleComposer.AddShadedDialogBG(bgBounds);
            SingleComposer.AddItemSlotGrid((IInventory)this.Inventory,
                new Action<object>(DoSendPacket), 1, new int[] { 0}, textBounds, "bookSlot");
            SingleComposer.AddDialogTitleBar(Lang.Get("canmarket:gui-warehouse-title-bar"), OnTitleBarCloseClicked);
            SingleComposer.AddButton(Lang.Get("canmarket:gui-warehouse-sign-book"), () => {

                capi.Network.SendBlockEntityPacket(BlockEntityPosition, 1042, null);
                return true;
            }, ElementBounds.Fixed(120, (double)textBounds.fixedY + 25, 90, 40));

            ElementBounds radiusInfoEB = ElementBounds.FixedSize(25, 25).FixedRightOf(textBounds);
            radiusInfoEB.fixedY = textBounds.fixedY + 30;
            SingleComposer.AddInset(radiusInfoEB);
            SingleComposer.AddRichtext("<icon name=select></icon>", CairoFont.ButtonText(), radiusInfoEB);
            SingleComposer.AddHoverText(Lang.Get("canmarket:gui-warehouse-radius-hover", canmarket.config.SEARCH_CONTAINER_RADIUS), CairoFont.ButtonText(), 400, radiusInfoEB);
            //.AddStaticText("This is a piece of text at the center of your screen - Enjoy!", CairoFont.WhiteDetailText(), textBounds)
            SingleComposer.Compose();
            return;           
        }
    }
}
