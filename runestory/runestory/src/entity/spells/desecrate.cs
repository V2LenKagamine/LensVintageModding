using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace runestory.src.entity.spells
{
    public class DesecrateSpell : BaseRuneEnt
    {
        public override void OnTouchEntity(Entity entity)
        {
            DoThing(entity);
            Die();
        }

        public override void OnCollided()
        {
            DoThing(Api.World.GetNearestEntity(Pos.XYZ, 1, 1));
            Die();
        }

        public void DoThing(Entity entity)
        {
            if(Api.Side == EnumAppSide.Client) { return; }
            IHarvestable harvestable = entity?.GetInterface<IHarvestable>();
            if (harvestable != null && !entity.Alive)
            {
                try
                {
                    harvestable.SetHarvested((spawnedBy as EntityPlayer).Player, 1.2f);
                }
                catch (Exception e)
                {
                    //Fuck you why and how
                }
            }
        }
    }
}
