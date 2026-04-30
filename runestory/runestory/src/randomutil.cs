using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace runestory
{
    public static class LenUtil
    {
        public static void TakeKBFrom(ICoreAPI api,Entity from,Entity target,float strength)
        {
            if(target is null || from is null) { return; }
            float exx = (float)(Math.Abs(from.Pos.X) - Math.Abs(target.Pos.X));
            float why = (float)(Math.Abs(from.Pos.Y) - Math.Abs(target.Pos.Y));
            float zee = (float)(Math.Abs(from.Pos.Z) - Math.Abs(target.Pos.Z));

            Vec3d normed = new(exx,why,zee);

            normed.Normalize();

            normed.Y *= 0.5f;

            float num = GameMath.Clamp((1f - target.Properties.KnockbackResistance) / 10f, 0f, 1f) * strength;


            target.OnGround = false;
            target.Pos.Y += 0.6f;
            api.World.RegisterCallback((dt) =>
            target.Pos.Motion += normed * -num, 100);
        }
    }
}
