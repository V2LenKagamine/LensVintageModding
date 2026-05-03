using System;
using System.Collections.Generic;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace runestory
{
    public static class LenUtil
    {
        public static void TakeKBFrom(ICoreAPI api, Entity from, Entity target, float strength)
        {
            if (target is null || from is null) { return; }
            float exx = (float)(Math.Abs(from.Pos.X) - Math.Abs(target.Pos.X));
            float why = (float)(Math.Abs(from.Pos.Y) - Math.Abs(target.Pos.Y));
            float zee = (float)(Math.Abs(from.Pos.Z) - Math.Abs(target.Pos.Z));

            Vec3d normed = new(exx, why, zee);

            normed.Normalize();

            normed.Y *= 0.5f;

            float num = GameMath.Clamp((1f - target.Properties.KnockbackResistance) / 10f, 0f, 1f) * strength;


            target.OnGround = false;
            target.Pos.Y += 0.6f;
            api.World.RegisterCallback((dt) =>
            target.Pos.Motion += normed * -num, 100);
        }

        public static void TessellateObj(this ShapeTesselator tessellator, CompositeShape compositeShape, out MeshData modeldata, TextureAtlasPosition pos,ICoreClientAPI api,string backupasset)
        {
            var meta = tessellator.GetField<TesselationMetaData>("meta");
            var objTesselator = tessellator.GetField<ObjTesselator>("objTesselator");
            var objs = tessellator.GetField<Vintagestory.API.Datastructures.OrderedDictionary<AssetLocation, IAsset>>("objs");

            meta.UsesColorMap = false;
            if (objs.TryGetValue(compositeShape.Base) is null)
            {
                objs.Add(compositeShape.Base, api.Assets.TryGet(backupasset));
            }
            objTesselator.Load(objs[compositeShape.Base],out modeldata,pos,meta,0);
            tessellator.ApplyCompositeShapeModifiers(ref modeldata, compositeShape);
        }
        public static T GetField<T>(this object instance, string fieldName)
        {
            return (T)AccessTools.Field(instance.GetType(), fieldName).GetValue(instance);
        }

        public static void SetField(this object instance, string fieldName,object value)
        {
           AccessTools.Field(instance.GetType(), fieldName).SetValue(instance,value);
        }

        public static IAsset GetOrCreateObj(ICoreClientAPI capi,AssetLocation objpath)
        {
            IAsset texAsset = capi.Assets.TryGet(objpath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".obj"));

            if(texAsset is null)
            {
                capi.Event.EnqueueMainThreadTask(() => capi.Assets.AddModOrigin("runestory", objpath), "");
                texAsset = capi.Assets.TryGet(objpath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".obj"));
            }

            return texAsset;
        }
    }
}
