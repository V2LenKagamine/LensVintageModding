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
    public class GoodBerry : BaseRuneEnt
    {
        public override void OnEntitySpawn()
        {
            base.OnEntitySpawn();
            DoBerry();
            Die();
        }

        public void DoBerry()
        {
            if (Api.Side == EnumAppSide.Client || spawnedBy is null) { return; }
            Api.World.SpawnItemEntity(new(World.GetItem("runestory:goodberryitem"), 1), Pos.AsBlockPos);
        }

        public override void OnTouchEntity(Entity entity)
        {
            //no.
        }
    }
}
