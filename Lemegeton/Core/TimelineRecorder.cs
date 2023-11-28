using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CharacterStruct = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using static Lemegeton.Core.State;
using System.Xml.Linq;

namespace Lemegeton.Core
{

    internal class TimelineRecorder
    {

        private enum RecordingStateEnum
        {
            Idle,
            Recording,
            Suspended
        }

        [Flags]
        internal enum RecordedEventsEnum
        {
            AutoAttack = 0x0001,
            HostileAction = 0x0010,
            HostileSpawn = 0x0020,
            HostileStatusGain = 0x0040,
            HostileStatusLost = 0x0080,
            FriendlyAction = 0x0100,
            FriendlySpawn = 0x0200,
            FriendlyStatusGain = 0x0400,
            FriendlyStatusLost = 0x0800,
        }

        public RecordedEventsEnum RecordedEvents = RecordedEventsEnum.HostileAction | RecordedEventsEnum.HostileSpawn;

        public bool StartRecordingOnCombat { get; set; } = false;
        public bool ResumeRecordingOnTargettable { get; set; } = false;

        public bool SuspendRecordingAfterCombat { get; set; } = false;
        public bool SuspendRecordingOnUntargettable { get; set; } = false;

        public bool StopRecordingAfterCombat { get; set; } = false;
        public bool StopRecordingOnUntargettable { get; set; } = false;

        private bool _subbed = false;
        private DateTime _startTime = DateTime.Now;
        private State _state = null;

        internal delegate void RecordingFinishedDelegate(Timeline result);
        internal event RecordingFinishedDelegate RecordingFinished;

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
            }
        }

        private uint _lastActionId = 0;
        private DateTime _lastActionTs = DateTime.Now;
        private uint _lastSpawnNameId = 0;
        private DateTime _lastSpawnTs = DateTime.Now;

        private Timeline _tl = null;
        private Timeline.Encounter _enc = null;

        public TimelineRecorder(State state)
        {
            _state = state;
            recState = RecordingStateEnum.Idle;
        }

        private void StartRecording()
        {
            if (recState == RecordingStateEnum.Recording)
            {
                return;
            }
            if (recState == RecordingStateEnum.Suspended)
            {
                _startTime = DateTime.Now;
                _state.Log(State.LogLevelEnum.Debug, null, "Resuming timeline recording with next encounter");
                Timeline.Encounter enc = new Timeline.Encounter() { Id = _tl.Encounters.Count + 1 };
                enc.Triggers.Add(new Timeline.Encounter.Trigger() { Type = Timeline.Encounter.Trigger.EventTypeEnum.Start, EventType = Timeline.Encounter.Trigger.TriggerTypeEnum.OnCombatStart });
                enc.Triggers.Add(new Timeline.Encounter.Trigger() { Type = Timeline.Encounter.Trigger.EventTypeEnum.Stop, EventType = Timeline.Encounter.Trigger.TriggerTypeEnum.OnCombatEnd });
                enc.Triggers.Add(new Timeline.Encounter.Trigger() { Type = Timeline.Encounter.Trigger.EventTypeEnum.Select, EventType = Timeline.Encounter.Trigger.TriggerTypeEnum.Default });
                _tl.Encounters.Add(enc);
                _enc = enc;
            }
            else
            {
                _startTime = DateTime.Now;
                _state.Log(State.LogLevelEnum.Debug, null, "Starting new timeline recording");
                _tl = new Timeline();
                _tl.Territory = _state.cs.TerritoryType;
                _tl.Description = String.Format("Recorded by TimelineRecorder on {0}", _startTime);
                Timeline.Encounter enc = new Timeline.Encounter() { Id = 1 };
                enc.Triggers.Add(new Timeline.Encounter.Trigger() { Type = Timeline.Encounter.Trigger.EventTypeEnum.Start, EventType = Timeline.Encounter.Trigger.TriggerTypeEnum.OnCombatStart });
                enc.Triggers.Add(new Timeline.Encounter.Trigger() { Type = Timeline.Encounter.Trigger.EventTypeEnum.Stop, EventType = Timeline.Encounter.Trigger.TriggerTypeEnum.OnCombatEnd });
                enc.Triggers.Add(new Timeline.Encounter.Trigger() { Type = Timeline.Encounter.Trigger.EventTypeEnum.Select, EventType = Timeline.Encounter.Trigger.TriggerTypeEnum.Default });
                _tl.Encounters.Add(enc);
                _enc = enc;
            }
            recState = RecordingStateEnum.Recording;
        }

        private void SuspendRecording()
        {
            if (recState != RecordingStateEnum.Recording)
            {
                return;
            }
            _state.Log(State.LogLevelEnum.Debug, null, "Timeline recording suspended");
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
                _state.Log(LogLevelEnum.Debug, null, "Finishing recorded timeline");
                RecordingFinished?.Invoke(_tl);
            }
            else
            {
                _state.Log(State.LogLevelEnum.Debug, null, "No encounters or events captured, not finishing timeline");
            }
            recState = RecordingStateEnum.Idle;
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
                _state.Log(State.LogLevelEnum.Debug, null, "Subscribing to events for timeline recording");
                _state.OnAction += _state_OnAction;
                _state.OnCombatChange += _state_OnCombatChange;
                _state.OnZoneChange += _state_OnZoneChange;
                _state.OnTargettable += _state_OnTargettable;
                _state.OnUntargettable += _state_OnUntargettable;
                _state.OnCombatantAdded += _state_OnCombatantAdded;
                _state.OnStatusChange += _state_OnStatusChange;
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
                _state.Log(State.LogLevelEnum.Debug, null, "Unsubscribing from events for timeline recording");
                _state.OnAction -= _state_OnAction;
                _state.OnCombatChange -= _state_OnCombatChange;
                _state.OnZoneChange -= _state_OnZoneChange;
                _state.OnTargettable -= _state_OnTargettable;
                _state.OnUntargettable -= _state_OnUntargettable;
                _state.OnCombatantAdded -= _state_OnCombatantAdded;
                _state.OnStatusChange -= _state_OnStatusChange;
                _subbed = false;
            }
        }

        private bool IsHostile(uint id)
        {
            GameObject go = _state.GetActorById(id);
            return IsHostile(go);
        }

        private bool IsFriendly(uint id)
        {
            return !IsHostile(id);
        }

        private bool IsHostile(GameObject go)
        {
            if (go is BattleChara)
            {
                BattleChara bc = (BattleChara)go;
                return ((_state.GetStatusFlags(bc) & Dalamud.Game.ClientState.Objects.Enums.StatusFlags.Hostile) != 0);
            }
            return false;
        }

        private bool IsFriendly(GameObject go)
        {
            return !IsHostile(go);
        }

        private void _state_OnStatusChange(uint src, uint dest, uint statusId, bool gained, float duration, int stacks)
        {
            Timeline.Entry e = new Timeline.Entry();
            e.StartTime = (float)Math.Round((DateTime.Now - _startTime).TotalSeconds, 1);
            e.KeyValues.Add(statusId);            
            e.Type = gained == true ? Timeline.Entry.EntryTypeEnum.StatusGain : Timeline.Entry.EntryTypeEnum.StatusLoss;
            e.Description = "todo";
            _enc.Entries.Add(e);
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
                    if (chs->CharacterData.ModelCharaId == 0)
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
                    if ((RecordedEvents & RecordedEventsEnum.AutoAttack) == 0 && a.ActionCategory.Row == 1)
                    {
                        return;
                    }
                    string abname = _state.plug.GetActionName(actionId);
                    if (abname.Trim().Length > 0)
                    {
                        Timeline.Entry e = new Timeline.Entry();
                        e.StartTime = (float)Math.Round((DateTime.Now - _startTime).TotalSeconds, 1);
                        e.Type = Timeline.Entry.EntryTypeEnum.Ability;
                        e.KeyValues.Add(actionId);
                        e.Description = String.Format("{0} ({1}): {2}", bc.Name, bc.NameId, abname);
                        _enc.Entries.Add(e);
                    }
                }
            }
        }

        private void _state_OnCombatChange(bool inCombat)
        {
            if (inCombat == true && StartRecordingOnCombat == true && recState != RecordingStateEnum.Recording)
            {
                _state.Log(State.LogLevelEnum.Debug, null, "Combat started, starting timeline recording");
                StartRecording();
            }
            if (inCombat == false && recState == RecordingStateEnum.Recording)
            {
                if (SuspendRecordingAfterCombat == true)
                {
                    _state.Log(State.LogLevelEnum.Debug, null, "Combat ended, suspending timeline recording");
                    StopRecording();
                }
                else if (StopRecordingAfterCombat == true)
                {
                    _state.Log(State.LogLevelEnum.Debug, null, "Combat ended, stopping timeline recording");
                    StopRecording();
                }
            }
        }

        private void _state_OnZoneChange(ushort newZone)
        {
            if (recState != RecordingStateEnum.Idle)
            {
                _state.Log(State.LogLevelEnum.Debug, null, "Zone changed, stopping timeline recording");
                StopRecording();
            }
        }

        private void _state_OnTargettable()
        {
            if (recState == RecordingStateEnum.Suspended && ResumeRecordingOnTargettable == true)
            {
                _state.Log(State.LogLevelEnum.Debug, null, "Hostiles targettable, resuming timeline recording");
                StartRecording();
            }
        }

        private void _state_OnUntargettable()
        {
            if (recState == RecordingStateEnum.Recording && SuspendRecordingOnUntargettable == true)
            {
                _state.Log(State.LogLevelEnum.Debug, null, "Hostiles untargettable, suspending timeline recording");
                SuspendRecording();
            }
            else if (recState != RecordingStateEnum.Idle && StopRecordingOnUntargettable == true)
            {
                _state.Log(State.LogLevelEnum.Debug, null, "Hostiles untargettable, stopping timeline recording");
                StopRecording();
            }
        }

        private void _state_OnCombatantAdded(GameObject go)
        {
            if (recState != RecordingStateEnum.Recording || (RecordedEvents & RecordedEventsEnum.HostileSpawn) == 0)
            {
                return;
            }
            if (go is Character)
            {
                Character ch = (Character)go;
                unsafe
                {
                    CharacterStruct* chs = (CharacterStruct*)ch.Address;
                    if (chs->CharacterData.ModelCharaId == 0)
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
                    Timeline.Entry e = new Timeline.Entry();
                    e.StartTime = (float)Math.Round((DateTime.Now - _startTime).TotalSeconds, 1);
                    e.Type = Timeline.Entry.EntryTypeEnum.Spawn;
                    e.KeyValues.Add(bc.NameId);
                    e.Description = String.Format("{0} ({1})", bc.Name, bc.NameId);
                    _enc.Entries.Add(e);
                }
            }
        }

    }

}
