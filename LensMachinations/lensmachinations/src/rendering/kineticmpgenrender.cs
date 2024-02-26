using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace LensstoryMod
{
    //Mostly stolen from 'ACulinaryArtillery'
    public class KineticMPRenderer : IRenderer
    {

        internal bool ShouldRender;
        internal bool Rotate;

        public KineticpotentianatorBhv mechBhv;
        public double RenderOrder => 0.5f;
        public int RenderRange => 24;

        private ICoreClientAPI api;
        private BlockPos blockPos;
        MeshRef meshRef;
        public Matrixf ModelMat = new();
        public float AngleRad;

        public KineticMPRenderer(ICoreClientAPI coreAPI,BlockPos pos,MeshData mesh) 
        {
            api = coreAPI;
            blockPos = pos;
            meshRef = coreAPI.Render.UploadMesh(mesh);
        }

        public void Dispose()
        {
            api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if(meshRef == null || !ShouldRender) { return; }
            IRenderAPI rpi = api.Render;
            Vec3d camPos = api.World.Player.Entity.CameraPos;
            rpi.GlDisableCullFace();
            rpi.GlToggleBlend(true);

            IStandardShaderProgram prog = rpi.PreparedStandardShader(blockPos.X, blockPos.Y, blockPos.Z);
            prog.Tex2D = api.BlockTextureAtlas.AtlasTextures[0].TextureId;
            prog.ModelMatrix = ModelMat
            .Identity()
            .Translate(blockPos.X - camPos.X, blockPos.Y - camPos.Y, blockPos.Z - camPos.Z)
            .Translate(0.5f, 0, 0.5f)
            .RotateY(AngleRad)
                .Translate(-0.5f, 0, -0.5f)
                .Values
            ;

            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            rpi.RenderMesh(meshRef);
            prog.Stop();


            if (Rotate)
            {
                AngleRad = mechBhv.AngleRad;
            }
        }
    }
}
