using canmarket.src.Inventories.slots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace canmarket.src.Inventories
{
    public class CANTakeOutItemSlotAbstract: CANNoPerishItemSlot
    {
        public CANTakeOutItemSlotAbstract(InventoryBase inventory) : base(inventory)
        {
        }
        public override bool CanHold(ItemSlot sourceSlot)
        {
            return false;
        }
        public override bool CanTake()
        {
            return false;
        }
        //from tmp to player
        protected void PutGoods(IPlayer player, ItemSlot tmpGoods)
        {
            var mouseInv = player.InventoryManager.GetOwnInventory("mouse");
            if (mouseInv[0].Itemstack != null)
            {
                //2 different items
                if (!itemstack.Collectible.Equals(mouseInv[0].Itemstack, itemstack, Config.Current.IGNORED_STACK_ATTRIBTES_ARRAY.Val) && IsReasonablyFresh(player.Entity.World, tmpGoods.Itemstack))
                {
                    if (!player.InventoryManager.TryGiveItemstack(tmpGoods.Itemstack))
                    {
                        this.inventory.Api.World.SpawnItemEntity(tmpGoods.Itemstack, player.Entity.Pos.XYZ.Clone().Add(0.5f, 0.25f, 0.5f));
                    }
                    return;
                }
                else
                {
                    if (mouseInv[0].StackSize + tmpGoods.StackSize > mouseInv[0].Itemstack.Collectible.MaxStackSize)
                    {
                        if (!player.InventoryManager.TryGiveItemstack(tmpGoods.Itemstack))
                        {
                            this.inventory.Api.World.SpawnItemEntity(tmpGoods.Itemstack, player.Entity.Pos.XYZ.Clone().Add(0.5f, 0.25f, 0.5f));
                        }
                        return;
                    }
                }
            }
            tmpGoods.TryPutInto(player.Entity.Api.World, mouseInv[0], tmpGoods.Itemstack.StackSize);
            if (tmpGoods?.StackSize > 0)
            {
                this.inventory.Api.World.SpawnItemEntity(tmpGoods.Itemstack, player.Entity.Pos.XYZ.Clone().Add(0.5f, 0.25f, 0.5f));
            }
        }

        //todo
        protected virtual bool PutPayment(ItemSlot tmpPayment)
        {
            return false;
        }
        protected virtual bool GetMarketGoodsSlots(List<ItemSlot> GLS, IPlayer player)
        {
            return false;
        }
           
        public virtual bool IsReasonablyFresh(IWorldAccessor world, ItemStack itemstack)
        {
            if (itemstack.Collectible.GetMaxDurability(itemstack) > 1 && (float)itemstack.Collectible.GetRemainingDurability(itemstack) / itemstack.Collectible.GetMaxDurability(itemstack) < Config.Current.MIN_DURABILITY_RATION.Val)
            {
                var c = (float)itemstack.Collectible.GetRemainingDurability(itemstack) / itemstack.Collectible.GetMaxDurability(itemstack);
                return false;
            }

            if (itemstack == null)
            {
                return true;
            }

            TransitionableProperties[] transitionableProperties = itemstack.Collectible.GetTransitionableProperties(world, itemstack, null);
            if (transitionableProperties == null)
            {
                return true;
            }

            ITreeAttribute treeAttribute = (ITreeAttribute)itemstack.Attributes["transitionstate"];
            if (treeAttribute == null)
            {
                return true;
            }

            float[] value = (treeAttribute["freshHours"] as FloatArrayAttribute).value;
            float[] value2 = (treeAttribute["transitionedHours"] as FloatArrayAttribute).value;
            for (int i = 0; i < transitionableProperties.Length; i++)
            {
                TransitionableProperties obj = transitionableProperties[i];
                if (obj != null && obj.Type == EnumTransitionType.Perish && value2[i] > value[i] / Config.Current.PERISH_DIVIDER.Val)
                {
                    return false;
                }
            }

            return true;
        }

        protected override void ActivateSlotMiddleClick(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
        {
            return;
        }
        protected override void ActivateSlotRightClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            return;
        }

    }
}
