using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace runestory.src.block.pylons
{
    public class BEBhvPylon : BlockEntityBehavior
    {

        PylonRenderer render;

        MeshData mesh
        {
            get
            {
                object val;
                Api.ObjectCache.TryGetValue("pylon-basemesh", out val);
                return (MeshData)val;
            }
            set
            {
                Api.ObjectCache["pylon-basemesh"] = value;
            }
        }

        internal MeshData GenMesh()
        {
            Block block = Api.World.BlockAccessor.GetBlock(Pos);
            if (block.Id == 0) return null;
            MeshData mesh;
            ITesselatorAPI mesman = ((ICoreClientAPI)Api).Tesselator;

            mesman.TesselateShape(block, Api.Assets.TryGet("runestory:shapes/blocks/runepylon.json").ToObject<Shape>(), out mesh);

            return mesh;
        }

        public BEBhvPylon(BlockEntity blockentity) : base(blockentity)
        {
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);

            if(api.Side != EnumAppSide.Client) { return; }
            render = new PylonRenderer(api as ICoreClientAPI,Pos,GenMesh());

            (api as ICoreClientAPI).Event.RegisterRenderer(render, EnumRenderStage.Opaque, "RunePylon");

            if(mesh is null)
            {
                mesh = GenMesh();
            }
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if(Block == null) return false;
            mesher.AddMeshData(mesh);
            return true;
        }

        public override void OnBlockRemoved()
        {
            if (Api.Side == EnumAppSide.Client)
            {
                mesh.Dispose();
                render.Dispose();
            }
            base.OnBlockRemoved();
        }

    }
}
