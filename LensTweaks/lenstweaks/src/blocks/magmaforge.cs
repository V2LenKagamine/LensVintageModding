using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualBasic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace LensstoryMod
{
    public class MagmaForgeBlock : Block
    {
        WorldInteraction[] interactions;
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            if (api.Side != EnumAppSide.Client) return;
            ICoreClientAPI capi = api as ICoreClientAPI;

            interactions = ObjectCacheUtil.GetOrCreate(api, "magmaforgeBlockInteractions", () =>
            {
                List<ItemStack> heatableStacklist = new List<ItemStack>();
                foreach (CollectibleObject obj in api.World.Collectibles)
                {
                    string firstCodePart = obj.FirstCodePart();

                    if (firstCodePart == "ingot" || firstCodePart == "metalplate" || firstCodePart == "workitem")
                    {
                        List<ItemStack> stacks = obj.GetHandBookStacks(capi);
                        if (stacks != null) heatableStacklist.AddRange(stacks);
                    }
                }

                return new WorldInteraction[] {
                    new WorldInteraction()
                    {
                        ActionLangCode = "Add Workable",
                        HotKeyCode = "shift",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = heatableStacklist.ToArray(),
                        GetMatchingStacks = (wi, bs, es) =>
                        {
                            MagmaForgeBe bef = api.World.BlockAccessor.GetBlockEntity(bs.Position) as MagmaForgeBe;
                            if (bef!= null && bef.contents != null)
                            {
                                return wi.Itemstacks.Where(stack => stack.Equals(api.World, bef.contents, GlobalConstants.IgnoredStackAttributes)).ToArray();
                            }
                            return wi.Itemstacks;
                        }
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "Take Workable",
                        HotKeyCode = null,
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = heatableStacklist.ToArray(),
                        GetMatchingStacks = (wi, bs, es) =>
                        {
                            MagmaForgeBe bef = api.World.BlockAccessor.GetBlockEntity(bs.Position) as MagmaForgeBe;
                            if (bef!= null && bef.contents != null)
                            {
                                return new ItemStack[] { bef.contents };
                            }
                            return null;
                        }
                    }
                };
            });
        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            MagmaForgeBe bea = world.BlockAccessor.GetBlockEntity(blockSel.Position) as MagmaForgeBe;
            if (bea != null)
            {
                return bea.OnPlayerInteract(world, byPlayer, blockSel);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
    }

    public class MagmaForgeBe : BlockEntity
    {
        WeatherSystemBase wthsys;
        public bool fueled
        {
            get
            {
                bool found = false;
                Api.World.BlockAccessor.SearchFluidBlocks(Pos, Pos, (lavamaybe, _) => 
                {
                    if (lavamaybe.FirstCodePart() == "lava") { found = true; return true; }
                    return false;
                });
                return found;
            }
        }
        public ItemStack contents;
        double lastTickTotalHours;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            if(contents != null)
            {
                contents.ResolveBlockOrItem(api.World);
            }
            wthsys = api.ModLoader.GetModSystem<WeatherSystemBase>();
            RegisterGameTickListener(OnCommonTick, 200);
        }

        Vec3d tmpPos = new();
        public void OnCommonTick(float dt)
        {
            if (fueled && contents != null)
            {
                var hoursPast = Api.World.Calendar.TotalHours - lastTickTotalHours;
                float temp = contents.Collectible.GetTemperature(Api.World, contents);
                if (temp < 1100)
                {
                    float gain = (float)(hoursPast * 1500);
                    contents.Collectible.SetTemperature(Api.World, contents, Math.Min(1100, temp + gain));
                    MarkDirty();
                }

            }
            tmpPos.Set(Pos.X + 0.5, Pos.Y + 0.5, Pos.Z + 0.5);
            double rainLevel = 0;
            bool rainCheck =
                Api.Side == EnumAppSide.Server
                && Api.World.Rand.NextDouble() < 0.15
                && Api.World.BlockAccessor.GetRainMapHeightAt(Pos.X, Pos.Z) <= Pos.Y
                && (rainLevel = wthsys.GetPrecipitation(tmpPos)) > 0.1
            ;

            if (rainCheck && Api.World.Rand.NextDouble() < rainLevel * 5)
            {
                bool playsound = false;
                if (fueled)
                {
                    playsound = true;
                }
                float temp = contents == null ? 0 : contents.Collectible.GetTemperature(Api.World, contents);
                if (temp > 20)
                {
                    playsound = temp > 100;
                    contents.Collectible.SetTemperature(Api.World, contents, Math.Min(1100, temp - 8), false);
                    MarkDirty(true);
                }

                if (playsound)
                {
                    Api.World.PlaySoundAt(new AssetLocation("game:sounds/effect/extinguish"), Pos.X + 0.5, Pos.Y + 0.75, Pos.Z + 0.5, null, false, 16);
                }
            }
            lastTickTotalHours = Api.World.Calendar.TotalHours;
        }

        public bool OnPlayerInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

            if (!byPlayer.Entity.Controls.ShiftKey)
            {
                if (contents == null) return false;
                ItemStack split = contents.Clone();
                split.StackSize = 1;
                contents.StackSize--;

                if (contents.StackSize == 0) contents = null;

                if (!byPlayer.InventoryManager.TryGiveItemstack(split))
                {
                    world.SpawnItemEntity(split, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }
                MarkDirty();
                Api.World.PlaySoundAt(new AssetLocation("game:sounds/block/ingot"), Pos.X, Pos.Y, Pos.Z, byPlayer, false);

                return true;

            }
            else
            {
                if (slot.Itemstack == null) return false;

                string firstCodePart = slot.Itemstack.Collectible.FirstCodePart();
                bool forgableGeneric = slot.Itemstack.Collectible.Attributes?.IsTrue("forgable") == true;

                // Add heatable item
                if (contents == null && (firstCodePart == "ingot" || firstCodePart == "metalplate" || firstCodePart == "workitem" || forgableGeneric))
                {
                    contents = slot.Itemstack.Clone();
                    contents.StackSize = 1;
                    slot.TakeOut(1);
                    slot.MarkDirty();
                    MarkDirty();
                    Api.World.PlaySoundAt(new AssetLocation("game:sounds/block/ingot"), Pos.X, Pos.Y, Pos.Z, byPlayer, false);

                    return true;
                }

                // Merge heatable item
                if (!forgableGeneric && contents != null && contents.Equals(Api.World, slot.Itemstack, GlobalConstants.IgnoredStackAttributes) && contents.StackSize < 4 && contents.StackSize < contents.Collectible.MaxStackSize)
                {
                    float myTemp = contents.Collectible.GetTemperature(Api.World, contents);
                    float histemp = slot.Itemstack.Collectible.GetTemperature(Api.World, slot.Itemstack);

                    contents.Collectible.SetTemperature(world, contents, (myTemp * contents.StackSize + histemp * 1) / (contents.StackSize + 1));
                    contents.StackSize++;
                    slot.TakeOut(1);
                    slot.MarkDirty();
                    Api.World.PlaySoundAt(new AssetLocation("game:sounds/block/ingot"), Pos.X, Pos.Y, Pos.Z, byPlayer, false);

                    MarkDirty();
                    return true;
                }

                return false;
            }
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            if (contents != null)
            {
                int temp = (int)contents.Collectible.GetTemperature(Api.World,contents);
                if (temp <= 25)
                {
                    dsc.AppendLine("Contains: " + contents.StackSize + "x " + contents.GetName());
                }
                else
                {
                    dsc.AppendLine("Contains: " + contents.StackSize + "x " +  contents.GetName() + " at " + temp + " degrees");
                }
            }
        }
        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            base.OnBlockBroken(byPlayer);

            if (contents != null)
            {
                Api.World.SpawnItemEntity(contents, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);

            contents = tree.GetItemstack("contents");
            lastTickTotalHours = tree.GetFloat("lastTickTotalHours");
            contents?.ResolveBlockOrItem(worldForResolving);

        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            tree.SetItemstack("contents", contents);
            tree.SetDouble("lastTickTotalHours", lastTickTotalHours);
        }
    }
}
