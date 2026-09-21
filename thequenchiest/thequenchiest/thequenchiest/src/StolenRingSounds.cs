using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace thequenchiest.src
{

    //If you're reading this, the only thing I """stole""" was the sounds, and I'd bet dimes to dollars you can find some on freesound.org. Deal with it.
    public class StolenRingSounds
    {
        private static readonly AssetLocation StrikeSound = new AssetLocation("thequenchiest:sounds/effect/anvilstrike");
        private static readonly AssetLocation DeadSound = new AssetLocation("thequenchiest:sounds/effect/anvilmuted");
        private static readonly string BaseRingSound = "thequenchiest:sounds/effect/anvilring-s";


        public static int StepFor(float stressRatio)
        {
            stressRatio = Math.Clamp(stressRatio, 0f, 1f);
            int num = Math.Clamp((int)Math.Floor(stressRatio * 12f + 0.5f),0,12);
            return num;
        }
        public static float PitchFor(float stressRatio)
        {
            stressRatio = Math.Clamp(stressRatio, 0f, 1f);
            return 0.45f + (1.2f * stressRatio);
        }
        public static void PlayMuted(IWorldAccessor world, Vec3d pos, float volume = 1f, float range = 24f)
        {
            world.PlaySoundAt(DeadSound, pos.X, pos.Y, pos.Z, null, 1f, range, volume);
        }
        public static void PlayRing(IWorldAccessor world, ItemStack stack, Vec3d pos)
        {
            if (stack.Attributes.GetBool(QuenchierQuenchBHV.OverStressed))
            {
                world.PlaySoundAt(StrikeSound, pos.X, pos.Y, pos.Z, null, 1f, 24f, 1f);
                world.PlaySoundAt(DeadSound, pos.X, pos.Y, pos.Z, null, 1f, 24f, 1f);
                return;
            }
            PlayRingAt(world, pos, stack.Collectible.GetBehavior<QuenchierQuenchBHV>().GetStressRatio(world,stack));
        }
        public static void PlayRingAt(IWorldAccessor world, Vec3d pos, float stressRatio)
        {
            world.PlaySoundAt(StrikeSound, pos.X, pos.Y, pos.Z, null, 1f, 24f, 1f);
            world.PlaySoundAt(new AssetLocation(BaseRingSound + world.Rand.NextInt64(0,13)), pos.X, pos.Y, pos.Z, null, PitchFor(stressRatio), 24f, 1f);
        }

    }
}
