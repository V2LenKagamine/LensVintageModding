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

namespace runestory
{
    public class RunicResearch : Item
    {

        public string spellUnlocked = "Fucking Nothing";
        public int tierUnlocked = 0;
        public bool onlyOneSpell = true;

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
                if (byEntity is EntityPlayer ply)
                {

                    string[] knownspells = (string[])ply.WatchedAttributes.GetAttribute(RunestoryMS.RMS_SpellKnowledge).GetValue();
                    if (validOptions.Count() == 1)
                    {
                        if (knownspells.Contains(validOptions.First().Code))
                        {
                            (ply.Player as IServerPlayer).SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get("runestory:knownspell"), EnumChatType.Notification);
                            (ply.Player as IServerPlayer).SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get("runestory:nowallknown"), EnumChatType.Notification);
                            return;
                        }
                        knownspells = knownspells.AddToArray(validOptions.First().Code);
                        ply.WatchedAttributes.SetAttribute(RunestoryMS.RMS_SpellKnowledge, new StringArrayAttribute(knownspells));
                    }
                    else
                    {
                        int failcount = 0;
                        for (int i = 0; i < validOptions.Count(); i++)
                        {

                            BaseRuneSpell target = validOptions.ElementAt(i);
                            if (!knownspells.Contains(target.Code))
                            {
                                knownspells = knownspells.AddToArray(target.Code);

                                (ply.Player as IServerPlayer).SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get("runestory:mindexpand") + Lang.Get("runestory:"+target.Code), EnumChatType.Notification);
                                ply.WatchedAttributes.SetAttribute(RunestoryMS.RMS_SpellKnowledge, new StringArrayAttribute(knownspells));
                                if (onlyOneSpell)
                                {
                                    break;
                                }
                                continue;
                            }
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
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        }
    }
}
