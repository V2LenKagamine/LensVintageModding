using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
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
            HitEntity(World.GetEntitiesAround(Pos.XYZ.ToVec3f().ToVec3d(),0.1f,0.1f).First());
            Die();
        }

        public void HitEntity(Entity entity)
        {
            if (Api.Side == EnumAppSide.Client || entity is null) { return; }
            int tier = ourSpell.spellTier;
            Vec2f aoe = new(0f, 0f);
            if (tier == 0) { return; }
            float dam = 1f;
            switch (tier)
            {
                case 1:
                    {
                        aoe = new(0f, 0f);
                        dam = 3.5f;
                        break;
                    }
                case 2:
                    {
                        aoe = new(0f, 0f);
                        dam = 4.5f;
                        break;
                    }
                case 3:
                    {
                        aoe = new(3f, 3f);
                        dam = 4.5f;
                        break;
                    }
                case 4:
                    {
                        aoe = new(3f, 3f);
                        dam = 6.5f;
                        break;
                    }
                case 5:
                    {
                        aoe = new(3f, 3f);
                        dam = 8f;
                        break;
                    }
            }
            DamageSource hitdmg = new()
            {
                Source = EnumDamageSource.Player,
                CauseEntity = spawnedBy,
                SourceEntity = this,
                KnockbackStrength = 0.75f * tier,
                Type = EnumDamageType.PiercingAttack

            };
            bool ignition = false;

            if (ourSpell.ElementalType.Contains("water"))
            {
                (spawnedBy as EntityPlayer).GetBehavior<PlayerTempBuffer>()?.AddTempBuff(spawnedBy as EntityPlayer, RunestoryMS.RMS_Stat_MagicDamage, 0.05f * tier, (30 * 1000) * tier, "waterbuff");
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

            Damage = dam;
            SimpleHitEntity(entity, hitdmg, aoe, ignition);
            Die();
        }
    }
}
