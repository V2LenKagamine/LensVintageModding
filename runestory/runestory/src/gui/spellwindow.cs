using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.Common;
using VSImGui.API;

namespace runestory
{
    public class SpellWindow
    {
        private bool isOpen = false;

        private string openTab = "all";

        private string tierTab = "all";

        public string SelectedSpell;
        private RunestoryMS RMS => RunestoryMS.runeCApi.ModLoader.GetModSystem<RunestoryMS>();  //Cursed.

        public void ToggleOpen()
        {
            if (isOpen)
            {
                Close();
            }
            else
            {
                RMS.capi_Runechannel.SendPacket(new CTS_SpellsPls());
                Open();
            }
        }

        public void Open()
        {
            if (isOpen) { return; }
            isOpen = true;

            RMS.runeGuiSys.Show();
        }

        public void Close()
        {
            if (!isOpen) return;
            isOpen = false;
        }

        public CallbackGUIStatus Draw(float dt)
        {
            if (!isOpen) return CallbackGUIStatus.Closed;
            if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            {
                Close();
            }
            Entity us = RunestoryMS.runeCApi.World.Player.Entity;

            us.WatchedAttributes.TryGetAttribute(RunestoryMS.RMS_SpellKnowledge, out IAttribute playerSpells);
            IEnumerable<BaseRuneSpell> validspells = RMS.AllSpells.Where(poss => (playerSpells.GetValue() as string[])?.Contains(poss.Code) ?? false);

            if ((us as EntityPlayer).Player.WorldData.CurrentGameMode == EnumGameMode.Creative) { validspells = RMS.AllSpells; }

            if (openTab == "supp") { validspells = validspells.Where(spell => spell.spellType == "supp"); }
            if (openTab == "dmg") { validspells = validspells.Where(spell => spell.spellType == "dmg"); }
            if (openTab == "util") { validspells = validspells.Where(spell => spell.spellType == "util"); }

            if (tierTab == "t1") { validspells = validspells.Where(spell => spell.spellTier == 1); }
            if (tierTab == "t2") { validspells = validspells.Where(spell => spell.spellTier == 2); }
            if (tierTab == "t3") { validspells = validspells.Where(spell => spell.spellTier == 3); }
            if (tierTab == "t4") { validspells = validspells.Where(spell => spell.spellTier == 4); }
            if (tierTab == "t5") { validspells = validspells.Where(spell => spell.spellTier == 5); }

            if (validspells is null) { return CallbackGUIStatus.Closed; }
            int spellcount = validspells.Count();
            ElementBounds window = RunestoryMS.runeCApi.Gui.WindowBounds;
            ImGui.Begin("Spell Select Window", ImGuiWindowFlags.AlwaysAutoResize);
            try
            {

                Vector2 buttsize = new Vector2(45, 45);
                Vector2 smolsize = new Vector2(100, 25);
                ImGui.BeginChild("Buttons",new Vector2(210,220));
                if (ImGui.Button("All Tiers",smolsize)) { SetTier("all"); }
                ImGui.SameLine();
                if (ImGui.Button("All Spells",smolsize)) { SetTab("all"); }
                if (ImGui.Button("Tier 1", smolsize)) { SetTier("t1"); }
                ImGui.SameLine();
                if (ImGui.Button("Damage", smolsize)) { SetTab("dmg"); }
                if (ImGui.Button("Tier 2", smolsize)) { SetTier("t2"); }
                ImGui.SameLine();
                if (ImGui.Button("Utility", smolsize)) { SetTab("util"); }
                if (ImGui.Button("Tier 3", smolsize)) { SetTier("t3"); }
                ImGui.SameLine();
                if (ImGui.Button("Support", smolsize)) { SetTab("supp"); }
                if (ImGui.Button("Tier 4", smolsize)) { SetTier("t4"); }
                if (ImGui.Button("Tier 5", smolsize)) { SetTier("t5"); }
                ImGui.BeginChild("stats", new Vector2(220, 40));
                ImGui.BulletText(Lang.Get("runestory:magicdamagestat") + (int)(us.Stats.GetBlended(RunestoryMS.RMS_Stat_MagicDamage) * 100f) + "%%");
                ImGui.BulletText(Lang.Get("runestory:runeconsumechance") + (int)Math.Max(((us.Stats.GetBlended(RunestoryMS.RMS_Stat_RuneChance) * 100f) - 100), 0) + "%%");
                ImGui.EndChild();
                ImGui.EndChild();
                ImGui.SameLine();
                ImGui.BeginChild("Spells", new Vector2(480f,(57f * (float)Math.Ceiling(spellcount / 8d))));
                for (int i = 0; i < spellcount; i++)
                {
                    BaseRuneSpell spell = validspells.ElementAt(i);
                    if (i % 8 != 0)
                    {
                        ImGui.SameLine();
                    }
                    bool pressed = ImGui.ImageButton(i.ToString(), RunestoryMS.runeCApi.Render.GetOrLoadTexture("runestory:textures/spellicons/" + spell.imgPath + ".png"), buttsize);
                    string req = "Requires:\n";
                    for (int j = 0; j < spell.Reagents.Count(); j++)
                    {
                        string k = Lang.Get(spell.ReagNames[j]);
                        if (k is not null)
                        {
                            req += k + " x " + spell.Reagents.ElementAt(j).Value.ToString() + "\n";
                        }
                    }
                    if (!ImGui.IsKeyDown(ImGuiKey.LeftShift))
                    {
                        ImGui.SetItemTooltip(Lang.Get("runestory:" + spell.Code) + "\n" + req + $"\nTier: {spell.spellTier}" + "\nHold LEFT SHIFT for info.");
                    }
                    else
                    {
                        ImGui.SetItemTooltip(Lang.Get("runestory:" + spell.langCode));
                    }

                    if (pressed)
                    {
                        onButtonClick(spell.Code);
                    }
                }
                ImGui.EndChild();
            }
            catch (Exception e) { RunestoryMS.Runelogger.LogException(EnumLogType.Error, e); }
            finally { ImGui.End(); }
            return CallbackGUIStatus.GrabMouse;
        }

        public void onButtonClick(string spell)
        {
            RunestoryMS.runeCApi.Network.GetChannel("runespellchannel").SendPacket(new CTS_SelectPacket
            {
                byPlayerID = RunestoryMS.runeCApi.World.Player.Entity.EntityId,
                spellID = spell
            });
            Close();
        }
        public void SetTab(string tab)
        {
            openTab = tab;
        }

        public void SetTier(string Tier)
        {
            tierTab = Tier;
        }
    }
}
