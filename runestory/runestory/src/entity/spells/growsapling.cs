using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace runestory
{
    public class GrowSapling : BaseRuneEnt
    {
        public override void OnTouchEntity(Entity entity)
        {
            Bees();
            Die();
        }
        public override void OnCollided()
        {
            Bees();
            Die();
        }
        public void Bees()
        {
            if (Api.Side==EnumAppSide.Client) { return; }
            bool done = false;
            Vec3i range = new(1, 1, 1);
            Api.World.BlockAccessor.WalkBlocks(new (Pos.Copy().AsBlockPos.AsVec3i - range,Pos.Dimension),new (Pos.Copy().AsBlockPos.AsVec3i + range, Pos.Dimension), (block,ex,why,zee) =>
            {
                if (!done) {
                    BlockPos targ = new(ex, why, zee);
                    if (Api.World.BlockAccessor.GetBlockEntity(targ) is BlockEntitySapling sap)
                    {
                        //I hate this too.
                        double hors = (double)HarmonyLib.AccessTools.Field(typeof(BlockEntitySapling), "totalHoursTillGrowth").GetValue(sap);
                        HarmonyLib.AccessTools.Field(typeof(BlockEntitySapling), "totalHoursTillGrowth").SetValue(sap,hors - 72f);
                        sap.MarkDirty();
                        done = true;
                        
                    }
                }
            });
        }
    }
}
