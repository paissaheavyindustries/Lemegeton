using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Dalamud.Bindings.ImGui;
using Lemegeton.Core;
using Lumina.Excel.Sheets;
using static Lemegeton.Core.State;

namespace Lemegeton.Content
{

    internal class DtRaidCruiser : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

        private enum ZoneEnum
        {
            None = 0,
            M5s = 1257,
            M6s = 1259,
            M7s = 1261,
            M8s = 1263,
        }

        private ZoneEnum _CurrentZone = ZoneEnum.None;
        private ZoneEnum CurrentZone
        {
            get
            {
                return _CurrentZone;
            }
            set
            {
                if (_CurrentZone != value)
                {
                    Log(State.LogLevelEnum.Debug, null, "Zone changing from {0} to {1}", _CurrentZone, value);
                    _CurrentZone = value;
                }
            }
        }

        private bool _subbed = false;

        private const int NameMu = 13831;
        private const int NameYan = 13832;
        private const int NameJabberwock = 13833;
        private const int NameManta = 13834;
        private const int NameCat = 13835;
        private const int AbilityMoonbeamLeft = 41923;
        private const int AbilityMoonbeamRight = 41922;

        private SoulSugarAm _soulSugarAm;
        private DrawMoonbeam _drawMoonbeam;


        #region SoulSugarAm

        public class SoulSugarAm : Automarker
        {

            private enum EnemyType
            {
                Yan,
                GimmeCat,
                Mu1,
                Mu2,
                MantaW,
                MantaE,
                Jabberwock
            }

            [AttributeOrderNumber(1000)]
            public AutomarkerSigns Set1 { get; set; }
            [AttributeOrderNumber(1010)]
            public AutomarkerSigns Set2 { get; set; }
            [AttributeOrderNumber(1020)]
            public AutomarkerSigns Set3 { get; set; }
            [AttributeOrderNumber(1030)]
            public AutomarkerSigns Set4 { get; set; }

            [DebugOption]
            [AttributeOrderNumber(2500)]
            public AutomarkerTiming Timing { get; set; }

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public System.Action Test { get; set; }
            
            private bool _fired = false;
            private int setnum = 1;
            private Dictionary<EnemyType, IGameObject> _set1 = new Dictionary<EnemyType, IGameObject>();
            private Dictionary<EnemyType, IGameObject> _set2 = new Dictionary<EnemyType, IGameObject>();
            private Dictionary<EnemyType, IGameObject> _set3 = new Dictionary<EnemyType, IGameObject>();
            private Dictionary<EnemyType, IGameObject> _set4 = new Dictionary<EnemyType, IGameObject>();

            public SoulSugarAm(State state) : base(state)
            {
                Enabled = false;
                Set1 = new AutomarkerSigns();
                Set2 = new AutomarkerSigns();
                Set3 = new AutomarkerSigns();
                Set4 = new AutomarkerSigns();
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Set1.SetRole("Yan", AutomarkerSigns.SignEnum.Attack1, false);
                Set1.SetRole("GimmeCat", AutomarkerSigns.SignEnum.Attack2, false);
                Set1.SetRole("Mu1", AutomarkerSigns.SignEnum.None, false);
                Set1.SetRole("Mu2", AutomarkerSigns.SignEnum.None, false);
                Set2.SetRole("MantaW", AutomarkerSigns.SignEnum.Attack3, false);
                Set2.SetRole("MantaE", AutomarkerSigns.SignEnum.Attack4, false);
                Set2.SetRole("Mu1", AutomarkerSigns.SignEnum.None, false);
                Set2.SetRole("Mu2", AutomarkerSigns.SignEnum.None, false);
                Set3.SetRole("Yan", AutomarkerSigns.SignEnum.Ignore1, false);
                Set3.SetRole("Jabberwock", AutomarkerSigns.SignEnum.Attack5, false);
                Set3.SetRole("GimmeCat", AutomarkerSigns.SignEnum.Attack6, false);                
                Set4.SetRole("Yan", AutomarkerSigns.SignEnum.Ignore2, false);
                Set4.SetRole("Jabberwock", AutomarkerSigns.SignEnum.Attack1, false);
                Set4.SetRole("GimmeCat", AutomarkerSigns.SignEnum.Attack3, false);
                Set4.SetRole("MantaW", AutomarkerSigns.SignEnum.Attack4, false);
                Set4.SetRole("MantaE", AutomarkerSigns.SignEnum.Attack2, false);
                Set4.SetRole("Mu1", AutomarkerSigns.SignEnum.None, false);
                Set4.SetRole("Mu2", AutomarkerSigns.SignEnum.None, false);
                Test = new System.Action(() => Set1.TestFunctionality(state, null, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _fired = false;
                setnum = 1;
                _set1.Clear();
                _set2.Clear();
                _set3.Clear();
                _set4.Clear();
            }

            internal void FeedCombatant(IGameObject go)
            {
                if (Active == false)
                {
                    return;
                }
                ICharacter ch = go as ICharacter;
                uint nameid = ch.NameId;
                switch (setnum)
                {
                    case 1:
                        switch (nameid)
                        {
                            case NameYan:
                                _set1[EnemyType.Yan] = go;
                                break;
                            case NameCat:
                                _set1[EnemyType.GimmeCat] = go;
                                break;
                            case NameMu:
                                if (_set1.ContainsKey(EnemyType.Mu1) == false)
                                {
                                    _set1[EnemyType.Mu1] = go;
                                }
                                else
                                {
                                    _set1[EnemyType.Mu2] = go;
                                }
                                break;
                        }
                        if (_set1.Count == 4)
                        {
                            Log(State.LogLevelEnum.Debug, null, "Ready for set 1 automarkers");
                            AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                            ap.Assign(Set1.Roles["Yan"], _set1[EnemyType.Yan]);
                            ap.Assign(Set1.Roles["GimmeCat"], _set1[EnemyType.GimmeCat]);
                            ap.Assign(Set1.Roles["Mu1"], _set1[EnemyType.Mu1]);
                            ap.Assign(Set1.Roles["Mu2"], _set1[EnemyType.Mu2]);
                            _state.ExecuteAutomarkers(ap, Timing);
                            setnum++;
                        }
                        break;
                    case 2:
                        switch (nameid)
                        {
                            case NameManta:
                                if (go.Position.X < 100.0f)
                                {
                                    _set2[EnemyType.MantaW] = go;
                                }
                                else
                                {
                                    _set2[EnemyType.MantaE] = go;
                                }                                    
                                break;
                            case NameMu:
                                if (_set2.ContainsKey(EnemyType.Mu1) == false)
                                {
                                    _set2[EnemyType.Mu1] = go;
                                }
                                else
                                {
                                    _set2[EnemyType.Mu2] = go;
                                }
                                break;
                        }
                        if (_set2.Count == 4)
                        {
                            Log(State.LogLevelEnum.Debug, null, "Ready for set 2 automarkers");
                            AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                            ap.Assign(Set2.Roles["MantaW"], _set2[EnemyType.MantaW]);
                            ap.Assign(Set2.Roles["MantaE"], _set2[EnemyType.MantaE]);
                            ap.Assign(Set2.Roles["Mu1"], _set2[EnemyType.Mu1]);
                            ap.Assign(Set2.Roles["Mu2"], _set2[EnemyType.Mu2]);
                            _state.ExecuteAutomarkers(ap, Timing);
                            setnum++;
                        }
                        break;
                    case 3:
                        switch (nameid)
                        {
                            case NameYan:
                                _set3[EnemyType.Yan] = go;
                                break;
                            case NameCat:
                                _set3[EnemyType.GimmeCat] = go;
                                break;
                            case NameJabberwock:
                                _set3[EnemyType.Jabberwock] = go;
                                break;
                        }
                        if (_set3.Count == 3)
                        {
                            AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                            ap.Assign(Set3.Roles["Jabberwock"], _set3[EnemyType.Jabberwock]);
                            ap.Assign(Set3.Roles["GimmeCat"], _set3[EnemyType.GimmeCat]);
                            ap.Assign(Set3.Roles["Yan"], _set3[EnemyType.Yan]);
                            _state.ExecuteAutomarkers(ap, Timing);
                            setnum++;
                        }
                        break;
                    case 4:
                        switch (nameid)
                        {
                            case NameYan:
                                _set4[EnemyType.Yan] = go;
                                break;
                            case NameCat:
                                _set4[EnemyType.GimmeCat] = go;
                                break;
                            case NameJabberwock:
                                _set4[EnemyType.Jabberwock] = go;
                                break;
                            case NameManta:
                                if (go.Position.X < 100.0f)
                                {
                                    _set4[EnemyType.MantaW] = go;
                                }
                                else
                                {
                                    _set4[EnemyType.MantaE] = go;
                                }
                                break;
                            case NameMu:
                                if (_set4.ContainsKey(EnemyType.Mu1) == false)
                                {
                                    _set4[EnemyType.Mu1] = go;
                                }
                                else
                                {
                                    _set4[EnemyType.Mu2] = go;
                                }
                                break;
                        }
                        if (_set4.Count == 7)
                        {
                            AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                            ap.Assign(Set4.Roles["Jabberwock"], _set4[EnemyType.Jabberwock]);
                            ap.Assign(Set4.Roles["GimmeCat"], _set4[EnemyType.GimmeCat]);
                            ap.Assign(Set4.Roles["Yan"], _set4[EnemyType.Yan]);
                            ap.Assign(Set4.Roles["MantaW"], _set4[EnemyType.MantaW]);
                            ap.Assign(Set4.Roles["MantaE"], _set4[EnemyType.MantaE]);
                            ap.Assign(Set4.Roles["Mu1"], _set4[EnemyType.Mu1]);
                            ap.Assign(Set4.Roles["Mu2"], _set4[EnemyType.Mu2]);
                            _state.ExecuteAutomarkers(ap, Timing);
                            setnum++;
                        }
                        break;
                }
            }

        }

        #endregion

        #region DrawMoonbeam

        public class DrawMoonbeam : Core.ContentItem
        {

            public override FeaturesEnum Features => FeaturesEnum.Drawing;

            [AttributeOrderNumber(1000)]
            public Vector4 EarlyHighlightColor { get; set; } = new Vector4(1.0f, 1.0f, 0.0f, 0.05f);

            [AttributeOrderNumber(1001)]
            public Vector4 SoonHighlightColor { get; set; } = new Vector4(1.0f, 0.0f, 0.0f, 0.1f);

            [DebugOption]
            [AttributeOrderNumber(2000)]
            public System.Action Test { get; set; }

            public int _activeLr = 0;
            public int _activeUd = 0;
            public DateTime _active = DateTime.MinValue;
            public List<Tuple<DateTime, int>> _cleaves = new List<Tuple<DateTime, int>>();

            public DrawMoonbeam(State state) : base(state)
            {
                Enabled = false;
                Test = new System.Action(() => TestFunctionality());
            }

            public override void Reset()
            {
                base.Reset();
                _cleaves.Clear();
            }

            public void FeedMoonbeam(IGameObject go, int actionId)
            {
                Log(State.LogLevelEnum.Debug, null, "Registered action {0} on {1}", actionId, go);
                int cleaveId = 0; // 1 north 2 south 3 west 4 east
                if (go.Position.X < 95.0f)
                {
                    // west
                    cleaveId = (actionId == AbilityMoonbeamLeft) ? 1 : 2;
                }
                else if (go.Position.X > 105.0f)
                {
                    // east
                    cleaveId = (actionId == AbilityMoonbeamLeft) ? 2 : 1;
                }
                else
                {
                    if (go.Position.Z < 95.0f)
                    {
                        // north
                        cleaveId = (actionId == AbilityMoonbeamLeft) ? 4 : 3;
                    }
                    else
                    {
                        // south
                        cleaveId = (actionId == AbilityMoonbeamLeft) ? 3 : 4;
                    }
                }
                _cleaves.Add(new Tuple<DateTime, int>(DateTime.Now.AddMilliseconds(8700), cleaveId));                
            }

            public void TestFunctionality()
            {
                if (_cleaves.Count > 0)
                {
                    _cleaves.Clear();
                    return;
                }
                _state.InvokeZoneChange((int)ZoneEnum.M8s);
                int ofs = 8;
                _cleaves.Add(new Tuple<DateTime, int>(DateTime.Now.AddSeconds(ofs + 0), 4));
                _cleaves.Add(new Tuple<DateTime, int>(DateTime.Now.AddSeconds(ofs + 3), 1));
                _cleaves.Add(new Tuple<DateTime, int>(DateTime.Now.AddSeconds(ofs + 6), 4));
                _cleaves.Add(new Tuple<DateTime, int>(DateTime.Now.AddSeconds(ofs + 9), 2));
            }

            private void DrawRectangle(ImDrawListPtr draw, float x1, float y1, float x2, float y2, float z, Vector4 col)
            {
                Vector3 t1 = new Vector3(x1, z, y1);
                Vector3 t2 = new Vector3(x2, z, y1);
                Vector3 t3 = new Vector3(x2, z, y2);
                Vector3 t4 = new Vector3(x1, z, y2);
                Vector3 v1 = _state.plug._ui.TranslateToScreen(t1.X, t1.Y, t1.Z);
                Vector3 v2 = _state.plug._ui.TranslateToScreen(t2.X, t2.Y, t2.Z);
                Vector3 v3 = _state.plug._ui.TranslateToScreen(t3.X, t3.Y, t3.Z);
                Vector3 v4 = _state.plug._ui.TranslateToScreen(t4.X, t4.Y, t4.Z);
                draw.PathLineTo(new Vector2(v1.X, v1.Y));
                draw.PathLineTo(new Vector2(v2.X, v2.Y));
                draw.PathLineTo(new Vector2(v3.X, v3.Y));
                draw.PathLineTo(new Vector2(v4.X, v4.Y));
                draw.PathLineTo(new Vector2(v1.X, v1.Y));
                draw.PathFillConvex(
                    ImGui.GetColorU32(col)
                );
            }

            protected override bool ExecutionImplementation()
            {
                ImDrawListPtr draw;
                if (_state.StartDrawing(out draw) == false)
                {
                    return false;
                }
                if (_cleaves.Count == 0)
                {
                    return false;
                }
                foreach (Tuple<DateTime, int> c in _cleaves)
                {
                    if (DateTime.Now > c.Item1)
                    {
                        continue;
                    }
                    if (DateTime.Now < c.Item1.AddSeconds(-6))
                    {
                        continue;
                    }
                    float x = 100.0f;
                    float y = 100.0f;
                    float z = 0.0f;
                    float x1 = x, x2 = x, y1 = y, y2 = y;
                    // // 1 north 2 south 3 west 4 east
                    switch (c.Item2)
                    {
                        case 1:
                            x1 -= 20.0f;
                            x2 += 20.0f;
                            y1 -= 20.0f;
                            break;
                        case 2:
                            x1 -= 20.0f;
                            x2 += 20.0f;
                            y2 += 20.0f;
                            break;
                        case 3:
                            y1 -= 20.0f;
                            y2 += 20.0f;
                            x1 -= 20.0f;
                            break;
                        case 4:
                            y1 -= 20.0f;
                            y2 += 20.0f;
                            x2 += 20.0f;
                            break;
                    }
                    DrawRectangle(draw, x1, y1, x2, y2, z, DateTime.Now < c.Item1.AddSeconds(-2) ? EarlyHighlightColor : SoonHighlightColor);
                }
                return true;
            }

        }

        #endregion

        public DtRaidCruiser(State st) : base(st)
        {
            st.OnZoneChange += OnZoneChange;
        }

        protected override bool ExecutionImplementation()
        {
            if (CurrentZone != ZoneEnum.None)
            {
                return base.ExecutionImplementation();
            }
            return false;
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
                _state.OnCastBegin += _state_OnCastBegin;
                _state.OnCombatantAdded += _state_OnCombatantAdded;
            }
        }

        private void _state_OnCombatantAdded(IGameObject go)
        {
            if (go is ICharacter)
            {
                ICharacter ch = go as ICharacter;
                switch (ch.NameId)
                {
                    case NameCat:
                    case NameJabberwock:
                    case NameManta:
                    case NameMu:
                    case NameYan:
                        _soulSugarAm.FeedCombatant(go);
                        break;
                }
            }
        }

        private void _state_OnCastBegin(uint src, uint dest, ushort actionId, float castTime, float rotation)
        {
            switch (actionId)
            {
                case AbilityMoonbeamLeft:
                case AbilityMoonbeamRight:
                    if (CurrentZone == ZoneEnum.M8s && _drawMoonbeam.Active == true)
                    {
                        _drawMoonbeam.FeedMoonbeam(_state.GetActorById(dest), actionId);
                    }
                    break;
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
                _state.OnCombatantAdded -=  _state_OnCombatantAdded;
                _state.OnCastBegin -= _state_OnCastBegin;
                _subbed = false;
            }
        }

        private void OnCombatChange(bool inCombat)
        {
            Reset();
            if (inCombat == true)
            {
                SubscribeToEvents();
            }
            else
            {
                UnsubscribeFromEvents();
            }
        }

        public override void Reset()
        {
            base.Reset();
        }

        private void OnZoneChange(ushort newZone)
        {
            if (Enum.TryParse<ZoneEnum>(newZone.ToString(), out ZoneEnum parsedZone) == true)
            {
                if (Enum.IsDefined<ZoneEnum>(parsedZone) == false)
                {
                    parsedZone = ZoneEnum.None;
                }
            }
            else
            {
                parsedZone = ZoneEnum.None;
            }
            if (parsedZone != ZoneEnum.None && CurrentZone == ZoneEnum.None)
            {
                Log(State.LogLevelEnum.Info, null, parsedZone + " Content available");
                CurrentZone = parsedZone;
                _state.OnCombatChange += OnCombatChange;
                switch (CurrentZone)
                {
                    case ZoneEnum.M6s:
                        _soulSugarAm = (SoulSugarAm)Items["SoulSugarAm"];
                        break;
                    case ZoneEnum.M8s:
                        _drawMoonbeam = (DrawMoonbeam)Items["DrawMoonbeam"];
                        break;
                }
                LogItems();
            }
            else if (parsedZone == ZoneEnum.None && CurrentZone != ZoneEnum.None)
            {
                Log(State.LogLevelEnum.Info, null, CurrentZone + " content unavailable");
                CurrentZone = ZoneEnum.None;
                _state.OnCombatChange -= OnCombatChange;
            }
        }

    }

}
