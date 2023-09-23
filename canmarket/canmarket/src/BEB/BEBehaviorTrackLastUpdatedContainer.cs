using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace canmarket.src.BEB
{
    public class BEBehaviorTrackLastUpdatedContainer : BlockEntityBehavior
    {
        public int markToUpdaete;
        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);

        }
            public BEBehaviorTrackLastUpdatedContainer(BlockEntity blockentity) : base(blockentity)
        {
            markToUpdaete = 0;
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("markToUpdaete", markToUpdaete);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            this.markToUpdaete = tree.GetInt("markToUpdaete");
        }
    }
}
