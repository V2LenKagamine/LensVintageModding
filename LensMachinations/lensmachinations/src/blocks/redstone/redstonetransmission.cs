using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace LensstoryMod
{


    public class RedstoneTransmissionBlock : BlockMPBase
    {
        private Dictionary<string, BlockFacing[]> Lazyness => new()
        {
            { "ud",new[] { BlockFacing.UP,BlockFacing.DOWN} },
            { "we",new[] { BlockFacing.WEST,BlockFacing.EAST} },
            { "ns",new[] { BlockFacing.NORTH,BlockFacing.SOUTH} }
        };
        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            bool yes = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            BlockFacing[] bfs;
            Lazyness.TryGetValue(LastCodePart(), out bfs);
            if (bfs != null)
            {
                if (yes)
                {
                    if (!tryConnect(world, byPlayer, blockSel.Position, bfs[0]))
                    {
                        tryConnect(world, byPlayer, blockSel.Position, bfs[1]);
                    }
                }
            }
            return yes;
        }
        public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {
        }

        public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {
            BlockFacing[] bfs;
            Lazyness.TryGetValue(LastCodePart(), out bfs);
            if(bfs == null) { return false; }
            return face == bfs[0] || face == bfs[1];
        }

        public override MechanicalNetwork GetNetwork(IWorldAccessor world, BlockPos pos)
        {
            RedstoneTransmissionBhv be = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<RedstoneTransmissionBhv>();
            if (be == null || !be.engaged) return null;
            return be.Network;
        }

    }

    public class RedstoneTransmissionBhv : BEBehaviorMPTransmission, IRedstoneTaker
    {
        BlockFacing[] orients = new BlockFacing[2];
        string orientations;
        private Dictionary<string, BlockFacing[]> Lazyness => new()
        {
            { "ud",new[] { BlockFacing.UP,BlockFacing.DOWN} },
            { "we",new[] { BlockFacing.WEST,BlockFacing.EAST} },
            { "ns",new[] { BlockFacing.NORTH,BlockFacing.SOUTH} }
        };
        public RedstoneTransmissionBhv(BlockEntity blockentity) : base(blockentity)
        {
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            orientations = Block.Variant["orientation"];
            switch (orientations)
            {
                case "ns":
                    AxisSign = new int[] { 0, 0, -1 };
                    orients[0] = BlockFacing.NORTH;
                    orients[1] = BlockFacing.SOUTH;
                    break;

                case "we":
                    AxisSign = new int[] { 1, 0, 0 };
                    orients[0] = BlockFacing.EAST;
                    orients[1] = BlockFacing.WEST;
                    break;

                case "ud":
                    AxisSign = new int[] { 0, 1, 0 };
                    orients[0] = BlockFacing.UP;
                    orients[1] = BlockFacing.DOWN;
                    break;
            }
            base.Initialize(api, properties);
            orientations = Block.Variant["orientation"];
            switch (orientations)
            {
                case "ns":
                    AxisSign = new int[] { 0, 0, -1 };
                    orients[0] = BlockFacing.NORTH;
                    orients[1] = BlockFacing.SOUTH;
                    break;

                case "we":
                    AxisSign = new int[] { 1, 0, 0 };
                    orients[0] = BlockFacing.EAST;
                    orients[1] = BlockFacing.WEST;
                    break;

                case "ud":
                    AxisSign = new int[] { 0, 1, 0 };
                    orients[0] = BlockFacing.UP;
                    orients[1] = BlockFacing.DOWN;
                    break;
            }
            if(engaged)
            {
                ChangeState(true);
            }
        }

        protected void ChangeState(bool newEngaged)
        {
            if (newEngaged)
            {
                CreateJoinAndDiscoverNetwork(orients[0]);
                CreateJoinAndDiscoverNetwork(orients[1]);
                tryConnect(orients[0]);
                Blockentity.MarkDirty(true);
            }
            else
            {
                if(network != null)
                {
                    manager.RebuildNetwork(network, this);
                }
            }
        }

        public override float GetResistance()
        {
            return 0f;
        }

        public void OnSignal(bool Activated)
        {
            if(Activated != engaged)
            {
                engaged = !engaged;
                ChangeState(engaged);
            }
        }
    }

}