using canmarket.src.Inventories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace canmarket.src.Utils
{
    public class UsefullUtils
    {
        public static bool IsReasonablyFresh(IWorldAccessor world, ItemStack itemstack, InventoryBase inventory = null)
        {
            if (itemstack.Collectible.GetMaxDurability(itemstack) > 1 && (float)itemstack.Collectible.GetRemainingDurability(itemstack) / itemstack.Collectible.GetMaxDurability(itemstack) < canmarket.config.MIN_DURABILITY_RATION)
            {
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

            float freshnessThreshold = canmarket.config.DEFAULT_MIN_FRESHNESS_FOR_SALE_PERCENTS;
            if (inventory != null)
            {
                if(inventory is InventoryCANStallWithMaxStocks)
                {
                    freshnessThreshold = (inventory as InventoryCANStallWithMaxStocks).be.CurrentFreshnessThreshold;
                }
            }

            float[] value = (treeAttribute["freshHours"] as FloatArrayAttribute).value;
            float[] value2 = (treeAttribute["transitionedHours"] as FloatArrayAttribute).value;
            for (int i = 0; i < transitionableProperties.Length; i++)
            {
                TransitionableProperties obj = transitionableProperties[i];
                if (obj != null && obj.Type == EnumTransitionType.Perish && ((freshnessThreshold * value[i] + value2[i] - value[i]) > 0))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
