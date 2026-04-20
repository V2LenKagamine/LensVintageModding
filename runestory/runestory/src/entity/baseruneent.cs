using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace runestory
{
    public abstract class BaseRuneEnt : Entity
    {
        public override bool ApplyGravity => false;
        public override bool IsInteractable => false;

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
            motionBeforeCollide.Set(ServerPos.Motion.X, ServerPos.Motion.Y, ServerPos.Motion.Z);
            beforeCollided = false;
        }
        public override void OnCollided()
        {
            Die();
        }

        void OnPhysTick(float dtFac)
        {
            if (ShouldDespawn || !Alive) { return; }
            var pos = SidedPos;
            Cuboidd projectileBox = SelectionBox.ToDouble().Translate(pos.X, pos.Y, pos.Z);

            if (pos.Motion.X < 0) projectileBox.X1 += pos.Motion.X * dtFac;
            else projectileBox.X2 += pos.Motion.X * dtFac;
            if (pos.Motion.Y < 0) projectileBox.Y1 += pos.Motion.Y * dtFac;
            else projectileBox.Y2 += pos.Motion.Y * dtFac;
            if (pos.Motion.Z < 0) projectileBox.Z1 += pos.Motion.Z * dtFac;
            else projectileBox.Z2 += pos.Motion.Z * dtFac;

            ep.WalkEntityPartitions(pos.XYZ, 5f, (e) => {
                if (e.EntityId == EntityId || (spawnedBy != null && e.EntityId == spawnedBy.EntityId)) return true;

                Cuboidd eBox = e.SelectionBox.ToDouble().Translate(e.ServerPos.X, e.ServerPos.Y, e.ServerPos.Z);

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
            if (World is IClientWorldAccessor || World.ElapsedMilliseconds <= msLaunch + 250) return false;

            Cuboidd projectileBox = SelectionBox.ToDouble().Translate(ServerPos.X, ServerPos.Y, ServerPos.Z);

            Entity attacked = World.GetNearestEntity(ServerPos.XYZ, 5f, 5f, (e) => {
                if (e.EntityId == this.EntityId || !e.IsInteractable) return false;
                if (spawnedBy != null && e.EntityId == spawnedBy.EntityId && World.ElapsedMilliseconds - msLaunch < 250)
                {
                    return false;
                }

                Cuboidd eBox = e.SelectionBox.ToDouble().Translate(e.ServerPos.X, e.ServerPos.Y, e.ServerPos.Z);

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
            EntityPos pos = SidedPos;

            double speed = pos.Motion.Length();

            if (speed > 0.01)
            {
                pos.Pitch = 0;
                pos.Yaw =
                    GameMath.PI + (float)Math.Atan2(pos.Motion.X / speed, pos.Motion.Z / speed)
                    + GameMath.Cos((World.ElapsedMilliseconds - msLaunch) / 200f) * 0.03f
                ;
                pos.Roll =
                    -(float)Math.Asin(GameMath.Clamp(-pos.Motion.Y / speed, -1, 1))
                    + GameMath.Sin((World.ElapsedMilliseconds - msLaunch) / 200f) * 0.03f
                ;
            }
        }
        public abstract void OnTouchEntity(Entity entity);

        public void SimpleHitEntity(Entity entity,DamageSource? dmgSrc = null)
        {
            if (!Alive) return;

            EntityPos pos = ServerPos;

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
                World.PlaySoundAt(new AssetLocation("game:sounds/arrow-impact"), this, null, false, 24);

                float dmg = Damage;
                if (spawnedBy != null) dmg *= spawnedBy.Stats.GetBlended("rangedWeaponsDamage");

                bool didDamage = entity.ReceiveDamage(dmgSrc ?? new DamageSource()
                {
                    Source = fromPlayer != null ? EnumDamageSource.Player : EnumDamageSource.Entity,
                    SourceEntity = this,
                    CauseEntity = spawnedBy,
                    Type = EnumDamageType.PiercingAttack
                }, dmg);

                    Die();
                if (spawnedBy is EntityPlayer && didDamage)
                {
                    World.PlaySoundFor(new AssetLocation("game:sounds/player/projectilehit"), (spawnedBy as EntityPlayer).Player, false, 24);
                }
            }
        }
    }
}
