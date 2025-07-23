using Cairo;
using canmarket.src.BE.SupportClasses;
using canmarket.src.helpers.Interfaces;
using canmarket.src.Inventories;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace canmarket.src.GUI
{
    public class GUIDialogCANMarketStall: GUIDialogCANMarketWithMaxStocks
    {

        public GUIDialogCANMarketStall(string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, ICoreClientAPI capi) : base(dialogTitle, inventory, blockEntityPos, capi)
        {
            if (IsDuplicate)
            {
                return;
            }
            capi.World.Player.InventoryManager.OpenInventory((IInventory)inventory);
            SetupDialog();
        }
        public override void SetupDialog()
        {
            BEStall be = (Inventory as InventoryCANStallWithMaxStocks).be;
            string ownerUID = (be as IOwnerProvider)?.OwnerGuid ?? "";
            string ownerName = (be as IOwnerProvider)?.OwnerName ?? "";
            bool isAdminShop = (be as IAdminShop).IsAdminShop;
            bool openedByOwner = ownerUID.Equals("") || ownerUID.Equals(capi.World.Player.PlayerUID) && !isAdminShop;

            SetSlotsColors(openedByOwner);

            int tradesInColumn = 2;
            int columns = (this.Inventory.Count - 2) / 3 / tradesInColumn;
            double mainWindowWidth = SSB * (columns > 1 ? columns - 1 : 2) + columns * (SSB * 3 + SSP * 4);
            double mainWindowHeight = SSB + SSB + tradesInColumn * SSB + (tradesInColumn + 1) * SSP + SSB;


            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);

            ElementBounds ownerNameBounds = ElementBounds.Fixed(0.0, 30.0, 250, 25).WithAlignment(EnumDialogArea.LeftTop);
            ElementBounds closeButton = ElementBounds.Fixed(0, 30, 0, 0).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0);

            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(new ElementBounds[]
             {
                    closeButton
             });
            GuiComposer stallComposer;
            base.SingleComposer = this.Composers["stallCompo"] = stallComposer = capi.Gui.CreateCompo("stallCompo", dialogBounds)
                .AddShadedDialogBG(bgBounds, false)
                .AddDialogTitleBar(Lang.Get("canmarket:gui-stall-bar"), () => TryClose())
                .BeginChildElements(bgBounds);

            if (isAdminShop)
            {
                stallComposer.AddDynamicText(Lang.Get("canmarket:gui-adminshop-name"), CairoFont.WhiteDetailText().WithFontSize(20), ownerNameBounds, "ownerName");
            }
            else
            {
                stallComposer.AddDynamicText(Lang.Get("canmarket:gui-stall-owner", ownerName), CairoFont.WhiteDetailText().WithFontSize(20), ownerNameBounds, "ownerName");
            }

            ElementBounds currentElementBounds = ownerNameBounds;
            ElementBounds previousPriceBounds = ownerNameBounds;
            int maxRaws = 8;
            for (int i = 0; i < (Inventory.Count - 2) / 3; i++)
            {

                if (i % maxRaws == 0)
                {
                    if (i == 0)
                    {
                        ElementBounds tmpPriceBounds = ElementBounds.FixedSize(160, 25).FixedUnder(currentElementBounds, 32);
                        stallComposer.AddStaticText(Lang.Get("canmarket:gui-stall-prices-goods"), CairoFont.WhiteDetailText().WithFontSize(20), tmpPriceBounds);
                        currentElementBounds = tmpPriceBounds;
                        previousPriceBounds = tmpPriceBounds;
                    }
                    else
                    {
                        ElementBounds tmpPriceBounds = ElementBounds.FixedSize(160, 25).FixedRightOf(previousPriceBounds, 32);
                        tmpPriceBounds.fixedY = previousPriceBounds.fixedY;

                        stallComposer.AddStaticText(Lang.Get("canmarket:gui-stall-prices-goods"), CairoFont.WhiteDetailText().WithFontSize(20), tmpPriceBounds);
                        currentElementBounds = tmpPriceBounds;
                        previousPriceBounds = tmpPriceBounds;
                    }
                }
                var tm = new int[] { 2 + i * 3, 3 + i * 3, 4 + i * 3 };



                ElementBounds tmpSlotGridBounds = ElementBounds.FixedSize(152, 48).FixedUnder(currentElementBounds);
                tmpSlotGridBounds.fixedX = currentElementBounds.fixedX;
                stallComposer.AddItemSlotGrid(this.Inventory,
                    new Action<object>((this).DoSendPacket),
                    3,
                    tm,
                    tmpSlotGridBounds,
                    "tradeRaw" + i.ToString());
                currentElementBounds = tmpSlotGridBounds;

                ElementBounds tmpMaxSellBounds = ElementBounds.FixedSize(40, 20).FixedRightOf(currentElementBounds);
                tmpMaxSellBounds.fixedY = currentElementBounds.fixedY;
                //currentElementBounds = tmpMaxSellBounds;

                stallComposer.AddDynamicText((be as IStocksContainer).MaxStocks[i] == -2
                                                  ? "-"
                                                  : (be as IStocksContainer).MaxStocks[i].ToString(),
                                           CairoFont.WhiteDetailText(),
                                           tmpMaxSellBounds,
                                           "maxStock" + i);

                ElementBounds tmpMaxSellSetBounds = ElementBounds.FixedSize(15, 15).FixedRightOf(tmpMaxSellBounds);
                tmpMaxSellSetBounds.fixedY = tmpMaxSellBounds.fixedY;
                if (openedByOwner)
                {
                    int tmpI = i;
                    stallComposer.AddIconButton("right", ((bool t) =>
                    {
                        maxStockButtonClicked(tmpI);
                    }), tmpMaxSellSetBounds, "maxStockButton" + i);
                }
             
                ElementBounds tmpStockBounds = ElementBounds.FixedSize(35, 17).FixedRightOf(currentElementBounds);
                tmpStockBounds.fixedY = currentElementBounds.fixedY + 30;
                string stockString = GetStockAmountToShow(this.Inventory[i * 3 + 4].Itemstack, (be as IStocksContainer).Stocks[i]);

                GetStockAmountToShow(this.Inventory[i * 3 + 4].Itemstack, (be as IStocksContainer).Stocks[i]);
               
                stallComposer.AddDynamicText(stockString, CairoFont.WhiteDetailText(), tmpStockBounds, "stock" + i);
            }

            if (openedByOwner)
            {
                ElementBounds booksBounds = ElementBounds.FixedSize(162, 48).FixedUnder(currentElementBounds, 48);
                stallComposer.AddItemSlotGrid(this.Inventory,
                     new Action<object>((this).DoSendPacket),
                     2,
                     new int[] { 0, 1 },
                     booksBounds,
                     "books");
                currentElementBounds = booksBounds;

                ElementBounds freshnessTextEB = ElementBounds.FixedSize(250, 24).FixedUnder(currentElementBounds, 48);
                stallComposer.AddStaticText(Lang.Get("canmarket:gui-freshness-text"), CairoFont.SmallButtonText(), freshnessTextEB);
                //stallComposer.AddInset(freshnessTextEB);
                currentElementBounds = freshnessTextEB;

                ElementBounds freshnessSliderEB = ElementBounds.FixedSize(162, 24).FixedUnder(currentElementBounds, 10);
                currentElementBounds = freshnessSliderEB;
                stallComposer.AddSlider(onFreshnessPercentChange, freshnessSliderEB, "freshnessSlider");
                stallComposer.GetSlider("freshnessSlider")?.SetValue((int)(be.CurrentFreshnessThreshold * 100));
            }


            if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                bool infiniteStocks = (be as IAdminShop).ProvidesInfiniteStocks;
                bool storePayment = (be as IAdminShop).MustStorePayment;
                ElementBounds settingsBounds = ElementBounds.FixedSize(150, 25).FixedUnder(currentElementBounds, 48);
                ElementBounds settingsButtonBounds = ElementBounds.FixedSize(50, 25).FixedRightOf(settingsBounds, 24);
                settingsButtonBounds.fixedY = settingsBounds.fixedY;
                currentElementBounds = settingsBounds;
                stallComposer.AddStaticText(Lang.Get("canmarket:infinite-stocks-info-gui"), CairoFont.WhiteDetailText().WithFontSize(20), settingsBounds);
                SingleComposer.AddSwitch(FlipInfiniteStocksState, settingsButtonBounds, "infinitestockstoggle");
                SingleComposer.GetSwitch("infinitestockstoggle")?.SetValue(infiniteStocks);


                settingsBounds = ElementBounds.FixedSize(150, 48).FixedUnder(currentElementBounds, 25);
                settingsButtonBounds = ElementBounds.FixedSize(50, 25).FixedRightOf(settingsBounds, 24);
                settingsButtonBounds.fixedY = settingsBounds.fixedY;

                stallComposer.AddStaticText(Lang.Get("canmarket:store-payment-info-gui"), CairoFont.WhiteDetailText().WithFontSize(20), settingsBounds);
                SingleComposer.AddSwitch(FlipStorePaymentState, settingsButtonBounds, "storepaymenttoggle");
                SingleComposer.GetSwitch("storepaymenttoggle")?.SetValue(storePayment);
            }
            //ComposeMaxSellStocksGui();
            stallComposer.Compose();
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
