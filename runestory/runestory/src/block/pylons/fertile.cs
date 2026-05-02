using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace runestory.src.block.pylons
{
    public class FertilePylonBe : BlockEntity
    {
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            api.World.RegisterGameTickListener(PylonTick, 300000);
        }

        public void PylonTick(float dt)
        {
            if(Api.Side == EnumAppSide.Client) { return; }
            Api.World.BlockAccessor.WalkBlocks(Pos.AddCopy(-4, -5, -4), Pos.AddCopy(4, 0, 4), (block, x, y, z) =>
            {
                BlockPos test = new(x, y, z);

                if (Api.World.BlockAccessor.GetBlockEntity<BlockEntityFarmland>(test) is BlockEntityFarmland soil)
                {
                    if(Api.World.Rand.NextDouble() < 0.05f)
                    {
                        switch(Api.World.Rand.Next(0,3))
                        {
                            case 0:
                                {
                                    soil.ConsumeNutrients(EnumSoilNutrient.N, -1f);
                                    break;
                                }
                            case 1:
                                {
                                    soil.ConsumeNutrients(EnumSoilNutrient.K, -1f);
                                    break;
                                }
                            case 2:
                                {
                                    soil.ConsumeNutrients(EnumSoilNutrient.P, -1f);
                                    break;
                                }
                        }
                        soil.MarkDirty();
                    }
                }

                if (Api.World.BlockAccessor.GetBlockEntity<BlockEntityBerryBushFarmland>(test) is BlockEntityBerryBushFarmland soi)
                {
                    if (Api.World.Rand.NextDouble() < 0.05f)
                    {
                        switch (Api.World.Rand.Next(0, 3))
                        {
                            case 0:
                                {
                                    soi.ConsumeNutrients(EnumSoilNutrient.N, -1f);
                                    break;
                                }
                            case 1:
                                {
                                    soi.ConsumeNutrients(EnumSoilNutrient.K, -1f);
                                    break;
                                }
                            case 2:
                                {
                                    soi.ConsumeNutrients(EnumSoilNutrient.P, -1f);
                                    break;
                                }
                        }
                        soi.MarkDirty();
                    }
                }

            });
        }
    }
}
