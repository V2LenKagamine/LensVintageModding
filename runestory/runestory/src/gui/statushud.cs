using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using ImGuiNET;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using VSImGui.API;

namespace runestory.src.gui
{
    public class TempStatusHud
    {
        private bool isOpen = false;
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
            Entity us = RunestoryMS.runeCApi.World.Player.Entity;

            IEnumerable<KeyValuePair<string,EntityFloatStats>> modifiedstats = us.Stats.Where(stat => stat.Value.ValuesByKey.Where(mod => mod.Key == PlayerTempBuffer.RunetempBuffKey).Any());
            if (modifiedstats.Any())
            {
                if (!isOpen)
                {
                    Open();
                }
                try
                {
                    ImGui.SetNextWindowBgAlpha(0.1f);
                    ImGui.SetNextWindowSizeConstraints(new Vector2(180,20),new Vector2(int.MaxValue, int.MaxValue));
                    ImGui.Begin("Temporary Effects",ImGuiWindowFlags.AlwaysAutoResize);
                    foreach (KeyValuePair<string, EntityFloatStats> entry in modifiedstats)
                    {
                        ImGui.Text(Lang.Get("runestory:" + entry.Key) + ": ");
                        ImGui.SameLine();
                        float val = entry.Value.ValuesByKey[PlayerTempBuffer.RunetempBuffKey].Value;
                        ImGui.Text(string.Format("{0}%%", (int)Math.Floor(val * 100)));
                    }
                }
                catch (Exception e) { RunestoryMS.Runelogger.LogException(EnumLogType.Error, e); }
                finally { ImGui.End(); }
                return CallbackGUIStatus.DontGrabMouse;
            }
            else if (isOpen)
            {
                Close();
            }
            return CallbackGUIStatus.Closed;
        }
    }
}
