using canmarket.src.Blocks;
using canmarket.src.GUI;
using canmarket.src.Inventories;
using canmarket.src.Render;
using canmarket.src.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace canmarket.src.BE
{
    public class BECANWareHouse : BlockEntityContainer
    {
        static Random rnd = new Random();
        public string type = "wood-aged";
        public InventoryCANWareHouse inventory;
        public override InventoryBase Inventory => this.inventory;

        public override string InventoryClassName => "canmarketwarehouse";
        GUIDialogCANWareHouse guiWareHouse;
        private BlockCANWareHouse ownBlock;
        private MeshData ownMesh;
        private static Vec3f origin = new Vec3f(0.5f, 0f, 0.5f);
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
        private float rndScale
        {
            get
            {
                return 1f + (float)(GameMath.MurmurHash3Mod(this.Pos.X, this.Pos.Y, this.Pos.Z, 100) - 50) / 1000f;
            }
        }
        private float rotAngleY;
        public Dictionary<string, int> quantities = new Dictionary<string, int>();
        public List<Vec3i> containerLocations = new List<Vec3i>();
        private int key = 1;
        private static readonly int _searchContainerRadius = canmarket.config.SEARCH_CONTAINER_RADIUS;
        public BECANWareHouse()
        {            
            this.inventory = new InventoryCANWareHouse((string)null, (ICoreAPI)null);
            this.inventory.Pos = this.Pos;
            this.inventory.OnInventoryClosed += new OnInventoryClosedDelegate(this.OnInventoryClosed);
            this.inventory.OnInventoryOpened += new OnInventoryOpenedDelegate(this.OnInvOpened);
            this.inventory.SlotModified += new Action<int>(this.OnSlotModified);            
        }
        public override void Initialize(ICoreAPI api)
        {
            this.ownBlock = (base.Block as BlockCANWareHouse);
            bool isNewlyplaced = this.inventory == null;
            if(key == 1)
            {
                do
                {
                    key = rnd.Next();
                }
                while (key == 1);          
            }
            base.Initialize(api);
            if (api.Side == EnumAppSide.Client && !isNewlyplaced)
            {
                this.loadOrCreateMesh();
            }
            this.inventory.LateInitialize("canmarketwarehouse-" + this.Pos.X.ToString() + "/" + this.Pos.Y.ToString() + "/" + this.Pos.Z.ToString(), api, this);
            this.inventory.Pos = this.Pos;
            this.MarkDirty(true);
        }

        //Events
        private void OnSlotModified(int slotNum)
        {
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
            this.guiWareHouse?.Dispose();
            this.guiWareHouse = (GUIDialogCANWareHouse)null;
        }
        protected virtual void OnInvOpened(IPlayer player) => this.inventory.PutLocked = false;
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
        }
        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            if (((byItemStack != null) ? byItemStack.Attributes : null) != null)
            {
                string nowType = byItemStack.Attributes.GetString("type", this.ownBlock.Props.DefaultType);
                if (nowType != this.type)
                {
                    this.type = nowType;
                    BlockContainer blockContainer = byItemStack?.Block as BlockContainer;
                    if (blockContainer != null)
                    {
                        ItemStack[] contents = blockContainer.GetContents(Api.World, byItemStack);
                        if (contents != null && contents.Length > Inventory.Count)
                        {
                            throw new InvalidOperationException($"OnBlockPlaced stack copy failed. Trying to set {contents.Length} stacks on an inventory with {Inventory.Count} slots");
                        }

                        int num = 0;
                        while (contents != null && num < contents.Length)
                        {
                            Inventory[num].Itemstack = contents[num]?.Clone();
                            num++;
                        }
                    }
                    this.Inventory.LateInitialize(string.Concat(new string[]
                    {
                        this.InventoryClassName,
                        "-",
                        this.Pos.X.ToString(),
                        "/",
                        this.Pos.Y.ToString(),
                        "/",
                        this.Pos.Z.ToString()
                    }), this.Api);
                    this.Inventory.ResolveBlocksOrItems();
                    this.Inventory.OnAcquireTransitionSpeed = new CustomGetTransitionSpeedMulDelegate(this.Inventory_OnAcquireTransitionSpeed);
                    this.MarkDirty(false, null);
                }
            }
            base.OnBlockPlaced(null);
        }
        //We check every blockentity around and count every item/block in it
        public void CalculateQuantitiesAround()
        {
            containerLocations.Clear();
            quantities.Clear();

            int startX = this.Pos.X - _searchContainerRadius;
            int endX = this.Pos.X + _searchContainerRadius;
            int startY = this.Pos.Y - _searchContainerRadius;
            int endY = this.Pos.Y + _searchContainerRadius;
            int startZ = this.Pos.Z - _searchContainerRadius;
            int endZ = this.Pos.Z + _searchContainerRadius;

            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    for (int z = startZ; z <= endZ; z++)
                    {
                        BlockEntity be = this.Api.World.BlockAccessor.GetBlockEntity(new BlockPos(x, y, z));

                        if (be is BlockEntityContainer && (be is BlockEntityGenericTypedContainer || be is BlockEntityCrate) )
                        {
                            containerLocations.Add(new Vec3i(x, y, z));
                            CalculateQuantityForContainer(be as BlockEntityContainer);
                        }
                    }
                }
            }
        }
        public void CalculateQuantityForContainer(BlockEntityContainer containerBE)
        {
            ItemStack tmpIS;
            foreach (var itSlot in containerBE.Inventory)
            {
                tmpIS = itSlot.Itemstack;
                if (tmpIS == null)
                {
                    continue;
                }
                string iSKey = tmpIS.Collectible.Code.Domain + tmpIS.Collectible.Code.Path;
                foreach(var it in tmpIS?.Attributes)
                {
                    if(canmarket.config.WAREHOUSE_ITEMSTACK_NOT_IGNORED_ATTRIBUTES.Contains(it.Key))
                    {
                        iSKey = iSKey + "-" + it.Value.ToString();
                    }
                }
                if (this.quantities.ContainsKey(iSKey))
                {
                    this.quantities[iSKey] += tmpIS.StackSize;
                }
                else
                {
                    this.quantities[iSKey] = tmpIS.StackSize;
                }
            }
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
            if (packetid == 1042)
            {
                ItemStack book = Inventory[0].Itemstack;
                if (book != null)
                {
                    ITreeAttribute tree = book.Attributes.GetTreeAttribute("warehouse");
                    if (tree == null)
                    {
                        tree = new TreeAttribute();
                    }
                    tree.SetVec3i("pos", Pos.ToVec3i());
                    tree.SetInt("num", key);
                    tree.SetString("byPlayer", player.PlayerName);
                    book.Attributes["warehouse"] = tree;
                    inventory.MarkSlotDirty(0);
                    inventory[0].MarkDirty();
                }
                return;
            }
        }

        //GUI
        protected void toggleInventoryDialogClient(IPlayer byPlayer)
        {
            if (guiWareHouse == null)
            {
                ICoreClientAPI capi = Api as ICoreClientAPI;
                foreach (var it in byPlayer.InventoryManager.OpenedInventories)
                {
                    if (it is InventoryCANWareHouse)
                    {
                        byPlayer.InventoryManager.CloseInventory(it);
                        capi.Network.SendBlockEntityPacket((it as InventoryCANWareHouse).be.Pos, 1001);
                        capi.Network.SendPacketClient(it.Close(byPlayer));
                        break;
                    }
                }

                guiWareHouse = new GUIDialogCANWareHouse("trade", Inventory, Pos, this.Api as ICoreClientAPI);
                guiWareHouse.OnClosed += delegate
                {
                    guiWareHouse = null;
                    capi.Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, 1001);
                    capi.Network.SendPacketClient(Inventory.Close(byPlayer));
                };

                guiWareHouse.TryOpen();
                capi.Network.SendPacketClient(Inventory.Open(byPlayer));
                capi.Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, 1000);
            }
            else
            {
                guiWareHouse.TryClose();
            }
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
            BlockCANWareHouse block = base.Block as BlockCANWareHouse;
            if (base.Block == null)
            {
                block = (this.Api.World.BlockAccessor.GetBlock(this.Pos) as BlockCANWareHouse);
                base.Block = block;
            }
            if (block == null)
            {
                return;
            }
            string cacheKey = "crateMeshes" + block.FirstCodePart(0);
            Dictionary<string, MeshData> meshes = ObjectCacheUtil.GetOrCreate<Dictionary<string, MeshData>>(this.Api, cacheKey, () => new Dictionary<string, MeshData>());
            CompositeShape cshape = this.ownBlock.Props[this.type].Shape;
            if (((cshape != null) ? cshape.Base : null) == null)
            {
                return;
            }
            ItemSlot firstNonEmptySlot = this.inventory.FirstNonEmptySlot;
            ItemStack firstStack = (firstNonEmptySlot != null) ? firstNonEmptySlot.Itemstack : null;
            string meshKey = string.Concat(new string[]
            {
                this.type
            });
            MeshData mesh;
            if (!meshes.TryGetValue(meshKey, out mesh))
            {
                mesh = block.GenMesh(this.Api as ICoreClientAPI, this.type, cshape, new Vec3f(cshape.rotateX, cshape.rotateY, cshape.rotateZ));
                meshes[meshKey] = mesh;
            }
            this.ownMesh = mesh.Clone().Rotate(BECANWareHouse.origin, 0f, this.MeshAngle, 0f).Scale(BECANWareHouse.origin, this.rndScale, this.rndScale, this.rndScale);
        }

        //Helpers
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            this.inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
            BlockCANWareHouse block = worldForResolving.GetBlock(new AssetLocation(tree.GetString("blockCode", null))) as BlockCANWareHouse;
            this.type = tree.GetString("type", (block != null) ? block.Props.DefaultType : null);
            this.MeshAngle = tree.GetFloat("meshAngle", this.MeshAngle);
            this.key = tree.GetInt("key");

            if (this.Api != null && this.Api.Side == EnumAppSide.Client)
            {
                this.loadOrCreateMesh();
                this.MarkDirty(true, null);
            }
            if (this.Api == null)
                return;
            this.inventory.AfterBlocksLoaded(this.Api.World);

        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            ITreeAttribute tree1 = (ITreeAttribute)new TreeAttribute();
            this.inventory.ToTreeAttributes(tree1);
            tree["inventory"] = (IAttribute)tree1;
            if (base.Block != null)
            {
                tree.SetString("blockCode", base.Block.Code.ToShortString());
            }
            if (this.type == null)
            {
                this.type = this.ownBlock.Props.DefaultType;
            }
            tree.SetString("type", this.type);
            tree.SetFloat("meshAngle", this.MeshAngle);
            tree.SetInt("key", this.key);
        }
        public int GetKey()
        {
            return this.key;
        }
        private bool CanPlaceItemStacksInContainers(ItemStack stack1, ItemStack stack2)
        {
            if (containerLocations.Count == 0)
            {
                return false;
            }

            if (stack1 != null && stack2 != null)
            {
                if (stack1.Collectible.Equals(stack1, stack2, canmarket.config.IGNORED_STACK_ATTRIBTES_ARRAY))
                {
                    //they are equals, sum them up and go as 1 item
                }
                else
                {
                    //2 different items
                }
            }
            else if (stack1 == null)
            {
                //only stack1
            }
            return false;
        }
        public bool ContainersContainCollectableWithQuantity(ItemStack itemStack)
        {
            if (quantities.Count == 0)
            {
                return false;
            }
            string iSKey = itemStack.Collectible.Code.Domain + itemStack.Collectible.Code.Path;
            foreach (var iter in itemStack?.Attributes)
            {
                if (canmarket.config.WAREHOUSE_ITEMSTACK_NOT_IGNORED_ATTRIBUTES.Contains(iter.Key))
                {
                    iSKey = iSKey + "-" + iter.Value.ToString();
                }
            }
            if (quantities.TryGetValue(iSKey, out int quantity))
            {
                if (quantity >= itemStack.StackSize)
                {
                    return true;
                }
            }
            return false;
        }

        //TODO make it less potato code
        public bool PlaceTakenPriceInContainers(TMPTradeInv tmpInv)
        {
            if (tmpInv[1].Itemstack == null)
            {
                int needToPut = tmpInv[0].StackSize;
                foreach (var itVec in containerLocations)
                {
                    BlockEntity be = this.Api.World.BlockAccessor.GetBlockEntity(new BlockPos(itVec));
                    if (be == null)
                    {
                        continue;
                    }
                    else if (be is BlockEntityCrate beCrate)
                    {
                        FieldInfo labelField = beCrate.GetType().GetField("labelStack", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (labelField != null)
                        {
                            ItemStack labelStack = (ItemStack)labelField.GetValue(beCrate);
                            if (labelStack != null && !labelStack.Collectible.Equals(labelStack, tmpInv[0].Itemstack))
                            {
                                continue;
                            }
                        }
                        //so a crate is empty
                        //or non empty slot has itemstack we're trying to place
                        if ((beCrate.Inventory.FirstNonEmptySlot == null ||
                            beCrate.Inventory.FirstNonEmptySlot.Itemstack.Collectible.Equals(beCrate.Inventory.FirstNonEmptySlot.Itemstack, tmpInv[0].Itemstack, canmarket.config.IGNORED_STACK_ATTRIBTES_ARRAY)) && UsefullUtils.IsReasonablyFresh(this.inventory.Api.World, tmpInv[0].Itemstack))
                        {
                            foreach (var itSlot in beCrate.Inventory)
                            {
                                needToPut -= tmpInv[0].TryPutInto(this.inventory.Api.World, itSlot, needToPut);
                                if (needToPut <= 0)
                                {
                                    beCrate.MarkDirty();
                                    return true;
                                }
                            }
                        }
                    }
                    else if(be is BlockEntityGenericTypedContainer beTypedGenericContainer)
                    {
                        foreach (var itSlot in beTypedGenericContainer.Inventory)
                        {
                            ItemStack iS = itSlot.Itemstack;
                            if (iS == null)
                            {
                                needToPut -= tmpInv[0].TryPutInto(this.inventory.Api.World, itSlot, Math.Min(tmpInv[0].Itemstack.StackSize, needToPut));
                                if (needToPut <= 0)
                                {
                                    return true;
                                }
                                continue;
                            }
                            if (iS.Collectible.Equals(iS, tmpInv[0].Itemstack, canmarket.config.IGNORED_STACK_ATTRIBTES_ARRAY) && UsefullUtils.IsReasonablyFresh(this.inventory.Api.World, tmpInv[0].Itemstack))
                            {
                                needToPut -= tmpInv[0].TryPutInto(this.inventory.Api.World, itSlot, needToPut);
                                if (needToPut <= 0)
                                {
                                    return true;
                                }
                            }

                        }
                    }
                }
            }
            else
            {
                int needToPut1 = tmpInv[0].StackSize;
                int needToPut2 = tmpInv[1].StackSize;
                foreach (var itVec in containerLocations)
                {
                    BlockEntity be = this.Api.World.BlockAccessor.GetBlockEntity(new BlockPos(itVec));

                    if(be == null)
                    {
                        continue;
                    }
                    else if (be is BlockEntityCrate beCrate)
                    {
                        //so a crate is empty
                        //or non empty slot has itemstack we're trying to place
                        //in NormalizedPrice we check that two slots are not the same or make ONE even with bigger stacksize than maxStackSize
                        if ((beCrate.Inventory.FirstNonEmptySlot == null ||
                            beCrate.Inventory.FirstNonEmptySlot.Itemstack.Collectible.Equals(beCrate.Inventory.FirstNonEmptySlot.Itemstack, tmpInv[0].Itemstack, canmarket.config.IGNORED_STACK_ATTRIBTES_ARRAY)) && UsefullUtils.IsReasonablyFresh(this.inventory.Api.World, tmpInv[0].Itemstack))
                        {
                            foreach (var itSlot in beCrate.Inventory)
                            {
                                needToPut1 -= tmpInv[0].TryPutInto(this.inventory.Api.World, itSlot, needToPut1);
                                if (needToPut1 <= 0)
                                {
                                    return true;
                                }
                            }
                        }
                        else if ((beCrate.Inventory.FirstNonEmptySlot == null ||
                            beCrate.Inventory.FirstNonEmptySlot.Itemstack.Collectible.Equals(beCrate.Inventory.FirstNonEmptySlot.Itemstack, tmpInv[1].Itemstack, canmarket.config.IGNORED_STACK_ATTRIBTES_ARRAY)) && UsefullUtils.IsReasonablyFresh(this.inventory.Api.World, tmpInv[1].Itemstack))
                        {
                            foreach (var itSlot in beCrate.Inventory)
                            {
                                needToPut2 -= tmpInv[1].TryPutInto(this.inventory.Api.World, itSlot, needToPut2);
                                if (needToPut2 <= 0)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                    else if (be is BlockEntityGenericTypedContainer beTypedGenericContainer)
                    {
                        foreach (var itSlot in (be as BlockEntityGenericTypedContainer).Inventory)
                        {
                            ItemStack iS = itSlot.Itemstack;
                            if (iS == null)
                            {
                                if (tmpInv[0].Itemstack != null)
                                {
                                    needToPut1 -= tmpInv[0].TryPutInto(this.inventory.Api.World, itSlot, Math.Min(tmpInv[0].Itemstack.StackSize, needToPut1));
                                    if (needToPut1 <= 0 && needToPut2 <= 0)
                                    {
                                        return true;
                                    }
                                }

                                if (tmpInv[1].Itemstack != null)
                                {
                                    needToPut2 -= tmpInv[1].TryPutInto(this.inventory.Api.World, itSlot, Math.Min(tmpInv[1].Itemstack.StackSize, needToPut2));
                                    if (needToPut1 <= 0 && needToPut2 <= 0)
                                    {
                                        return true;
                                    }
                                }

                                continue;
                            }
                            if (needToPut1 > 0 && iS.Collectible.Equals(iS, tmpInv[0].Itemstack, canmarket.config.IGNORED_STACK_ATTRIBTES_ARRAY) && UsefullUtils.IsReasonablyFresh(this.inventory.Api.World, tmpInv[0].Itemstack))
                            {
                                needToPut1 -= tmpInv[0].TryPutInto(this.inventory.Api.World, itSlot, needToPut1);
                                if (needToPut1 <= 0 && needToPut2 <= 0)
                                {
                                    return true;
                                }
                            }
                            else if (needToPut2 > 0 && iS.Collectible.Equals(iS, tmpInv[1].Itemstack, canmarket.config.IGNORED_STACK_ATTRIBTES_ARRAY) && UsefullUtils.IsReasonablyFresh(this.inventory.Api.World, tmpInv[1].Itemstack))
                            {
                                needToPut2 -= tmpInv[1].TryPutInto(this.inventory.Api.World, itSlot, needToPut2);
                                if (needToPut1 <= 0 && needToPut2 <= 0)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                   
                }
            }
            return false;
        }
        public string GetPlacedBlockName()
        {
            return Lang.Get(string.Format("canmarket:block-{0}-warehouse", type));
        }
    }
}
