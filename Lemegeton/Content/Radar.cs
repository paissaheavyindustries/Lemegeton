using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;
using ImGuiScene;
using Lemegeton.ContentCategory;
using Lemegeton.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static Lemegeton.Core.State;
using GameObject = Dalamud.Game.ClientState.Objects.Types.GameObject;
using GameObjectPtr = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace Lemegeton.Content
{

    public class Radar : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

        #if !SANS_GOETIA

        public class DrawEnemies : Core.ContentItem
        {

            public override FeaturesEnum Features => FeaturesEnum.Drawing;

            [AttributeOrderNumber(1000)]
            public Vector4 ObjectColor { get; set; } = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);

            [AttributeOrderNumber(2000)]
            public bool DrawLine { get; set; } = true;

            [AttributeOrderNumber(3000)]
            public bool ShowNames { get; set; } = true;
            [AttributeOrderNumber(3001)]
            public bool IncludeDistance { get; set; } = true;
            [AttributeOrderNumber(3002)]
            public Vector4 TextColor { get; set; } = new Vector4(1.0f, 0.8f, 0.8f, 1.0f);

            [AttributeOrderNumber(4000)]
            public bool ShowNameBg { get; set; } = true;
            [AttributeOrderNumber(4001)]
            public Vector4 BgColor { get; set; } = new Vector4(0.0f, 0.0f, 0.0f, 0.7f);

            protected override bool ExecutionImplementation()
            {
                ImDrawListPtr draw;
                if (_state.StartDrawing(out draw) == false)
                {
                    return false;
                }
                Vector3 me = _state.cs.LocalPlayer.Position;
                Vector2 pt = new Vector2();
                me = _state.plug._ui.TranslateToScreen(me.X, me.Y, me.Z);
                float defSize = ImGui.GetFontSize();
                float mul = 20.0f / defSize;
                foreach (GameObject go in _state.ot)
                {
                    if (go.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc && go.SubKind == 5 && go.IsDead == false)
                    {
                        BattleChara bc = (BattleChara)go;
                        if (bc.CurrentHp > 0 && (_state.GetStatusFlags(bc) & Dalamud.Game.ClientState.Objects.Enums.StatusFlags.Hostile) != 0 && bc.ClassJob.Id == 0)
                        {
                            double dist = Vector3.Distance(_state.cs.LocalPlayer.Position, go.Position);
                            Vector3 temp = _state.plug._ui.TranslateToScreen(go.Position.X, go.Position.Y, go.Position.Z);
                            pt.Y = temp.Y + 10.0f;
                            string name = IncludeDistance == true ? String.Format("{0} ({1:0})", go.Name.ToString(), dist) : String.Format("{0}", go.Name.ToString());
                            Vector2 sz = ImGui.CalcTextSize(name);
                            sz.X *= mul;
                            sz.Y *= mul;
                            pt.X = temp.X - (sz.X / 2.0f);
                            if (ShowNames == true)
                            {
                                if (ShowNameBg == true)
                                {
                                    draw.AddRectFilled(
                                        new Vector2(pt.X - 5, pt.Y - 5),
                                        new Vector2(pt.X + sz.X + 5, pt.Y + sz.Y + 5),
                                        ImGui.GetColorU32(BgColor),
                                        1.0f
                                    );
                                }
                                draw.AddText(
                                    ImGui.GetFont(),
                                    20.0f,
                                    pt,
                                    ImGui.GetColorU32(TextColor),
                                    name
                                );
                            }
                            if (dist < 50.0f && DrawLine == true && (_state.GetStatusFlags(bc) & Dalamud.Game.ClientState.Objects.Enums.StatusFlags.OffhandOut) == 0)
                            {
                                draw.AddLine(
                                    new Vector2(temp.X, temp.Y),
                                    new Vector2(me.X, me.Y),
                                    ImGui.GetColorU32(ObjectColor),
                                    dist < 20.0f ? 10.0f : (dist < 30.0f ? 5.0f : 2.0f)
                                );
                            }
                            draw.AddCircleFilled(
                                new Vector2(temp.X, temp.Y),
                                10.0f,
                                ImGui.GetColorU32(ObjectColor),
                                20
                            );
                        }
                    }
                }
                return true;
            }

            public DrawEnemies(State state) : base(state)
            {
                Enabled = false;
            }

        }

        public class DrawPlayers : Core.ContentItem
        {

            public override FeaturesEnum Features => FeaturesEnum.Drawing;

            [AttributeOrderNumber(1000)]
            public Vector4 ObjectColor { get; set; } = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

            [AttributeOrderNumber(2000)]
            public bool ShowNames { get; set; } = true;
            [AttributeOrderNumber(2001)]
            public bool IncludeDistance { get; set; } = true;
            [AttributeOrderNumber(2002)]
            public Vector4 TextColor { get; set; } = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

            [AttributeOrderNumber(3000)]
            public bool ShowNameBg { get; set; } = true;
            [AttributeOrderNumber(3001)]
            public Vector4 BgColor { get; set; } = new Vector4(0.0f, 0.0f, 0.0f, 0.7f);

            [AttributeOrderNumber(4000)]
            public bool ShowJobIcon { get; set; } = true;
            [AttributeOrderNumber(4001)]
            public bool ShowHpBar { get; set; } = false;

            protected override bool ExecutionImplementation()
            {
                ImDrawListPtr draw;
                if (_state.StartDrawing(out draw) == false)
                {
                    return false;
                }
                uint myid = _state.cs.LocalPlayer.ObjectId;
                Vector3 me = _state.cs.LocalPlayer.Position;
                Vector2 pt = new Vector2();
                me = _state.plug._ui.TranslateToScreen(me.X, me.Y, me.Z);
                float defSize = ImGui.GetFontSize();
                float mul = 20.0f / defSize;
                foreach (GameObject go in _state.ot)
                {
                    if (go.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player)
                    {
                        double dist = Vector3.Distance(_state.cs.LocalPlayer.Position, go.Position);
                        Vector3 temp = _state.plug._ui.TranslateToScreen(go.Position.X, go.Position.Y, go.Position.Z);
                        pt.Y = temp.Y + 10.0f;
                        string name = IncludeDistance == true && go.ObjectId != myid ? String.Format("{0} ({1:0})", go.Name.ToString(), dist) : String.Format("{0}", go.Name.ToString());
                        Vector2 sz = ImGui.CalcTextSize(name);
                        sz.X *= mul;
                        sz.Y *= mul;
                        pt.X = temp.X - (sz.X / 2.0f);
                        float bottomy = pt.Y;
                        TextureWrap jobicon = null;
                        if (ShowJobIcon == true)
                        {
                            Character chara = go as Character;
                            jobicon = _state.plug._ui.GetJobIcon(chara.ClassJob.Id);
                        }
                        if (ShowNames == true)
                        {
                            if (jobicon != null)
                            {
                                pt = new Vector2(pt.X + (jobicon.Width / 2.0f), pt.Y);
                            }
                            if (ShowNameBg == true)
                            {
                                bottomy = pt.Y + sz.Y + 5.0f;
                                draw.AddRectFilled(
                                    new Vector2(pt.X - 5.0f, pt.Y - 5.0f),
                                    new Vector2(pt.X + sz.X + 5.0f, bottomy),
                                    ImGui.GetColorU32(BgColor),
                                    1.0f
                                );
                            }
                            draw.AddText(
                                ImGui.GetFont(),
                                20.0f,
                                pt,
                                ImGui.GetColorU32(TextColor),
                                name
                            );
                        }
                        if (ShowJobIcon == true && jobicon != null)
                        {
                            if (ShowNames == false)
                            {
                                bottomy = pt.Y - 5.0f + jobicon.Height;
                                draw.AddImage(
                                    jobicon.ImGuiHandle,
                                    new Vector2(temp.X - (jobicon.Width / 2.0f), pt.Y - 5.0f),
                                    new Vector2(temp.X + (jobicon.Width / 2.0f), bottomy)
                                );
                            }
                            else
                            {
                                bottomy = pt.Y + (sz.Y / 2.0f) + (jobicon.Height / 2.0f);
                                draw.AddImage(
                                    jobicon.ImGuiHandle,
                                    new Vector2(pt.X - jobicon.Width - 5.0f, pt.Y + (sz.Y / 2.0f) - (jobicon.Height / 2.0f)),
                                    new Vector2(pt.X - 5.0f, bottomy)
                                );
                            }
                        }
                        if (ShowHpBar == true)
                        {
                            BattleChara bc = go as BattleChara;
                            if (bc.CurrentHp > 0)
                            {
                                float barw = sz.X + (ShowNameBg != true ? 10.0f : 0.0f) + (ShowJobIcon == true && jobicon != null ? jobicon.Width : 0.0f);
                                float leftx = temp.X - 50.0f;
                                float barst = bottomy;
                                if (bc.CurrentHp == bc.MaxHp)
                                {
                                    draw.AddRectFilled(
                                        new Vector2(leftx, barst),
                                        new Vector2(leftx + 100.0f, barst + 7.0f),
                                        ImGui.GetColorU32(new Vector4(0.0f, 1.0f, 1.0f, 1.0f))
                                    );
                                }
                                else
                                {
                                    float div = (bc.CurrentHp / (float)bc.MaxHp) * 100.0f;
                                    draw.AddRectFilled(
                                        new Vector2(leftx + div, barst),
                                        new Vector2(leftx + 100.0f, barst + 7.0f),
                                        ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 1.0f))
                                    );
                                    Vector4 hpcol;
                                    if (div > 50.0f)
                                    {
                                        hpcol = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
                                    }
                                    else if (div > 30.0f)
                                    {
                                        hpcol = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
                                    }
                                    else if (div > 15.0f)
                                    {
                                        float meow = DateTime.Now.Millisecond / 100.0f;
                                        hpcol = new Vector4(1.0f, 0.5f, 0.0f, 0.5f + (float)Math.Abs(Math.Cos(meow) / 2.0f));
                                    }
                                    else
                                    {
                                        float meow = DateTime.Now.Millisecond / 100.0f;
                                        hpcol = new Vector4(1.0f, 0.0f, 0.0f, (float)Math.Abs(Math.Cos(meow)));
                                    }
                                    draw.AddRectFilled(
                                        new Vector2(leftx, barst),
                                        new Vector2(leftx + div, barst + 7.0f),
                                        ImGui.GetColorU32(hpcol)
                                    );
                                }
                            }
                        }
                        draw.AddCircleFilled(
                            new Vector2(temp.X, temp.Y),
                            10.0f,
                            ImGui.GetColorU32(ObjectColor),
                            20
                        );
                    }
                }
                return true;
            }

            public DrawPlayers(State state) : base(state)
            {
                Enabled = false;
            }

        }

        public class DrawGatheringPoint : Core.ContentItem
        {

            public override FeaturesEnum Features => FeaturesEnum.Drawing;

            [AttributeOrderNumber(1000)]
            public Vector4 ObjectColor { get; set; } = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
            [AttributeOrderNumber(1001)]
            public bool OnlyOnGatherers { get; set; } = true;

            [AttributeOrderNumber(2000)]
            public bool ShowNames { get; set; } = true;
            [AttributeOrderNumber(2001)]
            public bool IncludeDistance { get; set; } = true;
            [AttributeOrderNumber(2002)]
            public Vector4 TextColor { get; set; } = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);

            [AttributeOrderNumber(3000)]
            public bool ShowNameBg { get; set; } = true;
            [AttributeOrderNumber(3001)]
            public Vector4 BgColor { get; set; } = new Vector4(0.0f, 0.0f, 0.0f, 0.7f);

            [AttributeOrderNumber(4000)]
            public bool ShowHidden { get; set; } = false;
            [AttributeOrderNumber(4001)]
            public Vector4 HiddenColor { get; set; } = new Vector4(1.0f, 0.529f, 0.0f, 1.0f);
            [AttributeOrderNumber(4002)]
            public bool ShowNameOnHidden { get; set; } = true;
            [AttributeOrderNumber(4003)]
            public bool ShowNameBgOnHidden { get; set; } = true;

            protected override bool ExecutionImplementation()
            {
                ImDrawListPtr draw;
                if (_state.StartDrawing(out draw) == false)
                {
                    return false;
                }
                if (OnlyOnGatherers == true)
                {
                    uint myjob = _state.cs.LocalPlayer.ClassJob.Id;
                    if (myjob != 16 && myjob != 17)
                    {
                        return false;
                    }
                }
                uint myid = _state.cs.LocalPlayer.ObjectId;
                Vector3 origme = _state.cs.LocalPlayer.Position;
                Vector2 pt = new Vector2();
                Vector3 me = _state.plug._ui.TranslateToScreen(origme.X, origme.Y, origme.Z);
                float defSize = ImGui.GetFontSize();
                float mul = 20.0f / defSize;
                foreach (GameObject go in _state.ot)
                {
                    bool isTargettable;
                    unsafe
                    {
                        GameObjectPtr* gop = (GameObjectPtr*)go.Address;
                        isTargettable = gop->GetIsTargetable();
                    }
                    bool isHidden = (isTargettable == false);
                    if (go.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.GatheringPoint && (isHidden == false || ShowHidden == true))
                    {
                        double dist = Vector3.Distance(origme, go.Position);
                        Vector3 temp = _state.plug._ui.TranslateToScreen(go.Position.X, go.Position.Y, go.Position.Z);
                        pt.Y = temp.Y + 10.0f;
                        string ntmp = go.Name.ToString();
                        string name = IncludeDistance == true && go.ObjectId != myid ? String.Format("{0} ({1:0})", ntmp, dist) : String.Format("{0}", ntmp);
                        Vector2 sz = ImGui.CalcTextSize(name);
                        sz.X *= mul;
                        sz.Y *= mul;
                        pt.X = temp.X - (sz.X / 2.0f);
                        if (ShowNames == true && (isHidden == false || ShowNameOnHidden == true))
                        {
                            if (ShowNameBg == true && (isHidden == false || ShowNameBgOnHidden == true))
                            {
                                draw.AddRectFilled(
                                    new Vector2(pt.X - 5, pt.Y - 5),
                                    new Vector2(pt.X + sz.X + 5, pt.Y + sz.Y + 5),
                                    ImGui.GetColorU32(BgColor),
                                    1.0f
                                );
                            }
                            draw.AddText(
                                ImGui.GetFont(),
                                20.0f,
                                pt,
                                ImGui.GetColorU32(isHidden == false ? TextColor : HiddenColor),
                                name
                            );
                        }
                        draw.AddCircleFilled(
                            new Vector2(temp.X, temp.Y),
                            10.0f,
                            ImGui.GetColorU32(isHidden == false ? ObjectColor : HiddenColor),
                            20
                        );
                    }
                }
                return true;
            }

            public DrawGatheringPoint(State state) : base(state)
            {
                Enabled = false;
            }

        }

        #endif

        public class AlertFinder : Core.ContentItem
        {

            public override FeaturesEnum Features
            {
                get
                {
                    return FeaturesEnum.Drawing | (SoundAlert == true ? FeaturesEnum.Sound : FeaturesEnum.None);
                }
            }

            public class Entry
            {

                public enum EntryTypeEnum
                {
                    Custom,
                    SRank,
                    ARank,
                    BRank,
                    RareAnimal,
                }

                public ObjectKind Kind { get; set; }
                public string Name { get; set; } = "";
                public bool IsRegex { get; set; } = false;
                public bool Selected { get; set; } = false;
                public DateTime LastSeen { get; set; } = DateTime.MinValue;
                public ushort Territory { get; set; } = 0;
                public uint NameId { get; set; } = 0;                
                public EntryTypeEnum Type { get; set; } = EntryTypeEnum.Custom;

                internal Regex Regex = null;


                public string Serialize()
                {
                    long time;
                    if (LastSeen == DateTime.MinValue)
                    {
                        time = 0;
                    }
                    else
                    {
                        DateTimeOffset dto = new DateTimeOffset(LastSeen);
                        time = dto.ToUnixTimeSeconds();
                    }
                    return String.Format("Kind={0};IsRegex={1};Name={2};LastSeen={3}", Kind.ToString(), IsRegex, Plugin.Base64Encode(Name), time);
                }

                public void Deserialize(string data)
                {
                    string[] temp = data.Split(";");
                    foreach (string t in temp)
                    {
                        string[] item = t.Split("=", 2);
                        switch (item[0])
                        {
                            case "Kind":
                                Kind = (ObjectKind)Enum.Parse(typeof(ObjectKind), item[1]);
                                break;
                            case "IsRegex":
                                IsRegex = bool.Parse(item[1]);
                                break;
                            case "Name":
                                Name = Plugin.Base64Decode(item[1]);
                                break;
                            case "LastSeen":
                                long time = long.Parse(item[1]);
                                if (time > 0)
                                {
                                    DateTimeOffset dto = DateTimeOffset.FromUnixTimeSeconds(time);
                                    LastSeen = dto.LocalDateTime;
                                }
                                break;
                        }
                        if (IsRegex == true)
                        {
                            Regex = new Regex(Name);
                        }
                    }
                }

            }

            public class AlertEditor : CustomPropertyInterface
            {

                internal List<Entry> entries { get; set; }
                internal State state;

                private string inputbuffer = "";
                private ObjectKind selKind = ObjectKind.Player;

                public override void Deserialize(string data)
                {
                    if (data.Length == 0)
                    {
                        return;
                    }
                    string[] temp = data.Split(",");
                    foreach (string t in temp)
                    {                        
                        Entry e = new Entry();
                        e.Deserialize(t);
                        entries.Add(e);
                    }
                }

                public override string Serialize()
                {
                    List<string> items = new List<string>();
                    foreach (Entry e in entries)
                    {
                        items.Add(e.Serialize());
                    }
                    return String.Join(",", items);
                }

                private void SortEntries()
                {
                    entries.Sort((a, b) => {
                        int i = a.Kind.CompareTo(b.Kind);
                        if (i != 0)
                        {
                            return i;
                        }
                        return a.Name.CompareTo(b.Name);
                    });
                }

                private void AcceptInput(ObjectKind kind, string name)
                {
                    name = name.Trim();
                    if (name.Length > 0)
                    {
                        if ((from ix in entries where String.Compare(ix.Name, name, true) == 0 && ix.Kind == kind select ix).Count() > 0)
                        {
                            return;
                        }

                        Entry e = new Entry() { Kind = kind, IsRegex = false, Name = name, Territory = 0 };
                        entries.Add(e);
                        SortEntries();
                    }
                }

                private void RemoveSelected()
                {
                    var toRem = (from ix in entries where ix.Selected == true select ix).ToList();
                    foreach (Entry e in toRem)
                    {
                        entries.Remove(e);
                    }
                }

                private void ClearAll()
                {
                    entries.Clear();
                }

                public override void RenderEditor(string path)
                {
                    string proptr = I18n.Translate(path);
                    ImGui.Text(proptr);
                    int numsel = 0;
                    if (entries.Count == 0)
                    {
                        ImGui.BeginDisabled();
                        ImGui.BeginChildFrame(1, new Vector2(ImGui.GetContentRegionAvail().X, 150.0f));
                        ImGui.EndChildFrame();
                        ImGui.EndDisabled();
                    }
                    else
                    {
                        ImGui.BeginChildFrame(1, new Vector2(ImGui.GetContentRegionAvail().X, 150.0f));
                        foreach (Entry e in entries)
                        {
                            string label = String.Format("[{0}] {1} ({2}: {3})", 
                                I18n.Translate("ObjectKind/" + e.Kind.ToString()), 
                                e.Name,
                                I18n.Translate("Content/Miscellaneous/Radar/AlertFinder/LastSeen"),
                                e.LastSeen != DateTime.MinValue ? e.LastSeen : I18n.Translate("Content/Miscellaneous/Radar/AlertFinder/Never")
                            );
                            if (ImGui.Selectable(label, e.Selected) == true)
                            {
                                e.Selected = (e.Selected == false);
                            }
                            if (e.Selected == true)
                            {
                                numsel++;
                            }
                        }
                        ImGui.EndChildFrame();
                    }
                    ImGui.PushItemWidth(150.0f);
                    string selname = I18n.Translate("ObjectKind/" + selKind.ToString());
                    if (ImGui.BeginCombo("##Kind" + proptr, selname) == true)
                    {
                        foreach (string name in Enum.GetNames(typeof(ObjectKind)))
                        {
                            string estr = I18n.Translate("ObjectKind/" + name);
                            if (ImGui.Selectable(estr, String.Compare(selname, estr) == 0) == true)
                            {
                                ObjectKind newKind = (ObjectKind)Enum.Parse(typeof(ObjectKind), name);
                                selKind = newKind;
                            }
                        }
                        ImGui.EndCombo();
                    }
                    ImGui.PopItemWidth();
                    ImGui.SameLine();
                    ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
                    unsafe
                    {
                        if (ImGui.InputText("##Add" + proptr, ref inputbuffer, 256, ImGuiInputTextFlags.EnterReturnsTrue) == true)
                        {
                            AcceptInput(selKind, inputbuffer);
                            inputbuffer = "";
                        }
                    }
                    ImGui.PopItemWidth();
                    if (ImGui.Button(I18n.Translate(path + "/AddNew")) == true)
                    {
                        AcceptInput(selKind, inputbuffer);
                        inputbuffer = "";
                    }
                    ImGui.SameLine();
                    if (numsel == 0)
                    {
                        ImGui.BeginDisabled();
                    }
                    if (ImGui.Button(I18n.Translate(path + "/Remove")) == true)
                    {
                        RemoveSelected();
                    }
                    if (numsel == 0)
                    {
                        ImGui.EndDisabled();
                    }
                    ImGui.SameLine();
                    if (entries.Count == 0)
                    {
                        ImGui.BeginDisabled();
                    }
                    if (ImGui.Button(I18n.Translate(path + "/Clear")) == true)
                    {
                        ClearAll();
                    }
                    if (entries.Count == 0)
                    {
                        ImGui.EndDisabled();
                    }
                }

            }

            internal List<Entry> entries;

            [AttributeOrderNumber(1000)]
            public bool IncludeRankS { get; set; } = true;
            [AttributeOrderNumber(1001)]
            public Vector4 SRankColor { get; set; } = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
            [AttributeOrderNumber(1010)]
            public bool IncludeRankA { get; set; } = true;
            [AttributeOrderNumber(1011)]
            public Vector4 ARankColor { get; set; } = new Vector4(1.0f, 0.5f, 0.0f, 1.0f);
            [AttributeOrderNumber(1020)]
            public bool IncludeRankB { get; set; } = true;
            [AttributeOrderNumber(1021)]
            public Vector4 BRankColor { get; set; } = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
            [AttributeOrderNumber(1030)]
            public bool IncludeIslandRare { get; set; } = true;
            [AttributeOrderNumber(1031)]
            public Vector4 RareAnimalColor { get; set; } = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);

            [AttributeOrderNumber(2000)]
            public AlertEditor LookFor { get; set; }
            [AttributeOrderNumber(2001)]
            public Vector4 ObjectColor { get; set; } = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);

            [AttributeOrderNumber(3000)]
            public bool SoundAlert { get; set; }

            [AttributeOrderNumber(5000)]
            public bool ShowNames { get; set; } = true;
            [AttributeOrderNumber(5001)]
            public bool IncludeDistance { get; set; } = true;
            [AttributeOrderNumber(5002)]
            public Vector4 TextColor { get; set; } = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

            [AttributeOrderNumber(6000)]
            public bool ShowNameBg { get; set; } = true;
            [AttributeOrderNumber(6001)]
            public Vector4 BgColor { get; set; } = new Vector4(0.0f, 0.0f, 0.0f, 0.7f);

            private List<Entry> _defaultsIsland = new List<Entry>()
            {
                // Alkonost
                new Entry() { Type = Entry.EntryTypeEnum.RareAnimal, Kind = ObjectKind.BattleNpc, Territory = 1055, NameId = 12462 },
                // alligator
                new Entry() { Type = Entry.EntryTypeEnum.RareAnimal, Kind = ObjectKind.BattleNpc, Territory = 1055, NameId = 11460 },
                // Amethyst Spriggan
                new Entry() { Type = Entry.EntryTypeEnum.RareAnimal, Kind = ObjectKind.BattleNpc, Territory = 1055, NameId = 12457 },
                // Apkallu of Paradise
                new Entry() { Type = Entry.EntryTypeEnum.RareAnimal, Kind = ObjectKind.BattleNpc, Territory = 1055, NameId = 11448 },
                // Beachcomb
                new Entry() { Type = Entry.EntryTypeEnum.RareAnimal, Kind = ObjectKind.BattleNpc, Territory = 1055, NameId = 11459 },
                // black chocobo
                new Entry() { Type = Entry.EntryTypeEnum.RareAnimal, Kind = ObjectKind.BattleNpc, Territory = 1055, NameId = 6942 },
                // Boar of Paradise
                new Entry() { Type = Entry.EntryTypeEnum.RareAnimal, Kind = ObjectKind.BattleNpc, Territory = 1055, NameId = 12459 },
                // dodo of paradise
                new Entry() { Type = Entry.EntryTypeEnum.RareAnimal, Kind = ObjectKind.BattleNpc, Territory = 1055, NameId = 11451 },
                // Funguar
                new Entry() { Type = Entry.EntryTypeEnum.RareAnimal, Kind = ObjectKind.BattleNpc, Territory = 1055, NameId = 12461 },
                // glyptodon
                new Entry() { Type = Entry.EntryTypeEnum.RareAnimal, Kind = ObjectKind.BattleNpc, Territory = 1055, NameId = 10173 },
                // Gold Back
                new Entry() { Type = Entry.EntryTypeEnum.RareAnimal, Kind = ObjectKind.BattleNpc, Territory = 1055, NameId = 2769 },
                // goobbue
                new Entry() { Type = Entry.EntryTypeEnum.RareAnimal, Kind = ObjectKind.BattleNpc, Territory = 1055, NameId = 353 },
                // grand buffalo
                new Entry() { Type = Entry.EntryTypeEnum.RareAnimal, Kind = ObjectKind.BattleNpc, Territory = 1055, NameId = 11455 },
                // Griffin
                new Entry() { Type = Entry.EntryTypeEnum.RareAnimal, Kind = ObjectKind.BattleNpc, Territory = 1055, NameId = 12313 },
                // Island Billy
                new Entry() { Type = Entry.EntryTypeEnum.RareAnimal, Kind = ObjectKind.BattleNpc, Territory = 1055, NameId = 11457 },
                // Island Stag
                new Entry() { Type = Entry.EntryTypeEnum.RareAnimal, Kind = ObjectKind.BattleNpc, Territory = 1055, NameId = 11453 },
                // Lemur
                new Entry() { Type = Entry.EntryTypeEnum.RareAnimal, Kind = ObjectKind.BattleNpc, Territory = 1055, NameId = 6 },
                // Morbol Seedling
                new Entry() { Type = Entry.EntryTypeEnum.RareAnimal, Kind = ObjectKind.BattleNpc, Territory = 1055, NameId = 11532 },
                // Ornery Karakul
                new Entry() { Type = Entry.EntryTypeEnum.RareAnimal, Kind = ObjectKind.BattleNpc, Territory = 1055, NameId = 795 },
                // paissa
                new Entry() { Type = Entry.EntryTypeEnum.RareAnimal, Kind = ObjectKind.BattleNpc, Territory = 1055, NameId = 3499 },
                // Star Marmot
                new Entry() { Type = Entry.EntryTypeEnum.RareAnimal, Kind = ObjectKind.BattleNpc, Territory = 1055, NameId = 262 },
                // Tiger of Paradise
                new Entry() { Type = Entry.EntryTypeEnum.RareAnimal, Kind = ObjectKind.BattleNpc, Territory = 1055, NameId = 12314 },
                // Twinklefleece
                new Entry() { Type = Entry.EntryTypeEnum.RareAnimal, Kind = ObjectKind.BattleNpc, Territory = 1055, NameId = 11458 },
                // Weird Spriggan
                new Entry() { Type = Entry.EntryTypeEnum.RareAnimal, Kind = ObjectKind.BattleNpc, Territory = 1055, NameId = 12460 },
                // Wild Boar
                new Entry() { Type = Entry.EntryTypeEnum.RareAnimal, Kind = ObjectKind.BattleNpc, Territory = 1055, NameId = 12458 },
                // Yellow Coblyn
                new Entry() { Type = Entry.EntryTypeEnum.RareAnimal, Kind = ObjectKind.BattleNpc, Territory = 1055, NameId = 11450 },
            };

            private List<Entry> _defaultsSRanks = new List<Entry>()
            {
                // Aglaope
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 8653 },
                // Agrippa the Mighty
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 2969 },
                // Armstrong
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 10619 },
                // Bird of Paradise
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 4378 },
                // Bone Crawler
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 5988 },
                // Bonnacon
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 2965 },
                // Brontes
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 2958 },
                // Burfurlur the Canny
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 10617 },
                // Chernobog
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 2967 },
                // Croakadile
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 2963 },
                // Croque-mitaine
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 2962 },
                // forgiven gossip (spawn trigger)
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 8916 },
                // forgiven pedantry (spawn trigger)
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 8910 },
                // forgiven rebellion
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 8915 },
                // Gamma
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 5985 },
                // Gandarewa
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 4377 },
                // the Garlok
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 2964 },
                // Gunitt
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 8895 },
                // Ixtab
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 8890 },
                // kaiser behemoth
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 4374 },
                // Ker
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 10615 },
                // Ker shroud (spawn trigger)
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 10616 },
                // Laideronnette
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 2953 },
                // Lampalagua
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 2959 },
                // Leucrotta
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 4380 },
                // mindflayer
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 2955 },
                // Minhocao
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 2961 },
                // Nandi
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 2966 },
                // Narrow-rift
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 10622 },
                // Nunyunuwi
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 2960 },
                // Okina
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 5984 },
                // Ophioneus
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 10621 },
                // Orghana
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 5986 },
                // the Pale Rider
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 4376 },
                // Ruminator
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 10620 },
                // Safat
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 2968 },
                // Salt and Light
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 5989 },
                // Senmurv
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 4375 },
                // sphatika
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 10618 },
                // Tarchia
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 8900 },
                // Thousand-cast Theda
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 2956 },
                // Tyger
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 8905 },
                // Udumbara
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 5987 },
                // Wulgaru
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 2954 },
                // Zona Seeker
                new Entry() { Type = Entry.EntryTypeEnum.SRank, Kind = ObjectKind.BattleNpc, NameId = 2957 },
            };

            private List<Entry> _defaultsARanks = new List<Entry>()
            {
                // Aegeiros
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 10628 },
                // Agathos
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 4369 },
                // Alectryon
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 2940 },
                // Angada
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 5999 },
                // Aqrabuamelu
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 5993 },
                // Arch-Eta
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 10634 },
                // Baal
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 8897 },
                // Bune
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 4368 },
                // Campacti
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 4372 },
                // Cornu
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 2950 },
                // Dalvag's Final Flame
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 2944 },
                // Enkelados
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 4370 },
                // Erle
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 5991 },
                // Fan Ail
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 10633 },
                // Forneus
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 2936 },
                // Funa Yurei
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 5996 },
                // Gajasura
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 5998 },
                // Ghede Ti Malice
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 2938 },
                // Girimekhala
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 6000 },
                // Girtab
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 2939 },
                // Grassman
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 8892 },
                // Gurangatch
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 10631 },
                // Hellsclaw
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 2947 },
                // hulder
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 10624 },
                // Huracan
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 8912 },
                // Kurrea
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 2952 },
                // Li'l Murderer
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 8911 },
                // Lord of the Wyverns
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 4365 },
                // Luminare
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 5995 },
                // lunatender queen
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 10629 },
                // Lyuba
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 4363 },
                // Maahes
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 2942 },
                // Mahisha
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 5994 },
                // Maliktender
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 8901 },
                // Marberry
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 2949 },
                // Marraco
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 2951 },
                // Melt
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 2937 },
                // Minerva
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 10627 },
                // Mirka
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 4362 },
                // mousse princess
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 10630 },
                // the mudman
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 8654 },
                // Nahn
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 2948 },
                // Nariphon
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 8907 },
                // Nuckelavee
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 8906 },
                // O Poorest Pauldia
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 8655 },
                // Oni Yumemi
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 5997 },
                // Orcus
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 5990 },
                // petalodus
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 10632 },
                // Pylraster
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 4364 },
                // Rusalka
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 8896 },
                // Sabotender Bailarina
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 2941 },
                // Sisiutl
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 4371 },
                // Slipkinx Steeljoints
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 4366 },
                // stench blossom
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 4373 },
                // Stolas
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 4367 },
                // Storsie
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 10623 },
                // Sugaar
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 8902 },
                // Sugriva
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 10626 },
                // Sum
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 6001 },
                // Supay
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 8891 },
                // Unktehi
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 2946 },
                // Vochstein
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 5992 },
                // Vogaal Ja
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 2945 },
                // Yilan
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 10625 },
                // Zanig'oh
                new Entry() { Type = Entry.EntryTypeEnum.ARank, Kind = ObjectKind.BattleNpc, NameId = 2943 },
            };

            private List<Entry> _defaultsBRanks = new List<Entry>()
            {
                // Albin the Ashen
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 2926 },
                // Alteci
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 4350 },
                // Aswang
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 6007 },
                // Barbastelle
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 2929 },
                // Coquecigrue
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 8913 },
                // daphnia magna
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 10641 },
                // Dark Helmet
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 2931 },
                // Deacon
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 8899 },
                // Deidar
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 6004 },
                // Domovoi
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 8656 },
                // Emperor's rose
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 10640 },
                // false gigantopithecus
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 4355 },
                // Flame Sergeant Dalvag
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 2927 },
                // Gatling
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 2925 },
                // Gauki Strongblade
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 6002 },
                // genesis rock
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 10642 },
                // Gilshs Aath Swiftclaw
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 8898 },
                // Gnath cometdrone
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 4352 },
                // green Archon
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 10635 },
                // Guhuo Niao
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 6003 },
                // Gwas-y-neidr
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 6010 },
                // Gyorai Quickstrike
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 6005 },
                // Indomitable
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 8914 },
                // Iravati
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 10638 },
                // Itzpapalotl
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 8909 },
                // Kiwa
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 6013 },
                // Kreutzet
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 4351 },
                // Kurma
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 6006 },
                // La Velue
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 8908 },
                // Leech King
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 2935 },
                // level cheater
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 10645 },
                // Lycidas
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 4360 },
                // Manes
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 6012 },
                // Mindmaker
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 8893 },
                // Monarch Ogrefly
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 2921 },
                // Myradrosh
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 2932 },
                // Naul
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 2934 },
                // Omni
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 4361 },
                // Oskh Rhei
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 10646 },
                // Ouzelum
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 6009 },
                // Ovjang
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 2924 },
                // Pachamama
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 8894 },
                // Phecda
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 2922 },
                // Pterygotus
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 4354 },
                // Sanu Vali of Dancing Wings
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 4359 },
                // the Scarecrow
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 4357 },
                // Scitalis
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 4356 },
                // Sewer Syrup
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 2923 },
                // Shadow-dweller Yamini
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 6008 },
                // Shockmaw
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 10644 },
                // Skogs Fru
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 2928 },
                // Squonk
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 4358 },
                // Stinging Sophie
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 2920 },
                // Thextera
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 4353 },
                // Vajrakumara
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 10637 },
                // Vulpangue
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 8657 },
                // Vuokho
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 2933 },
                // warmonger
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 10639 },
                // White Joker
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 2919 },
                // Worm of the Well
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 8903 },
                // Yumcax
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 10643 },
                // ü-u-ü-u
                new Entry() { Type = Entry.EntryTypeEnum.BRank, Kind = ObjectKind.BattleNpc, NameId = 10636 },
            };

            private Dictionary<nint, DateTime> _seen = new Dictionary<nint, DateTime>();
            private Dictionary<nint, DateTime> _firstSeen = new Dictionary<nint, DateTime>();
            private DateTime _loaded;
            private DateTime _lastSoundAlert = DateTime.MinValue;

            private void AlertForEntry(GameObject go, Entry e)
            {
                Log(LogLevelEnum.Debug, null, "Found object {0}", go);
                if (SoundAlert == true && _state.cfg.QuickToggleSound == true)
                {
                    if (_lastSoundAlert > DateTime.Now.AddSeconds(-10))
                    {
                        return;
                    }
                    _lastSoundAlert = DateTime.Now;
                    UIModule.PlayChatSoundEffect(6);
                }
            }

            private void SawEntry(GameObject go, Entry e, DateTime run)
            {
                ImDrawListPtr draw;
                if (_seen.ContainsKey(go.Address) == false)
                {
                    AlertForEntry(go, e);
                }
                e.LastSeen = run;
                _seen[go.Address] = run;
                if (_firstSeen.ContainsKey(go.Address) == false)
                {
                    _firstSeen[go.Address] = run.AddSeconds(2);
                }
                if (_state.StartDrawing(out draw) == false)
                {
                    return;
                }
                Vector2 pt = new Vector2();
                Vector3 temp = _state.plug._ui.TranslateToScreen(go.Position.X, go.Position.Y, go.Position.Z);
                double dist = Vector3.Distance(_state.cs.LocalPlayer.Position, go.Position);
                string name = IncludeDistance == true ? String.Format("{0} ({1:0})", go.Name.ToString(), dist) : String.Format("{0}", go.Name.ToString());
                Vector2 sz = ImGui.CalcTextSize(name);
                float defSize = ImGui.GetFontSize();
                float mul = 20.0f / defSize;
                sz.X *= mul;
                sz.Y *= mul;
                pt.X = temp.X - (sz.X / 2.0f);
                pt.Y = temp.Y + 10.0f;
                Vector3 tf = _state.cs.LocalPlayer.Position;
                Vector3 tt = go.Position;
                float distance = Vector3.Distance(tf, tt);
                double anglexz = Math.Atan2(tf.Z - tt.Z, tf.X - tt.X);
                distance = Math.Max(distance, 5.0f);
                Vector4 col;
                DateTime now = DateTime.Now;
                float ang = 1.0f;
                Vector4 mycol = ObjectColor;
                switch (e.Type)
                {
                    case Entry.EntryTypeEnum.Custom:
                        mycol = ObjectColor;
                        break;
                    case Entry.EntryTypeEnum.SRank:
                        mycol = SRankColor;
                        break;
                    case Entry.EntryTypeEnum.ARank:
                        mycol = ARankColor;
                        break;
                    case Entry.EntryTypeEnum.BRank:
                        mycol = BRankColor;
                        break;
                    case Entry.EntryTypeEnum.RareAnimal:
                        mycol = RareAnimalColor;
                        break;
                }
                if (now < _firstSeen[go.Address])
                {
                    float time = (float)(now - _firstSeen[go.Address]).TotalMilliseconds / 200.0f;
                    ang = (float)Math.Abs(Math.Cos(time));
                    col = new Vector4(ang * mycol.X, ang * mycol.Y, ang * mycol.Z, ang * mycol.W);
                }
                else
                {
                    col = mycol;
                }
                if (distance > 10.0f)
                {
                    tt = new Vector3(tf.X - (float)(Math.Cos(anglexz) * 9.0f * (1.0f + (ang * 0.1f))), tf.Y, tf.Z - (float)(Math.Sin(anglexz) * 9.0f * (1.0f + (ang * 0.1f))));
                    tf = new Vector3(tf.X - (float)(Math.Cos(anglexz) * 4.0f * (1.0f + (ang * 0.1f))), tf.Y, tf.Z - (float)(Math.Sin(anglexz) * 4.0f * (1.0f + (ang * 0.1f))));
                    distance = Vector3.Distance(tf, tt);
                    float head = distance * 0.7f;
                    float width = distance / 40.0f;
                    width = float.Clamp(width, 0.1f, 1.0f);
                    Vector3 tx;
                    Vector3 asp = _state.plug._ui.TranslateToScreen(tf.X, tf.Y, tf.Z);
                    List<Vector3> verts = new List<Vector3>();
                    verts.Add(tf);
                    verts.Add(tx = new Vector3(tf.X + (float)(Math.Cos(anglexz + (Math.PI / 2.0)) * width), tf.Y, tf.Z + (float)(Math.Sin(anglexz + (Math.PI / 2.0)) * width)));
                    verts.Add(tx = new Vector3(tx.X + (float)(Math.Cos(anglexz + Math.PI) * head), tx.Y + ((tt.Y - tf.Y) * 0.7f), tx.Z + (float)(Math.Sin(anglexz + Math.PI) * head)));
                    verts.Add(tx = new Vector3(tx.X + (float)(Math.Cos(anglexz + (Math.PI / 2.0)) * width * 2), tx.Y, tx.Z + (float)(Math.Sin(anglexz + (Math.PI / 2.0)) * width * 2)));
                    verts.Add(tt);
                    tx = verts[3];
                    verts.Add(tx = new Vector3(tx.X + (float)(Math.Cos(anglexz - (Math.PI / 2.0)) * width * 6), tx.Y, tx.Z + (float)(Math.Sin(anglexz - (Math.PI / 2.0)) * width * 6)));
                    verts.Add(tx = new Vector3(tx.X + (float)(Math.Cos(anglexz + (Math.PI / 2.0)) * width * 2), tx.Y, tx.Z + (float)(Math.Sin(anglexz + (Math.PI / 2.0)) * width * 2)));
                    verts.Add(tx = new Vector3(tf.X + (float)(Math.Cos(anglexz - (Math.PI / 2.0)) * width), tf.Y, tf.Z + (float)(Math.Sin(anglexz - (Math.PI / 2.0)) * width)));
                    verts.Add(tf);
                    foreach (Vector3 v in verts)
                    {
                        Vector3 vx = _state.plug._ui.TranslateToScreen(v.X, v.Y, v.Z);
                        draw.PathLineTo(new Vector2(vx.X, vx.Y));
                    }
                    draw.PathStroke(
                        ImGui.GetColorU32(col),
                        ImDrawFlags.None,
                        4.0f
                    );
                    float aspx = asp.X - (sz.X / 2.0f);
                    if (ShowNames == true)
                    {
                        if (ShowNameBg == true)
                        {
                            draw.AddRectFilled(
                                new Vector2(aspx - 5, asp.Y - 5),
                                new Vector2(aspx + sz.X + 5, asp.Y + sz.Y + 5),
                                ImGui.GetColorU32(BgColor),
                                1.0f
                            );
                        }
                        draw.AddText(
                            ImGui.GetFont(),
                            20.0f,
                            new Vector2(aspx, asp.Y),
                            ImGui.GetColorU32(TextColor),
                            name
                        );
                    }
                }
                if (ShowNames == true)
                {
                    if (ShowNameBg == true)
                    {
                        draw.AddRectFilled(
                            new Vector2(pt.X - 5, pt.Y - 5),
                            new Vector2(pt.X + sz.X + 5, pt.Y + sz.Y + 5),
                            ImGui.GetColorU32(BgColor),
                            1.0f
                        );
                    }
                    draw.AddText(
                        ImGui.GetFont(),
                        20.0f,
                        pt,
                        ImGui.GetColorU32(TextColor),
                        name
                    );
                }
                float ex = DateTime.Now.Millisecond / 1000.0f;
                Vector4 ping = new Vector4(col.X, col.Y, col.Z, col.W - (col.W * ex));
                draw.AddCircle(
                    new Vector2(temp.X, temp.Y),
                    10.0f + (ex * 60.0f),
                    ImGui.GetColorU32(ping),
                    20,
                    2.0f
                );
                draw.AddCircleFilled(
                    new Vector2(temp.X, temp.Y),
                    10.0f,
                    ImGui.GetColorU32(mycol),
                    20
                );
            }

            private void CleanupSeen(DateTime run)
            {
                var toRem = (from ix in _seen where ix.Value < run select ix.Key).ToList();
                foreach (nint e in toRem)
                {
                    _seen.Remove(e);
                    _firstSeen.Remove(e);
                }
            }

            protected override bool ExecutionImplementation()
            {
                if (entries.Count == 0 && IncludeRankS == false && IncludeRankA == false && IncludeIslandRare == false)
                {
                    return false;
                }
                ushort curter = _state.cs.TerritoryType;
                DateTime run = DateTime.Now;
                Dictionary<ObjectKind, List<Entry>> lookup = LookFor.entries.GroupBy(x => x.Kind).ToDictionary(x => x.Key, x => x.ToList());
                if (IncludeRankS == true)
                {
                    if (lookup.ContainsKey(ObjectKind.BattleNpc) == false)
                    {
                        lookup[ObjectKind.BattleNpc] = new List<Entry>();
                    }
                    lookup[ObjectKind.BattleNpc].AddRange(_defaultsSRanks);
                }
                if (IncludeRankA == true)
                {
                    if (lookup.ContainsKey(ObjectKind.BattleNpc) == false)
                    {
                        lookup[ObjectKind.BattleNpc] = new List<Entry>();
                    }
                    lookup[ObjectKind.BattleNpc].AddRange(_defaultsARanks);
                }
                if (IncludeRankB == true)
                {
                    if (lookup.ContainsKey(ObjectKind.BattleNpc) == false)
                    {
                        lookup[ObjectKind.BattleNpc] = new List<Entry>();
                    }
                    lookup[ObjectKind.BattleNpc].AddRange(_defaultsBRanks);
                }
                if (IncludeIslandRare == true)
                {
                    if (lookup.ContainsKey(ObjectKind.BattleNpc) == false)
                    {
                        lookup[ObjectKind.BattleNpc] = new List<Entry>();
                    }
                    lookup[ObjectKind.BattleNpc].AddRange(_defaultsIsland);
                }
                foreach (GameObject go in _state.ot)
                {
                    if (lookup.ContainsKey(go.ObjectKind) == false)
                    {
                        continue;
                    }
                    unsafe
                    {
                        GameObjectPtr* gop = (GameObjectPtr*)go.Address;
                        if (gop->GetIsTargetable() == false)
                        {                            
                            continue;
                        }
                    }
                    foreach (Entry ae in lookup[go.ObjectKind])
                    {
                        if (ae.Territory > 0 && ae.Territory != curter)
                        {
                            continue;
                        }
                        if (ae.NameId > 0)
                        {
                            if (ae.Territory == 1055)
                            {
                                Vector3 pos = go.Position;
                                if (pos.X < -150.0f && pos.Z > 100.0f)
                                {
                                    continue;
                                }
                            }
                            if (go is Character)
                            {
                                Character ch = go as Character;
                                if (ch.NameId == ae.NameId)
                                {
                                    SawEntry(go, ae, run);
                                }
                            }
                        }
                        else if (ae.Regex != null)
                        {
                            if (ae.Regex.IsMatch(go.Name.ToString()) == true)
                            {
                                SawEntry(go, ae, run);
                            }
                        }
                        else if (String.Compare(go.Name.ToString(), ae.Name, true) == 0)
                        {
                            SawEntry(go, ae, run);
                        }
                    }
                }
                CleanupSeen(run);
                return true;
            }

            public AlertFinder(State state) : base(state)
            {
                Enabled = false;
                _loaded = DateTime.Now;
                entries = new List<Entry>();
                LookFor = new AlertEditor();
                LookFor.state = state;
                LookFor.entries = entries;
            }

        }

        public Radar(State st) : base(st)
        {
        }

    }

}
