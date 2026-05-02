using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace runestory.src.entity.spells
{
    public class EnforceTrust : BaseRuneEnt
    {
        public override void OnTouchEntity(Entity entity)
        {
            YoureMyFriendNow();
            Die();
        }
        public override void OnCollided()
        {
            YoureMyFriendNow();
            Die();
        }
        public void YoureMyFriendNow()
        {
            Entity[] friends = World.GetEntitiesAround(Pos.AsBlockPos.ToVec3d(), 1f, 1f, ent => ent.WatchedAttributes.GetInt("generation", -1) < 3);
            for(int i=0; i<friends.Length;i++)
            {
                Entity Frien = friends.ElementAt(World.Rand.Next(0,friends.Length));
                if(Frien?.WatchedAttributes.GetInt("generation") < 3)
                {
                    Frien.Attributes.SetString("origin", "reproduction");

                    if (Api.Side == EnumAppSide.Client) { break; }
                    Frien.WatchedAttributes.SetInt("generation", 3);
                    Frien.MarkTagsDirty();
                    break;
                }
            }
        }
    }
}
