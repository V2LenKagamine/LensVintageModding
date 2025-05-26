
using System;
using System.Text;
using LensMiniTweaksModSystem;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace LensMiniTweaks
{
    //This is, unsuprisingly, similar to https://github.com/SpearAndFang/wildfarmingrevival
    //However, I find that version needlessly complex, and thus made a much simpler version.
    public class MushroomSubBlock : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is MushroomSubBE entity)
            {
                return entity.OnPlayerInteract(byPlayer);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
    public class MushroomSubBE : BlockEntity
    {
        Block growing; 
        string growingName;
        static int radius = 3;
        private static readonly Random randy = new();
        public bool OnPlayerInteract(IPlayer player)
        {
            ItemSlot slot = player.InventoryManager.ActiveHotbarSlot;
            var shroomaybe = slot.Itemstack?.Block?.Code.Path.Contains("mushroom");
            if(shroomaybe == null) { return true; }
            if (growing == null && (bool)shroomaybe)
            {
                if (Api.World.Side == EnumAppSide.Client) { return true; }
                growingName = slot.GetStackName();
                growing = slot.Itemstack.Block;
                slot.TakeOut(1);
                slot.MarkDirty();
                MarkDirty();
                return true;
            }
            return false;
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if(api.Side == EnumAppSide.Server)
            {
                RegisterGameTickListener(OnTick,900000); //15 minutes
            }
        }
        public void OnTick(float dt)
        {
            if(growing == null) { return; }
            if (!Lentills.IsChunkAreaLoaded(Pos, Api.World.BlockAccessor, radius)) { return; }
            Grow();
        }

        private void Grow()
        {
            if (growing == null) { return; }
            var side = growing.Variant.ContainsKey("side");
            if(side)
            {
                GrowSide();
            }else
            {
                GrowNormal();
            }
        }
        private void GrowNormal()
        {
           
            var togen = 1;
            var posholder = new BlockPos(Pos.dimension);
            var blockyboi = Api.World.BlockAccessor;
            while(togen-- >0)
            {
                var xpos = radius - randy.Next((2 * radius) + 1);
                var zpos = radius - randy.Next((2 * radius) + 1);
                posholder.Set(Pos.X + xpos, Pos.Y + 1, Pos.Z + zpos);
                var mushroom = blockyboi.GetBlock(posholder);
                var growmaybe = blockyboi.GetBlock(posholder.DownCopy());
                if(growmaybe.Fertility<8 || mushroom.LiquidCode!=null || blockyboi.GetLightLevel(posholder,EnumLightLevelType.MaxLight) > 12) { continue; }
                if(mushroom.Replaceable >=6000 || mushroom.Id == 0)
                {
                    blockyboi.SetBlock(growing.Id, posholder);
                }
            }
        }

        private void GrowSide()
        {
            var posholder = new BlockPos(Pos.dimension);
            var blockyboi = Api.World.BlockAccessor;
            var togen = 1;
            while (togen-- > 0)
            {
                var ymod = 1 + randy.Next(5);
                posholder.Set(Pos.X, Pos.Y + ymod, Pos.Z);
                if(!(blockyboi.GetBlock(posholder) is BlockLog log) || log.Variant["type"] == "resin" || blockyboi.GetLightLevel(posholder,EnumLightLevelType.MaxLight) > 12) { continue; }
                var side = randy.Next(4);
                BlockFacing right = null;
                for (int i = 0; i < 4; i++) 
                {
                    var f = BlockFacing.HORIZONTALS[(i + side) % 4];
                    posholder.Add(f);
                    if (blockyboi.GetBlock(posholder).Id != 0) { continue; }
                    right = f.Opposite;
                    break;
                }
                if(right == null) { continue; }
                var theshroom = blockyboi.GetBlock(growing.CodeWithVariant("side", right.Code));
                blockyboi.SetBlock(theshroom.Id, posholder);
            }
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if(growing == null)
            {
                dsc.AppendLine("Insert a starter culture");
            }
            else
            {
                dsc.AppendLine("Growing " + growingName);
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("hasitem", growing != null);
            if(growing !=null )
            {
                tree.SetInt("growing",growing.Id);
                tree.SetString("thename",growingName);
            }
            
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            var hasitem = tree.GetBool("hasitem");
            if(hasitem)
            {
                var mayshroom = worldAccessForResolve.GetBlock(tree.GetInt("growing"));
                if(mayshroom !=null && mayshroom.Code.Path.Contains("mushroom"))
                {
                    growing = mayshroom;
                    growingName = tree.GetString("thename");
                } else
                {
                    LensMiniTweaksModSystem.logger.Log(EnumLogType.Warning,string.Format("Substrate at {0},{1},{2} had a non-mushroom block/item [ID: {3}] in it! Bad! Setting to null...",Pos.X,Pos.Y,Pos.Z,mayshroom.Id));
                    growing = null;
                    growingName = "ERRORED. Check logs!";
                }
            }
        }
    }
}
