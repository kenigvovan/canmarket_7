using canmarket.src.Blocks;
using canmarket.src.GUI;
using canmarket.src.helpers.Interfaces;
using canmarket.src.Inventories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace canmarket.src.BE.SupportClasses
{
    public class BEStall : BlockEntityContainer, IStocksContainer, IOwnerProvider, IAdminShop, IStoreChestsSources, IWriteSoldLog
    {
        public InventoryCANStallWithMaxStocks inventory;
        public string ownerName;
        public string ownerUID;
        public bool adminShop = false;
        public bool InfiniteStocks = false;
        public bool StorePayment = true;
        public int[] stocks;
        public int[] maxStocks;
        public int quantitySlots = 14;
        protected BlockFacing facing;
        public GUIDialogCANMarket guiMarket;
        public HashSet<Vec3i> chestsCoords;
        protected Dictionary<string, Dictionary<string, int>> soldLog = new Dictionary<string, Dictionary<string, int>>();
        public override InventoryBase Inventory => throw new NotImplementedException();

        public override string InventoryClassName => throw new NotImplementedException();
        public bool IsAdminShop { get => adminShop; set => adminShop = value; }
        public bool MustStorePayment { get => StorePayment; set => StorePayment = value; }
        public bool ProvidesInfiniteStocks { get => InfiniteStocks; set => InfiniteStocks = value; }
        public string OwnerGuid { get => ownerUID; set => ownerUID = value; }
        public string OwnerName { get => ownerName; set => ownerName = value; }
        public int[] Stocks { get => stocks; set => stocks = value; }
        public int[] MaxStocks { get => maxStocks; set => maxStocks = value; }
        public HashSet<Vec3i> ChestsPositions { get => chestsCoords; set => chestsCoords = value; }

        public void updateGuiOwner()
        {
            if (adminShop)
            {
                this.guiMarket.Composers["stallCompo"].GetDynamicText("ownerName").SetNewText(Lang.Get("canmarket:gui-adminshop-name", ownerName));
            }
            else
            {
                this.guiMarket.Composers["stallCompo"].GetDynamicText("ownerName").SetNewText(Lang.Get("canmarket:gui-stall-owner", ownerName));
            }

        }

        public void OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (Api.Side == EnumAppSide.Client)
            {
                toggleInventoryDialogClient(byPlayer, blockSel);
            }
        }
        protected void toggleInventoryDialogClient(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (guiMarket == null)
            {
                ICoreClientAPI capi = Api as ICoreClientAPI;

                foreach (var it in byPlayer.InventoryManager.OpenedInventories)
                {
                    if (it is InventoryCANStall)
                    {
                        ((it as InventoryCANStall).be as BECANStall).guiMarket?.TryClose();
                        byPlayer.InventoryManager.CloseInventory(it);
                        //(it as InventoryCANStall).be
                        capi.Network.SendBlockEntityPacket((it as InventoryCANStall).be.Pos, 1001);
                        // capi.Network.SendPacketClient(it.Close(byPlayer));
                        break;
                    }
                    else if (it is InventoryCANMarketOnChest)
                    {
                        ((it as InventoryCANMarketOnChest).be as BEMarket).guiMarket?.TryClose();
                        byPlayer.InventoryManager.CloseInventory(it);
                        capi.Network.SendBlockEntityPacket((it as InventoryCANMarketOnChest).be.Pos, 1001);
                        //capi.Network.SendPacketClient(it.Close(byPlayer));
                        break;
                    }
                }
                if (blockSel.Block is BlockCANMarket)
                {
                    guiMarket = new GUIDialogCANMarketOwner("trade", Inventory, Pos, Api as ICoreClientAPI);
                }
                else if (blockSel.Block is BlockCANMarketSingle)
                {
                    guiMarket = new GUIDialogCANMarketSingleOwner("trade", Inventory, Pos, Api as ICoreClientAPI);
                }

                if (this is BECANMarketStall)
                {
                    guiMarket = new GUIDialogCANMarketStall("trade", Inventory, Pos, this.Api as ICoreClientAPI);
                }
                else
                {
                    guiMarket = new GUIDialogCANStall("trade", Inventory, Pos, this.Api as ICoreClientAPI);
                }
                guiMarket.OnClosed += delegate
                {
                    guiMarket = null;
                    capi.Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, 1001);
                    capi.Network.SendPacketClient(Inventory.Close(byPlayer));
                };
                guiMarket.TryOpen();
                capi.Network.SendPacketClient(Inventory.Open(byPlayer));
                capi.Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, 1000);
            }
            else
            {
                guiMarket.TryClose();
            }
        }
        public void AddSoldByLog(string playerName, string goodItemName, int amount)
        {
            if (soldLog.TryGetValue(playerName, out var playerDict))
            {
                if (playerDict.TryGetValue(goodItemName, out var itemCount))
                {
                    playerDict[goodItemName] = amount + itemCount;
                }
                else
                {
                    playerDict[goodItemName] = amount;
                }
            }
            else
            {
                soldLog[playerName] = new Dictionary<string, int> { { goodItemName, amount } };
            }
        }
        private void updateGuiStocks()
        {
            for (int i = 0; i < this.stocks.Length; i++)
            {
                var mainComposer = this.guiMarket.Composers["stallCompo"];
                var dynTextStock = mainComposer.GetDynamicText("stock" + i);
                if (this.stocks[i] == -2)
                {
                    dynTextStock
                     .SetNewText("∞");
                    continue;
                }
                dynTextStock
                .SetNewText(this.stocks[i] < 999
                    ? this.stocks[i].ToString()
                    : "999+");
            }
            for (int i = 0; i < this.stocks.Length; i++)
            {
                var mainComposer = this.guiMarket.Composers["stallCompo"];
                var dynTextStock = mainComposer.GetDynamicText("maxStock" + i);
                if (this.maxStocks[i] == -2)
                {
                    dynTextStock
                     .SetNewText("-");
                    continue;
                }
                dynTextStock
                .SetNewText(this.maxStocks[i] < 999
                    ? this.maxStocks[i].ToString()
                    : "999+");
            }
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            this.adminShop = tree.GetBool("adminShop");
            this.ownerName = tree.GetString("ownerName");
            this.ownerUID = tree.GetString("ownerUID");
            this.InfiniteStocks = tree.GetBool("InfiniteStocks");
            this.StorePayment = tree.GetBool("StorePayment");

            for (int i = 0; i < (inventory.Count - 2) / 3; i++)
            {
                this.stocks[i] = tree.GetInt("stockLeft" + i, 0);
            }
            for (int i = 0; i < (inventory.Count - 2) / 3; i++)
            {
                this.maxStocks[i] = tree.GetInt("maxStocks" + i, -2);
            }

            if (guiMarket != null)
            {
                updateGuiStocks();
            }

            base.FromTreeAttributes(tree, worldForResolving);
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("adminShop", adminShop);
            tree.SetString("ownerName", ownerName);
            tree.SetString("ownerUID", ownerUID);
            tree.SetBool("InfiniteStocks", this.InfiniteStocks);
            tree.SetBool("StorePayment", this.StorePayment);

            for (int i = 0; i < (inventory.Count - 2) / 3; i++)
            {
                tree.SetInt("stockLeft" + i, this.stocks[i]);
            }

            for (int i = 0; i < (inventory.Count - 2) / 3; i++)
            {
                tree.SetInt("maxStocks" + i, this.maxStocks[i]);
            }
        }
    }
}
