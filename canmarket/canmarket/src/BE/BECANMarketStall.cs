using canmarket.src.BE.SupportClasses;
using canmarket.src.Blocks;
using canmarket.src.GUI;
using canmarket.src.helpers.Interfaces;
using canmarket.src.Inventories;
using canmarket.src.Items;
using canmarket.src.Render;
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
    public class BECANMarketStall: BEStall, IDisposable, ITexPositionSource, IStocksContainer, IOwnerProvider, IAdminShop, IStoreChestsSources, BEWarehouseUser, IWriteSoldLog
    {        
        private MeshData ownMesh;
        private BlockCANMarketStall ownBlock;
        public string type = "oak";
        private float rotAngleY;
        private static Vec3f origin = new Vec3f(0.5f, 0f, 0.5f);
        public bool shouldDrawMeshes;
        protected BECANMarketStallRenderer renderer;
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

        //for every trade we have stock quantity
        public override InventoryBase Inventory => this.inventory;
        public Size2i AtlasSize => capi.BlockTextureAtlas.Size;
        public override string InventoryClassName => "canmarketstall";
        protected CollectibleObject nowTesselatingObj;

        protected Shape nowTesselatingShape;

        protected ICoreClientAPI capi;

        protected float[][] tfMatrices;
        public virtual string ClassCode => InventoryClassName;
        public virtual string AttributeTransformCode => "onDisplayTransform";
        protected Dictionary<string, MeshData> MeshCache => ObjectCacheUtil.GetOrCreate(Api, "meshesDisplay-" + ClassCode, () => new Dictionary<string, MeshData>());
        public virtual TextureAtlasPosition this[string textureCode]
        {
            get
            {
                IDictionary<string, CompositeTexture> dictionary;
                if (!(nowTesselatingObj is Item item))
                {
                    dictionary = (nowTesselatingObj as Block).Textures;
                }
                else
                {
                    IDictionary<string, CompositeTexture> textures = item.Textures;
                    dictionary = textures;
                }

                IDictionary<string, CompositeTexture> dictionary2 = dictionary;
                AssetLocation value = null;
                if (dictionary2.TryGetValue(textureCode, out var value2))
                {
                    value = value2.Baked.BakedName;
                }

                if (value == null && dictionary2.TryGetValue("all", out value2))
                {
                    value = value2.Baked.BakedName;
                }

                if (value == null)
                {
                    nowTesselatingShape?.Textures.TryGetValue(textureCode, out value);
                }

                if (value == null)
                {
                    value = new AssetLocation(textureCode);
                }

                return getOrCreateTexPos(value);
            }
        }
        protected TextureAtlasPosition getOrCreateTexPos(AssetLocation texturePath)
        {
            TextureAtlasPosition texPos = capi.BlockTextureAtlas[texturePath];
            if (texPos == null && !capi.BlockTextureAtlas.GetOrInsertTexture(texturePath, out var _, out texPos))
            {
                capi.World.Logger.Warning(string.Concat("For render in block ", base.Block.Code, ", item {0} defined texture {1}, no such texture found."), nowTesselatingObj.Code, texturePath);
                return capi.BlockTextureAtlas.UnknownTexturePosition;
            }

            return texPos;
        }
        public BECANMarketStall()
        {

            shouldDrawMeshes = false;
        }
        public override void Initialize(ICoreAPI api)
        {
            capi = api as ICoreClientAPI;
            this.ownBlock = (base.Block as BlockCANMarketStall);
            bool isNewlyplaced = this.inventory == null;
            if (isNewlyplaced)
            {
                this.InitInventory(base.Block);
            }
            base.Initialize(api);
            this.inventory.LateInitialize("canmarketstall-" + this.Pos.X.ToString() + "/" + this.Pos.Y.ToString() + "/" + this.Pos.Z.ToString(), api, this);
            this.inventory.Pos = this.Pos;
            
            this.MarkDirty(true);
            if (this.Api != null && this.Api.Side == EnumAppSide.Server)
            {
                this.RegisterGameTickListener(new Action<float>(CheckSoldLogAndWriteToBook), 30000);
            }
            if (this.Api != null && this.Api.Side == EnumAppSide.Client)
            {
                Block block = (this.Api as ICoreClientAPI).World.BlockAccessor.GetBlock(this.Pos);
                renderer = new BECANMarketStallRenderer(this, Pos.ToVec3d(), api as ICoreClientAPI);
                this.facing = BlockFacing.FromCode(block.LastCodePart());
                UpdateMeshes();
                this.MarkDirty(true);
            }
            if (api.Side == EnumAppSide.Client && !isNewlyplaced)
            {
                this.loadOrCreateMesh();
            }
            this.MarkDirty(true);
        }
        public void UpdateMeshes()
        {
            for (int i = 4; i < inventory.Count; i += 3)
            {
                updateMesh(i);
            }
            tfMatrices = genTransformationMatrices();
        }
        protected void updateMesh(int slotid)
        {
            if (this.Api == null || this.Api.Side == EnumAppSide.Server)
            {
                return;
            }
            if (this.Inventory[slotid].Empty)
            {
                return;
            }
            this.getOrCreateMesh(this.Inventory[slotid].Itemstack, slotid);
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
            this.inventory = new InventoryCANMarketStall((string)null, (ICoreAPI)null, this, quantitySlots);
            this.inventory.Pos = this.Pos;
            this.inventory.OnInventoryClosed += new OnInventoryClosedDelegate(this.OnInventoryClosed);
            this.inventory.OnInventoryOpened += new OnInventoryOpenedDelegate(this.OnInvOpened);
            this.inventory.SlotModified += new Action<int>(this.OnSlotModified);
            this.stocks = new int[(this.quantitySlots - 2) / 3];
            this.maxStocks = Enumerable.Repeat<int>(-2, (this.quantitySlots - 2) / 3).ToArray();
        }

        //Events
        private void OnSlotModified(int slotNum)
        {
            UpdateStockForItemSlot(slotNum);
            var chunk = Api.World.BlockAccessor.GetChunkAtBlockPos(Pos);
            if (chunk == null)
            {
                return;
            }
            if(slotNum < 2)
            {
                return;
            }
            if (!Inventory[slotNum].Empty && slotNum % 4 == 0)
            {
                updateMesh(slotNum);
            }
            tfMatrices = genTransformationMatrices();
            chunk.MarkModified();

            MarkDirty(true);
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
            if (this.Api.Side == EnumAppSide.Client)
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
                    if (warehouse != null)
                    {
                        if (this.InfiniteStocks)
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
                        for (int i = 4, j = 0; i <= inventory.Count; i += 3, j++)
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
                        if (shouldMarkDirty)
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
            if (soldLog.Count == 0)
            {
                return;
            }
            ItemSlot bookSlot = this.inventory[this.inventory.LogBookSlotId];
            if (bookSlot.Itemstack != null)
            {
                string signature = bookSlot.Itemstack.Attributes.GetString("signedby");
                if (signature != null && !signature.Equals("CAN_Market"))
                {
                    return;
                }

                StringBuilder sb = new StringBuilder();
                foreach (var it in soldLog)
                {
                    sb.Append(it.Key).Append(": ");
                    foreach (var bou in it.Value)
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
                if (signature == null)
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
                    if (rowId > (inventory.Count - 2) / 3 || rowId < 0)
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
            if (packetid == 1045)
            {
                if (!player.PlayerUID.Equals(this.ownerUID))
                {
                    return;
                }

                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryReader reader = new BinaryReader(ms);
                    int rowId = reader.ReadInt32();
                    if (rowId > (inventory.Count - 2) / 3 || rowId < 0)
                    {
                        return;
                    }


                    //if (!this.inventory[(rowId * 3) + 2].Empty)
                    {
                        //int stockNumber = reader.ReadInt32();
                        int slotNumber = reader.ReadInt32();
                        ItemStack newItemStack = new ItemStack();
                        newItemStack.FromBytes(reader);
                        newItemStack.ResolveBlockOrItem(this.Api.World);
                        int selectedStackSize = reader.ReadInt32();
                        if(newItemStack.Collectible.MaxStackSize < selectedStackSize)
                        {
                            newItemStack.StackSize = newItemStack.Collectible.MaxStackSize;
                        }
                        else
                        {
                            newItemStack.StackSize = selectedStackSize;
                        }
                        this.inventory[(rowId * 3) + 2 + slotNumber].Itemstack = newItemStack;
                        this.inventory[(rowId * 3) + 2 + slotNumber].MarkDirty();
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
            if (shouldDrawMeshes)
            {
                for (int index = 4; index < this.inventory.Count; index += 3)
                {
                    ItemSlot slot = this.Inventory[index];
                    if (!slot.Empty && this.tfMatrices != null)
                    {
                        mesher.AddMeshData(this.getMesh(slot.Itemstack), this.tfMatrices[(index - 2) / 3], 1);
                    }
                }
            }
            return false;

            /*if (base.OnTesselation(mesher, tesselator))
            {
                return true;
            }
            if (this.ownMesh == null)
            {
                return true;
            }

            mesher.AddMeshData(this.ownMesh, 1);
            return true;*/
        }
        protected MeshData getMesh(ItemStack stack)
        {
            string meshCacheKey = getMeshCacheKey(stack);
            MeshCache.TryGetValue(meshCacheKey, out var value);
            return value;
        }
        private void loadOrCreateMesh()
        {
            BlockCANMarketStall block = base.Block as BlockCANMarketStall;
            if (base.Block == null)
            {
                block = (this.Api.World.BlockAccessor.GetBlock(this.Pos) as BlockCANMarketStall);
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
            this.ownMesh = mesh.Clone().Rotate(BECANMarketStall.origin, 0f, this.MeshAngle, 0f).Scale(BECANMarketStall.origin, this.rndScale, this.rndScale, this.rndScale);
        }

        protected float[][] genTransformationMatrices()
        {
            float[][] tfMatrices = new float[4][];
            for (int index = 0; index < 4; index++)
            {
                float x = (index % 2 == 0) ? 0.3125f : 0.6875f;
                float y = 0.063125f;
                float z = (index > 1) ? 0.6875f : 0.3125f;
                int rnd = GameMath.MurmurHash3Mod(this.Pos.X, this.Pos.Y + index * 50, this.Pos.Z, 30) - 15;
                ItemSlot itemSlot = this.inventory[index];
                JsonObject jsonObject;
                bool facingTranslate = false;
                if (itemSlot == null)
                {
                    jsonObject = null;
                }
                else
                {
                    ItemStack itemstack = itemSlot.Itemstack;
                    if (itemstack == null)
                    {
                        jsonObject = null;
                    }
                    else
                    {
                        CollectibleObject collectible = itemstack.Collectible;
                        jsonObject = ((collectible != null) ? collectible.Attributes : null);
                    }
                }
                JsonObject collObjAttr = jsonObject;
                if (collObjAttr != null && !collObjAttr["randomizeInDisplayCase"].AsBool(true))
                {
                    rnd = 0;
                }
                float degY = (float)rnd;


                var matrix = new Matrixf()
                    .Translate(0.5f, 0f, 0.5f)
                    .Translate(x - 0.5f, y, z - 0.5f)
                    //.RotateYDeg(degY)
                    .Scale(0.75f, 0.75f, 0.75f)
                    .Translate(-0.5f, 0f, -0.5f);


                if (this.facing == null)
                {
                    Block block = this.Api.World.BlockAccessor.GetBlock(this.Pos);
                    this.facing = BlockFacing.FromCode(block.LastCodePart());
                }


                //for north
                if (this.facing == BlockFacing.EAST)
                {
                    if (index == 1 || index == 3)
                    {
                        facingTranslate = true;
                    }
                }
                else if (this.facing == BlockFacing.WEST)
                {
                    if (index == 0 || index == 2)
                    {
                        facingTranslate = true;
                    }
                }
                else if (this.facing == BlockFacing.NORTH)
                {
                    if (index == 0 || index == 1)
                    {
                        facingTranslate = true;
                    }
                }
                else
                {
                    if (index == 2 || index == 3)
                    {
                        facingTranslate = true;
                    }
                }


                if (facingTranslate)
                {
                    matrix.Translate(0, 3f / 16, 0);
                }
                tfMatrices[index] = matrix.Values;
            }
            return tfMatrices;
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
            tree.SetBool("adminShop", adminShop);

            for (int i = 0; i < (inventory.Count - 2) / 3; i++)
            {
                tree.SetInt("stockLeft" + i, this.stocks[i]);
            }
            for (int i = 0; i < (inventory.Count - 2) / 3; i++)
            {
                tree.SetInt("maxStocks" + i, this.maxStocks[i]);
            }
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
                    if (((BEWarehouseUser)this).existWarehouse(this.Pos, tree.GetInt("posX"), tree.GetInt("posY"), tree.GetInt("posZ"), tree.GetInt("num"), this.Api.World, out BECANWareHouse warehouse))
                    {
                        warehouse = (BECANWareHouse)this.Api.World.BlockAccessor.GetBlockEntity(new BlockPos(tree.GetInt("posX"), tree.GetInt("posY"), tree.GetInt("posZ")));
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
        protected MeshData getOrCreateMesh(ItemStack stack, int index)
        {
            MeshData mesh = getMesh(stack);
            //this.MeshCache.Clear();
            if (mesh != null)
            {
                return mesh;
            }
            IContainedMeshSource meshSource = stack.Collectible as IContainedMeshSource;
            if (meshSource != null)
            {
                mesh = meshSource.GenMesh(stack, (Api as ICoreClientAPI).BlockTextureAtlas, Pos);
            }
            if (mesh == null)
            {
                ICoreClientAPI capi = Api as ICoreClientAPI;
                if (stack.Block is BlockMicroBlock)
                {
                    ITreeAttribute treeAttribute = stack.Attributes;
                    if (treeAttribute == null)
                    {
                        treeAttribute = new TreeAttribute();
                    }

                    int[] materials = BlockEntityMicroBlock.MaterialIdsFromAttributes(treeAttribute, (Api as ICoreClientAPI).World);
                    uint[] array = (treeAttribute["cuboids"] as IntArrayAttribute)?.AsUint;
                    if (array == null)
                    {
                        array = (treeAttribute["cuboids"] as LongArrayAttribute)?.AsUint;
                    }

                    List<uint> voxelCuboids = array == null ? new List<uint>() : new List<uint>(array);
                    Block firstblock = capi.World.Blocks[materials[0]];
                    JsonObject blockAttributes = firstblock.Attributes;
                    bool flag = blockAttributes != null && blockAttributes.IsTrue("chiselShapeFromCollisionBox");
                    uint[] originalCuboids = null;
                    if (flag)
                    {
                        Cuboidf[] collboxes = firstblock.CollisionBoxes;
                        originalCuboids = new uint[collboxes.Length];
                        for (int i = 0; i < collboxes.Length; i++)
                        {
                            Cuboidf box = collboxes[i];
                            uint uintbox = BlockEntityMicroBlock.ToUint((int)(16f * box.X1), (int)(16f * box.Y1), (int)(16f * box.Z1), (int)(16f * box.X2), (int)(16f * box.Y2), (int)(16f * box.Z2), 0);
                            originalCuboids[i] = uintbox;
                        }
                    }

                    mesh = BlockEntityMicroBlock.CreateMesh(Api as ICoreClientAPI, voxelCuboids, materials, null, null, originalCuboids);
                    mesh.Translate(0f, -3f, 0f);
                    mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.15f, 0.15f, 0.15f);
                }
                else if (stack.Class == EnumItemClass.Block)
                {
                    if (stack.Block is BlockClutter)
                    {
                        Dictionary<string, MultiTextureMeshRef> clutterMeshRefs = ObjectCacheUtil.GetOrCreate(capi, (stack.Block as BlockShapeFromAttributes).ClassType + "MeshesInventory", () => new Dictionary<string, MultiTextureMeshRef>());
                        string type = stack.Attributes.GetString("type", "");
                        IShapeTypeProps cprops = (stack.Block as BlockShapeFromAttributes).GetTypeProps(type, stack, null);
                        if (cprops == null)
                        {
                            return null;
                        }
                        float rotX = stack.Attributes.GetFloat("rotX", 0f);
                        float rotY = stack.Attributes.GetFloat("rotY", 0f);
                        float rotZ = stack.Attributes.GetFloat("rotZ", 0f);
                        string otcode = stack.Attributes.GetString("overrideTextureCode", null);
                        string hashkey = string.Concat(new string[]
                        {
                            cprops.HashKey,
                            "-",
                            rotX.ToString(),
                            "-",
                            rotY.ToString(),
                            "-",
                            rotZ.ToString(),
                            "-",
                            otcode
                        });

                        mesh = (stack.Block as BlockShapeFromAttributes).GetOrCreateMesh(cprops, null, otcode);
                        mesh = mesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), rotX, rotY, rotZ);
                    }
                    else
                    {
                        mesh = capi.TesselatorManager.GetDefaultBlockMesh(stack.Block).Clone();
                    }
                    mesh.Translate(0f, -3f, 0f);
                    mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.15f, 0.15f, 0.15f);
                }
                else
                {
                    nowTesselatingObj = stack.Collectible;
                    nowTesselatingShape = null;
                    CompositeShape shape = stack.Item.Shape;
                    if ((shape != null ? shape.Base : null) != null)
                    {
                        nowTesselatingShape = capi.TesselatorManager.GetCachedShape(stack.Item.Shape.Base);
                    }
                    capi.Tesselator.TesselateItem(stack.Item, out mesh, this);
                    mesh.RenderPassesAndExtraBits.Fill((short)EnumChunkRenderPass.BlendNoCull);
                }
            }
            JsonObject attributes = stack.Collectible.Attributes;
            if (attributes != null && attributes[AttributeTransformCode].Exists)
            {
                JsonObject attributes2 = stack.Collectible.Attributes;
                ModelTransform transform = attributes2 != null ? attributes2[AttributeTransformCode].AsObject<ModelTransform>(null) : null;
                transform.EnsureDefaultValues();
                mesh.ModelTransform(transform);
            }
            else if (attributes != null && attributes["onshelfTransform"].Exists)
            {
                JsonObject attributes3 = stack.Collectible.Attributes;
                if (attributes3 != null && attributes3["onDisplayTransform"].Exists)
                {
                    JsonObject attributes4 = stack.Collectible.Attributes;
                    ModelTransform transform2 = attributes4 != null ? attributes4["onDisplayTransform"].AsObject<ModelTransform>(null) : null;
                    transform2.EnsureDefaultValues();
                    mesh.ModelTransform(transform2);
                }
            }
            mesh.Translate(0f, 3f / 16, 0f);

            SupportFunctions.getOrCreateMesh(ref mesh, stack, index, capi, facing);

            string key = getMeshCacheKey(stack);
            MeshCache[key] = mesh;
            return mesh;
        }
        protected string getMeshCacheKey(ItemStack stack)
        {
            if (stack.Collectible is IContainedMeshSource containedMeshSource)
            {
                return containedMeshSource.GetMeshCacheKey(stack);
            }

            return stack.Collectible.Code.ToString();
        }
    }
}
