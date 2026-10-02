using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace runestory.src.block.pylons
{
    public class RegenerativePylonBe : BlockEntity
    {
        private long ticklist;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            ticklist = api.World.RegisterGameTickListener(PylonTick, 15000);
        }

        public void PylonTick(float dt)
        {
            if (Api.Side == EnumAppSide.Client) { return; }

            Entity[] near = Api.World.GetEntitiesAround(Pos.ToVec3d(), 14, 14, (ent) => ent.GetBehavior<EntityBehaviorHealth>() is not null);

            for (int i = 0; i < near.Length; i++)
            {
                DamageSource heal = new() {
                    Source = EnumDamageSource.Unknown,
                    Type = EnumDamageType.Heal,
                    TicksPerDuration = 10,
                    Duration = TimeSpan.FromSeconds(15)
                };
                if (near[i].ShouldReceiveDamage(heal, 0.3f)) 
                {
                    near[i].ReceiveDamage(heal, 0.3f);
                }
            }
        }
        public override void OnBlockRemoved()
        {
            Api.World.UnregisterGameTickListener(ticklist);
            base.OnBlockRemoved();
        }
    }
}
