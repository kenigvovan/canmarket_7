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
            if(ownerName.Equals(capi.World.Player.PlayerName))
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
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            // Just a simple 300x300 pixel box
            ElementBounds textBounds = ElementBounds.Fixed(0, 20, 230, 310);
            ElementBounds ownerText = ElementBounds.FixedPos(EnumDialogArea.CenterTop, 0, 0).WithFixedHeight(20.0).WithFixedWidth(200);
            textBounds.WithChild(ownerText);
            //textBounds.BothSizing = ElementSizing.FitToChildren;
            // Background boundaries. Again, just make it fit it's child elements, then add the text as a child element
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.WithChildren(textBounds);
            ElementBounds leftPart = ElementBounds.FixedPos(EnumDialogArea.LeftTop, 0, 40).WithFixedHeight(250.0).WithFixedWidth(80);
            textBounds.WithChild(leftPart);
            ElementBounds leftText = ElementBounds.FixedPos(EnumDialogArea.CenterTop, 0, 0).WithFixedHeight(20.0).WithFixedWidth(65);
            leftPart.WithChild(leftText);
           // leftPart.BothSizing = ElementSizing.FitToChildren;
            ElementBounds rightPart = ElementBounds.FixedPos(EnumDialogArea.RightTop, 0, 60).WithFixedHeight(250.0).WithFixedWidth(120);
            bgBounds.WithChild(rightPart);


            ElementBounds rightText = ElementBounds.FixedPos(EnumDialogArea.CenterTop, 0, 0).WithFixedHeight(20.0).WithFixedWidth(65);
            rightPart.WithChild(rightText);
           // rightPart.BothSizing = ElementSizing.FitToChildren;
            ElementBounds leftSlots = ElementBounds.FixedPos(EnumDialogArea.CenterTop, 0, 30).WithFixedHeight(200.0).WithFixedWidth(50);
            ElementBounds rightSlots = ElementBounds.FixedPos(EnumDialogArea.CenterTop, 0, 30).WithFixedHeight(200.0).WithFixedWidth(50);
            leftPart.WithChild(leftSlots);
            // bgBounds.WithChild(leftText);
            // bgBounds.WithChild(rightText);
            rightPart.WithChild(rightSlots);
           
            

            // Lastly, create the dialog
            SingleComposer = capi.Gui.CreateCompo("marketCompo", dialogBounds)
                .AddShadedDialogBG(bgBounds, false)
                
            ;
            //SingleComposer.AddInset(leftPart);
            //SingleComposer.AddInset(textBounds);
            SingleComposer.AddInset(rightPart);
            SingleComposer.AddInset(leftPart);
            string additionalStr = capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative ? (((Inventory as InventoryCANMarketOnChest).be.InfiniteStocks ? "(IS)" : "")) : "";
            if(!(Inventory as InventoryCANMarketOnChest).be.StorePayment)
            {
                additionalStr += "(-SP)";
            }
            if ((Inventory as InventoryCANMarketOnChest)?.be?.ownerName != null)
            {            
                SingleComposer.AddStaticText((Inventory as InventoryCANMarketOnChest).be?.ownerName + additionalStr, CairoFont.WhiteDetailText().WithFontSize(20), ownerText);
            }
           //SingleComposer.AddInset(ownerText);
            //SingleComposer.AddInset(text);
            SingleComposer.AddStaticText(Lang.Get("canmarket:onchest-block-prices"), CairoFont.WhiteDetailText().WithFontSize(20), leftText)
                .AddStaticText(Lang.Get("canmarket:onchest-block-goods"), CairoFont.WhiteMediumText().WithFontSize(20), rightText)
                .AddDialogTitleBar(Lang.Get("canmarket:gui-onchesttradeblock-bar"), OnTitleBarCloseClicked); ;
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            int[] intArr = new int[Inventory.Count];
            for (int i = 0; i < intArr.Length; i++)
            {
                intArr[i] = i;
            }
           
            SingleComposer.AddItemSlotGrid((IInventory)this.Inventory, new Action<object>(((GUIDialogCANMarketOwner)this).DoSendPacket), 1, new int[] {0, 2, 4, 6} , leftSlots, "priceSlots");
            SingleComposer.AddItemSlotGrid((IInventory)this.Inventory, new Action<object>(((GUIDialogCANMarketOwner)this).DoSendPacket), 1, new int[] {1, 3, 5, 7 }, rightSlots, "goodsSlots");
            var slotSize = GuiElementPassiveItemSlot.unscaledSlotSize;
            var slotPaddingSize = GuiElementItemSlotGridBase.unscaledSlotPadding;
            ElementBounds rightSlotsStocks = ElementBounds.FixedPos(EnumDialogArea.RightTop, 0, 30).WithFixedHeight(200.0).WithFixedWidth(25);
            rightPart.WithChild(rightSlotsStocks);
            //SingleComposer.AddInset(rightSlotsStocks);
            
            for (int i =0; i < 4; i++)
            {
                ElementBounds tmpEB = ElementBounds.FixedPos(EnumDialogArea.LeftTop, -8, 3 + i * (slotSize + slotPaddingSize) + 25).WithFixedHeight(200.0).WithFixedWidth(35);
                rightSlotsStocks.WithChild(tmpEB);
                SingleComposer.AddDynamicText((this.Inventory as InventoryCANMarketOnChest).stocks[i] < 999 
                    ? (this.Inventory as InventoryCANMarketOnChest).stocks[i].ToString()
                    : "999+",
                    CairoFont.WhiteSmallText().WithFontSize(13), tmpEB, "stock" + i);               
            }
            

            // SingleComposer.AddItemSlotGrid((IInventory)this.Inventory, new Action<object>(((GUIDialogCANMarketOwner)this).DoSendPacket), 1, new int[1]
            //         {
            //             0
            //             }, textBounds, "encrusteditem");
            SingleComposer.Compose();
            return;
            ElementBounds elementBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds bounds1 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds bounds2 = ElementBounds.FixedPos(EnumDialogArea.LeftTop, 100, 40).WithFixedHeight(30.0).WithFixedWidth(140);

            //elementBounds.WithChild(bounds1);
            elementBounds.WithChild(bounds2);
            SingleComposer.AddItemSlotGrid((IInventory)this.Inventory, new Action<object>(((GUIDialogCANMarketOwner)this).DoSendPacket), 1, new int[1]
                            {
                                0
                            }, bounds2, "encrusteditem");

            this.SingleComposer = this.capi.Gui.CreateCompo("canmarketcompo-" + this.BlockEntityPosition?.ToString(), elementBounds).
                  AddShadedDialogBG(bounds1);
           // bounds1.WithChildren(bounds2, bounds3, bounds4);

            
                //gems input

                double elementToDialogPadding = GuiStyle.ElementToDialogPadding;
                double unscaledSlotPadding = GuiElementItemSlotGridBase.unscaledSlotPadding;
                ElementBounds elementBoundsbig = ElementStdBounds.SlotGrid(EnumDialogArea.None, 100.0, 40.0, 4, 3).FixedGrow(unscaledSlotPadding);



                SingleComposer.AddItemSlotGrid((IInventory)this.Inventory, new Action<object>(((GUIDialogCANMarketOwner)this).DoSendPacket), 1, new int[1]
                            {
                                0
                            }, bounds1, "encrusteditem");
               
                intArr = new int[Inventory.Count];
                for (int i = 0; i < intArr.Length; i++)
                {
                    intArr[i] = i + 1;
                }
                SingleComposer.AddItemSlotGrid((IInventory)this.Inventory, new Action<object>(((GUIDialogCANMarketOwner)this).DoSendPacket), 4, intArr, bounds1, "socketsslots");
                            
                        
                    
                
                SingleComposer.AddInset(bounds1);
               

            
            SingleComposer.Compose();
            this.SingleComposer.UnfocusOwnElements();
        }
        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }
    }
}
