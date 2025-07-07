using System;
using System.Collections;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace LensGemology
{
    class OreCrystalsCrystal : Block
    {
        private const int CRYSTAL_DURABILITY_DAMAGE = 2;

        WorldInteraction[] interactions = null;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            if (FirstCodePart() == "orecrystals_crystal_poor" || FirstCodePart() == "decocrystal") return;

            if (this.FirstCodePart() == "seed_crystals")
                interactions = ObjectCacheUtil.GetOrCreate(api, "crystalSeedsInteractions", () =>
                {
                    return new WorldInteraction[] {
                        new WorldInteraction()
                        {
                            ActionLangCode = "orecrystals:blockhelp-crystal-seed-take",
                            MouseButton = EnumMouseButton.Left
                        }
                    };
                });
            else
                interactions = ObjectCacheUtil.GetOrCreate(api, "crystalBlockInteractions", () =>
                {
                    List<ItemStack> chiselStackList = new List<ItemStack>();
                    List<ItemStack> pickaxeStackList = new List<ItemStack>();

                    foreach (Item item in api.World.Items)
                    {
                        if (item.Code == null) continue;

                        if (item.Tool == EnumTool.Chisel)
                        {
                            chiselStackList.Add(new ItemStack(item));
                        }
                        else if(item.Tool == EnumTool.Pickaxe)
                        {
                            pickaxeStackList.Add(new ItemStack(item));
                        }
                    }
                    return new WorldInteraction[] {
                        new WorldInteraction()
                        {
                            ActionLangCode = "orecrystals:blockhelp-crystal-harvest",
                            MouseButton = EnumMouseButton.Left,
                            Itemstacks = chiselStackList.ToArray()
                        },
                        new WorldInteraction()
                        {
                            ActionLangCode = "orecrystals:blockhelp-crystal-break",
                            MouseButton = EnumMouseButton.Left,
                            Itemstacks = pickaxeStackList.ToArray()
                        }
                    };
                });
        }

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            string codeLastPart = LastCodePart(0);

            switch (codeLastPart)
            {
                case "ore_up":
                    if (world.BlockAccessor.GetBlockId(pos.UpCopy()) == 0)
                    {
                        world.BlockAccessor.BreakBlock(pos, null, 0);
                    }
                    break;
                case "ore_down":
                    if (world.BlockAccessor.GetBlockId(pos.DownCopy()) == 0)
                    {
                        world.BlockAccessor.BreakBlock(pos, null, 0);
                    }
                    break;
                case "ore_north":
                    if (world.BlockAccessor.GetBlockId(pos.NorthCopy()) == 0)
                    {
                        world.BlockAccessor.BreakBlock(pos, null, 0);
                    }
                    break;
                case "ore_south":
                    if (world.BlockAccessor.GetBlockId(pos.SouthCopy()) == 0)
                    {
                        world.BlockAccessor.BreakBlock(pos, null, 0);
                    }
                    break;
                case "ore_east":
                    if (world.BlockAccessor.GetBlockId(pos.EastCopy()) == 0)
                    {
                        world.BlockAccessor.BreakBlock(pos, null, 0);
                    }
                    break;
                case "ore_west":
                    if (world.BlockAccessor.GetBlockId(pos.WestCopy()) == 0)
                    {
                        world.BlockAccessor.BreakBlock(pos, null, 0);
                    }
                    break;
                default:
                    break;
            }
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            string[] blockCode = Code.Path.Split('-');
            string newCode;

            if (blockSel.Face == BlockFacing.UP)
            {
                blockCode[blockCode.Length - 1] = "ore_down";
            }
            else if (blockSel.Face == BlockFacing.DOWN)
            {
                blockCode[blockCode.Length - 1] = "ore_up";
            }
            else if (blockSel.Face == BlockFacing.NORTH)
            {
                blockCode[blockCode.Length - 1] = "ore_south";
            }
            else if (blockSel.Face == BlockFacing.SOUTH)
            {
                blockCode[blockCode.Length - 1] = "ore_north";
            }
            else if (blockSel.Face == BlockFacing.EAST)
            {
                blockCode[blockCode.Length - 1] = "ore_west";
            }
            else if (blockSel.Face == BlockFacing.WEST)
            {
                blockCode[blockCode.Length - 1] = "ore_east";
            }

            newCode = string.Join("-", blockCode);

            int id = world.GetBlock(new AssetLocation("lensgemology",newCode)).Id;

            try
            {
                if (world.BlockAccessor.GetBlock(blockSel.Position).IsReplacableBy(this))
                {
                    world.BlockAccessor.SetBlock(id, blockSel.Position);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (byPlayer != null)
            {
                ItemStack[] drops = GetDrops(world, pos, byPlayer, dropQuantityMultiplier);

                if (FirstCodePart() != "seed_crystals")
                {
                    if (byPlayer.InventoryManager.ActiveTool == EnumTool.Chisel)
                    {
                        Block harvestedCrystal = world.GetBlock(new AssetLocation("lensgemology", "orecrystals_crystal_poor-" + FirstCodePart(1) + "-" + LastCodePart()));

                        if (harvestedCrystal.Id != world.BlockAccessor.GetBlockId(pos.Copy()))
                            world.BlockAccessor.SetBlock(harvestedCrystal.Id, pos);
                        else
                            world.BlockAccessor.SetBlock(0, pos);

                        if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
                            byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Collectible.DamageItem(world, byPlayer.Entity, byPlayer.InventoryManager.ActiveHotbarSlot, CRYSTAL_DURABILITY_DAMAGE);
                    }
                    else if (byPlayer.InventoryManager.ActiveTool == EnumTool.Pickaxe)
                    {
                        world.BlockAccessor.SetBlock(0, pos);

                        if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
                        {
                            if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Collectible.Durability - CRYSTAL_DURABILITY_DAMAGE > 0)
                                byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Collectible.DamageItem(world, byPlayer.Entity, byPlayer.InventoryManager.ActiveHotbarSlot, CRYSTAL_DURABILITY_DAMAGE - 1);
                            else
                                byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Collectible.Durability = 1;
                        }
                    }
                    else
                    {
                        world.BlockAccessor.SetBlock(0, pos);
                    }
                }
                else if(FirstCodePart() == "seed_crystals")
                {
                    world.BlockAccessor.SetBlock(0, pos);
                    drops = new ItemStack[1];

                    drops[0] = new ItemStack(this);
                }
                ItemStack[] real = drops;
                switch (FirstCodePart())
                {
                    case "orecrystals_crystal_poor":
                        {
                            real = drops.Append(FindItemDrops(2));
                            break;
                        }
                    case "orecrystals_crystal_medium":
                        {
                            real = drops.Append(FindItemDrops(4));
                            break;
                        }
                    case "orecrystals_crystal_rich":
                        {
                            real = drops.Append(FindItemDrops(6));
                            break;
                        }
                    case "orecrystals_crystal_bountiful":
                        {
                            real = drops.Append(FindItemDrops(10));
                            break;
                        }
                }


                if (real != null)
                {
                    for(int i = 0; i < real.Length; i++)
                    {
                        if (SplitDropStacks)
                        {
                            for (int k = 0; k < real[i].StackSize; k++)
                            {
                                ItemStack stack = real[i].Clone();
                                stack.StackSize = 1;
                                world.SpawnItemEntity(stack, new Vec3d(pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5), null);
                            }
                        }
                    }
                }
            }
            else
            {
                world.BlockAccessor.SetBlock(0, pos);
            }

            if (api.Side == EnumAppSide.Server)
                world.PlaySoundAt(Sounds.GetBreakSound(byPlayer), pos.X, pos.Y, pos.Z, byPlayer, true, 32, 1);
            else
                BreakParticles(pos);
        }
        private ItemStack[] FindItemDrops(int amount)
        {
            Random Randy = new Random();
            ItemStack[] returned = new ItemStack[1] { new ItemStack(api.World.GetItem(new AssetLocation("lensgemology:orecrystals_crystalshard")))};
            var attempt = api.World.GetItem(new AssetLocation("game:nugget-" + FirstCodePart(1)));
            if ( attempt != null)
            {
                returned[0]=(new ItemStack(attempt,amount));
                return returned;
            }
            attempt = api.World.GetItem(new AssetLocation("game:ore-" + FirstCodePart(1)));
            if( attempt != null)
            {
                returned[0]=(new ItemStack(attempt, amount));
                return returned;
            }
            attempt = api.World.GetItem(new AssetLocation("game:gem-" + FirstCodePart(1) + "-rough"));
            if( attempt != null)
            {
                ItemStack tmp = new ItemStack(attempt, 1);
                int roll = Randy.Next(0, 101);
                string poten;
                switch (roll)
                {
                    case int x when x >= 85:
                        {
                            poten = "high";
                            break;
                        }
                    case int x when x >= 50 && x < 85:
                        {
                            poten = "medium";
                            break;
                        }
                    default:
                        {
                            poten = "low";
                            break;
                        }
                }
                tmp.Attributes.SetString("potential", poten);
                returned[0]=(tmp);
                
                return returned;
            }
            switch (FirstCodePart(1)) {
                case "quartz_nativegold":
                    {
                        returned[0] = new ItemStack(api.World.GetItem(new AssetLocation("game:nugget-nativegold")),amount);
                        return returned;
                    }
                case "quartz_nativesilver":
                    {
                        returned[0] = new ItemStack(api.World.GetItem(new AssetLocation("game:nugget-nativesilver")), amount);
                        return returned;
                    }
                case "flint":
                    {
                        returned[0] = new ItemStack(api.World.GetItem(new AssetLocation("game:flint")), amount);
                        return returned;
                    }
                case "salt":
                    {
                        returned[0] = new ItemStack(api.World.GetItem(new AssetLocation("game:salt")), amount * 4);
                        return returned;
                    }
                case "saltpeter":
                    {
                        returned[0] = new ItemStack(api.World.GetItem(new AssetLocation("game:saltpeter")), amount);
                        return returned;
                    }
            } return null;
        }

        private SimpleParticleProperties InitBreakParticles(Vec3d pos)
        {
            Random rand = new Random();
            Vec3f velocityRand = new Vec3f((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble()) * 6;

            return new SimpleParticleProperties()
            {
                MinPos = new Vec3d(pos.X, pos.Y, pos.Z),
                AddPos = new Vec3d(1, 1, 1),

                MinVelocity = new Vec3f(velocityRand.X, velocityRand.Y, velocityRand.Z),
                AddVelocity = new Vec3f(-velocityRand.X, -velocityRand.Y, -velocityRand.Z) * 2,

                GravityEffect = 0.2f,

                MinSize = 0.1f,
                MaxSize = 0.4f,
                SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.1f),

                MinQuantity = 20,
                AddQuantity = 40,

                LifeLength = 1.2f,
                addLifeLength = 1.4f,

                ShouldDieInLiquid = false,

                WithTerrainCollision = true,

                Color = CrystalColour.GetColour(FirstCodePart(1)),
                OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARREDUCE, 255),

                Bounciness = 0.4f,

                VertexFlags = 150,

                ParticleModel = EnumParticleModel.Cube
            };
        }
        public void BreakParticles(BlockPos pos)
        {
            api.World.SpawnParticles(InitBreakParticles(pos.ToVec3d()));
        }
        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return interactions;
        }
    }
    public class OreCrystalsCrystalBE : BlockEntity
    {
        protected static Random rand = new Random();
        protected double nextGrowTime;
        protected double totalHoursLastUpdate;

        OreCrystalsCrystal crymtal;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            crymtal = Block as OreCrystalsCrystal;
            if(crymtal == null) { return; }
            totalHoursLastUpdate = Api.World.Calendar.TotalHours;
            nextGrowTime = Api.World.Calendar.TotalHours + Math.Max(120 * ((float)Pos.Y / Api.World.BlockAccessor.MapSizeY),12);
            if (Api is ICoreServerAPI)
            {
                RegisterGameTickListener(CommonTick, 3000 + rand.Next(50));
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            nextGrowTime = tree.GetDouble("growtime");
            totalHoursLastUpdate = tree.GetDouble("last");
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetDouble("growtime", nextGrowTime);
            tree.SetDouble("last", totalHoursLastUpdate);
        }


        private void CommonTick(float dt)
        {
            if (!(Api as ICoreServerAPI).World.IsFullyLoadedChunk(Pos)) { return; }

            double currentTotal = Api.World.Calendar.TotalHours;
            double hourInter = 6 * (Pos.Y / (float)Api.World.BlockAccessor.MapSizeY);

            if((currentTotal - totalHoursLastUpdate) < hourInter) {
                if(totalHoursLastUpdate > currentTotal) {
                    double rollbacc = totalHoursLastUpdate - currentTotal;
                    nextGrowTime -= rollbacc;
                }
                else
                {
                    //Maybe check lava?
                }
            }
            if(Block.FirstCodePart() == "orecrystals_crystal_bountiful") { return; }
            while((currentTotal - totalHoursLastUpdate)>hourInter)
            {
                totalHoursLastUpdate += hourInter; 
                if (Block.FirstCodePart() == "orecrystals_crystal_bountiful") { break; }
                if (nextGrowTime <= totalHoursLastUpdate)
                {
                    AttemptGrow(totalHoursLastUpdate);
                }
            }
            Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
        }

        private void AttemptGrow(double currentHours)
        {
            if (Block == null) { return; }
            if (Block.Id == 0) { return; }
            int newBlockID = 0;
            switch (Block.FirstCodePart())
            {
                case "seed_crystals":
                    {
                        newBlockID = Api.World.GetBlock(Block.Code.WildCardReplace(new AssetLocation("lensgemology:seed_crystals-*-*"), new AssetLocation("lensgemology:orecrystals_crystal_poor-*-*"))).Id;
                        break;
                    }
                case "orecrystals_crystal_poor": 
                    {
                        newBlockID = Api.World.GetBlock(Block.Code.WildCardReplace(new AssetLocation("lensgemology:orecrystals_crystal_poor-*-*"), new AssetLocation("lensgemology:orecrystals_crystal_medium-*-*"))).Id;
                        break; 
                    }
                case "orecrystals_crystal_medium":
                    {
                        newBlockID = Api.World.GetBlock(Block.Code.WildCardReplace(new AssetLocation("lensgemology:orecrystals_crystal_medium-*-*"), new AssetLocation("lensgemology:orecrystals_crystal_rich-*-*"))).Id;
                        break;
                    }
                case "orecrystals_crystal_rich":
                    {
                        newBlockID = Api.World.GetBlock(Block.Code.WildCardReplace(new AssetLocation("lensgemology:orecrystals_crystal_rich-*-*"), new AssetLocation("lensgemology:orecrystals_crystal_bountiful-*-*"))).Id;
                        break;
                    }
            }
            nextGrowTime = Api.World.Calendar.TotalHours + Math.Max(120 * ((float)Pos.Y / Api.World.BlockAccessor.MapSizeY), 12);
            Api.World.BlockAccessor.ExchangeBlock(newBlockID, Pos);
            MarkDirty();
            return;
        }
    }
}
