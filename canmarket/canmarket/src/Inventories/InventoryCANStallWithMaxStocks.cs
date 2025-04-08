using canmarket.src.BE;
using canmarket.src.BE.SupportClasses;
using canmarket.src.Inventories.slots.Stall;
using canmarket.src.Inventories.slots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;

namespace canmarket.src.Inventories
{
    public class InventoryCANStallWithMaxStocks: InventoryBase, ISlotProvider
    {
        private static readonly int _searchWarehouseDistance = canmarket.config.SEARCH_WAREHOUE_DISTANCE;
        protected ItemSlot[] slots;
        public BEStall be;
        public ItemSlot[] Slots => this.slots;
        public int WareHouseBookSlotId => 0;
        public int LogBookSlotId => 1;

        public override int Count => slots.Count();

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
        public InventoryCANStallWithMaxStocks(string inventoryID, ICoreAPI api, BEStall be, int slotsAmount = 74) : base(inventoryID, api)
        {
            this.be = be;
            this.slots = this.GenEmptySlotsInner(slotsAmount);
        }
        public virtual void LateInitialize(string inventoryID, ICoreAPI api, BEStall be)
        {
            base.LateInitialize(inventoryID, api);
            this.be = be;
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            throw new NotImplementedException();
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            throw new NotImplementedException();
        }
        public void SetCorrectSlotSize(int slotsAmount)
        {
            this.slots = this.GenEmptySlotsInner(slotsAmount);
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
            if (i == 0)
            {
                return new CANChestsListItemSlot((InventoryBase)this);
            }
            if (i == 1)
            {
                return new CANLogBookSItemSlot((InventoryBase)this);
            }
            if (((i - 2) % 3 == 0))
            {
                return (ItemSlot)new CANCostItemSlotStall((InventoryBase)this);
            }
            else if ((i - 2) % 3 == 1)
            {
                return (ItemSlot)new CANCostItemSlotStall((InventoryBase)this);
            }
            else
            {
                return (ItemSlot)new CANTakeOutItemSlotStall((InventoryBase)this);
            }
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
    }
}
