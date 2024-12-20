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
        public static bool IsReasonablyFresh(IWorldAccessor world, ItemStack itemstack)
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

            float[] value = (treeAttribute["freshHours"] as FloatArrayAttribute).value;
            float[] value2 = (treeAttribute["transitionedHours"] as FloatArrayAttribute).value;
            for (int i = 0; i < transitionableProperties.Length; i++)
            {
                TransitionableProperties obj = transitionableProperties[i];
                if (obj != null && obj.Type == EnumTransitionType.Perish && value2[i] > value[i] / canmarket.config.PERISH_DIVIDER)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
