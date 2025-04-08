using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace canmarket.src.BE
{
    public interface BEWarehouseUser
    {
        public bool existWarehouse(BlockPos pos, int warehouseX, int warehouseY, int warehouseZ, int key, IWorldAccessor world, out BECANWareHouse wareHouse)
        {
            double distance = Math.Sqrt(Math.Pow(pos.X - warehouseX, 2) + Math.Pow(pos.Y - warehouseY, 2) + Math.Pow(pos.Z - warehouseZ, 2));

            if (distance > canmarket.config.SEARCH_WAREHOUE_DISTANCE)
            {
                wareHouse = null;
                return false;
            }

            wareHouse = (BECANWareHouse)world.BlockAccessor.GetBlockEntity(new BlockPos(warehouseX, warehouseY, warehouseZ));

            if (wareHouse == null)
            {
                return false;
            }
            return wareHouse.GetKey() == key;
        }
    }
}
