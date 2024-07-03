using Dalamud.Hooking;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Lemegeton.Core
{

    public class FoodSelector
    {

        internal float MinimumTime { get; set; }        
        public uint CurrentlySelected { get; set; } = 0;
        public bool SelectedHQ { get; set; } = false;
        public List<InventoryItem> FoodList { get; set; } = null;
        private bool _firstSeen = false;

        internal State State = null;

        private void RefreshFoodList()
        {
            FoodList = State.GetAllFoodInInventory();
        }

        public bool Cycle()
        {
            if (State.CurrentlyWellFed(MinimumTime) == true)
            {
                return true;
            }
            if (CurrentlySelected == 0)
            {
                return true;
            }
            if (_firstSeen == false)
            {
                RefreshFoodList();
                _firstSeen = true;
            }
            InventoryItem ii = State.FindItemInInventory(CurrentlySelected, SelectedHQ);
            if (ii == null)
            {
                State.Log(State.LogLevelEnum.Debug, null, "Out of food");
                return true;
            }
            State.Log(State.LogLevelEnum.Debug, null, "Using food {0}, hq: {1}", ii.Item.Name, ii.HQ);
            unsafe
            {                   
                ActionManager* am = ActionManager.Instance();
                uint funky = 0;
                switch (ii.Type)
                {
                    case InventoryType.Inventory2: funky += 0x10000; break;
                    case InventoryType.Inventory3: funky += 0x20000; break;
                    case InventoryType.Inventory4: funky += 0x30000; break;
                }
                funky += (uint)ii.Slot;
                am->UseAction(ActionType.Item, (uint)(ii.Item.RowId + (ii.HQ == true ? 1000000 : 0)), 0xE0000000, funky, ActionManager.UseActionMode.None, 0, null);
            }
            return false;
        }

        public void Render(string path)
        {
            string none = I18n.Translate("Misc/None");
            string hq = I18n.Translate("Misc/HQ");
            string selname = "";
            if (CurrentlySelected > 0)
            {
                if (_firstSeen == false)
                {
                    RefreshFoodList();
                    _firstSeen = true;
                }
                var ii = (from ix in FoodList where ix.Item.RowId == CurrentlySelected select ix).FirstOrDefault();
                if (ii == null)
                {
                    CurrentlySelected = 0;
                }
                else
                {
                    selname = ii.Item.Name;
                    if (SelectedHQ == true)
                    {
                        selname += " (" + hq + ")";
                    }
                }
            }
            if (CurrentlySelected == 0)
            {
                selname = none;
            }
            ImGui.PushItemWidth(200.0f);
            ImGui.Text(I18n.Translate(path));
            if (ImGui.BeginCombo("##" + path, selname) == true)
            {
                if (_firstSeen == false)
                {
                    RefreshFoodList();
                    _firstSeen = true;
                }
                if (ImGui.Selectable(none, CurrentlySelected == 0) == true)
                {
                    CurrentlySelected = 0;
                }
                foreach (InventoryItem ii in FoodList)
                {
                    string itemname = ii.Item.Name;
                    string dispname = itemname;
                    if (ii.HQ == true)
                    {
                        dispname += " (" + hq + ")";
                    }
                    bool selected = (
                        (CurrentlySelected == ii.Item.RowId)
                        &&
                        (
                            (ii.HQ == true && SelectedHQ == true)
                            ||
                            (ii.HQ == false && SelectedHQ == false)
                        )
                    );
                    if (ImGui.Selectable(dispname, selected) == true)
                    {
                        CurrentlySelected = ii.Item.RowId;
                        SelectedHQ = ii.HQ;
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.PopItemWidth();
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            string ico = FontAwesomeIcon.SyncAlt.ToIconString();
            if (ImGui.Button(ico) == true)
            {
                RefreshFoodList();
            }
            ImGui.PopFont();
        }

        public string Serialize()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(String.Format("CurrentlySelected={0};", CurrentlySelected));
            sb.Append(String.Format("SelectedHQ={0};", SelectedHQ));
            return sb.ToString();
        }

        public void Deserialize(string data)
        {
            string[] items = data.Split(";");
            foreach (string item in items)
            {
                string[] kp = item.Split("=", 2);
                switch (kp[0])
                {
                    case "CurrentlySelected":
                        {
                            CurrentlySelected = uint.Parse(kp[1]);
                            break;
                        }
                    case "SelectedHQ":
                        {
                            SelectedHQ = bool.Parse(kp[1]);
                            break;
                        }
                }
            }
        }

    }

}
