using canmarket.src.BEB;
using canmarket.src.Inventories.slots;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace canmarket.src
{
    [HarmonyPatch]
    public class harmPatches
    {
        public static bool Prefix_CreateModel(MicroBlockModelCache __instance, ItemStack forStack, ICoreClientAPI ___capi, ref MeshRef __result)
        {
            ITreeAttribute tree = forStack.Attributes;
            if (tree == null)
            {
                tree = new TreeAttribute();
            }
            int[] materials = BlockEntityMicroBlock.MaterialIdsFromAttributes(tree, ___capi.World);
            IntArrayAttribute intArrayAttribute = tree["cuboids"] as IntArrayAttribute;
            uint[] cuboids = (intArrayAttribute != null) ? intArrayAttribute.AsUint : null;
            if (cuboids == null)
            {
                LongArrayAttribute longArrayAttribute = tree["cuboids"] as LongArrayAttribute;
                cuboids = ((longArrayAttribute != null) ? longArrayAttribute.AsUint : null);
            }
            List<uint> voxelCuboids = (cuboids == null) ? new List<uint>() : new List<uint>(cuboids);
            if(materials.Length == 0)
            {
                return false;
            }
            Block firstblock = ___capi.World.Blocks[materials[0]];
            JsonObject attributes = firstblock.Attributes;
            bool flag = attributes != null && attributes.IsTrue("chiselShapeFromCollisionBox");
            uint[] originalCuboids = null;
            if (flag)
            {
                Cuboidf[] collboxes = firstblock.CollisionBoxes;
                originalCuboids = new uint[collboxes.Length];
                for (int i = 0; i < collboxes.Length; i++)
                {
                    Cuboidf box = collboxes[i];
                    uint uintbox = BlockEntityMicroBlock.ToUint((int)(16f * box.X1), (int)(16f * box.Y1), (int)(16f * box.Z1), (int)(16f * box.X2), (int)(16f * box.Y2), (int)(16f * box.Z2), 0);
                    originalCuboids[i] = uintbox;
                }
            }
            MeshData mesh = BlockEntityMicroBlock.CreateMesh(___capi, voxelCuboids, materials, null, originalCuboids);
            mesh.Rgba.Fill(byte.MaxValue);
            __result = ___capi.Render.UploadMesh(mesh);
            return false;
        }
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
