using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace runestory
{
    public class RuneAltarBlock : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)

        {
            bool result = TryAddCover(world, byPlayer, blockSel);

            if (result) {  return true; }

            result = DoWrenchClick(world, byPlayer, blockSel);
            if (result) {  return true; }

            if (api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is RuneAltarBe altar)
            {
                return altar.OnInteract(world, byPlayer);
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        #region ChiselTools Assist
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
        #endregion
    }
    public class RuneAltarBe : BlockEntity
    {

        public ItemStack? Contents { get; private set; }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            Contents?.ResolveBlockOrItem(api.World);
        }
        public void DoRuneCraft(IWorldAccessor world,IPlayer ply) {
            if (Contents is null) { return; }

            Entity[] nearAltar = Api.World.GetEntitiesInsideCuboid(Pos.AddCopy(-1, -1, -1), Pos.AddCopy(1, 1, 1), ent => ent.OnGround && ent is EntityItem);

            for (int i = 0; i < Api.ModLoader.GetModSystem<runestoryModSystem>().AltarRecipes.Count; i++)
            {
                int MaxCanMake = Contents.StackSize;
                BaseRuneAltar recipe = Api.ModLoader.GetModSystem<runestoryModSystem>().AltarRecipes[i];
                Entity[] toEat = new Entity[recipe.Reagents.Count];
                bool valid = false;
                for (int j = 0; j < recipe.Reagents.Count; j++)
                {
                    bool found = false;
                    foreach (Entity ent in nearAltar)
                    {
                        if (recipe.Reagents.ElementAt(j).Key.Contains('*'))
                        {
                            if(WildcardUtil.Match(recipe.Reagents.ElementAt(j).Key,(ent as EntityItem).Itemstack?.Collectible.Code?.ToString())) 
                            {
                                MaxCanMake = Math.Min(MaxCanMake,(int)Math.Floor((ent as EntityItem).Itemstack.StackSize / (float)recipe.Reagents.ElementAt(j).Value));
                                found = true; 
                                toEat[j]=ent;
                                break;
                            }
                        }
                        else
                        {
                            if ((ent as EntityItem).Itemstack?.Collectible?.Code?.ToString() == recipe.Reagents.ElementAt(j).Key)
                            {
                                MaxCanMake = Math.Min(MaxCanMake, (int)Math.Floor((ent as EntityItem).Itemstack.StackSize / (float)recipe.Reagents.ElementAt(j).Value));
                                found = true;
                                toEat[j]=ent;
                                break;
                            }
                        }
                    }
                    if (!found) { break; }
                    if(j == recipe.Reagents.Count - 1) {  valid = true; }
                }
                if(!valid) { continue; }
                bool stillvalid = true;
                for (int i5 = 0; i5 < recipe.OutputItems.Count; i5++)
                {
                   if(recipe.OutputItems.ElementAt(i5).Value * MaxCanMake > Contents.StackSize) { if (MaxCanMake > 1) { MaxCanMake--; } else if (MaxCanMake <= 1) { stillvalid = false; break; } }
                }
                if(!stillvalid) { continue; }
                for (int i2 = 0; i2 < recipe.Reagents.Count;i2++)
                {
                    EntityItem victim = toEat[i2] as EntityItem;
                    victim.Itemstack.StackSize -= (recipe.Reagents.ElementAt(i2).Value * MaxCanMake);
                    victim.MarkTagsDirty();
                    if (victim.Itemstack.StackSize <= 0)
                    {
                        victim.Die();
                    }
                }
                for(int i3 = 0;i3< recipe.OutputItems.Count;i3++)
                {
                    EntityItem newstack = Api.World.SpawnItemEntity(new(Api.World.GetItem(recipe.OutputItems.ElementAt(i3).Key),recipe.OutputItems.ElementAt(i3).Value),Pos.AddCopy(0,1.5f,0),new(Api.World.Rand.Next(-1,1)*0.1f, Api.World.Rand.Next(-1, 1) * 0.1f, Api.World.Rand.Next(-1, 1) * 0.1f)) as EntityItem;

                    Contents.StackSize -= MaxCanMake * recipe.OutputItems.ElementAt(i3).Value;
                    if (Contents.StackSize <= 0) { Contents = null; }
                    MarkDirty();
                }
                break; //We've done enough for one craft...
            }
        }
        public bool OnInteract(IWorldAccessor world,IPlayer ply)
        {
            if(ply.Entity.Controls.ShiftKey)
            {
                if(Contents is not null && ply.InventoryManager.ActiveHotbarSlot?.Itemstack?.Collectible?.Code?.ToString() == "game:stick")
                {
                    if(Api.Side == EnumAppSide.Server) 
                    { DoRuneCraft(world, ply); }
                    
                    return true;
                }
                if(Contents is null) { return false; }
                if (!ply.InventoryManager.TryGiveItemstack(Contents))
                {
                    world.SpawnItemEntity(Contents, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }
                Contents = null;
                MarkDirty();
                return true;
            }
            var slot = ply.InventoryManager.ActiveHotbarSlot;
            if(slot.Itemstack is not null)
            {
                //Yes this is hardcoded for now. Deal with it.
                if (slot.Itemstack.Collectible.Code.ToString() == "runestory:rune-blank" && Contents is null)
                {
                    Contents = slot.Itemstack.Clone();
                    slot.TakeOutWhole();
                    slot.MarkDirty();
                    MarkDirty();
                    return true;
                }
            }
            return false;
        }

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            if(Api.World.Side == EnumAppSide.Server)
            {
                if (Contents != null)
                {
                    Api.World.SpawnItemEntity(Contents, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }
            }
            base.OnBlockBroken(byPlayer);
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (Contents != null)
            {
                dsc.AppendLine($"\n Contains: {Contents?.GetName()} x {Contents?.StackSize.ToString()}");
            }
            if(GetBehavior<BEBChiseledCover>()?.ChiseledItemStack is null)
            {
                dsc.AppendLine("If you dont like the look, you can always Right Click with a chiseled block to camo it!");
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            Contents = tree.GetItemstack("contents");
            Contents?.ResolveBlockOrItem(worldAccessForResolve);

        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetItemstack("contents", Contents);
        }
    }
}
