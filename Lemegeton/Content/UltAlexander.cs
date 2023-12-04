using System;
using Dalamud.Game.ClientState.Objects.Types;
using Lemegeton.Core;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using ImGuiNET;
using Character = Dalamud.Game.ClientState.Objects.Types.Character;
using CharacterStruct = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using GameObjectStruct = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using GameObjectPtr = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System.Diagnostics;
using System.Threading;

namespace Lemegeton.Content
{

    internal class UltAlexander : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.Experimental;

        private const int AbilityAlphaSword = 18484;
        private const int AbilitySuperBlasstyCharge = 19279;
        private const int AbilityJKick = 18516;
        private const int AbilityGavel = 18492;
        private const int AbilityJudgmentCrystal = 18524;
        private const int AbilityInceptionFormation = 18543;
        private const int AbilityWormholeFormation = 18542;
        private const int AbilityFinalWord = 18557;
        private const int AbilityFateProjectionAlpha = 18555;
        private const int AbilityFateProjectionBeta = 19219;

        private const int StatusCompressedWater = 2142;
        private const int StatusCompressedLightning = 2143;
        private const int StatusSharedSentence = 1122;
        private const int StatusRestrainingOrder = 1124;
        private const int StatusAggravatedAssault = 1121;
        private const int StatusHouseArrest = 1123;

        private const int StatusFinalWordContactRegulation = 1;
        private const int StatusFinalWordContactProhibition = 2;
        private const int StatusFinalWordEscapeDetection = 3;
        private const int StatusFinalWordEscapeProhibition = 4;

        private const int Headmarker1 = 79;
        private const int Headmarker2 = 80;
        private const int Headmarker3 = 81;
        private const int Headmarker4 = 82;
        private const int Headmarker5 = 83;
        private const int Headmarker6 = 84;
        private const int Headmarker7 = 85;
        private const int Headmarker8 = 86;

        private bool ZoneOk = false;
        private bool _sawFirstHeadMarker = false;
        private uint _firstHeadMarker = 0;

        private LimitCutAM _limitCutAm;
        private WaterLightningAM _waterningAm;
        private TemporalAM _temporalAm;
        private CrystalAM _crystalAm;
        private InceptionAM _inceptionAm;
        private WormholeAM _wormholeAm;
        private FinalWordAM _finalWordAm;

        private enum PhaseEnum
        {
            Start,
            LimitCut,
            BJCC,
            Inception,
            Wormhole,
            FinalWord,
            FateAlpha,
            FateBeta,
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

        #region LimitCutAM

        public class LimitCutAM : Automarker
        {

            [AttributeOrderNumber(1000)]
            public AutomarkerSigns Signs { get; set; }

            [DebugOption]
            [AttributeOrderNumber(2500)]
            public AutomarkerTiming Timing { get; set; }

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public System.Action Test { get; set; }

            private uint[] _markers = new uint[8];
            private int _markerCount = 0;
            private bool _fired = false;
            private int _cutCount = 0;

            public LimitCutAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Signs.SetRole("One", AutomarkerSigns.SignEnum.Attack1, false);
                Signs.SetRole("Two", AutomarkerSigns.SignEnum.Attack2, false);
                Signs.SetRole("Three", AutomarkerSigns.SignEnum.Attack3, false);
                Signs.SetRole("Four", AutomarkerSigns.SignEnum.Attack4, false);
                Signs.SetRole("Five", AutomarkerSigns.SignEnum.Attack5, false);
                Signs.SetRole("Six", AutomarkerSigns.SignEnum.Attack6, false);
                Signs.SetRole("Seven", AutomarkerSigns.SignEnum.Attack7, false);
                Signs.SetRole("Eight", AutomarkerSigns.SignEnum.Attack8, false);
                Test = new System.Action(() => Signs.TestFunctionality(state, null, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _markerCount = 0;
                _cutCount = 0;
                _fired = false;
            }

            internal void FeedAbility(uint abilityId)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered ability {0}", abilityId);
                switch (abilityId)
                {
                    case AbilityAlphaSword:
                    case AbilitySuperBlasstyCharge:
                        _cutCount++;
                        if (_cutCount == 8)
                        {
                            Log(State.LogLevelEnum.Debug, null, "Resetting automarkers");
                            _state.ClearAutoMarkers();
                        }
                        break;
                }
            }

            internal void FeedHeadmarker(uint actorId, uint markerId)
            {
                if (Active == false)
                {
                    return;
                }                
                switch (markerId)
                {
                    case Headmarker1: _markers[0] = actorId; _markerCount++; break;
                    case Headmarker2: _markers[1] = actorId; _markerCount++; break;
                    case Headmarker3: _markers[2] = actorId; _markerCount++; break;
                    case Headmarker4: _markers[3] = actorId; _markerCount++; break;
                    case Headmarker5: _markers[4] = actorId; _markerCount++; break;
                    case Headmarker6: _markers[5] = actorId; _markerCount++; break;
                    case Headmarker7: _markers[6] = actorId; _markerCount++; break;
                    case Headmarker8: _markers[7] = actorId; _markerCount++; break;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered headmarker {0} on {1:X}, marker count is {2}", markerId, actorId, _markerCount);
                if (_markerCount == 8 && _fired == false)
                {                    
                    Party pty = _state.GetPartyMembers();
                    AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                    ap.Assign(Signs.Roles["One"], pty.GetByActorId(_markers[0]).GameObject);
                    ap.Assign(Signs.Roles["Two"], pty.GetByActorId(_markers[1]).GameObject);
                    ap.Assign(Signs.Roles["Three"], pty.GetByActorId(_markers[2]).GameObject);
                    ap.Assign(Signs.Roles["Four"], pty.GetByActorId(_markers[3]).GameObject);
                    ap.Assign(Signs.Roles["Five"], pty.GetByActorId(_markers[4]).GameObject);
                    ap.Assign(Signs.Roles["Six"], pty.GetByActorId(_markers[5]).GameObject);
                    ap.Assign(Signs.Roles["Seven"], pty.GetByActorId(_markers[6]).GameObject);
                    ap.Assign(Signs.Roles["Eight"], pty.GetByActorId(_markers[7]).GameObject);
                    _state.ExecuteAutomarkers(ap, Timing);
                    _fired = true;
                }
            }

        }

        #endregion

        #region WaterLightningAM

        public class WaterLightningAM : Automarker
        {

            [AttributeOrderNumber(1000)]
            public AutomarkerSigns Signs { get; set; }

            [DebugOption]
            [AttributeOrderNumber(2500)]
            public AutomarkerTiming Timing { get; set; }

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public System.Action Test { get; set; }

            public WaterLightningAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Signs.SetRole("Water", AutomarkerSigns.SignEnum.Circle, false);
                Signs.SetRole("Lightning", AutomarkerSigns.SignEnum.Plus, false);
                Test = new System.Action(() => Signs.TestFunctionality(state, null, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
            }

            internal void FeedAbility(uint abilityId)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered action {0}", abilityId);
                if (abilityId == AbilityGavel)
                {
                    Log(State.LogLevelEnum.Debug, null, "Clearing automarkers");
                    _state.ClearAutoMarkers();
                }
            }

            internal void FeedStatus(uint actorId, uint statusId)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1:X}", statusId, actorId);
                switch (statusId)
                {
                    case StatusCompressedWater:
                        {
                            GameObject go = _state.GetActorById(actorId);
                            AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                            ap.Assign(Signs.Roles["Water"], go);
                            _state.ExecuteAutomarkers(ap, Timing);
                        }
                        break;
                    case StatusCompressedLightning:
                        {
                            GameObject go = _state.GetActorById(actorId);
                            AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                            ap.Assign(Signs.Roles["Lightning"], go);
                            _state.ExecuteAutomarkers(ap, Timing);
                        }
                        break;
                }
            }

        }

        #endregion

        #region TemporalAM

        public class TemporalAM : Automarker
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
            private List<uint> _house = new List<uint>();
            private List<uint> _aggro = new List<uint>();
            private List<uint> _rest = new List<uint>();

            public TemporalAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Prio = new AutomarkerPrio();
                Prio.Priority = AutomarkerPrio.PrioTypeEnum.Role;
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Signs.SetRole("House1", AutomarkerSigns.SignEnum.Bind1, false);
                Signs.SetRole("House2", AutomarkerSigns.SignEnum.Bind2, false);
                Signs.SetRole("Restraining1", AutomarkerSigns.SignEnum.Ignore1, false);
                Signs.SetRole("Restraining2", AutomarkerSigns.SignEnum.Ignore2, false);
                Signs.SetRole("Aggravated1", AutomarkerSigns.SignEnum.Circle, false);
                Signs.SetRole("Aggravated2", AutomarkerSigns.SignEnum.Square, false);
                Signs.SetRole("Nothing1", AutomarkerSigns.SignEnum.Attack1, false);
                Signs.SetRole("Nothing2", AutomarkerSigns.SignEnum.Attack2, false);
                Test = new System.Action(() => Signs.TestFunctionality(state, null, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _fired = false;
                _house.Clear();
                _aggro.Clear();
                _rest.Clear();
            }

            private void PerformMarking()
            {
                Log(State.LogLevelEnum.Debug, null, "Ready for automarkers");
                Party pty = _state.GetPartyMembers();
                List<Party.PartyMember> _houseGo = pty.GetByActorIds(_house);
                List<Party.PartyMember> _restGo = pty.GetByActorIds(_rest);
                List<Party.PartyMember> _aggroGo = pty.GetByActorIds(_aggro);
                List<Party.PartyMember> _unmarkedGo = new List<Party.PartyMember>(
                    from ix in pty.Members where 
                        _houseGo.Contains(ix) == false &&
                        _restGo.Contains(ix) == false &&
                        _aggroGo.Contains(ix) == false
                    select ix
                );
                Prio.SortByPriority(_houseGo);
                Prio.SortByPriority(_restGo);
                Prio.SortByPriority(_aggroGo);
                Prio.SortByPriority(_unmarkedGo);
                AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                ap.Assign(Signs.Roles["House1"], _houseGo[0].GameObject);
                ap.Assign(Signs.Roles["House2"], _houseGo[1].GameObject);
                ap.Assign(Signs.Roles["Restraining1"], _restGo[0].GameObject);
                ap.Assign(Signs.Roles["Restraining2"], _restGo[1].GameObject);
                ap.Assign(Signs.Roles["Aggravated1"], _aggroGo[0].GameObject);
                ap.Assign(Signs.Roles["Aggravated2"], _aggroGo[1].GameObject);
                ap.Assign(Signs.Roles["Nothing1"], _unmarkedGo[0].GameObject);
                ap.Assign(Signs.Roles["Nothing2"], _unmarkedGo[1].GameObject);
                _state.ExecuteAutomarkers(ap, Timing);
                _fired = true;
            }

            internal void FeedStatus(uint actorId, uint statusId, bool gained)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered status {0} {1} on {2:X}", statusId, gained, actorId);
                switch (statusId)
                {
                    case StatusHouseArrest:
                    case StatusRestrainingOrder:
                    case StatusAggravatedAssault:
                        if (gained == false && _fired == true)
                        {
                            Log(State.LogLevelEnum.Debug, null, "Clearing automarkers");
                            _state.ClearAutoMarkers();
                            _fired = false;
                        }
                        else if (gained == true && _fired == false)
                        {
                            if (statusId == StatusHouseArrest)
                            {
                                _house.Add(actorId);
                            }
                            if (statusId == StatusRestrainingOrder)
                            {
                                _rest.Add(actorId);
                            }
                            if (statusId == StatusAggravatedAssault)
                            {
                                _aggro.Add(actorId);
                            }
                            if (_house.Count == 2 && _rest.Count == 2 && _aggro.Count == 2)
                            {
                                PerformMarking();
                            }
                        }
                        break;
                }
            }

        }

        #endregion

        #region CrystalAM

        public class CrystalAM : Automarker
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

            public List<uint> _tethers = new List<uint>();
            private bool _fired = false;

            public CrystalAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Prio = new AutomarkerPrio();
                Prio.Priority = AutomarkerPrio.PrioTypeEnum.Role;
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Signs.SetRole("Crystal1", AutomarkerSigns.SignEnum.Attack1, false);
                Signs.SetRole("Crystal2", AutomarkerSigns.SignEnum.Attack2, false);
                Signs.SetRole("Crystal3", AutomarkerSigns.SignEnum.Attack3, false);
                Signs.SetRole("Crystal4", AutomarkerSigns.SignEnum.Attack4, false);
                Test = new System.Action(() => Signs.TestFunctionality(state, null, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _tethers.Clear();
                _fired = false;
            }

            internal void FeedAbility(uint abilityId)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered action {0}", abilityId);
                if (abilityId == AbilityJudgmentCrystal)
                {
                    Log(State.LogLevelEnum.Debug, null, "Clearing automarkers");
                    _state.ClearAutoMarkers();
                }
            }

            internal void FeedTether(uint actorId)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered tether on {0}", actorId);
                _tethers.Add(actorId);
                if (_tethers.Count == 4 && _fired == false)
                {
                    Party pty = _state.GetPartyMembers();
                    List<Party.PartyMember> unmarked = new List<Party.PartyMember>(
                        from ix in pty.Members where _tethers.Contains(ix.ObjectId) == false select ix
                    );
                    Prio.SortByPriority(unmarked);
                    AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                    ap.Assign(Signs.Roles["Crystal1"], unmarked[0].GameObject);
                    ap.Assign(Signs.Roles["Crystal2"], unmarked[1].GameObject);
                    ap.Assign(Signs.Roles["Crystal3"], unmarked[2].GameObject);
                    ap.Assign(Signs.Roles["Crystal4"], unmarked[3].GameObject);
                    _state.ExecuteAutomarkers(ap, Timing);                    
                    _fired = true;
                }
            }

        }

        #endregion

        #region InceptionAM

        public class InceptionAM : Automarker
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
            private uint _shared = 0;
            private List<uint> _aggro = new List<uint>();
            private List<uint> _rest = new List<uint>();

            public InceptionAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Prio = new AutomarkerPrio();
                Prio.Priority = AutomarkerPrio.PrioTypeEnum.Role;
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Signs.SetRole("SharedSentence", AutomarkerSigns.SignEnum.Circle, false);
                Signs.SetRole("Restraining1", AutomarkerSigns.SignEnum.Ignore1, false);
                Signs.SetRole("Restraining2", AutomarkerSigns.SignEnum.Ignore2, false);
                Signs.SetRole("Aggravated1", AutomarkerSigns.SignEnum.Bind1, false);
                Signs.SetRole("Aggravated2", AutomarkerSigns.SignEnum.Bind2, false);
                Signs.SetRole("Nothing1", AutomarkerSigns.SignEnum.Attack1, false);
                Signs.SetRole("Nothing2", AutomarkerSigns.SignEnum.Attack2, false);
                Signs.SetRole("Nothing3", AutomarkerSigns.SignEnum.Attack3, false);
                Test = new System.Action(() => Signs.TestFunctionality(state, null, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _fired = false;
                _shared = 0;
                _rest.Clear();
                _aggro.Clear();
            }

            private void PerformMarking()
            {
                Log(State.LogLevelEnum.Debug, null, "Ready for automarkers");
                Party pty = _state.GetPartyMembers();
                Party.PartyMember _sharedGo = pty.GetByActorId(_shared);
                List<Party.PartyMember> _restGo = pty.GetByActorIds(_rest);
                List<Party.PartyMember> _aggroGo = pty.GetByActorIds(_aggro);
                List<Party.PartyMember> _unmarkedGo = new List<Party.PartyMember>(
                    from ix in pty.Members
                    where
                        ix != _sharedGo &&
                        _restGo.Contains(ix) == false &&
                        _aggroGo.Contains(ix) == false
                    select ix
                );                
                Prio.SortByPriority(_restGo);
                Prio.SortByPriority(_aggroGo);
                Prio.SortByPriority(_unmarkedGo);
                AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                ap.Assign(Signs.Roles["SharedSentence"], _sharedGo.GameObject);
                ap.Assign(Signs.Roles["Restraining1"], _restGo[0].GameObject);
                ap.Assign(Signs.Roles["Restraining2"], _restGo[1].GameObject);
                ap.Assign(Signs.Roles["Aggravated1"], _aggroGo[0].GameObject);
                ap.Assign(Signs.Roles["Aggravated2"], _aggroGo[1].GameObject);
                ap.Assign(Signs.Roles["Nothing1"], _unmarkedGo[0].GameObject);
                ap.Assign(Signs.Roles["Nothing2"], _unmarkedGo[1].GameObject);
                ap.Assign(Signs.Roles["Nothing3"], _unmarkedGo[2].GameObject);
                _state.ExecuteAutomarkers(ap, Timing);
                _fired = true;
            }

            internal void FeedStatus(uint actorId, uint statusId, bool gained)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered status {0} {1} on {2:X}", statusId, gained, actorId);
                switch (statusId)
                {
                    case StatusSharedSentence:
                    case StatusRestrainingOrder:
                    case StatusAggravatedAssault:
                        if (gained == false && _fired == true)
                        {
                            Log(State.LogLevelEnum.Debug, null, "Clearing automarkers");
                            _state.ClearAutoMarkers();
                            _fired = false;
                        }
                        else if (gained == true && _fired == false)
                        {
                            if (statusId == StatusSharedSentence)
                            {
                                _shared = actorId;
                            }
                            if (statusId == StatusRestrainingOrder)
                            {
                                _rest.Add(actorId);
                            }
                            if (statusId == StatusAggravatedAssault)
                            {
                                _aggro.Add(actorId);
                            }
                            if (_shared > 0 && _rest.Count == 2 && _aggro.Count == 2)
                            {
                                PerformMarking();
                            }
                        }
                        break;
                }
            }

        }

        #endregion

        #region WormholeAM

        public class WormholeAM : LimitCutAM
        {

            public WormholeAM(State state) : base(state)
            {
                Enabled = false;
            }

        }

        #endregion

        #region FinalWordAM

        public class FinalWordAM : Automarker
        {

            [AttributeOrderNumber(1000)]
            public AutomarkerSigns Signs { get; set; }

            [DebugOption]
            [AttributeOrderNumber(2500)]
            public AutomarkerTiming Timing { get; set; }

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public System.Action Test { get; set; }

            private bool _fired = false;
            private uint _lightBeacon = 0;
            private uint _darkBeacon = 0;
            private List<uint> _light = new List<uint>();
            private List<uint> _dark = new List<uint>();

            public FinalWordAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Signs.SetRole("LightBeacon", AutomarkerSigns.SignEnum.Circle, false);
                Signs.SetRole("Light1", AutomarkerSigns.SignEnum.Attack1, false);
                Signs.SetRole("Light2", AutomarkerSigns.SignEnum.Attack2, false);
                Signs.SetRole("Light3", AutomarkerSigns.SignEnum.Attack3, false);
                Signs.SetRole("DarkBeacon", AutomarkerSigns.SignEnum.Triangle, false);
                Signs.SetRole("Dark1", AutomarkerSigns.SignEnum.Bind1, false);
                Signs.SetRole("Dark2", AutomarkerSigns.SignEnum.Bind2, false);
                Signs.SetRole("Dark3", AutomarkerSigns.SignEnum.Bind3, false);
                Test = new System.Action(() => Signs.TestFunctionality(state, null, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _fired = false;
                _lightBeacon = 0;
                _darkBeacon = 0;
                _light.Clear();
                _dark.Clear();
            }

            private void PerformMarking()
            {
                Log(State.LogLevelEnum.Debug, null, "Ready for automarkers");
                Party pty = _state.GetPartyMembers();
                Party.PartyMember _lightBeaconGo = pty.GetByActorId(_lightBeacon);
                Party.PartyMember _darkBeaconGo = pty.GetByActorId(_darkBeacon);
                List<Party.PartyMember> _lightGo = pty.GetByActorIds(_light);
                List<Party.PartyMember> _darkGo = pty.GetByActorIds(_dark);
                AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                ap.Assign(Signs.Roles["LightBeacon"], _lightBeaconGo.GameObject);
                ap.Assign(Signs.Roles["Light1"], _lightGo[0].GameObject);
                ap.Assign(Signs.Roles["Light2"], _lightGo[1].GameObject);
                ap.Assign(Signs.Roles["Light3"], _lightGo[2].GameObject);
                ap.Assign(Signs.Roles["DarkBeacon"], _darkBeaconGo.GameObject);
                ap.Assign(Signs.Roles["Dark1"], _darkGo[0].GameObject);
                ap.Assign(Signs.Roles["Dark2"], _darkGo[1].GameObject);
                ap.Assign(Signs.Roles["Dark3"], _darkGo[2].GameObject);
                _state.ExecuteAutomarkers(ap, Timing);
                _fired = true;
            }

            internal void FeedStatus(uint actorId, uint statusId, bool gained)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered status {0} {1} on {2:X}", statusId, gained, actorId);
                switch (statusId)
                {
                    case StatusFinalWordContactRegulation:
                    case StatusFinalWordContactProhibition:
                    case StatusFinalWordEscapeDetection:
                    case StatusFinalWordEscapeProhibition:
                        if (gained == false && _fired == true)
                        {
                            Log(State.LogLevelEnum.Debug, null, "Clearing automarkers");
                            _state.ClearAutoMarkers();
                            _fired = false;
                        }
                        else if (gained == true && _fired == false)
                        {
                            if (statusId == StatusFinalWordContactRegulation)
                            {
                                _lightBeacon = actorId;
                            }
                            if (statusId == StatusFinalWordContactProhibition)
                            {
                                _light.Add(actorId);
                            }
                            if (statusId == StatusFinalWordEscapeDetection)
                            {
                                _darkBeacon = actorId;
                            }
                            if (statusId == StatusFinalWordEscapeProhibition)
                            {
                                _dark.Add(actorId);
                            }
                            if (_lightBeacon > 0 && _darkBeacon > 0 && _light.Count == 3 && _dark.Count == 3)
                            {
                                PerformMarking();
                            }
                        }
                        break;
                }
            }

        }

        #endregion

        public UltAlexander(State st) : base(st)
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
            _state.OnTether += OnTether;
            _state.OnHeadMarker += OnHeadMarker;
        }

        private void OnHeadMarker(uint dest, uint markerId)
        {
            if (CurrentPhase == PhaseEnum.Start)
            {
                CurrentPhase = PhaseEnum.LimitCut;
            }
            if (_sawFirstHeadMarker == false)
            {
                _sawFirstHeadMarker = true;
                _firstHeadMarker = markerId - 79;
            }
            uint realMarkerId = markerId - _firstHeadMarker;
            if (CurrentPhase == PhaseEnum.LimitCut)
            {
                _limitCutAm.FeedHeadmarker(dest, realMarkerId);
            }
            if (CurrentPhase == PhaseEnum.Wormhole)
            {
                _wormholeAm.FeedHeadmarker(dest, realMarkerId);
            }
        }

        private void OnStatusChange(uint src, uint dest, uint statusId, bool gained, float duration, int stacks)
        {
            switch (statusId)
            {
                case StatusCompressedWater:
                case StatusCompressedLightning:
                    if (CurrentPhase == PhaseEnum.BJCC && gained == true)
                    {
                        _waterningAm.FeedStatus(dest, statusId);
                    }
                    break;
                case StatusRestrainingOrder:
                case StatusHouseArrest:
                case StatusAggravatedAssault:
                case StatusSharedSentence:
                    if (CurrentPhase == PhaseEnum.BJCC)
                    {
                        _temporalAm.FeedStatus(dest, statusId, gained);
                    }
                    if (CurrentPhase == PhaseEnum.Inception)
                    {
                        _inceptionAm.FeedStatus(dest, statusId, gained);
                    }
                    break;
                case StatusFinalWordContactRegulation:
                case StatusFinalWordContactProhibition:
                case StatusFinalWordEscapeDetection:
                case StatusFinalWordEscapeProhibition:
                    if (CurrentPhase == PhaseEnum.FinalWord)
                    {
                        _finalWordAm.FeedStatus(dest, statusId, gained);
                    }
                    break;
            }
        }

        private void OnCastBegin(uint src, uint dest, ushort actionId, float castTime, float rotation)
        {
            if (actionId == AbilityFinalWord)
            {
                CurrentPhase = PhaseEnum.FinalWord;
            }
            if (actionId == AbilityFateProjectionAlpha)
            {
                CurrentPhase = PhaseEnum.FateAlpha;
            }
            if (actionId == AbilityFateProjectionBeta)
            {
                CurrentPhase = PhaseEnum.FateBeta;
            }
        }

        private void OnAction(uint src, uint dest, ushort actionId)
        {
            if (actionId == AbilityAlphaSword || actionId == AbilitySuperBlasstyCharge)
            {
                if (CurrentPhase == PhaseEnum.LimitCut)
                {
                    _limitCutAm.FeedAbility(actionId);
                }
                if (CurrentPhase == PhaseEnum.Wormhole)
                {
                    _wormholeAm.FeedAbility(actionId);
                }
            }
            if (actionId == AbilityJKick && CurrentPhase == PhaseEnum.LimitCut)
            {
                CurrentPhase = PhaseEnum.BJCC;
            }
            if (actionId == AbilityInceptionFormation)
            {
                CurrentPhase = PhaseEnum.Inception;
            }
            if (actionId == AbilityWormholeFormation)
            {
                CurrentPhase = PhaseEnum.Wormhole;
            }
            if (actionId == AbilityGavel && CurrentPhase == PhaseEnum.BJCC)
            {
                _waterningAm.FeedAbility(actionId);
            }
            if (actionId == AbilityJudgmentCrystal && CurrentPhase == PhaseEnum.Inception)
            {
                _crystalAm.FeedAbility(actionId);
            }
        }

        private void OnTether(uint src, uint dest, uint tetherId)
        {
            if (CurrentPhase == PhaseEnum.Inception)
            {
                _crystalAm.FeedTether(dest);
            }
        }

        private void UnsubscribeFromEvents()
        {
            _state.OnHeadMarker -= OnHeadMarker;
            _state.OnTether -= OnTether;
            _state.OnStatusChange -= OnStatusChange;
            _state.OnAction -= OnAction;
            _state.OnCastBegin -= OnCastBegin;
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
            bool newZoneOk = (newZone == 887);
            if (newZoneOk == true && ZoneOk == false)
            {
                Log(State.LogLevelEnum.Info, null, "Content available");
                _sawFirstHeadMarker = false;
                _limitCutAm = (LimitCutAM)Items["LimitCutAM"];
                _waterningAm = (WaterLightningAM)Items["WaterLightningAM"];
                _temporalAm = (TemporalAM)Items["TemporalAM"];
                _crystalAm = (CrystalAM)Items["CrystalAM"];
                _inceptionAm = (InceptionAM)Items["InceptionAM"];
                _wormholeAm = (WormholeAM)Items["WormholeAM"];
                _finalWordAm = (FinalWordAM)Items["FinalWordAM"];
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
