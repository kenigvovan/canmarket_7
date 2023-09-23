using canmarket.src.BE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace canmarket.src.Render
{
    public class BECANMarketRenderer : IRenderer, IDisposable
    {
        public ICoreClientAPI capi;
        Vec3d bePos;
        BEMarket be;
        public double RenderOrder => 0.5f;

        public int RenderRange => 20;
        public BECANMarketRenderer(BEMarket be, Vec3d pos, ICoreClientAPI capi)
        {
            this.be = be;
            bePos = pos;
            this.capi = capi;
            capi.Event.RegisterRenderer((IRenderer)this, EnumRenderStage.Opaque, "becanmarketrenderer");
        }
        public void Dispose()
        {
            capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            Vec3d camPos = this.capi.World.Player.Entity.CameraPos;
            if(camPos.DistanceTo(bePos) > Config.Current.MESHES_RENDER_DISTANCE.Val)
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
