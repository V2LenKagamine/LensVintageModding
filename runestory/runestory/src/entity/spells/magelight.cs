using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace runestory.src.entity.spells
{
    public class MageLight : BaseRuneEnt
    {
        public override void OnEntitySpawn()
        {
            base.OnEntitySpawn();
            DoLight();
            Die();
        }

        public void DoLight()
        {
            if (Api.Side == EnumAppSide.Client || spawnedBy is null) { return; }
            Api.World.SpawnItemEntity(new(World.GetItem("runestory:runelamp"), 1), Pos.AsBlockPos);
        }

        public override void OnTouchEntity(Entity entity)
        {
            //no.
        }
    }
}
