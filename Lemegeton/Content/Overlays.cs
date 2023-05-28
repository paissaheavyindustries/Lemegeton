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

namespace Lemegeton.Content
{

    public class Overlays : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

        public class DotTracker : Core.ContentItem
        {

            public class DotEditor : CustomPropertyInterface
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

                public DotEditor(DotTracker dt)
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

                public TextureWrap StatusIcon { get; set; } = null;

            }

            [AttributeOrderNumber(1000)]
            public DotEditor DotSettings { get; set; }

            private Dictionary<uint, DotSpecification> specs = new Dictionary<uint, DotSpecification>();
            private List<DotApplication> apps = new List<DotApplication>();
            public override FeaturesEnum Features => FeaturesEnum.Drawing;
            private bool _subbed = false;

            public DotTracker(State state) : base(state)
            {
                Enabled = false;
                OnActiveChanged += DotTracker_OnActiveChanged;
                InitializeDotSpecs();
                DotSettings = new DotEditor(this);
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

            private void DrawTimerBar(ImDrawListPtr draw, float x, float y, float width, float height, float timeRemaining, float timeMax, TextureWrap icon)
            {
                float x2 = x + (width * timeRemaining / timeMax);
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
                if (icon != null)
                {
                    float iw = icon.Width * (height / icon.Height);
                    float ix = x - (iw + 3.0f);
                    draw.AddImage(icon.ImGuiHandle, new Vector2(ix, y), new Vector2(ix + iw, y + height));
                }
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
                draw.AddText(new Vector2(x2 + 5.0f, y), ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)), String.Format("{0:0.0}", timeRemaining));
            }

            protected override bool ExecutionImplementation()
            {
                ImDrawListPtr draw;
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
                        go.Position.X,
                        go.Position.Y + 3.0f,
                        go.Position.Z
                    );                    
                    if (stacks.TryGetValue(go, out float stack) == false)
                    {
                        stack = 0.0f;
                    }
                    float timeLeft = d.Duration - (float)(DateTime.Now - d.Applied).TotalSeconds;
                    float barWidth = 200.0f;
                    float barHeight = 20.0f;
                    DotSpecification spec = specs[d.StatusId];
                    DrawTimerBar(draw, pos.X - (barWidth / 2.0f), pos.Y - stack, barWidth, barHeight, timeLeft, d.Duration, spec.StatusIcon);
                    stacks[go] = stack + barHeight + 2.0f;
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
