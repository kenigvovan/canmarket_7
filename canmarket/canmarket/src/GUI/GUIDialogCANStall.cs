using canmarket.src.Inventories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using static System.Net.Mime.MediaTypeNames;

namespace canmarket.src.GUI
{
    public class GUIDialogCANStall: GuiDialogBlockEntity
    {
        int selectedStockRow = -1;
        bool newlyOpenMaxStock = true;
        string collectedIntValue;
        public GUIDialogCANStall(string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, ICoreClientAPI capi) : base(dialogTitle, inventory, blockEntityPos, capi)
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
            string ownerName = (Inventory as InventoryCANStall)?.be?.ownerName;
            bool openedByOwner = ownerName.Equals("") || ownerName.Equals(capi.World.Player.PlayerName) && !(Inventory as InventoryCANStall).be.adminShop;
            string green = "#79E02E";
            string grey = "#855522";
            if (openedByOwner)
            {
                for(int i = 0; i < Inventory.Count; i++)
                {
                    if(i != 0 && i != 1 && ((i - 2) % 3 == 0 || (i - 3) % 3 == 0))
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
            int tradesInColumn = 8;
            int columns = (this.Inventory.Count - 2) / 3 / tradesInColumn;
            double mainWindowWidth = SSB * (columns > 1 ? columns - 1 : 2) + columns * (SSB * 3 + SSP * 4);
            double mainWindowHeight = SSB + SSB + tradesInColumn * SSB + (tradesInColumn + 1) * SSP + SSB;


            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);

            ElementBounds ownerNameBounds = ElementBounds.Fixed(0.0, 30.0, 350, 25).WithAlignment(EnumDialogArea.LeftTop);
            ElementBounds closeButton = ElementBounds.Fixed(0, 30, 0, 0).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0);

            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(new ElementBounds[]
             {
                    closeButton
             });
            GuiComposer stallComposer;
            base.SingleComposer = this.Composers["stallCompo"] = stallComposer = capi.Gui.CreateCompo("stallCompo", dialogBounds)
                .AddShadedDialogBG(bgBounds, false)
                .AddDialogTitleBar(Lang.Get("canmarket:gui-stall-bar"), OnTitleBarCloseClicked)
                .BeginChildElements(bgBounds);

            if ((Inventory as InventoryCANStall).be.adminShop)
            {
                stallComposer.AddDynamicText(Lang.Get("canmarket:gui-adminshop-name"), CairoFont.WhiteDetailText().WithFontSize(20), ownerNameBounds, "ownerName");
            }
           else
            {
                stallComposer.AddDynamicText(Lang.Get("canmarket:gui-stall-owner", (Inventory as InventoryCANStall).be.ownerName), CairoFont.WhiteDetailText().WithFontSize(20), ownerNameBounds, "ownerName");
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
                if (openedByOwner)
                {
                    int tmpI = i;
                    stallComposer.AddSmallButton("", new ActionConsumable(() =>
                    {
                        maxStockButtonClicked(tmpI);
                        return true;
                    }), tmpMaxSellBounds, EnumButtonStyle.Normal, "maxStockButton" + i);
                }
                
                
                 stallComposer.AddDynamicText((this.Inventory as InventoryCANStall).be.maxStocks[i] == -2 
                                                    ? "-"
                                                    : (this.Inventory as InventoryCANStall).be.maxStocks[i].ToString(),
                                             CairoFont.WhiteDetailText(),
                                             tmpMaxSellBounds,
                                             "maxStock" + i);
                

                ElementBounds tmpStockBounds = ElementBounds.FixedSize(35, 17).FixedRightOf(currentElementBounds);
                tmpStockBounds.fixedY = currentElementBounds.fixedY + 30;
                string stockString = "";
                if ((this.Inventory as InventoryCANStall).be.stocks[i] == -2)
                {
                    stockString = "∞";
                }
                else if((this.Inventory as InventoryCANStall).be.stocks[i] < 999)
                {
                    stockString = (this.Inventory as InventoryCANStall).be.stocks[i].ToString();
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
                ElementBounds settingsBounds = ElementBounds.FixedSize(150, 25).FixedUnder(currentElementBounds, 48);
                ElementBounds settingsButtonBounds = ElementBounds.FixedSize(50, 25).FixedRightOf(settingsBounds, 24);
                settingsButtonBounds.fixedY = settingsBounds.fixedY;
                currentElementBounds = settingsBounds;
                stallComposer.AddStaticText(Lang.Get("canmarket:infinite-stocks-info-gui"), CairoFont.WhiteDetailText().WithFontSize(20), settingsBounds);
                if ((Inventory as InventoryCANStall).be.InfiniteStocks)
                {
                    stallComposer.AddSmallButton(Lang.Get("", Array.Empty<object>()), new ActionConsumable(this.FlipInfiniteStocksState), settingsButtonBounds, EnumButtonStyle.Normal);
                    stallComposer.AddDynamicText(Lang.Get("on"), CairoFont.WhiteDetailText().WithFontSize(20).WithOrientation(EnumTextOrientation.Center), settingsButtonBounds, "infinitestocks");
                }
                else
                {
                    stallComposer.AddSmallButton(Lang.Get("", Array.Empty<object>()), new ActionConsumable(this.FlipInfiniteStocksState), settingsButtonBounds, EnumButtonStyle.Normal);
                    stallComposer.AddDynamicText(Lang.Get("off"), CairoFont.WhiteDetailText().WithFontSize(20).WithOrientation(EnumTextOrientation.Center), settingsButtonBounds, "infinitestocks");
                }


                settingsBounds = ElementBounds.FixedSize(162, 48).FixedUnder(currentElementBounds, 48);
                settingsButtonBounds = ElementBounds.FixedSize(50, 25).FixedRightOf(settingsBounds, 24);
                settingsButtonBounds.fixedY = settingsBounds.fixedY;
                stallComposer.AddStaticText(Lang.Get("canmarket:store-payment-info-gui"), CairoFont.WhiteDetailText().WithFontSize(20), settingsBounds);
                if ((Inventory as InventoryCANStall).be.StorePayment)
                {
                    stallComposer.AddSmallButton(Lang.Get("", Array.Empty<object>()), new ActionConsumable(this.FlipStorePaymentState), settingsButtonBounds, EnumButtonStyle.Normal);
                    stallComposer.AddDynamicText(Lang.Get("on"), CairoFont.WhiteDetailText().WithFontSize(20).WithOrientation(EnumTextOrientation.Center), settingsButtonBounds, "storepayment");
                }
                else
                {
                    stallComposer.AddSmallButton(Lang.Get("", Array.Empty<object>()), new ActionConsumable(this.FlipStorePaymentState), settingsButtonBounds, EnumButtonStyle.Normal);
                    stallComposer.AddDynamicText(Lang.Get("off"), CairoFont.WhiteDetailText().WithFontSize(20).WithOrientation(EnumTextOrientation.Center), settingsButtonBounds, "storepayment");
                }
            }


            //ComposeMaxSellStocksGui();
            stallComposer.Compose();
        }
        public bool maxStockButtonClicked(int slotsRow)
        {
            if(slotsRow != selectedStockRow)
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
        public void ComposeMaxSellStocksGui()
        {
            //make new composer
            //add button
            //add number input with number of slot
            //on ok we check if out slot is not empty, and set max
            //on out slot change we remove max sell output
            //return;
            if(!newlyOpenMaxStock && !(this.Composers["maxSellStocks"] == null))
            {
                this.Composers.Remove("maxSellStocks");
                //this.capi.Event.EnqueueMainThreadTask(new Action(this.SetupDialog), "setupjewelersetdlg");
                //this.capi.Event.EnqueueMainThreadTask(new Action(this.ComposeMaxSellStocksGui), "setupavailabletypesdlg");
                //SetupDialog();
                return;
            }
            //return;
            //if(this.c)
            ElementBounds leftDlgBounds = this.Composers["stallCompo"].Bounds;
            double b = leftDlgBounds.InnerHeight / (double)RuntimeEnv.GUIScale + 40.0;

            //ElementBounds elementBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightBottom);
            //ElementBounds backgroundBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding).WithFixedSize(Width, Height);
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
                                                                0).WithFixedSize(80, 90);
            //.WithFixedHeight(leftDlgBounds.InnerHeight)
            //.WithFixedWidth(leftDlgBounds.InnerWidth / 2);
            bgBounds.WithChildren(textBounds);

            //SingleComposer.AddStaticText("hello", CairoFont.WhiteDetailText(), bgBounds);
            GuiComposer maxSellStocksComposer;
            this.Composers["maxSellStocks"] = maxSellStocksComposer = this.capi.Gui.CreateCompo("maxSellStocks", dialogBounds).AddShadedDialogBG(bgBounds, false, 5.0, 0.75f);

            ElementBounds el = textBounds.CopyOffsetedSibling().WithFixedHeight(20)
                    .WithFixedWidth(100)
                    .WithFixedPosition(0, 0);
            bgBounds.WithChildren(el);

            maxSellStocksComposer.AddStaticText(Lang.Get("canmarket:stock-slot-set-gui",  selectedStockRow.ToString()), CairoFont.WhiteDetailText(), el);


            ElementBounds inputMaxStackBounds = ElementBounds.FixedSize(80, 30).FixedUnder(el, 25);
            inputMaxStackBounds.fixedX += 10;
            maxSellStocksComposer.AddNumberInput(inputMaxStackBounds, (name) => collectedIntValue = name, CairoFont.WhiteDetailText(), "maxSellStockInput");

            ElementBounds applyValueBounds = ElementBounds.FixedSize(80, 30).FixedUnder(inputMaxStackBounds, 15);
            applyValueBounds.fixedX += 10;
            maxSellStocksComposer.AddButton(Lang.Get("canmarket:gui-ok"), () =>
            {
                if (this.collectedIntValue != "")
                {
                    if(int.TryParse(collectedIntValue, out var parsedValue))
                    {
                        if(parsedValue < 0)
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
                    //this.collectedIntValue = "";
                }
                return true;
            }, applyValueBounds);
            maxSellStocksComposer.Compose();
            //this.Composers["maxSellStocks"].Compose();
            /*GuiComposer maxStocksComposer;
            ElementBounds leftDlgBounds = this.Composers["stallCompo"].Bounds;
            double b = leftDlgBounds.InnerHeight / (double)RuntimeEnv.GUIScale + 10.0;
            ElementBounds bgBounds = ElementBounds.Fixed(0.0, 0.0,
                235, leftDlgBounds.InnerHeight / (double)RuntimeEnv.GUIScale - GuiStyle.ElementToDialogPadding - 20.0 + b).WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds dialogBounds = bgBounds.ForkBoundingParent(0.0, 0.0, 0.0, 0.0)
                .WithAlignment(EnumDialogArea.LeftMiddle)
                .WithFixedAlignmentOffset((leftDlgBounds.renderX + leftDlgBounds.OuterWidth + 10.0) / (double)RuntimeEnv.GUIScale, 0);
            this.Composers["maxSellStocks"] = maxStocksComposer = capi.Gui.CreateCompo("maxSellStocks", leftDlgBounds)
                .AddShadedDialogBG(bgBounds, false)
                .AddDialogTitleBar(Lang.Get("canmarket:gui-stall-bar"), OnTitleBarCloseClicked)
                .BeginChildElements(bgBounds);

            bgBounds.BothSizing = ElementSizing.FitToChildren;

            dialogBounds.BothSizing = ElementSizing.FitToChildren;
            dialogBounds.WithChild(bgBounds);
            ElementBounds textBounds = ElementBounds.FixedPos(EnumDialogArea.LeftTop,
                                                               0,
                                                                0).WithFixedSize(30, 30);

            maxStocksComposer.AddStaticText("hello", CairoFont.WhiteDetailText(), textBounds);
            bgBounds.WithChildren(textBounds);
            maxStocksComposer.Compose();*/
        }
        public bool FlipInfiniteStocksState()
        {
            (Inventory as InventoryCANStall).be.InfiniteStocks = !(Inventory as InventoryCANStall).be.InfiniteStocks;
            var button = this.Composers["stallCompo"].GetDynamicText("infinitestocks");
            if ((Inventory as InventoryCANStall).be.InfiniteStocks)
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
            (Inventory as InventoryCANStall).be.StorePayment = !(Inventory as InventoryCANStall).be.StorePayment;
            var button = this.Composers["stallCompo"].GetDynamicText("storepayment");
            if ((Inventory as InventoryCANStall).be.StorePayment)
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
        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }
        public override void Dispose()
        {
            base.Dispose();
            //this.SingleComposer.Dispose();
        }
    }
}
