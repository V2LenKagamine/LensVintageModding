using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace runestory.src.entity.spells
{
    public class Ignite : BaseRuneEnt
    {
        public override void OnTouchEntity(Entity entity)
        {
            Fire(entity);
            Die();
        }

        public override void OnCollided()
        {
            FireB();
            Die();
        }

        public void Fire(Entity entity)
        {
            if (Api.Side == EnumAppSide.Client) { return; }
            entity.Ignite();
        }
        public void FireB()
        {
            if (Api.Side == EnumAppSide.Client) { return; }
            World.BlockAccessor.WalkBlocks(Pos.XYZ.AddCopy(1, 1, 1).AsBlockPos, Pos.XYZ.AddCopy(-1, -1, -1).AsBlockPos, (blocc, ex, why, zee) =>
            {
                BlockPos bloc = new(ex, why, zee);
                if (Api.World.BlockAccessor.GetBlockEntity(bloc) is BlockEntityPitKiln pit)
                {
                    pit.TryIgnite(spawnedBy as IPlayer);
                    pit.MarkDirty();
                    return;
                }
                if (Api.World.BlockAccessor.GetBlockEntity(bloc) is BlockEntityCharcoalPit cha)
                {
                    cha.IgniteNow();
                    cha.MarkDirty();
                    return;
                }
                if (Api.World.BlockAccessor.GetBlockEntity(bloc) is BlockEntityFirepit firep)
                {
                    if (!firep.IsBurning && firep.fuelStack is not null)
                    {
                        firep.igniteFuel();
                        firep.setBlockState("lit");
                        firep.MarkDirty();
                    }
                    return;
                }
                if (Api.World.BlockAccessor.GetBlock(bloc) is BlockTorch torch)
                {
                    if (torch.Variant.ContainsKey("state"))
                    {
                        AssetLocation blockCode = torch.CodeWithVariant("state", "lit");
                        World.BlockAccessor.SetBlock(World.GetBlock(blockCode).Id, bloc);
                    }
                    return;
                }
                if (Api.World.BlockAccessor.GetBlockEntity(bloc) is BlockEntityBomb bomb)
                {
                    bomb.Combust(4f);
                    return;
                }
                if (World.BlockAccessor.GetBlockEntity(bloc) is BlockEntityForge forg)
                {
                    forg.TryIgnite();
                    return;
                }
            });
        }
    }
}
