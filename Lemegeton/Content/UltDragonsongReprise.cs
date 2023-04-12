using System;
using Lemegeton.Core;
using System.Collections.Generic;
using System.Linq;

namespace Lemegeton.Content
{

    internal class UltDragonsongReprise : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

        private const int AbilityAscalonsMercyConcealed = 25544;
        private const int AbilityWrothFlames = 27973;
        private const int AbilityHotWing = 27947;
        private const int AbilityHotTail = 27949;

        private const int StatusEntangledFlames = 2759;
        private const int StatusSpreadingFlames = 2758;

        private bool ZoneOk = false;

        private WrothAM _wrothAm;

        private enum PhaseEnum
        {
            P2,
            P6,
            P6_PastWroth,
        }

        private PhaseEnum CurrentPhase { get; set; } = PhaseEnum.P2;

        #region WrothAM

        public class WrothAM : Core.ContentItem
        {

            public override FeaturesEnum Features
            {
                get
                {
                    return _state.cfg.AutomarkerSoft == false ? FeaturesEnum.Automarker : FeaturesEnum.Drawing;
                }
            }

            [AttributeOrderNumber(1000)]
            public AutomarkerSigns Signs { get; set; }

            [AttributeOrderNumber(2000)]
            public AutomarkerPrio Prio { get; set; }

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public Action Test { get; set; }

            private List<uint> _spreads = new List<uint>();
            private List<uint> _stacks = new List<uint>();

            public WrothAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Prio = new AutomarkerPrio();
                Prio.Priority = AutomarkerPrio.PrioTypeEnum.Role;
                SetupPresets();
                Signs.ApplyPreset("LPDU");
                Test = new Action(() => Signs.TestFunctionality(state, Prio));
            }

            private void SetupPresets()
            {
                Dictionary<string, AutomarkerSigns.SignEnum> pr;
                pr = new Dictionary<string, AutomarkerSigns.SignEnum>();
                pr["Stack1_1"] = AutomarkerSigns.SignEnum.Bind1;
                pr["Stack1_2"] = AutomarkerSigns.SignEnum.Ignore1;
                pr["Stack2_1"] = AutomarkerSigns.SignEnum.Bind2;
                pr["Stack2_2"] = AutomarkerSigns.SignEnum.Ignore2;
                pr["Spread1"] = AutomarkerSigns.SignEnum.Attack1;
                pr["Spread2"] = AutomarkerSigns.SignEnum.Attack2;
                pr["Spread3"] = AutomarkerSigns.SignEnum.Attack3;
                pr["Spread4"] = AutomarkerSigns.SignEnum.Attack4;
                Signs.Presets["LPDU"] = pr;
            }

            internal void Reset()
            {
                _spreads.Clear();
                _stacks.Clear();
            }

            internal void FeedAction(uint actionId)
            {
                if (Active == false)
                {
                    return;
                }
                if (actionId == AbilityHotTail || actionId == AbilityHotWing)
                {
                    _state.Log(State.LogLevelEnum.Debug, null, "Registered ability {0}, clearing automarkers", actionId);
                    AutomarkerPayload ap = new AutomarkerPayload();
                    ap.Clear = true;
                    _state.ExecuteAutomarkers(ap);
                }
            }

            internal void FeedStatus(uint actorId, uint statusId)
            {
                if (Active == false)
                {
                    return;
                }
                switch (statusId)
                {
                    case StatusEntangledFlames:
                        _state.Log(State.LogLevelEnum.Debug, null, "Registered status {0} for {1}", statusId, actorId);
                        _stacks.Add(actorId);
                        break;
                    case StatusSpreadingFlames:
                        _state.Log(State.LogLevelEnum.Debug, null, "Registered status {0} for {1}", statusId, actorId);
                        _spreads.Add(actorId);
                        break;
                    default:
                        return;
                }
                if (_stacks.Count != 2 && _spreads.Count != 4)
                {
                    return;
                }
                _state.Log(State.LogLevelEnum.Debug, null, "All stacks and spreads registered, ready for automarkers");
                Party pty = _state.GetPartyMembers();
                List<Party.PartyMember> _stacksGo = new List<Party.PartyMember>(
                    from ix in pty.Members join jx in _stacks on ix.ObjectId equals jx select ix
                );                
                List<Party.PartyMember> _spreadsGo = new List<Party.PartyMember>(
                    from ix in pty.Members join jx in _spreads on ix.ObjectId equals jx select ix
                );
                List<Party.PartyMember> _unmarkedGo = new List<Party.PartyMember>(
                    from ix in pty.Members where _stacksGo.Contains(ix) == false && _spreadsGo.Contains(ix) == false select ix
                );
                Prio.SortByPriority(_stacksGo);
                Prio.SortByPriority(_spreadsGo);
                Prio.SortByPriority(_unmarkedGo);
                AutomarkerPayload ap = new AutomarkerPayload();
                ap.assignments[Signs.Roles["Stack1_1"]] = _stacksGo[0].GameObject;
                ap.assignments[Signs.Roles["Stack1_2"]] = _unmarkedGo[0].GameObject;
                ap.assignments[Signs.Roles["Stack2_1"]] = _stacksGo[1].GameObject;
                ap.assignments[Signs.Roles["Stack2_2"]] = _unmarkedGo[1].GameObject;
                ap.assignments[Signs.Roles["Spread1"]] = _spreadsGo[0].GameObject;
                ap.assignments[Signs.Roles["Spread2"]] = _spreadsGo[1].GameObject;
                ap.assignments[Signs.Roles["Spread3"]] = _spreadsGo[2].GameObject;
                ap.assignments[Signs.Roles["Spread4"]] = _spreadsGo[3].GameObject;
                _state.ExecuteAutomarkers(ap);
            }

        }

        #endregion

        public UltDragonsongReprise(State st) : base(st)
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
            _state.OnCastBegin += OnCastBegin;
            _state.OnAction += OnAction;
            _state.OnStatusChange += OnStatusChange;
        }

        private void OnStatusChange(uint src, uint dest, uint statusId, bool gained, float duration, int stacks)
        {
            if (gained == false)
            {
                return;
            }
            if (statusId == StatusEntangledFlames || statusId == StatusSpreadingFlames)
            {
                _wrothAm.FeedStatus(dest, statusId);
            }
        }

        private void OnCastBegin(uint src, uint dest, ushort actionId, float castTime, float rotation)
        {
            if (actionId == AbilityAscalonsMercyConcealed)
            {
                CurrentPhase = PhaseEnum.P2;
            }
            if (actionId == AbilityWrothFlames)
            {
                CurrentPhase = PhaseEnum.P6;
            }
        }

        private void OnAction(uint src, uint dest, ushort actionId)
        {
            if ((actionId == AbilityHotWing || actionId == AbilityHotTail) && CurrentPhase == PhaseEnum.P6)
            {
                CurrentPhase = PhaseEnum.P6_PastWroth;
                _wrothAm.FeedAction(actionId);
            }
        }

        private void UnsubscribeFromEvents()
        {
            _state.OnStatusChange -= OnStatusChange;
            _state.OnAction -= OnAction;
            _state.OnCastBegin -= OnCastBegin;
        }

        private void OnCombatChange(bool inCombat)
        {
            if (inCombat == true)
            {
                CurrentPhase = PhaseEnum.P2;
                SubscribeToEvents();
            }
            else
            {
                UnsubscribeFromEvents();
            }
        }

        private void OnZoneChange(ushort newZone)
        {
            bool newZoneOk = (newZone == 968);
            if (newZoneOk == true && ZoneOk == false)
            {
                _state.Log(State.LogLevelEnum.Info, null, "Content {0} available", GetType().Name);
                _wrothAm = (WrothAM)Items["WrothAM"];
                _state.OnCombatChange += OnCombatChange;
            }
            else if (newZoneOk == false && ZoneOk == true)
            {
                _state.Log(State.LogLevelEnum.Info, null, "Content {0} unavailable", GetType().Name);
                _state.OnCombatChange -= OnCombatChange;
            }
            ZoneOk = newZoneOk;
        }

    }

}
