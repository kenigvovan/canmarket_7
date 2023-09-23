using canmarket.src.BE;
using canmarket.src.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace canmarket.src.Inventories
{
    public class CANTakeOutItemSlotStall : CANTakeOutItemSlotAbstract
    {
        public CANTakeOutItemSlotStall(InventoryBase inventory) : base(inventory)
        {
        }
        public override bool CanHold(ItemSlot sourceSlot)
        {
            int slotId = this.inventory.GetSlotId(this);
            if (slotId < 2 && slotId >= 0)
            {
                //list of chests
                if(slotId == 0)
                {
                    if(sourceSlot.Itemstack != null && sourceSlot.Itemstack.Item is ItemCANStallBook)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public override bool CanTake()
        {
            int slotId = this.inventory.GetSlotId(this);
            if (slotId < 2 && slotId >= 0)
            {
                return true;
            }
            return false;
        }
        protected override bool PutPayment(ItemSlot tmpPayment)
        {
            //iterate through all connected containers and put into them
            //what if we iterate and put in different and on the last one we don't have space to place it there?
            //we probably do it in the same thread and nobody else should change this containers
            //we will just throw it on the ground
            //but it shouldn't occure because we check for space before we try to place it there
            BECANStall be = (this.inventory as InventoryCANStall).be;
            foreach (var chestPos in be.chestsCoords)
            {
                BlockEntityGenericTypedContainer entity = (BlockEntityGenericTypedContainer)this.inventory.Api.World.BlockAccessor.GetBlockEntity(new BlockPos(chestPos));
                //We go through chest inventory and check if there is empty space for our payment stack
                //we make list of slots where we can place our stack in parts or full
                //and we remember first empty slot
                if (entity != null)
                {
                    int needToPutNotChangedStack = tmpPayment.StackSize;
                    ItemSlot firstEmptySlot = null;
                    List<ItemSlot> whereToPlaceGoods = new List<ItemSlot>();
                    foreach (ItemSlot itemSlot in entity.Inventory)
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
            }
            return false;
        }
        protected bool GetMarketGoodsSlots(List<ItemSlot> GLS, IPlayer player, List<Vec3i> containerLocations)
        {
            //we iterate through all chest and try to collect slots with goods
            BECANStall be = (this.inventory as InventoryCANStall).be;
            foreach (var chestPos in containerLocations)
            {
                var entity = this.inventory.Api.World.BlockAccessor.GetBlockEntity(new BlockPos(chestPos));

                if (entity is BlockEntityContainer)
                {
                    int needToTrade = this.itemstack.StackSize;
                    foreach (ItemSlot itemSlot in (entity as BlockEntityContainer).Inventory)
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
                /*else if(entity is BlockEntityCrate)
                {
                    int needToTrade = this.itemstack.StackSize;
                    foreach (ItemSlot itemSlot in (entity as BlockEntityContainer).Inventory)
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
                }*/
            }
            return false;
        }
        protected override void ActivateSlotRightClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            if ((inventory as InventoryCANStall).be.adminShop || !op.ActingPlayer.PlayerUID.Equals((inventory as InventoryCANStall).be.ownerUID))
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
                    //(inventory as InventoryCANStall).be.calculateAmountForSlot(this.inventory.GetSlotId(this));
                    this.MarkDirty();
                }
                else
                {
                    itemstack = null;
                    //(inventory as InventoryCANStall).stocks[inventory.GetSlotId(this) / 2] = 0;
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
                //(inventory as InventoryCANStall).stocks[inventory.GetSlotId(this) / 2] = 0;
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
                    //(inventory as InventoryCANStall).be.calculateAmountForSlot(this.inventory.GetSlotId(this));
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
                //(inventory as InventoryCANStall).be.calculateAmountForSlot(this.inventory.GetSlotId(this));
                sourceSlot.MarkDirty();
                this.MarkDirty();
                return;
            }
        }
        protected bool GetPlayerPaymentSlots(List<ItemSlot> PLS, IPlayer player, ItemStack [] priceStacks)
        {
            InventoryPlayerBackPacks playerBackpacks = ((InventoryPlayerBackPacks)player.InventoryManager.GetOwnInventory("backpack"));
            if (priceStacks.Length == 1)
            {
                int needToPay1 = priceStacks[0].StackSize;
                //search for 1 item

                foreach (ItemSlot itemSlot in playerBackpacks)
                {
                    ItemStack iS = itemSlot.Itemstack;
                    if (iS == null)
                    {
                        continue;
                    }
                    if (itemstack.Collectible.Equals(iS, priceStacks[0], Config.Current.IGNORED_STACK_ATTRIBTES_ARRAY.Val) && IsReasonablyFresh(player.Entity.World, iS))
                    {
                        PLS.Add(itemSlot);
                        needToPay1 -= iS.StackSize; ;
                        if (needToPay1 <= 0)
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
                    if (itemstack.Collectible.Equals(iS, priceStacks[0], Config.Current.IGNORED_STACK_ATTRIBTES_ARRAY.Val) && IsReasonablyFresh(player.Entity.World, iS))
                    {
                        PLS.Add(itemSlot);
                        needToPay1 -= iS.StackSize; ;
                        if (needToPay1 <= 0)
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                int needToPay1 = priceStacks[0].StackSize;
                int needToPay2 = priceStacks[1].StackSize;

                //Search for 2 different
                foreach (ItemSlot itemSlot in playerBackpacks)
                {
                    ItemStack iS = itemSlot.Itemstack;
                    if (iS == null)
                    {
                        continue;
                    }
                    if (itemstack.Collectible.Equals(iS, priceStacks[0], Config.Current.IGNORED_STACK_ATTRIBTES_ARRAY.Val) && IsReasonablyFresh(player.Entity.World, iS))
                    {
                        PLS.Add(itemSlot);
                        needToPay1 -= iS.StackSize; ;
                        if (needToPay1 <= 0 && needToPay2 <= 0)
                        {
                            return true;
                        }
                    }
                    else if (itemstack.Collectible.Equals(iS, priceStacks[1], Config.Current.IGNORED_STACK_ATTRIBTES_ARRAY.Val) && IsReasonablyFresh(player.Entity.World, iS))
                    {
                        PLS.Add(itemSlot);
                        needToPay2 -= iS.StackSize; ;
                        if (needToPay2 <= 0 && needToPay1 <=0)
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
                    if (itemstack.Collectible.Equals(iS, priceStacks[0], Config.Current.IGNORED_STACK_ATTRIBTES_ARRAY.Val) && IsReasonablyFresh(player.Entity.World, iS))
                    {
                        PLS.Add(itemSlot);
                        needToPay1 -= iS.StackSize; ;
                        if (needToPay1 <= 0 && needToPay2 <= 0)
                        {
                            return true;
                        }
                    }
                    else if (itemstack.Collectible.Equals(iS, priceStacks[1], Config.Current.IGNORED_STACK_ATTRIBTES_ARRAY.Val) && IsReasonablyFresh(player.Entity.World, iS))
                    {
                        PLS.Add(itemSlot);
                        needToPay2 -= iS.StackSize; ;
                        if (needToPay1 <= 0 && needToPay2 <= 0)
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }
        private BECANWareHouse GetWareHouse()
        {
            ItemStack book = this.inventory[0].Itemstack;
            if (book != null && book.Item is ItemCANStallBook)
            {
                ITreeAttribute tree = book.Attributes.GetTreeAttribute("warehouse");
                if (tree == null)
                {
                    return null;
                }
                if ((this.inventory as InventoryCANStall).existWarehouse(tree.GetInt("posX"), tree.GetInt("posY"), tree.GetInt("posZ"), tree.GetInt("num"), this.inventory.Api.World))
                {
                    BECANWareHouse warehouse = (BECANWareHouse)this.inventory.Api.World.BlockAccessor.GetBlockEntity(new BlockPos(tree.GetInt("posX"), tree.GetInt("posY"), tree.GetInt("posZ")));
                    if (warehouse != null)
                    {
                        return warehouse;
                    }
                }                
            }
            return null;
        }       
        private ItemStack[] NormalizedPrice(ItemStack stack1, ItemStack stack2)
        {
            if (stack1 != null && stack2 != null)
            {
                if (stack1.Collectible.Equals(stack1, stack2, Config.Current.IGNORED_STACK_ATTRIBTES_ARRAY.Val))
                {
                    ItemStack tmpStack = stack1.Clone();
                    tmpStack.StackSize += stack2.StackSize;
                    return new ItemStack[] { tmpStack };
                }
                else
                {
                    return new ItemStack[] { stack1.Clone(), stack2.Clone() };
                }
            }
            else if(stack1 != null) 
            {
                return new ItemStack[] { stack1 };
            }
            else if(stack2 != null)
            {
                return new ItemStack[] { stack2 };
            }
            return null;
        }
        protected bool TakePrice(List<ItemSlot> PLC, TMPTradeInv tmpInv, ItemStack [] priceStacks)
        {
            
            if (priceStacks.Length == 1)
            {
                int needToPay = priceStacks[0].StackSize;
                foreach (var it in PLC)
                {
                    if (it.Itemstack == null)
                    {
                        continue;
                    }
                    if (itemstack.Collectible.Equals(it.Itemstack, priceStacks[0], Config.Current.IGNORED_STACK_ATTRIBTES_ARRAY.Val) && IsReasonablyFresh(this.inventory.Api.World, it.Itemstack))
                    {
                        int willTake = Math.Min(it.Itemstack.StackSize, needToPay);
                        needToPay -= it.TryPutInto(this.inventory.Api.World, tmpInv[0], willTake);
                        if (needToPay <= 0)
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                int needToPay1 = priceStacks[0].StackSize;
                int needToPay2 = priceStacks[1].StackSize;
                foreach (var it in PLC)
                {
                    if (it.Itemstack == null)
                    {
                        continue;
                    }
                    if (itemstack.Collectible.Equals(it.Itemstack, priceStacks[0], Config.Current.IGNORED_STACK_ATTRIBTES_ARRAY.Val) && IsReasonablyFresh(this.inventory.Api.World, it.Itemstack))
                    {
                        int willTake = Math.Min(it.Itemstack.StackSize, needToPay1);
                        needToPay1 -= it.TryPutInto(this.inventory.Api.World, tmpInv[0], willTake);
                        if (needToPay1 <= 0 && needToPay2 <= 0)
                        {
                            return true;
                        }
                    }
                    else if (itemstack.Collectible.Equals(it.Itemstack, priceStacks[1], Config.Current.IGNORED_STACK_ATTRIBTES_ARRAY.Val) && IsReasonablyFresh(this.inventory.Api.World, it.Itemstack))
                    {
                        int willTake = Math.Min(it.Itemstack.StackSize, needToPay2);
                        needToPay2 -= it.TryPutInto(this.inventory.Api.World, tmpInv[1], willTake);
                        if (needToPay2 <= 0 && needToPay1 <= 0)
                        {
                            return true;
                        }
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
        protected override void ActivateSlotLeftClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            BECANStall be = (this.inventory as InventoryCANStall).be;
            if (!be.adminShop && op.ActingPlayer.PlayerUID.Equals(be.ownerUID))
            {
                HandleOwnerActiveSlotLeftClick(sourceSlot);
                return;
            }

            int slotId = inventory.GetSlotId(this);

            //Goods are not set for the trade
            if(this.itemstack == null)
            {
                return;
            }

            //We assume this slot has price slot before goods slot
            ItemStack [] priceStacks = NormalizedPrice(inventory[slotId - 2].Itemstack, inventory[slotId - 1].Itemstack);
            if(priceStacks == null)
            {
                return;
            }

            //Check if mouse inv is empty or have ^^ item
            if (op.ActingPlayer == null)
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

            BECANWareHouse wareHouse = GetWareHouse();

            //Warehouse exists and BE was found
            if(wareHouse == null) 
            {
                return;
            }

            //Warehouse containers contain this collectable in needed quantity
            if(!wareHouse.ContainersContainCollectableWithQuantity(this.itemstack))
            {
                return;
            }
                      
            List<ItemSlot> PLS = new List<ItemSlot>();

            //Check if player has money and collect slots
            if (!GetPlayerPaymentSlots(PLS, op.ActingPlayer, priceStacks))
            {
                return;
            }
            
            bool infiniteStocks = (this.inventory as InventoryCANStall).be.InfiniteStocks;

            //check for goods and collect slots
            List<ItemSlot> GLS = new List<ItemSlot>();

            //iterate through all chests instead of under one
            if (!infiniteStocks)
            {
                if (!GetMarketGoodsSlots(GLS, op.ActingPlayer, wareHouse.containerLocations))
                {
                    return;
                }
            }
            //we have tmp slots for payments/goods

            TMPTradeInv inv = new TMPTradeInv("inv" + op.ActingPlayer.PlayerUID + this.inventory.InventoryID, this.Inventory.Api, 3);
            //take them out to tmp
            ItemSlot tmpPrice1 = inv[0];
            ItemSlot tmpPrice2 = inv[1];
            ItemSlot tmpGoods = inv[2];
            
            //We checked that we have enough collectables for price
            TakePrice(PLS, inv, priceStacks);


            if (!infiniteStocks)
            {
                //Warehouse should take care about iterating through all containers and take collectables from them
                //!!!
                TakeGoods(GLS, tmpGoods);
            }

            //Now try to put price from player to chest
            if (be.StorePayment)
            {
                wareHouse.PlaceTakenPriceInContainers(inv);
            }
            else
            {
                tmpPrice1.Itemstack = null;
                tmpPrice2.Itemstack = null;
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
                for (int i = 4, j=0; i < inventory.Count; i+=3, j++)
                {
                    if (!this.inventory[i].Empty && this.Itemstack.Collectible.Equals(this.itemstack, this.inventory[i].Itemstack, Config.Current.IGNORED_STACK_ATTRIBTES_ARRAY.Val))
                    {
                        (this.inventory as InventoryCANStall).be.stocks[j] -= this.Itemstack.StackSize;
                    }                    
                }
                if (wareHouse.quantities.TryGetValue(this.itemstack.Collectible.Code.Domain + this.itemstack.Collectible.Code.Path, out int qua))
                {
                    wareHouse.quantities[this.itemstack.Collectible.Code.Domain + this.itemstack.Collectible.Code.Path] = qua - this.itemstack.StackSize;
                }
                //also for price as well

            }
            (this.inventory as InventoryCANStall).be.MarkDirty(true);
            //this.MarkDirty();
        }
    }
}
