using System;
using Lemegeton.Core;
using System.Collections.Generic;
using System.Linq;

namespace Lemegeton.Content
{

    internal class UltUcob : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

        private const int AbilityChainLighting = 0x26c7;
        private const int StatusThunderstruck = 466;

        private bool ZoneOk = false;

        private ChainLightningAm _chainLightningAm;

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
            public Action Test { get; set; }

            private List<uint> _lightnings = new List<uint>();
            private bool _fired = false;

            public ChainLightningAm(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Signs.SetRole("Lightning1", AutomarkerSigns.SignEnum.Ignore1, false);
                Signs.SetRole("Lightning2", AutomarkerSigns.SignEnum.Ignore2, false);
                Test = new Action(() => Signs.TestFunctionality(state, null, Timing, SelfMarkOnly));
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
                AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly);
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
            _state.OnAction += _state_OnAction;
            _state.OnStatusChange += _state_OnStatusChange;
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
        }

        private void UnsubscribeFromEvents()
        {
            _state.OnStatusChange -= _state_OnStatusChange;
            _state.OnAction -= _state_OnAction;
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

        private void OnZoneChange(ushort newZone)
        {
            bool newZoneOk = (newZone == 733);
            if (newZoneOk == true && ZoneOk == false)
            {
                Log(State.LogLevelEnum.Info, null, "Content available");
                _chainLightningAm = (ChainLightningAm)Items["ChainLightningAm"];
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
