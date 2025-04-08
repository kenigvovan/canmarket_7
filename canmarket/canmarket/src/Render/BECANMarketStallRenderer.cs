using canmarket.src.BE;
using canmarket.src.BE.SupportClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace canmarket.src.Render
{
    public class BECANMarketStallRenderer: IRenderer, IDisposable
    {
        public ICoreClientAPI capi;
        Vec3d bePos;
        BECANMarketStall be;
        public double RenderOrder => 0.5f;

        public int RenderRange => 20;
        public BECANMarketStallRenderer(BECANMarketStall be, Vec3d pos, ICoreClientAPI capi)
        {
            this.be = be;
            bePos = pos;
            this.capi = capi;
            capi.Event.RegisterRenderer((IRenderer)this, EnumRenderStage.Opaque, "becanmarketstallrenderer");
        }
        public void Dispose()
        {
            capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (this?.capi?.World?.Player?.Entity?.CameraPos != null)
            {
                Vec3d camPos = this.capi.World.Player.Entity.CameraPos;
                if (camPos.DistanceTo(bePos) > canmarket.config.MESHES_RENDER_DISTANCE)
                {
                    if (this.be.shouldDrawMeshes)
                    {
                        this.be.shouldDrawMeshes = false;
                        this.be.MarkDirty(true);
                    }
                }
                else
                {
                    if (!this.be.shouldDrawMeshes)
                    {
                        this.be.shouldDrawMeshes = true;
                        this.be.MarkDirty(true);
                    }
                }
            }

        }
    }
}
