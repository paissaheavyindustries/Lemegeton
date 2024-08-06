using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using Dalamud.Game.ClientState.Objects.Types;
using Lemegeton.Core;
using static Lemegeton.Core.State;

namespace Lemegeton.Content
{

    internal class DtRaidLightHeavy : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

        private enum ZoneEnum
        {
            None = 0,
            M4s = 1232,
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

        private bool _subbed = false;

        private const int AbilityElectropeEdge = 38341;
        private const int AbilityWitchgleamHit = 38790;
        private const int AbilitySidewiseSpark1 = 38380;
        private const int AbilitySidewiseSpark2 = 38381;

        private const int StatusElectricalCondenser = 3999;
        private const int StatusSpecial = 2970;
        private const int StatusSpecialPair = 752;        

        private CondenserAM _condenserAM;

        #region CondenserAM

        public class CondenserAM : Automarker
        {

            [AttributeOrderNumber(1000)]
            public AutomarkerSigns Signs { get; set; }

            [AttributeOrderNumber(1100)]
            public AutomarkerSigns Signs2 { get; set; }

            [AttributeOrderNumber(2000)]
            public AutomarkerPrio Prio { get; set; }

            [DebugOption]
            [AttributeOrderNumber(2500)]
            public AutomarkerTiming Timing { get; set; }

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public System.Action Test { get; set; }

            public enum AutomarkerStepEnum
            {
                Start,
                Shorts,
                Pairs,
                Spreads,
                Longs,
                End,
            }

            private AutomarkerStepEnum _step = AutomarkerStepEnum.Start;
            private bool _fired = false;
            private int _hitTotal = 0;
            private Dictionary<uint, int> _hits = new Dictionary<uint, int>();
            private List<uint> _longs = new List<uint>();
            private List<uint> _shorts = new List<uint>();

            private AutomarkerPayload _apShorts = null;
            private AutomarkerPayload _apLongs = null;
            private AutomarkerPayload _apPairs = null;

            public CondenserAM(State state) : base(state)
            {
                Enabled = false;
                Prio = new AutomarkerPrio() { Priority = AutomarkerPrio.PrioTypeEnum.Role };
                Prio._prioByRole.Clear();
                Prio._prioByRole.AddRange(new AutomarkerPrio.PrioRoleEnum[] { AutomarkerPrio.PrioRoleEnum.Melee, AutomarkerPrio.PrioRoleEnum.Tank, AutomarkerPrio.PrioRoleEnum.Caster, AutomarkerPrio.PrioRoleEnum.Ranged, AutomarkerPrio.PrioRoleEnum.Healer });
                Signs = new AutomarkerSigns();
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Signs.SetRole("DPS2", AutomarkerSigns.SignEnum.Attack2, false);
                Signs.SetRole("DPS3", AutomarkerSigns.SignEnum.Attack3, false);
                Signs.SetRole("Support2", AutomarkerSigns.SignEnum.Bind2, false);
                Signs.SetRole("Support3", AutomarkerSigns.SignEnum.Bind3, false);
                Signs2 = new AutomarkerSigns();
                Signs2.SetRole("Pair1_S", AutomarkerSigns.SignEnum.Attack1, false);
                Signs2.SetRole("Pair1_L", AutomarkerSigns.SignEnum.Attack2, false);
                Signs2.SetRole("Pair2_S", AutomarkerSigns.SignEnum.Bind1, false);
                Signs2.SetRole("Pair2_L", AutomarkerSigns.SignEnum.Bind2, false);
                Signs2.SetRole("Pair3_S", AutomarkerSigns.SignEnum.Ignore1, false);
                Signs2.SetRole("Pair3_L", AutomarkerSigns.SignEnum.Ignore2, false);
                Signs2.SetRole("Pair4_S", AutomarkerSigns.SignEnum.Square, false);
                Signs2.SetRole("Pair4_L", AutomarkerSigns.SignEnum.Circle, false);
                Test = new System.Action(() => Signs.TestFunctionality(state, null, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _fired = false;
                _step = AutomarkerStepEnum.Start;
                _hits.Clear();
                _longs.Clear();
                _shorts.Clear();
                _hitTotal = 0;
            }

            internal void FeedHit(uint actorId)
            {
                if (Active == false || _hits.ContainsKey(actorId) == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered Witchgleam hit {0} on {1:X}", _hitTotal + 1, actorId);
                _hits[actorId] = _hits[actorId] + 1;
                _hitTotal++;
                if (_hitTotal == 16)
                {
                    // all hits registered
                    ReadyForAutoMarkers();
                }
            }

            internal void FeedSpecialStatus(int statusId)
            {
                if (Active == false || _step != AutomarkerStepEnum.Spreads)
                {
                    return;
                }
                _step = AutomarkerStepEnum.Pairs;
                _state.ExecuteAutomarkers(_apPairs, Timing);
            }

            internal void FeedAction(uint actionId)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered action {0}", actionId);
                switch (actionId)
                {
                    case AbilityElectropeEdge:
                        Reset();
                        break;
                    case AbilitySidewiseSpark1:
                    case AbilitySidewiseSpark2:
                        if (_step == AutomarkerStepEnum.Spreads || _step == AutomarkerStepEnum.Pairs)
                        {
                            if (_step == AutomarkerStepEnum.Pairs)
                            {
                                _state.ClearAutoMarkers();
                            }
                            _step = AutomarkerStepEnum.Longs;                            
                            _state.ExecuteAutomarkers(_apLongs, Timing);
                        }
                        break;
                }
            }

            internal void FeedStatus(uint actorId, bool gained, float duration)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered ElectricalCondenser {0} for {1} on {2:X}", gained, duration, actorId);
                if (gained)
                {
                    if (duration > 32.0f)
                    {
                        _longs.Add(actorId);
                        _hits[actorId] = 1;
                    }
                    else
                    {
                        _shorts.Add(actorId);
                        _hits[actorId] = 0;
                    }
                }
                else
                {
                    if (_step == AutomarkerStepEnum.Shorts)
                    {
                        _state.ClearAutoMarkers();
                        _step = AutomarkerStepEnum.Spreads;
                    }
                    if (_step == AutomarkerStepEnum.Longs)
                    {
                        _state.ClearAutoMarkers();
                        _step = AutomarkerStepEnum.End;
                    }
                }
            }

            private void ReadyForAutoMarkers()
            {
                Log(State.LogLevelEnum.Debug, null, "Ready for automarkers");
                Party pty = _state.GetPartyMembers();
                List<Party.PartyMember> allshorts = pty.GetByActorIds(_shorts);
                List<Party.PartyMember> alllongs = pty.GetByActorIds(_longs);
                List<Party.PartyMember> hits3 = pty.GetByActorIds(from hx in _hits where hx.Value >= 3 select hx.Key).Take(4).ToList();
                List<Party.PartyMember> hits2 = (from px in pty.Members where hits3.Contains(px) == false select px).ToList();
                _apShorts = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                _apShorts.Assign(Signs.Roles["DPS2"], (from px in hits2.Intersect(allshorts) where AutomarkerPrio.JobToTrinity(px.Job) == AutomarkerPrio.PrioTrinityEnum.DPS select px).First());
                _apShorts.Assign(Signs.Roles["DPS3"], (from px in hits3.Intersect(allshorts) where AutomarkerPrio.JobToTrinity(px.Job) == AutomarkerPrio.PrioTrinityEnum.DPS select px).First());
                _apShorts.Assign(Signs.Roles["Support2"], (from px in hits2.Intersect(allshorts) where AutomarkerPrio.JobToTrinity(px.Job) != AutomarkerPrio.PrioTrinityEnum.DPS select px).First());
                _apShorts.Assign(Signs.Roles["Support3"], (from px in hits3.Intersect(allshorts) where AutomarkerPrio.JobToTrinity(px.Job) != AutomarkerPrio.PrioTrinityEnum.DPS select px).First());
                _apPairs = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                Prio.SortByPriority(allshorts);
                Prio.SortByPriority(alllongs);
                for (int i = 0; i < 4; i++)
                {
                    _apPairs.Assign(Signs2.Roles[string.Format("Pair{0}_S", i + 1)], allshorts[i]);
                    _apPairs.Assign(Signs2.Roles[string.Format("Pair{0}_L", i + 1)], alllongs[i]);
                }
                _apLongs = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                _apLongs.Assign(Signs.Roles["DPS2"], (from px in hits2.Intersect(alllongs) where AutomarkerPrio.JobToTrinity(px.Job) == AutomarkerPrio.PrioTrinityEnum.DPS select px).First());
                _apLongs.Assign(Signs.Roles["DPS3"], (from px in hits3.Intersect(alllongs) where AutomarkerPrio.JobToTrinity(px.Job) == AutomarkerPrio.PrioTrinityEnum.DPS select px).First());
                _apLongs.Assign(Signs.Roles["Support2"], (from px in hits2.Intersect(alllongs) where AutomarkerPrio.JobToTrinity(px.Job) != AutomarkerPrio.PrioTrinityEnum.DPS select px).First());
                _apLongs.Assign(Signs.Roles["Support3"], (from px in hits3.Intersect(alllongs) where AutomarkerPrio.JobToTrinity(px.Job) != AutomarkerPrio.PrioTrinityEnum.DPS select px).First());
                _step = AutomarkerStepEnum.Shorts;
                _state.ExecuteAutomarkers(_apShorts, Timing);
                _fired = true;
            }

        }

        #endregion

        public DtRaidLightHeavy(State st) : base(st)
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
            lock (this)
            {
                if (_subbed == true)
                {
                    return;
                }
                _subbed = true;
                Log(LogLevelEnum.Debug, null, "Subscribing to events");
                _state.OnAction += _state_OnAction;
                _state.OnCastBegin += _state_OnCastBegin;
                _state.OnStatusChange += _state_OnStatusChange;
            }
        }

        private void _state_OnAction(uint src, uint dest, ushort actionId)
        {
            switch (actionId)
            {
                case AbilityWitchgleamHit:
                    if (CurrentZone == ZoneEnum.M4s && _condenserAM.Active == true)
                    {
                        _condenserAM.FeedHit(dest);
                    }
                    break;
                case AbilitySidewiseSpark1:
                case AbilitySidewiseSpark2:
                    if (CurrentZone == ZoneEnum.M4s && _condenserAM.Active == true)
                    {
                        _condenserAM.FeedAction(actionId);
                    }
                    break;
            }
        }

        private void _state_OnStatusChange(uint src, uint dest, uint statusId, bool gained, float duration, int stacks)
        {
            switch (statusId)
            {
                case StatusElectricalCondenser:                
                    if (CurrentZone == ZoneEnum.M4s && _condenserAM.Active == true)
                    {
                        _condenserAM.FeedStatus(dest, gained, duration);
                    }
                    break;
                case StatusSpecial:
                    if (CurrentZone == ZoneEnum.M4s && _condenserAM.Active == true)
                    {
                        if (stacks == StatusSpecialPair)
                        {
                            _condenserAM.FeedSpecialStatus(stacks);
                        }                        
                    }
                    break;
            }
        }

        private void _state_OnCastBegin(uint src, uint dest, ushort actionId, float castTime, float rotation)
        {
            switch (actionId)
            {
                case AbilityElectropeEdge:
                    if (CurrentZone == ZoneEnum.M4s && _condenserAM.Active == true)
                    {
                        _condenserAM.FeedAction(actionId);
                    }
                    break;
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
                _state.OnStatusChange -= _state_OnStatusChange;
                _state.OnCastBegin -= _state_OnCastBegin;
                _state.OnAction -= _state_OnAction;
                _subbed = false;
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

        public override void Reset()
        {
            base.Reset();
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
                    case ZoneEnum.M4s:
                        _condenserAM = (CondenserAM)Items["CondenserAM"];
                        break;
                }
                LogItems();
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
