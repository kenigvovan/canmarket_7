using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using canmarket.src.BE;
using canmarket.src.GUI;
using canmarket.src.helpers.Interfaces;
using canmarket.src.Inventories;
using canmarket.src.Items;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace canmarket.src.BE.SupportClasses
{
    public class BEStall : BlockEntityContainer, IStocksContainer, IOwnerProvider, IAdminShop, IStoreChestsSources, IWriteSoldLog, IFreshnessCheckChangable
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
        public CANStallDialog guiMarket;
        public HashSet<Vec3i> chestsCoords;
        public float currentFreshnessThreshold = canmarket.config.DEFAULT_MIN_FRESHNESS_FOR_SALE_PERCENTS;
        protected Dictionary<string, Dictionary<string, int>> soldLog = new Dictionary<string, Dictionary<string, int>>();

        // Shared mesh/display fields
        public string type;
        protected float rotAngleY;
        protected static Vec3f origin = new Vec3f(0.5f, 0f, 0.5f);
        protected float rndScale => 1f + (float)(GameMath.MurmurHash3Mod(Pos.X, Pos.Y, Pos.Z, 100) - 50) / 1000f;
        public virtual float MeshAngle { get => rotAngleY; set => rotAngleY = value; }

        public override InventoryBase Inventory => throw new NotImplementedException();
        public override string InventoryClassName => throw new NotImplementedException();

        public bool IsAdminShop          { get => adminShop;                  set => adminShop = value; }
        public bool MustStorePayment     { get => StorePayment;               set => StorePayment = value; }
        public bool ProvidesInfiniteStocks { get => InfiniteStocks;           set => InfiniteStocks = value; }
        public string OwnerGuid          { get => ownerUID;                   set => ownerUID = value; }
        public string OwnerName          { get => ownerName;                  set => ownerName = value; }
        public int[] Stocks              { get => stocks;                      set => stocks = value; }
        public int[] MaxStocks           { get => maxStocks;                  set => maxStocks = value; }
        public HashSet<Vec3i> ChestsPositions { get => chestsCoords;         set => chestsCoords = value; }
        public float CurrentFreshnessThreshold { get => currentFreshnessThreshold; set => currentFreshnessThreshold = value; }

        // ── GUI ──────────────────────────────────────────────────────────────────

        public void OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (Api.Side == EnumAppSide.Client)
                toggleInventoryDialogClient(byPlayer, blockSel);
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
                        ((it as InventoryCANStall).be as BECANStall).guiMarket?.Close();
                        byPlayer.InventoryManager.CloseInventory(it);
                        capi.Network.SendBlockEntityPacket((it as InventoryCANStall).be.Pos, 1001);
                        break;
                    }
                    else if (it is InventoryCANMarketOnChest)
                    {
                        ((it as InventoryCANMarketOnChest).be as BEMarket).guiMarket?.Close();
                        byPlayer.InventoryManager.CloseInventory(it);
                        capi.Network.SendBlockEntityPacket((it as InventoryCANMarketOnChest).be.Pos, 1001);
                        break;
                    }
                    else if (it is InventoryCANMarketStall)
                    {
                        (it as InventoryCANMarketStall).be.guiMarket?.Close();
                        byPlayer.InventoryManager.CloseInventory(it);
                        capi.Network.SendBlockEntityPacket((it as InventoryCANMarketStall).be.Pos, 1001);
                        break;
                    }
                }

                guiMarket = new CANStallDialog(capi, Pos, Inventory);
                guiMarket.OnClosed += () =>
                {
                    guiMarket = null;
                    capi.Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, 1001);
                    capi.Network.SendPacketClient(Inventory.Close(byPlayer));
                };
                capi.World.Player.InventoryManager.OpenInventory(Inventory);
                guiMarket.Open();
                capi.Network.SendPacketClient(Inventory.Open(byPlayer));
                capi.Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, 1000);
            }
            else
            {
                guiMarket.Close();
            }
        }

        public void RegenDialog() { }

        protected void OnInventoryClosed(IPlayer player)
        {
            guiMarket?.Dispose();
            guiMarket = null;
        }

        // ── Sold log ─────────────────────────────────────────────────────────────

        public void AddSoldByLog(string playerName, string goodItemName, int amount)
        {
            if (soldLog.TryGetValue(playerName, out var playerDict))
            {
                if (playerDict.TryGetValue(goodItemName, out var itemCount))
                    playerDict[goodItemName] = amount + itemCount;
                else
                    playerDict[goodItemName] = amount;
            }
            else
            {
                soldLog[playerName] = new Dictionary<string, int> { { goodItemName, amount } };
            }
        }

        public void CheckSoldLogAndWriteToBook(float dt)
        {
            if (soldLog.Count == 0) return;

            ItemSlot bookSlot = inventory[inventory.LogBookSlotId];
            if (bookSlot.Itemstack == null) return;

            string signature = bookSlot.Itemstack.Attributes.GetString("signedby");
            if (signature != null && !signature.Equals("CAN_Market")) return;

            var sb = new StringBuilder();
            foreach (var it in soldLog)
            {
                sb.Append(it.Key).Append(": ");
                foreach (var bou in it.Value)
                {
                    sb.Append(" " + bou.Value + " " + bou.Key);
                    sb.Append(it.Value.Last().Equals(bou) ? Environment.NewLine : ", ");
                }
            }

            string oldText = bookSlot.Itemstack.Attributes.GetString("text", "");
            string newText = sb.ToString();
            if (oldText.Length + newText.Length >= 45000)
                bookSlot.Itemstack.Attributes.SetString("text", oldText + newText.Substring(0, Math.Max(45000 - oldText.Length, 1)));
            else
                bookSlot.Itemstack.Attributes.SetString("text", oldText + newText);

            if (signature == null)
                bookSlot.Itemstack.Attributes.SetString("signedby", "CAN_Market");

            soldLog.Clear();
        }

        // ── Warehouse integration ─────────────────────────────────────────────────

        public void WareHouseNotFoundHandle(ItemStack book)
        {
            book.Attributes.RemoveAttribute("warehouse");
            for (int i = 0; i < stocks.Length; i++) stocks[i] = 0;
            MarkDirty(true);
        }

        protected virtual void OnInvOpened(IPlayer player)
        {
            inventory.PutLocked = false;
            if (Api.Side == EnumAppSide.Client) return;

            ItemStack book = inventory[0].Itemstack;
            if (book == null || !(book.Item is ItemCANStallBook)) return;

            ITreeAttribute tree = book.Attributes.GetTreeAttribute("warehouse");
            if (tree == null) return;

            if (!inventory.existWarehouse(tree.GetInt("posX"), tree.GetInt("posY"), tree.GetInt("posZ"), tree.GetInt("num"), Api.World))
            {
                WareHouseNotFoundHandle(book);
                return;
            }

            BECANWareHouse warehouse = Api.World.BlockAccessor.GetBlockEntity(
                new BlockPos(tree.GetInt("posX"), tree.GetInt("posY"), tree.GetInt("posZ"))) as BECANWareHouse;
            if (warehouse == null) return;

            if (InfiniteStocks)
            {
                bool dirty = false;
                for (int i = 4, j = 0; i <= inventory.Count; i += 3, j++)
                {
                    if (stocks[j] != -2) { stocks[j] = -2; dirty = true; }
                }
                if (dirty) MarkDirty(true);
                OnInfiniteStocksWarehouseReady(warehouse);
                return;
            }

            warehouse.CalculateQuantitiesAround();
            bool shouldMark = false;
            for (int i = 4, j = 0; i <= inventory.Count; i += 3, j++)
            {
                ItemStack it = inventory[i].Itemstack;
                if (it == null) { stocks[j] = 0; continue; }

                string key = it.Collectible.Code.Domain + it.Collectible.Code.Path;
                foreach (var attr in it.Attributes)
                {
                    if (canmarket.config.WAREHOUSE_ITEMSTACK_NOT_IGNORED_ATTRIBUTES.Contains(attr.Key))
                        key += "-" + attr.Value.ToString();
                }

                if (warehouse.quantities.TryGetValue(key, out int qua))
                {
                    if (stocks[j] != qua) { stocks[j] = qua; shouldMark = true; }
                }
                else
                {
                    stocks[j] = 0; shouldMark = true;
                }
            }
            if (shouldMark) MarkDirty(true);
        }

        /// <summary>Called when InfiniteStocks is active and the warehouse is valid. Override to run extra warehouse logic.</summary>
        protected virtual void OnInfiniteStocksWarehouseReady(BECANWareHouse warehouse) { }

        protected void UpdateStockForItemSlot(int slotId)
        {
            if (inventory[slotId] is CANTakeOutItemSlotStall && inventory[slotId].Itemstack != null)
            {
                ItemStack book = inventory[0].Itemstack;
                if (book == null || !(book.Item is ItemCANStallBook)) return;

                ITreeAttribute tree = book.Attributes.GetTreeAttribute("warehouse");
                if (tree == null) return;

                if (!inventory.existWarehouse(tree.GetInt("posX"), tree.GetInt("posY"), tree.GetInt("posZ"), tree.GetInt("num"), Api.World))
                    return;

                BECANWareHouse warehouse = Api.World.BlockAccessor.GetBlockEntity(
                    new BlockPos(tree.GetInt("posX"), tree.GetInt("posY"), tree.GetInt("posZ"))) as BECANWareHouse;
                if (warehouse == null) return;

                if (warehouse.quantities.TryGetValue(
                    inventory[slotId].Itemstack.Collectible.Code.Domain + inventory[slotId].Itemstack.Collectible.Code.Path,
                    out int qua))
                {
                    stocks[(slotId - 2) / 3] = qua;
                    maxStocks[(slotId - 2) / 3] = -2;
                    MarkDirty(true);
                }
            }
            else
            {
                stocks[(slotId - 2) / 3] = 0;
            }
        }

        // ── Network ───────────────────────────────────────────────────────────────

        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            base.OnReceivedClientPacket(player, packetid, data);
            if (packetid < 1000)
            {
                Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);
                Api.World.BlockAccessor.GetChunkAtBlockPos(Pos.X, Pos.Y, Pos.Z).MarkModified();
                return;
            }

            if (packetid == 1001) player.InventoryManager?.CloseInventory(Inventory);
            if (packetid == 1000) player.InventoryManager?.OpenInventory(Inventory);

            if (packetid == 1042)
            {
                if (!player.HasPrivilege(Privilege.controlserver)) return;
                InfiniteStocks = !InfiniteStocks;
                MarkDirty(true);
                return;
            }
            if (packetid == 1043)
            {
                if (!player.HasPrivilege(Privilege.controlserver)) return;
                StorePayment = !StorePayment;
                MarkDirty(true);
                return;
            }
            if (packetid == 1044)
            {
                if (!player.PlayerUID.Equals(ownerUID)) return;
                using var ms44 = new MemoryStream(data);
                var r44 = new BinaryReader(ms44);
                int rowId = r44.ReadInt32();
                if (rowId > (inventory.Count - 2) / 3 || rowId < 0) return;
                if (!inventory[(rowId * 3) + 4].Empty)
                    maxStocks[rowId] = r44.ReadInt32();
                MarkDirty(true);
                return;
            }
            if (packetid == 1045)
            {
                if (!player.PlayerUID.Equals(ownerUID)) return;
                using var ms45 = new MemoryStream(data);
                var r45 = new BinaryReader(ms45);
                int rowId = r45.ReadInt32();
                if (rowId > (inventory.Count - 2) / 3 || rowId < 0) return;
                int slotNumber = r45.ReadInt32();
                var newStack = new ItemStack();
                newStack.FromBytes(r45);
                newStack.ResolveBlockOrItem(Api.World);
                int selectedSize = r45.ReadInt32();
                newStack.StackSize = Math.Min(selectedSize, newStack.Collectible.MaxStackSize);
                inventory[(rowId * 3) + 2 + slotNumber].Itemstack = newStack;
                inventory[(rowId * 3) + 2 + slotNumber].MarkDirty();
                MarkDirty(true);
                return;
            }
            if (packetid == 1046)
            {
                if (!player.PlayerUID.Equals(ownerUID)) return;
                using var ms46 = new MemoryStream(data);
                CurrentFreshnessThreshold = new BinaryReader(ms46).ReadSingle();
                MarkDirty(true);
                return;
            }
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            base.OnReceivedServerPacket(packetid, data);
            if (packetid == 1001)
            {
                (Api.World as IClientWorldAccessor).Player.InventoryManager.CloseInventory(Inventory);
                guiMarket?.Close();
                guiMarket?.Dispose();
                guiMarket = null;
            }
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            if (byItemStack?.Attributes != null)
            {
                string nowType = byItemStack.Attributes.GetString("type",
                    byItemStack.Collectible.Attributes["defaultType"].AsString());
                if (nowType != type) type = nowType;
            }
            base.OnBlockPlaced(null);
        }

        public string GetPlacedBlockName() => Lang.Get($"canmarket:block-{type}-stall");

        // ── Serialization ─────────────────────────────────────────────────────────

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            adminShop = tree.GetBool("adminShop");
            ownerName = tree.GetString("ownerName");
            ownerUID = tree.GetString("ownerUID");
            InfiniteStocks = tree.GetBool("InfiniteStocks");
            StorePayment = tree.GetBool("StorePayment");
            CurrentFreshnessThreshold = tree.GetFloat("CurrentFreshnessThreshold", canmarket.config.DEFAULT_MIN_FRESHNESS_FOR_SALE_PERCENTS);

            for (int i = 0; i < (inventory.Count - 2) / 3; i++)
                stocks[i] = tree.GetInt("stockLeft" + i, 0);
            for (int i = 0; i < (inventory.Count - 2) / 3; i++)
                maxStocks[i] = tree.GetInt("maxStocks" + i, -2);

            base.FromTreeAttributes(tree, worldForResolving);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("adminShop", adminShop);
            tree.SetString("ownerName", ownerName);
            tree.SetString("ownerUID", ownerUID);
            tree.SetBool("InfiniteStocks", InfiniteStocks);
            tree.SetBool("StorePayment", StorePayment);
            tree.SetFloat("CurrentFreshnessThreshold", CurrentFreshnessThreshold);

            for (int i = 0; i < (inventory.Count - 2) / 3; i++)
                tree.SetInt("stockLeft" + i, stocks[i]);
            for (int i = 0; i < (inventory.Count - 2) / 3; i++)
                tree.SetInt("maxStocks" + i, maxStocks[i]);
        }
    }
}
