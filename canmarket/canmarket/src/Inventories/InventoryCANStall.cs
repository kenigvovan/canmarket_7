using canmarket.src.BE;
using canmarket.src.Inventories.slots;
using canmarket.src.Inventories.slots.Stall;
using canmarket.src.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace canmarket.src.Inventories
{
    public class InventoryCANStall : InventoryBase, ISlotProvider
    {
        private ItemSlot[] slots;
        public ItemSlot[] Slots => this.slots;
        public BECANStall be;
        public int slotsCount;
        private static readonly int _searchWarehouseDistance = canmarket.config.SEARCH_WAREHOUE_DISTANCE;

        public int WareHouseBookSlotId => 0;
        public int LogBookSlotId => 1;
        // 2 slots should be warehouse book and book for log       
        public InventoryCANStall(string inventoryID, ICoreAPI api, int slotsAmount = 74)
          : base(inventoryID, api)
        {
            this.slots = this.GenEmptySlotsInner(slotsAmount);
            //stocks = new int[slotsAmount / 2 - 1];
        }
        public void SetCorrectSlotSize(int slotsAmount)
        {
            this.slots = this.GenEmptySlotsInner(slotsAmount);
        }
        public bool existWarehouse(int warehouseX, int warehouseY, int warehouseZ, int key, IWorldAccessor world)
        {
            double distance = Math.Sqrt(Math.Pow(this.Pos.X - warehouseX, 2) + Math.Pow(this.Pos.Y - warehouseY, 2) + Math.Pow(this.Pos.Z - warehouseZ, 2));

            if (distance > _searchWarehouseDistance)
                return false;

            BlockEntity wareHouse = world.BlockAccessor.GetBlockEntity(new BlockPos(warehouseX, warehouseY, warehouseZ));

            if (wareHouse == null)
            {
                return false;
            }
            return (wareHouse as BECANWareHouse).GetKey() == key;
        }
        public override void OnItemSlotModified(ItemSlot slot)
        {
            //check if it is not too far away from
            //on list added to 0 slot
            if(this.GetSlotId(slot) == 0)
            {
                ItemStack book = slot.Itemstack;
                if (book != null && book.Item is ItemCANStallBook) 
                {
                    ITreeAttribute tree = book.Attributes.GetTreeAttribute("warehouse");
                    if(tree == null)
                    {
                        return;
                    }
                    if(existWarehouse(tree.GetInt("posX"), tree.GetInt("posY"), tree.GetInt("posZ"), tree.GetInt("num"), this.Api.World))
                    {
                        this.be.MarkDirty(true);
                    }
                
                }
            }
            base.OnItemSlotModified(slot);
        }
        public ItemSlot[] GenEmptySlotsInner(int quantity)
        {
            ItemSlot[] array = new ItemSlot[quantity];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = NewSlotInner(i);
            }

            return array;
        }
        protected ItemSlot NewSlotInner(int i)
        {
            if(i == 0)
            {
                return new CANChestsListItemSlot((InventoryBase)this);
            }
            if(i == 1)
            {
                return new CANLogBookSItemSlot((InventoryBase)this);
            }
            if (((i - 2) % 3 == 0))
            {
                return (ItemSlot)new CANCostItemSlotStall((InventoryBase)this);
            }
            else if((i - 2) % 3 == 1)
            {
                return (ItemSlot)new CANCostItemSlotStall((InventoryBase)this);
            }
            else
            {
                return (ItemSlot)new CANTakeOutItemSlotStall((InventoryBase)this);
            }
        }
        public override int Count => slots.Length;

        public override ItemSlot this[int slotId]
        {
            get => slotId < 0 || slotId >= this.Count ? (ItemSlot)null : this.slots[slotId];
            set
            {
                if (slotId < 0 || slotId >= this.Count)
                    throw new ArgumentOutOfRangeException(nameof(slotId));
                this.slots[slotId] = value != null ? value : throw new ArgumentNullException(nameof(value));
            }
        }
        public virtual void LateInitialize(string inventoryID, ICoreAPI api, BECANStall be)
        {
            base.LateInitialize(inventoryID, api);
            this.be = be;
        }

        public override void FromTreeAttributes(ITreeAttribute tree) => this.slots = this.SlotsFromTreeAttributes(tree, this.slots);

        public override void ToTreeAttributes(ITreeAttribute tree) => this.SlotsToTreeAttributes(this.slots, tree);

        protected override ItemSlot NewSlot(int i) => (ItemSlot)new ItemSlotSurvival((InventoryBase)this);

        public override float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge) => targetSlot == this.slots[0] && sourceSlot.Itemstack.Collectible.GrindingProps != null ? 4f : base.GetSuitability(sourceSlot, targetSlot, isMerge);

        public override ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
        {
            return null;
        }
        public override ItemSlot GetAutoPullFromSlot(BlockFacing atBlockFace)
        {
            return null;
        }
        public override bool CanContain(ItemSlot sinkSlot, ItemSlot sourceSlot)
        {
            int slotId = this.GetSlotId(sinkSlot);
            if (slotId == 0 || slotId == 1)
            {
                return true;
            }
            return false;
            if (sourceSlot.Itemstack == null)
            {
                return false;
            }
            return base.CanContain(sinkSlot, sourceSlot);
        }
        public override void DropAll(Vec3d pos, int maxStackSize = 0)
        {
            //drop only 0, 1 slots with book, other slots are clones
            int i = 0;
            foreach(var it in this.slots)
            {
                if(i > 1)
                {
                    break;
                }
                i++;
                if (it == null || it.Itemstack == null)
                {
                    continue;
                }
                
                if (maxStackSize > 0)
                {
                    while (it.Itemstack.StackSize > 0)
                    {
                        ItemStack itemstack = it.TakeOut(GameMath.Clamp(it.StackSize, 1, maxStackSize));
                        Api.World.SpawnItemEntity(itemstack, pos);
                    }
                }
                else
                {
                    Api.World.SpawnItemEntity(it.Itemstack, pos);
                }

                it.Itemstack = null;
                it.MarkDirty();
            }
        }
    }
}
