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
    public class GUIDialogCANStall: GuiDialogBlockEntity
    {
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

            // Background boundaries. Again, just make it fit it's child elements, then add the text as a child element
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);

            //Main gui box
            ElementBounds tradeSlotsBounds = ElementBounds.FixedPos(EnumDialogArea.LeftBottom, 0, 100)
                .WithFixedWidth(mainWindowWidth)
                .WithFixedHeight(mainWindowHeight);
            ElementBounds textBounds1 = ElementBounds.FixedPos(EnumDialogArea.LeftTop, 45, 60)
               .WithFixedWidth(mainWindowWidth)
               .WithFixedHeight(20);
            ElementBounds ownerNameBounds = ElementBounds.FixedPos(EnumDialogArea.LeftTop, 45, 20)
                           .WithFixedWidth(mainWindowWidth)
                           .WithFixedHeight(25);
            //Name of owner
            //ElementBounds ownerText = ElementBounds.FixedPos(EnumDialogArea.CenterTop, 0, 0).WithFixedHeight(20.0).WithFixedWidth(200);
            //textBounds1.WithChild(ownerText);


            // ElementBounds tradeSlotsBounds = ElementBounds.FixedPos(EnumDialogArea.CenterTop, 10, 10).WithFixedWidth(600).WithFixedHeight(500);
            bgBounds.WithChildren( tradeSlotsBounds, textBounds1, ownerNameBounds);
           
            // mainBounds.WithChild(tradeSlotsBounds);


            bgBounds.BothSizing = ElementSizing.FitToChildren;
            // Lastly, create the dialog
            SingleComposer = capi.Gui.CreateCompo("stallCompo", dialogBounds)
                .AddShadedDialogBG(bgBounds, false)
                .AddDialogTitleBar(Lang.Get("canmarket:gui-stall-bar"), OnTitleBarCloseClicked);

            string additionalStr = "";
            if (openedByOwner)
            {
                additionalStr = capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative ? (((Inventory as InventoryCANStall).be.InfiniteStocks ? "(IS)" : "")) : "";
                if (!(Inventory as InventoryCANStall).be.StorePayment)
                {
                    additionalStr += "(-SP)";
                }
            }
            if ((Inventory as InventoryCANStall).be.adminShop)
            {
                SingleComposer.AddDynamicText(Lang.Get("canmarket:gui-adminshop-name"), CairoFont.WhiteDetailText().WithFontSize(20), ownerNameBounds, "ownerName");
                //SingleComposer.AddStaticText(Lang.Get("canmarket:gui-adminshop-name", (Inventory as InventoryCANStall).be?.ownerName), CairoFont.WhiteDetailText().WithFontSize(20), ownerNameBounds);
            }
           else
            {
                SingleComposer.AddDynamicText(Lang.Get("canmarket:gui-stall-owner", (Inventory as InventoryCANStall).be.ownerName) + additionalStr, CairoFont.WhiteDetailText().WithFontSize(20), ownerNameBounds, "ownerName");
                //SingleComposer.AddStaticText(Lang.Get("canmarket:gui-stall-owner", (Inventory as InventoryCANStall).be?.ownerName), CairoFont.WhiteDetailText().WithFontSize(20), ownerNameBounds);
            }
            
                

            //SingleComposer.AddInset(bgBounds);
            int maxRaws = 8;
            int curColumn = 0;
            //List<ElementBounds> tmBounds = new List<ElementBounds>();
            for(int i = 0; i < columns; i++)
            {
                var textEl = ElementBounds.FixedPos(EnumDialogArea.LeftTop, tradeSlotsBounds.fixedX + 30 + (i * (162 + 40)), -30)
                   .WithFixedWidth((162))
                .WithFixedHeight(48);
                //tmBounds.Add(tmp); 
                tradeSlotsBounds.WithChild(textEl);
                SingleComposer.AddStaticText(Lang.Get("canmarket:gui-stall-prices-goods"), CairoFont.WhiteDetailText().WithFontSize(20), textEl);
            }
           
            for (int i = 0; i < (Inventory.Count - 2) / 3; i++)
            {
                if (i != 0 && i % maxRaws == 0)
                {
                    curColumn++;
                }
                var tm = new int[] { 2 + i * 3, 3 + i * 3, 4 + i * 3 };
                /*var tmp = ElementBounds.FixedPos(EnumDialogArea.LeftBottom, tradeSlotsBounds.fixedX + 30 + curColumn * 200, (i % maxRaws) * 60)
                .WithFixedWidth(200)
                .WithFixedHeight(40);*/
                var tmp = ElementBounds.FixedPos(EnumDialogArea.LeftTop, tradeSlotsBounds.fixedX + 30 + curColumn * 200, (i % maxRaws) * 60)
                    .WithFixedWidth(((162)))
                 .WithFixedHeight(48);
                //tmBounds.Add(tmp); 
                tradeSlotsBounds.WithChild(tmp);
                SingleComposer.AddItemSlotGrid(this.Inventory,
                    new Action<object>((this).DoSendPacket),
                    3,
                    tm,
                    tmp,
                    "tradeRaw" + i.ToString() );
                ElementBounds tmpEB = ElementBounds.FixedPos(EnumDialogArea.LeftFixed, tradeSlotsBounds.fixedX + 25 + curColumn * 200 + 165, (i % maxRaws) * 60 + 25).WithFixedHeight(GuiElement.scaled((200.0))).WithFixedWidth(35);
                tradeSlotsBounds.WithChild(tmpEB);
                SingleComposer.AddDynamicText((this.Inventory as InventoryCANStall).be.stocks[i] < 999
                    ? (this.Inventory as InventoryCANStall).be.stocks[i].ToString()
                    : "999+", CairoFont.WhiteDetailText(), tmpEB, "stock" + i);
                
            }
            if (openedByOwner) 
            {
                ElementBounds booksBounds = ElementBounds.FixedPos(EnumDialogArea.LeftBottom, 0, 0)
                   .WithFixedWidth(mainWindowWidth - 40)
                   .WithFixedHeight(40);
                tradeSlotsBounds.WithChild(booksBounds);
                SingleComposer.AddItemSlotGrid(this.Inventory,
                    new Action<object>((this).DoSendPacket),
                    2,
                    new int[] { 0, 1 },
                    booksBounds,
                    "books");
            }
            


            /*
              * SingleComposer.AddItemSlotGrid((IInventory)this.Inventory, new Action<object>(((GUIDialogCANMarketOwner)this).DoSendPacket), 1, new int[] { 0, 2, 4, 6 }, leftSlots, "priceSlots");
            SingleComposer.AddItemSlotGrid((IInventory)this.Inventory, new Action<object>(((GUIDialogCANMarketOwner)this).DoSendPacket), 1, new int[] { 1, 3, 5, 7 }, rightSlots, "goodsSlots");
            
            */
            SingleComposer.Compose();
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
