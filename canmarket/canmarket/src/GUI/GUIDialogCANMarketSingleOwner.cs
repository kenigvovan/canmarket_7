using canmarket.src.Inventories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace canmarket.src.GUI
{
    public class GUIDialogCANMarketSingleOwner : GUIDialogCANMarket
    {
        public GUIDialogCANMarketSingleOwner(string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, ICoreClientAPI capi) : base(dialogTitle, inventory, blockEntityPos, capi)
        {
            if (IsDuplicate)
            {
                return;
            }
            capi.World.Player.InventoryManager.OpenInventory((IInventory)inventory);
            SetupDialog();
        }
        public void SetupDialog()
        {
            string ownerName = (Inventory as InventoryCANMarketOnChest)?.be?.ownerName;

            //Set slots colors
            if (ownerName.Equals(capi.World.Player.PlayerName))
            {
                this.Inventory[0].HexBackgroundColor = "#79E02E";
            }
            else
            {
                this.Inventory[0].HexBackgroundColor = "#855522";
            }
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds ownerNameBounds = ElementBounds.Fixed(0.0, 30.0, 150, 25).WithAlignment(EnumDialogArea.CenterTop);
            ElementBounds closeButton = ElementBounds.Fixed(0, 30, 0, 0).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0);

            /*ElementBounds leftText = ElementBounds.FixedSize(70, 25).FixedUnder(ownerNameBounds, 20);
            ElementBounds rightText = ElementBounds.FixedSize(70, 25).RightOf(leftText, 25);
            rightText.fixedY = leftText.fixedY;*/

            ElementBounds leftSlots = ElementBounds.FixedSize(100, 120).FixedUnder(ownerNameBounds, 15);
            ElementBounds rightSlots = ElementBounds.FixedSize(60, 120).FixedRightOf(leftSlots);
            rightSlots.fixedY = leftSlots.fixedY;

            ElementBounds InfiniteStocksTextBounds = ElementBounds.FixedSize(150, 25).FixedUnder(leftSlots, 15);
            ElementBounds InfiniteStocksButtonBounds = ElementBounds.FixedSize(50, 25).RightOf(InfiniteStocksTextBounds, 15);
            InfiniteStocksButtonBounds.fixedY = InfiniteStocksTextBounds.fixedY;

            ElementBounds StorePaymentTextBounds = ElementBounds.FixedSize(150, 25).FixedUnder(InfiniteStocksButtonBounds, 15);
            ElementBounds StorePaymentButtonBounds = ElementBounds.FixedSize(50, 25).RightOf(StorePaymentTextBounds, 15);
            StorePaymentButtonBounds.fixedY = StorePaymentTextBounds.fixedY;

            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(new ElementBounds[]
            {
                closeButton
            });
            base.SingleComposer = this.capi.Gui
               .CreateCompo("marketCompo", dialogBounds)
               .AddShadedDialogBG(bgBounds, true, 5.0, 0.75f)
               .AddDialogTitleBar(Lang.Get("canmarket:gui-onchesttradeblock-bar"), delegate
               {
                   this.TryClose();
               }, null, null)
               .BeginChildElements(bgBounds);

            if ((Inventory as InventoryCANMarketOnChest)?.be?.ownerName != null)
            {
                SingleComposer.AddStaticText((Inventory as InventoryCANMarketOnChest).be?.ownerName, CairoFont.WhiteDetailText().WithFontSize(20), ownerNameBounds);
            }

            SingleComposer.AddItemSlotGrid((IInventory)this.Inventory, new Action<object>(((GUIDialogCANMarketSingleOwner)this).DoSendPacket), 1, new int[] { 0 }, leftSlots, "priceSlots");
            SingleComposer.AddItemSlotGrid((IInventory)this.Inventory, new Action<object>(((GUIDialogCANMarketSingleOwner)this).DoSendPacket), 1, new int[] { 1}, rightSlots, "goodsSlots");

            bool infiniteStocks = (Inventory as InventoryCANMarketOnChest)?.be.InfiniteStocks ?? false;
            bool storePayment = (Inventory as InventoryCANMarketOnChest)?.be.StorePayment ?? true;
            if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                SingleComposer.AddStaticText(Lang.Get("canmarket:infinite-stocks-info-gui"), CairoFont.WhiteDetailText().WithFontSize(20), InfiniteStocksTextBounds);
                SingleComposer.AddSwitch(FlipInfiniteStocksState, InfiniteStocksButtonBounds, "infinitestockstoggle");
                SingleComposer.GetSwitch("infinitestockstoggle")?.SetValue(infiniteStocks);

                SingleComposer.AddStaticText(Lang.Get("canmarket:store-payment-info-gui"), CairoFont.WhiteDetailText().WithFontSize(20), StorePaymentTextBounds);
                SingleComposer.AddSwitch(FlipStorePaymentState, StorePaymentButtonBounds, "storepaymenttoggle");
                SingleComposer.GetSwitch("storepaymenttoggle")?.SetValue(storePayment);
            }
            
            var slotSize = GuiElementPassiveItemSlot.unscaledSlotSize;
            var slotPaddingSize = GuiElementItemSlotGridBase.unscaledSlotPadding;

            for (int i = 0; i < 1; i++)
            {
                ElementBounds tmpEB = ElementBounds.
                    FixedSize(35, 35).
                    RightOf(rightSlots);
                tmpEB.fixedY = rightSlots.fixedY + i * (slotSize + slotPaddingSize) + 28;

                if (infiniteStocks)
                {
                    SingleComposer.AddDynamicText("∞",
                    CairoFont.WhiteSmallText().WithFontSize(13), tmpEB, "stock" + i);
                }
                else
                { 
                    SingleComposer.AddDynamicText((this.Inventory as InventoryCANMarketOnChest).stocks[i] < 999
                        ? (this.Inventory as InventoryCANMarketOnChest).stocks[i].ToString()
                        : "999+",
                        CairoFont.WhiteSmallText().WithFontSize(13), tmpEB, "stock" + i);
                }
            }
            

           SingleComposer.Compose();
        }
        public void FlipInfiniteStocksState(bool state)
        {
            capi.Network.SendBlockEntityPacket(this.BlockEntityPosition, 1042);
            return;
        }
        public void FlipStorePaymentState(bool state)
        {
            capi.Network.SendBlockEntityPacket(this.BlockEntityPosition, 1043);
            return;
        }
    }
}
