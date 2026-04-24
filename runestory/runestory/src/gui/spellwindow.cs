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
using Vintagestory.API.Util;
using VSImGui.API;

namespace runestory
{
    public class SpellWindow
    {
        private bool isOpen = false;

        public string SelectedSpell;
        private runestoryModSystem RMS => runestoryModSystem.runeCApi.ModLoader.GetModSystem<runestoryModSystem>();  //Cursed.

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
            if(ImGui.IsKeyPressed(ImGuiKey.Escape))
            {
                Close();
            }

            int spellcount = RMS.AllSpells.Count;

            ElementBounds window = runestoryModSystem.runeCApi.Gui.WindowBounds;
            ImGui.SetNextWindowSize(new Vector2(Math.Max(250,20f + (60f * Math.Min(spellcount,8))),40f + (55f * (float)Math.Ceiling(spellcount/8d))));
            ImGui.Begin("Spell Select Window");
            try
            {
                Vector2 buttsize = new Vector2(45, 45);
               
                for(int i = 0; i < spellcount ;i++)
                {
                    BaseRuneSpell spell = RMS.AllSpells[i];
                    if (i % 8 != 0)
                    {
                        ImGui.SameLine();
                    }
                    bool pressed = ImGui.ImageButton(i.ToString(),runestoryModSystem.runeCApi.Render.GetOrLoadTexture("runestory:textures/spellicons/" + spell.imgPath + ".png"), buttsize);
                    string req = "Requires:\n";
                    for (int j = 0; j < spell.ReagNames.Length; j++) 
                    {
                        string k = Lang.Get(spell.ReagNames[j]);
                        if(k is not null)
                        {
                            req += k + " x " + spell.Reagents.ElementAt(j).Value.ToString() + "\n";
                        }
                    }
                    if(!ImGui.IsKeyDown(ImGuiKey.LeftShift))
                    {
                        ImGui.SetItemTooltip(Lang.Get("runestory:" + spell.Code) + "\n" + req + "\nHold LEFT SHIFT for info.");
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
            }
            catch (Exception e) { runestoryModSystem.Runelogger.LogException(EnumLogType.Error, e); }
            finally { ImGui.End(); }
            return CallbackGUIStatus.GrabMouse;
        }

        public void onButtonClick(string spell)
        {
            runestoryModSystem.runeCApi.Network.GetChannel("runespellchannel").SendPacket(new CTS_SelectPacket
            {
                byPlayerID = runestoryModSystem.runeCApi.World.Player.Entity.EntityId,
                spellID = spell
            });
            Close();
        }
    }
}
