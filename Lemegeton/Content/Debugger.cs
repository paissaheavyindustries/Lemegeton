using Dalamud.Game.ClientState.Objects.Types;
using ImGuiNET;
using Lemegeton.Core;
using System;
using System.Numerics;
using System.Text;
using System.IO;
using static Lemegeton.Core.State;
using GameObject = Dalamud.Game.ClientState.Objects.Types.GameObject;
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
                    sb.AppendLine(String.Format("ID: {0} ({1})", go.ObjectId.ToString(), go.ObjectId.ToString("X8")));
                    sb.AppendLine(String.Format("Addr: {0} ({1})", go.Address.ToString(), go.Address.ToString("X8")));
                    if (go is Character)
                    {
                        Character ch = (Character)go;
                        sb.AppendLine(String.Format("NameId: {0} Name: {1}", ch.NameId, go.Name.ToString()));
                    }
                    else
                    {
                        sb.AppendLine(String.Format("Name: {0}", go.Name.ToString()));
                    }
                    if (go is BattleChara)
                    {
                        BattleChara bc = (BattleChara)go;
                        sb.AppendLine(String.Format("HP: {0}/{1}", bc.CurrentHp, bc.MaxHp));
                        sb.AppendLine(String.Format("Job: {0}", bc.ClassJob.Id));
                        sb.AppendLine(String.Format("Flags: {0}", _state.GetStatusFlags(bc)));
                        if (bc.CastActionId > 0)
                        {
                            sb.AppendLine(String.Format("Casting: {0} -> {1:X8}", bc.CastActionId, bc.CastTargetObjectId));
                        }
                    }                    
                    sb.AppendLine(String.Format("Position: {0},{1},{2}", go.Position.X, go.Position.Y, go.Position.Z));
                    if (go is Character)
                    {
                        Character ch = (Character)go;
                        unsafe
                        {
                            CharacterStruct* chs = (CharacterStruct*)ch.Address;
                            sb.AppendLine(String.Format("ObjectKind: {0} ModelId: {1}", go.ObjectKind, chs->ModelCharaId));
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
                                foreach (KeyValuePair<AutomarkerSigns.SignEnum, uint> kp2 in _state.SoftMarkers)
                                {
                                    if (kp2.Value == expectedActor.ObjectId)
                                    {
                                        currentSign = kp2.Key;
                                    }
                                }
                            }
                            else
                            { 
                                ret = _state.GetCurrentMarker(expectedActor.ObjectId, out currentSign);
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

            private bool _subbed = false;

            public EventLogger(State state) : base(state)
            {
                OnActiveChanged += EventLogger_OnActiveChanged;
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
                        CurrentLogFilename = realfn;
                    }
                    else
                    {
                        CurrentLogFilename = null;
                    }
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
                    _state.OnAction += _state_OnAction;
                    _state.OnCastBegin += _state_OnCastBegin;
                    _state.OnCombatChange += _state_OnCombatChange;
                    _state.OnHeadMarker += _state_OnHeadMarker;
                    _state.OnStatusChange += _state_OnStatusChange;
                    _state.OnZoneChange += _state_OnZoneChange;
                    _state.OnTether += _state_OnTether;
                    _state.OnMapEffect += _state_OnMapEffect;
                    _state.OnDirectorUpdate += _state_OnDirectorUpdate;
                    _state.OnCombatantAdded += _state_OnCombatantAdded;
                    _state.OnCombatantRemoved += _state_OnCombatantRemoved;
                    _state.OnEventPlay += _state_OnEventPlay;
                    _state.OnEventPlay64 += _state_OnEventPlay64;
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
                    _state.OnEventPlay64 -= _state_OnEventPlay64;
                    _state.OnEventPlay -= _state_OnEventPlay;
                    _state.OnCombatantRemoved -= _state_OnCombatantRemoved;
                    _state.OnCombatantAdded -= _state_OnCombatantAdded;
                    _state.OnDirectorUpdate -= _state_OnDirectorUpdate;
                    _state.OnMapEffect -= _state_OnMapEffect;
                    _state.OnTether -= _state_OnTether;
                    _state.OnAction -= _state_OnAction;
                    _state.OnCastBegin -= _state_OnCastBegin;
                    _state.OnCombatChange -= _state_OnCombatChange;
                    _state.OnHeadMarker -= _state_OnHeadMarker;
                    _state.OnStatusChange -= _state_OnStatusChange;
                    _state.OnZoneChange -= _state_OnZoneChange;
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
                return go == null ? "null" : String.Format("{0}({1} - {2}) at {3}", go.ObjectId.ToString("X"), go.Name.ToString(), go.ObjectKind, go.Address.ToString("X"));
            }

            private void LogEventToFile(string msg)
            {
                try
                {
                    lock (this)
                    {
                        File.AppendAllText(CurrentLogFilename, "[" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + "] " + msg + Environment.NewLine);
                    }
                }
                catch (Exception)
                {
                }
            }

            protected override bool ExecutionImplementation()
            {
                if (_subbed == false)
                {
                    SubscribeToEvents();
                }
                return true;
            }

            private void _state_OnEventPlay64()
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

            private void _state_OnEventPlay(uint actorId, uint eventId, ushort scene, uint flags, uint param1, ushort param2, byte param3, uint param4)
            {
                if (GoodToLog() == false)
                {
                    return;
                }
                GameObject actor = _state.GetActorById(actorId);
                string fmt = "InvokeEventPlay: {0:X8} {1} {2} {3} {4} {5} {6} {7}";
                if (LogToDalamudLog == true)
                {
                    Log(LogLevelEnum.Debug, null, fmt, actor, eventId, scene, flags, param1, param2, param3, param4);
                }
                if (LogToFile == true)
                {
                    LogEventToFile(String.Format(fmt, FormatGameObject(actor), eventId, scene, flags, param1, param2, param3, param4));
                }
            }

            private void _state_OnDirectorUpdate(uint param1, uint param2, uint param3, uint param4)
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

            private void _state_OnMapEffect(byte[] data)
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

            private void _state_OnTether(uint src, uint dest, uint tetherId)
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
                    Log(LogLevelEnum.Debug, null, fmt, srcgo, dstgo, tetherId);
                }
                if (LogToFile == true)
                {
                    LogEventToFile(String.Format(fmt, FormatGameObject(srcgo), FormatGameObject(dstgo), tetherId));
                }
            }

            private void _state_OnZoneChange(ushort newZone)
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
                if (LogToFile == true && CurrentLogFilename != null)
                {
                    LogEventToFile(String.Format(fmt, newZone));
                }
            }

            private void _state_OnStatusChange(uint src, uint dest, uint statusId, bool gained, float duration, int stacks)
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
                    Log(LogLevelEnum.Debug, null, fmt, srcgo, dstgo, gained == true ? "Gained" : "Lost", statusId, duration, stacks);
                }
                if (LogToFile == true)
                {
                    LogEventToFile(String.Format(fmt, FormatGameObject(srcgo), FormatGameObject(dstgo), gained == true ? "Gained" : "Lost", statusId, duration, stacks));
                }
            }

            private void _state_OnHeadMarker(uint dest, uint markerId)
            {
                if (GoodToLog() == false)
                {
                    return;
                }
                string fmt = "InvokeHeadmarker {0}: {1}";
                GameObject dstgo = _state.GetActorById(dest);
                if (LogToDalamudLog == true)
                {
                    Log(LogLevelEnum.Debug, null, fmt, dstgo, markerId);
                }
                if (LogToFile == true)
                {
                    LogEventToFile(String.Format(fmt, FormatGameObject(dstgo), markerId));
                }
            }

            private void _state_OnCombatChange(bool inCombat)
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

            private void _state_OnCastBegin(uint src, uint dest, ushort actionId, float castTime, float rotation)
            {
                if (GoodToLog() == false)
                {
                    return;
                }
                string fmt = "InvokeCastBegin {0} -> {1}: {2} in {3} s (rot {4})";
                GameObject srcgo = _state.GetActorById(src);
                GameObject dstgo = _state.GetActorById(dest);
                if (LogToDalamudLog == true)
                {
                    Log(LogLevelEnum.Debug, null, fmt, _state.GetActorById(src), _state.GetActorById(dest), actionId, castTime, rotation);
                }
                if (LogToFile == true)
                {
                    LogEventToFile(String.Format(fmt, FormatGameObject(srcgo), FormatGameObject(dstgo), actionId, castTime, rotation));
                }
            }

            private void _state_OnAction(uint src, uint dest, ushort actionId)
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
                    Log(LogLevelEnum.Debug, null, fmt, _state.GetActorById(src), _state.GetActorById(dest), actionId);
                }
                if (LogToFile == true)
                {
                    LogEventToFile(String.Format(fmt, FormatGameObject(srcgo), FormatGameObject(dstgo), actionId));
                }
            }

            private void _state_OnCombatantAdded(GameObject go)
            {
                if (GoodToLog() == false)
                {
                    return;
                }
                string fmt = "CombatantAdded {0}";
                if (LogToDalamudLog == true)
                {
                    Log(LogLevelEnum.Debug, null, fmt, go);
                }
                if (LogToFile == true)
                {
                    LogEventToFile(String.Format(fmt, FormatGameObject(go)));
                }
            }

            private void _state_OnCombatantRemoved(uint actorId, nint addr)
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
