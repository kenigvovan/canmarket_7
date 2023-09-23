using canmarket.src.BE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace canmarket.src.helpers
{
    public class WarehouseManager
    {
        private static readonly int _searchContainerRadius = 3;
        private static readonly int _searchWarehouseDistance = 10;

        private List<Vec3i> _containerLocations;
        private Dictionary<string, int> _quantities;
        public void searchContainerLocations(WarehouseBookInfo info, IWorldAccessor world)
        {
            _containerLocations.Clear();
            _quantities.Clear();

            int startX = info.pos.X - _searchContainerRadius;
            int endX = info.pos.X + _searchContainerRadius;
            int startY = info.pos.Y - _searchContainerRadius;
            int endY = info.pos.Y + _searchContainerRadius;
            int startZ = info.pos.Z - _searchContainerRadius;
            int endZ = info.pos.Z + _searchContainerRadius;

            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    for (int z = startZ; z <= endZ; z++)
                    {
                        BlockEntityGenericTypedContainer be = (BlockEntityGenericTypedContainer)world.BlockAccessor.GetBlockEntity(new BlockPos(x,y,z));

                        if (be != null)
                        {
                            _containerLocations.Add(new Vec3i(x, y, z));

                            calculateQuantities(be.Inventory);
                        }
                    }
                }
            }
        }
        private void calculateQuantities(InventoryBase inventory)
        {
            foreach(var sl in inventory)
            {
                if (sl.Itemstack == null)
                    continue;

                addItemStackQuantity(sl.Itemstack);
            }
        }

        private void addItemStackQuantity(ItemStack itemStack)
        {
            string itemKey = itemStack.Collectible.Code.Domain + ":" + itemStack.Collectible.Code.Path;

            if (_quantities.ContainsKey(itemKey))
            {
                _quantities.TryGetValue(itemKey, out int quantity);
                quantity += itemStack.StackSize;
                _quantities[itemKey] = quantity;
            }
        }
        public bool existWarehouse(int stallX, int stallY, int stallZ, WarehouseBookInfo info, IWorldAccessor world)
        {
            double distance = Math.Sqrt(Math.Pow(info.pos.X - stallX, 2) + Math.Pow(info.pos.Y - stallY, 2) + Math.Pow(info.pos.Z - stallZ, 2));

            if (distance > _searchWarehouseDistance)
                return false;

            BEWareHouse wareHouse = (BEWareHouse)world.BlockAccessor.GetBlockEntity(new BlockPos(info.pos.X, info.pos.Y, info.pos.Z));

            if(wareHouse == null)
            {
                return false;
            }
            return wareHouse.getKey() == info.key;
        }
    }
}
