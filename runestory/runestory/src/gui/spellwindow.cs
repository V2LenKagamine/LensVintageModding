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
            us.WatchedAttributes.TryGetAttribute("RMSKnownSpells", out IAttribute playerSpells);
            IEnumerable<BaseRuneSpell> validspells = RMS.AllSpells.Where(poss => (playerSpells.GetValue() as string[])?.Contains(poss.Code) ?? false);

            if ((us as EntityPlayer).Player.WorldData.CurrentGameMode == EnumGameMode.Creative) { validspells = RMS.AllSpells; }

            if (openTab == "supp") { validspells = validspells.Where(spell => spell.spellType == "supp"); }
            if (openTab == "dmg") { validspells = validspells.Where(spell => spell.spellType == "dmg"); }
            if (openTab == "util") { validspells = validspells.Where(spell => spell.spellType == "util"); }

            if (validspells is null) { return CallbackGUIStatus.Closed; }
            int spellcount = validspells.Count();
            ElementBounds window = RunestoryMS.runeCApi.Gui.WindowBounds;
            ImGui.SetNextWindowSize(new Vector2(500f, 85f + (56f * (float)Math.Ceiling(spellcount / 8d))));
            ImGui.Begin("Spell Select Window");
            try
            {
                Vector2 buttsize = new Vector2(45, 45);

                if(ImGui.SmallButton("All Spells")){SetTab("all");}
                ImGui.SameLine();
                if(ImGui.SmallButton("Damage Spells")){SetTab("dmg");}
                ImGui.SameLine();
                if(ImGui.SmallButton("Utility Spells")){SetTab("util"); }
                ImGui.SameLine();
                if (ImGui.SmallButton("Support Spells")) {SetTab("supp"); }

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
                        ImGui.SetItemTooltip(Lang.Get("runestory:" + spell.Code) + "\n" + req + $"\nTier: {spell.spellTier}"+  "\nHold LEFT SHIFT for info.");
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
                ImGui.BulletText(Lang.Get("runestory:magicdamagestat") + (int)(us.Stats.GetBlended(RunestoryMS.RMS_Stat_MagicDamage) * 100f) + "%%");
                ImGui.SameLine();
                ImGui.BulletText(Lang.Get("runestory:runeconsumechance") + (int)Math.Max(((us.Stats.GetBlended(RunestoryMS.RMS_Stat_RuneChance) * 100f) - 100),0) + "%%");

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
    }
}
