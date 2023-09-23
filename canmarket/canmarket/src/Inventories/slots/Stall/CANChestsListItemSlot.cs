using canmarket.src.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace canmarket.src.Inventories.slots
{
    public class CANChestsListItemSlot: ItemSlot
    {
        public CANChestsListItemSlot(InventoryBase inventory) : base(inventory)
        {
        }

        protected override void ActivateSlotLeftClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            string ownerUID = (inventory as InventoryCANStall).be.ownerUID;
            if ((inventory as InventoryCANStall).be.adminShop && !op.ActingPlayer.PlayerUID.Equals(ownerUID))
            {
                return;
            }
            if (this.inventory.GetSlotId(this) != 0)
            {
                return;
            }
            if (Empty)
            {
                if (CanHold(sourceSlot))
                {
                    itemstack = sourceSlot.TakeOut(1);
                    sourceSlot.OnItemSlotModified(itemstack);
                    base.OnItemSlotModified(itemstack);
                    (inventory as InventoryCANStall).be.ownerName = op.ActingPlayer.PlayerName;
                    (inventory as InventoryCANStall).be.ownerUID = op.ActingPlayer.PlayerUID;
                    if(this.inventory.Api.Side == EnumAppSide.Client)
                    {
                        (this.inventory as InventoryCANStall).be.updateGuiOwner();
                    }
                }
            }
            else if (sourceSlot.Empty)
            {
                op.RequestedQuantity = (int)Math.Ceiling((float)itemstack.StackSize / 2f);
                if(base.TryPutInto(sourceSlot, ref op) > 0)
                {
                    (inventory as InventoryCANStall).be.ownerName = "";
                    (inventory as InventoryCANStall).be.ownerUID = "";
                    if (this.inventory.Api.Side == EnumAppSide.Client)
                    {
                        (this.inventory as InventoryCANStall).be.updateGuiOwner();
                    }
                }
            }
            else
            {
                op.RequestedQuantity = 1;
                sourceSlot.TryPutInto(this, ref op);
                if (op.MovedQuantity <= 0)
                {
                    base.TryFlipWith(sourceSlot);
                }
            }
            //|| sourceSlot.Itemstack == null || !(sourceSlot.Itemstack.Item is ItemCANChestsList)
           // base.ActivateSlotRightClick(sourceSlot, ref op);
        }
        protected override void ActivateSlotRightClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            if (!op.ActingPlayer.PlayerName.Equals((inventory as InventoryCANStall).be.ownerName))
            {
                return;
            }
            if (this.inventory.GetSlotId(this) != 0 || sourceSlot.Itemstack == null || !(sourceSlot.Itemstack.Item is ItemCANStallBook))
            {
                return;
            }
            base.ActivateSlotRightClick(sourceSlot, ref op);
        }
        public override int TryPutInto(IWorldAccessor world, ItemSlot sinkSlot, int quantity = 1)
        {
            return 0;
        }
        public override int TryPutInto(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
        {
            if(op.ShiftDown)
            {
                return 0;
            }
            return base.TryPutInto(sinkSlot, ref op);
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
            if(!(sourceSlot.Itemstack.Item is ItemCANStallBook))
            {
                return false;
            }
            return true;
        }
        public override bool CanTake()
        {
            return base.CanTake();
        }
        public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
        {
            return false;
            if (inventory?.PutLocked ?? false)
            {
                return false;
            }
            ItemStack itemStack = sourceSlot.Itemstack;
            if (itemStack == null)
            {
                return false;
            }

            if ((itemStack.Collectible.GetStorageFlags(itemStack) & StorageType) > (EnumItemStorageFlags)0 && (itemstack == null || itemstack.Collectible.GetMergableQuantity(itemstack, itemStack, priority) > 0))
            {
                return GetRemainingSlotSpace(itemStack) > 0;
            }

            return false;
        }
    }
}
