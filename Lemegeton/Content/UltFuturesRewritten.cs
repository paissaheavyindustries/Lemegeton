using Lemegeton.Core;
using System.Collections.Generic;
using System.Linq;
using static Lemegeton.Core.State;

namespace Lemegeton.Content
{

    internal class UltFuturesRewritten : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

        private bool ZoneOk = false;
        private bool _subbed = false;

        private const int AbilityFallOfFaith = 40140;
        private const int AbilityQuadrupleSlap = 40191;

        private const int TetherFire = 249;
        private const int TetherLightning = 287;

        private FallOfFaithAM _fallFaithAm;

        private enum PhaseEnum
        {
            P1_Start,
            P1_Faith,
            P2_Shiva,
        }

        private PhaseEnum _CurrentPhase = PhaseEnum.P1_Start;
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

        #region FallOfFaithAM

        public class FallOfFaithAM : Automarker
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

            private bool _fired = false;
            private List<ulong> _tethers = new List<ulong>();

            public FallOfFaithAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Prio = new AutomarkerPrio();
                Prio.Priority = AutomarkerPrio.PrioTypeEnum.CongaX;
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Signs.SetRole("Tether1", AutomarkerSigns.SignEnum.Attack1, false);
                Signs.SetRole("Tether2", AutomarkerSigns.SignEnum.Attack2, false);
                Signs.SetRole("Tether3", AutomarkerSigns.SignEnum.Attack3, false);
                Signs.SetRole("Tether4", AutomarkerSigns.SignEnum.Attack4, false);
                Signs.SetRole("Overflow1", AutomarkerSigns.SignEnum.Bind1, false);
                Signs.SetRole("Overflow2", AutomarkerSigns.SignEnum.Bind2, false);
                Signs.SetRole("Overflow3", AutomarkerSigns.SignEnum.Ignore1, false);
                Signs.SetRole("Overflow4", AutomarkerSigns.SignEnum.Ignore2, false);
                Test = new System.Action(() => Signs.TestFunctionality(state, Prio, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _fired = false;
                _tethers.Clear();
            }

            internal void FeedTether(uint src, uint dest, uint tetherId)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered tether {0} from {1:X} on {2:X}", tetherId, src, dest);
                _tethers.Add((ulong)dest);
                AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                int tethernum = _tethers.Count;
                ap.Assign(Signs.Roles["Tether" + tethernum], _state.GetActorById(dest));
                if (tethernum == 4)
                {
                    Party pty = _state.GetPartyMembers();
                    List<Party.PartyMember> _overflowGo = new List<Party.PartyMember>(
                        from ix in pty.Members where _tethers.Contains(ix.ObjectId) == false select ix
                    );
                    Prio.SortByPriority(_overflowGo);
                    ap.Assign(Signs.Roles["Overflow1"], _overflowGo[0].GameObject);
                    ap.Assign(Signs.Roles["Overflow2"], _overflowGo[1].GameObject);
                    ap.Assign(Signs.Roles["Overflow3"], _overflowGo[2].GameObject);
                    ap.Assign(Signs.Roles["Overflow4"], _overflowGo[3].GameObject);
                }
                _state.ExecuteAutomarkers(ap, Timing);
            }

        }

        #endregion

        public UltFuturesRewritten(State st) : base(st)
        {
            st.OnZoneChange += OnZoneChange;
        }

        private void OnCastBegin(uint src, uint dest, ushort actionId, float castTime, float rotation)
        {
            switch (actionId)
            {
                case AbilityFallOfFaith:
                    CurrentPhase = PhaseEnum.P1_Faith;
                    break;
                case AbilityQuadrupleSlap:
                    CurrentPhase = PhaseEnum.P2_Shiva;
                    break;
            }
        }

        private void OnTether(uint src, uint dest, uint tetherId)
        {       
            switch (CurrentPhase)
            {
                case PhaseEnum.P1_Faith:
                    if (tetherId == TetherFire || tetherId == TetherLightning)
                    {
                        _fallFaithAm.FeedTether(src, dest, tetherId);
                    }
                    break;
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
                _state.OnCastBegin += OnCastBegin;
                _state.OnTether += OnTether;
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
                _state.OnTether -= OnTether;
                _state.OnCastBegin -= OnCastBegin;
                _subbed = false;
            }
        }

        protected override bool ExecutionImplementation()
        {
            if (ZoneOk == true)
            {
                return base.ExecutionImplementation();
            }
            return false;
        }

        private void OnCombatChange(bool inCombat)
        {
            Reset();
            if (inCombat == true)
            {
                CurrentPhase = PhaseEnum.P1_Start;
                SubscribeToEvents();
            }
            else
            {
                UnsubscribeFromEvents();
            }
        }

        private void OnZoneChange(ushort newZone)
        {
            bool newZoneOk = (newZone == 1238);
            if (newZoneOk == true && ZoneOk == false)
            {
                Log(State.LogLevelEnum.Info, null, "Content available");                
                _fallFaithAm = (FallOfFaithAM)Items["FallOfFaithAM"];
                _state.OnCombatChange += OnCombatChange;
                LogItems();
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
