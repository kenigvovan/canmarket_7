using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace canmarket.src.Inventories
{
    public class TMPTradeInv : InventoryBase
    {
        private ItemSlot[] slots;
        public ItemSlot[] Slots => this.slots;
        public TMPTradeInv(string inventoryID, ICoreAPI api, int slotsCount)
         : base(inventoryID, api)
        {
            this.slots = this.GenEmptySlots(slotsCount);
        }

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

        public override int Count => slots.Count();

        public override void FromTreeAttributes(ITreeAttribute tree) => this.slots = this.SlotsFromTreeAttributes(tree, this.slots);

        public override void ToTreeAttributes(ITreeAttribute tree) => this.SlotsToTreeAttributes(this.slots, tree);

    }
}
