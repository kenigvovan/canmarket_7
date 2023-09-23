using canmarket.src.BEB;
using canmarket.src.Inventories.slots;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace canmarket.src
{
    [HarmonyPatch]
    public class harmPatches
    {
        public static bool Prefix_UpdateAndGetTransitionStatesNative(Vintagestory.API.Common.CollectibleObject __instance, IWorldAccessor world, ItemSlot inslot, out TransitionState[] __result)
        {
            __result = null;
            if (inslot is CANNoPerishItemSlot)
            {
                ItemStack itemstack = inslot.Itemstack;
                TransitionableProperties[] propsm = __instance.GetTransitionableProperties(world, inslot.Itemstack, null);
                if (itemstack == null || propsm == null || propsm.Length == 0)
                {
                    return false;
                }

                if (itemstack.Attributes == null)
                {
                    itemstack.Attributes = new TreeAttribute();
                }
                if (!(itemstack.Attributes["transitionstate"] is ITreeAttribute))
                {
                    itemstack.Attributes["transitionstate"] = new TreeAttribute();
                }
                ITreeAttribute attr = (ITreeAttribute)itemstack.Attributes["transitionstate"];
                TransitionState[] states = new TransitionState[propsm.Length];
                float[] freshHours;
                float[] transitionHours;
                float[] transitionedHours;
                if (!attr.HasAttribute("createdTotalHours"))
                {
                    attr.SetDouble("createdTotalHours", world.Calendar.TotalHours);
                    attr.SetDouble("lastUpdatedTotalHours", world.Calendar.TotalHours);
                    freshHours = new float[propsm.Length];
                    transitionHours = new float[propsm.Length];
                    transitionedHours = new float[propsm.Length];
                    for (int i = 0; i < propsm.Length; i++)
                    {
                        transitionedHours[i] = 0f;
                        freshHours[i] = propsm[i].FreshHours.nextFloat(1f, world.Rand);
                        transitionHours[i] = propsm[i].TransitionHours.nextFloat(1f, world.Rand);
                    }
                    attr["freshHours"] = new FloatArrayAttribute(freshHours);
                    attr["transitionHours"] = new FloatArrayAttribute(transitionHours);
                    attr["transitionedHours"] = new FloatArrayAttribute(transitionedHours);
                }
                else
                {
                    freshHours = (attr["freshHours"] as FloatArrayAttribute).value;
                    transitionHours = (attr["transitionHours"] as FloatArrayAttribute).value;
                    transitionedHours = (attr["transitionedHours"] as FloatArrayAttribute).value;
                }

                for (int k = 0; k < propsm.Length; k++) {
                    TransitionableProperties prop = propsm[k];
                    states[k] = new TransitionState
                    {
                        FreshHoursLeft = freshHours[k],
                        TransitionLevel = 0f,
                        TransitionedHours = transitionedHours[k],
                        TransitionHours = transitionHours[k],
                        FreshHours = freshHours[k],
                        Props = prop
                    }; }
                __result =  (from s in states
                        where s != null
                        orderby (int)s.Props.Type
                        select s).ToArray<TransitionState>();
                return false;
            }
            return true;
        }
        public static bool TriggerTestBlockAccess_Patch(Vintagestory.Client.NoObf.ClientEventAPI __instance, IPlayer player, BlockSelection blockSel, EnumBlockAccessFlags accessType, string claimant, EnumWorldAccessResponse response, out EnumWorldAccessResponse __result)
        {
            if (accessType == EnumBlockAccessFlags.Use)
            {
                if (blockSel.Block != null && (blockSel.Block.Class.Equals("BlockCANMarket") || blockSel.Block.Class.Equals("BlockCANStall")) /*BlockCANStall*/)
                {
                    __result = EnumWorldAccessResponse.Granted;
                    return false;
                }
            }
            __result = EnumWorldAccessResponse.NoPrivilege;
            return true;
        }
        //it is not used, so why not
        public static void Postfix_InventoryBase_OnItemSlotModified(Vintagestory.API.Common.InventoryBase __instance, ItemSlot slot, ItemStack extractedStack = null)
        {
            if(__instance.Api.Side == EnumAppSide.Client || __instance.Pos == null)
            {
                return;
            }
            BlockEntity be = __instance.Api.World.BlockAccessor.GetBlockEntity(__instance.Pos);
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
