using canmarket.src.BEB;
using canmarket.src.GUI;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace canmarket.src
{
    [HarmonyPatch]
    public class harmPatches
    {
        /// <summary>Prevents game from dropping mouse cursor items while ImGui inventory grid is active.</summary>
        public static bool Prefix_DropMouseSlotItems()
        {
            return !ImGuiInventoryGrid.SuppressMouseDrop;
        }             
        //it is not used, so why not
        public static void Postfix_InventoryBase_OnItemSlotModified(Vintagestory.API.Common.InventoryBase __instance,
                                                                                            ItemSlot slot,
                                                                                            ItemStack extractedStack = null)
        {
            BlockPos bp = null;
            if (__instance.Api.Side == EnumAppSide.Client)
            {
                return;
            }
            else
            {
                if(__instance.Pos == null)
                {
                    string[] nameSplit = __instance.InventoryID.Split('-');
                    if(nameSplit.Length < 2 || nameSplit[0] != "chest") 
                    {
                        return;
                    }
                    string[] coords;
                    if (!nameSplit[1].Contains("/"))
                    {
                        coords = nameSplit[1].Split(",");
                    }
                    else
                    {
                        coords = nameSplit[1].Split("/");
                    }
                    
                    bp = new BlockPos(int.Parse(coords[0]), int.Parse(coords[1]), int.Parse(coords[2]), 0);
                }
                else
                {
                    bp = __instance.Pos;
                }
            }
            BlockEntity be = __instance.Api.World.BlockAccessor.GetBlockEntity(bp);
            if(be is BlockEntityGenericTypedContainer)
            {
                var beb = be.GetBehavior<BEBehaviorTrackLastUpdatedContainer>();
                if(beb == null)
                {
                    return;
                }
                beb.markToUpdaete = 1;
            }
        }
    }
}
