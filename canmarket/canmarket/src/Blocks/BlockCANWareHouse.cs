using canmarket.src.BE;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace canmarket.src.Blocks
{
    public class BlockCANWareHouse: Block, ITexPositionSource
    {
        public CrateProperties Props;
        private ITexPositionSource tmpTextureSource;
        private string curType;
        private ITextureAtlasAPI curAtlas;
        public Dictionary<string, AssetLocation> tmpAssets = new Dictionary<string, AssetLocation>();
        public Size2i AtlasSize
        {
            get
            {
                return this.tmpTextureSource.AtlasSize;
            }
        }
        private TextureAtlasPosition getOrCreateTexPos(AssetLocation texturePath)
        {
            TextureAtlasPosition texPos = curAtlas[texturePath];
            if (texPos == null)
            {
                IAsset asset = (this.api as ICoreClientAPI).Assets.TryGet(texturePath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
                if (asset != null)
                {
                    BitmapRef bitmap = asset.ToBitmap((this.api as ICoreClientAPI));
                    (this.api as ICoreClientAPI).BlockTextureAtlas.InsertTextureCached(texturePath, (IBitmap)bitmap, out int _, out texPos);
                }
                else
                {
                    (this.api as ICoreClientAPI).World.Logger.Warning("For render in block " + this.Code?.ToString() + ", item {0} defined texture {1}, not no such texture found.", "", (object)texturePath);
                }
            }
            return texPos;
        }
        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                if (tmpAssets.TryGetValue(textureCode, out var assetCode))
                {
                    return this.getOrCreateTexPos(assetCode);
                }
                TextureAtlasPosition pos = this.tmpTextureSource[this.curType + "-" + textureCode];
                if (pos == null)
                {
                    pos = this.tmpTextureSource[textureCode];
                }
                if (pos == null)
                {
                    pos = (this.api as ICoreClientAPI).BlockTextureAtlas.UnknownTexturePosition;
                }
                return pos;
            }
        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BECANWareHouse be = null;
            if (blockSel.Position != null)
            {
                be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BECANWareHouse;
            }


            if (be != null)
            {
                be.OnPlayerRightClick(byPlayer, blockSel);
            return true;
            }

                
            

            return false;
        }
        public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
        {
            return Lang.Get("canmarket:block-warehouse");
            StringBuilder stringBuilder = new StringBuilder();
            BECANWareHouse be = world.BlockAccessor.GetBlockEntity(pos) as BECANWareHouse;
            if (be != null)
            {
                stringBuilder.Append(be.GetPlacedBlockName());
            }
            else
            {
                stringBuilder.Append(OnPickBlock(world, pos)?.GetName());
            }
            BlockBehavior[] blockBehaviors = BlockBehaviors;
            for (int i = 0; i < blockBehaviors.Length; i++)
            {
                blockBehaviors[i].GetPlacedBlockName(stringBuilder, world, pos);
            }

            return stringBuilder.ToString().TrimEnd();
        }
        public override string GetHeldItemName(ItemStack itemStack)
        {
            string type = itemStack.Attributes.GetString("type", this.Props.DefaultType);
            return Lang.GetMatching(string.Format("canmarket:block-{0}-warehouse", type));
        }
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            this.Props = this.Attributes.AsObject<CrateProperties>(null, this.Code.Domain);
            //this.PriorityInteract = true;
        }
        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            bool flag = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
            if (flag)
            {
                BECANWareHouse bect = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BECANWareHouse;
                if (bect != null)
                {
                    BlockPos targetPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position;
                    double y = byPlayer.Entity.Pos.X - ((double)targetPos.X + blockSel.HitPosition.X);
                    double dz = (double)((float)byPlayer.Entity.Pos.Z) - ((double)targetPos.Z + blockSel.HitPosition.Z);
                    float angleHor = (float)Math.Atan2(y, dz);
                    string type = bect.type;
                    string rotatatableInterval = this.Props[type].RotatatableInterval;
                    if (rotatatableInterval == "22.5degnot45deg")
                    {
                        float rounded90degRad = (float)((int)Math.Round((double)(angleHor / 1.5707964f))) * 1.5707964f;
                        float deg45rad = 0.3926991f;
                        if (Math.Abs(angleHor - rounded90degRad) >= deg45rad)
                        {
                            bect.MeshAngle = rounded90degRad + 0.3926991f * (float)Math.Sign(angleHor - rounded90degRad);
                        }
                        else
                        {
                            bect.MeshAngle = rounded90degRad;
                        }
                    }
                    if (rotatatableInterval == "22.5deg")
                    {
                        float deg22dot5rad = 0.3926991f;
                        float roundRad = (float)((int)Math.Round((double)(angleHor / deg22dot5rad))) * deg22dot5rad;
                        bect.MeshAngle = roundRad;
                    }
                }
            }
            return flag;
        }
        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            string cacheKey = "warehouseMeshRefs" + base.FirstCodePart(0);
            Dictionary<string, MultiTextureMeshRef> meshrefs = ObjectCacheUtil.GetOrCreate<Dictionary<string, MultiTextureMeshRef>>(capi, cacheKey, () => new Dictionary<string, MultiTextureMeshRef>());
            string type = itemstack.Attributes.GetString("type", this.Props.DefaultType);

            this.tmpAssets["metal"] = new AssetLocation("game:block/metal/sheet/" + type + "1.png");
            if (type == "rusty")
            {
                this.tmpAssets["metal"] = new AssetLocation("game:block/metal/tarnished/rusty-iron.png");
            }
            string key = string.Concat(new string[]
            {
                type
            });
            if (!meshrefs.TryGetValue(key, out renderinfo.ModelRef))
            {
                CompositeShape cshape = this.Props[type].Shape;
                Vec3f rot = (this.ShapeInventory == null) ? null : new Vec3f(this.ShapeInventory.rotateX, this.ShapeInventory.rotateY, this.ShapeInventory.rotateZ);

                MeshData mesh = this.GenMesh(capi, type, cshape, rot);
                meshrefs[key] = (renderinfo.ModelRef = capi.Render.UploadMultiTextureMesh(mesh));
            }
        }
        public MeshData GenMesh(ICoreClientAPI capi, string type, CompositeShape cshape, Vec3f rotation = null)
        {
            Shape shape = this.GetShape(capi, type, cshape);
            ITesselatorAPI tesselator = capi.Tesselator;
            curAtlas = capi.BlockTextureAtlas;
            this.tmpAssets["metal"] = new AssetLocation("game:block/metal/sheet/" + type + "1.png");
            if (type == "rusty")
            {
                this.tmpAssets["metal"] = new AssetLocation("game:block/metal/tarnished/rusty-iron.png");
            }
            if (shape == null)
            {
                return new MeshData(true);
            }
            this.curType = type;
            MeshData mesh;
            tesselator.TesselateShape("warehouse", shape, out mesh, this, (rotation == null) ? new Vec3f(this.Shape.rotateX, this.Shape.rotateY, this.Shape.rotateZ) : rotation, 0, 0, 0, null, null);


            return mesh;
        }
        public Shape GetShape(ICoreClientAPI capi, string type, CompositeShape cshape)
        {
            if (((cshape != null) ? cshape.Base : null) == null)
            {
                return null;
            }
            ITesselatorAPI tesselator = capi.Tesselator;
            this.tmpTextureSource = tesselator.GetTextureSource(this, 0, true);
            AssetLocation shapeloc = cshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
            Shape result = Vintagestory.API.Common.Shape.TryGet(capi, shapeloc);
            this.curType = type;
            return result;
        }
        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            return new ItemStack[]
            {
                this.OnPickBlock(world, pos)
            };
        }
        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            ItemStack stack = new ItemStack(this, 1);
            BECANWareHouse be = world.BlockAccessor.GetBlockEntity(pos) as BECANWareHouse;
            if (be != null)
            {
                stack.Attributes.SetString("type", be.type);
            }
            else
            {
                stack.Attributes.SetString("type", this.Props.DefaultType);
            }
            return stack;
        }
    }
}
