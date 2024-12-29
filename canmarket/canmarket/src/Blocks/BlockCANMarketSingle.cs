using canmarket.src.BE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace canmarket.src.Blocks
{
    public class BlockCANMarketSingle: Block
    {
        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            var res = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            if(res)
            {
                if ((world.BlockAccessor.GetBlockEntity(blockSel.Position) is BECANMarketSingle blockEntity))
                    blockEntity.ownerName = byPlayer.PlayerName;
            }
            return res;
        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BECANMarketSingle be = null;
            if (blockSel.Position != null)
            {
                be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BECANMarketSingle;
            }

            if (byPlayer.WorldData.EntityControls.Sneak && blockSel.Position != null)
            {
                if (be != null)
                {
                    be.OnPlayerRightClick(byPlayer, blockSel);
                }

                return true;
            }

            if (!byPlayer.WorldData.EntityControls.Sneak && blockSel.Position != null)
            {
                if (be != null)
                {
                    be.OnPlayerRightClick(byPlayer, blockSel);
                }

                return true;
            }

            return false;
        }
        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, blockPos, byItemStack);
            if (canmarket.config.SAVE_SLOTS_ONCHESTTRADEBLOCK)
            {
                if (byItemStack != null)
                {
                    var entity = world.BlockAccessor.GetBlockEntity(blockPos);
                    if (entity != null)
                    {
                        int i = 0;
                        foreach (var slot_it in (entity as BECANMarketSingle).inventory)
                        {
                            ItemStack itemStack = byItemStack.Attributes.GetItemstack(i.ToString());
                            if (itemStack != null)
                            {
                                (entity as BECANMarketSingle).inventory[i].Itemstack = itemStack;
                            }
                            i++;
                        }
                    }

                }
            }
        }
        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            var drops = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
            if (canmarket.config.SAVE_SLOTS_ONCHESTTRADEBLOCK)
            {
                foreach (var it in drops)
                {
                    if (it.Block is BlockCANMarketSingle)
                    {
                        var entity = world.BlockAccessor.GetBlockEntity(pos);
                        if (entity != null)
                        {
                            int i = 0;
                            foreach (var slot_it in (entity as BECANMarketSingle).inventory)
                            {
                                if (!slot_it.Empty)
                                {
                                    it.Attributes.SetItemstack(i.ToString(), slot_it.Itemstack);
                                }
                                i++;
                            }
                        }
                    }
                }
            }
            return drops;
        }
    }
}
