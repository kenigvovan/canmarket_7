using canmarket.src.Inventories.slots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace canmarket.src.Inventories
{
    public abstract class CANCostItemSlotAbstract: CANNoPerishItemSlot
    {
        public CANCostItemSlotAbstract(InventoryBase inventory) : base(inventory)
        {
        }
        public override int TryPutInto(IWorldAccessor world, ItemSlot sinkSlot, int quantity = 1)
        {
            return 0;
        }
        public override int TryPutInto(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
        {
            return 0;
        }
        protected override void FlipWith(ItemSlot withSlot)
        {
            return;
        }
        public override bool TryFlipWith(ItemSlot itemSlot)
        {
            return false;
        }
        public override bool CanHold(ItemSlot sourceSlot)
        {
            return false;
        }
        public override bool CanTake()
        {
            return false;
        }
        public override void ActivateSlot(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            base.ActivateSlot(sourceSlot, ref op);
        }
        public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
        {
            return base.CanTakeFrom(sourceSlot, priority);
        }     
    }
}
