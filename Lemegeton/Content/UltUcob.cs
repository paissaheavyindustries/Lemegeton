using System;
using Lemegeton.Core;
using System.Collections.Generic;
using System.Linq;
using static Lemegeton.Content.UltOmegaProtocol;
using System.Collections;

namespace Lemegeton.Content
{

    internal class UltUcob : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

        private const int AbilityChainLighting = 0x26c7;
        private const int AbilityTenstrikeTrio = 9958;
        private const int AbilityGigaflare = 9942;
        private const int AbilityTwistingDive = 9906;
        private const int StatusThunderstruck = 466;
        private const int HeadmarkerEarthshaker = 28;
        private const int HeadmarkerHatch = 76;
        private const int HeadmarkerLunarDive = 77;
        private const int HeadmarkerCauterize = 14;
        private const int HeadmarkerMegaflareDive = 29;

        private bool ZoneOk = false;

        private ChainLightningAm _chainLightningAm;
        private TenstrikeAm _tenstrikeAm;
        private GrandOctetAm _grandOctetAm;

        private enum PhaseEnum
        {
            Start,
            Tenstrike,
            GrandOctet,
            Adds,
        }

        private PhaseEnum _CurrentPhase = PhaseEnum.Start;
        private PhaseEnum CurrentPhase
        {
            get
            {
                return _CurrentPhase;
            }
            set
            {
                if (_CurrentPhase != value)
                {
                    Log(State.LogLevelEnum.Debug, null, "Moving to phase {0}", value);
                    _CurrentPhase = value;
                }
            }
        }

        #region ChainLightningAm

        public class ChainLightningAm : Automarker
        {

            [AttributeOrderNumber(1000)]
            public AutomarkerSigns Signs { get; set; }

            [DebugOption]
            [AttributeOrderNumber(2500)]
            public AutomarkerTiming Timing { get; set; }

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public System.Action Test { get; set; }

            private List<uint> _lightnings = new List<uint>();
            private bool _fired = false;

            public ChainLightningAm(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Signs.SetRole("Lightning1", AutomarkerSigns.SignEnum.Ignore1, false);
                Signs.SetRole("Lightning2", AutomarkerSigns.SignEnum.Ignore2, false);
                Test = new System.Action(() => Signs.TestFunctionality(state, null, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _fired = false;
                _lightnings.Clear();
            }

            internal void FeedAction(uint actorId, uint actionId)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered action {0} on {1:X}", actionId, actorId);
                _lightnings.Add(actorId);
                if (_lightnings.Count < 2)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Ready for automarkers");
                Party pty = _state.GetPartyMembers();
                List<Party.PartyMember> _lightningsGo = new List<Party.PartyMember>(
                    from ix in pty.Members join jx in _lightnings on ix.ObjectId equals jx select ix
                );
                AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                ap.Assign(Signs.Roles["Lightning1"], _lightningsGo[0].GameObject);
                ap.Assign(Signs.Roles["Lightning2"], _lightningsGo[1].GameObject);
                _fired = true;
                _state.ExecuteAutomarkers(ap, Timing);
            }

            internal void FeedStatus(uint statusId, bool gained)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered status {0} {1}", statusId, gained);
                if (gained == false && _fired == true)
                {
                    Log(State.LogLevelEnum.Debug, null, "Clearing automarkers");
                    Reset();
                    _state.ClearAutoMarkers();
                }
            }

        }

        #endregion

        #region TenstrikeAm

        public class TenstrikeAm : Automarker
        {

            [AttributeOrderNumber(1000)]
            public AutomarkerSigns Signs1 { get; set; }

            [AttributeOrderNumber(1010)]
            public AutomarkerSigns Signs2 { get; set; }

            [AttributeOrderNumber(2000)]
            public AutomarkerPrio Prio { get; set; }

            [DebugOption]
            [AttributeOrderNumber(2500)]
            public AutomarkerTiming Timing { get; set; }

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public System.Action Test { get; set; }

            private List<uint> _hatches = new List<uint>();
            private List<uint> _shakers = new List<uint>();
            private bool _firstShaker = true;

            public TenstrikeAm(State state) : base(state)
            {
                Enabled = false;
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Prio = new AutomarkerPrio();
                Prio.Priority = AutomarkerPrio.PrioTypeEnum.Role;
                Signs1 = new AutomarkerSigns();
                Signs1.SetRole("Hatch1", AutomarkerSigns.SignEnum.Attack1, false);
                Signs1.SetRole("Hatch2", AutomarkerSigns.SignEnum.Attack2, false);
                Signs1.SetRole("Hatch3", AutomarkerSigns.SignEnum.Attack3, false);
                Signs1.SetRole("Nonhatch1", AutomarkerSigns.SignEnum.Bind1, false);
                Signs1.SetRole("Nonhatch2", AutomarkerSigns.SignEnum.Bind2, false);
                Signs1.SetRole("Nonhatch3", AutomarkerSigns.SignEnum.Bind3, false);
                Signs2 = new AutomarkerSigns();
                Signs2.SetRole("Shaker1", AutomarkerSigns.SignEnum.Attack1, false);
                Signs2.SetRole("Shaker2", AutomarkerSigns.SignEnum.Attack2, false);
                Signs2.SetRole("Shaker3", AutomarkerSigns.SignEnum.Attack3, false);
                Signs2.SetRole("Shaker4", AutomarkerSigns.SignEnum.Attack4, false);
                Test = new System.Action(() => Signs1.TestFunctionality(state, null, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _hatches.Clear();
                _shakers.Clear();
                _firstShaker = true;
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
                    case HeadmarkerEarthshaker:
                        if (_shakers.Count == 0 && _firstShaker == false)
                        {
                            _state.ClearAutoMarkers();
                        }
                        _firstShaker = false;
                        _shakers.Add(actorId);
                        if (_shakers.Count != 4)
                        {
                            return;
                        }
                        DecideShakers();
                        _shakers.Clear();
                        break;
                    case HeadmarkerHatch:
                        _hatches.Add(actorId);
                        if (_hatches.Count != 3)
                        {
                            return;
                        }
                        DecideHatches();
                        break;
                }
            }

            internal void DecideHatches()
            {
                Log(State.LogLevelEnum.Debug, null, "Ready for hatch automarkers");
                Party pty = _state.GetPartyMembers();
                List<Party.PartyMember> _hatchesGo = pty.GetByActorIds(_hatches);
                List<Party.PartyMember> _unmarkedGo = new List<Party.PartyMember>(
                    from ix in pty.Members where _hatchesGo.Contains(ix) == false select ix
                );
                Prio.SortByPriority(_hatchesGo);
                Prio.SortByPriority(_unmarkedGo);
                AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                ap.Assign(Signs1.Roles["Hatch1"], _hatchesGo[0].GameObject);
                ap.Assign(Signs1.Roles["Hatch2"], _hatchesGo[1].GameObject);
                ap.Assign(Signs1.Roles["Hatch3"], _hatchesGo[2].GameObject);
                ap.Assign(Signs1.Roles["Nonhatch1"], _unmarkedGo[0].GameObject);
                ap.Assign(Signs1.Roles["Nonhatch2"], _unmarkedGo[1].GameObject);
                ap.Assign(Signs1.Roles["Nonhatch3"], _unmarkedGo[2].GameObject);
                _state.ExecuteAutomarkers(ap, Timing);
            }

            internal void DecideShakers()
            {
                Log(State.LogLevelEnum.Debug, null, "Ready for shaker automarkers");
                Party pty = _state.GetPartyMembers();
                List<Party.PartyMember> _shakersGo = pty.GetByActorIds(_shakers);
                Prio.SortByPriority(_shakersGo);
                AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                ap.Assign(Signs2.Roles["Shaker1"], _shakersGo[0].GameObject);
                ap.Assign(Signs2.Roles["Shaker2"], _shakersGo[1].GameObject);
                ap.Assign(Signs2.Roles["Shaker3"], _shakersGo[2].GameObject);
                ap.Assign(Signs2.Roles["Shaker4"], _shakersGo[3].GameObject);
                _state.ExecuteAutomarkers(ap, Timing);
            }

        }

        #endregion

        #region GrandOctetAm

        public class GrandOctetAm : Automarker
        {

            [AttributeOrderNumber(1000)]
            public AutomarkerSigns Signs1 { get; set; }

            [DebugOption]
            [AttributeOrderNumber(2500)]
            public AutomarkerTiming Timing { get; set; }

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public System.Action Test { get; set; }

            private List<uint> _marked = new List<uint>();

            public GrandOctetAm(State state) : base(state)
            {
                Enabled = false;
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Signs1 = new AutomarkerSigns();
                Signs1.SetRole("TwistingDive", AutomarkerSigns.SignEnum.Triangle, false);
                Test = new System.Action(() => Signs1.TestFunctionality(state, null, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _marked.Clear();
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
                    case HeadmarkerLunarDive:
                    case HeadmarkerCauterize:
                    case HeadmarkerMegaflareDive:
                        _marked.Add(actorId);
                        if (_marked.Count != 7)
                        {
                            return;
                        }
                        DecideTwistingDive();
                        break;
                }
            }

            internal void DecideTwistingDive()
            {
                Log(State.LogLevelEnum.Debug, null, "Ready for Twisting Dive automarker");
                Party pty = _state.GetPartyMembers();
                List<Party.PartyMember> _markedPty = pty.GetByActorIds(_marked);
                List<Party.PartyMember> _unmarkedGo = new List<Party.PartyMember>(
                    from ix in pty.Members where _markedPty.Contains(ix) == false select ix
                );
                if (_unmarkedGo.Count != 1)
                {
                    // Someone probably died and/or got raised in the middle of Octet.
                    // Cannot properly determine Twisting Dive target.
                    return;
                }

                AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                ap.Assign(Signs1.Roles["TwistingDive"], _unmarkedGo[0].GameObject);
                _state.ExecuteAutomarkers(ap, Timing);
            }

        }

        #endregion

        public UltUcob(State st) : base(st)
        {
            st.OnZoneChange += OnZoneChange;
        }

        protected override bool ExecutionImplementation()
        {
            if (ZoneOk == true)
            {
                return base.ExecutionImplementation();
            }
            return false;
        }

        private void SubscribeToEvents()
        {
            _state.OnHeadMarker += OnHeadMarker;
            _state.OnAction += _state_OnAction;
            _state.OnStatusChange += _state_OnStatusChange;
        }

        private void OnHeadMarker(uint dest, uint markerId)
        {
            if (CurrentPhase == PhaseEnum.Tenstrike)
            {
                _tenstrikeAm.FeedHeadmarker(dest, markerId);
            }
            if (CurrentPhase == PhaseEnum.GrandOctet)
            {
                _grandOctetAm.FeedHeadmarker(dest, markerId);
            }
        }

        private void _state_OnStatusChange(uint src, uint dest, uint statusId, bool gained, float duration, int stacks)
        {
            if (statusId == StatusThunderstruck)
            {
                _chainLightningAm.FeedStatus(statusId, gained);
            }
        }

        private void _state_OnAction(uint src, uint dest, ushort actionId)
        {
            if (actionId == AbilityChainLighting)
            {
                _chainLightningAm.FeedAction(dest, actionId);
            }
            if (actionId == AbilityTenstrikeTrio)
            {
                CurrentPhase = PhaseEnum.Tenstrike;
            }
            if (actionId == AbilityGigaflare)
            {
                if (CurrentPhase == PhaseEnum.Tenstrike)
                {
                    if (_tenstrikeAm.Active == true)
                    {
                        _state.ClearAutoMarkers();
                    }
                    CurrentPhase = PhaseEnum.GrandOctet;
                }
            }
            if (actionId == AbilityTwistingDive)
            {
                if (CurrentPhase == PhaseEnum.GrandOctet)
                {
                    if (_grandOctetAm.Active == true)
                    {
                        _state.ClearAutoMarkers();
                    }
                    CurrentPhase = PhaseEnum.Adds;
                }
            }
        }

        private void UnsubscribeFromEvents()
        {
            _state.OnHeadMarker -= OnHeadMarker;
            _state.OnStatusChange -= _state_OnStatusChange;
            _state.OnAction -= _state_OnAction;
        }

        private void OnCombatChange(bool inCombat)
        {
            Reset();
            if (inCombat == true)
            {
                CurrentPhase = PhaseEnum.Start;
                SubscribeToEvents();
            }
            else
            {
                UnsubscribeFromEvents();
            }
        }

        private void OnZoneChange(ushort newZone)
        {
            bool newZoneOk = (newZone == 733);
            if (newZoneOk == true && ZoneOk == false)
            {
                Log(State.LogLevelEnum.Info, null, "Content available");
                _chainLightningAm = (ChainLightningAm)Items["ChainLightningAm"];
                _tenstrikeAm = (TenstrikeAm)Items["TenstrikeAm"];
                _grandOctetAm = (GrandOctetAm)Items["GrandOctetAm"];
                _state.OnCombatChange += OnCombatChange;
            }
            else if (newZoneOk == false && ZoneOk == true)
            {
                Log(State.LogLevelEnum.Info, null, "Content unavailable");
                _state.OnCombatChange -= OnCombatChange;
            }
            ZoneOk = newZoneOk;
        }

    }

}
