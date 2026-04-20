using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace runestory
{
    public class defaultSpell : Entity
    {
        public string spellCode;
        public Entity spawnedBy;
        public override void OnEntitySpawn()
        {
            base.OnEntitySpawn();
            if(spellCode != null)
            {
                EntityProperties? possible = Api.World.GetEntityType(new("runestory" + spellCode));
                if(possible is EntityProperties resolved)
                {
                    BaseRuneEnt goodspell = Api.World.ClassRegistry.CreateEntity(resolved) as BaseRuneEnt;
                    if (spawnedBy != null)
                    {
                        goodspell.spawnedBy = spawnedBy;
                        Vec3d pos = spawnedBy.ServerPos.XYZ.AddCopy(0, spawnedBy.LocalEyePos.Y, 0);
                        Vec3d ahead = pos.AheadCopy(1, spawnedBy.SidedPos.Pitch, spawnedBy.SidedPos.Yaw);
                        Vec3d velo = (ahead - pos);

                        goodspell.ServerPos.SetPos(spawnedBy.SidedPos.BehindCopy(0.21).XYZ.Add(0, spawnedBy.LocalEyePos.Y, 0));
                        goodspell.ServerPos.Motion.Set(velo);
                        goodspell.Pos.SetFrom(goodspell.ServerPos);
                        goodspell.World = spawnedBy.World;
                        goodspell.SetRotation();

                        Api.World.SpawnEntity(goodspell);
                    }
                }
            }
            Die();
        }
    }
}
