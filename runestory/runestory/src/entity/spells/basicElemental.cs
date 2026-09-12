using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

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
            Entity[] around = World.GetEntitiesAround(Pos.XYZ.ToVec3f().ToVec3d(), 0.1f, 0.1f);
            if(around.Length > 0)
            {
                HitEntity(around.First());
            }
            Die();
        }

        public void HitEntity(Entity entity)
        {
            if (Api.Side == EnumAppSide.Client || entity is null) { return; }
            int? tier = ourSpell.spellTier;
            Vec2f aoe = new(0f, 0f);
            if (tier is null || tier == 0) { return; }
            float dam = 1f;
            switch (tier)
            {
                case 1:
                    {
                        dam = 4.5f;
                        break;
                    }
                case 2:
                    {
                        dam = 6f;
                        break;
                    }
                case 3:
                    {
                        aoe = new(3f, 3f);
                        dam = 7.5f;
                        break;
                    }
                case 4:
                    {
                        aoe = new(4f, 4f);
                        dam = 10f;
                        break;
                    }
                case 5:
                    {
                        aoe = new(5f, 5f);
                        dam = 12.5f;
                        break;
                    }
            }
            DamageSource hitdmg = new()
            {
                Source = EnumDamageSource.Player,
                CauseEntity = spawnedBy,
                SourceEntity = this,
                KnockbackStrength = 0.75f * tier ?? 1,
                Type = EnumDamageType.PiercingAttack

            };
            bool ignition = false;

            if (ourSpell.ElementalType.Contains("water"))
            {
                spawnedBy.ReceiveDamage(new DamageSource()
                {
                    Source = EnumDamageSource.Unknown,
                    Type = EnumDamageType.Heal,
                    TicksPerDuration = 1,
                    Duration = TimeSpan.FromSeconds(0.5f * tier ?? 1)
                }, 0.25f * tier ?? 1);
            }
            if (ourSpell.ElementalType.Contains("earth"))
            {
                dam *= 1.2f;
            }
            if (ourSpell.ElementalType.Contains("air"))
            {
                hitdmg.KnockbackStrength *= 1.25f;
            }
            if (ourSpell.ElementalType.Contains("fire"))
            {
                ignition = true;
            }

            Damage = dam * Api.ModLoader.GetModSystem<RunestoryMS>().RMS_LoadedConfig?.GlobalMagicDamageMultiplier ?? 1f;
            SimpleHitEntity(entity, hitdmg, aoe, ignition);
            Die();
        }
    }
}
