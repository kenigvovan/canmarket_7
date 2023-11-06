using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace canmarket.src.Items
{
    public class ItemCANStallBook: Item
    {
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            ITreeAttribute tree = inSlot.Itemstack.Attributes.GetTreeAttribute("warehouse");
            if(tree != null) 
            {
                var pos = tree.GetVec3i("pos");
                pos.X -= world.DefaultSpawnPosition.AsBlockPos.X;
                pos.Z -= world.DefaultSpawnPosition.AsBlockPos.Z;
                dsc.Append(Lang.Get("canmarket:warehousebook-info", pos));
                if (tree.HasAttribute("byPlayer"))
                {
                    dsc.Append(Lang.Get("canmarket:signed-by-player", tree.GetString("byPlayer")));
                }
            }
            
        }
    }
}
