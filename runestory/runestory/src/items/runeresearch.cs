using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace runestory
{
    public class RunicResearch : Item
    {

        public string spellUnlocked = "Fucking Nothing";
        public int tierUnlocked = 0;
        public bool onlyOneSpell = true;


        public override void OnCreatedByCrafting(ItemSlot[] allInputSlots, ItemSlot outputSlot, IRecipeBase byRecipe)
        {
            if((byRecipe as RecipeBase)?.Attributes?["spelltounlock"]?.Exists == true)
            {
                outputSlot.Itemstack.Attributes.SetString("spelltounlock",(byRecipe as RecipeBase).Attributes["spelltounlock"].AsString());
            }
            base.OnCreatedByCrafting(allInputSlots, outputSlot, byRecipe);
        }
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            if(!Attributes.Exists) { api.World.Logger.Error("No attributes for '{0}', go fix it!", Code); }

            if (Attributes["spellIdUnlocked"].Exists)
            {
                spellUnlocked = Attributes["spellIdUnlocked"].AsObject<string>();
            }
            if (Attributes["spellTierUnlocked"].Exists)
            {
                tierUnlocked = Attributes["spellTierUnlocked"].AsObject<int>();
            }
            if (Attributes["onlyOneSpell"].Exists)
            {
                onlyOneSpell = Attributes["onlyOneSpell"].AsObject<bool>();
            }

            if (tierUnlocked == 0 && spellUnlocked == "Fucking Nothing")
            {
                api.World.Logger.Error("No spell unlocks for '{0}', go fix it!", Code);
            }
        }
        public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity)
        {
            return "eat";
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (spellUnlocked != null || tierUnlocked > 0)
            {
                handling = EnumHandHandling.PreventDefault;
                return;
            }
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (byEntity.World is IClientWorldAccessor)
            {
                return secondsUsed <= 1.5f;
            }
            return true;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (byEntity.World.Side == EnumAppSide.Server && secondsUsed >= 1.5f)
            {
                IEnumerable<BaseRuneSpell> validOptions = api.ModLoader.GetModSystem<RunestoryMS>().AllSpells.Where(spell => spell.Code == spellUnlocked || spell.spellTier == tierUnlocked);
                if(slot.Itemstack?.Attributes?.GetString("spelltounlock") is not null)
                {
                    validOptions = validOptions.Where(spll=>spll.Code == slot.Itemstack?.Attributes?.GetString("spelltounlock"));
                }
                if (byEntity is EntityPlayer ply)
                {
                    string[] knownspells = [];
                    if ((ply.Player as IServerPlayer).GetModdata(RunestoryMS.RMS_SpellKnowledge) is not null)
                    {
                        knownspells = SerializerUtil.Deserialize<string[]>((ply.Player as IServerPlayer).GetModdata(RunestoryMS.RMS_SpellKnowledge));
                    }
                    if (validOptions.Count() == 1)
                    {
                        if (knownspells.Contains(validOptions.First().Code))
                        {
                            (ply.Player as IServerPlayer).SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get("runestory:knownspell"), EnumChatType.Notification);
                            (ply.Player as IServerPlayer).SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get("runestory:nowallknown"), EnumChatType.Notification);
                            return;
                        }
                        (ply.Player as IServerPlayer).SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get("runestory:mindexpand") + Lang.Get("runestory:" + validOptions.First().Code), EnumChatType.Notification);
                        knownspells = knownspells.AddToArray(validOptions.First().Code);
                        ply.WatchedAttributes.SetAttribute(RunestoryMS.RMS_SpellKnowledge, new StringArrayAttribute(knownspells));
                        (ply.Player as IServerPlayer).SetModdata(RunestoryMS.RMS_SpellKnowledge, SerializerUtil.Serialize(knownspells.ToArray()));
                    }
                    else
                    {
                        int failcount = 0;
                        int origCount = validOptions.Count();
                        while (validOptions.Count() > 0 && validOptions.Count() <= origCount)
                        {
                            BaseRuneSpell target = validOptions.ElementAt(api.World.Rand.Next(0,validOptions.Count()));
                            if (!knownspells.Contains(target.Code))
                            {
                                knownspells = knownspells.AddToArray(target.Code);

                                (ply.Player as IServerPlayer).SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get("runestory:mindexpand") + Lang.Get("runestory:"+target.Code), EnumChatType.Notification);
                                ply.WatchedAttributes.SetAttribute(RunestoryMS.RMS_SpellKnowledge, new StringArrayAttribute(knownspells));
                                (ply.Player as IServerPlayer).SetModdata(RunestoryMS.RMS_SpellKnowledge, SerializerUtil.Serialize(knownspells.ToArray()));
                                if (onlyOneSpell)
                                {
                                    break;
                                }
                                continue;
                            }
                            validOptions = validOptions.ToArray().Remove(target);
                            failcount++;
                        }

                        if (failcount >= validOptions.Count())
                        {
                            (ply.Player as IServerPlayer).SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get("runestory:allknown"), EnumChatType.Notification);
                            return;
                        }
                    }
                    (ply.Player as IServerPlayer).SendMessage(GlobalConstants.InfoLogChatGroup,Lang.Get("runestory:crumbletodust"), EnumChatType.Notification);
                    slot.TakeOut(1);
                    slot.MarkDirty();
                }
            }
        }
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            dsc.Append(Lang.Get("runestory:runicresearch", onlyOneSpell ? 1 : "every", tierUnlocked));
            if(inSlot.Itemstack?.Attributes?["spelltounlock"] is not null)
            {
                dsc.Append("\nSpell: " + Lang.Get("runestory:" + inSlot.Itemstack.Attributes.GetString("spelltounlock")));
            }
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        }
    }
}
