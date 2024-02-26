using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace LensstoryMod
{
    public class WoodenHiveBlock : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is WoodenHiveBE entity)
            {
                return entity.OnPlayerInteract(byPlayer);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
    public class WoodenHiveBE : BlockEntity
    {
        int nearFlowers;
        double whenHarvestable;
        public int HoneyAmt;
        int bees;
        int walkCounter;
        RoomRegistry roomreg;
        float roomness;
        float honeytime;

        int tempNearFlowers;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            roomreg = Api.ModLoader.GetModSystem<RoomRegistry>();

            RegisterGameTickListener(OnCommonTick, 60000); //60 seconds, as its a walkblocks.
            RegisterGameTickListener(TestHarvest, 10000);
        }

        public void TestHarvest(float dt)
        {
            float temp = Api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, Api.World.Calendar.TotalDays).Temperature;

            if (roomness > 0) temp += 5;
            honeytime = GameMath.Clamp(temp / 4, 0f, 1f);

            if (temp <= - 12.5f)
            {
                whenHarvestable = Api.World.Calendar.TotalHours + 20 / 2 * (3 + Api.World.Rand.NextDouble() * 8);
            }
            if(HoneyAmt < 16 && Api.World.Calendar.TotalHours > whenHarvestable && bees > 1)
            {
                HoneyAmt++;
                whenHarvestable = Api.World.Calendar.TotalHours + 20 / 2 * (3 + Api.World.Rand.NextDouble() * 8);
                MarkDirty();
            }
        }

        public bool OnPlayerInteract(IPlayer player)
        {
            ItemSlot slot = player.InventoryManager.ActiveHotbarSlot;
            if (slot.Itemstack?.Block?.Code.Path.StartsWith("skep-populated") == true)
            {
                if(Api.World.Side == EnumAppSide.Client) { return true; }
                bees = 1;
                slot.TakeOut(1);
                player.InventoryManager.TryGiveItemstack(new(Api.World.GetBlock(AssetLocation.Create("game:skep-empty-east"))));
                MarkDirty();
                slot.MarkDirty();
                return true;
            }
            else if (slot.Itemstack?.Block?.Code.Path.StartsWith("skep-empty") == true && bees > 0)
            {
                if (Api.World.Side == EnumAppSide.Client) { return true; }
                bees = 0;
                slot.TakeOut(1);
                player.InventoryManager.TryGiveItemstack(new(Api.World.GetBlock(AssetLocation.Create("game:skep-populated-east"))));
                slot.MarkDirty();
                MarkDirty();
            }
            if (slot.Itemstack?.Collectible?.Code.Path.StartsWith("knife") == true && HoneyAmt > 0)
            {
                if (Api.World.Side == EnumAppSide.Client) { return true; }
                player.InventoryManager.TryGiveItemstack(new(Api.World.GetItem(AssetLocation.Create("game:honeycomb")), HoneyAmt * 3));
                HoneyAmt = 0;
                MarkDirty();
                int olddura = slot.Itemstack.Attributes.GetInt("durability", -1);
                if (olddura == -1)
                {
                    olddura = slot.Itemstack.Collectible.Durability;
                }
                slot.Itemstack.Attributes?.SetInt("durability", olddura - 1);
                if (slot.Itemstack.Attributes.GetInt("durability") <= 0)
                {
                    slot.TakeOutWhole();
                }
                slot.MarkDirty();
                return true;
            }
            return false;
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            if (Api?.World != null)
            {
                whenHarvestable = Api.World.Calendar.TotalHours + 20 / 2 * (3 + Api.World.Rand.NextDouble() * 8);
            }
        }
        public void OnCommonTick(float dt)
        {
            Room room = roomreg?.GetRoomForPosition(Pos);
            roomness = (room != null && room.SkylightCount > room.NonSkylightCount && room.ExitCount == 0) ? 1 : 0;
            if(honeytime < 1) { return; }
            if (Api.Side == EnumAppSide.Client) return;
            // Gets everything in a 15x5x15 cube, I think.
            if (walkCounter == 0)
            {
                tempNearFlowers = 0;
            }

            int minx = -8 + 8 * (walkCounter / 2);
            int minz = -8 + 8 * (walkCounter % 2);
            int size = 8;

            Api.World.BlockAccessor.WalkBlocks(Pos.AddCopy(minx, -3, minz), Pos.AddCopy(minx + size - 1, 3,minz + size -1), (block, x, y, z) =>
            {
                if (block.Id == 0) return;

                //Plant moment
                if (block.BlockMaterial == EnumBlockMaterial.Plant)
                {
                    if (block.Attributes?.IsTrue("beeFeed") == true) tempNearFlowers++;
                    return;
                }
            });

            walkCounter++;

            if(walkCounter >= 4)
            {
                walkCounter = 0;
                OnQuadWalk();
            }
        } 

        private void OnQuadWalk()
        {
            nearFlowers = tempNearFlowers;
            if (bees != 0)
            {
                bees = GameMath.Clamp(nearFlowers - 23, 1, 2);
            }
            MarkDirty();
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (bees > 0)
            {
                dsc.AppendLine("Looks like a hive is inside.");
            } else
            {
                dsc.AppendLine("Ready for a hive to be put in their new home.");
            }
            switch (HoneyAmt)
            {
                case int x when x < 1:
                    {
                        dsc.AppendLine("There's barely enough honey for the bees.");
                        break;
                    }
                case int x when x >= 1 && x < 8:
                    {
                        dsc.AppendLine("There's a small excess of honey inside.");
                        break;
                    }
                case int x when x >= 8 && x < 16:
                    {
                        dsc.AppendLine("There's a large excess of honey inside.");
                        break;
                    }
                case int x when x >= 16:
                    {
                        dsc.AppendLine("There's an obscene excess of honey inside.");
                        break;
                    }
                default:
                    {
                        dsc.AppendLine("Looking inside, you see the cracks of this reality manifested. That can't be good.");
                        break;
                    }
            }
            dsc.AppendLine(string.Format("The bees count {0} flowers nearby",nearFlowers));
            if(roomness > 0)
            {
                dsc.AppendLine("The bees appreciate the warmth of the greenhouse.");
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            nearFlowers = tree.GetInt("flowers");
            whenHarvestable = tree.GetDouble("harvesttime");
            HoneyAmt = tree.GetInt("honey");
            bees = tree.GetInt("bees");
            walkCounter = tree.GetInt("walks");
            roomness = tree.GetFloat("room");
            honeytime = tree.GetFloat("honeytime");
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("flowers", nearFlowers);
            tree.SetDouble("harvesttime", whenHarvestable);
            tree.SetInt("honey", HoneyAmt);
            tree.SetInt("bees", bees);
            tree.SetInt("walks", walkCounter);
            tree.SetFloat("room", roomness);
            tree.SetFloat("honeytime", honeytime);
        }
    }
}
