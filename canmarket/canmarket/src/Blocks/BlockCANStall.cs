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
    public class BlockCANStall: Block, ITexPositionSource
    {
        private ITexPositionSource tmpTextureSource;
        private string curType;
        public StallProperties Props;
        public Size2i AtlasSize
        {
            get
            {
                return this.tmpTextureSource.AtlasSize;
            }
        }

        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
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
            this.Props = this.Attributes.AsObject<StallProperties>(null, this.Code.Domain);
        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BECANStall be = null;
            if (blockSel.Position != null)
            {
                be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BECANStall;
            }

            if (byPlayer.WorldData.EntityControls.Sneak && blockSel.Position != null)
            {
                if (be != null)
                {
                    be.OnPlayerRightClick(byPlayer, blockSel);
                }

                return true;
            }

            if (!byPlayer.WorldData.EntityControls.Sneak && blockSel.Position != null)
            {
                if (be != null)
                {
                    be.OnPlayerRightClick(byPlayer, blockSel);
                }

                return true;
            }

            return false;
        }
        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, blockPos, byItemStack);
            if (Config.Current.SAVE_SLOTS_STALL.Val)
            {
                if (byItemStack != null)
                {
                    var entity = world.BlockAccessor.GetBlockEntity(blockPos);
                    if (entity != null)
                    {
                        int i = 0;
                        foreach (var slot_it in (entity as BECANStall).inventory)
                        {
                            ItemStack itemStack = byItemStack.Attributes.GetItemstack(i.ToString());
                            if (itemStack != null)
                            {
                                (entity as BECANStall).inventory[i].Itemstack = itemStack;
                            }
                            i++;
                        }
                    }

                }
            }
        }
        public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
        {
            StringBuilder stringBuilder = new StringBuilder();
            BECANStall be = world.BlockAccessor.GetBlockEntity(pos) as BECANStall;
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
        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            bool flag = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
            if (flag)
            {
                BECANStall bect = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BECANStall;
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
            return Lang.GetMatching(string.Format("canmarket:block-{0}-stall", type));
        }
        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            string cacheKey = "stallMeshRefs" + base.FirstCodePart(0);
            Dictionary<string, MultiTextureMeshRef> meshrefs = ObjectCacheUtil.GetOrCreate<Dictionary<string, MultiTextureMeshRef>>(capi, cacheKey, () => new Dictionary<string, MultiTextureMeshRef>());
            string type = itemstack.Attributes.GetString("type", this.Attributes["defaultType"].AsString());
            /*string key = string.Concat(new string[]
            {
                type
            });*/
            if (!meshrefs.TryGetValue(type, out renderinfo.ModelRef))
            {
                //+		[4030]	{[{canmarket:shapes/block/stall.json}, {canmarket:shapes/block/stall.json}]}	System.Collections.Generic.KeyValuePair<Vintagestory.API.Common.AssetLocation, Vintagestory.API.Common.IAsset>

                /*foreach(var it in api.Assets.AllAssets)
                {
                    if(it.Value.Name.Contains("stall"))
                    {
                        var c = 3;
                        c = 4;
                        c = 5;
                    }
                }*/
               // var ff = capi.Assets.TryGet("canmarket:shapes/block/stall.json").ToObject<Shape>();
                var cshape = Vintagestory.API.Common.Shape.TryGet(capi, "canmarket:shapes/block/stall.json");
                //  capi.Assets.TryGet("canmarket:shapes/block/stall.json").ToObject<Shape>();
                //CompositeShape cshape = this.Props[type].Shape;
                //ff = capi.Assets.TryGet("canmarket:shapes/block/stall.json").ToObject<Shape>();
                Vec3f rot = (this.ShapeInventory == null) ? null : new Vec3f(this.ShapeInventory.rotateX, this.ShapeInventory.rotateY, this.ShapeInventory.rotateZ);

                MeshData mesh = this.GenMesh(capi, type, cshape, rot);
                meshrefs[type] = (renderinfo.ModelRef = capi.Render.UploadMultiTextureMesh(mesh));
            }
        }
        public MeshData GenMesh(ICoreClientAPI capi, string type, Shape cshape, Vec3f rotation = null)
        {
            Shape shape = capi.Assets.TryGet("canmarket:shapes/block/stall.json").ToObject<Shape>();
            ITesselatorAPI tesselator = capi.Tesselator;
            this.tmpTextureSource = tesselator.GetTextureSource(this, 0, true);
            //AssetLocation shapeloc = cshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
            //Shape result = Vintagestory.API.Common.Shape.TryGet(capi, shapeloc);
            this.curType = type;

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
            var drops = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
            if (Config.Current.SAVE_SLOTS_STALL.Val)
            {
                foreach (var it in drops)
                {
                    if (it.Block is BlockCANStall)
                    {
                        var entity = world.BlockAccessor.GetBlockEntity(pos);
                        if (entity != null)
                        {
                            int i = 0;
                            foreach (var slot_it in (entity as BECANStall).inventory)
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
            BECANStall be = world.BlockAccessor.GetBlockEntity(pos) as BECANStall;
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
