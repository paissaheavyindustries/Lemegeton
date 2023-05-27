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
using static Lemegeton.Core.Timeline.Encounter;
using static Lemegeton.Core.Timeline;
using Lumina.Excel.GeneratedSheets;

namespace Lemegeton.Content
{

    public class Timelines : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

        public class TimelineRecorder : Core.ContentItem
        {

            private enum RecordingStateEnum
            {
                Idle,
                Recording,
                Suspended
            }

            public override FeaturesEnum Features => FeaturesEnum.None;

            [AttributeOrderNumber(1000)]
            public bool StartRecordingOnCombat { get; set; } = false;
            [AttributeOrderNumber(1001)]
            public bool ResumeRecordingOnTargettable { get; set; } = false;

            [AttributeOrderNumber(1010)]
            public bool SuspendRecordingAfterCombat { get; set; } = false;
            [AttributeOrderNumber(1011)]
            public bool SuspendRecordingOnUntargettable { get; set; } = false;

            [AttributeOrderNumber(1020)]
            public bool StopRecordingAfterCombat { get; set; } = false;
            [AttributeOrderNumber(1021)]
            public bool StopRecordingOnUntargettable { get; set; } = false;

            [AttributeOrderNumber(1500)]
            public System.Action StartRecordingAction { get; set; }
            [AttributeOrderNumber(1501)]
            public System.Action SuspendRecordingAction { get; set; }
            [AttributeOrderNumber(1502)]
            public System.Action StopRecordingAction { get; set; }

            [AttributeOrderNumber(2000)]
            public string TargetFolder { get; set; }

            [AttributeOrderNumber(2500)]
            public bool IgnoreAutoAttacks { get; set; } = true;
            [AttributeOrderNumber(2501)]
            public bool IgnoreSpawns { get; set; } = false;

            [AttributeOrderNumber(3000)]
            public string CurrentStatus { get; private set; } = "";
            [AttributeOrderNumber(3001)]
            public string CurrentTargetFile { get; private set; } = "";

            private bool _subbed = false;
            private DateTime _startTime = DateTime.Now;
            
            private RecordingStateEnum _recState = RecordingStateEnum.Idle;
            private RecordingStateEnum recState
            {
                get
                {
                    return _recState;
                }
                set
                {
                    _recState = value;
                    CurrentStatus = I18n.Translate("TimelineRecorder/" + _recState);
                }
            }

            private uint _lastActionId = 0;
            private DateTime _lastActionTs = DateTime.Now;
            private uint _lastSpawnNameId = 0;
            private DateTime _lastSpawnTs = DateTime.Now;

            private Timeline _tl = null;
            private Encounter _enc = null;

            public TimelineRecorder(State state) : base(state)
            {
                Enabled = false;
                TargetFolder = Path.GetTempPath();
                OnActiveChanged += EventLogger_OnActiveChanged;
                recState = RecordingStateEnum.Idle;
                StartRecordingAction = new System.Action(() => StartRecording());
                SuspendRecordingAction = new System.Action(() => SuspendRecording());
                StopRecordingAction = new System.Action(() => StopRecording());
            }

            private void StartRecording()
            {
                if (Active == false || recState == RecordingStateEnum.Recording)
                {
                    return;
                }
                if (recState == RecordingStateEnum.Suspended)
                {
                    _startTime = DateTime.Now;
                    _state.Log(LogLevelEnum.Debug, null, "Resuming timeline recording for {0} with next encounter", CurrentTargetFile);
                    Encounter enc = new Encounter() { Id = _tl.Encounters.Count + 1 };
                    enc.Triggers.Add(new Trigger() { Type = Trigger.EventTypeEnum.Start, EventType = Trigger.TriggerTypeEnum.OnCombatStart });
                    enc.Triggers.Add(new Trigger() { Type = Trigger.EventTypeEnum.Stop, EventType = Trigger.TriggerTypeEnum.OnCombatEnd });
                    enc.Triggers.Add(new Trigger() { Type = Trigger.EventTypeEnum.Select, EventType = Trigger.TriggerTypeEnum.Default });
                    _tl.Encounters.Add(enc);
                    _enc = enc;
                }
                else
                {
                    _startTime = DateTime.Now;
                    string paff = TargetFolder.Trim();
                    string filename = String.Format("Lemegeton_{0}_{1}.timeline.xml", _state.cs.TerritoryType, _startTime.ToString("yyyyMMdd_HHmmss"));
                    CurrentTargetFile = Path.Combine(paff, filename);
                    _state.Log(LogLevelEnum.Debug, null, "Starting new timeline recording for {0}", CurrentTargetFile);
                    _tl = new Timeline();
                    _tl.Territory = _state.cs.TerritoryType;
                    _tl.Description = String.Format("Recorded by TimelineRecorder on {0}", _startTime);
                    Encounter enc = new Encounter() { Id = 1 };
                    enc.Triggers.Add(new Trigger() { Type = Trigger.EventTypeEnum.Start, EventType = Trigger.TriggerTypeEnum.OnCombatStart });
                    enc.Triggers.Add(new Trigger() { Type = Trigger.EventTypeEnum.Stop, EventType = Trigger.TriggerTypeEnum.OnCombatEnd });
                    enc.Triggers.Add(new Trigger() { Type = Trigger.EventTypeEnum.Select, EventType = Trigger.TriggerTypeEnum.Default });
                    _tl.Encounters.Add(enc);
                    _enc = enc;
                }
                recState = RecordingStateEnum.Recording;
            }

            private void SuspendRecording()
            {
                if (Active == false || recState != RecordingStateEnum.Recording)
                {
                    return;
                }
                _state.Log(LogLevelEnum.Debug, null, "Timeline recording suspended");
                recState = RecordingStateEnum.Suspended;
            }

            private void StopRecording()
            {
                if (recState == RecordingStateEnum.Idle)
                {
                    return;
                }
                _lastActionId = 0;
                _lastActionTs = DateTime.Now;
                _lastSpawnNameId = 0;
                _lastSpawnTs = DateTime.Now;
                if (_tl != null && (_tl.Encounters.Count > 1 || _enc.Entries.Count > 0))
                {
                    _state.Log(LogLevelEnum.Debug, null, "Writing recorded timeline to {0}", CurrentTargetFile);
                    string data = XmlSerializer<Timeline>.Serialize(_tl);
                    File.WriteAllText(CurrentTargetFile, data);                    
                }
                else
                {
                    _state.Log(LogLevelEnum.Debug, null, "No encounters or events captured, won't write timeline to {0}", CurrentTargetFile);
                }
                recState = RecordingStateEnum.Idle;
            }

            private void EventLogger_OnActiveChanged(bool newState)
            {
                if (newState == true)
                {
                    SubscribeToEvents();
                }
                else
                {
                    StopRecording();
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
                    _state.OnCombatChange += _state_OnCombatChange;
                    _state.OnZoneChange += _state_OnZoneChange;
                    _state.OnTargettable += _state_OnTargettable;
                    _state.OnUntargettable += _state_OnUntargettable;
                    _state.OnCombatantAdded += _state_OnCombatantAdded;
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
                    _state.OnAction -= _state_OnAction;
                    _state.OnCombatChange -= _state_OnCombatChange;
                    _state.OnZoneChange -= _state_OnZoneChange;
                    _state.OnTargettable -= _state_OnTargettable;
                    _state.OnUntargettable -= _state_OnUntargettable;
                    _state.OnCombatantAdded -= _state_OnCombatantAdded;
                    _subbed = false;
                }
            }

            private void _state_OnAction(uint src, uint dest, ushort actionId)
            {
                if (recState != RecordingStateEnum.Recording)
                {
                    return;
                }
                if (_lastActionId == actionId && (DateTime.Now - _lastActionTs).TotalMilliseconds < 50.0)
                {
                    return;
                }                
                _lastActionId = actionId;
                _lastActionTs = DateTime.Now;
                GameObject go = _state.GetActorById(src);
                if (go is Character)
                {
                    Character ch = (Character)go;
                    unsafe
                    {
                        CharacterStruct* chs = (CharacterStruct*)ch.Address;
                        if (chs->ModelCharaId == 0)
                        {
                            return;
                        }
                    }
                }
                if (go is BattleChara)
                {
                    BattleChara bc = (BattleChara)go;
                    if ((_state.GetStatusFlags(bc) & Dalamud.Game.ClientState.Objects.Enums.StatusFlags.Hostile) != 0)
                    {
                        Lumina.Excel.GeneratedSheets.Action a = _state.dm.Excel.GetSheet<Lumina.Excel.GeneratedSheets.Action>().GetRow(actionId);
                        if (IgnoreAutoAttacks == true && a.ActionCategory.Row == 1)
                        {
                            return;
                        }
                        string abname = a.Name;
                        if (abname.Trim().Length > 0)
                        {
                            Entry e = new Entry();
                            e.StartTime = (float)Math.Round((DateTime.Now - _startTime).TotalSeconds, 1);
                            e.Type = Entry.EntryTypeEnum.Ability;
                            e.Keys.Add(actionId);
                            e.Description = String.Format("{0} ({1}): {2}", bc.Name, bc.NameId, a.Name);
                            _enc.Entries.Add(e);
                        }
                    }
                }
            }

            private void _state_OnCombatChange(bool inCombat)
            {
                if (inCombat == true && StartRecordingOnCombat == true && recState != RecordingStateEnum.Recording)
                {
                    Log(LogLevelEnum.Debug, null, "Combat started, starting recording");
                    StartRecording();
                }
                if (inCombat == false && recState == RecordingStateEnum.Recording)
                {
                    if (SuspendRecordingAfterCombat == true)
                    {
                        Log(LogLevelEnum.Debug, null, "Combat ended, suspending recording");
                        StopRecording();
                    }
                    else if (StopRecordingAfterCombat == true)
                    {
                        Log(LogLevelEnum.Debug, null, "Combat ended, stopping recording");
                        StopRecording();
                    }
                }
            }

            private void _state_OnZoneChange(ushort newZone)            
            {
                if (recState != RecordingStateEnum.Idle)
                {
                    Log(LogLevelEnum.Debug, null, "Zone changed, stopping recording");
                    StopRecording();
                }
            }

            private void _state_OnTargettable()
            {
                if (recState == RecordingStateEnum.Suspended && ResumeRecordingOnTargettable == true)
                {
                    Log(LogLevelEnum.Debug, null, "Hostiles targettable, resuming recording");
                    StartRecording();
                }
            }

            private void _state_OnUntargettable()
            {
                if (recState == RecordingStateEnum.Recording && SuspendRecordingOnUntargettable == true)
                {
                    Log(LogLevelEnum.Debug, null, "Hostiles untargettable, suspending recording");
                    SuspendRecording();
                }
                else if (recState != RecordingStateEnum.Idle && StopRecordingOnUntargettable == true)
                {
                    Log(LogLevelEnum.Debug, null, "Hostiles untargettable, stopping recording");
                    StopRecording();
                }
            }

            private void _state_OnCombatantAdded(GameObject go)
            {
                if (recState != RecordingStateEnum.Recording || IgnoreSpawns == true)
                {
                    return;
                }
                if (go is Character)
                {
                    Character ch = (Character)go;
                    unsafe
                    {
                        CharacterStruct* chs = (CharacterStruct*)ch.Address;
                        if (chs->ModelCharaId == 0)
                        {
                            return;
                        }
                    }
                }
                if (go is BattleChara)
                {
                    BattleChara bc = (BattleChara)go;
                    if (_lastSpawnNameId == bc.NameId && (DateTime.Now - _lastSpawnTs).TotalMilliseconds < 50.0)
                    {
                        return;
                    }
                    _lastSpawnNameId = bc.NameId;
                    _lastSpawnTs = DateTime.Now;
                    if ((_state.GetStatusFlags(bc) & Dalamud.Game.ClientState.Objects.Enums.StatusFlags.Hostile) != 0 && bc.MaxHp > 0)
                    {
                        Entry e = new Entry();
                        e.StartTime = (float)Math.Round((DateTime.Now - _startTime).TotalSeconds, 1);
                        e.Type = Entry.EntryTypeEnum.Spawn;
                        e.Keys.Add(bc.NameId);                        
                        e.Description = String.Format("{0} ({1})", bc.Name, bc.NameId);
                        _enc.Entries.Add(e);
                    }
                }
            }

        }

        public Timelines(State st) : base(st)
        {
        }

    }

}
