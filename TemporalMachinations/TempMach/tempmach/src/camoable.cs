using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace TempMach
{
    public class CamoableBE : BlockEntity
    {
        public ItemStack? copy;
        MeshData CurrentMesh;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (copy != null) { copy.ResolveBlockOrItem(api.World); }
            if (CurrentMesh == null && api.Side == EnumAppSide.Client)
            {
                CurrentMesh = GenMesh();
            }
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if (Block == null) { return false; }
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
            IDictionary<string, CompositeTexture> textures;
            if (copy != null)
            {
                textures = copy.Block.Textures;
            }
            else
            {
                textures = Block.Textures;
            }
            var jank = new ShapeTextureSource(Api as ICoreClientAPI, shape, "Len Camo Jank");
            foreach (var val in textures)
            {
                var ctex = val.Value.Clone();
                ctex.Bake((Api as ICoreClientAPI).Assets);
                jank.textures[val.Key] = ctex;
            }

            meshman.TesselateShape("Lens Camo Jank", shape, out mesh, jank);
            return mesh;
        }

        public bool TryCamo(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
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

        public void InvalidateMesh()
        {
            if (Api.World.Side == EnumAppSide.Client)
            {
                CurrentMesh = GenMesh();
                MarkDirty(true);
            }
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetItemstack("clone", copy);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            var temp = tree.GetItemstack("clone");
            temp?.ResolveBlockOrItem(worldAccessForResolve);
            copy = temp;
        }

    }
}
