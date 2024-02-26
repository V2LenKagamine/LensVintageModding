using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;

namespace LensstoryMod
{
    public class KineticpotentianatorBlock : BlockMPBase
    {
        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            bool yes = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);

            if (yes)
            {
                if (!tryConnect(world, byPlayer, blockSel.Position, BlockFacing.UP))
                {
                    tryConnect(world, byPlayer, blockSel.Position, BlockFacing.DOWN);
                }
            }
            return yes;
        }
        public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {
        }

        public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {
            return face == BlockFacing.UP || face == BlockFacing.DOWN;
        }
    }
    public class KineticpotentianatorBe : BlockEntity
    {
        public bool powered;
        public KineticpotentianatorBhv mpc;
        KineticMPRenderer renderer;

        MeshData baseMesh
        {
            get
            {
                object value;
                Api.ObjectCache.TryGetValue(Block.FirstCodePart() + "basemesh-mpgen",out value);
                return (MeshData)value;
            }
            set
            {
                Api.ObjectCache[Block.FirstCodePart() + "basemesh-mpgen"] = value;
            }
        }

        MeshData orbMesh
        {
            get
            {
                object value;
                Api.ObjectCache.TryGetValue(Block.FirstCodePart() + "orbmesh-mpgen",out value);
                return (MeshData)value;
            }
            set
            {
                Api.ObjectCache[Block.FirstCodePart() + "orbmesh-mpgen"] = value;
            }
        }

        internal MeshData GenMesh(string type = "base")
        {
            Block block = Api.World.BlockAccessor.GetBlock(Pos);
            if (block.Id == 0) return null;
            MeshData mesh;
            ITesselatorAPI meshman = ((ICoreClientAPI)Api).Tesselator;

            meshman.TesselateShape(block, Api.Assets.TryGet("lensmachinations:shapes/block/machines/kineticmpgen-" + type + ".json").ToObject<Shape>(), out mesh);

            return mesh;
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            if(api.Side == EnumAppSide.Client)
            {
                renderer = new KineticMPRenderer(api as ICoreClientAPI, Pos, GenMesh("orb"));
                renderer.mechBhv = mpc;
                renderer.ShouldRender = false;
                if(powered)
                {
                    renderer.ShouldRender = true;
                    renderer.Rotate = true;
                }


                (api as ICoreClientAPI).Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, Block.FirstCodePart());

                if(baseMesh == null)
                {
                    baseMesh = GenMesh("base");
                }
                if(orbMesh == null)
                {
                    orbMesh = GenMesh("orb");
                }
            }
            
            GetBehavior<Mana>().begin();
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if(Block == null) { return false; }
            mesher.AddMeshData(baseMesh);
            /*
            if(powered)
            {
                mesher.AddMeshData(orbMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, renderer.AngleRad, 0).Translate(0, 0, 0));
            }
            */
            return true;
        }

        public override void CreateBehaviors(Block block, IWorldAccessor worldForResolve)
        {
            base.CreateBehaviors(block, worldForResolve);

            mpc = GetBehavior<KineticpotentianatorBhv>();
            if(mpc!= null)
            {
                mpc.OnConnected = () => {
                    powered = true;
                    if(renderer!=null)
                    {
                        renderer.ShouldRender = true;
                        renderer.Rotate = true;
                    }
                    MarkDirty();
                };   

                mpc.OnDisconnected = () => {
                    powered = false;
                    if (renderer != null)
                    {
                        renderer.ShouldRender = false;
                        renderer.Rotate = false;
                    }
                    MarkDirty();
                };
            }
        }
    }

    public class KineticpotentianatorBhv : BEBehaviorMPConsumer, IManaMaker
    {
        public KineticpotentianatorBhv(BlockEntity blockentity) : base(blockentity)
        {
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            Shape = Blockentity.Block.Shape;
        }
        public int MakeMana()
        {
            if(Blockentity is KineticpotentianatorBe correct)
            {
                if (correct.powered && correct.mpc != null)
                {
                    return (int)Math.Floor(correct.mpc.TrueSpeed/0.15f);
                }
            }
            return 0;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);

            dsc.AppendLine("MP:")
                .AppendLine("Producing: " + MakeMana());
        }
        public override float GetResistance()
        {
            return 0.12f;
        }
    }
}
