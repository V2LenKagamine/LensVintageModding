using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace runestory
{
    public class GrowSpell : BaseRuneEnt
    {
        public override void OnTouchEntity(Entity entity)
        {
        }
        public override void OnEntitySpawn()
        {
            base.OnEntitySpawn();
            if ((spawnedBy as EntityPlayer)?.BlockSelection?.Position is null || Api.Side==EnumAppSide.Client) { return; }
            BlockPos target = (spawnedBy as EntityPlayer)?.BlockSelection?.Position;
            Vec3i range = new(0, 1, 0);
            Api.World.BlockAccessor.WalkBlocks(new (target.Copy().AsVec3i - range,Pos.Dimension),new (target.Copy().AsVec3i + range, Pos.Dimension), (block,ex,why,zee) =>
            {
                if(Api.World.BlockAccessor.GetBlockEntity(new(ex,why,zee))is BlockEntityFarmland farm)
                {
                    farm.ConsumeNutrients(EnumSoilNutrient.N, -5);
                    farm.ConsumeNutrients(EnumSoilNutrient.K, -5);
                    farm.ConsumeNutrients(EnumSoilNutrient.P, -5);
                }
            });
            Die();
        }
    }
}
