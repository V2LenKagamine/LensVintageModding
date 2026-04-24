using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace runestory
{
    public class MakeStickEnt : BaseRuneEnt
    {
        public override void OnTouchEntity(Entity entity)
        {
            //sorry nothing
        }

        public override void OnCollided()
        {
            Api.World.BlockAccessor.WalkBlocks(Pos.XYZ.AddCopy(-1, -1, -1).AsBlockPos, Pos.XYZ.AddCopy(1, 1, 1).AsBlockPos, (block,ex,why,zee) =>
            {
                if(block.Code.Path.Contains("leaves") || block.Code.Path.Contains("leavesbranchy")) {
                    Api.World.BlockAccessor.SetBlock(0, new(ex, why, zee));
                    Api.World.SpawnItemEntity(new(Api.World.GetItem("game:stick")), new Vec3d(ex, why, zee));
                }
            });
            Die();
        }
    }
}
