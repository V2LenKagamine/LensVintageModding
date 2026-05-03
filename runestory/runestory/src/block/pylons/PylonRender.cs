using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace runestory
{
    //Mostly stolen from 'ACulinaryArtillery'
    public class PylonRenderer : IRenderer
    {

        public double RenderOrder => 0.5f;
        public int RenderRange => 24;

        private ICoreClientAPI api;
        private BlockPos blockPos;
        MeshRef meshRef;
        MeshRef meshRefRunes;
        public Matrixf ModelMat = new();
        public float AngleRad;
        public float runeRad;
        public float whyPos;

        public PylonRenderer(ICoreClientAPI coreAPI, BlockPos pos, MeshData mesh,MeshData runes)
        {
            api = coreAPI;
            blockPos = pos;
            meshRef = coreAPI.Render.UploadMesh(mesh);
            meshRefRunes = coreAPI.Render.UploadMesh(runes);
        }

        public void Dispose()
        {
            api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (meshRef == null) { return; }
            IRenderAPI rpi = api.Render;
            Vec3d camPos = api.World.Player.Entity.CameraPos;
            rpi.GlDisableCullFace();
            rpi.GlToggleBlend(true);

            IStandardShaderProgram prog = rpi.PreparedStandardShader(blockPos.X, blockPos.Y, blockPos.Z);
            prog.Tex2D = api.BlockTextureAtlas.AtlasTextures[0].TextureId;
            prog.ModelMatrix = ModelMat
            .Identity()
            .Translate(blockPos.X - camPos.X, blockPos.Y - camPos.Y, blockPos.Z - camPos.Z)
            .Translate(0.5f, 0.5f, 0.5f)
            .RotateY(AngleRad)
                .Translate(-0.5, -0.5f + (Math.Sin(whyPos)*0.08f), -0.5f)
                .Values
            ;
            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            rpi.RenderMesh(meshRef);
            //runes
            prog.Tex2D = api.BlockTextureAtlas.AtlasTextures[0].TextureId;
            prog.ModelMatrix = ModelMat
            .Identity()
            .Translate(blockPos.X - camPos.X, blockPos.Y - camPos.Y, blockPos.Z - camPos.Z)
            .Translate(0.5f, 0.5f, 0.5f)
            .RotateY(runeRad)
                .Translate(-0.5, -0.5f + (Math.Sin(whyPos) * 0.08f), -0.5f)
                .Values
            ;
            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            rpi.RenderMesh(meshRefRunes);

            prog.Stop();

            AngleRad = (AngleRad + 0.005f)%360f;
            runeRad = (runeRad - 0.005f) % 360f;
            whyPos += deltaTime * 0.5f;
        }
    }
}
