using System;
using System.Collections.Generic;
using System.Linq;
using canmarket.src.BE.SupportClasses;
using canmarket.src.Blocks;
using canmarket.src.helpers.Interfaces;
using canmarket.src.Inventories;
using canmarket.src.Items;
using canmarket.src.Render;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace canmarket.src.BE
{
    public class BECANMarketStall : BEStall, IDisposable, ITexPositionSource, IStocksContainer, IOwnerProvider, IAdminShop, IStoreChestsSources, BEWarehouseUser, IWriteSoldLog
    {
        private MeshData ownMesh;
        private BlockCANMarketStall ownBlock;
        public bool shouldDrawMeshes;
        protected BECANMarketStallRenderer renderer;

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
                    dictionary = (nowTesselatingObj as Block).Textures;
                else
                    dictionary = item.Textures;

                AssetLocation value = null;
                if (dictionary.TryGetValue(textureCode, out var value2))
                    value = value2.Baked.BakedName;
                if (value == null && dictionary.TryGetValue("all", out value2))
                    value = value2.Baked.BakedName;
                if (value == null)
                    nowTesselatingShape?.Textures.TryGetValue(textureCode, out value);
                if (value == null)
                    value = new AssetLocation(textureCode);
                return getOrCreateTexPos(value);
            }
        }

        protected TextureAtlasPosition getOrCreateTexPos(AssetLocation texturePath)
        {
            TextureAtlasPosition texPos = capi.BlockTextureAtlas[texturePath];
            if (texPos == null && !capi.BlockTextureAtlas.GetOrInsertTexture(texturePath, out var _, out texPos))
            {
                capi.World.Logger.Warning("For render in block " + base.Block.Code + ", item {0} defined texture {1}, no such texture found.", nowTesselatingObj.Code, texturePath);
                return capi.BlockTextureAtlas.UnknownTexturePosition;
            }
            return texPos;
        }

        public BECANMarketStall()
        {
            type = "oak";
            shouldDrawMeshes = false;
        }

        public override void Initialize(ICoreAPI api)
        {
            capi = api as ICoreClientAPI;
            this.ownBlock = (base.Block as BlockCANMarketStall);
            bool isNewlyplaced = this.inventory == null;
            if (isNewlyplaced)
                this.InitInventory(base.Block);
            base.Initialize(api);
            this.inventory.LateInitialize("canmarketstall-" + this.Pos.X + "/" + this.Pos.Y + "/" + this.Pos.Z, api, this);
            this.inventory.Pos = this.Pos;
            this.MarkDirty(true);
            if (this.Api != null && this.Api.Side == EnumAppSide.Server)
                this.RegisterGameTickListener(new Action<float>(CheckSoldLogAndWriteToBook), 30000);
            if (this.Api != null && this.Api.Side == EnumAppSide.Client)
            {
                Block block = (this.Api as ICoreClientAPI).World.BlockAccessor.GetBlock(this.Pos);
                renderer = new BECANMarketStallRenderer(this, Pos.ToVec3d(), api as ICoreClientAPI);
                this.facing = BlockFacing.FromCode(block.LastCodePart());
                UpdateMeshes();
                this.MarkDirty(true);
            }
            if (api.Side == EnumAppSide.Client && !isNewlyplaced)
                this.loadOrCreateMesh();
            this.MarkDirty(true);
        }

        public void UpdateMeshes()
        {
            for (int i = 4; i < inventory.Count; i += 3)
                updateMesh(i);
            tfMatrices = genTransformationMatrices();
        }

        protected void updateMesh(int slotid)
        {
            if (this.Api == null || this.Api.Side == EnumAppSide.Server) return;
            if (this.Inventory[slotid].Empty) return;
            this.getOrCreateMesh(this.Inventory[slotid], slotid);
        }

        protected virtual void InitInventory(Block block)
        {
            if (block?.Attributes != null)
            {
                JsonObject props = block.Attributes["properties"][this.type];
                if (!props.Exists)
                    props = block.Attributes["properties"]["*"];
                this.quantitySlots = props["quantitySlots"].AsInt(this.quantitySlots);
            }
            this.inventory = new InventoryCANMarketStall(null, null, this, quantitySlots);
            this.inventory.Pos = this.Pos;
            this.inventory.OnInventoryClosed += OnInventoryClosed;
            this.inventory.OnInventoryOpened += OnInvOpened;
            this.inventory.SlotModified += OnSlotModified;
            this.stocks = new int[(this.quantitySlots - 2) / 3];
            this.maxStocks = Enumerable.Repeat(-2, (this.quantitySlots - 2) / 3).ToArray();
        }

        private void OnSlotModified(int slotNum)
        {
            UpdateStockForItemSlot(slotNum);
            var chunk = Api.World.BlockAccessor.GetChunkAtBlockPos(Pos);
            if (chunk == null) return;
            if (slotNum < 2) return;
            if (!Inventory[slotNum].Empty && (slotNum % 4) == 0)
                updateMesh(slotNum);
            tfMatrices = genTransformationMatrices();
            chunk.MarkModified();
            MarkDirty(true);
        }

        protected override void OnInfiniteStocksWarehouseReady(BECANWareHouse warehouse)
        {
            warehouse.FindContainersAround();
        }

        // ── Draw ─────────────────────────────────────────────────────────────────

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            if (shouldDrawMeshes)
            {
                for (int index = 4; index < this.inventory.Count; index += 3)
                {
                    ItemSlot slot = this.Inventory[index];
                    if (!slot.Empty && this.tfMatrices != null)
                        mesher.AddMeshData(this.getMesh(slot), this.tfMatrices[(index - 2) / 3], 1);
                }
            }
            return false;
        }

        protected MeshData getMesh(ItemSlot slot)
        {
            string meshCacheKey = getMeshCacheKey(slot);
            MeshCache.TryGetValue(meshCacheKey, out var value);
            return value;
        }

        private void loadOrCreateMesh()
        {
            BlockCANMarketStall block = base.Block as BlockCANMarketStall;
            if (base.Block == null)
            {
                block = this.Api.World.BlockAccessor.GetBlock(this.Pos) as BlockCANMarketStall;
                base.Block = block;
            }
            if (block == null) return;
            string cacheKey = "stallMeshes" + block.FirstCodePart(0);
            Dictionary<string, MeshData> meshes = ObjectCacheUtil.GetOrCreate<Dictionary<string, MeshData>>(this.Api, cacheKey, () => new Dictionary<string, MeshData>());
            Shape cshape = Vintagestory.API.Common.Shape.TryGet(this.Api, "canmarket:shapes/block/stall.json");
            MeshData mesh = block.GenMesh(this.Api as ICoreClientAPI, this.type, cshape, null);
            meshes[this.type] = mesh;
            this.ownMesh = mesh.Clone().Rotate(origin, 0f, this.MeshAngle, 0f).Scale(origin, this.rndScale, this.rndScale, this.rndScale);
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
                JsonObject collObjAttr = itemSlot?.Itemstack?.Collectible?.Attributes;
                if (collObjAttr != null && !collObjAttr["randomizeInDisplayCase"].AsBool(true))
                    rnd = 0;

                var matrix = new Matrixf()
                    .Translate(0.5f, 0f, 0.5f)
                    .Translate(x - 0.5f, y, z - 0.5f)
                    .Scale(0.75f, 0.75f, 0.75f)
                    .Translate(-0.5f, 0f, -0.5f);

                if (this.facing == null)
                {
                    Block block = this.Api.World.BlockAccessor.GetBlock(this.Pos);
                    this.facing = BlockFacing.FromCode(block.LastCodePart());
                }

                bool facingTranslate = false;
                if (this.facing == BlockFacing.EAST)
                    facingTranslate = (index == 1 || index == 3);
                else if (this.facing == BlockFacing.WEST)
                    facingTranslate = (index == 0 || index == 2);
                else if (this.facing == BlockFacing.NORTH)
                    facingTranslate = (index == 0 || index == 1);
                else
                    facingTranslate = (index == 2 || index == 3);

                if (facingTranslate)
                    matrix.Translate(0, 3f / 16, 0);
                tfMatrices[index] = matrix.Values;
            }
            return tfMatrices;
        }

        // ── Serialization ─────────────────────────────────────────────────────────

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            BlockCANStall block = worldForResolving.GetBlock(new AssetLocation(tree.GetString("blockCode", null))) as BlockCANStall;
            this.type = tree.GetString("type", (block != null) ? block.Props.DefaultType : null);
            this.MeshAngle = tree.GetFloat("meshAngle", this.MeshAngle);
            if (this.inventory == null)
            {
                if (tree.HasAttribute("blockCode"))
                    this.InitInventory(block);
                else
                    this.InitInventory(null);
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
                tree.SetString("forBlockCode", base.Block.Code.ToShortString());
            if (this.type == null)
                this.type = this.ownBlock.Props.DefaultType;
            tree.SetString("type", this.type);
            tree.SetFloat("meshAngle", this.MeshAngle);
        }

        protected MeshData getOrCreateMesh(ItemSlot slot, int index)
        {
            MeshData mesh = getMesh(slot);
            if (mesh != null) return mesh;

            IContainedMeshSource meshSource = slot.Itemstack.Collectible as IContainedMeshSource;
            if (meshSource != null)
                mesh = meshSource.GenMesh(slot, (Api as ICoreClientAPI).BlockTextureAtlas, Pos);

            if (mesh == null)
            {
                ICoreClientAPI capi = Api as ICoreClientAPI;
                if (slot.Itemstack.Block is BlockMicroBlock)
                {
                    ITreeAttribute treeAttribute = slot.Itemstack.Attributes ?? new TreeAttribute();
                    int[] materials = BlockEntityMicroBlock.MaterialIdsFromAttributes(treeAttribute, capi.World);
                    uint[] array = (treeAttribute["cuboids"] as IntArrayAttribute)?.AsUint
                               ?? (treeAttribute["cuboids"] as LongArrayAttribute)?.AsUint;
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
                            originalCuboids[i] = BlockEntityMicroBlock.ToUint((int)(16f * box.X1), (int)(16f * box.Y1), (int)(16f * box.Z1), (int)(16f * box.X2), (int)(16f * box.Y2), (int)(16f * box.Z2), 0);
                        }
                    }
                    mesh = BlockEntityMicroBlock.CreateMesh(capi, voxelCuboids, materials, null, null, originalCuboids);
                    mesh.Translate(0f, -3f, 0f);
                    mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.15f, 0.15f, 0.15f);
                }
                else if (slot.Itemstack.Class == EnumItemClass.Block)
                {
                    if (slot.Itemstack.Block is BlockClutter)
                    {
                        string slotType = slot.Itemstack.Attributes.GetString("type", "");
                        IShapeTypeProps cprops = (slot.Itemstack.Block as BlockShapeFromAttributes).GetTypeProps(slotType, slot.Itemstack, null);
                        if (cprops == null) return null;
                        float rotX = slot.Itemstack.Attributes.GetFloat("rotX");
                        float rotY = slot.Itemstack.Attributes.GetFloat("rotY");
                        float rotZ = slot.Itemstack.Attributes.GetFloat("rotZ");
                        string otcode = slot.Itemstack.Attributes.GetString("overrideTextureCode");
                        mesh = (slot.Itemstack.Block as BlockShapeFromAttributes).GetOrCreateMesh(cprops, null, otcode);
                        mesh = mesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), rotX, rotY, rotZ);
                    }
                    else
                    {
                        mesh = capi.TesselatorManager.GetDefaultBlockMesh(slot.Itemstack.Block).Clone();
                    }
                    mesh.Translate(0f, -3f, 0f);
                    mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.15f, 0.15f, 0.15f);
                }
                else
                {
                    nowTesselatingObj = slot.Itemstack.Collectible;
                    nowTesselatingShape = null;
                    CompositeShape shape = slot.Itemstack.Item.Shape;
                    if (shape?.Base != null)
                        nowTesselatingShape = capi.TesselatorManager.GetCachedShape(slot.Itemstack.Item.Shape.Base);
                    capi.Tesselator.TesselateItem(slot.Itemstack.Item, out mesh, this);
                    mesh.RenderPassesAndExtraBits.Fill((short)EnumChunkRenderPass.BlendNoCull);
                }
            }

            JsonObject attributes = slot.Itemstack.Collectible.Attributes;
            if (attributes != null && attributes[AttributeTransformCode].Exists)
            {
                ModelTransform transform = attributes[AttributeTransformCode].AsObject<ModelTransform>(null);
                transform.EnsureDefaultValues();
                mesh.ModelTransform(transform);
            }
            else if (attributes != null && attributes["onshelfTransform"].Exists && attributes["onDisplayTransform"].Exists)
            {
                ModelTransform transform = attributes["onDisplayTransform"].AsObject<ModelTransform>(null);
                transform.EnsureDefaultValues();
                mesh.ModelTransform(transform);
            }

            mesh.Translate(0f, 3f / 16, 0f);
            SupportFunctions.getOrCreateMesh(ref mesh, slot.Itemstack, index, Api as ICoreClientAPI, facing);

            string key = getMeshCacheKey(slot);
            MeshCache[key] = mesh;
            return mesh;
        }

        protected string getMeshCacheKey(ItemSlot slot)
        {
            if (slot.Itemstack.Collectible is IContainedMeshSource containedMeshSource)
                return containedMeshSource.GetMeshCacheKey(slot);
            return slot.Itemstack.Collectible.Code.ToString();
        }

        public void Dispose() { }
    }
}
