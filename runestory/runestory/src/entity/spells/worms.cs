using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace runestory.src.entity.spells
{
    public class WormSpell : BaseRuneEnt
    {
        public override void OnTouchEntity(Entity entity)
        {
            Worms();
            Die();
        }

        public override void OnCollided()
        {
            Worms();
            Die();
        }

        public void Worms()
        {
            if (Api.Side == EnumAppSide.Client) { return; }
            Api.World.BlockAccessor.WalkBlocks(Pos.XYZ.AddCopy(-1, -1, -1).AsBlockPos, Pos.XYZ.AddCopy(1, 1, 1).AsBlockPos, (blocc, x, y, z) =>
            {
                BlockPos looking = new BlockPos(x, y, z);

                if (Api.World.BlockAccessor.GetBlock(looking).Fertility > 0 && Api.World.Rand.NextDouble() <= 0.5f)
                {
                    EntityProperties type = World.GetEntityType(new AssetLocation("game:earthworm"));
                    Entity eWorm = World.ClassRegistry.CreateEntity(type);

                    eWorm.Pos.X = x + (float)World.Rand.NextDouble();
                    eWorm.Pos.Y = y + 1;
                    eWorm.Pos.Z = z + (float)World.Rand.NextDouble();
                    eWorm.Pos.Yaw = (float)World.Rand.NextDouble() * 2 * GameMath.PI;

                    World.SpawnEntity(eWorm);
                }
            });
        }
    }
}
