using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

namespace runestory.src.block.pylons
{
    public class BEBhvPylon : BlockEntityBehavior,ITexPositionSource
    {

        PylonRenderer render;

       private MeshData mesh(ITesselatorAPI tess)
        {
            Dictionary<string,MeshData> blockMeshes = ObjectCacheUtil.GetOrCreate(Api, "runepylonMeshes", () => new Dictionary<string, MeshData>());
            if (blockMeshes.TryGetValue("runepylonmeshcode", out var mesh)) return mesh;
            return blockMeshes["runepylonmeshcode"] = GenMesh((Api as ICoreClientAPI).BlockTextureAtlas);
        }
        public ITextureAtlasAPI targetAtlas;
        public Size2i AtlasSize => targetAtlas.Size;

        public TextureAtlasPosition this[string textureCode] => GetOrCreateTexPos(tmpTextures[textureCode]);
        public readonly Dictionary<string, AssetLocation> tmpTextures = new();

        protected TextureAtlasPosition GetOrCreateTexPos(AssetLocation texturePath)
        {
            ICoreClientAPI capi = Api as ICoreClientAPI;
            IAsset texAsset = capi.Assets.TryGet(texturePath.Clone().WithPathPrefixOnce("textures/blocks/").WithPathAppendixOnce(".png"));
            TextureAtlasPosition texPos = targetAtlas[texturePath];

            if (texPos != null)
            {
                return texPos;
            }

            if (texAsset != null)
            {
                capi.Event.EnqueueMainThreadTask(() => targetAtlas.GetOrInsertTexture(texturePath, out int _, out texPos, () =>texAsset.ToBitmap(capi)), "");
            }

            return texPos ?? capi.BlockTextureAtlas.UnknownTexturePosition;
        }

        internal MeshData GenMesh(ITextureAtlasAPI tart)
        {
            Block block = Api.World.BlockAccessor.GetBlock(Pos);
            if (block.Id == 0) return null;
            MeshData mesh;
            ITesselatorAPI mesman = ((ICoreClientAPI)Api).Tesselator;

            targetAtlas = tart;

            foreach (KeyValuePair<string, CompositeTexture> key in Block.Textures)
            {
                tmpTextures[key.Key] = Block.Textures.FirstOrDefault().Value.Base;
            }

            CompositeShape shape = block.Attributes["shape"].AsObject<CompositeShape>();
            LenUtil.TessellateObj(mesman as ShapeTesselator, shape, out mesh, this["obj"],Api as ICoreClientAPI,"runestory:shapes/blocks/runepylon.obj");
            //mesman.TesselateShape(block, Api.Assets.TryGet("runestory:shapes/blocks/runepylon.json").ToObject<Shape>(), out mesh);

            return mesh;
        }

        public BEBhvPylon(BlockEntity blockentity) : base(blockentity)
        {
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);

            if(api.Side != EnumAppSide.Client) { return; }
            render = new PylonRenderer(api as ICoreClientAPI,Pos,GenMesh((api as ICoreClientAPI).BlockTextureAtlas));

            (api as ICoreClientAPI).Event.RegisterRenderer(render, EnumRenderStage.Opaque, "RunePylon");
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if(Block == null) return false;
            mesher.AddMeshData(mesh(tessThreadTesselator));
            return true;
        }

        public override void OnBlockRemoved()
        {
            if (Api.Side == EnumAppSide.Client)
            {
                mesh((Api as ICoreClientAPI).Tesselator).Dispose();
                render.Dispose();
            }
            base.OnBlockRemoved();
        }

    }
}
