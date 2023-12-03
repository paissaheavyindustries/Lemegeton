using System;
using Lemegeton.Core;
using System.Collections.Generic;
using System.Linq;

namespace Lemegeton.Content
{

    internal class UltWeaponsRefrain : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

        private const int AbilityTitanGaol1 = 0x2b6b;
        private const int AbilityTitanGaol2 = 0x2b6c;
        private const int StatusFetters = 292;

        private bool ZoneOk = false;

        private GaolAM _gaolAm;

        #region GaolAM

        public class GaolAM : Automarker
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

            private List<uint> _gaols = new List<uint>();
            private bool _fired = false;

            public GaolAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Prio = new AutomarkerPrio();
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Prio.Priority = AutomarkerPrio.PrioTypeEnum.Role;
                Prio._prioByRole.Clear();
                Prio._prioByRole.Add(AutomarkerPrio.PrioRoleEnum.Melee);
                Prio._prioByRole.Add(AutomarkerPrio.PrioRoleEnum.Tank);
                Prio._prioByRole.Add(AutomarkerPrio.PrioRoleEnum.Healer);
                Prio._prioByRole.Add(AutomarkerPrio.PrioRoleEnum.Ranged);
                Prio._prioByRole.Add(AutomarkerPrio.PrioRoleEnum.Caster);
                Signs.SetRole("Gaol1", AutomarkerSigns.SignEnum.Attack1, false);
                Signs.SetRole("Gaol2", AutomarkerSigns.SignEnum.Attack2, false);
                Signs.SetRole("Gaol3", AutomarkerSigns.SignEnum.Attack3, false);
                Test = new System.Action(() => Signs.TestFunctionality(state, Prio, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _fired = false;
                _gaols.Clear();
            }

            internal void FeedAction(uint actorId, uint actionId)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered action {0} on {1:X}", actionId, actorId);
                _gaols.Add(actorId);
                if (_gaols.Count < 3)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Ready for automarkers");
                Party pty = _state.GetPartyMembers();
                List<Party.PartyMember> _gaolsGo = new List<Party.PartyMember>(
                    from ix in pty.Members join jx in _gaols on ix.ObjectId equals jx select ix
                );
                Prio.SortByPriority(_gaolsGo);
                AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                ap.Assign(Signs.Roles["Gaol1"], _gaolsGo[0].GameObject);
                ap.Assign(Signs.Roles["Gaol2"], _gaolsGo[1].GameObject);
                ap.Assign(Signs.Roles["Gaol3"], _gaolsGo[2].GameObject);
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

        public UltWeaponsRefrain(State st) : base(st)
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
            if (statusId == StatusFetters)
            {
                _gaolAm.FeedStatus(statusId, gained);
            }
        }

        private void _state_OnAction(uint src, uint dest, ushort actionId)
        {
            if (actionId == AbilityTitanGaol1 || actionId == AbilityTitanGaol2)
            {
                _gaolAm.FeedAction(dest, actionId);
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
            bool newZoneOk = (newZone == 777);
            if (newZoneOk == true && ZoneOk == false)
            {
                Log(State.LogLevelEnum.Info, null, "Content available");
                _gaolAm = (GaolAM)Items["GaolAM"];
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
