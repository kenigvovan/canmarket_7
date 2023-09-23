using canmarket.src.BE;
using canmarket.src.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace canmarket.src.Inventories.slots.Stall
{
    public class CANLogBookSItemSlot : ItemSlot
    {
        public CANLogBookSItemSlot(InventoryBase inventory) : base(inventory)
        {
        }
        public override bool CanHold(ItemSlot sourceSlot)
        {
            //this slot only for book
            if (sourceSlot.Itemstack == null || !(sourceSlot.Itemstack.Item is ItemBook))
            {
                return false;
            }
            return base.CanHold(sourceSlot);
        }
        protected override void ActivateSlotLeftClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            BECANStall be = (this.inventory as InventoryCANStall).be;

            if (be.adminShop)
            {
                return;
            }
            if (!op.ActingPlayer.PlayerUID.Equals(be.ownerUID))
            {
                return;
            }
            base.ActivateSlotLeftClick(sourceSlot, ref op);
        }
        protected override void ActivateSlotRightClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            return;
            base.ActivateSlotRightClick(sourceSlot, ref op);
        }
        protected override void FlipWith(ItemSlot withSlot)
        {
            return;
        }
        public override bool TryFlipWith(ItemSlot itemSlot)
        {
            return false;
        }
        public override int TryPutInto(IWorldAccessor world, ItemSlot sinkSlot, int quantity = 1)
        {
            return 0;
        }
        public override int TryPutInto(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
        {
            if (op.ShiftDown)
            {
                return 0;
            }
            return base.TryPutInto(sinkSlot, ref op);
        }
        public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
        {
            return false;
        }
    }
}