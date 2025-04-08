using canmarket.src.BEB;
using canmarket.src.Blocks;
using canmarket.src.GUI;
using canmarket.src.Inventories;
using canmarket.src.Render;
using canmarket.src.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace canmarket.src.BE.SupportClasses
{
    public abstract class BEMarket : BlockEntityDisplay
    {
        public InventoryCANMarketOnChest inventory;
        public GUIDialogCANMarket guiMarket;
        public string ownerName;
        public string ownerUID;
        protected MeshData[] meshes;
        protected BECANMarketRenderer renderer;
        protected BlockFacing facing;
        public bool InfiniteStocks = false;
        public bool StorePayment = true;
        public override InventoryBase Inventory => inventory;
        public override string InventoryClassName => "canmarket";
        public bool shouldDrawMeshes;

        public BEMarket(int inventorySlotsAmount = 8)
        {
            inventory = new InventoryCANMarketOnChest(null, null, inventorySlotsAmount);
            inventory.Pos = Pos;
            inventory.OnInventoryClosed += new OnInventoryClosedDelegate(OnInventoryClosed);
            inventory.OnInventoryOpened += new OnInventoryOpenedDelegate(OnInvOpened);
            inventory.SlotModified += new Action<int>(OnSlotModified);
            meshes = new MeshData[inventory.Count / 2];
            shouldDrawMeshes = false;
        }

        private void OnInventoryClosed(IPlayer player)
        {
            guiMarket?.Dispose();
            guiMarket = null;
        }
        protected virtual void OnInvOpened(IPlayer player) => inventory.PutLocked = false;
        private void OnSlotModified(int slotNum)
        {
            var chunk = Api.World.BlockAccessor.GetChunkAtBlockPos(Pos);
            if (chunk == null)
            {
                return;
            }

            if (!Inventory[slotNum].Empty && slotNum % 2 == 1)
            {
                updateMesh(slotNum);
            }
            tfMatrices = genTransformationMatrices();
            chunk.MarkModified();

            MarkDirty(true);
        }
        public override void updateMeshes()
        {
            for (int i = 1; i < inventory.Count; i += 2)
            {
                updateMesh(i);
            }
            tfMatrices = genTransformationMatrices();
        }

        public void UpdateMeshes()
        {
            if (inventory == null)
            {
                return;
            }
            for (int slotid = 0; slotid < inventory.Count; slotid++)
            {
                updateMesh(slotid);
            }
            MarkDirty(true);
        }
        protected override void updateMesh(int slotid)
        {
            if (Api == null || Api.Side == EnumAppSide.Server)
            {
                return;
            }
            if (Inventory[slotid].Empty)
            {
                return;
            }
            getOrCreateMesh(Inventory[slotid].Itemstack, slotid);
        }

        protected override string getMeshCacheKey(ItemStack stack)
        {
            if (stack.Collectible is IContainedMeshSource containedMeshSource)
            {
                return containedMeshSource.GetMeshCacheKey(stack);
            }

            return stack.Collectible.Code.ToString();
        }
        protected override MeshData getOrCreateMesh(ItemStack stack, int index)
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


            if (stack.Class == EnumItemClass.Item && (stack.Item.Shape == null || stack.Item.Shape.VoxelizeTexture))
            {
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 1.5707964f, 0f, 0f);
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.33f, 0.33f, 0.33f);
                mesh.Translate(0f, -0.46875f, 0f);
            }

            if (stack.Collectible is ItemPlantableSeed)
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.35f, 0.35f, 0.35f);
                mesh.Translate(0f, -4f / 16, 0f);
            }
            else if (stack.Collectible.Code.Path.Contains("axehead-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.55f, 0.55f, 0.55f);
                mesh.Translate(0.0f, -0.09f, 0.03f);
            }
            else if (stack.Collectible.Code.Path.Contains("axe-felling"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.6f, 0.6f, 0.6f);
                mesh.Translate(-0.01f, -0.09f, 0.03f);
            }
            else if (stack.Collectible.Code.Path.Contains("knife-"))
            {
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), (float)Math.PI / 2, 0.0f, 0.0f);
                mesh.Translate(-0.0f, -0.32f, 0.25f);
            }
            else if (stack.Collectible.Code.Path.Contains("knifeblade-"))
            {
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), (float)Math.PI / 2, 0.0f, 0.0f);
                mesh.Translate(-0.0f, -0.32f, 0.25f);
            }
            else if (stack.Collectible.Code.Path.Contains("cleaver"))
            {
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 1.77f, 0f, 0f);
                mesh.Translate(0f, -0.20f, 0f);
            }
            else if (stack.Collectible.Code.Path.Contains("scythe-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.3f, 0.3f, 0.3f);
                mesh.Translate(-0.0f, -0.17f, 0.05f);
            }
            else if (stack.Collectible.Code.Path.Contains("scythehead-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.55f, 0.55f, 0.55f);
                mesh.Translate(-0.4f, -0.09f, 0.23f);
            }
            else if (stack.Collectible.Code.Path.Contains("hoe-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.5f, 0.5f, 0.5f);
                mesh.Translate(0.05f, -0.13f, 0.03f);
            }
            else if (stack.Collectible.Code.Path.Contains("hammer-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.7f, 0.7f, 0.7f);
                mesh.Translate(0.05f, -0.45f, 0.03f);
            }
            else if (stack.Collectible.Code.Path.Contains("hoehead-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.5f, 0.5f, 0.5f);
                mesh.Translate(0.05f, -0.13f, 0.03f);
            }
            else if (stack.Collectible.Code.Path.Contains("saw-") || stack.Collectible.Code.Path.Contains("sawblade-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.65f, 0.65f, 0.65f);
                mesh.Translate(0.05f, -0.4f, 0.07f);
            }
            else if (stack.Collectible.Code.Path.Contains("shovel-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.35f, 0.35f, 0.35f);
                mesh.Translate(0.05f, -0.13f, 0.03f);
            }
            else if (stack.Collectible.Code.Path.Contains("shovelhead-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.55f, 0.55f, 0.55f);
                mesh.Translate(0.05f, -0.13f, 0.03f);
            }
            else if (stack.Collectible.Code.Path.Contains("bladehead-") || stack.Collectible.Code.Path.Contains("blade-falx"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.55f, 0.55f, 0.55f);
                mesh.Translate(0.0f, -0.09f, 0.03f);
            }
            else if (stack.Collectible.Code.Path.Contains("sword-short"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.5f, 0.5f, 0.5f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                //mesh.Translate(-0.14f, -0.09f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("quarterstaff"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.35f, 0.35f, 0.35f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, 0.75f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("mace-plain"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.45f, 0.45f, 0.45f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, 0f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("halberd-plain"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.35f, 0.35f, 0.35f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, 0.7f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("poleaxe-plain"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.35f, 0.35f, 0.35f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, 0.7f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("club-plain"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.35f, 0.35f, 0.35f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, -0.1f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("javelin-plain"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.35f, 0.35f, 0.35f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, 0.3f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("pike-plain"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.35f, 0.35f, 0.35f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, 0.3f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("sword-long"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.35f, 0.35f, 0.35f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, -0.1f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("axe-long"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.45f, 0.45f, 0.45f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, 0.2f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("sword-great"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.45f, 0.45f, 0.45f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, 0.2f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("shield-heavy-plain"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.45f, 0.45f, 0.45f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), (float)Math.PI / 2, 0.0f, 0);
                mesh.Translate(-0.05f, 0.1f, 0.15f);
            }
            else if (stack.Collectible.Code.Path.Contains("shield-light-plain"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.45f, 0.45f, 0.45f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), (float)Math.PI / 2, 0.0f, 0);
                mesh.Translate(-0.05f, -0.1f, 0.15f);
            }
            else if (stack.Collectible.Code.Path.Contains("quiver-waist"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.45f, 0.45f, 0.45f);
                //mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), ((float)Math.PI / 2), 0.0f, 0);
                mesh.Translate(-0.05f, -0.4f, 0.15f);
            }
            else if (stack.Collectible.Code.Path.Contains("poultice"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.55f, 0.55f, 0.55f);
                mesh.Translate(0f, -3f / 16, 0f);
            }
            else if (stack.Collectible.Code.Path.Contains("stone-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.65f, 0.65f, 0.65f);
                mesh.Translate(0f, -2.5f / 16, 0f);
            }
            else if (stack.Collectible.Code.Path.Equals("rope"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.65f, 0.65f, 0.65f);
                mesh.Translate(0f, -2.5f / 16, 0f);
            }
            else if (stack.Collectible.Code.Path.Contains("plank-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.65f, 0.65f, 0.65f);
                mesh.Translate(0f, -2.5f / 16, 0f);
            }
            else if (stack.Collectible.Code.Path.Contains("clothes-lowerbody"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.55f, 0.55f, 0.55f);
                mesh.Translate(0f, -2.5f / 16, 0f);
            }
            else if (stack.Collectible.Code.Path.Contains("clothes-upperbody"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.55f, 0.55f, 0.55f);
                mesh.Translate(0f, -7f / 16, 0f);
            }
            else if (stack.Collectible.Code.Path.Contains("clothes-nadiya-head"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.55f, 0.55f, 0.55f);
                mesh.Translate(0f, -15f / 16, 0f);
            }
            else if (stack.Collectible.Code.Path.Contains("clothes-nadiya-foot"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.55f, 0.55f, 0.55f);
                mesh.Translate(0f, -2f / 16, 0f);
            }
            else if (stack.Collectible.Code.Path.Contains("clothes-head"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.55f, 0.55f, 0.55f);
                mesh.Translate(0f, -15f / 16, 0f);
            }
            else if (stack.Collectible.Code.Path.Contains("clothes-face"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.55f, 0.55f, 0.55f);
                mesh.Translate(0f, -15f / 16, 0f);
            }
            else if (stack.Collectible.Code.Path.Equals("blade-forlorn-iron") || stack.Collectible.Code.Path.Equals("blade-blackguard-iron"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.45f, 0.45f, 0.45f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, 0.2f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("part-shortsword-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.45f, 0.45f, 0.45f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, 0.2f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("part-greatsword-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.45f, 0.45f, 0.45f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, 0.2f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("handle-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.45f, 0.45f, 0.45f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, 0.2f, 0.1f);
            }

            if (facing == BlockFacing.EAST)
            {
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, -2.35f, 0f);
            }
            else if (facing == BlockFacing.WEST)
            {
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 1.0f, 0f);
            }
            else if (facing == BlockFacing.NORTH)
            {
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, -1.0f, 0f);
            }
            else
            {
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 2.35f, 0f);
            }


            string key = getMeshCacheKey(stack);
            MeshCache[key] = mesh;
            return mesh;
        }
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if (shouldDrawMeshes)
            {
                for (int index = 1; index < inventory.Count; index += 2)
                {
                    ItemSlot slot = Inventory[index];
                    if (!slot.Empty && tfMatrices != null)
                    {
                        mesher.AddMeshData(getMesh(slot.Itemstack), tfMatrices[index / 2], 1);
                    }
                }
            }
            return false;
        }


        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            inventory.LateInitialize("canmarket-" + Pos.X.ToString() + "/" + Pos.Y.ToString() + "/" + Pos.Z.ToString(), api, this);
            inventory.Pos = Pos;
            MarkDirty(true);
            if (api != null && api.Side == EnumAppSide.Client)
            {
                renderer = new BECANMarketRenderer(this, Pos.ToVec3d(), api as ICoreClientAPI);
                Block block = (api as ICoreClientAPI).World.BlockAccessor.GetBlock(Pos);
                facing = BlockFacing.FromCode(block.LastCodePart());
                UpdateMeshes();
                MarkDirty(true);
            }
            if (api != null && api.Side == EnumAppSide.Server && facing == null)
            {
                Block block = api.World.BlockAccessor.GetBlock(Pos);
                facing = BlockFacing.FromCode(block.LastCodePart());
            }

        }
        public void calculateAmountForSlot(int slotId)
        {
            var entity = inventory.Api.World.BlockAccessor.GetBlockEntity(Pos.DownCopy(1));
            if (entity is BlockEntityGenericTypedContainer)
            {
                for (int i = 0; i < (inventory as InventoryCANMarketOnChest).stocks.Length; i++)
                {
                    (inventory as InventoryCANMarketOnChest).stocks[i] = 0;
                }
                ItemStack tmp = null;
                foreach (var itSlot in (entity as BlockEntityGenericTypedContainer).Inventory)
                {
                    tmp = itSlot.Itemstack;
                    if (tmp == null)
                    {
                        continue;
                    }
                    for (int i = 1; i < Inventory.Count; i += 2)
                    {
                        if (inventory[i].Itemstack == null)
                        {
                            continue;
                        }
                        if (tmp.Collectible.Equals(tmp, inventory[i].Itemstack, canmarket.config.IGNORED_STACK_ATTRIBTES_ARRAY) && UsefullUtils.IsReasonablyFresh(inventory.Api.World, tmp))
                        {
                            (inventory as InventoryCANMarketOnChest).stocks[i / 2] += tmp.StackSize;
                        }
                    }
                }
            }
        }
        public void calculateAmounts(BlockEntityGenericTypedContainer entity)
        {
            for (int i = 0; i < (inventory as InventoryCANMarketOnChest).stocks.Length; i++)
            {
                (inventory as InventoryCANMarketOnChest).stocks[i] = 0;
            }
            ItemStack tmp = null;
            foreach (var itSlot in (entity as BlockEntityGenericTypedContainer).Inventory)
            {
                tmp = itSlot.Itemstack;
                if (tmp == null)
                {
                    continue;
                }
                for (int i = 1; i < Inventory.Count; i += 2)
                {
                    if (inventory[i].Itemstack == null)
                    {
                        continue;
                    }
                    if (tmp.Collectible.Equals(tmp, inventory[i].Itemstack, canmarket.config.IGNORED_STACK_ATTRIBTES_ARRAY) && UsefullUtils.IsReasonablyFresh(inventory.Api.World, tmp))
                    {
                        (inventory as InventoryCANMarketOnChest).stocks[i / 2] += tmp.StackSize;
                    }
                }
            }
        }
        private void checkChestInventoryUnder()
        {
            var entity = inventory.Api.World.BlockAccessor.GetBlockEntity(Pos.DownCopy(1));
            if (entity is BlockEntityGenericTypedContainer)
            {
                var beb = (entity as BlockEntityGenericTypedContainer).GetBehavior<BEBehaviorTrackLastUpdatedContainer>();
                if (beb == null || beb.markToUpdaete < 1)
                {
                    return;
                }
                calculateAmounts(entity as BlockEntityGenericTypedContainer);
                beb.markToUpdaete = 0;
                //send custom packet with stocks info
                //and handle on client side
                MarkDirty();
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
                checkChestInventoryUnder();
            }
            if (packetid == 1042)
            {
                if (!player.HasPrivilege(Privilege.controlserver))
                {
                    return;
                }
                InfiniteStocks = !InfiniteStocks;
                MarkDirty(true);
                return;
            }
            if (packetid == 1043)
            {
                if (!player.HasPrivilege(Privilege.controlserver))
                {
                    return;
                }
                StorePayment = !StorePayment;
                MarkDirty(true);
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
            return;
        }
        private void updateGui()
        {
            var SingleComposer = guiMarket.SingleComposer;
            for (int i = 0; i < inventory.stocks.Length; i++)
            {
                if (InfiniteStocks)
                {
                    SingleComposer.GetDynamicText("stock" + i).SetNewText("∞");
                }
                else
                {
                    SingleComposer.GetDynamicText("stock" + i).SetNewText((Inventory as InventoryCANMarketOnChest).stocks[i] < 999
                        ? (Inventory as InventoryCANMarketOnChest).stocks[i].ToString()
                        : "999+");
                }
            }
            SingleComposer.GetSwitch("infinitestockstoggle")?.SetValue(InfiniteStocks);

            SingleComposer.GetSwitch("storepaymenttoggle")?.SetValue(StorePayment);

        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
            ownerName = tree.GetString("ownerName");
            ownerUID = tree.GetString("ownerUID");
            for (int i = 0; i < inventory.Count / 2; i++)
            {
                inventory.stocks[i] = tree.GetInt("stockLeft" + i, 0);
            }
            InfiniteStocks = tree.GetBool("InfiniteStocks");
            bool newStorePayment = tree.GetBool("StorePayment");
            if (StorePayment != newStorePayment)
            {

            }
            StorePayment = tree.GetBool("StorePayment");
            UpdateMeshes();
            if (guiMarket != null)
            {
                updateGui();
            }
            if (Api == null)
                return;
            inventory.AfterBlocksLoaded(Api.World);
            if (Api.Side != EnumAppSide.Client)
                return;
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            ITreeAttribute tree1 = new TreeAttribute();
            inventory.ToTreeAttributes(tree1);
            tree["inventory"] = tree1;
            tree.SetString("ownerName", ownerName);
            tree.SetString("ownerUID", ownerUID);
            for (int i = 0; i < inventory.Count / 2; i++)
            {
                tree.SetInt("stockLeft" + i, inventory.stocks[i]);
            }
            tree.SetBool("InfiniteStocks", InfiniteStocks);
            tree.SetBool("StorePayment", StorePayment);
        }
        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            base.OnBlockBroken(byPlayer);
            if (renderer != null)
            {
                renderer.Dispose();
            }
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            if (renderer != null)
            {
                renderer.Dispose();
            }
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            if (renderer != null)
            {
                renderer.Dispose();
            }
        }
        protected override float[][] genTransformationMatrices()
        {
            float[][] tfMatrices = new float[4][];
            for (int index = 0; index < 4; index++)
            {
                float x = index % 2 == 0 ? 0.3125f : 0.6875f;
                float y = 0.063125f;
                float z = index > 1 ? 0.6875f : 0.3125f;
                int rnd = GameMath.MurmurHash3Mod(Pos.X, Pos.Y + index * 50, Pos.Z, 30) - 15;
                ItemSlot itemSlot = inventory[index];
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
                        jsonObject = collectible != null ? collectible.Attributes : null;
                    }
                }
                JsonObject collObjAttr = jsonObject;
                if (collObjAttr != null && !collObjAttr["randomizeInDisplayCase"].AsBool(true))
                {
                    rnd = 0;
                }
                float degY = rnd;


                var matrix = new Matrixf()
                    .Translate(0.5f, 0f, 0.5f)
                    .Translate(x - 0.5f, y, z - 0.5f)
                    //.RotateYDeg(degY)
                    .Scale(0.75f, 0.75f, 0.75f)
                    .Translate(-0.5f, 0f, -0.5f);


                if (facing == null)
                {
                    Block block = Api.World.BlockAccessor.GetBlock(Pos);
                    facing = BlockFacing.FromCode(block.LastCodePart());
                }


                //for north
                if (facing == BlockFacing.EAST)
                {
                    if (index == 1 || index == 3)
                    {
                        facingTranslate = true;
                    }
                }
                else if (facing == BlockFacing.WEST)
                {
                    if (index == 0 || index == 2)
                    {
                        facingTranslate = true;
                    }
                }
                else if (facing == BlockFacing.NORTH)
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

    }
}
