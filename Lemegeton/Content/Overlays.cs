using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ImGuiNET;
using Lemegeton.Core;
using System;
using System.Numerics;
using GameObject = Dalamud.Game.ClientState.Objects.Types.GameObject;
using GameObjectPtr = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using CharacterStruct = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Statuses;
using static Lemegeton.Core.State;
using System.IO;
using static Lemegeton.Core.NetworkDecoder;
using System.Linq;
using System.Reflection.Metadata;
using static Lemegeton.Content.Radar.AlertFinder;
using ImGuiScene;
using Lemegeton.ContentCategory;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using Dalamud.Interface;
using System.ComponentModel.Design;
using Lumina.Excel.GeneratedSheets;
using Dalamud.Interface.Internal;

namespace Lemegeton.Content
{

    public class Overlays : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

        public class DotTracker : Core.ContentItem
        {

            public class DotVisualsWidget : CustomPropertyInterface
            {

                public override void Deserialize(string data)
                {
                    string[] temp = data.Split(";");
                    foreach (string t in temp)
                    {
                        string[] item = t.Split("=", 2);
                        switch (item[0])
                        {
                            case "BarWidth": _dt._visualBarWidth = float.Parse(item[1]); break;
                            case "ItemHeight": _dt._visualItemHeight = float.Parse(item[1]); break;
                            case "ShowBar": _dt._visualShowBar = bool.Parse(item[1]); break;
                            case "ShowIcon": _dt._visualShowIcon = bool.Parse(item[1]); break;
                            case "ShowTime": _dt._visualShowTime = bool.Parse(item[1]); break;
                            case "ItemOffsetWorldX": _dt._visualItemOffsetWorldX = float.Parse(item[1]); break;
                            case "ItemOffsetWorldY": _dt._visualItemOffsetWorldY = float.Parse(item[1]); break;
                            case "ItemOffsetWorldZ": _dt._visualItemOffsetWorldZ = float.Parse(item[1]); break;
                            case "ItemOffsetScreenX": _dt._visualItemOffsetScreenX = float.Parse(item[1]); break;
                            case "ItemOffsetScreenY": _dt._visualItemOffsetScreenY = float.Parse(item[1]); break;
                        }
                    }
                }

                public override string Serialize()
                {
                    List<string> items = new List<string>();
                    items.Add(String.Format("BarWidth={0}", _dt._visualBarWidth));
                    items.Add(String.Format("ItemHeight={0}", _dt._visualItemHeight));
                    items.Add(String.Format("ShowBar={0}", _dt._visualShowBar));
                    items.Add(String.Format("ShowIcon={0}", _dt._visualShowIcon));
                    items.Add(String.Format("ShowTime={0}", _dt._visualShowTime));
                    items.Add(String.Format("ItemOffsetWorldX={0}", _dt._visualItemOffsetWorldX));
                    items.Add(String.Format("ItemOffsetWorldY={0}", _dt._visualItemOffsetWorldY));
                    items.Add(String.Format("ItemOffsetWorldZ={0}", _dt._visualItemOffsetWorldZ));
                    items.Add(String.Format("ItemOffsetScreenX={0}", _dt._visualItemOffsetScreenX));
                    items.Add(String.Format("ItemOffsetScreenY={0}", _dt._visualItemOffsetScreenY));
                    return String.Join(";", items);
                }

                public override void RenderEditor(string path)
                {
                    Vector2 avail = ImGui.GetContentRegionAvail();
                    string proptr = I18n.Translate(path);
                    ImGui.Text(proptr);
                    ImGui.PushItemWidth(avail.X);

                    ImGui.Text(Environment.NewLine + I18n.Translate(path + "/BarWidth"));
                    float barw = _dt._visualBarWidth;
                    if (ImGui.SliderFloat("##" + path + "/BarWidth", ref barw, 50.0f, 400.0f, "%.0f") == true)
                    {
                        _dt._visualBarWidth = barw;
                    }
                    ImGui.Text(Environment.NewLine + I18n.Translate(path + "/ItemHeight"));
                    float barh = _dt._visualItemHeight;
                    if (ImGui.SliderFloat("##" + path + "/ItemHeight", ref barh, 10.0f, 40.0f, "%.0f") == true)
                    {
                        _dt._visualItemHeight = barh;
                    }
                    ImGui.Text("");
                    bool shb = _dt._visualShowBar;
                    if (ImGui.Checkbox(I18n.Translate(path + "/ShowBar"), ref shb) == true)
                    {
                        _dt._visualShowBar = shb;
                    }
                    bool shi = _dt._visualShowIcon;
                    if (ImGui.Checkbox(I18n.Translate(path + "/ShowIcon"), ref shi) == true)
                    {
                        _dt._visualShowIcon = shi;
                    }
                    bool sht = _dt._visualShowTime;
                    if (ImGui.Checkbox(I18n.Translate(path + "/ShowTime"), ref sht) == true)
                    {
                        _dt._visualShowTime = sht;
                    }
                    ImGui.TextWrapped(Environment.NewLine + I18n.Translate(path + "/ItemOffsetWorld"));
                    float ofsworldx = _dt._visualItemOffsetWorldX;
                    if (ImGui.DragFloat("X##" + path + "/ItemOffsetWorldX", ref ofsworldx, 0.01f, -5.0f, 5.0f, "%.2f", ImGuiSliderFlags.AlwaysClamp) == true)
                    {
                        _dt._visualItemOffsetWorldX = ofsworldx;
                    }
                    float ofsworldy = _dt._visualItemOffsetWorldY;
                    if (ImGui.DragFloat("Y##" + path + "/ItemOffsetWorldY", ref ofsworldy, 0.01f, -5.0f, 5.0f, "%.2f", ImGuiSliderFlags.AlwaysClamp) == true)
                    {
                        _dt._visualItemOffsetWorldY = ofsworldy;
                    }
                    float ofsworldz = _dt._visualItemOffsetWorldZ;
                    if (ImGui.DragFloat("Z##" + path + "/ItemOffsetWorldZ", ref ofsworldz, 0.01f, -5.0f, 5.0f, "%.2f", ImGuiSliderFlags.AlwaysClamp) == true)
                    {
                        _dt._visualItemOffsetWorldZ = ofsworldz;
                    }
                    ImGui.TextWrapped(Environment.NewLine + I18n.Translate(path + "/ItemOffsetScreen"));
                    float ofsscreenx = _dt._visualItemOffsetScreenX;
                    if (ImGui.DragFloat("X##" + path + "/ItemOffsetScreenX", ref ofsscreenx, 1.0f, -300.0f, 300.0f, "%.0f", ImGuiSliderFlags.AlwaysClamp) == true)
                    {
                        _dt._visualItemOffsetScreenX = ofsscreenx;
                    }
                    float ofsscreeny = _dt._visualItemOffsetScreenY;
                    if (ImGui.DragFloat("Y##" + path + "/ItemOffsetScreenY", ref ofsscreeny, 1.0f, -300.0f, 300.0f, "%.0f", ImGuiSliderFlags.AlwaysClamp) == true)
                    {
                        _dt._visualItemOffsetScreenY = ofsscreeny;
                    }
                    ImGui.PopItemWidth();
                }

                private DotTracker _dt;

                public DotVisualsWidget(DotTracker dt)
                {
                    _dt = dt;
                }

            }

            public class DotSettingsWidget : CustomPropertyInterface
            {

                public override void Deserialize(string data)
                {
                    string[] temp = data.Split(";");
                    foreach (string t in temp)
                    {
                        string[] item = t.Split("=", 2);
                        if (uint.TryParse(item[0], out uint statusId) == false)
                        {
                            continue;
                        }
                        if (bool.TryParse(item[1], out bool tracked) == false)
                        {
                            continue;
                        }
                        if (_dt.specs.TryGetValue(statusId, out DotSpecification spec) == false)
                        {
                            continue;
                        }
                        spec.Tracking = tracked;
                    }
                }

                public override string Serialize()
                {
                    List<string> items = new List<string>();
                    foreach (KeyValuePair<uint, DotSpecification> kp in _dt.specs)
                    {
                        items.Add(String.Format("{0}={1}", kp.Value.Id, kp.Value.Tracking));
                    }
                    return String.Join(";", items);
                }

                public override void RenderEditor(string path)
                {
                    Vector2 avail = ImGui.GetContentRegionAvail();
                    string proptr = I18n.Translate(path);
                    ImGui.Text(proptr);
                    ImGui.PushItemWidth(avail.X);
                    if (ImGui.BeginListBox("##Lb" + path) == true)
                    {
                        foreach (KeyValuePair<uint, DotSpecification> kp in _dt.specs)
                        {
                            DotSpecification ds = kp.Value;
                            string name = ds.CachedName;
                            if (ds.Job != 0)
                            {
                                AutomarkerPrio.PrioJobEnum job = (AutomarkerPrio.PrioJobEnum)ds.Job;
                                name = "(" + I18n.Translate("Job/" + job) + ") " + name;
                            }
                            if (ImGui.Selectable(name, ds.Tracking) == true)
                            {
                                ds.Tracking = (ds.Tracking == false);
                            }
                        }
                        ImGui.EndListBox();
                    }
                    ImGui.PopItemWidth();
                }

                private DotTracker _dt;                

                public DotSettingsWidget(DotTracker dt)
                {
                    _dt = dt;
                }

            }

            private class DotApplication
            {

                public uint StatusId { get; set; } = 0;
                public uint ActorId { get; set; } = 0;
                public float Duration { get; set; } = 10.0f;
                public DateTime Applied { get; set; } = DateTime.Now;

            }

            private class DotSpecification
            {

                public uint Job { get; set; }
                public uint Id { get; set; }
                public string CachedName { get; set; } = null;
                public bool Tracking { get; set; } = true;

                public IDalamudTextureWrap StatusIcon { get; set; } = null;

            }

            [AttributeOrderNumber(1000)]
            public DotVisualsWidget DotVisuals { get; set; }

            [AttributeOrderNumber(2000)]
            public DotSettingsWidget DotSettings { get; set; }

            private Dictionary<uint, DotSpecification> specs = new Dictionary<uint, DotSpecification>();
            private List<DotApplication> apps = new List<DotApplication>();
            public override FeaturesEnum Features => FeaturesEnum.Drawing;
            private bool _subbed = false;

            internal float _visualItemHeight { get; set; } = 20.0f;
            internal float _visualBarWidth { get; set; } = 200.0f;
            internal bool _visualShowTime { get; set; } = true;
            internal bool _visualShowIcon { get; set; } = true;
            internal bool _visualShowBar { get; set; } = true;

            internal float _visualItemOffsetWorldX { get; set; } = 0.0f;
            internal float _visualItemOffsetWorldY { get; set; } = 3.0f;
            internal float _visualItemOffsetWorldZ { get; set; } = 0.0f;
            internal float _visualItemOffsetScreenX { get; set; } = 0.0f;
            internal float _visualItemOffsetScreenY { get; set; } = 0.0f;

            public DotTracker(State state) : base(state)
            {
                Enabled = false;
                OnActiveChanged += DotTracker_OnActiveChanged;
                InitializeDotSpecs();
                DotVisuals = new DotVisualsWidget(this);
                DotSettings = new DotSettingsWidget(this);
            }

            private void AddDotSpec(DotSpecification spec)
            {
                Lumina.Excel.GeneratedSheets.Status st = _state.dm.Excel.GetSheet<Lumina.Excel.GeneratedSheets.Status>().GetRow(spec.Id);
                string name;
                if (st != null)
                {                    
                    name = st.Name;
                    spec.StatusIcon = _state.plug._ui.GetOnDemandIcon(st.Icon);
                }
                else
                {
                    name = String.Format("{0} ??", spec.Id);
                }
                spec.CachedName = name;
                specs[spec.Id] = spec;
            }

            private void InitializeDotSpecs()
            {
                AddDotSpec(new DotSpecification() { Id = 838, Job = (uint)AutomarkerPrio.PrioJobEnum.AST }); // Combust
                AddDotSpec(new DotSpecification() { Id = 843, Job = (uint)AutomarkerPrio.PrioJobEnum.AST }); // Combust II
                AddDotSpec(new DotSpecification() { Id = 1881, Job = (uint)AutomarkerPrio.PrioJobEnum.AST }); // Combust III
                AddDotSpec(new DotSpecification() { Id = 161, Job = (uint)AutomarkerPrio.PrioJobEnum.BLM }); // Thunder
                AddDotSpec(new DotSpecification() { Id = 162, Job = (uint)AutomarkerPrio.PrioJobEnum.BLM }); // Thunder II
                AddDotSpec(new DotSpecification() { Id = 163, Job = (uint)AutomarkerPrio.PrioJobEnum.BLM }); // Thunder III
                AddDotSpec(new DotSpecification() { Id = 1210, Job = (uint)AutomarkerPrio.PrioJobEnum.BLM }); // Thunder IV
                AddDotSpec(new DotSpecification() { Id = 124, Job = (uint)AutomarkerPrio.PrioJobEnum.BRD }); // Venomous Bite
                AddDotSpec(new DotSpecification() { Id = 129, Job = (uint)AutomarkerPrio.PrioJobEnum.BRD }); // Windbite
                AddDotSpec(new DotSpecification() { Id = 1200, Job = (uint)AutomarkerPrio.PrioJobEnum.BRD }); // Caustic Bite
                AddDotSpec(new DotSpecification() { Id = 1201, Job = (uint)AutomarkerPrio.PrioJobEnum.BRD }); // Stormbite
                AddDotSpec(new DotSpecification() { Id = 118, Job = (uint)AutomarkerPrio.PrioJobEnum.DRG }); // Chaos Thrust
                AddDotSpec(new DotSpecification() { Id = 2719, Job = (uint)AutomarkerPrio.PrioJobEnum.DRG }); // Chaotic Spring
                AddDotSpec(new DotSpecification() { Id = 1838, Job = (uint)AutomarkerPrio.PrioJobEnum.GNB }); // Bow Shock
                AddDotSpec(new DotSpecification() { Id = 1837, Job = (uint)AutomarkerPrio.PrioJobEnum.GNB }); // Sonic Break
                AddDotSpec(new DotSpecification() { Id = 1866, Job = (uint)AutomarkerPrio.PrioJobEnum.MCH }); // Bioblaster
                AddDotSpec(new DotSpecification() { Id = 246, Job = (uint)AutomarkerPrio.PrioJobEnum.MNK }); // Demolish
                AddDotSpec(new DotSpecification() { Id = 248, Job = (uint)AutomarkerPrio.PrioJobEnum.PLD }); // Circle of Corn
                AddDotSpec(new DotSpecification() { Id = 1228, Job = (uint)AutomarkerPrio.PrioJobEnum.SAM }); // Higanbana
                AddDotSpec(new DotSpecification() { Id = 179, Job = (uint)AutomarkerPrio.PrioJobEnum.SCH }); // Bio
                AddDotSpec(new DotSpecification() { Id = 189, Job = (uint)AutomarkerPrio.PrioJobEnum.SCH }); // Bio II
                AddDotSpec(new DotSpecification() { Id = 1895, Job = (uint)AutomarkerPrio.PrioJobEnum.SCH }); // Biolysis
                AddDotSpec(new DotSpecification() { Id = 2614, Job = (uint)AutomarkerPrio.PrioJobEnum.SGE }); // Dosis
                AddDotSpec(new DotSpecification() { Id = 2615, Job = (uint)AutomarkerPrio.PrioJobEnum.SGE }); // Dosis II
                AddDotSpec(new DotSpecification() { Id = 2616, Job = (uint)AutomarkerPrio.PrioJobEnum.SGE }); // Dosis III
                AddDotSpec(new DotSpecification() { Id = 143, Job = (uint)AutomarkerPrio.PrioJobEnum.WHM }); // Aero
                AddDotSpec(new DotSpecification() { Id = 144, Job = (uint)AutomarkerPrio.PrioJobEnum.WHM }); // Aero II
                AddDotSpec(new DotSpecification() { Id = 1871, Job = (uint)AutomarkerPrio.PrioJobEnum.WHM }); // Dia
                // other assorted dots
                AddDotSpec(new DotSpecification() { Id = 1714, Job = 0 }); // Bleeding (BLU Song of Torment)
                AddDotSpec(new DotSpecification() { Id = 3359, Job = 0 }); // Sustained Damage (Variant donjon Spirit Dart action)
            }

            private void DrawTimerBar(ImDrawListPtr draw, float x, float y, float width, float height, float timeRemaining, float timeMax, IDalamudTextureWrap icon)
            {
                float x2 = x + (_visualShowBar == true ? (width * timeRemaining / timeMax) : 0.0f);
                float yt = y + (height * 0.3f);
                Vector4 maincol;
                if (timeRemaining > 10.0f)
                {
                    maincol = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
                }
                else if (timeRemaining > 5.0f)
                {
                    maincol = new Vector4(1.0f - ((timeRemaining - 5.0f) / 5.0f), 1.0f, 0.0f, 1.0f);
                }
                else
                {
                    maincol = new Vector4(1.0f, timeRemaining / 5.0f, 0.0f, 1.0f);
                }
                if (icon != null && _visualShowIcon == true)
                {
                    float iw = icon.Width * (height / icon.Height);
                    float ix = x - (iw + 3.0f);
                    draw.AddImage(icon.ImGuiHandle, new Vector2(ix, y), new Vector2(ix + iw, y + height));
                }
                if (_visualShowBar == true)
                {
                    Vector4 bg = new Vector4(0.0f, 0.0f, 0.0f, 0.5f);
                    Vector4 shadow = new Vector4(
                        Math.Clamp(maincol.X * 0.5f, 0.0f, 1.0f),
                        Math.Clamp(maincol.Y * 0.5f, 0.0f, 1.0f),
                        Math.Clamp(maincol.Z * 0.5f, 0.0f, 1.0f),
                        maincol.W
                    );
                    Vector4 hilite = new Vector4(
                        Math.Clamp(maincol.X + 0.2f, 0.0f, 1.0f),
                        Math.Clamp(maincol.Y + 0.2f, 0.0f, 1.0f),
                        Math.Clamp(maincol.Z + 0.2f, 0.0f, 1.0f),
                        maincol.W
                    );
                    draw.AddRectFilled(
                        new Vector2(x2, y),
                        new Vector2(x + width, y + height),
                        ImGui.GetColorU32(bg)
                    );
                    draw.AddRectFilledMultiColor(
                        new Vector2(x, y),
                        new Vector2(x2, yt),
                        ImGui.GetColorU32(maincol),
                        ImGui.GetColorU32(maincol),
                        ImGui.GetColorU32(hilite),
                        ImGui.GetColorU32(hilite)
                    );
                    draw.AddRectFilledMultiColor(
                        new Vector2(x, yt),
                        new Vector2(x2, y + height),
                        ImGui.GetColorU32(hilite),
                        ImGui.GetColorU32(hilite),
                        ImGui.GetColorU32(shadow),
                        ImGui.GetColorU32(shadow)
                    );
                }
                if (_visualShowTime == true)
                {
                    string str = String.Format("{0:0.0}", timeRemaining);
                    Vector2 sz = ImGui.CalcTextSize(str);
                    draw.AddText(new Vector2(x2 + 5.0f, y + (height / 2.0f) - (sz.Y / 2.0f)), ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)), str);
                }
            }

            protected override bool ExecutionImplementation()
            {
                ImDrawListPtr draw;
                if (apps.Count == 0)
                {
                    return false;
                }
                if (_state.StartDrawing(out draw) == false)
                {
                    return false;
                }
                uint me = _state.cs.LocalPlayer != null ? _state.cs.LocalPlayer.ObjectId : 0;
                if (me == 0)
                {
                    return false;
                }
                Dictionary<GameObject, float> stacks = new Dictionary<GameObject, float>();
                List<DotApplication> exps = new List<DotApplication>();                
                foreach (DotApplication d in apps)
                {
                    if (DateTime.Now > d.Applied.AddSeconds(d.Duration))
                    {
                        exps.Add(d);
                        continue;
                    }
                    GameObject go = _state.GetActorById(d.ActorId);
                    if (go == null)
                    {
                        continue;
                    }
                    Vector3 pos = _state.plug._ui.TranslateToScreen(
                        go.Position.X + _visualItemOffsetWorldX,
                        go.Position.Y + _visualItemOffsetWorldY,
                        go.Position.Z + _visualItemOffsetWorldZ
                    );
                    pos = new Vector3(pos.X + _visualItemOffsetScreenX, pos.Y + _visualItemOffsetScreenY, pos.Z);
                    if (stacks.TryGetValue(go, out float stack) == false)
                    {
                        stack = 0.0f;
                    }
                    float timeLeft = d.Duration - (float)(DateTime.Now - d.Applied).TotalSeconds;
                    DotSpecification spec = specs[d.StatusId];
                    DrawTimerBar(draw, pos.X - (_visualShowBar == true ? (_visualBarWidth / 2.0f) : 0.0f), pos.Y - (_visualItemHeight + stack), _visualBarWidth, _visualItemHeight, timeLeft, d.Duration, spec.StatusIcon);
                    stacks[go] = stack + _visualItemHeight + 2.0f;
                }
                foreach (DotApplication d in exps)
                {
                    apps.Remove(d);
                }
                return true;
            }

            private void DotTracker_OnActiveChanged(bool newState)
            {
                if (newState == true)
                {
                    SubscribeToEvents();
                }
                else
                {
                    UnsubscribeFromEvents();
                }
            }

            private void SubscribeToEvents()
            {
                lock (this)
                {
                    if (_subbed == true)
                    {
                        return;
                    }
                    _subbed = true;
                    Log(LogLevelEnum.Debug, null, "Subscribing to events");
                    _state.OnStatusChange += _state_OnStatusChange;
                    _state.OnZoneChange += _state_OnZoneChange;
                    _state.OnCombatantRemoved += _state_OnCombatantRemoved;
                    _state.OnDirectorUpdate += _state_OnDirectorUpdate;
                }
            }

            private void UnsubscribeFromEvents()
            {
                lock (this)
                {
                    if (_subbed == false)
                    {
                        return;
                    }
                    Log(LogLevelEnum.Debug, null, "Unsubscribing from events");
                    _state.OnDirectorUpdate -= _state_OnDirectorUpdate;
                    _state.OnCombatantRemoved -= _state_OnCombatantRemoved;
                    _state.OnStatusChange -= _state_OnStatusChange;
                    _state.OnZoneChange -= _state_OnZoneChange;
                    _subbed = false;
                }
            }

            private void ApplyDot(DotApplication da)
            {
                var ex = (from ix in apps where ix.StatusId == da.StatusId && ix.ActorId == da.ActorId select ix).FirstOrDefault();
                if (ex != null)
                {
                    ex.Applied = da.Applied;
                    ex.Duration = da.Duration;
                }
                else
                {
                    apps.Add(da);
                }
            }

            private void RemoveDot(uint statusId, uint actorId)
            {
                var ex = (from ix in apps where ix.StatusId == statusId && ix.ActorId == actorId select ix).FirstOrDefault();
                if (ex != null)
                {
                    apps.Remove(ex);
                }
            }

            private void ClearDots(uint actorId)
            {
                if (actorId == 0)
                {
                    apps.Clear();
                }
                else
                {
                    List<DotApplication> toclear = new List<DotApplication>(from ix in apps where ix.ActorId == actorId select ix);
                    foreach (DotApplication d in toclear)
                    {
                        apps.Remove(d);
                    }
                }
            }

            private void _state_OnDirectorUpdate(uint param1, uint param2, uint param3, uint param4)
            {
                if (param2 == (uint)DirectorTypeEnum.FadeOut)
                {
                    ClearDots(0);
                }
            }

            private void _state_OnZoneChange(ushort newZone)
            {
                ClearDots(0);
            }

            private void _state_OnCombatantRemoved(uint actorId, nint addr)
            {
                ClearDots(actorId);
            }

            private void _state_OnStatusChange(uint src, uint dest, uint statusId, bool gained, float duration, int stacks)
            {
                uint me = _state.cs.LocalPlayer != null ? _state.cs.LocalPlayer.ObjectId : 0;
                if (me == 0 || src != me)
                {
                    return;
                }
                if (specs.TryGetValue(statusId, out DotSpecification spec) == false)
                {
                    return;
                }
                if (spec.Tracking == false)
                {
                    return;
                }
                if (gained == false)
                {
                    return;
                }
                // only one thunder applied at a time
                if (statusId == 161 || statusId == 162 || statusId == 163 || statusId == 1210)
                {
                    RemoveDot(161, dest);
                    RemoveDot(162, dest);
                    RemoveDot(163, dest);
                    RemoveDot(1210, dest);
                }
                Log(LogLevelEnum.Debug, null, "DoT applied - {0} on {1} for {2} s", statusId, dest, duration);
                DotApplication da = new DotApplication() { StatusId = statusId, ActorId = dest, Applied = DateTime.Now, Duration = duration };
                ApplyDot(da);
            }

        }

        public Overlays(State st) : base(st)
        {
        }

    }

}
