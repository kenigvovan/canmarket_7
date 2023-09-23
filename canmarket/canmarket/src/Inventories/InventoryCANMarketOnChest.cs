using canmarket.src.BE;
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
    public class InventoryCANMarketOnChest : InventoryBase, ISlotProvider
    {
        private ItemSlot[] slots;
        public int[] stocks;
        public ItemSlot[] Slots => this.slots;
        public BECANMarket be;
        public int slotsCount;
        public InventoryCANMarketOnChest(string inventoryID, ICoreAPI api, int slotsAmount = 8)
          : base(inventoryID, api)
        {
            this.slots = this.GenEmptySlotsInner(slotsAmount);
            stocks = new int[slotsAmount / 2];
        }

        /*public InventoryCANMarket(string className, string instanceID, ICoreAPI api)
          : base(className, instanceID, api)
        {
            this.slots = this.GenEmptySlotsInner(10);
        }*/
        public override void OnItemSlotModified(ItemSlot slot)
        {
            base.OnItemSlotModified(slot);
            //this.lastChangedSinceServerStart = 1;
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
            if ((i % 2 == 0))
            {
                return (ItemSlot)new CANCostItemSlotOnChest((InventoryBase)this);
            }
            else
            {
               return (ItemSlot)new CANTakeOutItemSlotOnChest((InventoryBase)this);
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
        public virtual void LateInitialize(string inventoryID, ICoreAPI api, BECANMarket be)
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
            return false;
            if (sourceSlot.Itemstack == null)
            {
                return false;
            }
            return base.CanContain(sinkSlot, sourceSlot);
        }
        public override void DropAll(Vec3d pos, int maxStackSize = 0)
        {
            //now we will only have clone slots
        }
    }
}
