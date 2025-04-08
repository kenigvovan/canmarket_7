using canmarket.src.BE.SupportClasses;
using canmarket.src.BEB;
using canmarket.src.Blocks;
using canmarket.src.GUI;
using canmarket.src.Inventories;
using canmarket.src.Items;
using canmarket.src.Render;
using canmarket.src.Utils;
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
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace canmarket.src.BE
{
    public class BECANStall : BEStall, BEWarehouseUser, IDisposable
    {       
        protected CollectibleObject nowTesselatingObj;
        protected Shape nowTesselatingShape;       
        public HashSet<Vec3i> chestsCoords;
        private MeshData ownMesh;
        private BlockCANStall ownBlock;
        public string type = "rusty";
        private float rotAngleY;
        private static Vec3f origin = new Vec3f(0.5f, 0f, 0.5f);
        private float rndScale
        {
            get
            {
                return 1f + (float)(GameMath.MurmurHash3Mod(this.Pos.X, this.Pos.Y, this.Pos.Z, 100) - 50) / 1000f;
            }
        }
        public virtual float MeshAngle
        {
            get
            {
                return this.rotAngleY;
            }
            set
            {
                this.rotAngleY = value;
            }
        }

        public virtual string AttributeTransformCode => "onDisplayTransform";

        public override InventoryBase Inventory => this.inventory;

        public override string InventoryClassName => "canmarket";
        public BECANStall()
        {
           
        }
        public override void Initialize(ICoreAPI api)
        {
            this.ownBlock = (base.Block as BlockCANStall);
            bool isNewlyplaced = this.inventory == null;
            if (isNewlyplaced)
            {
                this.InitInventory(base.Block);
            }
            base.Initialize(api);
            this.inventory.LateInitialize("canmarket-" + this.Pos.X.ToString() + "/" + this.Pos.Y.ToString() + "/" + this.Pos.Z.ToString(), api, this);
            this.inventory.Pos = this.Pos;
            //this.UpdateMeshes();
            this.MarkDirty(true);
            if (this.Api != null && this.Api.Side == EnumAppSide.Server)
            {
                this.RegisterGameTickListener(new Action<float>(CheckSoldLogAndWriteToBook), 30000);
            }
            if (this.Api != null && this.Api.Side == EnumAppSide.Client)
            {         
                Block block = (this.Api as ICoreClientAPI).World.BlockAccessor.GetBlock(this.Pos);
                this.facing = BlockFacing.FromCode(block.LastCodePart());
                this.MarkDirty(true);
            }
            if (api.Side == EnumAppSide.Client && !isNewlyplaced)
            {
                this.loadOrCreateMesh();
            }
            this.MarkDirty(true);
        }        
        protected virtual void InitInventory(Block block)
        {
            if (((block != null) ? block.Attributes : null) != null)
            {
                JsonObject props = block.Attributes["properties"][this.type];
                if (!props.Exists)
                {
                    props = block.Attributes["properties"]["*"];
                }
                this.quantitySlots = props["quantitySlots"].AsInt(this.quantitySlots);
            }
            this.inventory = new InventoryCANStall((string)null, (ICoreAPI)null, this, quantitySlots);
            this.inventory.Pos = this.Pos;
            this.inventory.OnInventoryClosed += new OnInventoryClosedDelegate(this.OnInventoryClosed);
            this.inventory.OnInventoryOpened += new OnInventoryOpenedDelegate(this.OnInvOpened);
            this.inventory.SlotModified += new Action<int>(this.OnSlotModified);
            this.stocks = new int[(this.quantitySlots - 2) / 3];
            this.maxStocks = Enumerable.Repeat<int>(-2, (this.quantitySlots - 2) / 3).ToArray();
            //new int[(this.quantitySlots - 2) / 3];
        }

        //Events
        private void OnSlotModified(int slotNum)
        {
            UpdateStockForItemSlot(slotNum);
            var chunk = this.Api.World.BlockAccessor.GetChunkAtBlockPos(this.Pos);
            if (chunk == null)
            {
                return;
            }
            chunk.MarkModified();
            this.MarkDirty(true);
        }      
        private void OnInventoryClosed(IPlayer player)
        {
            this.guiMarket?.Dispose();
            this.guiMarket = null;
        }
        protected virtual void OnInvOpened(IPlayer player) 
        {
            //calculate items in chests around
            this.inventory.PutLocked = false;
            if(this.Api.Side == EnumAppSide.Client)
            {
                return;
            }
            ItemStack book = this.inventory[0].Itemstack;
            if (book != null && book.Item is ItemCANStallBook)
            {
                ITreeAttribute tree = book.Attributes.GetTreeAttribute("warehouse");
                if (tree == null)
                {
                    return;
                }
                if (this.inventory.existWarehouse(tree.GetInt("posX"), tree.GetInt("posY"), tree.GetInt("posZ"), tree.GetInt("num"), this.Api.World))
                {
                    BECANWareHouse warehouse = (BECANWareHouse)this.Api.World.BlockAccessor.GetBlockEntity(new BlockPos(tree.GetInt("posX"), tree.GetInt("posY"), tree.GetInt("posZ")));
                    if(warehouse != null)
                    {
                        if(this.InfiniteStocks)
                        {
                            bool shouldMarkDirtyInner = false;
                            for (int i = 4, j = 0; i <= inventory.Count; i += 3, j++)
                            {
                                if (stocks[j] != -2)
                                {
                                    shouldMarkDirtyInner = true;
                                    stocks[j] = -2;
                                }
                            }
                            
                            if (shouldMarkDirtyInner)
                            {
                                this.MarkDirty(true);
                            }

                            return;
                        }
                        warehouse.CalculateQuantitiesAround();
                        bool shouldMarkDirty = false;
                        for(int i = 4, j = 0 ; i <= inventory.Count; i+=3, j++)
                        {
                            ItemStack it = inventory[i].Itemstack;
                            if (it == null)
                            {
                                stocks[j] = 0;
                            }
                            else
                            {
                                string iSKey = it.Collectible.Code.Domain + it.Collectible.Code.Path;
                                foreach (var iter in it?.Attributes)
                                {
                                    if (canmarket.config.WAREHOUSE_ITEMSTACK_NOT_IGNORED_ATTRIBUTES.Contains(iter.Key))
                                    {
                                        iSKey = iSKey + "-" + iter.Value.ToString();
                                    }
                                }
                                if (warehouse.quantities.TryGetValue(iSKey, out int qua))
                                {
                                    if (this.stocks[j] != qua)
                                    {
                                        this.stocks[j] = qua;
                                        shouldMarkDirty = true;
                                    }
                                }
                                else
                                {
                                    this.stocks[j] = 0;
                                    shouldMarkDirty = true;
                                }
                            }
                        }
                        if(shouldMarkDirty)
                        {
                            this.MarkDirty(true);
                        }
                    }
                }
                else
                {
                    WareHouseNotFoundHandle(book);
                }

            }
        }       
        public void WareHouseNotFoundHandle(ItemStack book)
        {
            book.Attributes.RemoveAttribute("warehouse");
            for (int i = 0; i < stocks.Length; i++)
            {
                stocks[i] = 0;
            }
            this.MarkDirty(true);
        }

        public void CheckSoldLogAndWriteToBook(float dt)
        {
            if(soldLog.Count == 0) 
            {
                return;
            }
            ItemSlot bookSlot = this.inventory[this.inventory.LogBookSlotId];
            if (bookSlot.Itemstack != null)
            {
                string signature = bookSlot.Itemstack.Attributes.GetString("signedby");
                if(signature != null && !signature.Equals("CAN_Market"))
                {
                    return;
                }

                StringBuilder sb = new StringBuilder();
                foreach(var it in soldLog)
                {
                    sb.Append(it.Key).Append(": ");
                    foreach(var bou in it.Value)
                    {                
                        sb.Append(" " + bou.Value + " " + bou.Key);
                        if (it.Value.Last().Equals(bou))
                        {
                            sb.AppendLine();
                        }
                        else
                        {
                            sb.Append(", ");
                        }
                    }
                }

                string oldText = bookSlot.Itemstack.Attributes.GetString("text", "");
                if ((oldText.Length + sb.ToString().Length) >= 45000)
                {
                    bookSlot.Itemstack.Attributes.SetString("text", oldText + sb.ToString().Substring(0, Math.Max(45000 - oldText.Length, 1)));
                }
                else
                {
                    bookSlot.Itemstack.Attributes.SetString("text", oldText + sb.ToString());
                }
                if(signature == null)
                {
                    bookSlot.Itemstack.Attributes.SetString("signedby", "CAN_Market");
                }
                soldLog.Clear();
                return;
            }
        }
        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            if (((byItemStack != null) ? byItemStack.Attributes : null) != null)
            {
                string nowType = byItemStack.Attributes.GetString("type", byItemStack.Collectible.Attributes["defaultType"].AsString());
                if (nowType != this.type)
                {
                    this.type = nowType;
                }
            }
            base.OnBlockPlaced(null);
        }


        //Network
        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            base.OnReceivedClientPacket(player, packetid, data);
            if (packetid < 1000)
            {
                Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);
                Api.World.BlockAccessor.GetChunkAtBlockPos(Pos.X, Pos.Y, Pos.Z).MarkModified();
                return;
            }

            if (packetid == 1001)
            {
                player.InventoryManager?.CloseInventory(Inventory);
            }

            if (packetid == 1000)
            {
                player.InventoryManager?.OpenInventory(Inventory);
                //checkChestInventoryUnder();
            }
            if (packetid == 1042)
            {
                if (!player.HasPrivilege(Privilege.controlserver))
                {
                    return;
                }
                this.InfiniteStocks = !this.InfiniteStocks;
                this.MarkDirty(true);
                return;
            }
            if (packetid == 1043)
            {
                if (!player.HasPrivilege(Privilege.controlserver))
                {
                    return;
                }
                this.StorePayment = !this.StorePayment;
                this.MarkDirty(true);
                return;
            }
            if (packetid == 1044)
            {
                if (!player.PlayerUID.Equals(this.ownerUID))
                {
                    return;
                }

                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryReader reader = new BinaryReader(ms);
                    int rowId = reader.ReadInt32();
                    if(rowId > (inventory.Count - 2) / 3 || rowId < 0)
                    {
                        return;
                    }
                    if (!this.inventory[(rowId * 3) + 4].Empty)
                    {
                        int stockNumber = reader.ReadInt32();
                        this.maxStocks[rowId] = stockNumber;
                    }
                }
                
                this.MarkDirty(true);
                return;
            }
            return;
        }
        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            base.OnReceivedServerPacket(packetid, data);
            if (packetid == 1001)
            {
                (Api.World as IClientWorldAccessor).Player.InventoryManager.CloseInventory(Inventory);
                guiMarket?.TryClose();
                guiMarket?.Dispose();
                guiMarket = null;
            }          
        }


        //GUI
        public void RegenDialog()
        {
            this.guiMarket.SetupDialog();
            /*for (int i = 0; i < this.inventory.stocks.Length; i++)
            {
                this.guiMarket.SingleComposer.GetDynamicText("stock" + i).SetNewText(this.inventory.stocks[i].ToString());
            }*/
        }
        
        

        //Draw
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            if (base.OnTesselation(mesher, tesselator))
            {
                return true;
            }
            if (this.ownMesh == null)
            {
                return true;
            }

            mesher.AddMeshData(this.ownMesh, 1);
            return true;
        }
        private void loadOrCreateMesh()
        {
            BlockCANStall block = base.Block as BlockCANStall;
            if (base.Block == null)
            {
                block = (this.Api.World.BlockAccessor.GetBlock(this.Pos) as BlockCANStall);
                base.Block = block;
            }
            if (block == null)
            {
                return;
            }
            string cacheKey = "stallMeshes" + block.FirstCodePart(0);
            Dictionary<string, MeshData> meshes = ObjectCacheUtil.GetOrCreate<Dictionary<string, MeshData>>(this.Api, cacheKey, () => new Dictionary<string, MeshData>());
            Shape cshape = Vintagestory.API.Common.Shape.TryGet(this.Api, "canmarket:shapes/block/stall.json");

            ItemSlot firstNonEmptySlot = this.inventory.FirstNonEmptySlot;
            ItemStack firstStack = (firstNonEmptySlot != null) ? firstNonEmptySlot.Itemstack : null;
            string meshKey = string.Concat(new string[]
            {
                this.type
            });
            MeshData mesh;
            //if (!meshes.TryGetValue(meshKey, out mesh))
            {
                mesh = block.GenMesh(this.Api as ICoreClientAPI, this.type, cshape, null);
                meshes[meshKey] = mesh;
            }
            this.ownMesh = mesh.Clone().Rotate(BECANStall.origin, 0f, this.MeshAngle, 0f).Scale(BECANStall.origin, this.rndScale, this.rndScale, this.rndScale);
        }


        //Helpers
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            BlockCANStall block = worldForResolving.GetBlock(new AssetLocation(tree.GetString("blockCode", null))) as BlockCANStall;
            this.type = tree.GetString("type", (block != null) ? block.Props.DefaultType : null);
            this.MeshAngle = tree.GetFloat("meshAngle", this.MeshAngle);            
            
            if (this.inventory == null)
            {
                if (tree.HasAttribute("blockCode"))
                {
                    this.InitInventory(block);
                }
                else
                {
                    this.InitInventory(null);
                }
            }

            if (this.Api != null && this.Api.Side == EnumAppSide.Client)
            {
                this.loadOrCreateMesh();
                this.MarkDirty(true, null);
            }
            base.FromTreeAttributes(tree, worldForResolving);
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {           
            base.ToTreeAttributes(tree);
            if (base.Block != null)
            {
                tree.SetString("forBlockCode", base.Block.Code.ToShortString());
            }
            if (this.type == null)
            {
                this.type = this.ownBlock.Props.DefaultType;
            }
            tree.SetString("type", this.type);
            tree.SetFloat("meshAngle", this.MeshAngle);
            
            
        }
        public string GetPlacedBlockName()
        {
            return Lang.Get(string.Format("canmarket:block-{0}-stall", type));
        }
        private void UpdateStockForItemSlot(int slotId)
        {
            if (this.inventory[slotId] is CANTakeOutItemSlotStall && this.inventory[slotId].Itemstack != null)
            {
                ItemStack book = this.inventory[0].Itemstack;
                if (book != null && book.Item is ItemCANStallBook)
                {
                    ITreeAttribute tree = book.Attributes.GetTreeAttribute("warehouse");
                    if (tree == null)
                    {
                        return;
                    }
                    if (this.inventory.existWarehouse(tree.GetInt("posX"), tree.GetInt("posY"), tree.GetInt("posZ"), tree.GetInt("num"), this.Api.World))
                    {
                        BECANWareHouse warehouse = (BECANWareHouse)this.Api.World.BlockAccessor.GetBlockEntity(new BlockPos(tree.GetInt("posX"), tree.GetInt("posY"), tree.GetInt("posZ")));
                        if (warehouse != null)
                        {

                            if (warehouse.quantities.TryGetValue(this.inventory[slotId].Itemstack.Collectible.Code.Domain + this.inventory[slotId].Itemstack.Collectible.Code.Path, out int qua))
                            {
                                this.stocks[(slotId - 2) / 3] = qua;
                                this.maxStocks[(slotId - 2) / 3] = -2;
                                this.MarkDirty(true);
                            }
                            
                        }
                    }

                }
            }
            else
            {
                this.stocks[(slotId - 2) / 3] = 0;
                //this.maxStocks[(slotId - 2) / 3] = 0;
            }
        }
        public void Dispose()
        {
           
        }

        public int[] GetStocks()
        {
            return this.stocks;
        }

        public int[] GetMaxStocks()
        {
            return this.maxStocks;
        }
        public HashSet<Vec3i> GetChestsPositions()
        {
            return this.chestsCoords;
        }
    }
}
