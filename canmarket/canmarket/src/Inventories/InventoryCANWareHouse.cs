using canmarket.src.BE;
using canmarket.src.Items;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace canmarket.src.Inventories
{
    public class InventoryCANWareHouse : InventoryBase, ISlotProvider
    {
        public override int Count => 1;
        public ItemSlot[] Slots => this.slots;
        public BECANWareHouse be;

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
        private ItemSlot[] slots;

        public InventoryCANWareHouse(string className, string instanceID, ICoreAPI api) : base(className, instanceID, api)
        {
            this.slots = this.GenEmptySlotsInner(this.Count);
        }
        public InventoryCANWareHouse(string inventoryID, ICoreAPI api, int slotsAmount = 1)
                                                                                        : base(inventoryID, api)
        {
            this.slots = this.GenEmptySlotsInner(slotsAmount);
        }
        public ItemSlot[] GenEmptySlotsInner(int quantity)
        {
            ItemSlot[] array = new ItemSlotSurvival[quantity];
            array[0] = new ItemSlotSurvival(this);
            return array;
        }

        public override object ActivateSlot(int slotId, ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            return base.ActivateSlot(slotId, sourceSlot, ref op);
        }
        public virtual void LateInitialize(string inventoryID, ICoreAPI api, BECANWareHouse be)
        {
            base.LateInitialize(inventoryID, api);
            this.be = be;
        }
        public override bool CanContain(ItemSlot sinkSlot, ItemSlot sourceSlot)
        {
            var t = sourceSlot.Itemstack != null && sourceSlot.Itemstack.Item is ItemCANStallBook && base.CanContain(sinkSlot, sourceSlot);
            return t;
        }
        public override void FromTreeAttributes(ITreeAttribute tree) => this.slots = this.SlotsFromTreeAttributes(tree, this.slots);

        public override void ToTreeAttributes(ITreeAttribute tree) => this.SlotsToTreeAttributes(this.slots, tree);
    }
}
