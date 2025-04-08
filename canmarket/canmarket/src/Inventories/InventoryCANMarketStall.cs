using canmarket.src.BE;
using canmarket.src.Inventories.slots.Stall;
using canmarket.src.Inventories.slots;
using canmarket.src.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;
using canmarket.src.BE.SupportClasses;

namespace canmarket.src.Inventories
{
    public class InventoryCANMarketStall: InventoryCANStallWithMaxStocks
    {


        // 2 slots should be warehouse book and book for log       
        public InventoryCANMarketStall(string inventoryID, ICoreAPI api, BEStall be, int slotsAmount = 14)
          : base(inventoryID, api, be, slotsAmount)
        {

        }
       
       
        public override void OnItemSlotModified(ItemSlot slot)
        {
            //check if it is not too far away from
            //on list added to 0 slot
            if (this.GetSlotId(slot) == 0)
            {
                ItemStack book = slot.Itemstack;
                if (book != null && book.Item is ItemCANStallBook)
                {
                    ITreeAttribute tree = book.Attributes.GetTreeAttribute("warehouse");
                    if (tree == null)
                    {
                        return;
                    }
                    if (existWarehouse(tree.GetInt("posX"), tree.GetInt("posY"), tree.GetInt("posZ"), tree.GetInt("num"), this.Api.World))
                    {
                        this.be.MarkDirty(true);
                    }

                }
            }
            base.OnItemSlotModified(slot);
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
        public virtual void LateInitialize(string inventoryID, ICoreAPI api, BECANMarketStall be)
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
        }
        public override void DropAll(Vec3d pos, int maxStackSize = 0)
        {
            //drop only 0, 1 slots with book, other slots are clones
            int i = 0;
            foreach (var it in this.slots)
            {
                if (i > 1)
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
        public override float GetTransitionSpeedMul(EnumTransitionType transType, ItemStack stack)
        {
            return 0f;
        }
    }
}
