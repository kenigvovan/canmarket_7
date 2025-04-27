using Cairo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace canmarket.src.GUI
{
    public class GUIDialogCANMarketWithMaxStocks : GUIDialogCANMarket
    {
        public int selectedStockRow = -1;
        public bool newlyOpenMaxStock = true;
        public string collectedIntValue;
        public string collectedItemShadowCode;
        public string selectedSlotForShadow;
        public string collectedCreatedStackSize;
        public float freshnessPercent = 1f;
        protected long SliderCallbackId = 0;
        public GUIDialogCANMarketWithMaxStocks(string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, ICoreClientAPI capi) 
            : base(dialogTitle, inventory, blockEntityPos, capi)
        {
        }
        public void SetSlotsColors(bool openedByOwner)
        {
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
                    if (int.TryParse(newValueMaxStocksToSell, out int newInt))
                    {
                        if (newInt < 0)
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
                if (this.collectedItemShadowCode != "" && this.collectedItemShadowCode != null)
                {
                    CollectibleObject co = this.capi.World.GetItem(new AssetLocation(collectedItemShadowCode));
                    if (co == null)
                    {
                        co = this.capi.World.GetBlock(new AssetLocation(collectedItemShadowCode));
                    }
                    if (co != null)
                    {
                        ItemStack newItem = new ItemStack(co);
                        if (!int.TryParse(this.selectedSlotForShadow, out var parsedValue))
                        {
                            parsedValue = 0;
                        }
                        int stackSizeForItem = 1;
                        if (collectedCreatedStackSize != null || collectedCreatedStackSize != "")
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
        public bool onFreshnessPercentChange(int value)
        {
            this.freshnessPercent = (float)value / 100f;
            if (this.SliderCallbackId == 0)
            {
                this.SliderCallbackId = this.capi.Event.RegisterCallback((float v) =>
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        BinaryWriter writer = new BinaryWriter(ms);
                        writer.Write(this.freshnessPercent);
                        this.capi.Network.SendBlockEntityPacket(this.BlockEntityPosition, 1046, ms.ToArray());
                    }
                    SliderCallbackId = 0;
                }, 2000);
            }
            return true;
        }
    }
}
