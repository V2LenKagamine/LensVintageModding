using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace LensMiniTweaks
{
    public class TreegrowItem : Item
    {
        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            handling = EnumHandHandling.PreventDefaultAction;
            return;
        }

        public override void OnHeldUseStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumHandInteract useType, bool firstEvent, ref EnumHandHandling handling)
        {
            if (blockSel != null && api.World.BlockAccessor.GetBlockEntity(blockSel?.Position?.Copy()) is BlockEntityFruitTreeBranch fruity)
            {
                FruitTreeGrowingBranchBH rooty = fruity.GetBehavior<FruitTreeGrowingBranchBH>();
                if (rooty != null)
                {
                    handling = EnumHandHandling.PreventDefaultAction; //God forgive me for the next for statements.
                    for (int y = 0; y <= 4; y++)
                    {
                        for (int x = -2; x <= 2; x++)
                        {
                            for (int z = -2; z <= 2; z++)
                            {
                                BlockPos lookingAt = blockSel.Position.Copy().Add(x, y, z);
                                if (api.World.BlockAccessor.GetBlockEntity(lookingAt) is BlockEntityFruitTreeBranch targetBranch)
                                {
                                    FruitTreeGrowingBranchBH branchBH = targetBranch.GetBehavior<FruitTreeGrowingBranchBH>();
                                    if (branchBH == null) { continue; }
                                    for (int grows = 0; grows <= api.World.Rand.NextInt64(1, 5);grows++) {
                                        AccessTools.Method(typeof(FruitTreeGrowingBranchBH), "TryGrow").Invoke(branchBH, null);
                                    }
                                    targetBranch.lastGrowthAttemptTotalDays = api.World.Calendar.TotalDays;
                                    targetBranch.GrowTries++;
                                    targetBranch.MarkDirty(true);
                                }
                            }
                        }
                    }
                    if (api.Side == EnumAppSide.Server)
                    {
                        if (slot.StackSize <= 0)
                        {
                            slot.TakeOutWhole();
                        }
                        else
                        {
                            slot.TakeOut(1);
                        }
                        slot.MarkDirty();
                    }
                }
                return;
            }
            base.OnHeldUseStart(slot, byEntity, blockSel, entitySel, useType, firstEvent, ref handling);
        }

    }
}
