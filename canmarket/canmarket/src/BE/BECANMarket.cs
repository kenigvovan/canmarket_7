using canmarket.src.BEB;
using canmarket.src.GUI;
using canmarket.src.Inventories;
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
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace canmarket.src.BE
{
    public class BECANMarket : BEMarket,  ITexPositionSource
    {
        public InventoryCANMarketOnChest inventory;
        public GUIDialogCANMarketOwner guiMarket;
        protected CollectibleObject nowTesselatingObj;
        protected Shape nowTesselatingShape;
        public string ownerName;
        protected MeshData[] meshes;
        BECANMarketRenderer renderer;
        BlockFacing facing;
        public bool InfiniteStocks = false;
        public bool StorePayment = true;
        public virtual string AttributeTransformCode => "onDisplayTransform";
        long lastTimeCheckedChest;
        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                Dictionary<string, CompositeTexture> dictionary = this.nowTesselatingObj is Vintagestory.API.Common.Item nowTesselatingObj ? nowTesselatingObj.Textures : (Dictionary<string, CompositeTexture>)(this.nowTesselatingObj as Block).Textures;
                AssetLocation texturePath = (AssetLocation)null;
                CompositeTexture compositeTexture;
                if (dictionary.TryGetValue(textureCode, out compositeTexture))
                    texturePath = compositeTexture.Baked.BakedName;
                if ((object)texturePath == null && dictionary.TryGetValue("all", out compositeTexture))
                    texturePath = compositeTexture.Baked.BakedName;
                if ((object)texturePath == null)
                    this.nowTesselatingShape?.Textures.TryGetValue(textureCode, out texturePath);
                if ((object)texturePath == null)
                    texturePath = new AssetLocation(textureCode);
                return this.getOrCreateTexPos(texturePath);
            }
        }
        private TextureAtlasPosition getOrCreateTexPos(AssetLocation texturePath)
        {
            TextureAtlasPosition texPos = (this.Api as ICoreClientAPI).BlockTextureAtlas[texturePath];
            if (texPos == null)
            {
                IAsset asset = this.Api.Assets.TryGet(texturePath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
                if (asset != null)
                {
                    BitmapRef bitmap = asset.ToBitmap((this.Api as ICoreClientAPI));
                    (this.Api as ICoreClientAPI).BlockTextureAtlas.InsertTextureCached(texturePath, (IBitmap)bitmap, out int _, out texPos);
                }
                else
                    this.Api.World.Logger.Warning("For render in block " + this.Block.Code?.ToString() + ", item {0} defined texture {1}, not no such texture found.", (object)this.nowTesselatingObj.Code, (object)texturePath);
            }
            return texPos;
        }
        public Size2i AtlasSize => (this.Api as ICoreClientAPI).BlockTextureAtlas.Size;

        public override InventoryBase Inventory => this.inventory;

        public override string InventoryClassName => "canmarket";
        public BECANMarket()
        {
            this.inventory = new InventoryCANMarketOnChest((string)null, (ICoreAPI)null);
            // this.inventory.SlotModified += new Action<int>(this.OnSlotModified);
            this.inventory.Pos = this.Pos;
            // this.meshes = new MeshData[this.inventory.Count - 1];
            //this.inventory = new InventoryJewelerSet(4, (string)null, (ICoreAPI)null);
            //  this.inventory.OnInventoryClosed += new OnInventoryClosedDelegate(this.OnInventoryClosed);
            //this.inventory.OnInventoryOpened += new OnInventoryOpenedDelegate(this.OnInvOpened);
            // this.inventory.SlotModified += new Action<int>(this.OnSlotModified);

            // this.inventory.Pos = this.Pos;
            //this.inventory[0].MaxSlotStackSize = 1;
            this.inventory.OnInventoryClosed += new OnInventoryClosedDelegate(this.OnInventoryClosed);
            this.inventory.OnInventoryOpened += new OnInventoryOpenedDelegate(this.OnInvOpened);
            this.inventory.SlotModified += new Action<int>(this.OnSlotModified);
            this.meshes = new MeshData[this.inventory.Count/2];
            this.lastTimeCheckedChest = 0;
            shouldDrawMeshes = false;
            
        }
        private void OnSlotModified(int slotNum)
        {
            var chunk = this.Api.World.BlockAccessor.GetChunkAtBlockPos(this.Pos);
            if (chunk == null)
            {
                return;
            }
            chunk.MarkModified();
            this.updateMesh(slotNum);
            this.MarkDirty(true);
        }
        public void UpdateMeshes()
        {
            if(this.inventory == null)
            {
                return;
            }
            for (int slotid = 0; slotid < this.inventory.Count; slotid++)
            {
                this.updateMesh(slotid);
            }
            this.MarkDirty(true);
        }
        public void updateMesh(int slotid)
        {
            if (this.Api == null || this.Api.Side == EnumAppSide.Server || slotid % 2 == 0)
            {
                return;
            }
            if (this.inventory[slotid].Empty)
            {
                this.meshes[slotid/2] = (MeshData)null;
            }
            else
            {
                MeshData meshData = this.GenMesh(this.inventory[slotid].Itemstack);
                if(meshData == null)
                {
                    return;
                }
                if (this.facing == BlockFacing.EAST)
                {
                    if (slotid == 3 || slotid == 5)
                    {
                        meshData.Translate(0, 3f / 16, 0);
                    }
                }else if (this.facing == BlockFacing.WEST)
                {
                    if (slotid == 1 || slotid == 7)
                    {
                        meshData.Translate(0, 3f / 16, 0);
                    }
                }
                else if (this.facing == BlockFacing.NORTH)
                {
                    if (slotid == 1 || slotid == 3)
                    {
                        meshData.Translate(0, 3f / 16, 0);
                    }
                }
                else
                {
                    if (slotid == 5 || slotid == 7)
                    {
                        meshData.Translate(0, 3f / 16, 0);
                    }
                }
                TranslateMesh(meshData, slotid, this.inventory[slotid].Itemstack);
                if (meshData == null)
                {
                    return;
                }
                float scale = this.inventory[slotid].Itemstack.Class != EnumItemClass.Item ? 3f / 16f : 0.33f;               
                this.meshes[slotid/2] = meshData;
            }
        }
        public virtual void TranslateMesh(MeshData mesh, int index, ItemStack iS)        
        {
            var xTr = -4f / 16f;
            var zTr = -4f / 16f;
            var yTr = -2f / 16;
            if (iS.Collectible.Code.Path.StartsWith("saw-"))
            {
                 yTr = -4f / 16;
            }
           if (iS.Collectible.Code.Path.StartsWith("leather-"))
            {
                xTr = -3f / 16f;
                zTr = -3f / 16f;
                if (index == 3)
                {
                    xTr = 3f / 16f;
                    zTr = -3f / 16f;
                }
                else if (index == 5)
                {
                    xTr = 3f / 16f;
                    zTr = 3f / 16f;
                }
                else if (index == 7)
                {
                    xTr = -3f / 16f;
                    zTr = 3f / 16f;
                }
                yTr = 0f / 16;
            }
            else
            {
                if (index == 3)
                {
                    xTr = 4f / 16f;
                    zTr = -4f / 16f;
                }
                else if (index == 5)
                {
                    xTr = 4f / 16f;
                    zTr = 4f / 16f;
                }
                else if (index == 7)
                {
                    xTr = -4f / 16f;
                    zTr = 4f / 16f;
                }
            }
           mesh.Translate( xTr, yTr, zTr);
        }
        public MeshData GenMesh(ItemStack stack)
        {
            MeshData mesh = null;
            var meshSource = stack.Collectible as IContainedMeshSource;
            //var c = stack.Collectible.GetType();
            
            if (meshSource != null)
            {
                mesh = meshSource.GenMesh(stack, (this.Api as ICoreClientAPI).BlockTextureAtlas, Pos);
                if(mesh == null)
                {
                    return null;
                }
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.25f, 0.25f, 0.25f);
                mesh.Translate(0, -2f / 16, 0);
                if (stack.Collectible.Code.Path.Contains("shield-"))
                {
                    mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.95f, 0.95f, 0.95f);
                     mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), ((float)Math.PI / 2), 0.0f, 0.0f);
                    mesh.Translate(0.1f, -0.3f, 0.25f);
                }
            }
            else
            {
                ICoreClientAPI capi = Api as ICoreClientAPI;
                if (stack.Block is BlockMicroBlock)
                {
                    ITreeAttribute treeAttribute = stack.Attributes;
                    if (treeAttribute == null)
                    {
                        treeAttribute = new TreeAttribute();
                    }

                    int[] materials = BlockEntityMicroBlock.MaterialIdsFromAttributes(treeAttribute, (this.Api as ICoreClientAPI).World);
                    uint[] array = (treeAttribute["cuboids"] as IntArrayAttribute)?.AsUint;
                    if (array == null)
                    {
                        array = (treeAttribute["cuboids"] as LongArrayAttribute)?.AsUint;
                    }

                    List<uint> voxelCuboids = (array == null) ? new List<uint>() : new List<uint>(array);
                    mesh = BlockEntityMicroBlock.CreateMesh((this.Api as ICoreClientAPI), voxelCuboids, materials);
                    mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.15f, 0.15f, 0.15f);
                    mesh.Translate(0, -1f / 16, 0);
                }
                else if (stack.Class == EnumItemClass.Block)
                {
                    if (stack.Block is BlockClutter)
                    {
                        Dictionary<string, MeshRef> clutterMeshRefs = ObjectCacheUtil.GetOrCreate<Dictionary<string, MeshRef>>(capi, (stack.Block as BlockShapeFromAttributes).ClassType + "MeshesInventory", () => new Dictionary<string, MeshRef>());
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
                        MeshRef meshref;
                        if (clutterMeshRefs.TryGetValue(hashkey, out meshref))
                        {
                            mesh = (stack.Block as BlockShapeFromAttributes).GetOrCreateMesh(cprops, null, otcode);
                            mesh = mesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), rotX, rotY, rotZ);
                            //meshref = capi.Render.UploadMesh(mesh);
                            //clutterMeshRefs[hashkey] = meshref;
                        }


                    }
                    else
                    {
                        mesh = capi.TesselatorManager.GetDefaultBlockMesh(stack.Block).Clone();
                    }
                    if (true)//stack.Collectible.Code.Path.Equals("peatbrick"))
                    {//block/peatbrick
                       // mesh = null;
                        //nowTesselatingShape  = capi.TesselatorManager.GetCachedShape(stack.Block.Shape.Base).Clone();
                        //Shape shape = Shape.TryGet(Api, "shapes/block/peatbrick.json");
                       // Shape shape = capi.TesselatorManager.GetCachedShape(stack.Block.ShapeInventory != null ? stack.Block.ShapeInventory.Base: stack.Block.Shape.Base);
                       
                        //capi.Render.
                       // capi.Tesselator.TesselateShape(stack.Block, shape, out mesh, null, 0);
                      //  capi.Tesselator.TesselateBlock(stack.Block, out mesh);
                       if(mesh == null)
                        {
                            return null;
                        }
                        mesh.RenderPassesAndExtraBits.Fill((short)EnumChunkRenderPass.BlendNoCull);
                        // mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0f, 1.57f);
                        mesh.Translate(0f, -1f, 0f);
                        //nowTesselatingShape = capi.TesselatorManager.GetCachedShape(stack.Item.Shape.Base);
                        mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.15f, 0.15f, 0.15f);
                        // capi.Tesselator.TesselateItem(stack.Item, out mesh, this);

                        //mesh.RenderPassesAndExtraBits.Fill((short)EnumChunkRenderPass.BlendNoCull);
                    }
                    else
                    {
                        mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.15f, 0.15f, 0.15f);
                        mesh.Translate(0, -1f / 16, 0);
                    }
                }
                else
                {
                    nowTesselatingObj = stack.Collectible;
                    nowTesselatingShape = null;
                    if (stack.Item.Shape?.Base != null)
                    {
                        nowTesselatingShape = capi.TesselatorManager.GetCachedShape(stack.Item.Shape.Base);
                    }
                    capi.Tesselator.TesselateItem(stack.Item, out mesh, this);

                    mesh.RenderPassesAndExtraBits.Fill((short)EnumChunkRenderPass.BlendNoCull);
                    mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.45f, 0.45f, 0.45f);
                    if(stack.Collectible.Code.Path.Contains("xrowboat-"))
                    {
                        mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.5f, 0.5f, 0.5f);
                    }
                    else if(stack.Collectible.Code.Domain.Contains("xmelee"))
                    {
                        mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0f, 1.57f);
                        mesh.Translate(-0.2f, -0.05f, 0f);
                    }
                    else if (stack.Collectible.Code.Path.Contains("cleaver"))
                    {
                        mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 1.77f, 0f, 0f);
                        mesh.Translate(0f, -0.20f, 0f);
                    }
                    else if (stack.Collectible.Code.Path.Contains("axe-"))
                    {
                        mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.6f, 0.6f, 0.6f);
                        //mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, -1.0f, 0f);
                        mesh.Translate(-0.01f, -0.09f, 0.03f);
                    }
                    else if (stack.Collectible.Code.Path.Contains("knife-"))
                    {
                        //mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.6f, 0.6f, 0.6f);
                        mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), ((float)Math.PI/2), 0.0f, 0.0f);
                        mesh.Translate(-0.0f, -0.23f, 0.2f);
                    }
                    else if (stack.Collectible.Code.Path.Contains("scythe-"))
                    {                       
                        mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.3f, 0.3f, 0.3f);
                       // mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), ((float)Math.PI / 2), 0.0f, 0.0f);
                        mesh.Translate(-0.0f, -0.17f, 0.05f);
                    }
                    else if (stack.Collectible.Code.Path.Contains("saw-"))
                    {
                        mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.7f, 0.7f, 0.7f);
                        // mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), ((float)Math.PI / 2), 0.0f, 0.0f);
                        mesh.Translate(0.05f, -0.1f, 0.07f);
                    }
                    else if (stack.Collectible.Code.Path.Contains("hoe-"))
                    {
                        mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.5f, 0.5f, 0.5f);
                        // mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), ((float)Math.PI / 2), 0.0f, 0.0f);
                        mesh.Translate(0.05f, -0.13f, 0.03f);
                    }
                    else if (stack.Collectible.Code.Path.Contains("shovel-"))
                    {
                        mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.35f, 0.35f, 0.35f);
                        // mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), ((float)Math.PI / 2), 0.0f, 0.0f);
                        mesh.Translate(0.05f, -0.13f, 0.03f);
                    }
                    else if (stack.Collectible.Code.Path.Contains("shovelhead-"))
                    {
                        mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.55f, 0.55f, 0.55f);
                        // mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), ((float)Math.PI / 2), 0.0f, 0.0f);
                        mesh.Translate(0.05f, -0.13f, 0.03f);
                    }
                    else if (stack.Collectible.Code.Path.Contains("axehead-"))
                    {
                        mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.55f, 0.55f, 0.55f);
                        // mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), ((float)Math.PI / 2), 0.0f, 0.0f);
                        mesh.Translate(0.0f, -0.09f, 0.03f);
                    }
                    else if (stack.Collectible.Code.Path.Contains("bladehead-"))
                    {
                        mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.55f, 0.55f, 0.55f);
                        // mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), ((float)Math.PI / 2), 0.0f, 0.0f);
                        mesh.Translate(0.0f, -0.09f, 0.03f);
                    }
                    else if (stack.Collectible.Code.Path.Contains("scythehead-"))
                    {
                        mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.55f, 0.55f, 0.55f);
                        // mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), ((float)Math.PI / 2), 0.0f, 0.0f);
                        mesh.Translate(-0.17f, -0.09f, 0.13f);
                    }
                    
                    if (this.facing == BlockFacing.EAST)
                    {
                        mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, -2.35f, 0f);
                    }
                    else if (this.facing == BlockFacing.WEST)
                    {
                        mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 1.0f, 0f);
                    }
                    else if (this.facing == BlockFacing.NORTH)
                    {
                        mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, -1.0f, 0f);
                    }
                    else
                    {
                        mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 2.35f, 0f);
                    }
                }
            }

            if (stack.Collectible.Attributes?[AttributeTransformCode].Exists == true)
            {
                ModelTransform transform = stack.Collectible.Attributes?[AttributeTransformCode].AsObject<ModelTransform>();
                transform.EnsureDefaultValues();
                mesh.ModelTransform(transform);

                /*transform.Rotation.X = 0;
                transform.Rotation.Y = Block.Shape.rotateY;
                transform.Rotation.Z = 0;
                mesh.ModelTransform(transform);*/
            }
            else
            {

            }
            if(stack.Collectible is ItemPlantableSeed || stack.Collectible.Code.Path.Contains("grain-") || stack.Collectible.Code.Path.Contains("flour-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.35f, 0.35f, 0.35f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0f, ((float)Math.PI));
            }
            if (stack.Collectible is ItemCheese || stack.Collectible.Code.Path.Contains("crushed-") || stack.Collectible is ItemCandle || stack.Collectible is ItemClay)
            {
                mesh.Translate(0, 0.1f, 0f);
            }
            /*if (stack.Class == EnumItemClass.Item && (stack.Item.Shape == null || stack.Item.Shape.VoxelizeTexture))
            {
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), GameMath.PIHALF, 0, 0);
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.33f, 0.33f, 0.33f);
                mesh.Translate(0, -7.5f / 16f, 0f);
            }*/

                // mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, Block.Shape.rotateY * GameMath.DEG2RAD, 0);

                return mesh;
        }
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if (shouldDrawMeshes)
            {
                for (int index = 0; index < this.meshes.Length; index++)
                {
                    if (this.meshes[index] != null)
                    {
                        mesher.AddMeshData(this.meshes[index]);
                    }
                }
            }
            return false;
        }

        private void OnInventoryClosed(IPlayer player)
        {
            this.guiMarket?.Dispose();
            this.guiMarket = (GUIDialogCANMarketOwner)null;
        }
        protected virtual void OnInvOpened(IPlayer player) => this.inventory.PutLocked = false;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            this.inventory.LateInitialize("canmarket-" + this.Pos.X.ToString() + "/" + this.Pos.Y.ToString() + "/" + this.Pos.Z.ToString(), api, this);
            this.inventory.Pos = this.Pos;
            //this.UpdateMeshes();
            this.MarkDirty(true);
            if(this.Api != null && this.Api.Side == EnumAppSide.Client)
            {
                this.renderer = new BECANMarketRenderer(this, this.Pos.ToVec3d(), this.Api as ICoreClientAPI);
                Block block = (this.Api as ICoreClientAPI).World.BlockAccessor.GetBlock(this.Pos);
                this.facing = BlockFacing.FromCode(block.LastCodePart());
                UpdateMeshes();
                this.MarkDirty(true);
            }
        }
        public void calculateAmountForSlot(int slotId)
        {
            var entity = this.inventory.Api.World.BlockAccessor.GetBlockEntity(this.Pos.DownCopy(1));
            if (entity is BlockEntityGenericTypedContainer)
            {
                for (int i = 0; i < (this.inventory as InventoryCANMarketOnChest).stocks.Length; i++)
                {
                    (this.inventory as InventoryCANMarketOnChest).stocks[i] = 0;
                }
                ItemStack tmp = null;
                foreach (var itSlot in (entity as BlockEntityGenericTypedContainer).Inventory)
                {
                    tmp = itSlot.Itemstack;
                    if (tmp == null)
                    {
                        continue;
                    }
                    for (int i = 1; i < this.Inventory.Count; i += 2)
                    {
                        if (this.inventory[i].Itemstack == null)
                        {
                            continue;
                        }
                        if (tmp.Collectible.Equals(tmp, this.inventory[i].Itemstack, Config.Current.IGNORED_STACK_ATTRIBTES_ARRAY.Val) && UsefullUtils.IsReasonablyFresh(this.inventory.Api.World, tmp))
                        {
                            //holy fuck just use two inventories in the next trade blocks...or not
                            (this.inventory as InventoryCANMarketOnChest).stocks[i / 2] += tmp.StackSize;
                        }
                    }
                }
            }
        }
        public void calculateAmounts(BlockEntityGenericTypedContainer entity)
        {
            for (int i = 0; i < (this.inventory as InventoryCANMarketOnChest).stocks.Length; i++)
            {
                (this.inventory as InventoryCANMarketOnChest).stocks[i] = 0;
            }
            ItemStack tmp = null;
            foreach (var itSlot in (entity as BlockEntityGenericTypedContainer).Inventory)
            {
                tmp = itSlot.Itemstack;
                if (tmp == null)
                {
                    continue;
                }
                for (int i = 1; i < this.Inventory.Count; i += 2)
                {
                    if (this.inventory[i].Itemstack == null)
                    {
                        continue;
                    }
                    if (tmp.Collectible.Equals(tmp, this.inventory[i].Itemstack, Config.Current.IGNORED_STACK_ATTRIBTES_ARRAY.Val) && UsefullUtils.IsReasonablyFresh(this.inventory.Api.World, tmp))
                    {
                        //holy fuck just use two inventories in the next trade blocks...or not
                        (this.inventory as InventoryCANMarketOnChest).stocks[i / 2] += tmp.StackSize;
                    }
                }
            }
        }
        private void checkChestInventoryUnder()
        {
            var entity = this.inventory.Api.World.BlockAccessor.GetBlockEntity(this.Pos.DownCopy(1));
            if (entity is BlockEntityGenericTypedContainer)
            {
                var beb = (entity as BlockEntityGenericTypedContainer).GetBehavior<BEBehaviorTrackLastUpdatedContainer>();
                if(beb== null || beb.markToUpdaete < 1)
                {
                    return;
                }
                calculateAmounts(entity as BlockEntityGenericTypedContainer);
                beb.markToUpdaete = 0;
                //send custom packet with stocks info
                //and handle on client side
                this.MarkDirty();
            }
        }

        public void OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (Api.Side == EnumAppSide.Client)
            {
                toggleInventoryDialogClient(byPlayer);
            }
            else
            {

            }
            return;
            if (this.Api.Side == EnumAppSide.Server)
            {
                /*foreach(var it in byPlayer.InventoryManager.OpenedInventories)
                {
                    if( it is InventoryCANMarket)
                    {
                        byPlayer.InventoryManager.CloseInventory(it);
                        ((ICoreServerAPI)this.Api).Network.SendBlockEntityPacket((IServerPlayer)byPlayer, this.Pos.X, this.Pos.Y, this.Pos.Z, 1001, null);
                        break;
                    }
                }*/
                byte[] array;
                using (MemoryStream output = new MemoryStream())
                {
                    BinaryWriter stream = new BinaryWriter((Stream)output);
                    //stream.Write("BlockEntityCANMarket");
                   // stream.Write("123");
                   // stream.Write((byte)4);
                    TreeAttribute tree = new TreeAttribute();
                    this.inventory.ToTreeAttributes((ITreeAttribute)tree);
                    tree.ToBytes(stream);
                    array = output.ToArray();
                }
                ((ICoreServerAPI)this.Api).Network.SendBlockEntityPacket((IServerPlayer)byPlayer, this.Pos.X, this.Pos.Y, this.Pos.Z, (int)EnumBlockStovePacket.OpenGUI, array);
                byPlayer.InventoryManager.OpenInventory((IInventory)this.inventory);
            }
            return;
        }
        protected void toggleInventoryDialogClient(IPlayer byPlayer)
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
                        ((it as InventoryCANMarketOnChest).be as BECANMarket).guiMarket?.TryClose();
                        byPlayer.InventoryManager.CloseInventory(it);                     
                        capi.Network.SendBlockEntityPacket((it as InventoryCANMarketOnChest).be.Pos, 1001);
                        //capi.Network.SendPacketClient(it.Close(byPlayer));
                        break;
                    }
                }
                
                guiMarket = new GUIDialogCANMarketOwner("trade", Inventory, Pos, this.Api as ICoreClientAPI);
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
                this.InfiniteStocks = !this.InfiniteStocks;
                this.MarkDirty(true);
                return;
            }
            if (packetid == 1043)
            {
                if(!player.HasPrivilege(Privilege.controlserver))
                {
                    return;
                }
                this.StorePayment = !this.StorePayment;
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
            return;
            IClientWorldAccessor clientWorldAccessor = (IClientWorldAccessor)Api.World;
            if (packetid == (int)EnumBlockStovePacket.OpenGUI)
            {
                /*if (guiMarket != null && Pos.Equals(guiMarket.BlockEntityPosition))
                {
                    return;
                }*/
                if (guiMarket != null)
                {
                    if (guiMarket?.IsOpened() ?? false)
                    {
                        guiMarket.TryClose();
                    }

                    guiMarket?.Dispose();
                    guiMarket = null;
                    return;
                }

                TreeAttribute treeAttribute = new TreeAttribute();
                string dialogTitle;
                int cols;
                using (MemoryStream input = new MemoryStream(data))
                {
                    BinaryReader binaryReader = new BinaryReader(input);
                    binaryReader.ReadString();
                    dialogTitle = binaryReader.ReadString();
                    cols = binaryReader.ReadByte();
                    treeAttribute.FromBytes(binaryReader);
                }

                Inventory.FromTreeAttributes(treeAttribute);
                Inventory.ResolveBlocksOrItems();
                guiMarket = new GUIDialogCANMarketOwner(dialogTitle, Inventory, Pos, this.Api as ICoreClientAPI);
                /*Block block = Api.World.BlockAccessor.GetBlock(Pos);
                string text = block.Attributes?["openSound"]?.AsString();
                string text2 = block.Attributes?["closeSound"]?.AsString();
                AssetLocation assetLocation = (text == null) ? null : AssetLocation.Create(text, block.Code.Domain);
                AssetLocation assetLocation2 = (text2 == null) ? null : AssetLocation.Create(text2, block.Code.Domain);
                invDialog.OpenSound = (assetLocation ?? OpenSound);
                invDialog.CloseSound = (assetLocation2 ?? CloseSound);*/
                guiMarket.TryOpen();
            }

            if (packetid == 1001)
            {
                clientWorldAccessor.Player.InventoryManager.CloseInventory(Inventory);
                if (guiMarket?.IsOpened() ?? false)
                {
                    guiMarket?.TryClose();
                }

                guiMarket?.Dispose();
                guiMarket = null;
            }
        }
        private void updateGui()
        {
            for(int i =0;i<this.inventory.stocks.Length;i++)
            {
                this.guiMarket.SingleComposer.GetDynamicText("stock" + i).SetNewText((this.Inventory as InventoryCANMarketOnChest).stocks[i] < 999
                    ? (this.Inventory as InventoryCANMarketOnChest).stocks[i].ToString()
                    : "999+");
            }
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            this.inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
            this.ownerName = tree.GetString("ownerName");
            for(int i = 0; i < 4; i++)
            {
                this.inventory.stocks[i] = tree.GetInt("stockLeft" + i, 0);
            }
            this.InfiniteStocks = tree.GetBool("InfiniteStocks");
            
            this.StorePayment = tree.GetBool("StorePayment");
            this.UpdateMeshes();
            if (guiMarket != null)
            {
                updateGui();
            }
            if (this.Api == null)
                return;
            this.inventory.AfterBlocksLoaded(this.Api.World);                   
            if (this.Api.Side != EnumAppSide.Client)
                return;
            
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            ITreeAttribute tree1 = (ITreeAttribute)new TreeAttribute();
            this.inventory.ToTreeAttributes(tree1);
            tree["inventory"] = (IAttribute)tree1;
            tree.SetString("ownerName", ownerName);
            for(int i = 0; i < 4; i++)
            {
                tree.SetInt("stockLeft" + i, this.inventory.stocks[i]);
            }
            tree.SetBool("InfiniteStocks", this.InfiniteStocks);
            tree.SetBool("StorePayment", this.StorePayment);
        }
        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            base.OnBlockBroken(byPlayer);
            if(this.renderer != null)
            {
                this.renderer.Dispose();
            }
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            if (this.renderer != null)
            {
                this.renderer.Dispose();
            }
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            if (this.renderer != null)
            {
                this.renderer.Dispose();
            }
        }
    }
}
