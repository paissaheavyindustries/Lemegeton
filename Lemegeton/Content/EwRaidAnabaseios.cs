using System;
using System.Collections.Generic;
using System.Linq;
using Lemegeton.Core;
using Lumina.Excel.GeneratedSheets;

namespace Lemegeton.Content
{

    internal class EwRaidAnabaseios : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

        private enum ZoneEnum
        {
            None = 0,
            P9s = 1148,
            P10s = 1150,
            P11s = 1152,
            P12s = 1154,
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

        private bool _sawFirstHeadMarker = false;
        private uint _firstHeadMarker = 0;

        private const int AbilityTwoMinds1 = 33156;
        private const int AbilityTwoMinds2 = 33157;
        private const int AbilityPalladianGrasp1 = 33562;
        private const int AbilityPalladianGrasp2 = 33563;
        private const int AbilityPalladianGrasp3 = 33564;
        private const int AbilityPalladianGrasp4 = 33565;

        private const int StatusUnstableSystem = 3593;
        private const int StatusUmbralTilt = 3576;
        private const int StatusAstralTilt = 3577;

        private const int HeadmarkerLC1 = 79;
        private const int HeadmarkerLC2 = HeadmarkerLC1 + 1;
        private const int HeadmarkerLC3 = HeadmarkerLC2 + 1;
        private const int HeadmarkerLC4 = HeadmarkerLC3 + 1;
        private const int HeadmarkerLC5 = HeadmarkerLC4 + 1;
        private const int HeadmarkerLC6 = HeadmarkerLC5 + 1;
        private const int HeadmarkerLC7 = HeadmarkerLC6 + 1;
        private const int HeadmarkerLC8 = HeadmarkerLC7 + 1;
        private const int HeadmarkerBlue = 330;

        private LevinballAM _levinballAM;
        private PangenesisAM _pankoAM;

        #region LevinballAM

        public class LevinballAM : Automarker
        {

            [AttributeOrderNumber(1000)]
            public AutomarkerSigns Signs { get; set; }

            [DebugOption]
            [AttributeOrderNumber(2500)]
            public AutomarkerTiming Timing { get; set; }

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public System.Action Test { get; set; }

            private uint _limitCut2 = 0;
            private uint _limitCut4 = 0;
            private uint _limitCut6 = 0;
            private uint _limitCut8 = 0;
            private bool _fired = false;

            public LevinballAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Signs.SetRole("LimitCut2", AutomarkerSigns.SignEnum.Attack2, false);
                Signs.SetRole("LimitCut4", AutomarkerSigns.SignEnum.Attack4, false);
                Signs.SetRole("LimitCut6", AutomarkerSigns.SignEnum.Attack6, false);
                Signs.SetRole("LimitCut8", AutomarkerSigns.SignEnum.Attack8, false);
                Signs.SetRole("BlueMarker", AutomarkerSigns.SignEnum.Circle, false);
                Test = new System.Action(() => Signs.TestFunctionality(state, null, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _fired = false;
                _limitCut2 = 0;
                _limitCut4 = 0;
                _limitCut6 = 0;
                _limitCut8 = 0;
            }

            internal void FeedHeadmarker(uint actorId, uint headMarkerId)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered headMarkerId {0} on {1:X}", headMarkerId, actorId);
                switch (headMarkerId)
                {
                    case HeadmarkerLC2:
                        _limitCut2 = actorId;
                        break;
                    case HeadmarkerLC4:
                        _limitCut4 = actorId;
                        break;
                    case HeadmarkerLC6:
                        _limitCut6 = actorId;
                        break;
                    case HeadmarkerLC8:
                        _limitCut8 = actorId;
                        break;
                    case HeadmarkerBlue:
                        AssignBlueMarker(actorId);
                        return;
                }
                if (_limitCut2 == 0 || _limitCut4 == 0 || _limitCut4 == 6 || _limitCut4 == 8 || _fired == true)
                {
                    return;
                }
                ReadyForLCMarkers();
            }

            private void AssignBlueMarker(uint actorId)
            {
                Log(State.LogLevelEnum.Debug, null, "Assigning blue marker to {0:X}", actorId);
                Party pty = _state.GetPartyMembers();
                Party.PartyMember _bmGo = pty.GetByActorId(actorId);
                AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                ap.Assign(Signs.Roles["BlueMarker"], _bmGo.GameObject);
                _state.ExecuteAutomarkers(ap, Timing);
            }

            private void ReadyForLCMarkers()
            {
                Log(State.LogLevelEnum.Debug, null, "Ready for automarkers");
                Party pty = _state.GetPartyMembers();
                Party.PartyMember _lc2Go = pty.GetByActorId(_limitCut2);
                Party.PartyMember _lc4Go = pty.GetByActorId(_limitCut4);
                Party.PartyMember _lc6Go = pty.GetByActorId(_limitCut6);
                Party.PartyMember _lc8Go = pty.GetByActorId(_limitCut8);
                AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                ap.Assign(Signs.Roles["LimitCut2"], _lc2Go.GameObject);
                ap.Assign(Signs.Roles["LimitCut4"], _lc4Go.GameObject);
                ap.Assign(Signs.Roles["LimitCut6"], _lc6Go.GameObject);
                ap.Assign(Signs.Roles["LimitCut8"], _lc8Go.GameObject);
                _state.ExecuteAutomarkers(ap, Timing);
                _fired = true;
            }

        }

        #endregion

        #region PangenesisAM

        public class PangenesisAM : Automarker
        {

            [AttributeOrderNumber(1000)]
            public AutomarkerSigns Signs { get; set; }

            [AttributeOrderNumber(2000)]
            public AutomarkerPrio Prio { get; set; }

            [DebugOption]
            [AttributeOrderNumber(2500)]
            public AutomarkerTiming Timing { get; set; }

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public System.Action Test { get; set; }

            private uint _longLight = 0;
            private uint _shortLight = 0;
            private uint _longDark = 0;
            private uint _shortDark = 0;
            private List<uint> _ones = new List<uint>();
            internal bool _fired = false;

            public PangenesisAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Prio = new AutomarkerPrio() { Priority = AutomarkerPrio.PrioTypeEnum.CongaX };
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Signs.SetRole("LongLight", AutomarkerSigns.SignEnum.None, false);
                Signs.SetRole("ShortLight", AutomarkerSigns.SignEnum.None, false);
                Signs.SetRole("LongDark", AutomarkerSigns.SignEnum.None, false);
                Signs.SetRole("ShortDark", AutomarkerSigns.SignEnum.None, false);
                Signs.SetRole("One1", AutomarkerSigns.SignEnum.Bind1, false);
                Signs.SetRole("One2", AutomarkerSigns.SignEnum.Bind2, false);
                Signs.SetRole("Nothing1", AutomarkerSigns.SignEnum.Ignore1, false);
                Signs.SetRole("Nothing2", AutomarkerSigns.SignEnum.Ignore2, false);                
                Test = new System.Action(() => Signs.TestFunctionality(state, Prio, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _fired = false;
                _longLight = 0;
                _shortLight = 0;
                _longDark = 0;
                _shortDark = 0;
                _ones.Clear();
            }

            internal void FeedStatus(uint statusId, uint actorId, float duration, int stacks)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1:X} for {2} with {3} stacks", statusId, actorId, duration, stacks);
                switch (statusId)
                {
                    case StatusUmbralTilt:
                        if (duration >= 15 && duration <= 17)
                        {
                            _shortLight = actorId;
                        }
                        if (duration >= 19 && duration <= 21)
                        {
                            _longLight = actorId;
                        }
                        break;
                    case StatusAstralTilt:
                        if (duration >= 15 && duration <= 17)
                        {
                            _shortDark = actorId;
                        }
                        if (duration >= 19 && duration <= 21)
                        {
                            _longDark = actorId;
                        }
                        break;
                    case StatusUnstableSystem:
                        if (stacks == 1)
                        {
                            _ones.Add(actorId);
                        }
                        break;
                }
                if (_longLight == 0 || _shortLight == 0 || _longDark == 6 || _shortDark == 8 || _ones.Count < 2 || _fired == true)
                {
                    return;
                }
                ReadyForMarkers();
            }

            internal void ReadyForMarkers()
            {
                Log(State.LogLevelEnum.Debug, null, "Ready for automarkers");
                Party pty = _state.GetPartyMembers();
                Party.PartyMember _longLightGo = pty.GetByActorId(_longLight);
                Party.PartyMember _shortLightGo = pty.GetByActorId(_shortLight);
                Party.PartyMember _longDarkGo = pty.GetByActorId(_longDark);
                Party.PartyMember _shortDarkGo = pty.GetByActorId(_shortDark);
                List<Party.PartyMember> _onesGo = pty.GetByActorIds(_ones);
                List<Party.PartyMember> _nothingsGo = new List<Party.PartyMember>(
                    from ix in pty.Members where _onesGo.Contains(ix) == false && ix != _longDarkGo && ix != _longLightGo
                    && ix != _shortDarkGo && ix != _shortLightGo select ix
                );
                Prio.SortByPriority(_onesGo);
                Prio.SortByPriority(_nothingsGo);
                AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                ap.Assign(Signs.Roles["LongLight"], _longLightGo.GameObject);
                ap.Assign(Signs.Roles["ShortLight"], _shortLightGo.GameObject);
                ap.Assign(Signs.Roles["LongDark"], _longDarkGo.GameObject);
                ap.Assign(Signs.Roles["ShortDark"], _shortDarkGo.GameObject);
                ap.Assign(Signs.Roles["One1"], _onesGo[0].GameObject);
                ap.Assign(Signs.Roles["One2"], _onesGo[1].GameObject);
                ap.Assign(Signs.Roles["Nothing1"], _nothingsGo[0].GameObject);
                ap.Assign(Signs.Roles["Nothing2"], _nothingsGo[1].GameObject);
                _state.ExecuteAutomarkers(ap, Timing);
                _fired = true;
            }

        }

        #endregion

        public EwRaidAnabaseios(State st) : base(st)
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
            _state.OnHeadMarker += _state_OnHeadMarker;
            _state.OnCastBegin += _state_OnCastBegin;
            _state.OnStatusChange += _state_OnStatusChange;
        }

        private void _state_OnStatusChange(uint src, uint dest, uint statusId, bool gained, float duration, int stacks)
        {
            switch (statusId)
            {
                case StatusAstralTilt:
                case StatusUmbralTilt:
                case StatusUnstableSystem:
                    if (CurrentZone == ZoneEnum.P12s && _pankoAM.Active == true && gained == true)
                    {
                        _pankoAM.FeedStatus(statusId, dest, duration, stacks);
                    }
                    break;
            }
        }

        private void _state_OnCastBegin(uint src, uint dest, ushort actionId, float castTime, float rotation)
        {
            switch (actionId)
            {
                case AbilityTwoMinds1:
                case AbilityTwoMinds2:
                    if (CurrentZone == ZoneEnum.P9s && _levinballAM.Active == true)
                    {
                        _state.ClearAutoMarkers();
                    }
                    break;
                case AbilityPalladianGrasp1:
                case AbilityPalladianGrasp2:
                case AbilityPalladianGrasp3:
                case AbilityPalladianGrasp4:
                    if (CurrentZone == ZoneEnum.P12s && _pankoAM.Active == true && _pankoAM._fired == true)
                    {
                        _state.ClearAutoMarkers();
                        _pankoAM._fired = false;
                    }
                    break;
            }
        }

        private void _state_OnHeadMarker(uint dest, uint markerId)
        {
            if (_sawFirstHeadMarker == false)
            {
                _sawFirstHeadMarker = true;
                switch (CurrentZone)
                {
                    case ZoneEnum.P9s:
                        _firstHeadMarker = markerId - 468;
                        break;
                }                
            }
            uint realMarkerId = markerId - _firstHeadMarker;
            switch (CurrentZone)
            {
                case ZoneEnum.P9s:
                    _levinballAM.FeedHeadmarker(dest, realMarkerId);
                    break;
            }
        }

        private void UnsubscribeFromEvents()
        {
            _state.OnStatusChange -= _state_OnStatusChange;
            _state.OnCastBegin -= _state_OnCastBegin;
            _state.OnHeadMarker -= _state_OnHeadMarker;
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
            _sawFirstHeadMarker = false;
            _firstHeadMarker = 0;
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
                    case ZoneEnum.P9s:
                        _levinballAM = (LevinballAM)Items["LevinballAM"];
                        break;
                    case ZoneEnum.P12s:
                        _pankoAM = (PangenesisAM)Items["PangenesisAM"];
                        break;
                }
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
