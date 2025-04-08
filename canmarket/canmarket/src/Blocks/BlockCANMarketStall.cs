using canmarket.src.BE;
using canmarket.src.Blocks.Properties;
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
    public class BlockCANMarketStall: Block, ITexPositionSource
    {
        private ITexPositionSource tmpTextureSource;
        private string curType;
        public MarketStallProperties Props;
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
               /* if(textureCode == "oak-inside")
                {
                    this.tmpAssets["oak-inside"] = new AssetLocation("canmarket:" + this.Variant["wood"] + "-inside.png");
                    return this.getOrCreateTexPos(this.tmpAssets["oak-inside"]);
                }*/
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
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            this.Props = this.Attributes.AsObject<MarketStallProperties>(null, this.Code.Domain);
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, blockPos, byItemStack);
            if (canmarket.config.SAVE_SLOTS_STALL)
            {
                if (byItemStack != null)
                {
                    var entity = world.BlockAccessor.GetBlockEntity(blockPos);
                    if (entity != null)
                    {
                        int i = 0;
                        foreach (var slot_it in (entity as BECANMarketStall).inventory)
                        {
                            ItemStack itemStack = byItemStack.Attributes.GetItemstack(i.ToString());
                            if (itemStack != null)
                            {
                                (entity as BECANMarketStall).inventory[i].Itemstack = itemStack;
                            }
                            i++;
                        }
                    }

                }
            }
        }
        public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
        {
            return Lang.Get("canmarket:block-marketstall");
        }
        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            bool flag = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
            if (flag)
            {
                BECANMarketStall bect = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BECANMarketStall;
                if (bect != null)
                {
                    BlockPos targetPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position;
                    double y = byPlayer.Entity.Pos.X - ((double)targetPos.X + blockSel.HitPosition.X);
                    double dz = (double)((float)byPlayer.Entity.Pos.Z) - ((double)targetPos.Z + blockSel.HitPosition.Z);
                    float angleHor = (float)Math.Atan2(y, dz);
                    string type = bect.type;
                    bect.inventory.SetCorrectSlotSize(this.Props[type].QuantitySlots);
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
        public string GetType(IBlockAccessor blockAccessor, BlockPos pos)
        {
            BlockEntityGenericTypedContainer be = blockAccessor.GetBlockEntity(pos) as BlockEntityGenericTypedContainer;
            if (be != null)
            {
                return be.type;
            }
            return this.Props.DefaultType;
        }
        public override string GetHeldItemName(ItemStack itemStack)
        {
            string type = itemStack.Attributes.GetString("type", this.Props.DefaultType);
            return Lang.GetMatching(string.Format("canmarket:block-{0}-marketstall", type));
        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BECANMarketStall be = null;
            be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BECANMarketStall;
            be?.OnPlayerRightClick(byPlayer, blockSel);
            return true;
        }
        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            string cacheKey = "stallMeshRefs" + base.FirstCodePart(0);
            Dictionary<string, MultiTextureMeshRef> meshrefs = ObjectCacheUtil.GetOrCreate<Dictionary<string, MultiTextureMeshRef>>(capi, cacheKey, () => new Dictionary<string, MultiTextureMeshRef>());
            //string type = itemstack.Attributes.GetString("type", this.Attributes["defaultType"].AsString());
            string metalType = itemstack.Block.Variant["wood"];
            this.tmpAssets["buttons-outside"] = new AssetLocation("game:block/metal/sheet/" + metalType + "1.png");
            this.tmpAssets["glow-inside"] = new AssetLocation("game:block/machine/statictranslocator/rustyglow.png");
            this.tmpAssets["oak-inside"] = new AssetLocation("canmarket:" + itemstack.Block.Variant["wood"] + "-inside.png");

            if (metalType == "rusty")
            {
                this.tmpAssets["buttons-outside"] = new AssetLocation("game:block/metal/tarnished/rusty-iron.png");
            }
            /*string key = string.Concat(new string[]
            {
                type
            });*/
            if (!meshrefs.TryGetValue(metalType, out renderinfo.ModelRef))
            {
                var cshape = Vintagestory.API.Common.Shape.TryGet(capi, "canmarket:shapes/block/stall.json");
                Vec3f rot = (this.ShapeInventory == null) ? null : new Vec3f(this.ShapeInventory.rotateX, this.ShapeInventory.rotateY, this.ShapeInventory.rotateZ);

                MeshData mesh = this.GenMesh(capi, metalType, cshape, rot);
                meshrefs[metalType] = (renderinfo.ModelRef = capi.Render.UploadMultiTextureMesh(mesh));
            }
        }
        public MeshData GenMesh(ICoreClientAPI capi, string type, Shape cshape, Vec3f rotation = null)
        {
            Shape shape = capi.Assets.TryGet("canmarket:shapes/block/marketbase2.json").ToObject<Shape>();
            ITesselatorAPI tesselator = capi.Tesselator;
            this.tmpTextureSource = tesselator.GetTextureSource(this, 0, true);
            curAtlas = capi.BlockTextureAtlas;
            //AssetLocation shapeloc = cshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
            //Shape result = Vintagestory.API.Common.Shape.TryGet(capi, shapeloc);
            this.curType = type;
            //oak-inside "canmarket:{wood}-inside" block\wood\planks\  nn1
            //linen2 "game:block/linen"
            //linen "game:item/bag/linensack/linen"
            this.tmpAssets["linen2"] = new AssetLocation("game:block/linen.png");
            this.tmpAssets["linen"] = new AssetLocation("game:item/bag/linensack/linen.png");
            this.tmpAssets["oak-inside"] = new AssetLocation("canmarket:" + this.Variant["wood"] + "-inside.png");


            //this.tmpAssets["buttons-outside"] = new AssetLocation("game:block/metal/sheet/" + type + "1.png");
            //this.tmpAssets["glow-inside"] = new AssetLocation("game:block/machine/statictranslocator/rustyglow.png");


            /* if (type == "rusty")
             {
                 this.tmpAssets["buttons-outside"] = new AssetLocation("game:block/metal/tarnished/rusty-iron.png");
             }*/
            if (shape == null)
            {
                return new MeshData(true);
            }
            this.curType = type;
            MeshData mesh;
            tesselator.TesselateShape("stall", shape, out mesh, this, (rotation == null) ? new Vec3f(this.Shape.rotateX, this.Shape.rotateY, this.Shape.rotateZ) : rotation, 0, 0, 0, null, null);


            return mesh;
        }
        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            var drops = new ItemStack[]
            {
                this.OnPickBlock(world, pos)
            };
            if (canmarket.config.SAVE_SLOTS_STALL)
            {
                foreach (var it in drops)
                {
                    if (it.Block is BlockCANStall)
                    {
                        var entity = world.BlockAccessor.GetBlockEntity(pos);
                        if (entity != null)
                        {
                            int i = 0;
                            foreach (var slot_it in (entity as BECANMarketStall).inventory)
                            {
                                if (!slot_it.Empty)
                                {
                                    it.Attributes.SetItemstack(i.ToString(), slot_it.Itemstack);
                                }
                                i++;
                            }
                        }
                    }
                }
            }
            return drops;
        }
        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            ItemStack stack = new ItemStack(this, 1);
            BECANMarketStall be = world.BlockAccessor.GetBlockEntity(pos) as BECANMarketStall;
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
