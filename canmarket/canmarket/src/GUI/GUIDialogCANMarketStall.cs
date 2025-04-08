using Cairo;
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
    public class GUIDialogCANMarketStall: GUIDialogCANMarket
    {
        int selectedStockRow = -1;
        bool newlyOpenMaxStock = true;
        string collectedIntValue;
        string collectedItemShadowCode;
        string selectedSlotForShadow;
        string collectedCreatedStackSize;
        public GUIDialogCANMarketStall(string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, ICoreClientAPI capi) : base(dialogTitle, inventory, blockEntityPos, capi)
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
            /*double elementToDialogPadding = GuiStyle.ElementToDialogPadding;
           var slotsize = GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
           double unscaledSlotPadding = GuiElementItemSlotGridBase.unscaledSlotPadding;*/

            double SSB = (GuiElementPassiveItemSlot.unscaledSlotSize);
            double SSP = (GuiElementItemSlotGridBase.unscaledSlotPadding);
            /*var f = (Inventory as InventoryCANStallWithMaxStocks);
            var g = (Inventory as InventoryCANStall);*/
            BlockEntityContainer be = (Inventory as InventoryCANStallWithMaxStocks).be;
            string ownerUID = (be as IOwnerProvider)?.OwnerGuid ?? "";
            string ownerName = (be as IOwnerProvider)?.OwnerName ?? "";
            bool isAdminShop = (be as IAdminShop).IsAdminShop;
            bool openedByOwner = ownerUID.Equals("") || ownerUID.Equals(capi.World.Player.PlayerUID) && !isAdminShop;

            string green = "#79E02E";
            string grey = "#855522";
            if (openedByOwner)
            {
                for (int i = 0; i < Inventory.Count; i++)
                {
                    if (i != 0 && i != 1 && ((i - 2) % 3 == 0 || (i - 3) % 3 == 0))
                    {
                        this.Inventory[i].HexBackgroundColor = green;
                    }
                }
            }
            else
            {
                for (int i = 0; i < Inventory.Count; i++)
                {
                    if (i != 0 && i != 1 && ((i - 2) % 3 == 0 || (i - 3) % 3 == 0))
                    {
                        this.Inventory[i].HexBackgroundColor = grey;
                    }
                }
            }
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
                string stockString = "";
                if ((be as IStocksContainer).Stocks[i] == -2)
                {
                    stockString = "∞";
                }
                else if ((be as IStocksContainer).Stocks[i] < 999)
                {
                    stockString = (be as IStocksContainer).Stocks[i].ToString();
                }
                else
                {
                    stockString = "999+";
                }
                //stallComposer.AddInset(tmpStockBounds);
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
        public void ComposeMaxSellStocksGui()
        {
            if (!newlyOpenMaxStock && !(this.Composers["maxSellStocks"] == null))
            {
                this.Composers.Remove("maxSellStocks");
                return;
            }

            ElementBounds leftDlgBounds = this.Composers["stallCompo"].Bounds;
            double b = leftDlgBounds.InnerHeight / (double)RuntimeEnv.GUIScale + 40.0;

            ElementBounds bgBounds = ElementBounds.Fixed(0.0, 0.0,
                235, leftDlgBounds.InnerHeight / (double)RuntimeEnv.GUIScale - GuiStyle.ElementToDialogPadding - 20.0 + b).WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds dialogBounds = bgBounds.ForkBoundingParent(0.0, 0.0, 0.0, 0.0)
                .WithAlignment(EnumDialogArea.LeftMiddle)
                .WithFixedAlignmentOffset((leftDlgBounds.renderX + leftDlgBounds.OuterWidth + 10.0) / (double)RuntimeEnv.GUIScale, 0);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            dialogBounds.BothSizing = ElementSizing.FitToChildren;
            dialogBounds.WithChild(bgBounds);
            ElementBounds textBounds = ElementBounds.FixedPos(EnumDialogArea.LeftTop,
                                                               0,
                                                                0).WithFixedSize(180, 250);
            bgBounds.WithChildren(textBounds);

            GuiComposer maxSellStocksComposer;
            this.Composers["maxSellStocks"] = maxSellStocksComposer = this.capi.Gui.CreateCompo("maxSellStocks", dialogBounds).AddShadedDialogBG(bgBounds, false, 5.0, 0.75f);



            //Select max stock sell amount
            ElementBounds whichTradeEB = textBounds.CopyOffsetedSibling().WithFixedHeight(20)
                    .WithFixedWidth(170)
                    .WithFixedPosition(0, 0);
            bgBounds.WithChildren(whichTradeEB);

            maxSellStocksComposer.AddStaticText(Lang.Get("canmarket:stock-slot-set-gui", (selectedStockRow + 1).ToString()), CairoFont.WhiteDetailText(), whichTradeEB);
            TextExtents textExtents = CairoFont.WhiteSmallText().GetTextExtents(Lang.Get("canmarket:this-sets-max-stock-sell-gui"));
            maxSellStocksComposer.AddHoverText(Lang.Get("canmarket:this-sets-max-stock-sell-gui"), CairoFont.WhiteMediumText(), (int)textExtents.Width, whichTradeEB);

            //NUMBER INPUT FOR MAX STOCKS
            ElementBounds inputMaxStackBounds = ElementBounds.FixedSize(80, 30).FixedUnder(whichTradeEB, 25);
            inputMaxStackBounds.fixedX += 10;
            maxSellStocksComposer.AddNumberInput(inputMaxStackBounds,
                (newValueMaxStocksToSell) => {
                    if(int.TryParse(newValueMaxStocksToSell , out int newInt))
                    {
                        if(newInt < 0)
                        {
                            maxSellStocksComposer.GetNumberInput("maxSellStockInput").SetValue(0);
                            collectedIntValue = "0";
                        }
                        else
                        {
                            collectedIntValue = newValueMaxStocksToSell;
                        }
                    }
                    

                }, CairoFont.WhiteDetailText(), "maxSellStockInput");
            //==

            //APPLY MAX STOCKS BUTTON
            ElementBounds applyValueBounds = ElementBounds.FixedSize(60, 30).FixedRightOf(inputMaxStackBounds, 15);
            applyValueBounds.fixedY += inputMaxStackBounds.fixedY;
            maxSellStocksComposer.AddButton(Lang.Get("canmarket:gui-ok"), () =>
            {
                if (this.collectedIntValue != "")
                {
                    if (int.TryParse(collectedIntValue, out var parsedValue))
                    {
                        if (parsedValue < 0)
                        {
                            return false;
                        }
                        byte[] data;
                        using (MemoryStream ms = new MemoryStream())
                        {
                            BinaryWriter writer = new BinaryWriter(ms);
                            writer.Write(selectedStockRow);
                            writer.Write(parsedValue);
                            data = ms.ToArray();
                        }
                        capi.Network.SendBlockEntityPacket(this.BlockEntityPosition, 1044, data);
                        var tmpNumberInput = maxSellStocksComposer.GetNumberInput("maxSellStockInput");
                        tmpNumberInput.SetValue("");
                    }
                }
                return true;
            }, applyValueBounds);
            //==

            //DESC TEXT FOR ITEM SELECTION
            ElementBounds setItemUsingTextBE = ElementBounds.FixedSize(180, 30).FixedUnder(inputMaxStackBounds, 15);
            setItemUsingTextBE.fixedX += inputMaxStackBounds.fixedX;
            maxSellStocksComposer.AddStaticText(Lang.Get("canmarket:set-item-using-text-gui", (selectedStockRow + 1).ToString()), CairoFont.WhiteDetailText(), setItemUsingTextBE);
            //==


            //DROPDOWN TO SELECT IN WHICH SLOT
            ElementBounds itemDropDownSelectSlotBE = ElementBounds.FixedSize(120, 30).FixedUnder(setItemUsingTextBE, 0);
            itemDropDownSelectSlotBE.fixedX += 10;
            itemDropDownSelectSlotBE.WithFixedWidth(60);

            maxSellStocksComposer.AddDropDown(new string[] { "0", "1" }, new string[] { "0", "1" }, 0, (string code, bool selected) =>
            {
                this.selectedSlotForShadow = code;
            }, itemDropDownSelectSlotBE);
            //==

            //SELECT STACKSIZE
            ElementBounds itemStackSizeEB = ElementBounds.FixedSize(120, 30).FixedRightOf(itemDropDownSelectSlotBE, 15);
            itemStackSizeEB.fixedY = itemDropDownSelectSlotBE.fixedY;
            maxSellStocksComposer.AddNumberInput(itemStackSizeEB, 
                (name) => {
                    if (int.TryParse(name, out int newInt))
                    {
                        if (newInt < 1)
                        {
                            maxSellStocksComposer.GetNumberInput("createdStackSize").SetValue(1);
                            collectedCreatedStackSize = "1";
                        }
                        else
                        {
                            collectedCreatedStackSize = name;
                        }

                    }
                    var f = 3;
                }, CairoFont.WhiteDetailText(), "createdStackSize");

            //Create item shadow from code
            ElementBounds itemCodeEnterEB = ElementBounds.FixedSize(120, 30).FixedUnder(itemDropDownSelectSlotBE, 15);
            itemCodeEnterEB.fixedX += 10;

            maxSellStocksComposer.AddTextInput(itemCodeEnterEB, (string input) =>
            {
                collectedItemShadowCode = input;
                return;
            }, key: "itemShadowCodeEnter1");

            ElementBounds itemCodeEnterApplyEB = ElementBounds.FixedSize(60, 30).FixedUnder(itemCodeEnterEB, 15);
            itemCodeEnterApplyEB.fixedX += 10;

            maxSellStocksComposer.AddButton(Lang.Get("canmarket:gui-ok"), () =>
            {
                if (this.collectedItemShadowCode != "")
                {
                    CollectibleObject co = this.capi.World.GetItem(new AssetLocation(collectedItemShadowCode));
                    if (co == null)
                    {
                        co = this.capi.World.GetBlock(new AssetLocation(collectedItemShadowCode));
                    }
                    if (co != null)
                    {
                        ItemStack newItem = new ItemStack(co);
                        if(!int.TryParse(this.selectedSlotForShadow, out var parsedValue))
                        {
                            parsedValue = 0;
                        }
                        int stackSizeForItem = 1;
                        if(collectedCreatedStackSize != null || collectedCreatedStackSize != "")
                        {
                            int.TryParse(collectedCreatedStackSize, out stackSizeForItem);
                        }
                        byte[] data;
                        using (MemoryStream ms = new MemoryStream())
                        {
                            BinaryWriter writer = new BinaryWriter(ms);
                            writer.Write(selectedStockRow);
                            writer.Write(parsedValue);
                            var bytesNow = newItem.ToBytes();
                            writer.Write(bytesNow);
                            writer.Write(stackSizeForItem);
                            data = ms.ToArray();
                        }
                        capi.Network.SendBlockEntityPacket(this.BlockEntityPosition, 1045, data);
                        var tmpNumberInput = maxSellStocksComposer.GetNumberInput("maxSellStockInput");
                        tmpNumberInput.SetValue("");
                    }
                }
                return true;
            }, itemCodeEnterApplyEB);



            maxSellStocksComposer.Compose();
        }
        public bool maxStockButtonClicked(int slotsRow)
        {
            if (slotsRow != selectedStockRow)
            {
                newlyOpenMaxStock = true;
            }
            else
            {
                newlyOpenMaxStock = false;
            }
            selectedStockRow = slotsRow;
            this.capi.Event.EnqueueMainThreadTask(new Action(this.ComposeMaxSellStocksGui), "setupmaxsellstocksdlg");
            return true;
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
