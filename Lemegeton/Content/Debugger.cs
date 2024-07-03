using Dalamud.Game.ClientState.Objects.Types;
using ImGuiNET;
using Lemegeton.Core;
using System;
using System.Numerics;
using System.Text;
using System.IO;
using static Lemegeton.Core.State;
using GameObject = Dalamud.Game.ClientState.Objects.Types.IGameObject;
using GameObjectPtr = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using CharacterStruct = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using Vector3 = System.Numerics.Vector3;
using System.Collections.Generic;

namespace Lemegeton.Content
{

    public class Debugger : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

        public class ObjectMonitor : Core.ContentItem
        {

            public override FeaturesEnum Features => FeaturesEnum.Drawing;

            [AttributeOrderNumber(1000)]
            public Vector4 TextColor { get; set; } = new Vector4(0.8f, 0.8f, 0.8f, 1.0f);

            [AttributeOrderNumber(2000)]
            public bool ShowTextBg { get; set; } = true;
            [AttributeOrderNumber(2001)]
            public Vector4 BgColor { get; set; } = new Vector4(0.0f, 0.0f, 0.0f, 0.7f);

            [AttributeOrderNumber(3000)]
            public bool TagPlayers { get; set; } = true;
            [AttributeOrderNumber(3001)]
            public bool TagEventObjs { get; set; } = true;
            [AttributeOrderNumber(3002)]
            public bool TagBattleNpcs { get; set; } = true;
            [AttributeOrderNumber(3003)]
            public bool TagOthers { get; set; } = true;

            [AttributeOrderNumber(4001)]
            public bool OnlyVisible { get; set; } = true;
            [AttributeOrderNumber(4002)]
            public bool OnlyTargettable { get; set; } = true;

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
                float mul = 18.0f / defSize;
                Vector2 disp = ImGui.GetIO().DisplaySize;
                foreach (GameObject go in _state.ot)
                {
                    int renderFlags;
                    bool targettable;
                    unsafe
                    {
                        GameObjectPtr* gop = (GameObjectPtr*)go.Address;
                        renderFlags = gop->RenderFlags;
                        targettable = gop->GetIsTargetable();
                    }
                    if (targettable == false && OnlyTargettable == true)
                    {
                        continue;
                    }
                    if (renderFlags != 0 && OnlyVisible == true)
                    {
                        continue;
                    }
                    switch (go.ObjectKind)
                    {
                        case Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player:
                            if (TagPlayers == false)
                            {
                                continue;
                            }
                            break;
                        case Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventObj:
                            if (TagEventObjs == false)
                            {
                                continue;
                            }
                            break;
                        case Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc:
                            if (TagBattleNpcs == false)
                            {
                                continue;
                            }
                            break;
                        default:
                            if (TagOthers == false)
                            {
                                continue;
                            }
                            break;
                    }
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine(String.Format("ID: {0} ({1})", go.GameObjectId.ToString(), go.GameObjectId.ToString("X8")));
                    sb.AppendLine(String.Format("Addr: {0} ({1})", go.Address.ToString(), go.Address.ToString("X8")));
                    ICharacter ch = null;
                    if (go is ICharacter)
                    {
                        ch = (ICharacter)go;
                    }
                    if (ch != null)
                    {                        
                        sb.AppendLine(String.Format("NameId: {0} Name: {1}", ch.NameId, go.Name.ToString()));
                    }
                    else
                    {
                        sb.AppendLine(String.Format("Name: {0}", go.Name.ToString()));
                    }
                    if (go is IBattleChara)
                    {
                        IBattleChara bc = (IBattleChara)go;
                        sb.AppendLine(String.Format("HP: {0}/{1}", bc.CurrentHp, bc.MaxHp));
                        if (ch != null)
                        {
                            unsafe
                            {
                                CharacterStruct* chs = (CharacterStruct*)ch.Address;
                                sb.AppendLine(String.Format("Job: {0} Omen: {1:X} VFX: {2:X} VFX2: {3:X}", bc.ClassJob.Id, (IntPtr)chs->Vfx.Omen, (IntPtr)chs->Vfx.VfxData, (IntPtr)chs->Vfx.VfxData2));
                            }
                        }
                        else
                        {
                            sb.AppendLine(String.Format("Job: {0}", bc.ClassJob.Id));
                        }
                        sb.AppendLine(String.Format("Flags: {0}", _state.GetStatusFlags(bc)));
                        if (bc.CastActionId > 0)
                        {
                            sb.AppendLine(String.Format("Casting: {0} -> {1:X8}", bc.CastActionId, bc.CastTargetObjectId));
                        }
                    }                    
                    sb.AppendLine(String.Format("Position: {0},{1},{2}", go.Position.X, go.Position.Y, go.Position.Z));
                    if (ch != null)
                    {
                        unsafe
                        {
                            CharacterStruct* chs = (CharacterStruct*)ch.Address;
                            sb.AppendLine(String.Format("ObjectKind: {0} ModelId: {1}", go.ObjectKind, chs->CharacterData.ModelCharaId));
                        }
                    }
                    else
                    {
                        sb.AppendLine(String.Format("ObjectKind: {0}", go.ObjectKind));
                    }
                    sb.AppendLine(String.Format("SubKind: {0} DataId: {0}", go.SubKind, go.DataId));
                    sb.AppendLine(String.Format("RenderFlags: {0} Targettable: {1}", renderFlags, targettable));
                    string text = sb.ToString();
                    Vector2 sz = ImGui.CalcTextSize(text);
                    Vector3 temp = _state.plug._ui.TranslateToScreen(go.Position.X, go.Position.Y, go.Position.Z);
                    sz.X *= mul;
                    sz.Y *= mul;
                    pt.X = temp.X - (sz.X / 2.0f);
                    pt.Y = temp.Y + 10.0f;
                    if (pt.X + sz.X < 0.0f || pt.X > disp.X)
                    {
                        continue;
                    }
                    if (pt.Y + sz.Y < 0.0f || pt.Y > disp.Y)
                    {
                        continue;
                    }                    
                    if (ShowTextBg == true)
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
                        18.0f,
                        pt,
                        ImGui.GetColorU32(TextColor),
                        text
                    );
                }
                return true;
            }

            public ObjectMonitor(State state) : base(state)
            {
                Enabled = false;
            }

        }

        public class StressTest : Core.ContentItem
        {

            public override FeaturesEnum Features => TestAutomarkers == true ? FeaturesEnum.Automarker : FeaturesEnum.None;

            [AttributeOrderNumber(1000)]
            public bool TestAutomarkers { get; set; } = true;
            [AttributeOrderNumber(1001)]
            public int AmFails { get; private set; } = 0;

            [AttributeOrderNumber(1100)]
            public bool TestAutomarkerRapidfire { get; set; } = true;

            private int _amTestCycle = 0;
            private DateTime _amTestCycleNext = DateTime.MinValue;
            private AutomarkerPayload _amLastPayload = null;            

            private AutomarkerSigns Signs;

            protected override bool ExecutionImplementation()
            {
                if (TestAutomarkers == true)
                {
                    if (DateTime.Now > _amTestCycleNext)
                    {
                        _amTestCycle++;
                        if (_amTestCycle > 6)
                        {
                            _amTestCycle = 1;
                        }
                        Log(LogLevelEnum.Debug, null, "Running AM test step {0}", _amTestCycle);
                        switch (_amTestCycle)
                        {
                            case 1: // full clear
                                {
                                    _amLastPayload = null;
                                    _state.ClearAutoMarkers();
                                    _amTestCycleNext = DateTime.Now.AddSeconds(2.0f);
                                }
                                break;
                            case 3: // assign on full clear
                            case 5: // reassign/overwrite
                                {
                                    _amLastPayload = Signs.TestFunctionality(_state, null, _state.cfg.DefaultAutomarkerTiming, false, false);
                                    int sets = 0;
                                    foreach (var ass in _amLastPayload.assignments)
                                    {
                                        sets += ass.Value.Count;
                                    }
                                    double delay = _state.cfg.DefaultAutomarkerTiming.IniDelayMax + 0.5f
                                        + (_state.cfg.DefaultAutomarkerTiming.SubDelayMax * (float)sets);
                                    _amTestCycleNext = DateTime.Now.AddSeconds(delay);
                                }
                                break;
                            case 2: // check result
                            case 4: // check result
                            case 6: // check result
                                CheckAmResult();
                                _amTestCycleNext = DateTime.Now.AddSeconds(2);
                                break;
                        }
                    }
                }
                else if (TestAutomarkerRapidfire == true)
                {
                    if (DateTime.Now > _amTestCycleNext)
                    {
                        _amTestCycle++;
                        switch (_amTestCycle % 2)
                        {
                            case 0: // full clear
                                {
                                    _state.ClearAutoMarkers();
                                    _amTestCycleNext = DateTime.Now.AddSeconds(0.5f);
                                }
                                break;
                            case 1:
                                {
                                    AutomarkerTiming timing = new AutomarkerTiming() { 
                                        IniDelayMin = 0.0f, IniDelayMax = 0.0f, SubDelayMin = 0.0f, SubDelayMax = 0.0f,
                                        TimingType = AutomarkerTiming.TimingTypeEnum.Explicit, Parent = _state.cfg.DefaultAutomarkerTiming
                                    };
                                    Signs.TestFunctionality(_state, null, timing, false, false);
                                    _amTestCycleNext = DateTime.Now.AddSeconds(0.5f);
                                }
                                break;
                        }
                    }
                }
                return true;
            }

            private void CheckAmResult()
            {
                if (_amLastPayload != null)
                {
                    foreach (var kp in _amLastPayload.assignments)
                    {
                        AutomarkerSigns.SignEnum expectedSign = kp.Key;
                        foreach (GameObject expectedActor in kp.Value)
                        {
                            bool ret;
                            AutomarkerSigns.SignEnum currentSign;
                            if (_state.cfg.AutomarkerSoft == true)
                            {
                                ret = true;
                                currentSign = AutomarkerSigns.SignEnum.None;
                                foreach (KeyValuePair<AutomarkerSigns.SignEnum, ulong> kp2 in _state.SoftMarkers)
                                {
                                    if (kp2.Value == expectedActor.GameObjectId)
                                    {
                                        currentSign = kp2.Key;
                                    }
                                }
                            }
                            else
                            { 
                                ret = _state.GetCurrentMarker(expectedActor.GameObjectId, out currentSign);
                            }
                            if (ret == false)
                            {
                                Log(LogLevelEnum.Debug, null, "Couldn't figure out marker on {0}", expectedActor);
                                AmFails++;
                            }
                            else
                            {
                                if (
                                    (
                                        (expectedSign == AutomarkerSigns.SignEnum.AttackNext)
                                        &&
                                        (
                                            currentSign == AutomarkerSigns.SignEnum.Attack1
                                            ||
                                            currentSign == AutomarkerSigns.SignEnum.Attack2
                                            ||
                                            currentSign == AutomarkerSigns.SignEnum.Attack3
                                            ||
                                            currentSign == AutomarkerSigns.SignEnum.Attack4
                                            ||
                                            currentSign == AutomarkerSigns.SignEnum.Attack5
                                        )
                                    )
                                    ||
                                    (
                                        (expectedSign == AutomarkerSigns.SignEnum.BindNext)
                                        &&
                                        (
                                            currentSign == AutomarkerSigns.SignEnum.Bind1
                                            ||
                                            currentSign == AutomarkerSigns.SignEnum.Bind2
                                            ||
                                            currentSign == AutomarkerSigns.SignEnum.Bind3
                                        )
                                    )
                                    ||
                                    (
                                        (expectedSign == AutomarkerSigns.SignEnum.IgnoreNext)
                                        &&
                                        (
                                            currentSign == AutomarkerSigns.SignEnum.Ignore1
                                            ||
                                            currentSign == AutomarkerSigns.SignEnum.Ignore2
                                        )
                                    )
                                    ||
                                    (currentSign == expectedSign)
                                )
                                {
                                    Log(LogLevelEnum.Debug, null, "{0} has {1} as expected ({2})", expectedActor, currentSign, expectedSign);
                                }
                                else
                                {
                                    Log(LogLevelEnum.Error, null, "{0} has {1} instead of expected {2}", expectedActor, currentSign, expectedSign);
                                    AmFails++;
                                }
                            }
                        }
                    }
                }
                else
                {
                    Party pty = _state.GetPartyMembers();
                    foreach (Party.PartyMember pm in pty.Members)
                    {
                        bool ret = _state.GetCurrentMarker(pm.ObjectId, out AutomarkerSigns.SignEnum currentSign);
                        if (ret == false)
                        {
                            Log(LogLevelEnum.Debug, null, "Couldn't figure out marker on {0}", pm.GameObject);
                            AmFails++;
                        }
                        else
                        {
                            if (currentSign == AutomarkerSigns.SignEnum.None)
                            {
                                Log(LogLevelEnum.Debug, null, "{0} doesn't have a sign as expected", pm.GameObject);
                            }
                            else
                            {
                                Log(LogLevelEnum.Error, null, "{0} has {1} instead of nothing", pm.GameObject, currentSign);
                                AmFails++;
                            }
                        }
                    }
                }
            }

            public StressTest(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Signs.SetRole("Ignore1", AutomarkerSigns.SignEnum.Ignore1, false);
                Signs.SetRole("Ignore2", AutomarkerSigns.SignEnum.Ignore2, false);
                Signs.SetRole("Bind1", AutomarkerSigns.SignEnum.Bind1, false);
                Signs.SetRole("Bind2", AutomarkerSigns.SignEnum.Bind2, false);
                Signs.SetRole("Attack1", AutomarkerSigns.SignEnum.Attack1, false);
                Signs.SetRole("Attack2", AutomarkerSigns.SignEnum.Attack2, false);
                Signs.SetRole("Attack3", AutomarkerSigns.SignEnum.Attack3, false);
                Signs.SetRole("Attack4", AutomarkerSigns.SignEnum.Attack4, false);
                OnEnabledChanged += StressTest_OnEnabledChanged;
            }

            private void StressTest_OnEnabledChanged(bool newState)
            {
                _amTestCycle = 0;
                AmFails = 0;
                _amTestCycleNext = DateTime.MinValue;
                _amLastPayload = null;
            }
        }

        public class EventLogger : Core.ContentItem
        {

            public override FeaturesEnum Features => FeaturesEnum.None;

            [AttributeOrderNumber(2000)]
            public bool LogInCombat { get; set; } = true;

            [AttributeOrderNumber(2001)]
            public bool LogOutsideCombat { get; set; } = true;

            [AttributeOrderNumber(3000)]
            public bool LogToDalamudLog { get; set; } = true;
            [AttributeOrderNumber(3001)]
            public bool LogToFile { get; set; } = false;
            [AttributeOrderNumber(3002)]
            public string LogFilename { get; set; } = "lemegeton_events.log";
            [AttributeOrderNumber(3003)]
            public string CurrentLogFilename { get; private set; } = "";

            [AttributeOrderNumber(4000)]
            public bool IncludeLocation { get; set; } = true;
            [AttributeOrderNumber(4001)]
            public bool ResolveNames { get; set; } = true;
            
            [AttributeOrderNumber(4100)]
            public bool LogActorControl { get; set; } = true;

            private bool _MonitorOmen = false;
            [AttributeOrderNumber(5001)]
            public bool MonitorOmen
            {
                get
                {
                    return _MonitorOmen;
                }
                set
                {
                    if (_MonitorOmen != value)
                    {
                        _omenTracking.Clear();
                        _MonitorOmen = value;
                    }
                }
            }

            private bool _MonitorVFX = false;
            [AttributeOrderNumber(5002)]
            public bool MonitorVFX
            {
                get
                {
                    return _MonitorVFX;
                }
                set
                {
                    if (_MonitorVFX != value)
                    {
                        _vfx1Tracking.Clear();
                        _vfx2Tracking.Clear();
                        _MonitorVFX = value;
                    }
                }
            }

            private Dictionary<IntPtr, IntPtr> _omenTracking = new Dictionary<IntPtr, IntPtr>();
            private Dictionary<IntPtr, IntPtr> _vfx1Tracking = new Dictionary<IntPtr, IntPtr>();
            private Dictionary<IntPtr, IntPtr> _vfx2Tracking = new Dictionary<IntPtr, IntPtr>();

            private bool _subbed = false;
            private bool _firstRun = true;

            public EventLogger(State state) : base(state)
            {
                OnActiveChanged += EventLogger_OnActiveChanged;
            }

            public override void Reset()
            {
                base.Reset();
                _omenTracking.Clear();
                _vfx1Tracking.Clear();
                _vfx2Tracking.Clear();
            }

            private void EventLogger_OnActiveChanged(bool newState)
            {
                if (newState == true)
                {
                    string realfn = LogFilename.Trim();
                    if (realfn.Length > 0)
                    {
                        FileInfo fi = new FileInfo(realfn);
                        realfn = fi.FullName;
                        Log(LogLevelEnum.Debug, null, "EventLogger file set to {0}", realfn);
                        lock (this)
                        {
                            CurrentLogFilename = realfn;
                        }
                    }
                    else
                    {
                        lock (this)
                        {
                            CurrentLogFilename = "";
                        }
                    }
                    SubscribeToEvents();
                }
                else
                {
                    lock (this)
                    {
                        CurrentLogFilename = "";
                    }
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
                    Reset();
                    _state.OnAction += OnAction;
                    _state.OnCastBegin += OnCastBegin;
                    _state.OnCombatChange += OnCombatChange;
                    _state.OnHeadMarker += OnHeadMarker;
                    _state.OnStatusChange += OnStatusChange;
                    _state.OnZoneChange += OnZoneChange;
                    _state.OnTether += OnTether;
                    _state.OnMapEffect += OnMapEffect;
                    _state.OnDirectorUpdate += OnDirectorUpdate;
                    _state.OnCombatantAdded += OnCombatantAdded;
                    _state.OnCombatantRemoved += OnCombatantRemoved;
                    _state.OnEventPlay += OnEventPlay;
                    _state.OnEventPlay64 += OnEventPlay64;
                    _state.OnActorControl += OnActorControl;
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
                    Reset();
                    _state.OnActorControl -= OnActorControl;
                    _state.OnEventPlay64 -= OnEventPlay64;
                    _state.OnEventPlay -= OnEventPlay;
                    _state.OnCombatantRemoved -= OnCombatantRemoved;
                    _state.OnCombatantAdded -= OnCombatantAdded;
                    _state.OnDirectorUpdate -= OnDirectorUpdate;
                    _state.OnMapEffect -= OnMapEffect;
                    _state.OnTether -= OnTether;
                    _state.OnAction -= OnAction;
                    _state.OnCastBegin -= OnCastBegin;
                    _state.OnCombatChange -= OnCombatChange;
                    _state.OnHeadMarker -= OnHeadMarker;
                    _state.OnStatusChange -= OnStatusChange;
                    _state.OnZoneChange -= OnZoneChange;
                    _subbed = false;
                }
            }

            private bool GoodToLog()
            {
                if (LogInCombat == false || LogOutsideCombat == false)
                {
                    bool inc = _state._inCombat;
                    if (LogInCombat == false && inc == true)
                    {
                        return false;
                    }
                    if (LogOutsideCombat == false && inc == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            private string FormatGameObject(GameObject go)
            {
                if (IncludeLocation == true)
                {
                    return go == null ? "null" : String.Format("{0}({1} - {2}) at {3} pos {4},{5},{6} rot {7}", go.GameObjectId.ToString("X"), go.Name.ToString(), go.ObjectKind, go.Address.ToString("X"),
                        go.Position.X, go.Position.Y, go.Position.Z, go.Rotation
                    );
                }
                else
                {
                    return go == null ? "null" : String.Format("{0}({1} - {2}) at {3}", go.GameObjectId.ToString("X"), go.Name.ToString(), go.ObjectKind, go.Address.ToString("X"));
                }
            }

            private string FormatStatusId(uint statusId)
            {
                if (ResolveNames == true)
                {
                    string name = _state.plug.GetStatusName(statusId).Trim();
                    return String.Format("{0} ({1})", statusId, name.Length > 0 ? name : "(null)");
                }
                else
                {
                    return String.Format("{0}", statusId);
                }
            }

            private string FormatActionId(uint actionId)
            {
                if (ResolveNames == true)
                {
                    string name = _state.plug.GetActionName(actionId).Trim();
                    return String.Format("{0} ({1})", actionId, name.Length > 0 ? name : "(null)");
                }
                else
                {
                    return String.Format("{0}", actionId);
                }
            }

            private void LogEventToFile(string msg)
            {
                try
                {
                    lock (this)
                    {
                        if (CurrentLogFilename != "")
                        {
                            File.AppendAllText(CurrentLogFilename, "[" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + "] " + msg + Environment.NewLine);
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            protected override bool ExecutionImplementation()
            {
                if (_firstRun == true)
                {
                    EventLogger_OnActiveChanged(Active);
                    _firstRun = false;
                }
                if (MonitorOmen == true)
                {
                    MonitorOmens();
                }
                if (MonitorVFX == true)
                {
                    MonitorVFXs();
                }
                return true;
            }

            private void MonitorOmens()
            {                
                foreach (GameObject go in _state.ot)
                {
                    if (
                        (go.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc)
                        &&
                        (go.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player)
                        &&
                        (go.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventObj)
                    )
                    {
                        continue;
                    }
                    if (go is ICharacter)
                    {
                        ICharacter ch = (ICharacter)go;
                        unsafe
                        {
                            CharacterStruct* chs = (CharacterStruct*)ch.Address;
                            IntPtr my, omen = (IntPtr)chs->Vfx.Omen;
                            if (_omenTracking.TryGetValue(go.Address, out my) == true)
                            {
                                if (omen != my)
                                {
                                    string fmt = "OmenTracking: {0} Changed {1:X} -> {2:X}";
                                    if (LogToDalamudLog == true)
                                    {
                                        Log(LogLevelEnum.Debug, null, fmt, FormatGameObject(go), my, omen);
                                    }
                                    if (LogToFile == true)
                                    {
                                        LogEventToFile(String.Format(fmt, FormatGameObject(go), my, omen));
                                    }
                                    if (omen == IntPtr.Zero)
                                    {
                                        _omenTracking.Remove(go.Address);
                                    }
                                    else
                                    {
                                        _omenTracking[go.Address] = omen;
                                    }
                                }
                            }
                            else if (omen != IntPtr.Zero)
                            {                                
                                _omenTracking[go.Address] = omen;
                                string fmt = "OmenTracking: {0} Started {1:X}";
                                if (LogToDalamudLog == true)
                                {
                                    Log(LogLevelEnum.Debug, null, fmt, FormatGameObject(go), omen);
                                }
                                if (LogToFile == true)
                                {
                                    LogEventToFile(String.Format(fmt, FormatGameObject(go), omen));
                                }
                            }
                        }
                    }
                }
            }

            private void MonitorVFXs()
            {
                foreach (GameObject go in _state.ot)
                {
                    if (
                        (go.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc)
                        &&
                        (go.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player)
                        &&
                        (go.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventObj)
                    )
                    {
                        continue;
                    }
                    if (go is ICharacter)
                    {
                        ICharacter ch = (ICharacter)go;
                        unsafe
                        {
                            CharacterStruct* chs = (CharacterStruct*)ch.Address;                            
                            IntPtr my1, my2, vfx1 = (IntPtr)chs->Vfx.VfxData, vfx2 = (IntPtr)chs->Vfx.VfxData2;
                            if (_vfx1Tracking.TryGetValue(go.Address, out my1) == true)
                            {
                                if (vfx1 != my1)
                                {
                                    string fmt = "VFXTracking: {0} Changed VFX1 {1:X} -> {2:X}";
                                    if (LogToDalamudLog == true)
                                    {
                                        Log(LogLevelEnum.Debug, null, fmt, FormatGameObject(go), my1, vfx1);
                                    }
                                    if (LogToFile == true)
                                    {
                                        LogEventToFile(String.Format(fmt, FormatGameObject(go), my1, vfx1));
                                    }
                                    if (vfx1 == IntPtr.Zero)
                                    {
                                        _vfx1Tracking.Remove(go.Address);
                                    }
                                    else
                                    {
                                        _vfx1Tracking[go.Address] = vfx1;
                                    }
                                }
                            }
                            else if (vfx1 != IntPtr.Zero)
                            {
                                _vfx1Tracking[go.Address] = vfx1;
                                string fmt = "VFXTracking: {0} Started VFX1 {1:X}";
                                if (LogToDalamudLog == true)
                                {
                                    Log(LogLevelEnum.Debug, null, fmt, FormatGameObject(go), vfx1);
                                }
                                if (LogToFile == true)
                                {
                                    LogEventToFile(String.Format(fmt, FormatGameObject(go), vfx1));
                                }
                            }
                            if (_vfx2Tracking.TryGetValue(go.Address, out my2) == true)
                            {
                                if (vfx2 != my2)
                                {
                                    string fmt = "VFXTracking: {0} Changed VFX2 {1:X} -> {2:X}";
                                    if (LogToDalamudLog == true)
                                    {
                                        Log(LogLevelEnum.Debug, null, fmt, FormatGameObject(go), my2, vfx2);
                                    }
                                    if (LogToFile == true)
                                    {
                                        LogEventToFile(String.Format(fmt, FormatGameObject(go), my2, vfx2));
                                    }
                                    if (vfx2 == IntPtr.Zero)
                                    {
                                        _vfx2Tracking.Remove(go.Address);
                                    }
                                    else
                                    {
                                        _vfx2Tracking[go.Address] = vfx2;
                                    }
                                }
                            }
                            else if (vfx2 != IntPtr.Zero)
                            {
                                _vfx2Tracking[go.Address] = vfx2;
                                string fmt = "VFXTracking: {0} Started VFX2 {1:X}";
                                if (LogToDalamudLog == true)
                                {
                                    Log(LogLevelEnum.Debug, null, fmt, FormatGameObject(go), vfx2);
                                }
                                if (LogToFile == true)
                                {
                                    LogEventToFile(String.Format(fmt, FormatGameObject(go), vfx2));
                                }
                            }
                        }
                    }
                }
            }

            private void OnEventPlay64()
            {
                if (GoodToLog() == false)
                {
                    return;
                }
                string fmt = "InvokeEventPlay64";
                if (LogToDalamudLog == true)
                {
                    Log(LogLevelEnum.Debug, null, fmt);
                }
                if (LogToFile == true)
                {
                    LogEventToFile(String.Format(fmt));
                }
            }

            private void OnEventPlay(uint actorId, uint eventId, ushort scene, uint flags, uint param1, ushort param2, byte param3, uint param4)
            {
                if (GoodToLog() == false)
                {
                    return;
                }
                GameObject actor = _state.GetActorById(actorId);
                string fmt = "InvokeEventPlay: {0:X8} {1} {2} {3} {4} {5} {6} {7}";
                if (LogToDalamudLog == true)
                {
                    Log(LogLevelEnum.Debug, null, fmt, FormatGameObject(actor), eventId, scene, flags, param1, param2, param3, param4);
                }
                if (LogToFile == true)
                {
                    LogEventToFile(String.Format(fmt, FormatGameObject(actor), eventId, scene, flags, param1, param2, param3, param4));
                }
            }

            private void OnDirectorUpdate(uint param1, uint param2, uint param3, uint param4)
            {
                if (GoodToLog() == false)
                {
                    return;
                }
                string fmt = "InvokeDirectorUpdate: {0:X8} {1:X8} {2:X8} {3:X8}";
                if (LogToDalamudLog == true)
                {
                    Log(LogLevelEnum.Debug, null, fmt, param1, param2, param3, param4);
                }
                if (LogToFile == true)
                {
                    LogEventToFile(String.Format(fmt, param1, param2, param3, param4));
                }
            }

            private void OnActorControl(ushort category, uint sourceActorId, uint targetActorId, uint param1, uint param2, uint param3, uint param4)
            {
                if (LogActorControl == false || GoodToLog() == false)
                {
                    return;
                }
                string fmt = "InvokeActorControl: {0} {1} -> {2} {3:X8} {4:X8} {5:X8} {6:X8}";
                GameObject src = _state.GetActorById(sourceActorId);
                GameObject dst = _state.GetActorById(targetActorId);
                if (LogToDalamudLog == true)
                {
                    Log(LogLevelEnum.Debug, null, fmt, category, FormatGameObject(src), FormatGameObject(dst), param1, param2, param3, param4);
                }
                if (LogToFile == true)
                {
                    LogEventToFile(String.Format(fmt, category, FormatGameObject(src), FormatGameObject(dst), param1, param2, param3, param4));
                }
            }

            private void OnMapEffect(byte[] data)
            {
                if (GoodToLog() == false)
                {
                    return;
                }
                string fmt = "InvokeMapEffect: {0}";
                List<string> bytes = new List<string>();
                foreach (byte b in data)
                {
                    bytes.Add(b.ToString("X2"));
                }
                if (LogToDalamudLog == true)
                {
                    Log(LogLevelEnum.Debug, null, fmt, String.Join(" ", bytes));
                }
                if (LogToFile == true)
                {
                    LogEventToFile(String.Format(fmt, String.Join(" ", bytes)));
                }
            }

            private void OnTether(uint src, uint dest, uint tetherId)
            {
                if (GoodToLog() == false)
                {
                    return;
                }
                string fmt = "InvokeTether {0} -> {1}: {2}";
                GameObject srcgo = _state.GetActorById(src);
                GameObject dstgo = _state.GetActorById(dest);
                if (LogToDalamudLog == true)
                {
                    Log(LogLevelEnum.Debug, null, fmt, FormatGameObject(srcgo), FormatGameObject(dstgo), tetherId);
                }
                if (LogToFile == true)
                {
                    LogEventToFile(String.Format(fmt, FormatGameObject(srcgo), FormatGameObject(dstgo), tetherId));
                }
            }

            private void OnZoneChange(ushort newZone)
            {
                if (GoodToLog() == false)
                {
                    return;
                }
                string fmt = "InvokeZoneChange {0}";
                if (LogToDalamudLog == true)
                {
                    Log(LogLevelEnum.Debug, null, fmt, newZone);
                }
                if (LogToFile == true)
                {
                    LogEventToFile(String.Format(fmt, newZone));
                }
            }

            private void OnStatusChange(uint src, uint dest, uint statusId, bool gained, float duration, int stacks)
            {
                if (GoodToLog() == false)
                {
                    return;
                }
                string fmt = "InvokeStatusChange {0} -> {1}: {2} {3} for {4} s with {5} stacks";
                GameObject srcgo = _state.GetActorById(src);
                GameObject dstgo = _state.GetActorById(dest);
                if (LogToDalamudLog == true)
                {
                    Log(LogLevelEnum.Debug, null, fmt, FormatGameObject(srcgo), FormatGameObject(dstgo), gained == true ? "Gained" : "Lost", FormatStatusId(statusId), duration, stacks);
                }
                if (LogToFile == true)
                {
                    LogEventToFile(String.Format(fmt, FormatGameObject(srcgo), FormatGameObject(dstgo), gained == true ? "Gained" : "Lost", FormatStatusId(statusId), duration, stacks));
                }
            }

            private void OnHeadMarker(uint dest, uint markerId)
            {
                if (GoodToLog() == false)
                {
                    return;
                }
                string fmt = "InvokeHeadmarker {0}: {1}";
                GameObject dstgo = _state.GetActorById(dest);
                if (LogToDalamudLog == true)
                {
                    Log(LogLevelEnum.Debug, null, fmt, FormatGameObject(dstgo), markerId);
                }
                if (LogToFile == true)
                {
                    LogEventToFile(String.Format(fmt, FormatGameObject(dstgo), markerId));
                }
            }

            private void OnCombatChange(bool inCombat)
            {
                if (GoodToLog() == false)
                {
                    return;
                }
                string fmt = "InvokeCombatChange {0}";
                if (LogToDalamudLog == true)
                {
                    Log(LogLevelEnum.Debug, null, fmt, inCombat);
                }
                if (LogToFile == true)
                {
                    LogEventToFile(String.Format(fmt, inCombat));
                }
            }

            private void OnCastBegin(uint src, uint dest, ushort actionId, float castTime, float rotation)
            {
                if (GoodToLog() == false)
                {
                    return;
                }
                string fmt = "InvokeCastBegin {0} -> {1}: {2} in {3} s";
                GameObject srcgo = _state.GetActorById(src);
                GameObject dstgo = _state.GetActorById(dest);
                if (LogToDalamudLog == true)
                {
                    Log(LogLevelEnum.Debug, null, fmt, FormatGameObject(srcgo), FormatGameObject(dstgo), FormatActionId(actionId), castTime);
                }
                if (LogToFile == true)
                {
                    LogEventToFile(String.Format(fmt, FormatGameObject(srcgo), FormatGameObject(dstgo), FormatActionId(actionId), castTime));
                }
            }

            private void OnAction(uint src, uint dest, ushort actionId)
            {
                if (GoodToLog() == false)
                {
                    return;
                }
                string fmt = "InvokeAction {0} -> {1}: {2}";
                GameObject srcgo = _state.GetActorById(src);
                GameObject dstgo = _state.GetActorById(dest);
                if (LogToDalamudLog == true)
                {
                    Log(LogLevelEnum.Debug, null, fmt, FormatGameObject(srcgo), FormatGameObject(dstgo), FormatActionId(actionId));
                }
                if (LogToFile == true)
                {
                    LogEventToFile(String.Format(fmt, FormatGameObject(srcgo), FormatGameObject(dstgo), FormatActionId(actionId)));
                }
            }

            private void OnCombatantAdded(GameObject go)
            {
                if (GoodToLog() == false)
                {
                    return;
                }
                string fmt = "CombatantAdded {0}";
                if (LogToDalamudLog == true)
                {
                    Log(LogLevelEnum.Debug, null, fmt, FormatGameObject(go));
                }
                if (LogToFile == true)
                {
                    LogEventToFile(String.Format(fmt, FormatGameObject(go)));
                }
            }

            private void OnCombatantRemoved(ulong actorId, nint addr)
            {
                if (GoodToLog() == false)
                {
                    return;
                }
                string fmt = "CombatantRemoved {0:X8} at {1}";
                if (LogToDalamudLog == true)
                {
                    Log(LogLevelEnum.Debug, null, fmt, actorId, addr);
                }
                if (LogToFile == true)
                {
                    LogEventToFile(String.Format(fmt, actorId, addr));
                }
            }

        }

        public Debugger(State st) : base(st)
        {
        }

    }

}
