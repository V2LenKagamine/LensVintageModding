using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace runestory.src.entity.spells
{
    public class RefineItemSpell : BaseRuneEnt
    {
        public void DoRefineCraft()
        {
            if (Api.Side == EnumAppSide.Client) { return; }

            Entity[] NearUs = Api.World.GetEntitiesInsideCuboid(Pos.XYZ.AddCopy(-1, -1, -1).AsBlockPos, Pos.XYZ.AddCopy(1, 1, 1).AsBlockPos, ent => ent.OnGround && ent is EntityItem);

            for (int i = 0; i < Api.ModLoader.GetModSystem<runestoryModSystem>().RefineRecipes.Count; i++)
            {
                BaseRefineRecipe recipe = Api.ModLoader.GetModSystem<runestoryModSystem>().RefineRecipes[i];
                List<Entity> toEat = [];
                bool valid = false;
                int MaxCanMake = int.MaxValue;
                for (int j = 0; j < recipe.Reagents.Count; j++)
                {
                    bool found = false;
                    foreach (Entity ent in NearUs)
                    {
                        if (recipe.Reagents.ElementAt(j).Key.Contains('*'))
                        {
                            if (WildcardUtil.Match(recipe.Reagents.ElementAt(j).Key, (ent as EntityItem).Itemstack?.Collectible.Code?.ToString()))
                            {
                                found = true; 
                                EntityItem tmp = null;
                                if (toEat.Count > 0)
                                {
                                    tmp = toEat.Where(boi => (boi as EntityItem).Itemstack.Collectible.Code == (ent as EntityItem).Itemstack.Collectible.Code)?.First() as EntityItem;
                                }
                                if (tmp is not null)
                                {
                                    //Pray to god this doesnt cause a Memory Leak
                                    ItemStack Hell = new ItemStack((ent as EntityItem).Itemstack.Collectible, (ent as EntityItem).Itemstack.StackSize + tmp.Itemstack.StackSize);
                                    tmp.Slot.Set(Hell);
                                    tmp.Itemstack = Hell;
                                    tmp.Slot.MarkDirty();
                                    ent.Die();
                                }
                                else
                                {
                                    toEat.Add(ent);
                                }
                            }
                        }
                        else
                        {
                            if ((ent as EntityItem).Itemstack?.Collectible?.Code?.ToString() == recipe.Reagents.ElementAt(j).Key)
                            {
                                found = true;
                                EntityItem tmp = null;
                                if(toEat.Count >0)
                                {
                                    tmp = toEat.Where(boi => (boi as EntityItem).Itemstack.Collectible.Code == (ent as EntityItem).Itemstack.Collectible.Code)?.First() as EntityItem;
                                }
                                if (tmp is not null)
                                {
                                    ItemStack Hell = new ItemStack((ent as EntityItem).Itemstack.Collectible, (ent as EntityItem).Itemstack.StackSize + tmp.Itemstack.StackSize);
                                    tmp.Slot.Set(Hell);
                                    tmp.Itemstack = Hell;
                                    tmp.Slot.MarkDirty();
                                    ent.Die();
                                }
                                else
                                {
                                    toEat.Add(ent);
                                }
                            }
                        }
                    }
                    if (!found) { break; }
                    if (j == recipe.Reagents.Count - 1) { valid = true; }
                }
                for(int hate = 0; hate < toEat.Count;hate++)
                {
                    for (int stupid = 0; stupid < recipe.Reagents.Count; stupid++)
                    {
                        EntityItem succ = toEat[hate] as EntityItem;
                        if (!recipe.SatisfiesAsIngredient(stupid, succ.Slot.Itemstack)) { continue; }
                        MaxCanMake = (int)Math.Min(MathF.Floor((succ.Itemstack.StackSize / (float)recipe.Reagents.ElementAt(stupid).Value)), MaxCanMake);
                    }
                }
                if (!valid) { continue; }
                for (int i2 = 0; i2 < toEat.Count; i2++)
                {
                    EntityItem victim = toEat[i2] as EntityItem;
                    for (int i3 = 0; i3 < recipe.Reagents.Count(); i3++) {
                        if(!recipe.SatisfiesAsIngredient(i3,victim.Slot.Itemstack)) { continue; }
                        victim.Slot.Set(victim.Slot.TakeOut(recipe.Reagents.ElementAt(i3).Value * MaxCanMake));
                        victim.MarkTagsDirty();
                        if (victim.Itemstack is null || victim.Itemstack?.StackSize <=0 || victim.Slot.Itemstack is null || victim.Slot.Itemstack?.StackSize <= 0)
                        {
                            victim.Die();
                        }
                    }
                }
                for (int i3 = 0; i3 < recipe.Outputs.Count; i3++)
                {
                    EntityItem newstack = Api.World.SpawnItemEntity(new(Api.World.GetItem(recipe.Outputs.ElementAt(i3).Key), recipe.Outputs.ElementAt(i3).Value * MaxCanMake), Pos.XYZ.AddCopy(0, 1.5f, 0), new(Api.World.Rand.Next(-1, 1) * 0.1f, Api.World.Rand.Next(-1, 1) * 0.1f, Api.World.Rand.Next(-1, 1) * 0.1f)) as EntityItem;
                }
                break; //We've done enough for one craft...
            }
        }

        public override void OnCollided()
        {
            DoRefineCraft();
            Die();
        }

        public override void OnTouchEntity(Entity entity)
        {
            DoRefineCraft();
            Die();
        }
    }
}
