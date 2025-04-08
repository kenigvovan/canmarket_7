using canmarket.src.BE.SupportClasses;
using canmarket.src.BEB;
using canmarket.src.GUI;
using canmarket.src.Inventories;
using canmarket.src.Render;
using canmarket.src.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace canmarket.src.BE
{
    public class BECANMarketSingle : BEMarket
    {
        public BECANMarketSingle(): base(2)
        {

        }
        protected override MeshData getOrCreateMesh(ItemStack stack, int index)
        {
            MeshData mesh = this.getMesh(stack);
            //this.MeshCache.Clear();
            if (mesh != null)
            {
                return mesh;
            }
            IContainedMeshSource meshSource = stack.Collectible as IContainedMeshSource;
            if (meshSource != null)
            {
                mesh = meshSource.GenMesh(stack, (this.Api as ICoreClientAPI).BlockTextureAtlas, this.Pos);
            }
            if (mesh == null)
            {
                ICoreClientAPI capi = this.Api as ICoreClientAPI;
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

                    mesh = BlockEntityMicroBlock.CreateMesh((this.Api as ICoreClientAPI), voxelCuboids, materials, null, null, originalCuboids);
                    mesh.Translate(0f, -3f, 0f);
                    mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.15f, 0.15f, 0.15f);
                }
                else if (stack.Class == EnumItemClass.Block)
                {
                    if (stack.Block is BlockClutter)
                    {
                        Dictionary<string, MultiTextureMeshRef> clutterMeshRefs = ObjectCacheUtil.GetOrCreate<Dictionary<string, MultiTextureMeshRef>>(capi, (stack.Block as BlockShapeFromAttributes).ClassType + "MeshesInventory", () => new Dictionary<string, MultiTextureMeshRef>());
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
                    mesh.Translate(0f, -0.5f, 0f);
                    mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.45f, 0.45f, 0.45f);
                }
                else
                {
                    this.nowTesselatingObj = stack.Collectible;
                    this.nowTesselatingShape = null;
                    CompositeShape shape = stack.Item.Shape;
                    if (((shape != null) ? shape.Base : null) != null)
                    {
                        this.nowTesselatingShape = capi.TesselatorManager.GetCachedShape(stack.Item.Shape.Base);
                    }
                    capi.Tesselator.TesselateItem(stack.Item, out mesh, this);
                    mesh.RenderPassesAndExtraBits.Fill((short)EnumChunkRenderPass.BlendNoCull);
                }
            }
            JsonObject attributes = stack.Collectible.Attributes;
            if (attributes != null && attributes[this.AttributeTransformCode].Exists)
            {
                JsonObject attributes2 = stack.Collectible.Attributes;
                ModelTransform transform = (attributes2 != null) ? attributes2[this.AttributeTransformCode].AsObject<ModelTransform>(null) : null;
                transform.EnsureDefaultValues();
                mesh.ModelTransform(transform);
            }
            else if (attributes != null && attributes["onshelfTransform"].Exists)
            {
                JsonObject attributes3 = stack.Collectible.Attributes;
                if (attributes3 != null && attributes3["onDisplayTransform"].Exists)
                {
                    JsonObject attributes4 = stack.Collectible.Attributes;
                    ModelTransform transform2 = (attributes4 != null) ? attributes4["onDisplayTransform"].AsObject<ModelTransform>(null) : null;
                    transform2.EnsureDefaultValues();
                    mesh.ModelTransform(transform2);
                }
            }
            mesh.Translate(0f, 2f/16, 0f);

            
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
            else if (stack.Collectible.Code.Path.Contains("axe-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.6f, 0.6f, 0.6f);
                mesh.Translate(-0.01f, -0.09f, 0.03f);
            }
            else if (stack.Collectible.Code.Path.Contains("knife-"))
            {
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), ((float)Math.PI / 2), 0.0f, 0.0f);
                mesh.Translate(-0.0f, -0.32f, 0.25f);
            }
            else if (stack.Collectible.Code.Path.Contains("knifeblade-"))
            {
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), ((float)Math.PI / 2), 0.0f, 0.0f);
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
            else if (stack.Collectible.Code.Path.Contains("hoehead-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.5f, 0.5f, 0.5f);
                mesh.Translate(0.05f, -0.13f, 0.03f);
            }
            else if (stack.Collectible.Code.Path.Contains("saw-") || stack.Collectible.Code.Path.Contains("sawblade-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.65f, 0.65f, 0.65f);
                mesh.Translate(0.05f, -0.3f, 0.07f);
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
            else if (stack.Collectible.Code.Path.Contains("armor-legs"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.65f, 0.65f, 0.65f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 0f, -1.6f);
                mesh.Translate(-0.15f, -0.2f, -0.09f);
            }
            else if (stack.Collectible.Code.Path.Contains("armor-head"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.75f, 0.75f, 0.75f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, -1.57f, 0f);
                mesh.Translate(0.0f, -1.25f, 0.03f);
            }
            else if (stack.Collectible.Code.Path.Contains("armor-body"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.75f, 0.75f, 0.75f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 0f, -1.6f);
                mesh.Translate(-0.5f, -0.25f, 0.03f);
            }
            else if (stack.Collectible.Code.Path.Contains("book"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.75f, 0.75f, 0.75f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 0f, -1.6f);
                mesh.Translate(0.1f, -0.35f, 0.03f);
            }

            string key = this.getMeshCacheKey(stack);
            this.MeshCache[key] = mesh;
            return mesh;
        }       
        protected override float[][] genTransformationMatrices()
        {
            float[][] tfMatrices = new float[1][];
            for (int index = 0; index < 1; index++)
            {
                //float x = 0.5f;
               // float x = 0;
                //float y = 0.063125f;
               // float z = 0.5f;
                //float z = 0;
                int rnd = GameMath.MurmurHash3Mod(this.Pos.X, this.Pos.Y + index * 50, this.Pos.Z, 30) - 15;
                ItemSlot itemSlot = this.inventory[index];
                JsonObject jsonObject;
                
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


                var matrix = new Matrixf();
                    //.Rotate(0f, 1.57f, 0f)
                   // .Translate(0.85f, 0.05f, 0.85f)
                    //.Translate(x - 0.5f, y, z - 0.5f)
                    //.RotateYDeg(degY)
                    //.Scale(0.75f, 0.75f, 0.75f);
                    //.Translate(-0.5f, 0f, -0.5f);
                if (this.facing == BlockFacing.EAST)
                {
                    matrix.Rotate( 0f, 1.57f, 0f);
                    matrix.Translate(-1f, 0.05f, 0f);
                }
                else if (this.facing == BlockFacing.WEST)
                {
                    matrix.Rotate( 0f, -1.57f, 0f);
                    matrix.Translate(0f, 0.05f, -1f);
                }
                else if (this.facing == BlockFacing.NORTH)
                {
                    matrix.Rotate(0, 3.14f, 0f);
                    matrix.Translate(-1f, 0.05f, -1f);
                }
                else
                {
                    matrix.Rotate( 0, 0, 0f);
                }


                // matrix.Rotate(0f, 1.57f, 0f);

                //for north
                bool facingTranslate;
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
                
                
                /*if(facingTranslate)
                {
                    matrix.Translate(0, 3f/16, 0);
                }*/
                tfMatrices[index] = matrix.Values;
            }
            return tfMatrices;
        }
    }
}
