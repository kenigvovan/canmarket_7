using System;
using System.Collections.Generic;
using System.Linq;
using canmarket.src.BE.SupportClasses;
using canmarket.src.Blocks;
using canmarket.src.Inventories;
using canmarket.src.Items;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace canmarket.src.BE
{
    public class BECANStall : BEStall, BEWarehouseUser, IDisposable
    {
        protected CollectibleObject nowTesselatingObj;
        protected Shape nowTesselatingShape;
        private MeshData ownMesh;
        private BlockCANStall ownBlock;

        public virtual string AttributeTransformCode => "onDisplayTransform";
        public override InventoryBase Inventory => this.inventory;
        public override string InventoryClassName => "canmarket";

        public BECANStall()
        {
            type = "rusty";
        }

        public override void Initialize(ICoreAPI api)
        {
            this.ownBlock = (base.Block as BlockCANStall);
            bool isNewlyplaced = this.inventory == null;
            if (isNewlyplaced)
                this.InitInventory(base.Block);
            base.Initialize(api);
            this.inventory.LateInitialize("canmarket-" + this.Pos.X + "/" + this.Pos.Y + "/" + this.Pos.Z, api, this);
            this.inventory.Pos = this.Pos;
            this.MarkDirty(true);
            if (this.Api != null && this.Api.Side == EnumAppSide.Server)
                this.RegisterGameTickListener(new Action<float>(CheckSoldLogAndWriteToBook), 30000);
            if (this.Api != null && this.Api.Side == EnumAppSide.Client)
            {
                Block block = (this.Api as ICoreClientAPI).World.BlockAccessor.GetBlock(this.Pos);
                this.facing = BlockFacing.FromCode(block.LastCodePart());
                this.MarkDirty(true);
            }
            if (api.Side == EnumAppSide.Client && !isNewlyplaced)
                this.loadOrCreateMesh();
            this.MarkDirty(true);
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
            this.inventory = new InventoryCANStall(null, null, this, quantitySlots);
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
            var chunk = this.Api.World.BlockAccessor.GetChunkAtBlockPos(this.Pos);
            if (chunk == null) return;
            chunk.MarkModified();
            this.MarkDirty(true);
        }

        // ── Draw ─────────────────────────────────────────────────────────────────

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            if (base.OnTesselation(mesher, tesselator)) return true;
            if (this.ownMesh == null) return true;
            mesher.AddMeshData(this.ownMesh, 1);
            return true;
        }

        private void loadOrCreateMesh()
        {
            BlockCANStall block = base.Block as BlockCANStall;
            if (base.Block == null)
            {
                block = this.Api.World.BlockAccessor.GetBlock(this.Pos) as BlockCANStall;
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

        public void Dispose() { }

        public int[] GetStocks() => this.stocks;
        public int[] GetMaxStocks() => this.maxStocks;
        public HashSet<Vec3i> GetChestsPositions() => this.chestsCoords;
    }
}
