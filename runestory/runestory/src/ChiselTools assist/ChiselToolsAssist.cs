using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.API.Server;
using System.Diagnostics;

using System.Collections;

namespace runestory
{

    /*
     *          Hey! This WHOLE FILE was made by the based dude of QP chisels! Go check out QpTechs mods!
     * 
     *          Thay have graciously given me permission to steal, err, i mean, use these for the altar.
     * 
     *          If its you reading this, thanks a ton! saves me having to make a model for the altar lol!
     *          
     */
    public class BEBChiseledCover : BlockEntityBehavior
    {
        /// <summary>
        /// TODO:
        /// Thank QP chisel man for supplying me this entire class.
        /// </summary>

        protected ItemStack chiseleditemstack;

        public virtual ItemStack ChiseledItemStack => chiseleditemstack;
        protected ICoreClientAPI capi;
        protected ICoreServerAPI sapi;
        protected bool shapeLocked = false;
        public bool ShapeLocked => shapeLocked;

        public BEBChiseledCover(BlockEntity blockentity) : base(blockentity)
        {
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            if (api is ICoreClientAPI)
            {
                capi = api as ICoreClientAPI;
                if (ChiseledItemStack != null) { GenMesh(); }
            }
            else
            {
                sapi = api as ICoreServerAPI;
            }
        }

        public virtual bool SetShape(ItemSlot fromslot, bool dontdropblock = false)
        {
            if (shapeLocked) { return false; }
            meshdata = null;
            if (fromslot == null || fromslot.Empty) { return false; }
            ItemStack shapestack = fromslot.Itemstack;
            if (shapestack.Collectible == null || shapestack.StackSize == 0 || shapestack.Block == null) { return false; }
            if (shapestack.Block.Code.ToString() != "game:chiseledblock") { return false; }
            if (!dontdropblock && chiseleditemstack != null && chiseleditemstack.StackSize > 0 && sapi != null)
            {

                DumpInventory(false);

            }
            chiseleditemstack = shapestack.Clone();
            chiseleditemstack.StackSize = 1;
            dumped = false;
            Blockentity.MarkDirty(true);


            return true;
        }

        protected MeshData meshdata;
        public virtual void GenMesh()
        {


            meshdata = null;

            if (capi == null) { return; }

            if (ChiseledItemStack == null || (ChiseledItemStack.Item == null && ChiseledItemStack.Block == null)) { return; }

            meshdata = CreateModelFromChiseledItemStack(capi, ChiseledItemStack);

            //Blockentity.MarkDirty(true);


        }
        public static MeshData CreateModelFromChiseledItemStack(ICoreClientAPI capi, ItemStack forStack)
        {
            ITreeAttribute tree = forStack.Attributes;
            if (tree == null) tree = new TreeAttribute();
            int[] materials = BlockEntityMicroBlock.MaterialIdsFromAttributes(tree, capi.World);
            uint[] cuboids = (tree["cuboids"] as IntArrayAttribute)?.AsUint;

            // When loaded from json
            if (cuboids == null)
            {
                cuboids = (tree["cuboids"] as LongArrayAttribute)?.AsUint;
            }

            List<uint> voxelCuboids = cuboids == null ? new List<uint>() : new List<uint>(cuboids);

            var firstblock = capi.World.Blocks[materials[0]];
            bool collBoxCuboid = firstblock.Attributes?.IsTrue("chiselShapeFromCollisionBox") == true;
            uint[] originalCuboids = null;
            if (collBoxCuboid)
            {
                Cuboidf[] collboxes = firstblock.CollisionBoxes;
                originalCuboids = new uint[collboxes.Length];

                for (int i = 0; i < collboxes.Length; i++)
                {
                    Cuboidf box = collboxes[i];
                    var uintbox = BlockEntityMicroBlock.ToUint((int)(16 * box.X1), (int)(16 * box.Y1), (int)(16 * box.Z1), (int)(16 * box.X2), (int)(16 * box.Y2), (int)(16 * box.Z2), 0);
                    originalCuboids[i] = uintbox;
                }
            }

            //1.19
            //int[] decorids = new int[Api.World.BlockAccessor.GetDecors(Pos).Count()];
            /*for (int c = 0; c < decorids.Length; c++)
            {
                decorids[c] = Api.World.BlockAccessor.GetDecors(Pos)[c].BlockId;
            }*/
            MeshData mesh = BlockEntityMicroBlock.CreateMesh(capi, voxelCuboids, materials, null);
            //MeshData mesh = BlockEntityMicroBlock.CreateMesh(capi, voxelCuboids, materials);
            mesh.Rgba.Fill((byte)255);
            return mesh;
        }


        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {

            if (meshdata == null) { GenMesh(); }
            if (meshdata == null) { return base.OnTesselation(mesher, tessThreadTesselator); }
            try
            {

                mesher.AddMeshData(meshdata); return true;
            }
            catch { return base.OnTesselation(mesher, tessThreadTesselator); }


        }
        public virtual string GetChiseledName()
        {
            if (chiseleditemstack == null || chiseleditemstack.Attributes == null) { return ""; }
            return chiseleditemstack.Attributes.GetAsString("blockName", "");

        }
        public virtual MeshData GetChiseledMesh()
        {
            if (chiseleditemstack == null) { return null; }
            if (meshdata == null) { GenMesh(); }
            return meshdata;
        }

        public void ToggleShapeLock()
        {
            shapeLocked = !shapeLocked;
            Blockentity.MarkDirty();
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            tree.SetItemstack("itemstack", chiseleditemstack);
            tree.SetBool("shapelocked", shapeLocked);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            shapeLocked = tree.GetBool("shapelocked", false);
            chiseleditemstack = tree.GetItemstack("itemstack");
            if (chiseleditemstack != null)
            {
                chiseleditemstack.ResolveBlockOrItem(worldAccessForResolve);

            }


        }
        protected bool dumped = false;
        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            if (chiseleditemstack != null)
            {
                DumpInventory(false);
                dumped = true;
            }
            base.OnBlockBroken(byPlayer);
        }
        public const int resetmeshpacket = 32322;
        public virtual void DumpInventory(bool triggernewmesh)
        {
            if (chiseleditemstack == null || chiseleditemstack.StackSize == 0 || dumped) { return; }
            //DummyInventory di = new DummyInventory(Api, 1);
            ///di[0].Itemstack = new ItemStack(chiseleditemstack.Block,1);
            //di[0].Itemstack.ResolveBlockOrItem(Api.World);
            //chiseleditemstack = null;
            //di.DropAll(Pos.Offset(BlockFacing.UP).ToVec3d());
            dumped = true;
            if (sapi != null)
            {
                sapi.World.SpawnItemEntity(chiseleditemstack, base.Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }
            chiseleditemstack = null;
            meshdata = null;
            Blockentity.MarkDirty(true);
            byte[] b = new byte[0];
            if (triggernewmesh && sapi != null) { sapi.Network.BroadcastBlockEntityPacket(Pos, resetmeshpacket, b); }
        }
        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            if (packetid == resetmeshpacket) { chiseleditemstack = null; GenMesh(); Blockentity.MarkDirty(true); return; }
            base.OnReceivedServerPacket(packetid, data);
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            if (shapeLocked)
            {
                dsc.AppendLine(GetChiseledName());
                dsc.AppendLine("<font color=#ffff00>Shape Locked in Place!</font> (Ctrl click with wrench to unlock)");
            }
            base.GetBlockInfo(forPlayer, dsc);
        }
        public virtual Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            if (ChiseledItemStack == null) { return null; }
            ITreeAttribute tree = ChiseledItemStack.Attributes;

            if (tree == null) { return null; }
            int[] materials = BlockEntityMicroBlock.MaterialIdsFromAttributes(tree, Api.World);
            uint[] cuboids = (tree["cuboids"] as IntArrayAttribute)?.AsUint;

            // When loaded from json
            if (cuboids == null)
            {
                cuboids = (tree["cuboids"] as LongArrayAttribute)?.AsUint;
            }

            List<uint> voxelCuboids = cuboids == null ? new List<uint>() : new List<uint>(cuboids);

            if (voxelCuboids == null || voxelCuboids.Count == 0) { return null; }
            List<Cuboidf> boxes = new List<Cuboidf>();
            foreach (uint u in cuboids)
            {
                CuboidWithMaterial tocuboid = new CuboidWithMaterial();
                BlockEntityMicroBlock.FromUint(u, tocuboid);
                boxes.Add(tocuboid.ToCuboidf());

            }

            return boxes.ToArray();
        }
        public virtual Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {

            return GetSelectionBoxes(blockAccessor, pos);
        }
    }



    public class BBChiseledCover : BlockBehavior
    {
        public BBChiseledCover(Block block) : base(block)
        {
        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)

        {

            bool result = TryAddCover(world, byPlayer, blockSel);

            if (result) { handling = EnumHandling.Handled; return true; }

            result = DoWrenchClick(world, byPlayer, blockSel);
            if (result) { handling = EnumHandling.Handled; return true; }

            return base.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
        }

        public virtual bool DoWrenchClick(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (byPlayer == null || byPlayer.InventoryManager == null || byPlayer.InventoryManager.ActiveTool == null) return false;
            if (byPlayer.InventoryManager.ActiveTool == EnumTool.Wrench)
            {

                BlockEntity myBE = world.BlockAccessor.GetBlockEntity(blockSel.Position);
                if (myBE == null) return false;
                BEBChiseledCover mycover = myBE.GetBehavior<BEBChiseledCover>();
                if (mycover == null) return false;
                if (byPlayer.Entity.Controls.CtrlKey)
                {
                    mycover.ToggleShapeLock();
                    return true;
                }
                if (mycover.ShapeLocked) { return false; }
                mycover.DumpInventory(true);

                return true;
            }
            return false;
        }

        public virtual bool TryAddCover(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntity myBE = world.BlockAccessor.GetBlockEntity(blockSel.Position);
            if (myBE != null && byPlayer != null)
            {
                BEBChiseledCover mycover = myBE.GetBehavior<BEBChiseledCover>();
                if (mycover != null)
                {
                    //see if the cover system accepts what they are holding
                    bool success = mycover.SetShape(byPlayer.InventoryManager.ActiveHotbarSlot);
                    if (success)
                    {

                        if (byPlayer?.WorldData.CurrentGameMode != EnumGameMode.Creative)
                        {
                            byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.StackSize--;
                            if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.StackSize == 0)
                            {
                                byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack = null;
                            }
                            byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
                        }
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
