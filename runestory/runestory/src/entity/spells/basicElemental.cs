using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace runestory.src.entity.spells
{
    public class BasicElemental : BaseRuneEnt
    {
        public override void OnTouchEntity(Entity entity)
        {
            if (entity != spawnedBy && entity is not null)
            {
                HitEntity(entity);
                Die();
            }
        }

        public override void OnCollided()
        {
            HitEntity(null);
            Die();
        }

        public void HitEntity(Entity entity)
        {
            if(Api.Side == EnumAppSide.Client) { return; }
            int tier = 0;
            Vec2f aoe = new(0f, 0f);
            foreach (KeyValuePair<string, int> reag in ourSpell.Reagents)
            {
                switch (reag.Key)
                {
                    case string x when x.Contains("alpha"):
                        {
                            tier = 1;
                            aoe = new(0.5f, 0.5f);
                            break;
                        }
                    case string x when x.Contains("beta"):
                        {
                            tier = 2;
                            aoe = new(1f, 1f);
                            break;
                        }
                    case string x when x.Contains("gamma"):
                        {
                            tier = 3;
                            aoe = new(5f, 5f);
                            break;
                        }
                    default: { break; }
                }
                if (tier > 0) { continue; }
            }
            if(tier == 0) { return; }
            float dam = 3f * tier;
            DamageSource hitdmg = new()
            {
                Source = EnumDamageSource.Player,
                CauseEntity = spawnedBy,
                SourceEntity = this,
                KnockbackStrength = 0.5f,
                Type = EnumDamageType.PiercingAttack

            };
            bool ignition = false;
            switch (ourSpell.ElementalType)
            {
                case "water":
                    {
                        TempBuff tmp = new();
                        tmp.DoStats(spawnedBy as EntityPlayer, "hungerrate", -0.05f * tier, (30 * 1000) * tier, "waterbuff" ,"waterbuff");

                        break;
                    }
                case "earth":
                    {
                        dam *= 1.2f;
                        break;
                    }
                case "air":
                    {
                        hitdmg.KnockbackStrength *= 2;
                        break;
                    }
                case "fire":
                    {
                        dam *= 1.1f;
                        ignition = true;
                        break;
                    }
            }
            Damage = dam;
            SimpleHitEntity(entity ?? null, hitdmg, aoe, ignition);
            Die();
        }
    }
}
