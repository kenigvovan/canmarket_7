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
    public class GUIDialogCANMarketOwner : GuiDialogBlockEntity
    {
        public GUIDialogCANMarketOwner(string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, ICoreClientAPI capi) : base(dialogTitle, inventory, blockEntityPos, capi)
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
                this.Inventory[2].HexBackgroundColor = "#79E02E";
                this.Inventory[4].HexBackgroundColor = "#79E02E";
                this.Inventory[6].HexBackgroundColor = "#79E02E";
            }
            else
            {
                this.Inventory[0].HexBackgroundColor = "#855522";
                this.Inventory[2].HexBackgroundColor = "#855522";
                this.Inventory[4].HexBackgroundColor = "#855522";
                this.Inventory[6].HexBackgroundColor = "#855522";
            }
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds ownerNameBounds = ElementBounds.Fixed(0.0, 30.0, 150, 25).WithAlignment(EnumDialogArea.CenterTop);
            ElementBounds closeButton = ElementBounds.Fixed(0, 30, 0, 0).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0);

            ElementBounds leftText = ElementBounds.FixedSize(70, 25).FixedUnder(ownerNameBounds, 20);
            ElementBounds rightText = ElementBounds.FixedSize(70, 25).RightOf(leftText, 25);
            rightText.fixedY = leftText.fixedY;

            ElementBounds leftSlots = ElementBounds.FixedSize(100, 230).FixedUnder(leftText, 15);
            ElementBounds rightSlots = ElementBounds.FixedSize(60, 230).FixedRightOf(leftSlots);
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
            //SingleComposer.AddInset(ownerNameBounds);
            SingleComposer.AddStaticText(Lang.Get("canmarket:onchest-block-prices"), CairoFont.WhiteDetailText().WithFontSize(20), leftText);
            SingleComposer.AddStaticText(Lang.Get("canmarket:onchest-block-goods"), CairoFont.WhiteDetailText().WithFontSize(20), rightText);
            /*SingleComposer.AddStaticText(Lang.Get("canmarket:onchest-block-prices"), CairoFont.WhiteDetailText().WithFontSize(20), leftText)
                 .AddStaticText(Lang.Get("canmarket:onchest-block-goods"), CairoFont.WhiteMediumText().WithFontSize(20), rightText)
                 .AddDialogTitleBar(Lang.Get("canmarket:gui-onchesttradeblock-bar"), OnTitleBarCloseClicked);*/
            SingleComposer.AddItemSlotGrid((IInventory)this.Inventory, new Action<object>(((GUIDialogCANMarketOwner)this).DoSendPacket), 1, new int[] { 0, 2, 4, 6 }, leftSlots, "priceSlots");
            SingleComposer.AddItemSlotGrid((IInventory)this.Inventory, new Action<object>(((GUIDialogCANMarketOwner)this).DoSendPacket), 1, new int[] { 1, 3, 5, 7 }, rightSlots, "goodsSlots");

            if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                SingleComposer.AddStaticText(Lang.Get("canmarket:infinite-stocks-info-gui"), CairoFont.WhiteDetailText().WithFontSize(20), InfiniteStocksTextBounds);
                if ((Inventory as InventoryCANMarketOnChest).be.InfiniteStocks)
                {
                    SingleComposer.AddSmallButton(Lang.Get("", Array.Empty<object>()), new ActionConsumable(this.FlipInfiniteStocksState), InfiniteStocksButtonBounds, EnumButtonStyle.Normal);
                    SingleComposer.AddDynamicText(Lang.Get("on"), CairoFont.WhiteDetailText().WithFontSize(20).WithOrientation(EnumTextOrientation.Center), InfiniteStocksButtonBounds, "infinitestocks");
                }
                else
                {
                    SingleComposer.AddSmallButton(Lang.Get("", Array.Empty<object>()), new ActionConsumable(this.FlipInfiniteStocksState), InfiniteStocksButtonBounds, EnumButtonStyle.Normal);
                    SingleComposer.AddDynamicText(Lang.Get("off"), CairoFont.WhiteDetailText().WithFontSize(20).WithOrientation(EnumTextOrientation.Center), InfiniteStocksButtonBounds, "infinitestocks");
                }

                SingleComposer.AddStaticText(Lang.Get("canmarket:store-payment-info-gui"), CairoFont.WhiteDetailText().WithFontSize(20), StorePaymentTextBounds);
                if ((Inventory as InventoryCANMarketOnChest).be.StorePayment)
                {
                    SingleComposer.AddSmallButton(Lang.Get("", Array.Empty<object>()), new ActionConsumable(this.FlipStorePaymentState), StorePaymentButtonBounds, EnumButtonStyle.Normal);
                    SingleComposer.AddDynamicText(Lang.Get("on"), CairoFont.WhiteDetailText().WithFontSize(20).WithOrientation(EnumTextOrientation.Center), StorePaymentButtonBounds, "storepayment");
                }
                else
                {
                    SingleComposer.AddSmallButton(Lang.Get("", Array.Empty<object>()), new ActionConsumable(this.FlipStorePaymentState), StorePaymentButtonBounds, EnumButtonStyle.Normal);
                    SingleComposer.AddDynamicText(Lang.Get("off"), CairoFont.WhiteDetailText().WithFontSize(20).WithOrientation(EnumTextOrientation.Center), StorePaymentButtonBounds, "storepayment");
                }
            }
            
            var slotSize = GuiElementPassiveItemSlot.unscaledSlotSize;
            var slotPaddingSize = GuiElementItemSlotGridBase.unscaledSlotPadding;

            for (int i = 0; i < 4; i++)
            {
                ElementBounds tmpEB = ElementBounds.
                    FixedSize(35, 35).
                    RightOf(rightSlots);
                tmpEB.fixedY = rightSlots.fixedY + i * (slotSize + slotPaddingSize) + 28;

                SingleComposer.AddDynamicText((this.Inventory as InventoryCANMarketOnChest).stocks[i] < 999
                    ? (this.Inventory as InventoryCANMarketOnChest).stocks[i].ToString()
                    : "999+",
                    CairoFont.WhiteSmallText().WithFontSize(13), tmpEB, "stock" + i);
            }
            

           SingleComposer.Compose(true);
        }
        public bool FlipInfiniteStocksState()
        {
            (Inventory as InventoryCANMarketOnChest).be.InfiniteStocks = !(Inventory as InventoryCANMarketOnChest).be.InfiniteStocks;
            var button = SingleComposer.GetDynamicText("infinitestocks");
            if ((Inventory as InventoryCANMarketOnChest).be.InfiniteStocks)
            {
                button.SetNewText("on");
            }
            else
            {
                button.SetNewText("off");
            }
            capi.Network.SendBlockEntityPacket(this.BlockEntityPosition, 1042);
            return true;
        }
        public bool FlipStorePaymentState()
        {
            (Inventory as InventoryCANMarketOnChest).be.StorePayment = !(Inventory as InventoryCANMarketOnChest).be.StorePayment;
            var button = SingleComposer.GetDynamicText("storepayment");
            if ((Inventory as InventoryCANMarketOnChest).be.StorePayment)
            {
                button.SetNewText("on");
            }
            else
            {
                button.SetNewText("off");
            }
            capi.Network.SendBlockEntityPacket(this.BlockEntityPosition, 1043);
            return true;
        }
    }
}
