using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using Lemegeton.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static Lemegeton.Core.AutomarkerPrio;
using static Lemegeton.Core.State;

namespace Lemegeton.Content
{

    internal class UltDancingMad : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

        private const int AbilityForsaken = 47804;
        private const int AbilityPathOfLight = 47806;
        private const int AbilityDefinitionOfInsanity = 47842;
        private const int AbilityUltimaBlaster = 47843;
        private const int AbilityUltimaBlaster2 = 47844;
        private const int AbilityMax = 47845;        
        private const int AbilityNothingness = 47868;
        private const int AbilityKefkaSays = 49884;
        private const int AbilityFloodOfNaught1 = 50066;
        private const int AbilityFloodOfNaught2 = 50067;
        private const int AbilityFloodOfNaught3 = 50081;
        private const int AbilityFloodOfNaught4 = 50082;
        private const int AbilityUltimaUpsurge = 49738;
        private const int AbilityUltimaRepeater = 47936;
        private const int AbilityManaRelease = 47781;
        private const int AbilityBlizzardBlowout = 47765;

        private const int HeadmarkerStack = 734;
        private const int HeadmarkerPointblank = 735;
        private const int HeadmarkerCone = 736;
        private const int Headmarker1 = 355;
        private const int Headmarker2 = 356;
        private const int Headmarker3 = 357;
        private const int Headmarker4 = 358;
        private const int Headmarker5 = 456;
        private const int Headmarker6 = 457;
        private const int Headmarker7 = 458;
        private const int Headmarker8 = 459;

        private const int StatusDoubleTroubleTrap = 5078;
        private const int StatusFirstInLine = 3004;
        private const int StatusSecondInLine = 3005;
        private const int StatusThirdInLine = 3006;
        private const int StatusAccretion = 1604;
        private const int StatusThunderCharged = 1485;

        private const int StatusSpecial2 = 2056;
        private const int StatusSpecialChaosFake = 1119;
        private const int StatusSpecialChaosReal = 1120;
        private const int StatusSpecialExdeathFake = 1121;
        private const int StatusSpecialExdeathReal = 1122;
        private const int StatusForkedLightning = 5544; // exdeath      real spread, fake 3 person stack
        private const int StatusAccelerationBomb = 5546; // exdeath     real stop, fake keep moving
        private const int StatusCursedShriek = 5543; // exdeath         real look away, fake look towards
        private const int StatusCompressedWater = 5545; // exdeath      real 3 person stack, fake spread
        private const int StatusDynamicFluid = 5548; // chaos           real donut, fake pbaoe
        private const int StatusEntropy = 5547; // chaos                real pbaoe, fake donut
        private const int StatusBeyondDeath = 5464; // exdeath          real die, fake don't take damage
        private const int StatusAllaganField = 454; // exdeath          real don't take damage, fake die
        private const int StatusBlackWound = 4888; // exdeath           
        private const int StatusWhiteWound = 4887; // exdeath

        private bool ZoneOk = false;
        private bool _subbed = false;
        private bool _sawFirstHeadMarker = false;
        private uint _firstHeadMarker = 0;

        private DoubleTroubleAM _doubleTroubleAm;
        private ForsakenAM _forsakenAm;
        private UltimaBlasterAM _ultimaBlasterAm;
        private BlackHoleAM _blackHoleAM;
        private KefkaSaysAM _kefkaSaysAM;        

        private enum PhaseEnum
        {
            P1_Start,
            P2_Forsaken,
            P3_ExChaos,
            P3_BlackHole,
            P4_KefkaSays,
            P5_UltimaKefka,
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

        #region DoubleTroubleAM

        public class DoubleTroubleAM : Automarker
        {

            public override FeaturesEnum Features => base.Features | FeaturesEnum.Experimental;

            [AttributeOrderNumber(1000)]
            public AutomarkerSigns Signs { get; set; }

            [DebugOption]
            [AttributeOrderNumber(2500)]
            public AutomarkerTiming Timing { get; set; }

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public System.Action Test { get; set; }

            private List<uint> _traps = new List<uint>();
            private bool _fired = false;

            public DoubleTroubleAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Signs.SetRole("Trap1", AutomarkerSigns.SignEnum.Ignore1, false);
                Signs.SetRole("Trap2", AutomarkerSigns.SignEnum.Ignore2, false);
                Test = new System.Action(() => Signs.TestFunctionality(state, null, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _fired = false;
                _traps.Clear();
            }

            internal void FeedStatus(uint dest, bool gained)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1}", gained, dest);
                if (gained == false)
                {
                    if (_fired == true)
                    {
                        Log(State.LogLevelEnum.Debug, null, "Clearing automarkers");
                        Reset();
                        _state.ClearAutoMarkers();
                    }
                }
                else
                {
                    _traps.Add(dest);
                    if (_traps.Count == 2)
                    {
                        Log(State.LogLevelEnum.Debug, null, "Ready for automarkers");
                        Party pty = _state.GetPartyMembers();
                        List<Party.PartyMember> _trapsGo = new List<Party.PartyMember>(
                            from ix in pty.Members join jx in _traps on ix.ObjectId equals jx select ix
                        );
                        AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                        ap.Assign(Signs.Roles["Trap1"], _trapsGo[0].GameObject);
                        ap.Assign(Signs.Roles["Trap2"], _trapsGo[1].GameObject);
                        _fired = true;
                        _state.ExecuteAutomarkers(ap, Timing);
                    }
                }
            }

        }

        #endregion

        #region ForsakenAM

        public class ForsakenAM : Automarker
        {

            public override FeaturesEnum Features => base.Features | FeaturesEnum.Experimental;

            public class StrategyWidget : CustomPropertyInterface
            {

                public override void Deserialize(string data)
                {
                    string[] temp = data.Split(";");
                    foreach (string t in temp)
                    {
                        string[] item = t.Split("=", 2);
                        switch (item[0])
                        {
                            case "Strategy":
                                if (int.TryParse(item[1], out int stratid) == false)
                                {
                                    continue;
                                }
                                _fs._strat = (StratEnum)stratid;
                                break;
                        }
                    }
                }

                public override string Serialize()
                {
                    return string.Format("Strategy={0}", (int)_fs._strat);
                }

                public override void RenderEditor(string path)
                {
                    ImGui.TextWrapped(I18n.Translate(path) + Environment.NewLine + Environment.NewLine);
                    string selname = I18n.Translate(path + "/" + _fs._strat.ToString());
                    if (ImGui.BeginCombo("##" + path, selname) == true)
                    {
                        foreach (ForsakenAM.StratEnum p in Enum.GetValues(typeof(ForsakenAM.StratEnum)))
                        {
                            string name = I18n.Translate(path + "/" + p.ToString());
                            if (ImGui.Selectable(name, String.Compare(name, selname) == 0) == true)
                            {
                                _fs._strat = p;
                            }
                        }
                        ImGui.EndCombo();
                    }
                }

                private ForsakenAM _fs;

                public StrategyWidget(ForsakenAM fs)
                {
                    _fs = fs;
                }

            }

            public enum StratEnum
            {
                AAABBBBA,
                ABBAABBA,
            }

            [AttributeOrderNumber(1000)]
            public StrategyWidget Strategy { get; set; }

            [AttributeOrderNumber(1500)]
            public AutomarkerSigns Signs { get; set; }

            [AttributeOrderNumber(2000)]
            public AutomarkerPrio Prio { get; set; }

            [DebugOption]
            [AttributeOrderNumber(2500)]
            public AutomarkerTiming Timing { get; set; }

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public System.Action Test { get; set; }

            private List<uint> _cones = new List<uint>();
            private List<uint> _pointblanks = new List<uint>();
            private List<uint> _stacks = new List<uint>();
            List<Party.PartyMember> _conesGo = new List<Party.PartyMember>();
            List<Party.PartyMember> _pointblanksGo = new List<Party.PartyMember>();
            List<Party.PartyMember> _stacksGo = new List<Party.PartyMember>();

            internal int CurrentTower
            {
                get
                {
                    return _currentTower;
                }
                set
                {
                    if (_currentTower != value)
                    {
                        _currentTower = value;
                        UpdateTowerSet();
                    }
                }
            }

            private int _currentTower = 0;
            private int _numMarkers = 0;
            private bool _fired = false;
            internal DateTime _lastTowerUpdate = DateTime.MinValue;
            internal StratEnum _strat { get; set; } = StratEnum.AAABBBBA;

            public ForsakenAM(State state) : base(state)
            {
                Enabled = false;
                Strategy = new StrategyWidget(this);
                Signs = new AutomarkerSigns();
                Prio = new AutomarkerPrio() { Priority = AutomarkerPrio.PrioTypeEnum.CongaY };
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Signs.SetRole("InsideCone1", AutomarkerSigns.SignEnum.Bind1, false);
                Signs.SetRole("InsideCone2", AutomarkerSigns.SignEnum.Bind2, false);
                Signs.SetRole("InsideStack1", AutomarkerSigns.SignEnum.Plus, false);
                Signs.SetRole("InsideStack2", AutomarkerSigns.SignEnum.Square, false);
                Signs.SetRole("InsidePb1", AutomarkerSigns.SignEnum.Ignore1, false);
                Signs.SetRole("InsidePb2", AutomarkerSigns.SignEnum.Ignore2, false);
                Signs.SetRole("Outside1", AutomarkerSigns.SignEnum.Attack1, false);
                Signs.SetRole("Outside2", AutomarkerSigns.SignEnum.Attack2, false);
                Signs.SetRole("Outside3", AutomarkerSigns.SignEnum.Attack3, false);
                Signs.SetRole("Outside4", AutomarkerSigns.SignEnum.Attack4, false);
                Test = new System.Action(() => Signs.TestFunctionality(state, Prio, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _cones.Clear();
                _pointblanks.Clear();
                _stacks.Clear();
                _conesGo.Clear();
                _pointblanksGo.Clear();
                _stacksGo.Clear();
                _lastTowerUpdate = DateTime.MinValue;
                _numMarkers = 0;
                _fired = false;
            }

            internal void UpdateTowerSet()
            {
                Log(State.LogLevelEnum.Debug, null, "Updating to tower {0}", _currentTower);
            }

            internal void FeedHeadmarker(uint actorId, uint headMarkerId)
            {
                if (Active == false)
                {
                    return;
                }
                string prevname = "none";
                if (_cones.Contains(actorId))
                {
                    _cones.Remove(actorId);
                    prevname = "cone";
                }
                if (_pointblanks.Contains(actorId))
                {
                    _pointblanks.Remove(actorId);
                    prevname = "pb";
                }
                if (_stacks.Contains(actorId))
                {
                    _stacks.Remove(actorId);
                    prevname = "stack";
                }
                Log(State.LogLevelEnum.Debug, null, "Registered headMarkerId {0} on {1:X}, was {2}", headMarkerId, actorId, prevname);
                switch (headMarkerId)
                {
                    case HeadmarkerCone:
                        _cones.Add(actorId);
                        break;
                    case HeadmarkerPointblank:
                        _pointblanks.Add(actorId);
                        break;
                    case HeadmarkerStack:
                        _stacks.Add(actorId);
                        break;
                }
                switch (_currentTower)
                {
                    case 1:
                        if (_numMarkers < 8)
                        {
                            _numMarkers++;
                            if (_numMarkers == 8)
                            {
                                Log(State.LogLevelEnum.Debug, null, "Full set of {0} received", _numMarkers);
                                _numMarkers = 0;
                                Party pty = _state.GetPartyMembers();
                                _conesGo = pty.GetByActorIds(_cones);
                                _pointblanksGo = pty.GetByActorIds(_pointblanks);
                                _stacksGo = pty.GetByActorIds(_stacks);
                                Prio.SortByPriority(_conesGo);
                                Prio.SortByPriority(_pointblanksGo);
                                Prio.SortByPriority(_stacksGo);
                                BuildTowerset();
                            }
                        }
                        break;
                    default:
                        if (_numMarkers < 4)
                        {
                            _numMarkers++;
                            if (_numMarkers == 4)
                            {
                                Log(State.LogLevelEnum.Debug, null, "Full set of {0} received", _numMarkers);
                                _numMarkers = 0;
                                BuildTowerset();
                            }
                        }
                        break;
                }
            }

            internal void BuildTowerset()
            {
                if (_currentTower > 8)
                {
                    if (_fired == true)
                    {
                        Log(State.LogLevelEnum.Debug, null, "Clearing automarkers");
                        _state.ClearAutoMarkers();
                        _fired = false;
                    }
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Ready for automarkers");
                AutomarkerPayload ap = BuildPayload();
                _state.ExecuteAutomarkers(ap, Timing);
                _fired = true;
            }

            internal AutomarkerPayload BuildPayload()
            {
                AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                if (_currentTower == 1)
                {
                    ap.Assign(Signs.Roles["InsideCone1"], _conesGo[0].GameObject);
                    ap.Assign(Signs.Roles["InsidePb1"], _pointblanksGo[0].GameObject);
                    ap.Assign(Signs.Roles["InsideStack1"], _stacksGo[0].GameObject);
                    ap.Assign(Signs.Roles["InsideStack2"], _stacksGo[1].GameObject);
                    ap.Assign(Signs.Roles["Outside1"], _conesGo[1].GameObject);
                    ap.Assign(Signs.Roles["Outside2"], _conesGo[2].GameObject);
                    ap.Assign(Signs.Roles["Outside3"], _pointblanksGo[1].GameObject);
                    ap.Assign(Signs.Roles["Outside4"], _pointblanksGo[2].GameObject);
                }
                else
                {
                    if (
                        (_strat == StratEnum.AAABBBBA && (_currentTower == 4 || _currentTower == 8))
                        ||
                        (_strat == StratEnum.ABBAABBA && (_currentTower == 2 || _currentTower == 4 || _currentTower == 6 || _currentTower == 8))
                    )
                    {
                        // switch groups
                        if (_currentTower % 2 == 0)
                        {
                            // even (pbs cones)
                            Log(State.LogLevelEnum.Debug, null, "Switch even for {0} {1}", _currentTower);
                            ap.Assign(Signs.Roles["InsideCone1"], _conesGo[0].GameObject);
                            ap.Assign(Signs.Roles["InsideCone2"], _conesGo[1].GameObject);
                            ap.Assign(Signs.Roles["InsidePb1"], _pointblanksGo[0].GameObject);
                            ap.Assign(Signs.Roles["InsidePb2"], _pointblanksGo[1].GameObject);
                            ap.Assign(Signs.Roles["Outside1"], _stacksGo[0].GameObject);
                            ap.Assign(Signs.Roles["Outside2"], _stacksGo[1].GameObject);
                            ap.Assign(Signs.Roles["Outside3"], _conesGo[2].GameObject);
                            ap.Assign(Signs.Roles["Outside4"], _pointblanksGo[2].GameObject);
                        }
                        else
                        {
                            Log(State.LogLevelEnum.Debug, null, "Switch odd for {0} {1}", _currentTower);
                            // odd (stacks pb cone)
                            ap.Assign(Signs.Roles["InsideCone1"], _conesGo[0].GameObject);
                            ap.Assign(Signs.Roles["InsidePb1"], _pointblanksGo[0].GameObject);
                            ap.Assign(Signs.Roles["InsideStack1"], _stacksGo[0].GameObject);
                            ap.Assign(Signs.Roles["InsideStack2"], _stacksGo[1].GameObject);
                            ap.Assign(Signs.Roles["Outside1"], _conesGo[1].GameObject);
                            ap.Assign(Signs.Roles["Outside2"], _conesGo[2].GameObject);
                            ap.Assign(Signs.Roles["Outside3"], _pointblanksGo[1].GameObject);
                            ap.Assign(Signs.Roles["Outside4"], _pointblanksGo[2].GameObject);
                        }
                    }
                    else
                    {
                        // keep groups
                        if (_currentTower % 2 == 0)
                        {
                            Log(State.LogLevelEnum.Debug, null, "Keep even for {0} {1}", _currentTower);
                            // even (pbs cones)
                            ap.Assign(Signs.Roles["InsideCone1"], _conesGo[1].GameObject);
                            ap.Assign(Signs.Roles["InsideCone2"], _conesGo[2].GameObject);
                            ap.Assign(Signs.Roles["InsidePb1"], _pointblanksGo[1].GameObject);
                            ap.Assign(Signs.Roles["InsidePb2"], _pointblanksGo[2].GameObject);
                            ap.Assign(Signs.Roles["Outside1"], _stacksGo[0].GameObject);
                            ap.Assign(Signs.Roles["Outside2"], _stacksGo[1].GameObject);
                            ap.Assign(Signs.Roles["Outside3"], _conesGo[0].GameObject);
                            ap.Assign(Signs.Roles["Outside4"], _pointblanksGo[0].GameObject);
                        }
                        else
                        {
                            Log(State.LogLevelEnum.Debug, null, "Keep even for {0} {1}", _currentTower);
                            // odd (stacks pb cone)
                            ap.Assign(Signs.Roles["InsideCone1"], _conesGo[2].GameObject);
                            ap.Assign(Signs.Roles["InsidePb1"], _pointblanksGo[2].GameObject);
                            ap.Assign(Signs.Roles["InsideStack1"], _stacksGo[0].GameObject);
                            ap.Assign(Signs.Roles["InsideStack2"], _stacksGo[1].GameObject);
                            ap.Assign(Signs.Roles["Outside2"], _conesGo[0].GameObject);
                            ap.Assign(Signs.Roles["Outside1"], _conesGo[1].GameObject);
                            ap.Assign(Signs.Roles["Outside4"], _pointblanksGo[0].GameObject);
                            ap.Assign(Signs.Roles["Outside3"], _pointblanksGo[1].GameObject);
                        }
                    }
                }
                return ap;
            }

        }

        #endregion

        #region UltimaBlasterAM

        public class UltimaBlasterAM : Automarker
        {

            public override FeaturesEnum Features => base.Features | FeaturesEnum.Experimental;

            [AttributeOrderNumber(500)]
            public bool ClockwiseFromNorth { get; set; }

            [AttributeOrderNumber(1000)]
            public AutomarkerSigns Signs { get; set; }

            [DebugOption]
            [AttributeOrderNumber(2000)]
            public AutomarkerTiming Timing { get; set; }

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public System.Action Test { get; set; }

            private float _startAngle = 0.0f;
            private float _nextAngle = 0.0f;
            private int _diveDirection = 0;
            private int _dives = 0;
            private int _markers = 0;
            private int _firstSafeSpot = 0;
            private int _startSpot = 0;
            private uint[] _marked = new uint[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
            private Tuple<float, float, string, int, int>[] pts = new Tuple<float, float, string, int, int>[8];
            private DateTime _lastSeen = DateTime.MinValue;
            private bool _fired = false;

            public UltimaBlasterAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Signs.SetRole("Sign1", AutomarkerSigns.SignEnum.Attack1, false);
                Signs.SetRole("Sign2", AutomarkerSigns.SignEnum.Attack2, false);
                Signs.SetRole("Sign3", AutomarkerSigns.SignEnum.Attack3, false);
                Signs.SetRole("Sign4", AutomarkerSigns.SignEnum.Attack4, false);
                Signs.SetRole("Sign5", AutomarkerSigns.SignEnum.Attack5, false);
                Signs.SetRole("Sign6", AutomarkerSigns.SignEnum.Attack6, false);
                Signs.SetRole("Sign7", AutomarkerSigns.SignEnum.Attack7, false);
                Signs.SetRole("Sign8", AutomarkerSigns.SignEnum.Attack8, false);
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Test = new System.Action(() => TestFunctionality());
                pts[0] = new Tuple<float, float, string, int, int>(85.85786f, 114.1421f, "SW->NE", 8, 1);
                pts[1] = new Tuple<float, float, string, int, int>(100.0f, 120.0f, "S->N", 1, 8);
                pts[2] = new Tuple<float, float, string, int, int>(114.1421f, 114.1421f, "SE->NW", 2, 7);
                pts[3] = new Tuple<float, float, string, int, int>(120.0f, 100.0f, "E->W", 3, 6);
                pts[4] = new Tuple<float, float, string, int, int>(114.1421f, 85.85787f, "NE->SW", 4, 5);
                pts[5] = new Tuple<float, float, string, int, int>(100.0f, 80.0f, "N->S", 5, 4); 
                pts[6] = new Tuple<float, float, string, int, int>(85.85786f, 85.85787f, "NW->SE", 6, 3);
                pts[7] = new Tuple<float, float, string, int, int>(80.0f, 100.0f, "W->E", 7, 2);
            }

            public override void Reset()
            {                
                _diveDirection = 0;
                _dives = 0;
                _markers = 0;
                _lastSeen = DateTime.MinValue;
                _fired = false;
                for (int i = 0; i < 8; i++)
                {
                    _marked[i] = 0;
                }
            }

            public void FeedAction(uint src, ushort actionId)
            {
                if (_diveDirection != 0)
                {
                    return;
                }
                IGameObject go = _state.GetActorById(src);
                if (go == null)
                {
                    return;
                }
                if (actionId == AbilityUltimaBlaster)
                {
                    Log(State.LogLevelEnum.Debug, null, "Registered action {0} from {1} at x {2} y {3}", actionId, go.Name.ToString(), go.Position.X, go.Position.Z);
                    FeedAction(go.Position.X, go.Position.Z);
                }
                if (actionId == AbilityUltimaBlaster2 && _fired == true)
                {
                    Log(State.LogLevelEnum.Debug, null, "Registered action {0} from {1} at x {2} y {3}", actionId, go.Name.ToString(), go.Position.X, go.Position.Z);
                    _state.ClearAutoMarkers();
                    _fired = false;
                }
            }

            public void FeedHeadmarker(uint dest, uint markerId)
            {
                if (Active == false)
                {
                    return;
                }
                switch (markerId)
                {
                    case Headmarker1: _marked[0] = dest; _markers++; break;
                    case Headmarker2: _marked[1] = dest; _markers++; break;
                    case Headmarker3: _marked[2] = dest; _markers++; break;
                    case Headmarker4: _marked[3] = dest; _markers++; break;
                    case Headmarker5: _marked[4] = dest; _markers++; break;
                    case Headmarker6: _marked[5] = dest; _markers++; break;
                    case Headmarker7: _marked[6] = dest; _markers++; break;
                    case Headmarker8: _marked[7] = dest; _markers++; break;
                    default: return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered #{0} headMarkerId {1} on {2:X}", _markers, markerId, dest);
                if (_markers == 8)
                {
                    Log(State.LogLevelEnum.Debug, null, "Ready for automarkers");
                    AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                    Party pty = _state.GetPartyMembers();
                    if (ClockwiseFromNorth == false)
                    {
                        ap.Assign(Signs.Roles["Sign1"], pty.GetByActorId(_marked[0]).GameObject);
                        ap.Assign(Signs.Roles["Sign2"], pty.GetByActorId(_marked[1]).GameObject);
                        ap.Assign(Signs.Roles["Sign3"], pty.GetByActorId(_marked[2]).GameObject);
                        ap.Assign(Signs.Roles["Sign4"], pty.GetByActorId(_marked[3]).GameObject);
                        ap.Assign(Signs.Roles["Sign5"], pty.GetByActorId(_marked[4]).GameObject);
                        ap.Assign(Signs.Roles["Sign6"], pty.GetByActorId(_marked[5]).GameObject);
                        ap.Assign(Signs.Roles["Sign7"], pty.GetByActorId(_marked[6]).GameObject);
                        ap.Assign(Signs.Roles["Sign8"], pty.GetByActorId(_marked[7]).GameObject);
                    }
                    else
                    {                        
                        for (int i = 0; i < 8; i++)
                        {
                            ap.Assign(Signs.Roles["Sign" + (i + 1).ToString()], pty.GetByActorId(_marked[_firstSafeSpot - 1]).GameObject);
                            _firstSafeSpot += _diveDirection == 1 ? 1 : -1;
                            if (_firstSafeSpot < 1)
                            {
                                _firstSafeSpot = 8;
                            }
                            if (_firstSafeSpot > 8)
                            {
                                _firstSafeSpot = 1;
                            }
                        }
                    }
                    _state.ExecuteAutomarkers(ap, Timing);
                    _fired = true;
                }
            }

            public void FeedAction(float x, float y)
            {
                if (_diveDirection != 0)
                {
                    return;
                }
                if (DateTime.Now < _lastSeen.AddSeconds(1))
                {
                    return;
                }
                _lastSeen = DateTime.Now;
                Log(State.LogLevelEnum.Debug, null, "Registered action at x {0} y {1}", x, y);
                if (_dives == 0)
                {
                    _startAngle = (float)Math.Atan2(y - 100.0f, x - 100.0f);
                    Log(State.LogLevelEnum.Debug, null, "Start angle is {0}", _startAngle);
                    for (int i = 0; i < 8; i++)
                    {
                        if (pts[i].Item1 - 1.0 <= x && pts[i].Item1 + 1.0 >= x)
                        {
                            if (pts[i].Item2 - 1.0 <= y && pts[i].Item2 + 1.0 >= y)
                            {
                                Log(State.LogLevelEnum.Debug, null, "Start spot is {0}", pts[i].Item3);
                                _startSpot = i;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    _nextAngle = (float)Math.Atan2(y - 100.0f, x - 100.0f);
                    Log(State.LogLevelEnum.Debug, null, "Next angle is {0}", _nextAngle);
                    if (Math.Atan2(Math.Sin(_nextAngle - _startAngle), Math.Cos(_nextAngle - _startAngle)) < 0.0f)                    
                    {
                        _diveDirection = 1;
                    }
                    else
                    {
                        _diveDirection = 2;
                    }
                    Log(State.LogLevelEnum.Debug, null, "First {0} is {1}", _diveDirection == 1 ? "CW" : "CCW", _diveDirection == 1 ? pts[_startSpot].Item4 : pts[_startSpot].Item5);
                }
                _dives++;
            }

            public void TestFunctionality()
            {
                if (_diveDirection != 0)
                {
                    Reset();
                    return;
                }
                _state.InvokeZoneChange(1363);
                Random r = new Random();
                int i = r.Next(0, 8);
                IGameObject me = _state.ot.LocalPlayer as IGameObject;
                int j = (i + (r.Next(0, 2) == 0 ? 1 : -1)) % 8;
                if (j < 0)
                {
                    j += 8;
                }
                Log(State.LogLevelEnum.Debug, null, "Testing with {0} ({1}) -> {2} ({3})", i, pts[i].Item3, j, pts[j].Item3);
                FeedAction(pts[i].Item1, pts[i].Item2);
                _lastSeen = DateTime.MinValue;
                FeedAction(pts[j].Item1, pts[j].Item2);
            }

        }

        #endregion

        #region BlackHoleAM

        public class BlackHoleAM : Automarker
        {

            public override FeaturesEnum Features => base.Features | FeaturesEnum.Experimental;

            [AttributeOrderNumber(500)]
            public bool MarkOnlyNecessary { get; set; }

            [AttributeOrderNumber(1000)]
            public AutomarkerSigns Signs { get; set; }

            [AttributeOrderNumber(2000)]
            public AutomarkerPrio Prio { get; set; }

            [AttributeOrderNumber(2100)]
            public bool AccretionLast { get; set; }

            [DebugOption]
            [AttributeOrderNumber(2500)]
            public AutomarkerTiming Timing { get; set; }

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public System.Action Test { get; set; }

            private List<uint> _first = new List<uint>();
            private List<uint> _second = new List<uint>();
            private List<uint> _third = new List<uint>();
            private List<uint> _accretion = new List<uint>();
            List<Party.PartyMember> _firstGo = new List<Party.PartyMember>();
            List<Party.PartyMember> _secondGo = new List<Party.PartyMember>();
            List<Party.PartyMember> _thirdGo = new List<Party.PartyMember>();

            internal int CurrentSet
            {
                get
                {
                    return _currentSet;
                }
                set
                {
                    if (_currentSet != value)
                    {
                        _currentSet = value;
                        UpdateBhSet();
                    }
                }
            }

            private int _currentSet = 0;
            private int _numMarkers = 0;
            private bool _fired = false;
            internal DateTime _lastBhUpdate = DateTime.MinValue;

            public BlackHoleAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Prio = new AutomarkerPrio() { Priority = AutomarkerPrio.PrioTypeEnum.Role };
                Prio._prioByRole.Clear();
                Prio._prioByRole.AddRange(new AutomarkerPrio.PrioRoleEnum[] { PrioRoleEnum.Melee, PrioRoleEnum.Ranged, PrioRoleEnum.Caster, PrioRoleEnum.Tank, PrioRoleEnum.Healer });
                AccretionLast = true;
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Signs.SetRole("First1", AutomarkerSigns.SignEnum.Attack1, false);
                Signs.SetRole("First2", AutomarkerSigns.SignEnum.Attack2, false);
                Signs.SetRole("First3", AutomarkerSigns.SignEnum.Attack3, false);
                Signs.SetRole("Second1", AutomarkerSigns.SignEnum.Bind1, false);
                Signs.SetRole("Second2", AutomarkerSigns.SignEnum.Bind2, false);
                Signs.SetRole("Second3", AutomarkerSigns.SignEnum.Bind3, false);
                Signs.SetRole("Third1", AutomarkerSigns.SignEnum.Ignore1, false);
                Signs.SetRole("Third2", AutomarkerSigns.SignEnum.Ignore2, false);
                Test = new System.Action(() => Signs.TestFunctionality(state, Prio, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _first.Clear();
                _second.Clear();
                _third.Clear();
                _firstGo.Clear();
                _secondGo.Clear();
                _thirdGo.Clear();
                _accretion.Clear();
                _lastBhUpdate = DateTime.MinValue;
                _numMarkers = 0;
                _fired = false;
            }

            private void UpdateBhSet()
            {
                Log(State.LogLevelEnum.Debug, null, "Updating to set {0}", _currentSet);
                if (_currentSet == 11 && _fired == true)
                {
                    _state.ClearAutoMarkers();
                }
                else if (MarkOnlyNecessary == true)
                {                 
                    MarkSet();
                }
            }

            internal void MarkSet()
            {
                AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                switch (_currentSet)
                {
                    case 1:
                        ap.Assign(Signs.Roles["First1"], _firstGo[0].GameObject);
                        break;
                    case 2:
                        ap.Assign(Signs.Roles["First1"], _firstGo[0].GameObject);
                        ap.Assign(Signs.Roles["First2"], _firstGo[1].GameObject);
                        break;
                    case 3:
                        ap.Assign(Signs.Roles["First1"], _firstGo[0].GameObject);
                        ap.Assign(Signs.Roles["First2"], _firstGo[1].GameObject);
                        ap.Assign(Signs.Roles["First3"], _firstGo[2].GameObject);
                        break;
                    case 4:
                        ap.Assign(Signs.Roles["First1"], _secondGo[0].GameObject);
                        ap.Assign(Signs.Roles["First2"], _firstGo[1].GameObject);
                        ap.Assign(Signs.Roles["First3"], _firstGo[2].GameObject);
                        break;
                    case 5:
                        ap.Assign(Signs.Roles["First1"], _secondGo[0].GameObject);
                        ap.Assign(Signs.Roles["First2"], _secondGo[1].GameObject);
                        ap.Assign(Signs.Roles["First3"], _firstGo[2].GameObject);
                        break;
                    case 6:
                        ap.Assign(Signs.Roles["First1"], _secondGo[0].GameObject);
                        ap.Assign(Signs.Roles["First2"], _secondGo[1].GameObject);
                        ap.Assign(Signs.Roles["First3"], _secondGo[2].GameObject);
                        break;
                    case 7:
                        ap.Assign(Signs.Roles["First1"], _thirdGo[0].GameObject);
                        ap.Assign(Signs.Roles["First2"], _secondGo[1].GameObject);
                        ap.Assign(Signs.Roles["First3"], _secondGo[2].GameObject);
                        break;
                    case 8:
                        ap.Assign(Signs.Roles["First1"], _thirdGo[0].GameObject);
                        ap.Assign(Signs.Roles["First2"], _thirdGo[1].GameObject);
                        ap.Assign(Signs.Roles["First3"], _secondGo[2].GameObject);
                        break;
                    case 9:
                        ap.Assign(Signs.Roles["First1"], _thirdGo[0].GameObject);
                        ap.Assign(Signs.Roles["First2"], _thirdGo[1].GameObject);
                        ap.Assign(AutomarkerSigns.SignEnum.None, _secondGo[2].GameObject);
                        break;
                    case 10:
                        ap.Assign(Signs.Roles["First1"], _thirdGo[1].GameObject);
                        ap.Assign(AutomarkerSigns.SignEnum.None, _thirdGo[0].GameObject);                        
                        break;
                }
                _fired = true;
                _state.ExecuteAutomarkers(ap, Timing);
            }

            internal void FeedStatus(uint statusId, uint dest)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1}", statusId, dest);
                switch (statusId)
                {
                    case StatusFirstInLine:
                        _first.Add(dest);
                        break;
                    case StatusSecondInLine:
                        _second.Add(dest);
                        break;
                    case StatusThirdInLine:
                        _third.Add(dest);
                        break;
                    case StatusAccretion:
                        _accretion.Add(dest);
                        break;
                }
                _numMarkers++;
                if (_numMarkers == 10)
                {
                    Log(State.LogLevelEnum.Debug, null, "Ready for automarkers, updating to set 1");
                    _currentSet = 1;
                    Party pty = _state.GetPartyMembers();
                    _firstGo = pty.GetByActorIds(_first);
                    _secondGo = pty.GetByActorIds(_second);
                    _thirdGo = pty.GetByActorIds(_third);
                    Prio.SortByPriority(_firstGo);
                    Prio.SortByPriority(_secondGo);
                    Prio.SortByPriority(_thirdGo);
                    if (AccretionLast == true)
                    {
                        var px = (from gx in _firstGo where _accretion.Contains((uint)gx.ObjectId) == true select gx).FirstOrDefault();
                        if (px != null)
                        {
                            _firstGo.Remove(px);
                            _firstGo.Add(px);
                        }
                        px = (from gx in _secondGo where _accretion.Contains((uint)gx.ObjectId) == true select gx).FirstOrDefault();
                        if (px != null)
                        {
                            _secondGo.Remove(px);
                            _secondGo.Add(px);
                        }
                        px = (from gx in _thirdGo where _accretion.Contains((uint)gx.ObjectId) == true select gx).FirstOrDefault();
                        if (px != null)
                        {
                            _thirdGo.Remove(px);
                            _thirdGo.Add(px);
                        }
                    }
                    if (MarkOnlyNecessary == true)
                    {
                        MarkSet();
                    }
                    else
                    {
                        AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                        ap.Assign(Signs.Roles["First1"], _firstGo[0].GameObject);
                        ap.Assign(Signs.Roles["First2"], _firstGo[1].GameObject);
                        ap.Assign(Signs.Roles["First3"], _firstGo[2].GameObject);
                        ap.Assign(Signs.Roles["Second1"], _secondGo[0].GameObject);
                        ap.Assign(Signs.Roles["Second2"], _secondGo[1].GameObject);
                        ap.Assign(Signs.Roles["Second3"], _secondGo[2].GameObject);
                        ap.Assign(Signs.Roles["Third1"], _thirdGo[0].GameObject);
                        ap.Assign(Signs.Roles["Third2"], _thirdGo[1].GameObject);
                        _fired = true;
                        _state.ExecuteAutomarkers(ap, Timing);
                    }
                }
            }

        }

        #endregion

        #region KefkaSaysAM

        public class KefkaSaysAM : Automarker
        {

            public override FeaturesEnum Features => base.Features | FeaturesEnum.Experimental;

            private bool _exdeathReal = true;
            private bool _chaosReal = true;

            private List<Tuple<IGameObject, DateTime>> _spreads = new List<Tuple<IGameObject, DateTime>>();
            private List<Tuple<IGameObject, DateTime, bool>> _gazes = new List<Tuple<IGameObject, DateTime, bool>>();
            private List<Tuple<bool, DateTime>> _fireWater = new List<Tuple<bool, DateTime>>();
            private int _currentSet = 0;
            public int CurrentSet
            {
                get
                {
                    return _currentSet;
                }
                set
                {
                    if (_currentSet != value)
                    {
                        _currentSet = value;
                        Log(State.LogLevelEnum.Debug, null, "Current set is {0}", _currentSet);
                        PerformMarking();
                    }
                }
            }

            [AttributeOrderNumber(1000)]
            public AutomarkerSigns Signs1 { get; set; }

            [AttributeOrderNumber(1100)]
            public AutomarkerSigns Signs2 { get; set; }

            [AttributeOrderNumber(1200)]
            public AutomarkerSigns Signs3 { get; set; }

            [DebugOption]
            [AttributeOrderNumber(1500)]
            public AutomarkerTiming Timing { get; set; }

            [DebugOption]
            [AttributeOrderNumber(2000)]
            public System.Action Test { get; set; }

            public KefkaSaysAM(State state) : base(state)
            {
                Enabled = false;
                Signs1 = new AutomarkerSigns();
                Signs2 = new AutomarkerSigns();
                Signs3 = new AutomarkerSigns();
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Test = new System.Action(() => TestFunctionality());                
                Signs1.SetRole("Stack1", AutomarkerSigns.SignEnum.Attack1, false);
                Signs1.SetRole("Stack2", AutomarkerSigns.SignEnum.Attack2, false);
                Signs1.SetRole("Stack3", AutomarkerSigns.SignEnum.Attack3, false);
                Signs1.SetRole("Stack4", AutomarkerSigns.SignEnum.Attack4, false);
                Signs1.SetRole("Stack5", AutomarkerSigns.SignEnum.Attack5, false);
                Signs1.SetRole("Stack6", AutomarkerSigns.SignEnum.Attack6, false);
                Signs1.SetRole("Forked1", AutomarkerSigns.SignEnum.Ignore1, false);
                Signs1.SetRole("Forked2", AutomarkerSigns.SignEnum.Ignore2, false);
                Signs2.SetRole("LookAt1", AutomarkerSigns.SignEnum.Bind1, false);
                Signs2.SetRole("LookAt2", AutomarkerSigns.SignEnum.Bind2, false);
                Signs2.SetRole("LookAway1", AutomarkerSigns.SignEnum.Ignore1, false);
                Signs2.SetRole("LookAway2", AutomarkerSigns.SignEnum.Ignore2, false);
                Signs3.SetRole("Donut", AutomarkerSigns.SignEnum.Circle, false);
                Signs3.SetRole("Twister", AutomarkerSigns.SignEnum.Plus, false);
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _spreads.Clear();
                _gazes.Clear();
                _fireWater.Clear();
                _currentSet = 0;
            }

            public void TestFunctionality()
            {
                _state.InvokeZoneChange(1363);
                if (_spreads.Count > 0)
                {
                    Reset();
                }
                else
                {
                    Random r = new Random();
                    FeedRealFake((uint)(r.Next(2) == 0 ? StatusSpecialChaosFake : StatusSpecialChaosReal));
                    FeedRealFake((uint)(r.Next(2) == 0 ? StatusSpecialExdeathFake : StatusSpecialExdeathReal));
                    IGameObject me = _state.ot.LocalPlayer as IGameObject;
                    FeedStatus((uint)me.GameObjectId, StatusCompressedWater, 10.0f, true);
                    FeedStatus((uint)me.GameObjectId, StatusForkedLightning, 10.0f, true);
                    FeedStatus((uint)me.GameObjectId, StatusDynamicFluid, 15.0f, true);
                    FeedStatus((uint)me.GameObjectId, StatusEntropy, 20.0f, true);
                    FeedStatus((uint)me.GameObjectId, StatusAccelerationBomb, 25.0f, true);
                    FeedStatus((uint)me.GameObjectId, StatusBeyondDeath, 30.0f, true);
                    FeedStatus((uint)me.GameObjectId, StatusAllaganField, 30.0f, true);
                    FeedStatus((uint)me.GameObjectId, StatusCursedShriek, 35.0f, true);
                    FeedStatus((uint)me.GameObjectId, StatusCursedShriek, 35.0f, true);
                    FeedRealFake((uint)(r.Next(2) == 0 ? StatusSpecialExdeathFake : StatusSpecialExdeathReal));
                    FeedStatus((uint)me.GameObjectId, StatusCursedShriek, 55.0f, true);
                }
            }

            internal void FeedRealFake(uint id)
            {
                if (Active == false)
                {
                    return;
                }
                switch (id)
                {
                    case StatusSpecialChaosFake:
                        Log(LogLevelEnum.Debug, null, "Chaos fake");
                        _chaosReal = false;
                        break;
                    case StatusSpecialChaosReal:
                        Log(LogLevelEnum.Debug, null, "Chaos real");
                        _chaosReal = true;
                        break;
                    case StatusSpecialExdeathFake:
                        Log(LogLevelEnum.Debug, null, "Exdeath fake");
                        _exdeathReal = false;
                        break;
                    case StatusSpecialExdeathReal:
                        Log(LogLevelEnum.Debug, null, "Exdeath real");
                        _exdeathReal = true;
                        break;
                }
            }

            internal void FeedStatus(uint dest, uint statusId, float duration, bool gained)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered status {0} {1} on {2} for {3} s", statusId, gained, dest, duration);
                IGameObject de = _state.GetActorById(dest);
                switch (statusId)
                {
                    case StatusForkedLightning: // exdeath
                        if (gained == true)
                        {
                            if (_exdeathReal == true)
                            {
                                _spreads.Add(new Tuple<IGameObject, DateTime>(de, DateTime.Now.AddSeconds(duration)));
                            }
                        }
                        else
                        {
                            if (CurrentSet == 1 || CurrentSet == 7)
                            {
                                AdvanceSet();
                            }
                        }
                        break;
                    case StatusCompressedWater: // exdeath
                        if (_exdeathReal == false)
                        {
                            _spreads.Add(new Tuple<IGameObject, DateTime>(de, DateTime.Now.AddSeconds(duration)));
                        }
                        break;
                    case StatusDynamicFluid: // chaos
                        _fireWater.Add(new Tuple<bool, DateTime>(_chaosReal, DateTime.Now.AddSeconds(duration)));
                        break;
                    case StatusEntropy: // chaos
                        _fireWater.Add(new Tuple<bool, DateTime>(_chaosReal == false, DateTime.Now.AddSeconds(duration)));
                        break;
                    case StatusCursedShriek: // exdeath
                        if (gained == true)
                        {
                            _gazes.Add(new Tuple<IGameObject, DateTime, bool>(de, DateTime.Now.AddSeconds(duration), _exdeathReal));
                        }
                        else
                        {
                            if (CurrentSet == 3 || CurrentSet == 10)
                            {
                                AdvanceSet();
                            }
                        }
                        break;
                    case StatusThunderCharged:
                        AdvanceSet();
                        break;
                }
            }

            public void AdvanceSet()
            {
                CurrentSet++;
                PerformMarking();
            }

            private void PerformMarking()
            {
                AutomarkerPayload ap = null; 
                switch (_currentSet)
                {
                    case 1: // early spreads and stacks (flood of naught)
                        {
                            ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                            var spreads = (from ix in _spreads orderby ix.Item2 ascending select ix.Item1).Take(2).ToList();
                            Party pty = _state.GetPartyMembers();
                            List<Party.PartyMember> stacks = new List<Party.PartyMember>(
                                from ix in pty.Members where spreads.Contains(ix.GameObject) == false select ix
                            );
                            ap.Assign(Signs1.Roles["Forked1"], spreads[0]);
                            ap.Assign(Signs1.Roles["Forked2"], spreads[1]);
                            ap.Assign(Signs1.Roles["Stack1"], stacks[0]);
                            ap.Assign(Signs1.Roles["Stack2"], stacks[1]);
                            ap.Assign(Signs1.Roles["Stack3"], stacks[2]);
                            ap.Assign(Signs1.Roles["Stack4"], stacks[3]);
                            ap.Assign(Signs1.Roles["Stack5"], stacks[4]);
                            ap.Assign(Signs1.Roles["Stack6"], stacks[5]);
                        }
                        break;
                    case 2: // forked lightning lost
                        _state.ClearAutoMarkers();
                        break;
                    case 3: // early gaze (thunder charged)
                        {
                            ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                            var gazes = (from ix in _gazes orderby ix.Item2 ascending select ix).Take(2).ToList();
                            if (gazes[0].Item3 == true)
                            {
                                ap.Assign(Signs2.Roles["LookAt1"], gazes[0].Item1);
                                ap.Assign(Signs2.Roles["LookAt2"], gazes[1].Item1);
                            }
                            else
                            {
                                ap.Assign(Signs2.Roles["LookAway1"], gazes[0].Item1);
                                ap.Assign(Signs2.Roles["LookAway2"], gazes[1].Item1);
                            }
                        }
                        break;
                    case 4: // cursed shriek lost
                        _state.ClearAutoMarkers();
                        break;
                    case 5: // early firewater (ultima upsurge begin)
                        {
                            ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                            var donut = (from ix in _fireWater orderby ix.Item2 ascending select ix).Take(1).ToList();
                            Party pty = _state.GetPartyMembers();
                            if (donut[0].Item1 == true)
                            {
                                ap.Assign(Signs3.Roles["Donut"], pty.Members[0].GameObject);
                            }
                            else
                            {
                                ap.Assign(Signs3.Roles["Twister"], pty.Members[0].GameObject);
                            }
                        }
                        break;
                    case 6: // ultima upsurge hit
                        _state.ClearAutoMarkers();
                        break;
                    case 7: // late spreads and stacks (blizzard iii blowout)
                        {
                            ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                            var spreads = (from ix in _spreads orderby ix.Item2 descending select ix.Item1).Take(2).ToList();
                            Party pty = _state.GetPartyMembers();
                            List<Party.PartyMember> stacks = new List<Party.PartyMember>(
                                from ix in pty.Members where spreads.Contains(ix.GameObject) == false select ix
                            );
                            ap.Assign(Signs1.Roles["Forked1"], spreads[0]);
                            ap.Assign(Signs1.Roles["Forked2"], spreads[1]);
                            ap.Assign(Signs1.Roles["Stack1"], stacks[0]);
                            ap.Assign(Signs1.Roles["Stack2"], stacks[1]);
                            ap.Assign(Signs1.Roles["Stack3"], stacks[2]);
                            ap.Assign(Signs1.Roles["Stack4"], stacks[3]);
                            ap.Assign(Signs1.Roles["Stack5"], stacks[4]);
                            ap.Assign(Signs1.Roles["Stack6"], stacks[5]);
                        }
                        break;
                    case 8: // forked lightning lost
                        _state.ClearAutoMarkers();
                        break;
                    case 9: // late gaze (blizzard iii blowout)
                        {
                            ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                            var gazes = (from ix in _gazes orderby ix.Item2 descending select ix).Take(2).ToList();
                            if (gazes[0].Item3 == true)
                            {
                                ap.Assign(Signs2.Roles["LookAt1"], gazes[0].Item1);
                                ap.Assign(Signs2.Roles["LookAt2"], gazes[1].Item1);
                            }
                            else
                            {
                                ap.Assign(Signs2.Roles["LookAway1"], gazes[0].Item1);
                                ap.Assign(Signs2.Roles["LookAway2"], gazes[1].Item1);
                            }
                        }
                        break;
                    case 10: // mana release
                        _state.ClearAutoMarkers();
                        break;
                    case 11: // late firewater (cursed shriek lost)
                        {
                            ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                            var donut = (from ix in _fireWater orderby ix.Item2 descending select ix).Take(1).ToList();
                            Party pty = _state.GetPartyMembers();
                            if (donut[0].Item1 == true)
                            {
                                ap.Assign(Signs3.Roles["Donut"], pty.Members[0].GameObject);
                            }
                            else
                            {
                                ap.Assign(Signs3.Roles["Twister"], pty.Members[0].GameObject);
                            }
                        }
                        break;
                    case 12: // ultima upsurge
                        _state.ClearAutoMarkers();
                        break;
                }
                if (ap != null)
                {
                    _state.ExecuteAutomarkers(ap, Timing);
                }
            }

        }

        #endregion

        public UltDancingMad(State st) : base(st)
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
            lock (this)
            {
                if (_subbed == true)
                {
                    return;
                }
                _subbed = true;
                Log(LogLevelEnum.Debug, null, "Subscribing to events");
                _state.OnCastBegin += OnCastBegin;
                _state.OnHeadMarker += OnHeadMarker;
                _state.OnAction += OnAction;
                _state.OnStatusChange += OnStatusChange;
            }
        }

        private void OnAction(uint src, uint dest, ushort actionId)
        {
            switch (CurrentPhase)
            {
                case PhaseEnum.P2_Forsaken:
                    if (actionId == AbilityPathOfLight)
                    {
                        if (DateTime.Now.AddSeconds(-5) > _forsakenAm._lastTowerUpdate)
                        {
                            _forsakenAm._lastTowerUpdate = DateTime.Now;
                            _forsakenAm.CurrentTower++;
                        }
                    }
                    break;
                case PhaseEnum.P3_ExChaos:
                    if (actionId == AbilityUltimaBlaster || actionId == AbilityUltimaBlaster2)
                    {
                        _ultimaBlasterAm.FeedAction(src, actionId);
                    }
                    break;
                case PhaseEnum.P3_BlackHole:
                    if (actionId == AbilityNothingness)
                    {
                        if (DateTime.Now.AddSeconds(-3) > _blackHoleAM._lastBhUpdate)
                        {
                            _blackHoleAM._lastBhUpdate = DateTime.Now;
                            _blackHoleAM.CurrentSet++;
                        }
                    }
                    break;
                case PhaseEnum.P4_KefkaSays:
                    if (actionId == AbilityUltimaUpsurge && _kefkaSaysAM.CurrentSet == 5)
                    {
                        _kefkaSaysAM.AdvanceSet();
                    }
                    break;
            }
        }

        private void OnHeadMarker(uint dest, uint markerId)
        {
            if (_sawFirstHeadMarker == false)
            {
                _sawFirstHeadMarker = true;
                _firstHeadMarker = markerId - 237;
            }
            uint realMarkerId = markerId - _firstHeadMarker;
            Log(State.LogLevelEnum.Info, null, "Marker {0} offset {1} Real marker id {2}", markerId, _firstHeadMarker, realMarkerId);
            if (CurrentPhase == PhaseEnum.P2_Forsaken)
            {
                _forsakenAm.FeedHeadmarker(dest, realMarkerId);
            }
            if (CurrentPhase == PhaseEnum.P3_ExChaos)
            {
                _ultimaBlasterAm.FeedHeadmarker(dest, realMarkerId);
            }
        }

        private void OnCastBegin(uint src, uint dest, ushort actionId, float castTime, float rotation)
        {
            switch (actionId)
            {
                case AbilityForsaken:
                    CurrentPhase = PhaseEnum.P2_Forsaken;
                    _forsakenAm.CurrentTower = 1;
                    break;
                case AbilityDefinitionOfInsanity:
                    CurrentPhase = PhaseEnum.P3_ExChaos;
                    break;
                case AbilityMax:
                    CurrentPhase = PhaseEnum.P3_BlackHole;
                    break;
                case AbilityKefkaSays:
                    CurrentPhase = PhaseEnum.P4_KefkaSays;
                    _kefkaSaysAM.CurrentSet = 1;
                    break;
                case AbilityFloodOfNaught1:
                case AbilityFloodOfNaught2:
                case AbilityFloodOfNaught3:
                case AbilityFloodOfNaught4:
                case AbilityUltimaUpsurge:
                case AbilityManaRelease:
                case AbilityBlizzardBlowout:
                    if (CurrentPhase == PhaseEnum.P4_KefkaSays)
                    {
                        _kefkaSaysAM.AdvanceSet();
                    }
                    break;
                case AbilityUltimaRepeater:
                    if (CurrentPhase != PhaseEnum.P5_UltimaKefka)
                    {
                        CurrentPhase = PhaseEnum.P5_UltimaKefka;
                    }
                    break;
            }
        }

        private void OnStatusChange(uint src, uint dest, uint statusId, bool gained, float duration, int stacks)
        {
            switch (statusId)
            {
                case StatusDoubleTroubleTrap:
                    if (CurrentPhase == PhaseEnum.P1_Start)
                    {
                        _doubleTroubleAm.FeedStatus(dest, gained);
                    }
                    break;
                case StatusFirstInLine:
                case StatusSecondInLine:
                case StatusThirdInLine:
                case StatusAccretion:
                    if (CurrentPhase == PhaseEnum.P3_BlackHole && gained == true)
                    {
                        _blackHoleAM.FeedStatus(statusId, dest);
                    }
                    break;
                case StatusSpecial2:
                    if (CurrentPhase == PhaseEnum.P4_KefkaSays && gained == true)
                    {
                        if (stacks == StatusSpecialExdeathFake || stacks == StatusSpecialExdeathReal || stacks == StatusSpecialChaosFake || stacks == StatusSpecialChaosReal)
                        {
                            _kefkaSaysAM.FeedRealFake((uint)stacks);
                        }
                    }
                    break;
                case StatusAccelerationBomb:
                case StatusCompressedWater:
                case StatusDynamicFluid:
                case StatusEntropy:
                case StatusBeyondDeath:
                case StatusAllaganField:
                case StatusBlackWound:
                case StatusWhiteWound:
                case StatusThunderCharged:
                    if (CurrentPhase == PhaseEnum.P4_KefkaSays && gained == true)
                    {
                        _kefkaSaysAM.FeedStatus(dest, statusId, duration, gained);
                    }
                    break;
                case StatusForkedLightning:
                case StatusCursedShriek:
                    if (CurrentPhase == PhaseEnum.P4_KefkaSays)
                    {
                        _kefkaSaysAM.FeedStatus(dest, statusId, duration, gained);
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
                _state.OnAction -= OnAction;
                _state.OnHeadMarker -= OnHeadMarker;
                _state.OnCastBegin -= OnCastBegin;
                _state.OnStatusChange -= OnStatusChange;
                _subbed = false;
            }
        }

        private void OnCombatChange(bool inCombat)
        {
            Reset();
            if (inCombat == true)
            {
                CurrentPhase = PhaseEnum.P1_Start;
                _forsakenAm.CurrentTower = 0;
                SubscribeToEvents();
            }
            else
            {
                UnsubscribeFromEvents();
            }
        }

        private void OnZoneChange(uint newZone)
        {
            // normal modes included for some easier testing
            bool newZoneOk = (newZone == 1363);
            if (newZoneOk == true && ZoneOk == false)
            {
                Log(State.LogLevelEnum.Info, null, "Content available");
                _sawFirstHeadMarker = false;
                _doubleTroubleAm = (DoubleTroubleAM)Items["DoubleTroubleAM"];
                _forsakenAm = (ForsakenAM)Items["ForsakenAM"];                
                _ultimaBlasterAm = (UltimaBlasterAM)Items["UltimaBlasterAM"];
                _blackHoleAM = (BlackHoleAM)Items["BlackHoleAM"];
                _kefkaSaysAM = (KefkaSaysAM)Items["KefkaSaysAM"];
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
