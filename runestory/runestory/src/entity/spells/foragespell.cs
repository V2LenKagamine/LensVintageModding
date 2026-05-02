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
    public class ForageSpell : BaseRuneEnt
    {
        public override void OnEntitySpawn()
        {
            base.OnEntitySpawn();
            DoForage();
            Die();
        }

        public void DoForage()
        {
            if (Api.Side == EnumAppSide.Client || spawnedBy is null) { return; }

            Item[] seeds = Api.World.SearchItems("game:seeds-*");
            Api.World.SpawnItemEntity(new(seeds.ElementAt(World.Rand.Next(0, seeds.Length)), World.Rand.Next(1, 3)), Pos.AsBlockPos);
            if(World.Rand.NextDouble() >0.975f)
            {
                Item[] nice = Api.World.SearchItems("game:fruit-*");
                Api.World.SpawnItemEntity(new(nice.ElementAt(World.Rand.Next(0, nice.Length)), World.Rand.Next(1, 3)), Pos.AsBlockPos);
            }
        }

        public override void OnTouchEntity(Entity entity)
        {
            //no.
        }
    }
}
