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
using VSImGui.API;

namespace runestory
{
    public class SpellWindow
    {
        private bool isOpen = false;

        public string SelectedSpell;
        private runestoryModSystem RMS => runestoryModSystem.runeApi.ModLoader.GetModSystem<runestoryModSystem>();  //Cursed.

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

            ElementBounds window = runestoryModSystem.runeApi.Gui.WindowBounds;
            ImGui.SetNextWindowSize(new Vector2(Math.Max(400f,50f*(spellcount)),17.5f + (50f * (float)Math.Ceiling(spellcount/8d))));
            ImGui.Begin("Spell Select Window");
            try
            {
                for(int i = 0; i< spellcount ;i++)
                {

                    ImGui.SetNextItemWidth(45);
                    bool pressed = ImGui.Button(i.ToString()+1);
                    ImGui.SetItemTooltip(Lang.Get("runestory:" + RMS.AllSpells[i].Code));

                    if (pressed)
                    {
                        onButtonClick(RMS.AllSpells[i].Code);
                    }
                }
            }
            catch (Exception e) { runestoryModSystem.Runelogger.LogException(EnumLogType.Error, e); }
            finally { ImGui.End(); }
            return CallbackGUIStatus.GrabMouse;
        }

        public void onButtonClick(string spell)
        {
            runestoryModSystem.runeApi.Network.GetChannel("runespellchannel").SendPacket(new CTS_SelectPacket
            {
                byPlayerID = runestoryModSystem.runeApi.World.Player.Entity.EntityId,
                spellID = spell
            });
        }
    }
}
