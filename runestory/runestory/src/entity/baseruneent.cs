using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace runestory
{
    public abstract class BaseRuneEnt : Entity
    {
        public override bool ApplyGravity => false;
        public override bool IsInteractable => false;

        public BaseRuneSpell ourSpell;

        long msLaunch;

        protected bool beforeCollided;
        protected Vec3d motionBeforeCollide = new Vec3d();

        public Entity spawnedBy;
        public float Damage;
        EntityPartitioning ep;

        public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
        {
            base.Initialize(properties, api, InChunkIndex3d);

            msLaunch = World.ElapsedMilliseconds;

            GetBehavior<EntityBehaviorPassivePhysics>().OnPhysicsTickCallback = OnPhysTick;
            ep = api.ModLoader.GetModSystem<EntityPartitioning>();
        }

        public override void OnGameTick(float dt)
        {
            base.OnGameTick(dt);
            if (ShouldDespawn) return;
            if (TryAttackEntity()) { return; }
            motionBeforeCollide.Set(Pos.Motion.X, Pos.Motion.Y, Pos.Motion.Z);
            beforeCollided = false;
        }
        public override void OnCollided()
        {
            Die();
        }

        public override void OnEntitySpawn()
        {
            base.OnEntitySpawn();
            StartAnimation("idle");
        }

        void OnPhysTick(float dtFac)
        {
            if (ShouldDespawn || !Alive) { return; }
            var pos = Pos;
            Cuboidd projectileBox = SelectionBox.ToDouble().Translate(pos.X, pos.Y, pos.Z);

            if (pos.Motion.X < 0) projectileBox.X1 += pos.Motion.X * dtFac;
            else projectileBox.X2 += pos.Motion.X * dtFac;
            if (pos.Motion.Y < 0) projectileBox.Y1 += pos.Motion.Y * dtFac;
            else projectileBox.Y2 += pos.Motion.Y * dtFac;
            if (pos.Motion.Z < 0) projectileBox.Z1 += pos.Motion.Z * dtFac;
            else projectileBox.Z2 += pos.Motion.Z * dtFac;

            ep.WalkEntityPartitions(pos.XYZ, 3f, (e) => {
                if (e.EntityId == EntityId || (spawnedBy != null && e.EntityId == spawnedBy.EntityId)) return true;

                Cuboidd eBox = e.SelectionBox.ToDouble().Translate(e.Pos.X, e.Pos.Y, e.Pos.Z);

                if (eBox.IntersectsOrTouches(projectileBox))
                {
                    OnTouchEntity(e);
                    return false;
                }

                return true;
            });
        }

        bool TryAttackEntity()
        {
            if (World is IClientWorldAccessor || World.ElapsedMilliseconds <= msLaunch + 100) return false;

            Cuboidd projectileBox = SelectionBox.ToDouble().Translate(Pos.X, Pos.Y, Pos.Z);

            Entity attacked = World.GetNearestEntity(Pos.XYZ, 5f, 5f, (e) => {
                if (e.EntityId == this.EntityId || !e.IsInteractable) return false;
                if (spawnedBy != null && e.EntityId == spawnedBy.EntityId)
                {
                    return false;
                }

                Cuboidd eBox = e.SelectionBox.ToDouble().Translate(e.Pos.X, e.Pos.Y, e.Pos.Z);

                return eBox.IntersectsOrTouches(projectileBox);
            });

            if (attacked != null)
            {
                OnTouchEntity(attacked);
                return true;
            }
            return false;
        }

        public virtual void SetRotation()
        {
            EntityPos pos = Pos;

            double speed = pos.Motion.Length();

            if (speed > 0.01)
            {
                pos.Pitch = 0;
                pos.Yaw =
                    GameMath.PI + (float)Math.Atan2(pos.Motion.X / speed, pos.Motion.Z / speed)
                    + GameMath.Cos((World.ElapsedMilliseconds - msLaunch) / 200f) * 0.03f;
                pos.Roll =
                    -(float)Math.Asin(GameMath.Clamp(-pos.Motion.Y / speed, -1, 1))
                    + GameMath.Sin((World.ElapsedMilliseconds - msLaunch) / 200f) * 0.03f;
            }
        }
        public abstract void OnTouchEntity(Entity entity);

        public void SimpleHitEntity(Entity? entity,DamageSource? dmgSrc = null, Vec2f? range = null,bool ignite = false)
        {
            if (!Alive) return;

            EntityPos pos = Pos;

            IServerPlayer fromPlayer = null;
            if (spawnedBy is EntityPlayer)
            {
                fromPlayer = (spawnedBy as EntityPlayer).Player as IServerPlayer;
            }

            bool targetIsPlayer = entity is EntityPlayer;
            bool targetIsCreature = entity is EntityAgent;
            bool canDamage = true;

            ICoreServerAPI sapi = World.Api as ICoreServerAPI;
            if (fromPlayer != null)
            {
                if (targetIsPlayer && (!sapi.Server.Config.AllowPvP || !fromPlayer.HasPrivilege("attackplayers"))) canDamage = false;
                if (targetIsCreature && !fromPlayer.HasPrivilege("attackcreatures")) canDamage = false;
            }

            pos.Motion.Set(0, 0, 0);

            if (canDamage && World.Side == EnumAppSide.Server)
            {
                float dmg = Damage;
                if (spawnedBy != null) dmg *= spawnedBy.Stats.GetBlended(runestoryModSystem.RMS_Stat_MagicDamage);

                bool didDamage = false;

                range = range ?? new(0.25f, 0.25f);
                Entity[] inrange = Api.World.GetEntitiesAround(Pos.XYZ, (float)range.X, (float)range.Y, inr => inr.Alive && inr != this);
                if(entity is not null)
                {
                    inrange = inrange.Append(entity);
                }
                for(int i =0;i<inrange.Length;i++) 
                {
                    Entity target = inrange.ElementAt(i);
                    target.ReceiveDamage(dmgSrc ?? new DamageSource()
                    {
                        Source = fromPlayer != null ? EnumDamageSource.Player : EnumDamageSource.Entity,
                        SourceEntity = this,
                        CauseEntity = spawnedBy,
                        Type = EnumDamageType.PiercingAttack
                    }, dmg);
                    if (ignite) {
                        target.ReceiveDamage(new DamageSource()
                        {
                            Source = fromPlayer != null ? EnumDamageSource.Player : EnumDamageSource.Entity,
                            SourceEntity = this,
                            CauseEntity = spawnedBy,
                            TicksPerDuration = 20,
                            Duration = TimeSpan.FromSeconds(10),
                            Type = EnumDamageType.Fire
                        }, dmg);
                    }
                }


                if (spawnedBy is EntityPlayer && didDamage)
                {
                    World.PlaySoundFor(new AssetLocation("game:sounds/player/projectilehit"), (spawnedBy as EntityPlayer).Player, false, 24);
                }

                Die();
            }
        }
    }
}
