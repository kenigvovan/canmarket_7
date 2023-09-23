using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace canmarket.src.Inventories
{
    public class CANTakeOutItemSlotOnChest: CANTakeOutItemSlotAbstract
    {
        public CANTakeOutItemSlotOnChest(InventoryBase inventory) : base(inventory)
        {
        }
        protected override void ActivateSlotLeftClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            if (op.ActingPlayer.PlayerName.Equals((inventory as InventoryCANMarketOnChest).be.ownerName))
            {
                HandleOwnerActiveSlotLeftClick(sourceSlot);
                return;
            }
            //check if price is set
            int slotId = inventory.GetSlotId(this);

            //we assume this slot has price slot before it
            if (inventory[slotId - 1].Itemstack == null)
            {
                return;
            }

            //check for good is set
            if(this.itemstack == null)
            {
                return;
            }

            //check if mouse inv is empty or have ^^ item
            if(op.ActingPlayer == null)
            {
                return;
            }
            else
            {
                var mouseInv = op.ActingPlayer.InventoryManager.GetOwnInventory("mouse");
                if (mouseInv[0].Itemstack != null && !itemstack.Collectible.Equals(mouseInv[0].Itemstack, itemstack, Config.Current.IGNORED_STACK_ATTRIBTES_ARRAY.Val))
                {
                    return;
                }
            }


            //check if player has money and collect slots
            List<ItemSlot> PLS = new List<ItemSlot>();
            if(!GetPlayerPaymentSlots(PLS, op.ActingPlayer, inventory[slotId - 1].Itemstack))
            {
                return;
            }

            bool infiniteStocks = (this.inventory as InventoryCANMarketOnChest).be.InfiniteStocks;

            //check for goods and collect slots
            List<ItemSlot> GLS = new List<ItemSlot>();
            if (!infiniteStocks) {
                if (!GetMarketGoodsSlots(GLS, op.ActingPlayer))
                {
                    return;
                }
            }
            //we have tmp slots for payments/goods

            TMPTradeInv inv = new TMPTradeInv("inv" + op.ActingPlayer.PlayerUID + this.inventory.InventoryID, this.Inventory.Api, 2);
            //take them out to tmp
            ItemSlot tmpPrice = inv[0];
            ItemSlot tmpGoods = inv[1];
           // var c = inv.GetSlotId(tmpPrice);

            if (!TakePrice(PLS, tmpPrice, inventory[slotId - 1].Itemstack))
            {
                if (tmpPrice.Itemstack != null && tmpPrice.StackSize > 0)
                {
                    op.ActingPlayer.InventoryManager.TryGiveItemstack(tmpPrice.Itemstack);
                }
                return;
            }

            if (!infiniteStocks)
            {
                //Take items from chest
                if (!TakeGoods(GLS, tmpGoods))
                {
                    //Try to return price back to player
                    if (tmpPrice.Itemstack != null && tmpPrice.StackSize > 0)
                    {
                        if (!op.ActingPlayer.InventoryManager.TryGiveItemstack(tmpPrice.Itemstack))
                        {
                            //we failed to return normaly to inventory, just push it out
                            this.inventory.Api.World.SpawnItemEntity(tmpPrice.Itemstack, op.ActingPlayer.Entity.Pos.XYZ.Clone().Add(0.5f, 0.25f, 0.5f));
                        }
                    }
                    if (tmpGoods.Itemstack != null && tmpGoods.StackSize > 0)
                    {
                        //change func name, it just tries to put stack into chest
                        //and for ffs do not copy it, just make "returnOnFailFunc
                        if (!PutPayment(tmpGoods))
                        {
                            this.inventory.Api.World.SpawnItemEntity(tmpGoods.Itemstack, op.ActingPlayer.Entity.Pos.XYZ.Clone().Add(0.5f, 0.25f, 0.5f));
                        }
                    }
                    return;
                }
            }

            //Now try to put price from player to chest
            if ((this.inventory as InventoryCANMarketOnChest).be.StorePayment)
            {
                if (!PutPayment(tmpPrice))
                {
                    if (tmpPrice.Itemstack != null && tmpPrice.StackSize > 0)
                    {
                        if (!op.ActingPlayer.InventoryManager.TryGiveItemstack(tmpPrice.Itemstack))
                        {
                            this.inventory.Api.World.SpawnItemEntity(tmpPrice.Itemstack, op.ActingPlayer.Entity.Pos.XYZ.Clone().Add(0.5f, 0.25f, 0.5f));
                        }
                    }
                    if (tmpGoods.Itemstack != null && tmpGoods.StackSize > 0)
                    {
                        if (!PutPayment(tmpGoods))
                        {
                            this.inventory.Api.World.SpawnItemEntity(tmpGoods.Itemstack, op.ActingPlayer.Entity.Pos.XYZ.Clone().Add(0.5f, 0.25f, 0.5f));
                        }
                    }
                    return;
                }
            }
            else
            {
                tmpPrice.Itemstack = null;
            }
            //just copy goods from out slot to give player after payment
            if (infiniteStocks)
            {
                tmpGoods.Itemstack = this.Itemstack.Clone();
            }
            PutGoods(op.ActingPlayer, tmpGoods);
            GLS.Clear();
            PLS.Clear();
            //we do not update if it is infinite
            if (!infiniteStocks)
            {
                for (int i = 1; i < 8; i++)
                {
                    if (!this.Empty && !this.inventory[i].Empty && this.Itemstack.Collectible.Equals(this.itemstack, this.inventory[i].Itemstack, Config.Current.IGNORED_STACK_ATTRIBTES_ARRAY.Val))
                    {
                        (this.inventory as InventoryCANMarketOnChest).stocks[i / 2] -= this.Itemstack.StackSize;
                    }

                }
            }
            this.MarkDirty();
        }
        protected override void ActivateSlotRightClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            if (!op.ActingPlayer.PlayerName.Equals((inventory as InventoryCANMarketOnChest).be.ownerName))
            {
                return;
            }
            //slot is already empty
            if (itemstack == null)
            {
                return;
            }
            else
            {
                if (itemstack.StackSize > 1)
                {
                    itemstack.StackSize /= 2;
                    (inventory as InventoryCANMarketOnChest).be.calculateAmountForSlot(this.inventory.GetSlotId(this));
                    this.MarkDirty();
                }
                else
                {
                    itemstack = null;
                    (inventory as InventoryCANMarketOnChest).stocks[inventory.GetSlotId(this) / 2] = 0;
                    this.MarkDirty();
                }
            }
        }
        protected void HandleOwnerActiveSlotLeftClick(ItemSlot sourceSlot)
        {
            if (sourceSlot.Itemstack == null)
            {
                if (itemstack == null)
                {
                    return;
                }
                   (inventory as InventoryCANMarketOnChest).stocks[inventory.GetSlotId(this) / 2] = 0;
                itemstack = null;
                this.MarkDirty();
                return;
            }

            if (itemstack != null)
            {
                //Slot already has the same item, just try to add stacksize from source or set maximum
                if (itemstack.Collectible.Equals(itemstack, sourceSlot.Itemstack, Config.Current.IGNORED_STACK_ATTRIBTES_ARRAY.Val))
                {
                    // itemstack.StackSize += sourceSlot.StackSize;
                    itemstack.StackSize = Math.Min(itemstack.StackSize + sourceSlot.StackSize, itemstack.Collectible.MaxStackSize);
                    (inventory as InventoryCANMarketOnChest).be.calculateAmountForSlot(this.inventory.GetSlotId(this));
                    sourceSlot.MarkDirty();
                    this.MarkDirty();
                    return;
                }
                else
                {
                    //Just ignore because this item is different
                    return;
                }
            }
            //Slot is empty, just fill it with source slot item
            else
            {
                itemstack = sourceSlot.Itemstack.Clone();
                //just to be calm
                itemstack.StackSize = Math.Min(itemstack.StackSize, itemstack.Collectible.MaxStackSize);
                (inventory as InventoryCANMarketOnChest).be.calculateAmountForSlot(this.inventory.GetSlotId(this));
                sourceSlot.MarkDirty();
                this.MarkDirty();
                return;
            }
        }
        //return true if goods is enough
        protected override bool GetMarketGoodsSlots(List<ItemSlot> GLS, IPlayer player)
        {
            var posChest = this.Inventory.Pos.DownCopy();
            var entity = this.inventory.Api.World.BlockAccessor.GetBlockEntity(posChest);
            
            if (entity is BlockEntityGenericTypedContainer)
            {
                int needToTrade = this.itemstack.StackSize;
                foreach (ItemSlot itemSlot in (entity as BlockEntityGenericTypedContainer).Inventory)
                {
                    ItemStack iS = itemSlot.Itemstack;
                    //No IS or is not an item
                    if (iS == null)
                    {
                        continue;
                    }
                    if (itemstack.Collectible.Equals(iS, itemstack, Config.Current.IGNORED_STACK_ATTRIBTES_ARRAY.Val) && IsReasonablyFresh(this.inventory.Api.World, iS))
                    {
                        GLS.Add(itemSlot);
                        needToTrade -= iS.StackSize; ;
                        if (needToTrade <= 0)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        protected override bool PutPayment(ItemSlot tmpPayment)
        {
            var entity = this.inventory.Api.World.BlockAccessor.GetBlockEntity((inventory as InventoryCANMarketOnChest).be.Pos.DownCopy(1));

            //We go through chest inventory and check if there is empty space for our payment stack
            //we make list of slots where we can place our stack in parts or full
            //and we remember first empty slot
            if (entity is BlockEntityGenericTypedContainer)
            {
                int needToPutNotChangedStack = tmpPayment.StackSize;
                ItemSlot firstEmptySlot = null;
                List<ItemSlot> whereToPlaceGoods = new List<ItemSlot>();
                foreach (ItemSlot itemSlot in (entity as BlockEntityGenericTypedContainer).Inventory)
                {
                    ItemStack iS = itemSlot.Itemstack;
                    //No IS or is not an item                  
                    if (iS == null)
                    {
                        if (firstEmptySlot == null)
                        {
                            firstEmptySlot = itemSlot;
                        }
                        continue;
                    }
                    if (itemstack.Collectible.Equals(iS, tmpPayment.Itemstack, Config.Current.IGNORED_STACK_ATTRIBTES_ARRAY.Val) && IsReasonablyFresh(this.inventory.Api.World, tmpPayment.Itemstack))
                    {
                        if (iS.Collectible.MaxStackSize > iS.StackSize)
                        {
                            needToPutNotChangedStack -= iS.Collectible.MaxStackSize - iS.StackSize;
                            whereToPlaceGoods.Add(itemSlot);
                            if (needToPutNotChangedStack <= 0)
                            {
                                break;
                            }
                        }
                    }
                }
                //if needToPutNotChangedStack <= 0 we can place our stack divided in parts
                //if needToPutNotChangedStack > 0 we were not able to place it in parts
                //but if firstempty slot is not null then we can place goods stack in it
                int needToPut = tmpPayment.StackSize;
                if (needToPutNotChangedStack <= 0 || (needToPutNotChangedStack > 0 && firstEmptySlot != null))
                {
                    foreach (ItemSlot itemSlot in whereToPlaceGoods)
                    {
                        ItemStack iS = itemSlot.Itemstack;
                        //No IS or is not an item
                        //idk, just to be sure
                        if (iS == null)
                        {
                            continue;
                            // tmpPayment.TryPutInto(this.inventory.Api.World, itemSlot, needToPut);
                            // return;
                        }
                        if (itemstack.Collectible.Equals(iS, tmpPayment.Itemstack, Config.Current.IGNORED_STACK_ATTRIBTES_ARRAY.Val) && IsReasonablyFresh(this.inventory.Api.World, tmpPayment.Itemstack))
                        {

                            needToPut -= tmpPayment.TryPutInto(this.inventory.Api.World, itemSlot, needToPut);
                            if (needToPut <= 0)
                            {
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    return false;
                }
                if (firstEmptySlot != null)
                {
                    var placed = tmpPayment.TryPutInto(this.inventory.Api.World, firstEmptySlot, needToPut);
                    if (needToPut > 0)
                    {
                        this.inventory.Api.World.SpawnItemEntity(tmpPayment.Itemstack, entity.Pos.ToVec3d().Clone().Add(0.5f, 0.25f, 0.5f));
                    }
                    return true;
                }
            }
            return false;
        }
        protected bool TakePrice(List<ItemSlot> PLC, ItemSlot tmpPrice, ItemStack paymentStack)
        {
            int needToPay = paymentStack.StackSize;
            foreach (var it in PLC)
            {
                if (it.Itemstack == null)
                {
                    continue;
                }
                if (itemstack.Collectible.Equals(it.Itemstack, paymentStack, Config.Current.IGNORED_STACK_ATTRIBTES_ARRAY.Val) && IsReasonablyFresh(this.inventory.Api.World, it.Itemstack))
                {
                    int willTake = Math.Min(it.Itemstack.StackSize, needToPay);
                    needToPay -= it.TryPutInto(this.inventory.Api.World, tmpPrice, willTake);
                    if (needToPay <= 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        protected bool TakeGoods(List<ItemSlot> GLS, ItemSlot tmpGoods)
        {
            int needGoods = this.StackSize;
            foreach (var it in GLS)
            {
                if (it.Itemstack == null)
                {
                    continue;
                }
                if (itemstack.Collectible.Equals(it.Itemstack, this.itemstack, Config.Current.IGNORED_STACK_ATTRIBTES_ARRAY.Val) && IsReasonablyFresh(this.inventory.Api.World, it.Itemstack))
                {
                    needGoods -= it.TryPutInto(this.inventory.Api.World, tmpGoods, Math.Min(it.Itemstack.StackSize, needGoods));
                    if (needGoods <= 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        //return true if we found enough money
        protected bool GetPlayerPaymentSlots(List<ItemSlot> PLS, IPlayer player, ItemStack paymentStack)
        {
            InventoryPlayerBackPacks playerBackpacks = ((InventoryPlayerBackPacks)player.InventoryManager.GetOwnInventory("backpack"));

            int needToPay = paymentStack.StackSize;
            foreach (ItemSlot itemSlot in playerBackpacks)
            {
                ItemStack iS = itemSlot.Itemstack;
                if (iS == null)
                {
                    continue;
                }
                if (itemstack.Collectible.Equals(iS, paymentStack, Config.Current.IGNORED_STACK_ATTRIBTES_ARRAY.Val) && IsReasonablyFresh(player.Entity.World, iS))
                {
                    PLS.Add(itemSlot);
                    needToPay -= iS.StackSize; ;
                    if (needToPay <= 0)
                    {
                        return true;
                    }
                }
            }
            IInventory playerHotbar = player.InventoryManager.GetHotbarInventory();
            foreach (ItemSlot itemSlot in playerHotbar)
            {
                ItemStack iS = itemSlot.Itemstack;
                if (iS == null)
                {
                    continue;
                }
                if (itemstack.Collectible.Equals(iS, paymentStack, Config.Current.IGNORED_STACK_ATTRIBTES_ARRAY.Val) && IsReasonablyFresh(player.Entity.World, iS))
                {
                    PLS.Add(itemSlot);
                    needToPay -= iS.StackSize; ;
                    if (needToPay <= 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public override void MarkDirty()
        {
            if (this.inventory != null)
            {
                this.inventory.DidModifyItemSlot(this, null);
               /* ItemStack itemStack = this.itemstack;
                if (((itemStack != null) ? itemStack.Collectible : null) != null)
                {
                    this.itemstack.Collectible.UpdateAndGetTransitionStates(this.inventory.Api.World, this);
                }*/
            }
        }

    }
}
