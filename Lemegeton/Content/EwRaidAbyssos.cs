using System;
using Lemegeton.Core;
using System.Collections.Generic;
using System.Linq;
using System.Data;

namespace Lemegeton.Content
{

    internal class EwRaidAbyssos : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

        private const int AbilityInviolateBonds = 30746;
        private const int AbilityInviolatePurgation = 30750;
        private const int StatusBond1 = 0xced;
        private const int StatusBond2 = 0xd46;
        private const int StatusPurgation1 = 0xcef;
        private const int StatusPurgation2 = 0xd42;
        private const int StatusPurgation3 = 0xd43;
        private const int StatusPurgation4 = 0xd44;

        private bool ZoneOk = false;

        private InviolateAM _inviolateAm;

        #region InviolateAM

        public class InviolateAM : Automarker
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
            public Action Test { get; set; }

            private uint _currentAction = 0;
            private uint _share1 = 0;
            private uint _share2 = 0;
            private uint _share3 = 0;
            private uint _share4 = 0;

            private Queue<AutomarkerPayload> payloads = new Queue<AutomarkerPayload>();

            public InviolateAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Prio = new AutomarkerPrio();
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Prio.Priority = AutomarkerPrio.PrioTypeEnum.Job;
                Signs.SetRole("ShareTarget", AutomarkerSigns.SignEnum.Circle, false);
                Signs.SetRole("Share1", AutomarkerSigns.SignEnum.Bind1, false);
                Signs.SetRole("Share2", AutomarkerSigns.SignEnum.Bind2, false);
                Signs.SetRole("Share3", AutomarkerSigns.SignEnum.Bind3, false);
                Signs.SetRole("Spread1", AutomarkerSigns.SignEnum.Attack1, false);
                Signs.SetRole("Spread2", AutomarkerSigns.SignEnum.Attack2, false);
                Signs.SetRole("Spread3", AutomarkerSigns.SignEnum.Attack3, false);
                Signs.SetRole("Spread4", AutomarkerSigns.SignEnum.Attack4, false);
                Test = new Action(() => Signs.TestFunctionality(state, Prio, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                payloads.Clear();
                _currentAction = 0;
                _share1 = 0;
                _share2 = 0;
                _share3 = 0;
                _share4 = 0;
            }

            internal void FeedAction(uint actionId)
            {
                if (Active == false)
                {
                    return;
                }
                Reset();
                Log(State.LogLevelEnum.Debug, null, "Registered action {0}", actionId);
                _currentAction = actionId;
            }

            internal void NextAutomarkers()
            {
                AutomarkerPayload ap;
                if (payloads.Count > 0)
                {
                    ap = payloads.Dequeue();
                    _state.ExecuteAutomarkers(ap, Timing);
                }
                else
                {
                    _state.ClearAutoMarkers();
                }
            }

            internal void FeedStatus(uint dest, uint statusId, bool gained)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered status {0} {1} on {2:X}", statusId, gained, dest);
                switch (statusId)
                {
                    case StatusBond1:
                    case StatusPurgation1:
                        if (gained == false)
                        {
                            NextAutomarkers();
                        }
                        else
                        {
                            _share1 = dest;
                        }
                        break;
                    case StatusBond2:
                    case StatusPurgation2:
                        if (gained == false)
                        {
                            NextAutomarkers();
                        }
                        else
                        {
                            _share2 = dest;
                        }
                        break;
                    case StatusPurgation3:
                        if (gained == false)
                        {
                            NextAutomarkers();
                        }
                        else
                        {
                            _share3 = dest;
                        }
                        break;
                    case StatusPurgation4:
                        if (gained == false)
                        {
                            NextAutomarkers();
                        }
                        else
                        {
                            _share4 = dest;
                        }
                        break;
                }
                if (
                    (_currentAction == AbilityInviolateBonds && _share1 > 0 && _share2 > 0)
                    ||
                    (_currentAction == AbilityInviolatePurgation && _share1 > 0 && _share2 > 0 && _share3 > 0 && _share4 > 0)
                )
                {
                    Log(State.LogLevelEnum.Debug, null, "Ready for automarkers");
                    CreatePayloadForShare(_share1);
                    CreatePayloadForShare(_share2);
                    if (_currentAction == AbilityInviolatePurgation)
                    {
                        CreatePayloadForShare(_share3);
                        CreatePayloadForShare(_share4);
                    }
                    NextAutomarkers();
                }
            }

            internal void CreatePayloadForShare(uint shareId)
            {
                AutomarkerPayload ap;
                AutomarkerPrio.PrioArchetypeEnum role;
                Party.PartyMember pm;
                List<Party.PartyMember> _sharesGo;
                List<Party.PartyMember> _spreadsGo;
                Party pty = _state.GetPartyMembers();
                pm = (from ix in pty.Members where ix.ObjectId == _share1 select ix).First();
                ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                role = AutomarkerPrio.JobToArchetype(pm.Job);
                _sharesGo = new List<Party.PartyMember>(
                                from ix in pty.Members
                                where AutomarkerPrio.JobToArchetype(ix.Job) == role && ix.ObjectId != _share1
                                select ix);
                _spreadsGo = new List<Party.PartyMember>(
                                from ix in pty.Members
                                where AutomarkerPrio.JobToArchetype(ix.Job) != role
                                select ix);
                Prio.SortByPriority(_sharesGo);
                Prio.SortByPriority(_spreadsGo);
                ap.Assign(Signs.Roles["ShareTarget"], pm.GameObject);
                ap.Assign(Signs.Roles["Share1"], _sharesGo[0].GameObject);
                ap.Assign(Signs.Roles["Share2"], _sharesGo[1].GameObject);
                ap.Assign(Signs.Roles["Share3"], _sharesGo[2].GameObject);
                ap.Assign(Signs.Roles["Spread1"], _spreadsGo[0].GameObject);
                ap.Assign(Signs.Roles["Spread2"], _spreadsGo[1].GameObject);
                ap.Assign(Signs.Roles["Spread3"], _spreadsGo[2].GameObject);
                ap.Assign(Signs.Roles["Spread4"], _spreadsGo[3].GameObject);
                payloads.Enqueue(ap);
            }

        }

        #endregion

        public EwRaidAbyssos(State st) : base(st)
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
            _state.OnCastBegin += _state_OnCastBegin;
            _state.OnStatusChange += _state_OnStatusChange;
        }

        private void _state_OnStatusChange(uint src, uint dest, uint statusId, bool gained, float duration, int stacks)
        {
            if (statusId == StatusBond1 || statusId == StatusBond2 || statusId == StatusPurgation1 || statusId == StatusPurgation2 || statusId == StatusPurgation3 || statusId == StatusPurgation4)
            {
                _inviolateAm.FeedStatus(dest, statusId, gained);
            }
        }

        private void UnsubscribeFromEvents()
        {
            _state.OnStatusChange -= _state_OnStatusChange;
            _state.OnCastBegin -= _state_OnCastBegin;
        }

        private void _state_OnCastBegin(uint src, uint dest, ushort actionId, float castTime, float rotation)
        {
            if (actionId == AbilityInviolateBonds || actionId == AbilityInviolatePurgation)
            {
                _inviolateAm.FeedAction(actionId);
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

        private void OnZoneChange(ushort newZone)
        {
            bool newZoneOk = (newZone == 1086);
            if (newZoneOk == true && ZoneOk == false)
            {
                Log(State.LogLevelEnum.Info, null, "Content available");
                _inviolateAm = (InviolateAM)Items["InviolateAM"];
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
