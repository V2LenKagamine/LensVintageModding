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

namespace LensstoryMod
{

    public class SwapperBlock : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is SwapperBE entity)
            {
                return entity.OnInteract(world, byPlayer, blockSel);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
    public class SwapperBE : BlockEntity
    {
        public bool toggle = false;

        public ItemStack? copy;
        MeshData CurrentMesh;

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if(Block == null) { return false; }
            mesher.AddMeshData(CurrentMesh);
            base.OnTesselation(mesher, tessThreadTesselator);
            return true;
        }

        internal MeshData GenMesh()
        {
            Block block = Api.World.BlockAccessor.GetBlock(Pos);
            if (block.Id == 0) return null;
            MeshData mesh;
            ITesselatorAPI meshman = ((ICoreClientAPI)Api).Tesselator;

            var shape = Api.Assets.TryGet("game:shapes/block/basic/cube.json").ToObject<Shape>();
            IDictionary<string,CompositeTexture> textures;
            if(copy != null)
            {
                textures = copy.Block.Textures;
            }
            else
            {
                textures = Block.Textures;
            }
            var jank = new ShapeTextureSource(Api as ICoreClientAPI, shape, "Len Swapper Jank");
            foreach(var val in textures)
            {
                var ctex = val.Value.Clone();
                ctex.Bake((Api as ICoreClientAPI).Assets);
                jank.textures[val.Key] = ctex;
            }

            meshman.TesselateShape("Lens Swapper Jank",shape, out mesh, jank);
            return mesh;
        }
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (copy != null) { copy.ResolveBlockOrItem(api.World); }
            if (CurrentMesh == null && api.Side == EnumAppSide.Client)
            {
                CurrentMesh = GenMesh();
            }
            GetBehavior<Redstone>().begin(true);
        }

        public bool OnInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            var selslot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (selslot?.Itemstack?.Block != null)
            {
                copy = selslot.Itemstack.Clone();
                if (world.Side == EnumAppSide.Client)
                {
                    CurrentMesh = GenMesh();
                }
                MarkDirty(true);
            }
            else if (byPlayer.Entity.Controls.ShiftKey && selslot.Itemstack == null)
            {
                copy = null;
                if (world.Side == EnumAppSide.Client)
                {
                    CurrentMesh = GenMesh();

                }
                MarkDirty(true);
            }
            return true;
        }

        public void SwapBlocks(bool activate)
        {
            if (activate != toggle) { return; }
            if (Block.FirstCodePart() == "blockswapper")
            {
                var Ori = Block.Variant["orientation"] is string v ? string.Intern(v) : null;
                switch (Ori)
                {
                    case "ud":
                        {
                            DoTheSwap(Pos.UpCopy(), Pos.DownCopy());
                            break;
                        }
                    case "we":
                        {
                            DoTheSwap(Pos.EastCopy(), Pos.WestCopy());
                            break;
                        }
                    case "ns":
                        {
                            DoTheSwap(Pos.NorthCopy(), Pos.SouthCopy());
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            } else
            {
                var Ori = Block.Variant["orientation"] is string v ? string.Intern(v) : null;
                var side = Block.Variant["side"] is string x ? string.Intern(x) : null;
                switch (Ori) //NIGHTMARE NIGHTMARE NIGHTMARE
                {
                    case "north":
                        {
                            switch (side)
                            {
                                case "east":
                                    {
                                        DoTheSwap(Pos.EastCopy(), Pos.NorthCopy());
                                        break;
                                    }
                                case "west":
                                    {
                                        DoTheSwap(Pos.WestCopy(), Pos.NorthCopy());
                                        break;
                                    }
                                case "up":
                                    {
                                        DoTheSwap(Pos.UpCopy(), Pos.NorthCopy());
                                        break;
                                    }
                                case "down":
                                    {
                                        DoTheSwap(Pos.DownCopy(), Pos.NorthCopy());
                                        break;
                                    }
                            }
                            break;

                        }
                    case "east":
                        {
                            switch (side)
                            {
                                case "south":
                                    {
                                        DoTheSwap(Pos.SouthCopy(), Pos.EastCopy());
                                        break;
                                    }
                                case "north":
                                    {
                                        DoTheSwap(Pos.EastCopy(), Pos.NorthCopy());
                                        break;
                                    }
                                case "up":
                                    {
                                        DoTheSwap(Pos.UpCopy(), Pos.EastCopy());
                                        break;
                                    }
                                case "down":
                                    {
                                        DoTheSwap(Pos.DownCopy(), Pos.EastCopy());
                                        break;
                                    }
                            }
                            break;
                        }
                    case "south":
                        {
                            switch (side)
                            {
                                case "east":
                                    {
                                        DoTheSwap(Pos.EastCopy(), Pos.SouthCopy());
                                        break;
                                    }
                                case "west":
                                    {
                                        DoTheSwap(Pos.WestCopy(), Pos.SouthCopy());
                                        break;
                                    }
                                case "up":
                                    {
                                        DoTheSwap(Pos.UpCopy(), Pos.SouthCopy());
                                        break;
                                    }
                                case "down":
                                    {
                                        DoTheSwap(Pos.DownCopy(), Pos.SouthCopy());
                                        break;
                                    }
                            }
                            break;
                        }
                    case "west":
                        {
                            switch (side)
                            {
                                case "south":
                                    {
                                        DoTheSwap(Pos.WestCopy(), Pos.SouthCopy());
                                        break;
                                    }
                                case "north":
                                    {
                                        DoTheSwap(Pos.WestCopy(), Pos.NorthCopy());
                                        break;
                                    }
                                case "up":
                                    {
                                        DoTheSwap(Pos.UpCopy(), Pos.WestCopy());
                                        break;
                                    }
                                case "down":
                                    {
                                        DoTheSwap(Pos.DownCopy(), Pos.WestCopy());
                                        break;
                                    }
                            }
                            break;
                        }
                    case "up":
                        {
                            switch (side)
                            {
                                case "north":
                                    {
                                        DoTheSwap(Pos.UpCopy(), Pos.NorthCopy());
                                        break;
                                    }
                                case "east":
                                    {
                                        DoTheSwap(Pos.NorthCopy(), Pos.UpCopy());
                                        break;
                                    }
                                case "south":
                                    {
                                        DoTheSwap(Pos.UpCopy(), Pos.SouthCopy());
                                        break;
                                    }
                                case "west":
                                    {
                                        DoTheSwap(Pos.UpCopy(), Pos.WestCopy());
                                        break;
                                    }
                            }
                            break;
                        }
                    case "down":
                        {
                            switch (side)
                            {
                                case "north":
                                    {
                                        DoTheSwap(Pos.DownCopy(), Pos.NorthCopy());
                                        break;
                                    }
                                case "east":
                                    {
                                        DoTheSwap(Pos.NorthCopy(), Pos.DownCopy());
                                        break;
                                    }
                                case "south":
                                    {
                                        DoTheSwap(Pos.DownCopy(), Pos.SouthCopy());
                                        break;
                                    }
                                case "west":
                                    {
                                        DoTheSwap(Pos.DownCopy(), Pos.WestCopy());
                                        break;
                                    }
                            }
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
        }
        public void DoTheSwap(BlockPos one, BlockPos two)
        {
            if (Api.World.BlockAccessor.GetBlockEntity(one) != null || Api.World.BlockAccessor.GetBlockEntity(two) != null)
            { return; }

            Block alpha = Api.World.BlockAccessor.GetBlock(one);
            Block omega = Api.World.BlockAccessor.GetBlock(two);

            Api.World.BlockAccessor.SetBlock(alpha.BlockId, two);
            Api.World.BlockAccessor.SetBlock(omega.BlockId, one);

            toggle = !toggle;
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("wason", toggle);
            tree.SetItemstack("clone", copy);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            toggle = tree.GetBool("wason");
            var temp = tree.GetItemstack("clone");
            temp?.ResolveBlockOrItem(worldAccessForResolve);
            copy = temp;
        }
    }
    public class SwapperBhv : BlockEntityBehavior, IRedstoneTaker
    {
        public SwapperBhv(BlockEntity block) : base(block)
        {
        }

        public void OnSignal(bool Activated)
        {
            if (Api.Side == EnumAppSide.Client) { return; }
            if(Blockentity is SwapperBE swapper)
            {
                swapper.SwapBlocks(Activated);
            }
        }
    }

    //Diag Variant
    public class SwapperDiagBE : BlockEntity
    {
        public bool toggle = false;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            GetBehavior<Redstone>().begin(true);
        }
        public void SwapBlocks(bool activate)
        {
            if (activate != toggle) { return; }
            var Ori = Block.Variant["orientation"] is string v ? string.Intern(v) : null;
            var side = Block.Variant["side"] is string x ? string.Intern(x) : null;
            switch (Ori) //NIGHTMARE NIGHTMARE NIGHTMARE
            {
                case "north":
                    {
                        switch (side)
                        {
                            case "east": 
                                {
                                    DoTheSwap(Pos.EastCopy(), Pos.NorthCopy());
                                    break;
                                }
                            case "west":
                                {
                                    DoTheSwap(Pos.WestCopy(), Pos.NorthCopy());
                                    break;
                                }
                            case "up":
                                {
                                    DoTheSwap(Pos.UpCopy(), Pos.NorthCopy());
                                    break;
                                }
                            case "down":
                                {
                                    DoTheSwap(Pos.DownCopy(), Pos.NorthCopy());
                                    break;
                                }
                        }
                        break;
                        
                    }
                case "east":
                    {
                        switch (side)
                        {
                            case "south":
                                {
                                    DoTheSwap(Pos.SouthCopy(), Pos.EastCopy());
                                    break;
                                }
                            case "north":
                                {
                                    DoTheSwap(Pos.EastCopy(), Pos.NorthCopy());
                                    break;
                                }
                            case "up":
                                {
                                    DoTheSwap(Pos.UpCopy(), Pos.EastCopy());
                                    break;
                                }
                            case "down":
                                {
                                    DoTheSwap(Pos.DownCopy(), Pos.EastCopy());
                                    break;
                                }
                        }
                        break;
                    }
                case "south":
                    {
                        switch (side)
                        {
                            case "east":
                                {
                                    DoTheSwap(Pos.EastCopy(), Pos.SouthCopy());
                                    break;
                                }
                            case "west":
                                {
                                    DoTheSwap(Pos.WestCopy(), Pos.SouthCopy());
                                    break;
                                }
                            case "up":
                                {
                                    DoTheSwap(Pos.UpCopy(), Pos.SouthCopy());
                                    break;
                                }
                            case "down":
                                {
                                    DoTheSwap(Pos.DownCopy(), Pos.SouthCopy());
                                    break;
                                }
                        }
                        break;
                    }
                case "west":
                    {
                        switch (side)
                        {
                            case "south":
                                {
                                    DoTheSwap(Pos.WestCopy(), Pos.SouthCopy());
                                    break;
                                }
                            case "north":
                                {
                                    DoTheSwap(Pos.WestCopy(), Pos.NorthCopy());
                                    break;
                                }
                            case "up":
                                {
                                    DoTheSwap(Pos.UpCopy(), Pos.WestCopy());
                                    break;
                                }
                            case "down":
                                {
                                    DoTheSwap(Pos.DownCopy(), Pos.WestCopy());
                                    break;
                                }
                        }
                        break;
                    }
                case "up":
                    {
                        switch (side)
                        {
                            case "north":
                                {
                                    DoTheSwap(Pos.UpCopy(), Pos.NorthCopy());
                                    break;
                                }
                            case "east":
                                {
                                    DoTheSwap(Pos.NorthCopy(), Pos.UpCopy());
                                    break;
                                }
                            case "south":
                                {
                                    DoTheSwap(Pos.UpCopy(), Pos.SouthCopy());
                                    break;
                                }
                            case "west":
                                {
                                    DoTheSwap(Pos.UpCopy(), Pos.WestCopy());
                                    break;
                                }
                        }
                        break;
                    }
                case "down": {
                        switch (side)
                        {
                            case "north":
                                {
                                    DoTheSwap(Pos.DownCopy(), Pos.NorthCopy());
                                    break;
                                }
                            case "east":
                                {
                                    DoTheSwap(Pos.NorthCopy(), Pos.DownCopy());
                                    break;
                                }
                            case "south":
                                {
                                    DoTheSwap(Pos.DownCopy(), Pos.SouthCopy());
                                    break;
                                }
                            case "west":
                                {
                                    DoTheSwap(Pos.DownCopy(), Pos.WestCopy());
                                    break;
                                }
                        }
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }
        public void DoTheSwap(BlockPos one, BlockPos two)
        {
            if (Api.World.BlockAccessor.GetBlockEntity(one) != null || Api.World.BlockAccessor.GetBlockEntity(two) != null)
            { return; }

            Block alpha = Api.World.BlockAccessor.GetBlock(one);
            Block omega = Api.World.BlockAccessor.GetBlock(two);

            Api.World.BlockAccessor.SetBlock(alpha.BlockId, two);
            Api.World.BlockAccessor.SetBlock(omega.BlockId, one);

            toggle = !toggle;
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("wason", toggle);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            toggle = tree.GetBool("wason");
        }
    }
    public class SwapperDiagBhv : BlockEntityBehavior, IRedstoneTaker
    {
        public SwapperDiagBhv(BlockEntity block) : base(block)
        {
        }

        public void OnSignal(bool Activated)
        {
            if (Blockentity is SwapperDiagBE swapper && Api.Side == EnumAppSide.Server)
            {
                swapper.SwapBlocks(Activated);
            }
        }
    }
}
