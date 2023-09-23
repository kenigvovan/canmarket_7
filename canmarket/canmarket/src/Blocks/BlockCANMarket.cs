using canmarket.src.BE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace canmarket.src.Blocks
{
    public class BlockCANMarket: Block
    {
        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            var res = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            if(res)
            {
                if ((world.BlockAccessor.GetBlockEntity(blockSel.Position) is BECANMarket blockEntity))
                    blockEntity.ownerName = byPlayer.PlayerName;
            }
            return res;
        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BECANMarket be = null;
            if (blockSel.Position != null)
            {
                be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BECANMarket;
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
        }
    }
}
