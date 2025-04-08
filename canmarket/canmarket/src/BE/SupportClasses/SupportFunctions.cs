using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace canmarket.src.BE.SupportClasses
{
    public static class SupportFunctions
    {
        public static void getOrCreateMesh(ref MeshData mesh, ItemStack stack, int index, ICoreClientAPI capi, BlockFacing facing)
        {
            if (stack.Class == EnumItemClass.Item && (stack.Item.Shape == null || stack.Item.Shape.VoxelizeTexture))
            {
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 1.5707964f, 0f, 0f);
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.33f, 0.33f, 0.33f);
                mesh.Translate(0f, -0.46875f, 0f);
            }

            if (stack.Collectible is ItemPlantableSeed)
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.35f, 0.35f, 0.35f);
                mesh.Translate(0f, -4f / 16, 0f);
            }
            else if (stack.Collectible.Code.Path.Contains("axehead-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.55f, 0.55f, 0.55f);
                mesh.Translate(0.0f, -0.09f, 0.03f);
            }
            else if (stack.Collectible.Code.Path.Contains("axe-felling"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.6f, 0.6f, 0.6f);
                mesh.Translate(-0.01f, -0.09f, 0.03f);
            }
            else if (stack.Collectible.Code.Path.Contains("knife-"))
            {
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), (float)Math.PI / 2, 0.0f, 0.0f);
                mesh.Translate(-0.0f, -0.32f, 0.25f);
            }
            else if (stack.Collectible.Code.Path.Contains("knifeblade-"))
            {
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), (float)Math.PI / 2, 0.0f, 0.0f);
                mesh.Translate(-0.0f, -0.32f, 0.25f);
            }
            else if (stack.Collectible.Code.Path.Contains("cleaver"))
            {
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 1.77f, 0f, 0f);
                mesh.Translate(0f, -0.20f, 0f);
            }
            else if (stack.Collectible.Code.Path.Contains("scythe-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.3f, 0.3f, 0.3f);
                mesh.Translate(-0.0f, -0.17f, 0.05f);
            }
            else if (stack.Collectible.Code.Path.Contains("scythehead-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.55f, 0.55f, 0.55f);
                mesh.Translate(-0.4f, -0.09f, 0.23f);
            }
            else if (stack.Collectible.Code.Path.Contains("hoe-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.5f, 0.5f, 0.5f);
                mesh.Translate(0.05f, -0.13f, 0.03f);
            }
            else if (stack.Collectible.Code.Path.Contains("hammer-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.7f, 0.7f, 0.7f);
                mesh.Translate(0.05f, -0.45f, 0.03f);
            }
            else if (stack.Collectible.Code.Path.Contains("hoehead-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.5f, 0.5f, 0.5f);
                mesh.Translate(0.05f, -0.13f, 0.03f);
            }
            else if (stack.Collectible.Code.Path.Contains("saw-") || stack.Collectible.Code.Path.Contains("sawblade-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.65f, 0.65f, 0.65f);
                mesh.Translate(0.05f, -0.4f, 0.07f);
            }
            else if (stack.Collectible.Code.Path.Contains("shovel-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.35f, 0.35f, 0.35f);
                mesh.Translate(0.05f, -0.13f, 0.03f);
            }
            else if (stack.Collectible.Code.Path.Contains("shovelhead-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.55f, 0.55f, 0.55f);
                mesh.Translate(0.05f, -0.13f, 0.03f);
            }
            else if (stack.Collectible.Code.Path.Contains("bladehead-") || stack.Collectible.Code.Path.Contains("blade-falx"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.55f, 0.55f, 0.55f);
                mesh.Translate(0.0f, -0.09f, 0.03f);
            }
            else if (stack.Collectible.Code.Path.Contains("sword-short"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.5f, 0.5f, 0.5f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                //mesh.Translate(-0.14f, -0.09f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("quarterstaff"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.35f, 0.35f, 0.35f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, 0.75f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("mace-plain"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.45f, 0.45f, 0.45f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, 0f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("halberd-plain"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.35f, 0.35f, 0.35f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, 0.7f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("poleaxe-plain"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.35f, 0.35f, 0.35f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, 0.7f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("club-plain"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.35f, 0.35f, 0.35f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, -0.1f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("javelin-plain"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.35f, 0.35f, 0.35f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, 0.3f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("pike-plain"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.35f, 0.35f, 0.35f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, 0.3f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("sword-long"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.35f, 0.35f, 0.35f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, -0.1f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("axe-long"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.45f, 0.45f, 0.45f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, 0.2f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("sword-great"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.45f, 0.45f, 0.45f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, 0.2f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("shield-heavy-plain"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.45f, 0.45f, 0.45f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), (float)Math.PI / 2, 0.0f, 0);
                mesh.Translate(-0.05f, 0.1f, 0.15f);
            }
            else if (stack.Collectible.Code.Path.Contains("shield-light-plain"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.45f, 0.45f, 0.45f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), (float)Math.PI / 2, 0.0f, 0);
                mesh.Translate(-0.05f, -0.1f, 0.15f);
            }
            else if (stack.Collectible.Code.Path.Contains("quiver-waist"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.45f, 0.45f, 0.45f);
                //mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), ((float)Math.PI / 2), 0.0f, 0);
                mesh.Translate(-0.05f, -0.4f, 0.15f);
            }
            else if (stack.Collectible.Code.Path.Contains("poultice"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.55f, 0.55f, 0.55f);
                mesh.Translate(0f, -3f / 16, 0f);
            }
            else if (stack.Collectible.Code.Path.Contains("stone-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.65f, 0.65f, 0.65f);
                mesh.Translate(0f, -2.5f / 16, 0f);
            }
            else if (stack.Collectible.Code.Path.Equals("rope"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.65f, 0.65f, 0.65f);
                mesh.Translate(0f, -2.5f / 16, 0f);
            }
            else if (stack.Collectible.Code.Path.Contains("plank-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.65f, 0.65f, 0.65f);
                mesh.Translate(0f, -2.5f / 16, 0f);
            }
            else if (stack.Collectible.Code.Path.Contains("clothes-lowerbody"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.55f, 0.55f, 0.55f);
                mesh.Translate(0f, -2.5f / 16, 0f);
            }
            else if (stack.Collectible.Code.Path.Contains("clothes-upperbody"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.55f, 0.55f, 0.55f);
                mesh.Translate(0f, -7f / 16, 0f);
            }
            else if (stack.Collectible.Code.Path.Contains("clothes-nadiya-head"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.55f, 0.55f, 0.55f);
                mesh.Translate(0f, -15f / 16, 0f);
            }
            else if (stack.Collectible.Code.Path.Contains("clothes-nadiya-foot"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.55f, 0.55f, 0.55f);
                mesh.Translate(0f, -2f / 16, 0f);
            }
            else if (stack.Collectible.Code.Path.Contains("clothes-head"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.55f, 0.55f, 0.55f);
                mesh.Translate(0f, -15f / 16, 0f);
            }
            else if (stack.Collectible.Code.Path.Contains("clothes-face"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.55f, 0.55f, 0.55f);
                mesh.Translate(0f, -15f / 16, 0f);
            }
            else if (stack.Collectible.Code.Path.Equals("blade-forlorn-iron") || stack.Collectible.Code.Path.Equals("blade-blackguard-iron"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.45f, 0.45f, 0.45f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, 0.2f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("part-shortsword-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.45f, 0.45f, 0.45f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, 0.2f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("part-greatsword-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.45f, 0.45f, 0.45f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, 0.2f, 0.1f);
            }
            else if (stack.Collectible.Code.Path.Contains("handle-"))
            {
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.45f, 0.45f, 0.45f);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0.0f, (float)Math.PI / 2);
                mesh.Translate(-0.14f, 0.2f, 0.1f);
            }

            if (facing == BlockFacing.EAST)
            {
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, -2.35f, 0f);
            }
            else if (facing == BlockFacing.WEST)
            {
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 1.0f, 0f);
            }
            else if (facing == BlockFacing.NORTH)
            {
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, -1.0f, 0f);
            }
            else
            {
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 2.35f, 0f);
            }
        }
    }
}
