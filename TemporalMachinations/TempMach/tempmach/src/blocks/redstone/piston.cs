using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace TempMach
{
    public class PistonBlock : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is PistonBE entity && (byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack?.Block != null || byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack == null))
            {
                return entity.TryCamo(world, byPlayer, blockSel);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
    public class PistonBE : CamoableBE
    {
        public bool toggle = false;
        public BlockPos head;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            GetBehavior<Redstone>().begin(true);
        }

        public bool TryPush()
        {
            var Looking = Block.Variant["rot"] is string v ? string.Intern(v) : null;
            Vec3i offset = null;
            switch(Looking)
            {
                case "up":
                    {
                        offset = new(0, 1, 0);
                        break;
                    }
                case "down":
                    {
                        offset = new(0, -1, 0);
                        break;
                    }
                case "north":
                    {
                        offset = new(0, 0, -1);
                        break;
                    }
                case "east":
                    {
                        offset = new(1, 0, 0);
                        break;
                    }
                case "south":
                    {
                        offset = new(0, 0, 1);
                        break;
                    }
                case "west":
                    {
                        offset = new(-1, 0, 0);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
            if(offset == null) { return false; }

            List<int> blockcodes = [];
            blockcodes.Insert(0,Api.World.GetBlock("tempmach:temppistonhead-"+Block.LastCodePart()).Id);
            for (int i = 1;i<=12;i++)
            {
                if (Api.World.BlockAccessor.GetBlockEntity(Pos.AddCopy(offset * i)) is not null) { return false; }
                var theblocc = Api.World.BlockAccessor.GetBlock(Pos.AddCopy(offset * i)).Id;
                if(theblocc == 0) { break; }
                blockcodes.Insert(i,theblocc);
                if(i==12)
                {
                    if(Api.World.BlockAccessor.GetBlock(Pos.AddCopy(offset*13)).Id != 0 ) { return false; }
                }
            }
            DoPush(blockcodes,offset);
            return true;
        }

        public void DoPush(List<int> codesinorder, Vec3i offset)
        {
            for (int i = 0; i < codesinorder.Count; i++)
            {
                Vec3i dopos = offset * (i + 1);
                Api.World.BlockAccessor.SetBlock(Api.World.GetBlock(codesinorder[i]).Id, Pos.AddCopy(dopos));
                Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(Pos.AddCopy(dopos));
                if (i == codesinorder.Count - 1) {
                    Entity[] inway = Api.World.GetEntitiesAround(Pos.AddCopy(dopos).ToVec3d(), 1, 1);
                    foreach (Entity pushed in inway)
                    {
                        pushed.Pos.Add(offset.ToBlockPos().ToVec3f());
                    }
                }
            }
            if(Api.World.BlockAccessor.GetBlockEntity(Pos.AddCopy(offset)) is PistonHeadBE boi)
            {
                boi.pisston = Pos.Copy();
                if(copy !=null)
                {
                    boi.copy = copy;
                    boi.InvalidateMesh();
                    MarkDirty(true);
                }
                head = Pos.AddCopy(offset);
                Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(Pos.AddCopy(offset));
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("wason",toggle);
            tree.SetVec3i("thehead", head?.AsVec3i ?? Vec3i.Zero);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            toggle = tree.GetBool("wason");
            Vec3i temp = tree.GetVec3i("thehead");
            if (temp != Vec3i.Zero)
            {
                head = temp.AsBlockPos;
            }
        }
    }
    public class PistonBhv : BlockEntityBehavior, IRedstoneTaker
    {
        public PistonBhv(BlockEntity blockentity) : base(blockentity)
        {
        }

        public void OnSignal(bool Activated)
        {
            PistonBE bones = (Blockentity as PistonBE);
            if ( bones?.toggle == false && Activated)
            {
                bones.toggle = true;
                bones.TryPush();
            }
            if(!Activated && bones?.toggle == true) 
            { 
                bones.toggle = false;
                if(bones.head != null && Api.World.BlockAccessor.GetBlockEntity(bones.head) is PistonHeadBE)
                {
                    Api.World.BlockAccessor.SetBlock(0, bones.head);
                    Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(bones.head);

                }
            }
        }
    }

    public class PistonHead : Block
    {
        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (api.World.BlockAccessor.GetBlockEntity(pos) is PistonHeadBE buh) {
                buh.BreakPiston(byPlayer,dropQuantityMultiplier);
            }
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }
    }
    public class PistonHeadBE : CamoableBE
    {
        public BlockPos pisston;
        public void BreakPiston(IPlayer byPlayer,float dqm)
        {
            if (pisston != null)
            {
                Api.World.BlockAccessor.BreakBlock(pisston, byPlayer, dqm);
                Api.World.SpawnItemEntity(new(Api.World.GetBlock("tempmach:temppiston-up")),Pos);
            }
        }
    }
}
