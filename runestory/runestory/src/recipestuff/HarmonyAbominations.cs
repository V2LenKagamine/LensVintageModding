using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace runestory.src.recipestuff
{
    public static class AddCreatedByPatch
    {
        public static bool isRunning = false;
        public static void Postfix(
            bool __result,
            ICoreClientAPI capi,
            ItemStack[] allStacks,
            ActionConsumable<string> openDetailPageFor,
            ItemStack stack,
            List<RichTextComponentBase> components,
            float marginTop,
            ref bool haveText)
        {
            // Add smoking information after the original method logic
            __result = components.AddCreatedByRuneAltar(stack, capi, allStacks, openDetailPageFor, haveText);
            __result = components.AddCreatedByRefine(stack, capi, allStacks, openDetailPageFor, haveText);
        }
    }

    public static class GetHandbookInfoPatch
    {
        public static void Postfix(
            ref RichTextComponentBase[] __result,
            ItemSlot inSlot,
            ICoreClientAPI capi,
            ItemStack[] allStacks,
            ActionConsumable<string> openDetailPageFor)
        {
            List<RichTextComponentBase> list = (__result).ToList<RichTextComponentBase>();
            list.AddRuneAltarRecipe(inSlot?.Itemstack, capi, allStacks, openDetailPageFor, true);
            list.AddRefineRecipe(inSlot?.Itemstack, capi, allStacks, openDetailPageFor, true);
            __result = list.ToArray();
        }
    }

    public static class HandbookExtensions
    {
        public static List<ItemStack> GetRuneFrom(CollectibleObject collectible, ICoreAPI api)
        {
            List<ItemStack> Output = [];
            foreach (BaseRuneAltar rec in api.ModLoader.GetModSystem<RunestoryMS>().AltarRecipes.Where(pos =>
            {
                bool found = false;
                for (int i = 0; i < pos.Reagents.Count(); i++)
                {
                    if (pos.Reagents.ElementAt(i).Key.Contains('*'))
                    {
                        found = WildcardUtil.Match(pos.Reagents.ElementAt(i).Key, collectible.Code.ToString());
                    }
                    else
                    {
                        found = collectible.Code.ToString() == pos.Reagents.ElementAt(i).Key;
                    }
                    if (found) { break; }
                }
                return found;
            }))
            {
                for (int i2 = 0; i2 < rec.OutputItems.Count(); i2++)
                {
                    KeyValuePair<string, int> targ = rec.OutputItems.ElementAt(i2);
                    if (api.World.GetItem(targ.Key) is not null)
                    {
                        Output.Add(new(api.World.GetItem(targ.Key), targ.Value));
                    }
                    else if (api.World.GetBlock(targ.Key) is not null)
                    {
                        Output.Add(new(api.World.GetBlock(targ.Key), targ.Value));
                    }
                }
            }
            return Output;
        }


        public static List<ItemStack> GetRuneInto(CollectibleObject collectible, ICoreAPI api)
        {
            List<ItemStack> Output = [];
            foreach (BaseRuneAltar rec in api.ModLoader.GetModSystem<RunestoryMS>().AltarRecipes.Where(pos =>
            {
                bool found = false;
                for (int i = 0; i < pos.OutputItems.Count(); i++)
                {
                    if (pos.OutputItems.ElementAt(i).Key.Contains('*'))
                    {
                        found = WildcardUtil.Match(pos.OutputItems.ElementAt(i).Key, collectible.Code.ToString());
                    }
                    else
                    {
                        found = collectible.Code.ToString() == pos.OutputItems.ElementAt(i).Key;
                    }
                    if (found) { break; }
                }
                return found;
            }))
            {
                for (int i2 = 0; i2 < rec.Reagents.Count(); i2++)
                {
                    KeyValuePair<string, int> targ = rec.Reagents.ElementAt(i2);
                    if (targ.Key.Contains("*"))
                    {
                        IEnumerable<CollectibleObject> poss = api.World.Collectibles.Where(poss => WildcardUtil.Match(targ.Key, poss.Code.ToString()));
                        foreach (CollectibleObject obj in poss)
                        {
                            if (api.World.GetItem(obj.Code) is not null)
                            {
                                Output.Add(new(api.World.GetItem(obj.Code), targ.Value));
                            }
                            else if (api.World.GetBlock(targ.Key) is not null)
                            {
                                Output.Add(new(api.World.GetBlock(obj.Code), targ.Value));
                            }
                        }
                    } else
                    {
                        if (api.World.GetItem(targ.Key) is not null)
                        {
                            Output.Add(new(api.World.GetItem(targ.Key), targ.Value));
                        }
                        else if (api.World.GetBlock(targ.Key) is not null)
                        {
                            Output.Add(new(api.World.GetBlock(targ.Key), targ.Value));
                        }
                    }
                }
            }
            return Output;
        }

        public static List<ItemStack> GetRefinedFrom(CollectibleObject collectible, ICoreAPI api)
        {
            List<ItemStack> Output = [];
            foreach (BaseRefineRecipe rec in api.ModLoader.GetModSystem<RunestoryMS>().RefineRecipes.Where(pos =>
            {
                bool found = false;
                for (int i = 0; i < pos.Reagents.Count(); i++)
                {
                    if (pos.Reagents.ElementAt(i).Key.Contains('*'))
                    {
                        found = WildcardUtil.Match(pos.Reagents.ElementAt(i).Key, collectible.Code.ToString());
                    }
                    else
                    {
                        found = collectible.Code.ToString() == pos.Reagents.ElementAt(i).Key;
                    }
                    if (found) { break; }
                }
                return found;
            }))
            {
                for (int i2 = 0; i2 < rec.Outputs.Count(); i2++)
                {
                    KeyValuePair<string, int> targ = rec.Outputs.ElementAt(i2);
                    if (api.World.GetItem(targ.Key) is not null)
                    {
                        Output.Add(new(api.World.GetItem(targ.Key), targ.Value));
                    }
                    else if (api.World.GetBlock(targ.Key) is not null)
                    {
                        Output.Add(new(api.World.GetBlock(targ.Key), targ.Value));
                    }
                }
            }
            return Output;
        }
        public static List<ItemStack> GetRefinedInto(CollectibleObject collectible, ICoreAPI api)
        {
            List<ItemStack> Output = [];
            foreach (BaseRefineRecipe rec in api.ModLoader.GetModSystem<RunestoryMS>().RefineRecipes.Where(pos =>
            {
                bool found = false;
                for (int i = 0; i < pos.Outputs.Count(); i++)
                {
                    if (pos.Outputs.ElementAt(i).Key.Contains('*'))
                    {
                        found = WildcardUtil.Match(pos.Outputs.ElementAt(i).Key, collectible.Code.ToString());
                    }
                    else
                    {
                        found = collectible.Code.ToString() == pos.Outputs.ElementAt(i).Key;
                    }
                    if (found) { break; }
                }
                return found;
            }))
            {
                for (int i2 = 0; i2 < rec.Reagents.Count(); i2++)
                {
                    KeyValuePair<string, int> targ = rec.Reagents.ElementAt(i2);
                    if (targ.Key.Contains("*"))
                    {
                        IEnumerable<CollectibleObject> poss = api.World.Collectibles.Where(poss => WildcardUtil.Match(targ.Key, poss.Code.ToString()));
                        foreach (CollectibleObject obj in poss)
                        {
                            if (api.World.GetItem(obj.Code) is not null)
                            {
                                Output.Add(new(api.World.GetItem(obj.Code), targ.Value));
                            }
                            else if (api.World.GetBlock(targ.Key) is not null)
                            {
                                Output.Add(new(api.World.GetBlock(obj.Code), targ.Value));
                            }
                        }
                    }
                    else
                    {
                        if (api.World.GetItem(targ.Key) is not null)
                        {
                            Output.Add(new(api.World.GetItem(targ.Key), targ.Value));
                        }
                        else if (api.World.GetBlock(targ.Key) is not null)
                        {
                            Output.Add(new(api.World.GetBlock(targ.Key), targ.Value));
                        }
                    }
                }
            }
            return Output;
        }

        public static bool AddCreatedByRuneAltar(
            this List<RichTextComponentBase> components,
            ItemStack itemStack,
            ICoreClientAPI capi,
            ItemStack[] allStacks,
            ActionConsumable<string> openDetailPageFor,
            bool haveText)
        {
            List<ItemStack> itemStackList1 = GetRuneFrom(itemStack.Collectible,capi);
            if (itemStackList1.Count() == 0) return haveText;
            ClearFloatTextComponent floatTextComponent1 = new ClearFloatTextComponent(capi, 7f);
            bool haveHeading =
              components.Any(c => c is RichTextComponent rtc && rtc.DisplayText.Contains(Lang.Get("runestory:runecraftsinto") + "\n"));
            if (!haveHeading)
                AddHeadingComponent(components, capi, Lang.Get("runestory:runecraftsinto"), ref haveText);
            components.Add(floatTextComponent1);
            AddSubHeadingComponent(components, capi, openDetailPageFor, Lang.Get("runestory:atrunealtar"), null);
            while (itemStackList1.Count() > 0)
            {
                ItemStack itemstackgroup = itemStackList1.ElementAt(0);
                itemStackList1.RemoveAt(0);
                if (itemstackgroup != null)
                {
                    int num2;
                    SlideshowItemstackTextComponent itemstackTextComponent = new SlideshowItemstackTextComponent(capi, itemstackgroup, itemStackList1, 40.0, EnumFloat.Inline, cs => num2 = openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)) ? 1 : 0);
                    itemstackTextComponent.ShowStackSize = true;
                    itemstackTextComponent.PaddingLeft = 2;
                    components.Add(itemstackTextComponent);
                }
            }
            components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
            return true;
        }

        public static bool AddRuneAltarRecipe(
          this List<RichTextComponentBase> components,
          ItemStack itemStack,
          ICoreClientAPI capi,
          ItemStack[] allStacks,
          ActionConsumable<string> openDetailPageFor,
          bool haveText)
        {
            List<ItemStack> recipeOutstacks = GetRuneInto(itemStack.Collectible,capi);
            if (recipeOutstacks.Count() <=0) return haveText;
            ClearFloatTextComponent floatTextComponent1 = new ClearFloatTextComponent(capi, 7f);
            bool haveHeading =
              components.Any(c => c is RichTextComponent rtc && rtc.DisplayText.Contains(Lang.Get("runestory:createdbyrunealtar") + "\n"));
            if (!haveHeading)
                AddHeadingComponent(components, capi, Lang.Get("runestory:createdbyrunealtar"), ref haveText);
            components.Add(floatTextComponent1);
            AddSubHeadingComponent(components, capi, openDetailPageFor, Lang.Get("runestory:atrunealtar"), null);
            while (recipeOutstacks.Count() > 0)
            {
                ItemStack itemstackgroup = recipeOutstacks.ElementAt(0);
                recipeOutstacks.RemoveAt(0);
                if (itemstackgroup != null)
                {
                    int num2;
                    SlideshowItemstackTextComponent itemstackTextComponent = new SlideshowItemstackTextComponent(capi, itemstackgroup, recipeOutstacks, 40.0, EnumFloat.Inline, cs => num2 = openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)) ? 1 : 0);
                    itemstackTextComponent.ShowStackSize = true;
                    itemstackTextComponent.PaddingLeft = 2;
                    components.Add(itemstackTextComponent);
                }
            }
            components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
            return true;
        }

        public static bool AddCreatedByRefine(
            this List<RichTextComponentBase> components,
            ItemStack itemStack,
            ICoreClientAPI capi,
            ItemStack[] allStacks,
            ActionConsumable<string> openDetailPageFor,
            bool haveText)
        {
            List<ItemStack> itemStackList1 = GetRefinedFrom(itemStack.Collectible, capi);
            if (itemStackList1.Count == 0) return haveText;
            ClearFloatTextComponent floatTextComponent1 = new ClearFloatTextComponent(capi, 7f);
            bool haveHeading =
              components.Any(c => c is RichTextComponent rtc && rtc.DisplayText.Contains(Lang.Get("runestory:refinesinto") + "\n"));
            if (!haveHeading)
                AddHeadingComponent(components, capi, Lang.Get("runestory:refinesinto"), ref haveText);
            components.Add(floatTextComponent1);
            AddSubHeadingComponent(components, capi, openDetailPageFor, Lang.Get("runestory:atrefine"), null);
            while (itemStackList1.Count > 0)
            {
                ItemStack itemstackgroup = itemStackList1[0];
                itemStackList1.RemoveAt(0);
                if (itemstackgroup != null)
                {
                    int num2;
                    SlideshowItemstackTextComponent itemstackTextComponent = new SlideshowItemstackTextComponent(capi, itemstackgroup, itemStackList1, 40.0, EnumFloat.Inline, cs => num2 = openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)) ? 1 : 0);
                    itemstackTextComponent.ShowStackSize = true;
                    itemstackTextComponent.PaddingLeft = 2;
                    components.Add(itemstackTextComponent);
                }
            }
            components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
            return true;
        }

        public static bool AddRefineRecipe(
          this List<RichTextComponentBase> components,
          ItemStack itemStack,
          ICoreClientAPI capi,
          ItemStack[] allStacks,
          ActionConsumable<string> openDetailPageFor,
          bool haveText)
        {
            List<ItemStack> recipeOutstacks = GetRefinedInto(itemStack.Collectible, capi);
            if (recipeOutstacks.Count() <= 0) return haveText;
            ClearFloatTextComponent floatTextComponent1 = new ClearFloatTextComponent(capi, 7f);
            bool haveHeading =
              components.Any(c => c is RichTextComponent rtc && rtc.DisplayText.Contains(Lang.Get("runestory:createdbyrefine") + "\n"));
            if (!haveHeading)
                AddHeadingComponent(components, capi, Lang.Get("runestory:createdbyrefine"), ref haveText);
            components.Add(floatTextComponent1);
            AddSubHeadingComponent(components, capi, openDetailPageFor, Lang.Get("runestory:atrefine"), null);
            while (recipeOutstacks.Count > 0)
            {
                ItemStack itemstackgroup = recipeOutstacks[0];
                recipeOutstacks.RemoveAt(0);
                if (itemstackgroup != null)
                {
                    int num2;
                    SlideshowItemstackTextComponent itemstackTextComponent = new SlideshowItemstackTextComponent(capi, itemstackgroup, recipeOutstacks, 40.0, EnumFloat.Inline, cs => num2 = openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)) ? 1 : 0);
                    itemstackTextComponent.ShowStackSize = true;
                    itemstackTextComponent.PaddingLeft = 2;
                    components.Add(itemstackTextComponent);
                }
            }
            components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
            return true;
        }


        public static void AddHeadingComponent(
          List<RichTextComponentBase> components,
          ICoreClientAPI capi,
          string heading,
          ref bool haveText)
        {
            if (haveText)
                components.Add(new ClearFloatTextComponent(capi, 14f));
            haveText = true;
            RichTextComponent richTextComponent = new RichTextComponent(capi, Lang.Get(heading) + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold));
            components.Add(richTextComponent);
        }

        public static void AddSubHeadingComponent(
          List<RichTextComponentBase> components,
          ICoreClientAPI capi,
          ActionConsumable<string> openDetailPageFor,
          string subheading,
          string detailpage)
        {
            if (detailpage == null)
            {
                RichTextComponent richTextComponent = new RichTextComponent(capi, "• " + Lang.Get(subheading) + "\n", CairoFont.WhiteSmallText());
                richTextComponent.PaddingLeft = 2.0;
                components.Add(richTextComponent);
            }
            else
            {
                RichTextComponent richTextComponent = new RichTextComponent(capi, "• ", CairoFont.WhiteSmallText());
                richTextComponent.PaddingLeft = 2.0;
                components.Add(richTextComponent);
                int num;
                components.Add(new LinkTextComponent(capi, Lang.Get(subheading) + "\n", CairoFont.WhiteSmallText(), _ => num = openDetailPageFor(detailpage) ? 1 : 0));
            }
        }

    }
}