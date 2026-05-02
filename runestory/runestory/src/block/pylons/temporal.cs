using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace runestory.src.block.pylons
{
    public class TemporalPylonBe : BlockEntity
    {
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            api.World.RegisterGameTickListener(PylonTick, 15000);
        }

        public void PylonTick(float dt)
        {
            if (Api.Side == EnumAppSide.Client) { return; }

            Entity[] near = Api.World.GetEntitiesAround(Pos.ToVec3d(),14,14,(ent) => (ent as EntityPlayer)?.Player is not null);

            for (int i = 0; i < near.Length; i++) {
                EntityPlayer ply = (EntityPlayer)near[i];
                if (ply.GetBehavior<EntityBehaviorTemporalStabilityAffected>() is EntityBehaviorTemporalStabilityAffected beh)
                {
                    beh.OwnStability += 0.005f*dt;
                }
            }
        }
    }
}
