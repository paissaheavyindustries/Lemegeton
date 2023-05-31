using System;
using Lemegeton.Core;

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
        }

        private ZoneEnum CurrentZone = ZoneEnum.None;
        private bool _sawFirstHeadMarker = false;
        private uint _firstHeadMarker = 0;

        private const int AbilityTwoMinds = 33156;

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
        }

        private void _state_OnCastBegin(uint src, uint dest, ushort actionId, float castTime, float rotation)
        {
            switch (actionId)
            {
                case AbilityTwoMinds:
                    if (CurrentZone == ZoneEnum.P9s && _levinballAM.Active == true)
                    {
                        _state.ClearAutoMarkers();
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
            if (Enum.TryParse<ZoneEnum>(newZone.ToString(), out ZoneEnum newZoneOk) == false)
            {
                newZoneOk = ZoneEnum.None;
            }            
            if (newZoneOk != ZoneEnum.None && CurrentZone == ZoneEnum.None)
            {
                Log(State.LogLevelEnum.Info, null, newZone + " Content available");
                CurrentZone = newZoneOk;
                switch (CurrentZone)
                {
                    case ZoneEnum.P9s:
                        _levinballAM = (LevinballAM)Items["LevinballAM"];
                        break;
                }
                _state.OnCombatChange += OnCombatChange;
            }
            else if (newZoneOk == ZoneEnum.None && CurrentZone != ZoneEnum.None)
            {
                Log(State.LogLevelEnum.Info, null, CurrentZone + " content unavailable");
                CurrentZone = ZoneEnum.None;
                _state.OnCombatChange -= OnCombatChange;
            }            
        }

    }

}
