using System;
using Dalamud.Game.ClientState.Objects.Types;
using Lemegeton.Core;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using ImGuiNET;
using CharacterStruct = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using GameObjectStruct = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using GameObjectPtr = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using System.Collections;

namespace Lemegeton.Content
{

    internal class UltOmegaProtocol : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

        private const int AbilityProgramLoop = 31491;
        private const int AbilityPantokrator = 31499;
        private const int AbilityBossMonitorEast = 31595;
        private const int AbilityBossMonitorWest = 31596;
        private const int AbilityBossMonitorDeltaLeft = 31639;
        private const int AbilityBossMonitorDeltaRight = 31638;
        private const int AbilityDynamisDelta = 31624;
        private const int AbilityDynamisSigma = 32788;
        private const int AbilityDynamisOmega = 32789;
        private const int AbilityHelloDistantWorldBig = 33040;
        private const int AbilityLaserShower = 0x7B45;

        private const int StatusDistantWorld = 3443;
        private const int StatusNearWorld = 3442;
        private const int StatusDynamis = 3444;
        private const int StatusInLine1 = 3004;
        private const int StatusInLine2 = 3005;
        private const int StatusInLine3 = 3006;
        private const int StatusInLine4 = 3451;
        private const uint StatusMidGlitch = 3427;
        private const uint StatusRemoteGlitch = 3428;
        private const uint StatusStackSniper = 3426;
        private const uint StatusSpreadSniper = 3425;
        private const uint StatusMonitorLeft = 0xD7D;
        private const uint StatusMonitorRight = 0xD7C;

        private const int HeadmarkerCircle = 416;
        private const int HeadmarkerSquare = 418;
        private const int HeadmarkerCross = 419;
        private const int HeadmarkerTriangle = 417;
        private const int HeadmarkerTarget = 244;

        private bool ZoneOk = false;
        private bool _sawFirstHeadMarker = false;
        private uint _firstHeadMarker = 0;

#if !SANS_GOETIA
        private ChibiOmega _chibiOmega;
        private GlitchTether _glitchTether;
        private HelloWorldDrawBossMonitor _hwMonitor;
        private DynamisDeltaDrawBossMonitor _deltaMonitor;
        private DynamisOmegaDrawBossMonitor _omegaMonitor;
#endif

        private ProgramLoopAM _loopAm;
        private PantokratorAM _pantoAm;
        private P3TransitionAM _p3transAm;
        private P3MonitorAM _p3moniAm;
        private DynamisDeltaAM _deltaAm;
        private DynamisSigmaAM _sigmaAm;
        private DynamisOmegaAM _omegaAm;

        private enum PhaseEnum
        {
            P1_Start,
            P1_ProgramLoop,
            P1_Pantokrator,
            P3_Transition,
            P5_Delta,
            P5_Sigma,
            P5_Omega,
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

#if !SANS_GOETIA

        #region ChibiOmega

        public class ChibiOmega : Core.ContentItem
        {

            public override FeaturesEnum Features => FeaturesEnum.Hack;

            [AttributeOrderNumber(1000)]
            public bool ApplyP1 { get; set; } = true;
            [AttributeOrderNumber(1001)]
            public Percentage SizeP1 { get; set; }

            [AttributeOrderNumber(2000)]
            public bool ApplyP3 { get; set; } = true;
            [AttributeOrderNumber(2001)]
            public Percentage SizeP3 { get; set; }

            private bool _lookingForOmega = false;
            private DateTime _omegaSearchStartTime;
            private DateTime _omegaFoundAt;
            private DateTime _giveUpAt;

            public ChibiOmega(State state) : base(state)
            {
                SizeP1 = new Percentage() { MinValue = 10.0f, MaxValue = 100.0f, CurrentValue = 100.0f };
                SizeP3 = new Percentage() { MinValue = 10.0f, MaxValue = 100.0f, CurrentValue = 100.0f };
                Enabled = false;
            }

            public void StartLooking(float giveUp)
            {
                Log(State.LogLevelEnum.Debug, null, "Looking for Omega for {0} secs", giveUp);
                _omegaSearchStartTime = DateTime.Now;
                _giveUpAt = _omegaSearchStartTime.AddSeconds(giveUp);
                _omegaFoundAt = DateTime.MinValue;
                _lookingForOmega = true;
            }

            public void StopLooking()
            {
                if (_lookingForOmega == true)
                {
                    Log(State.LogLevelEnum.Debug, null, "Not looking for Omega anymore");
                    _lookingForOmega = false;
                }
            }

            protected override bool ExecutionImplementation()
            {
                if (_lookingForOmega == true)
                {
                    LookForOmega();
                }
                return true;
            }

            public unsafe void LookForOmega()
            {
                if (_lookingForOmega == true && _omegaFoundAt == DateTime.MinValue && DateTime.Now > _giveUpAt)
                {
                    Log(State.LogLevelEnum.Debug, null, "Couldn't find Omega in time, giving up");
                    StopLooking();
                    return;
                }
                if (_omegaFoundAt != DateTime.MinValue && DateTime.Now > _omegaFoundAt.AddSeconds(3.0f))
                {
                    Log(State.LogLevelEnum.Debug, null, "Done smollening Omega");
                    StopLooking();
                    return;
                }
                foreach (GameObject go in _state.ot)
                {
                    if (go.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc && go is Character)
                    {
                        Character bc = (Character)go;
                        CharacterStruct* bcs = (CharacterStruct*)bc.Address;
                        if (
                            // normal mode beetle, useful for testing
                            (bcs->ModelCharaId == 327 && ApplyP1 == true)
                            ||
                            // p1 beetle omega
                            (bcs->ModelCharaId == 3771 && bcs->Health == 8557964 && ApplyP1 == true)
                        )
                        {
                            GameObjectStruct* gos = (GameObjectStruct*)go.Address;
                            float scale = SizeP1.CurrentValue / 100.0f;
                            bcs->ModelScale = scale;
                            gos->Scale = scale;
                            if (_omegaFoundAt == DateTime.MinValue)
                            {
                                Log(State.LogLevelEnum.Debug, null, "P1 Omega found, ensmollening to {0}x", scale);
                                _omegaFoundAt = DateTime.Now;
                            }
                            return;
                        }
                        else if (
                            // normal mode beetle, useful for testing
                            (bcs->ModelCharaId == 327 && ApplyP3 == true)
                            ||
                            // p3 not-really-final omega
                            (bcs->ModelCharaId == 3775 && bcs->Health == 11125976 && ApplyP3 == true)
                        )
                        {
                            GameObjectStruct* gos = (GameObjectStruct*)go.Address;
                            float scale = SizeP3.CurrentValue / 100.0f;
                            bcs->ModelScale = scale;
                            gos->Scale = scale;
                            if (_omegaFoundAt == DateTime.MinValue)
                            {
                                Log(State.LogLevelEnum.Debug, null, "P3 Omega found, ensmollening to {0}x", scale);
                                _omegaFoundAt = DateTime.Now;
                            }
                            return;
                        }
                    }
                }
            }

        }

        #endregion

#endif

        #region ProgramLoopAM

        public class ProgramLoopAM : Core.ContentItem
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
            [AttributeOrderNumber(2500)]
            public AutomarkerTiming Timing { get; set; }

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public Action Test { get; set; }

            private List<uint> _first = new List<uint>();
            private List<uint> _second = new List<uint>();
            private List<uint> _third = new List<uint>();
            private List<uint> _fourth = new List<uint>();
            private List<uint> _symbols = new List<uint>();
            private bool _fired = false;
            private int _currentStep = 0;

            private List<Party.PartyMember> _firstGo;
            private List<Party.PartyMember> _secondGo;
            private List<Party.PartyMember> _thirdGo;
            private List<Party.PartyMember> _fourthGo;

            public ProgramLoopAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Prio = new AutomarkerPrio();
                Prio.Priority = AutomarkerPrio.PrioTypeEnum.Role;
                Prio._prioByRole.Clear();
                Prio._prioByRole.Add(AutomarkerPrio.PrioRoleEnum.Melee);
                Prio._prioByRole.Add(AutomarkerPrio.PrioRoleEnum.Tank);
                Prio._prioByRole.Add(AutomarkerPrio.PrioRoleEnum.Ranged);
                Prio._prioByRole.Add(AutomarkerPrio.PrioRoleEnum.Caster);
                Prio._prioByRole.Add(AutomarkerPrio.PrioRoleEnum.Healer);
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Signs.SetRole("Tower1", AutomarkerSigns.SignEnum.Attack1, false);
                Signs.SetRole("Tower2", AutomarkerSigns.SignEnum.Attack2, false);
                Signs.SetRole("Tether1", AutomarkerSigns.SignEnum.Ignore1, false);
                Signs.SetRole("Tether2", AutomarkerSigns.SignEnum.Ignore2, false);
                Test = new Action(() => Signs.TestFunctionality(state, null, Timing));
            }

            internal void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _currentStep = 0;
                _fired = false;
                _first.Clear();
                _second.Clear();
                _third.Clear();
                _fourth.Clear();
                _symbols.Clear();
            }

            internal void PerformDecision()
            {
                _fired = true;
                Party pty = _state.GetPartyMembers();
                _firstGo = new List<Party.PartyMember>(
                    from ix in pty.Members join jx in _first on ix.ObjectId equals jx select ix
                );
                _secondGo = new List<Party.PartyMember>(
                    from ix in pty.Members join jx in _second on ix.ObjectId equals jx select ix
                );
                _thirdGo = new List<Party.PartyMember>(
                    from ix in pty.Members join jx in _third on ix.ObjectId equals jx select ix
                );
                _fourthGo = new List<Party.PartyMember>(
                    from ix in pty.Members join jx in _fourth on ix.ObjectId equals jx select ix
                );
                Prio.SortByPriority(_firstGo);
                Prio.SortByPriority(_secondGo);
                Prio.SortByPriority(_thirdGo);
                Prio.SortByPriority(_fourthGo);
                SendMarkers(1);
            }

            internal void SendMarkers(int index)
            {
                if (_currentStep == index)
                {
                    return;
                }
                _currentStep = index;
                Log(State.LogLevelEnum.Debug, null, "Sending marker set {0}", index);
                switch (index)
                {
                    case 1:
                        {
                            AutomarkerPayload ap = new AutomarkerPayload();
                            ap.assignments[Signs.Roles["Tether1"]] = _thirdGo[0].GameObject;
                            ap.assignments[Signs.Roles["Tether2"]] = _thirdGo[1].GameObject;
                            ap.assignments[Signs.Roles["Tower1"]] = _firstGo[0].GameObject;
                            ap.assignments[Signs.Roles["Tower2"]] = _firstGo[1].GameObject;
                            _state.ExecuteAutomarkers(ap, Timing);
                        }
                        break;
                    case 2:
                        {
                            AutomarkerPayload ap = new AutomarkerPayload();
                            ap.assignments[Signs.Roles["Tether1"]] = _fourthGo[0].GameObject;
                            ap.assignments[Signs.Roles["Tether2"]] = _fourthGo[1].GameObject;
                            ap.assignments[Signs.Roles["Tower1"]] = _secondGo[0].GameObject;
                            ap.assignments[Signs.Roles["Tower2"]] = _secondGo[1].GameObject;
                            _state.ExecuteAutomarkers(ap, Timing);
                        }
                        break;
                    case 3:
                        {
                            AutomarkerPayload ap = new AutomarkerPayload();
                            ap.assignments[Signs.Roles["Tether1"]] = _firstGo[0].GameObject;
                            ap.assignments[Signs.Roles["Tether2"]] = _firstGo[1].GameObject;
                            ap.assignments[Signs.Roles["Tower1"]] = _thirdGo[0].GameObject;
                            ap.assignments[Signs.Roles["Tower2"]] = _thirdGo[1].GameObject;
                            _state.ExecuteAutomarkers(ap, Timing);
                        }
                        break;
                    case 4:
                        {
                            AutomarkerPayload ap = new AutomarkerPayload();
                            ap.assignments[Signs.Roles["Tether1"]] = _secondGo[0].GameObject;
                            ap.assignments[Signs.Roles["Tether2"]] = _secondGo[1].GameObject;
                            ap.assignments[Signs.Roles["Tower1"]] = _fourthGo[0].GameObject;
                            ap.assignments[Signs.Roles["Tower2"]] = _fourthGo[1].GameObject;
                            _state.ExecuteAutomarkers(ap, Timing);
                        }
                        break;
                    case 5:
                        {
                            AutomarkerPayload ap = new AutomarkerPayload();
                            ap.Clear = true;
                            _state.ExecuteAutomarkers(ap, Timing);
                        }
                        break;
                }
            }

            internal void FeedStatus(uint actorId, uint statusId, bool gained)
            {
                if (Active == false)
                {
                    return;
                }
                if (gained == true)
                {
                    switch (statusId)
                    {
                        case StatusInLine1:
                            Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1}", statusId, actorId);
                            _first.Add(actorId);
                            _symbols.Add(actorId);
                            break;
                        case StatusInLine2:
                            Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1}", statusId, actorId);
                            _second.Add(actorId);
                            _symbols.Add(actorId);
                            break;
                        case StatusInLine3:
                            Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1}", statusId, actorId);
                            _third.Add(actorId);
                            _symbols.Add(actorId);
                            break;
                        case StatusInLine4:
                            Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1}", statusId, actorId);
                            _fourth.Add(actorId);
                            _symbols.Add(actorId);
                            break;
                    }
                    if (_fired == false)
                    {
                        if (_symbols.Count == 8)
                        {
                            Log(State.LogLevelEnum.Debug, null, "All statuses registered, ready for automarkers");
                            PerformDecision();
                        }
                        else
                        {
                            Log(State.LogLevelEnum.Debug, null, "No ready for automarkers yet; {0} symbols",
                                _symbols.Count
                            );
                        }
                    }
                }
                else
                {
                    switch (statusId)
                    {
                        case StatusInLine1:
                            Log(State.LogLevelEnum.Debug, null, "Lost status {0}", statusId);
                            SendMarkers(2);
                            break;
                        case StatusInLine2:
                            Log(State.LogLevelEnum.Debug, null, "Lost status {0}", statusId);
                            SendMarkers(3);
                            break;
                        case StatusInLine3:
                            Log(State.LogLevelEnum.Debug, null, "Lost status {0}", statusId);
                            SendMarkers(4);
                            break;
                        case StatusInLine4:
                            Log(State.LogLevelEnum.Debug, null, "Lost status {0}", statusId);
                            SendMarkers(5);
                            break;
                    }
                }
            }

        }

        #endregion

        #region PantokratorAM

        public class PantokratorAM : Core.ContentItem
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

            [DebugOption]
            [AttributeOrderNumber(2500)]
            public AutomarkerTiming Timing { get; set; }

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public Action Test { get; set; }

            private List<uint> _first = new List<uint>();
            private List<uint> _second = new List<uint>();
            private List<uint> _third = new List<uint>();
            private List<uint> _fourth = new List<uint>();
            private List<uint> _symbols = new List<uint>();
            private bool _fired = false;
            private int _currentStep = 0;

            private List<Party.PartyMember> _firstGo;
            private List<Party.PartyMember> _secondGo;
            private List<Party.PartyMember> _thirdGo;
            private List<Party.PartyMember> _fourthGo;

            public PantokratorAM(State state) : base(state)
            {
                Enabled = false;
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Signs = new AutomarkerSigns();
                Signs.SetRole("Beam1", AutomarkerSigns.SignEnum.Attack1, false);
                Signs.SetRole("Beam2", AutomarkerSigns.SignEnum.Attack2, false);
                Signs.SetRole("Missile1", AutomarkerSigns.SignEnum.Ignore1, false);
                Signs.SetRole("Missile2", AutomarkerSigns.SignEnum.Ignore2, false);
                Test = new Action(() => Signs.TestFunctionality(state, null, Timing));
            }

            internal void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _currentStep = 0;
                _fired = false;
                _first.Clear();
                _second.Clear();
                _third.Clear();
                _fourth.Clear();
                _symbols.Clear();
            }

            internal void PerformDecision()
            {
                _fired = true;
                Party pty = _state.GetPartyMembers();
                _firstGo = new List<Party.PartyMember>(
                    from ix in pty.Members join jx in _first on ix.ObjectId equals jx select ix
                );
                _secondGo = new List<Party.PartyMember>(
                    from ix in pty.Members join jx in _second on ix.ObjectId equals jx select ix
                );
                _thirdGo = new List<Party.PartyMember>(
                    from ix in pty.Members join jx in _third on ix.ObjectId equals jx select ix
                );
                _fourthGo = new List<Party.PartyMember>(
                    from ix in pty.Members join jx in _fourth on ix.ObjectId equals jx select ix
                );
                SendMarkers(1);
            }

            internal void SendMarkers(int index)
            {
                if (_currentStep == index)
                {
                    return;
                }
                _currentStep = index;
                Log(State.LogLevelEnum.Debug, null, "Sending marker set {0}", index);
                switch (index)
                {
                    case 1:
                        {
                            AutomarkerPayload ap = new AutomarkerPayload();
                            ap.assignments[Signs.Roles["Beam1"]] = _thirdGo[0].GameObject;
                            ap.assignments[Signs.Roles["Beam2"]] = _thirdGo[1].GameObject;
                            ap.assignments[Signs.Roles["Missile1"]] = _firstGo[0].GameObject;
                            ap.assignments[Signs.Roles["Missile2"]] = _firstGo[1].GameObject;
                            _state.ExecuteAutomarkers(ap, Timing);
                        }
                        break;
                    case 2:
                        {
                            AutomarkerPayload ap = new AutomarkerPayload();
                            ap.assignments[Signs.Roles["Beam1"]] = _fourthGo[0].GameObject;
                            ap.assignments[Signs.Roles["Beam2"]] = _fourthGo[1].GameObject;
                            ap.assignments[Signs.Roles["Missile1"]] = _secondGo[0].GameObject;
                            ap.assignments[Signs.Roles["Missile2"]] = _secondGo[1].GameObject;
                            _state.ExecuteAutomarkers(ap, Timing);
                        }
                        break;
                    case 3:
                        {
                            AutomarkerPayload ap = new AutomarkerPayload();
                            ap.assignments[Signs.Roles["Beam1"]] = _firstGo[0].GameObject;
                            ap.assignments[Signs.Roles["Beam2"]] = _firstGo[1].GameObject;
                            ap.assignments[Signs.Roles["Missile1"]] = _thirdGo[0].GameObject;
                            ap.assignments[Signs.Roles["Missile2"]] = _thirdGo[1].GameObject;
                            _state.ExecuteAutomarkers(ap, Timing);
                        }
                        break;
                    case 4:
                        {
                            AutomarkerPayload ap = new AutomarkerPayload();
                            ap.assignments[Signs.Roles["Beam1"]] = _secondGo[0].GameObject;
                            ap.assignments[Signs.Roles["Beam2"]] = _secondGo[1].GameObject;
                            ap.assignments[Signs.Roles["Missile1"]] = _fourthGo[0].GameObject;
                            ap.assignments[Signs.Roles["Missile2"]] = _fourthGo[1].GameObject;
                            _state.ExecuteAutomarkers(ap, Timing);
                        }
                        break;
                    case 5:
                        {
                            AutomarkerPayload ap = new AutomarkerPayload();
                            ap.Clear = true;
                            _state.ExecuteAutomarkers(ap, Timing);
                        }
                        break;
                }
            }

            internal void FeedStatus(uint actorId, uint statusId, bool gained)
            {
                if (Active == false)
                {
                    return;
                }
                if (gained == true)
                {
                    switch (statusId)
                    {
                        case StatusInLine1:
                            Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1}", statusId, actorId);
                            _first.Add(actorId);
                            _symbols.Add(actorId);
                            break;
                        case StatusInLine2:
                            Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1}", statusId, actorId);
                            _second.Add(actorId);
                            _symbols.Add(actorId);
                            break;
                        case StatusInLine3:
                            Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1}", statusId, actorId);
                            _third.Add(actorId);
                            _symbols.Add(actorId);
                            break;
                        case StatusInLine4:
                            Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1}", statusId, actorId);
                            _fourth.Add(actorId);
                            _symbols.Add(actorId);
                            break;
                    }
                    if (_fired == false)
                    {
                        if (_symbols.Count == 8)
                        {
                            Log(State.LogLevelEnum.Debug, null, "All statuses registered, ready for automarkers");
                            PerformDecision();
                        }
                        else
                        {
                            Log(State.LogLevelEnum.Debug, null, "No ready for automarkers yet; {0} symbols",
                                _symbols.Count
                            );
                        }
                    }
                }
                else
                {
                    switch (statusId)
                    {
                        case StatusInLine1:
                            Log(State.LogLevelEnum.Debug, null, "Lost status {0}", statusId);
                            SendMarkers(2);
                            break;
                        case StatusInLine2:
                            Log(State.LogLevelEnum.Debug, null, "Lost status {0}", statusId);
                            SendMarkers(3);
                            break;
                        case StatusInLine3:
                            Log(State.LogLevelEnum.Debug, null, "Lost status {0}", statusId);
                            SendMarkers(4);
                            break;
                        case StatusInLine4:
                            Log(State.LogLevelEnum.Debug, null, "Lost status {0}", statusId);
                            SendMarkers(5);
                            break;
                    }
                }
            }

        }

        #endregion

#if !SANS_GOETIA

        #region GlitchTether

        public class GlitchTether : Core.ContentItem
        {

            public override FeaturesEnum Features => FeaturesEnum.Drawing;

            [AttributeOrderNumber(1000)]
            public Vector4 TetherOkColor { get; set; } = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
            [AttributeOrderNumber(1001)]
            public Vector4 TetherNokColor { get; set; } = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
            [AttributeOrderNumber(1002)]
            public Vector4 TetherSafeColor { get; set; } = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);

            [DebugOption]
            [AttributeOrderNumber(2000)]
            public Action Test { get; set; }

            private uint _partnerId = 0;
            private uint _currentDebuff = 0;

            public GlitchTether(State state) : base(state)
            {
                Enabled = false;
                Test = new Action(() => TestFunctionality());
            }

            public void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _partnerId = 0;
                _currentDebuff = 0;
            }

            public void FeedTether(uint actorId1, uint actorId2)
            {
                uint myid = _state.cs.LocalPlayer.ObjectId;
                if (actorId1 == myid || actorId2 == myid)
                {
                    Log(State.LogLevelEnum.Debug, null, "Registered tether between {0} and {1}", actorId1, actorId2);
                    if (actorId1 == myid)
                    {
                        _partnerId = actorId2;
                    }
                    if (actorId2 == myid)
                    {
                        _partnerId = actorId1;
                    }
                }
            }

            public void FeedStatus(uint statusId)
            {
                Log(State.LogLevelEnum.Debug, null, "Registered status {0}", statusId);
                _currentDebuff = statusId;
            }

            public void TestFunctionality()
            {
                if (_partnerId > 0)
                {
                    _currentDebuff = 0;
                    _partnerId = 0;
                    return;
                }
                _state.InvokeZoneChange(1122);
                _currentDebuff = 0;
                _partnerId = 0;
                GameObject me = _state.cs.LocalPlayer as GameObject;
                foreach (GameObject go in _state.ot)
                {
                    if (go.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player && go.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc)
                    {
                        continue;
                    }
                    if (go.ObjectId == me.ObjectId)
                    {
                        continue;
                    }
                    bool isTargettable;
                    unsafe
                    {
                        GameObjectPtr* gop = (GameObjectPtr*)go.Address;
                        isTargettable = gop->GetIsTargetable();
                    }
                    bool isHidden = (isTargettable == false);
                    if (isHidden == true)
                    {
                        continue;
                    }
                    Random r = new Random();
                    _currentDebuff = r.Next(0, 2) == 0 ? StatusMidGlitch : StatusRemoteGlitch;
                    _partnerId = go.ObjectId;
                    Log(State.LogLevelEnum.Debug, null, "Testing from {0} to {1}", me, go);
                    return;
                }
            }

            protected override bool ExecutionImplementation()
            {
                ImDrawListPtr draw;
                if (_partnerId == 0 || _currentDebuff == 0)
                {
                    return false;
                }
                if (_state.StartDrawing(out draw) == false)
                {
                    return false;
                }
                GameObject go = _state.GetActorById(_partnerId);
                if (go == null)
                {
                    return false;
                }
                Vector3 t1, t2;
                Vector3 p1 = _state.cs.LocalPlayer.Position;
                Vector3 p2 = go.Position;
                float dist = Vector3.Distance(p1, p2);
                double anglexz = Math.Atan2(p1.Z - p2.Z, p1.X - p2.X);
                double angley = (p1.Y - p2.Y) / dist;
                if (_currentDebuff == StatusMidGlitch)
                {
                    float minDist = 20.0f;
                    float maxDist = 26.0f;
                    bool distOk = (dist >= minDist && dist <= maxDist);
                    t1 = _state.plug.TranslateToScreen(p2.X, p2.Y, p2.Z);
                    t2 = _state.plug.TranslateToScreen(p2.X + (Math.Cos(anglexz) * minDist), p2.Y + (angley * minDist), p2.Z + (Math.Sin(anglexz) * minDist));
                    draw.AddLine(
                        new Vector2(t1.X, t1.Y),
                        new Vector2(t2.X, t2.Y),
                        ImGui.GetColorU32(TetherNokColor),
                        3.0f
                    );
                    t1 = _state.plug.TranslateToScreen(p2.X + (Math.Cos(anglexz) * minDist), p2.Y + (angley * minDist), p2.Z + (Math.Sin(anglexz) * minDist));
                    t2 = _state.plug.TranslateToScreen(p2.X + (Math.Cos(anglexz) * maxDist), p2.Y + (angley * maxDist), p2.Z + (Math.Sin(anglexz) * maxDist));
                    draw.AddLine(
                        new Vector2(t1.X, t1.Y),
                        new Vector2(t2.X, t2.Y),
                        ImGui.GetColorU32(distOk == true ? TetherOkColor : TetherSafeColor),
                        6.0f
                    );
                    float distm = Math.Max(dist, maxDist + 5.0f);
                    t1 = _state.plug.TranslateToScreen(p2.X + (Math.Cos(anglexz) * maxDist), p2.Y + (angley * maxDist), p2.Z + (Math.Sin(anglexz) * maxDist));
                    t2 = _state.plug.TranslateToScreen(p2.X + (Math.Cos(anglexz) * distm), p2.Y + (angley * distm), p2.Z + (Math.Sin(anglexz) * distm));
                    draw.AddLine(
                        new Vector2(t1.X, t1.Y),
                        new Vector2(t2.X, t2.Y),
                        ImGui.GetColorU32(TetherNokColor),
                        3.0f
                    );
                }
                else if (_currentDebuff == StatusRemoteGlitch)
                {
                    float minDist = 35.0f;
                    bool distOk = (dist > minDist);
                    t1 = _state.plug.TranslateToScreen(p2.X, p2.Y, p2.Z);
                    t2 = _state.plug.TranslateToScreen(p2.X + (Math.Cos(anglexz) * minDist), p2.Y + (angley * minDist), p2.Z + (Math.Sin(anglexz) * minDist));
                    draw.AddLine(
                        new Vector2(t1.X, t1.Y),
                        new Vector2(t2.X, t2.Y),
                        ImGui.GetColorU32(TetherNokColor),
                        3.0f
                    );
                    float distm = Math.Max(dist, minDist + 5.0f);
                    t1 = _state.plug.TranslateToScreen(p2.X + (Math.Cos(anglexz) * minDist), p2.Y + (angley * minDist), p2.Z + (Math.Sin(anglexz) * minDist));
                    t2 = _state.plug.TranslateToScreen(p2.X + (Math.Cos(anglexz) * distm), p2.Y + (angley * distm), p2.Z + (Math.Sin(anglexz) * distm));
                    draw.AddLine(
                        new Vector2(t1.X, t1.Y),
                        new Vector2(t2.X, t2.Y),
                        ImGui.GetColorU32(distOk == true ? TetherOkColor : TetherSafeColor),
                        6.0f
                    );
                }
                return true;
            }

        }

        #endregion

#endif

        #region P3TransitionAM

        public class P3TransitionAM : Core.ContentItem
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
            [AttributeOrderNumber(2500)]
            public AutomarkerTiming Timing { get; set; }

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public Action Test { get; set; }

            private List<uint> _stacks = new List<uint>();
            private List<uint> _spreads = new List<uint>();
            private bool _signs = false;

            public P3TransitionAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Prio = new AutomarkerPrio();
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Prio.Priority = AutomarkerPrio.PrioTypeEnum.CongaX;
                Signs.SetRole("Stack1_1", AutomarkerSigns.SignEnum.Bind1, false);
                Signs.SetRole("Stack1_2", AutomarkerSigns.SignEnum.Ignore1, false);
                Signs.SetRole("Stack2_1", AutomarkerSigns.SignEnum.Bind2, false);
                Signs.SetRole("Stack2_2", AutomarkerSigns.SignEnum.Ignore2, false);
                Signs.SetRole("Spread1", AutomarkerSigns.SignEnum.Attack1, false);
                Signs.SetRole("Spread2", AutomarkerSigns.SignEnum.Attack2, false);
                Signs.SetRole("Spread3", AutomarkerSigns.SignEnum.Attack3, false);
                Signs.SetRole("Spread4", AutomarkerSigns.SignEnum.Attack4, false);
                Test = new Action(() => Signs.TestFunctionality(state, null, Timing));
            }

            internal void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _signs = false;
                _stacks.Clear();
                _spreads.Clear();
            }

            internal void FeedStatus(uint actorId, uint statusId)
            {
                if (Active == false)
                {
                    return;
                }
                switch (statusId)
                {
                    case 0:
                        if (_signs == true)
                        {
                            Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1}", statusId, actorId);
                            _signs = false;
                            AutomarkerPayload ap = new AutomarkerPayload();
                            ap.Clear = true;
                            _state.ExecuteAutomarkers(ap, Timing);
                        }
                        return;
                    case StatusStackSniper:
                        Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1}", statusId, actorId);
                        _stacks.Add(actorId);
                        break;
                    case StatusSpreadSniper:
                        Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1}", statusId, actorId);
                        _spreads.Add(actorId);
                        break;
                    default:
                        return;
                }
                ReadyForDecision();
            }

            internal void ReadyForDecision()
            {
                if (_stacks.Count != 2 || _spreads.Count != 4)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "All statuses registered, ready for automarkers");
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
                _state.ExecuteAutomarkers(ap, Timing);
                _signs = true;
            }

        }

        #endregion

#if !SANS_GOETIA

        #region HelloWorldDrawBossMonitor

        public class HelloWorldDrawBossMonitor : DynamisOmegaDrawBossMonitor
        {

            public HelloWorldDrawBossMonitor(State st) : base(st)
            {
                Enabled = false;
            }

        }

        #endregion

#endif

        #region P3MonitorAM

        public class P3MonitorAM : Core.ContentItem
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
            [AttributeOrderNumber(2500)]
            public AutomarkerTiming Timing { get; set; }

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public Action Test { get; set; }

            private List<uint> _monitors = new List<uint>();
            private bool _signs = false;

            public P3MonitorAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Prio = new AutomarkerPrio();
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Prio.Priority = AutomarkerPrio.PrioTypeEnum.CongaY;
                Signs.SetRole("Monitor1", AutomarkerSigns.SignEnum.Bind1, false);
                Signs.SetRole("Monitor2", AutomarkerSigns.SignEnum.Bind2, false);
                Signs.SetRole("Monitor3", AutomarkerSigns.SignEnum.Bind3, false);
                Signs.SetRole("None1", AutomarkerSigns.SignEnum.Attack1, false);
                Signs.SetRole("None2", AutomarkerSigns.SignEnum.Attack2, false);
                Signs.SetRole("None3", AutomarkerSigns.SignEnum.Attack3, false);
                Signs.SetRole("None4", AutomarkerSigns.SignEnum.Attack4, false);
                Signs.SetRole("None5", AutomarkerSigns.SignEnum.Attack5, false);
                Test = new Action(() => Signs.TestFunctionality(state, null, Timing));
            }

            internal void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _signs = false;
                _monitors.Clear();
            }

            internal void FeedStatus(uint actorId, uint statusId)
            {
                if (Active == false)
                {
                    return;
                }
                switch (statusId)
                {
                    case 0:
                        if (_signs == true)
                        {
                            Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1}", statusId, actorId);
                            _signs = false;
                            AutomarkerPayload ap = new AutomarkerPayload();
                            ap.Clear = true;
                            _state.ExecuteAutomarkers(ap, Timing);
                        }
                        return;
                    case StatusMonitorLeft:
                    case StatusMonitorRight:
                        Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1}", statusId, actorId);
                        _monitors.Add(actorId);
                        break;
                    default:
                        return;
                }
                ReadyForDecision();
            }

            internal void ReadyForDecision()
            {
                if (_monitors.Count != 3)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "All monitors registered, ready for automarkers");
                Party pty = _state.GetPartyMembers();
                List<Party.PartyMember> _monitorsGo = new List<Party.PartyMember>(
                    from ix in pty.Members join jx in _monitors on ix.ObjectId equals jx select ix
                );
                List<Party.PartyMember> _unmarkedGo = new List<Party.PartyMember>(
                    from ix in pty.Members where _monitorsGo.Contains(ix) == false select ix
                );
                Prio.SortByPriority(_monitorsGo);
                Prio.SortByPriority(_unmarkedGo);
                AutomarkerPayload ap = new AutomarkerPayload();
                ap.assignments[Signs.Roles["Monitor1"]] = _monitorsGo[0].GameObject;
                ap.assignments[Signs.Roles["Monitor2"]] = _monitorsGo[1].GameObject;
                ap.assignments[Signs.Roles["Monitor3"]] = _monitorsGo[2].GameObject;
                ap.assignments[Signs.Roles["None1"]] = _unmarkedGo[0].GameObject;
                ap.assignments[Signs.Roles["None2"]] = _unmarkedGo[1].GameObject;
                ap.assignments[Signs.Roles["None3"]] = _unmarkedGo[2].GameObject;
                ap.assignments[Signs.Roles["None4"]] = _unmarkedGo[3].GameObject;
                ap.assignments[Signs.Roles["None5"]] = _unmarkedGo[4].GameObject;
                _state.ExecuteAutomarkers(ap, Timing);
                _signs = true;
            }

        }

        #endregion

#if !SANS_GOETIA

        #region DynamisDeltaDrawBossMonitor

        public class DynamisDeltaDrawBossMonitor : Core.ContentItem
        {

            public override FeaturesEnum Features => FeaturesEnum.Drawing;

            [AttributeOrderNumber(1000)]
            public Vector4 HighlightColor { get; set; } = new Vector4(1.0f, 0.0f, 0.0f, 0.2f);

            [DebugOption]
            [AttributeOrderNumber(2000)]
            public Action Test { get; set; }

            private DirectionsEnum omegaPos = DirectionsEnum.North;
            private uint _currentAction = 0;

            private enum DirectionsEnum
            {
                North,
                South,
                West,
                East
            }

            public DynamisDeltaDrawBossMonitor(State state) : base(state)
            {
                Enabled = false;
                Test = new Action(() => TestFunctionality());
            }

            public void FeedAction(uint actorId, uint actionId)
            {
                if (actionId > 0)
                {
                    GameObject omegachan = _state.GetActorById(actorId);
                    Vector3 pos = omegachan.Position;
                    if (pos.X < 90.0f)
                    {
                        omegaPos = DirectionsEnum.West;
                    }
                    else if (pos.X > 110.0f)
                    {
                        omegaPos = DirectionsEnum.East;
                    }
                    else if (pos.Z < 90.0f)
                    {
                        omegaPos = DirectionsEnum.North;
                    }
                    else if (pos.Z > 110.0f)
                    {
                        omegaPos = DirectionsEnum.South;
                    }
                    Log(State.LogLevelEnum.Debug, null, "Omega is {0} (pos: {1},{2},{3}) and currently {4}", omegachan, pos.X, pos.Y, pos.Z, omegaPos);
                }
                _currentAction = actionId;
            }

            public void TestFunctionality()
            {
                if (_currentAction != 0)
                {
                    _currentAction = 0;
                    return;
                }
                _state.InvokeZoneChange(1122);
                Random r = new Random();
                int test = r.Next(0, 2);
                FeedAction(_state.cs.LocalPlayer.ObjectId, (uint)(test == 0 ? AbilityBossMonitorDeltaLeft : AbilityBossMonitorDeltaRight));
                omegaPos = (DirectionsEnum)r.Next(0, 4);
            }

            protected override bool ExecutionImplementation()
            {
                ImDrawListPtr draw;
                if (_state.StartDrawing(out draw) == false)
                {
                    return false;
                }
                if (_currentAction == 0)
                {
                    return false;
                }
                DirectionsEnum monitors = DirectionsEnum.North;
                float x1 = 0.0f, x2 = 0.0f, y1 = 0.0f, y2 = 0.0f;
                switch (omegaPos)
                {
                    case DirectionsEnum.North:
                        monitors = _currentAction == AbilityBossMonitorDeltaLeft ? DirectionsEnum.East : DirectionsEnum.West;
                        break;
                    case DirectionsEnum.South:
                        monitors = _currentAction == AbilityBossMonitorDeltaLeft ? DirectionsEnum.West : DirectionsEnum.East;
                        break;
                    case DirectionsEnum.East:
                        monitors = _currentAction == AbilityBossMonitorDeltaLeft ? DirectionsEnum.South : DirectionsEnum.North;
                        break;
                    case DirectionsEnum.West:
                        monitors = _currentAction == AbilityBossMonitorDeltaLeft ? DirectionsEnum.North : DirectionsEnum.South;
                        break;
                }
                switch (monitors)
                {
                    case DirectionsEnum.West:
                        x1 = 80.0f; x2 = 100.0f; y1 = 80.0f; y2 = 120.0f;
                        break;
                    case DirectionsEnum.East:
                        x1 = 100.0f; x2 = 120.0f; y1 = 80.0f; y2 = 120.0f;
                        break;
                    case DirectionsEnum.North:
                        x1 = 80.0f; x2 = 120.0f; y1 = 80.0f; y2 = 100.0f;
                        break;
                    case DirectionsEnum.South:
                        x1 = 80.0f; x2 = 120.0f; y1 = 100.0f; y2 = 120.0f;
                        break;
                }
                Vector3 t1 = new Vector3(x1, 0.0f, y1);
                Vector3 t3 = new Vector3(x2, 0.0f, y2);
                Vector3 t2 = new Vector3(t3.X, (t1.Y + t3.Y) / 2.0f, t1.Z);
                Vector3 t4 = new Vector3(t1.X, (t1.Y + t3.Y) / 2.0f, t3.Z);
                Vector3 v1 = _state.plug.TranslateToScreen(t1.X, t1.Y, t1.Z);
                Vector3 v2 = _state.plug.TranslateToScreen(t2.X, t2.Y, t2.Z);
                Vector3 v3 = _state.plug.TranslateToScreen(t3.X, t3.Y, t3.Z);
                Vector3 v4 = _state.plug.TranslateToScreen(t4.X, t4.Y, t4.Z);
                draw.PathLineTo(new Vector2(v1.X, v1.Y));
                draw.PathLineTo(new Vector2(v2.X, v2.Y));
                draw.PathLineTo(new Vector2(v3.X, v3.Y));
                draw.PathLineTo(new Vector2(v4.X, v4.Y));
                draw.PathLineTo(new Vector2(v1.X, v1.Y));
                draw.PathFillConvex(
                    ImGui.GetColorU32(HighlightColor)
                );
                return true;
            }

        }

        #endregion

#endif

        #region DynamisDeltaAM

        public class DynamisDeltaAM : Core.ContentItem
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

            [DebugOption]
            [AttributeOrderNumber(2500)]
            public AutomarkerTiming Timing { get; set; }

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public Action Test { get; set; }

            private Dictionary<uint, GameObject> _debuffs = new Dictionary<uint, GameObject>();

            public DynamisDeltaAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                SetupPresets();
                Signs.ApplyPreset("LPDU");
                Test = new Action(() => Signs.TestFunctionality(state, null, Timing));
            }

            private void SetupPresets()
            {
                Dictionary<string, AutomarkerSigns.SignEnum> pr;
                pr = new Dictionary<string, AutomarkerSigns.SignEnum>();
                pr["DistantWorld"] = AutomarkerSigns.SignEnum.Plus;
                pr["NearWorld"] = AutomarkerSigns.SignEnum.Triangle;
                Signs.Presets["LPDU"] = pr;
                pr = new Dictionary<string, AutomarkerSigns.SignEnum>();
                pr["DistantWorld"] = AutomarkerSigns.SignEnum.Ignore2;
                pr["NearWorld"] = AutomarkerSigns.SignEnum.Ignore1;
                Signs.Presets["ElementalDC"] = pr;
            }

            internal void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _debuffs.Clear();
            }

            internal void FeedStatus(uint actorId, uint statusId, float duration, int stacks)
            {
                if (Active == false || (statusId != StatusDistantWorld && statusId != StatusNearWorld))
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1}", statusId, actorId);
                _debuffs[statusId] = _state.GetActorById(actorId);
                if (_debuffs.Count == 2)
                {
                    Log(State.LogLevelEnum.Debug, null, "All statuses registered, ready for automarkers");
                    AutomarkerPayload ap = new AutomarkerPayload();
                    ap.assignments[Signs.Roles["DistantWorld"]] = _debuffs[StatusDistantWorld];
                    ap.assignments[Signs.Roles["NearWorld"]] = _debuffs[StatusNearWorld];
                    _state.ExecuteAutomarkers(ap, Timing);
                }
            }

        }

        #endregion

        #region DynamisSigmaAM

        public class DynamisSigmaAM : Core.ContentItem
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

            [DebugOption]
            [AttributeOrderNumber(2500)]
            public AutomarkerTiming Timing { get; set; }

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public Action Test { get; set; }

            private Dictionary<uint, int> _dynamisStacks = new Dictionary<uint, int>();
            private Dictionary<uint, uint> _debuffs = new Dictionary<uint, uint>();
            private List<uint> _wavecannons = new List<uint>();

            private List<uint> _psMarkers = new List<uint>();
            private List<uint> _psCross = new List<uint>();
            private List<uint> _psSquare = new List<uint>();
            private List<uint> _psTriangle = new List<uint>();
            private List<uint> _psCircle = new List<uint>();

            public DynamisSigmaAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                SetupPresets();
                Signs.ApplyPreset("LPDU");
                Test = new Action(() => Signs.TestFunctionality(state, null, Timing));
            }

            private void SetupPresets()
            {
                Dictionary<string, AutomarkerSigns.SignEnum> pr;
                pr = new Dictionary<string, AutomarkerSigns.SignEnum>();
                pr["Arm1"] = AutomarkerSigns.SignEnum.Bind1;
                pr["Arm2"] = AutomarkerSigns.SignEnum.Bind2;
                pr["DistantWorld"] = AutomarkerSigns.SignEnum.Plus;
                pr["NearWorld"] = AutomarkerSigns.SignEnum.Triangle;
                pr["DistantFarBait"] = AutomarkerSigns.SignEnum.Attack1;
                pr["DistantCloseBait"] = AutomarkerSigns.SignEnum.Attack4;
                pr["NearBait1"] = AutomarkerSigns.SignEnum.Attack2;
                pr["NearBait2"] = AutomarkerSigns.SignEnum.Attack3;
                Signs.Presets["LPDU"] = pr;
                pr = new Dictionary<string, AutomarkerSigns.SignEnum>();
                pr["Arm1"] = AutomarkerSigns.SignEnum.Attack1;
                pr["Arm2"] = AutomarkerSigns.SignEnum.Attack2;
                pr["DistantWorld"] = AutomarkerSigns.SignEnum.Ignore2;
                pr["NearWorld"] = AutomarkerSigns.SignEnum.Ignore1;
                pr["DistantFarBait"] = AutomarkerSigns.SignEnum.Attack3;
                pr["DistantCloseBait"] = AutomarkerSigns.SignEnum.Bind1;
                pr["NearBait1"] = AutomarkerSigns.SignEnum.Bind2;
                pr["NearBait2"] = AutomarkerSigns.SignEnum.Bind3;
                Signs.Presets["ElementalDC"] = pr;
            }

            internal void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _dynamisStacks.Clear();
                _debuffs.Clear();
                _psMarkers.Clear();
                _psCross.Clear();
                _psSquare.Clear();
                _psTriangle.Clear();
                _psCircle.Clear();
                _wavecannons.Clear();
            }

            internal void FeedHeadmarker(uint actorId, uint headMarkerId)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered headmarker {0} on {1}", headMarkerId, actorId);
                switch (headMarkerId)
                {
                    case HeadmarkerCircle:
                        _psCircle.Add(actorId);
                        _psMarkers.Add(actorId);
                        break;
                    case HeadmarkerCross:
                        _psCross.Add(actorId);
                        _psMarkers.Add(actorId);
                        break;
                    case HeadmarkerSquare:
                        _psSquare.Add(actorId);
                        _psMarkers.Add(actorId);
                        break;
                    case HeadmarkerTriangle:
                        _psTriangle.Add(actorId);
                        _psMarkers.Add(actorId);
                        break;
                    case HeadmarkerTarget:
                        _wavecannons.Add(actorId);
                        break;
                    default:
                        return;
                }
                ReadyForDecision();
            }

            internal void FeedStatus(uint actorId, uint statusId, float duration, int stacks)
            {
                if (Active == false || (statusId != StatusDynamis && statusId != StatusDistantWorld && statusId != StatusNearWorld))
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1} with {2} stacks", statusId, actorId, stacks);
                if (statusId == StatusDynamis)
                {
                    _dynamisStacks[actorId] = stacks;
                    return;
                }
                _debuffs[statusId] = actorId;
                ReadyForDecision();
            }

            internal void ReadyForDecision()
            {
                if (_wavecannons.Count != 6 || _debuffs.Count != 2 || _psMarkers.Count != 8)
                {
                    Log(State.LogLevelEnum.Debug, null, "No ready for automarkers yet; {0} cannons {1} debuffs {2} markers",
                        _wavecannons.Count, _debuffs.Count, _psMarkers.Count
                    );
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "All statuses registered, ready for automarkers");
                AutomarkerPayload ap = new AutomarkerPayload();
                uint distant = _debuffs[StatusDistantWorld];
                uint near = _debuffs[StatusNearWorld];
                ap.assignments[Signs.Roles["DistantWorld"]] = _state.GetActorById(distant);
                ap.assignments[Signs.Roles["NearWorld"]] = _state.GetActorById(near);
                var pplWithStack = (from ix in _dynamisStacks
                                    where ix.Value == 1
                                    && ix.Key != distant
                                    && ix.Key != near
                                    select ix.Key).Take(3).ToList();
                ap.assignments[Signs.Roles["Arm1"]] = _state.GetActorById(pplWithStack[0]);
                ap.assignments[Signs.Roles["Arm2"]] = _state.GetActorById(pplWithStack[1]);
                ap.assignments[Signs.Roles["DistantFarBait"]] = _state.GetActorById(pplWithStack[2]);
                var theRest = (from ix in _psMarkers
                               where pplWithStack.Contains(ix) == false
                               && ix != distant
                               && ix != near
                               select ix).Take(3).ToList();
                ap.assignments[Signs.Roles["DistantCloseBait"]] = _state.GetActorById(theRest[0]);
                ap.assignments[Signs.Roles["NearBait1"]] = _state.GetActorById(theRest[1]);
                ap.assignments[Signs.Roles["NearBait2"]] = _state.GetActorById(theRest[2]);
                _state.ExecuteAutomarkers(ap, Timing);
            }

        }

        #endregion

#if !SANS_GOETIA

        #region DynamisOmegaDrawBossMonitor

        public class DynamisOmegaDrawBossMonitor : Core.ContentItem
        {

            public override FeaturesEnum Features => FeaturesEnum.Drawing;

            [AttributeOrderNumber(1000)]
            public Vector4 HighlightColor { get; set; } = new Vector4(1.0f, 0.0f, 0.0f, 0.2f);

            [DebugOption]
            [AttributeOrderNumber(2000)]
            public Action Test { get; set; }

            public uint _currentAction = 0;

            public DynamisOmegaDrawBossMonitor(State state) : base(state)
            {
                Enabled = false;
                Test = new Action(() => TestFunctionality());
            }

            public void FeedAction(uint actionId)
            {
                Log(State.LogLevelEnum.Debug, null, "Registered action {0}", actionId);
                _currentAction = actionId;
            }

            public void TestFunctionality()
            {
                if (_currentAction != 0)
                {
                    _currentAction = 0;
                    return;
                }
                _state.InvokeZoneChange(1122);
                Random r = new Random();
                int test = r.Next(0, 2);
                FeedAction((uint)(test == 0 ? AbilityBossMonitorEast : AbilityBossMonitorWest));
            }

            protected override bool ExecutionImplementation()
            {
                ImDrawListPtr draw;
                if (_state.StartDrawing(out draw) == false)
                {
                    return false;
                }
                if (_currentAction == 0)
                {
                    return false;
                }
                float x1 = 0.0f, x2 = 0.0f;
                switch (_currentAction)
                {
                    case AbilityBossMonitorDeltaLeft:
                        x1 = 80.0f;
                        x2 = 100.0f;
                        break;
                    case AbilityBossMonitorDeltaRight:
                        x1 = 100.0f;
                        x2 = 120.0f;
                        break;
                }
                Vector3 t1 = new Vector3(x1, 0.0f, 80.0f);
                Vector3 t3 = new Vector3(x2, 0.0f, 120.0f);
                Vector3 t2 = new Vector3(t3.X, (t1.Y + t3.Y) / 2.0f, t1.Z);
                Vector3 t4 = new Vector3(t1.X, (t1.Y + t3.Y) / 2.0f, t3.Z);
                Vector3 v1 = _state.plug.TranslateToScreen(t1.X, t1.Y, t1.Z);
                Vector3 v2 = _state.plug.TranslateToScreen(t2.X, t2.Y, t2.Z);
                Vector3 v3 = _state.plug.TranslateToScreen(t3.X, t3.Y, t3.Z);
                Vector3 v4 = _state.plug.TranslateToScreen(t4.X, t4.Y, t4.Z);
                draw.PathLineTo(new Vector2(v1.X, v1.Y));
                draw.PathLineTo(new Vector2(v2.X, v2.Y));
                draw.PathLineTo(new Vector2(v3.X, v3.Y));
                draw.PathLineTo(new Vector2(v4.X, v4.Y));
                draw.PathLineTo(new Vector2(v1.X, v1.Y));
                draw.PathFillConvex(
                    ImGui.GetColorU32(HighlightColor)
                );
                return true;
            }

        }

        #endregion

#endif

        #region DynamisOmegaAM

        public class DynamisOmegaAM : Core.ContentItem
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

            [DebugOption]
            [AttributeOrderNumber(2500)]
            public AutomarkerTiming Timing { get; set; }

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public Action Test { get; set; }

            private Dictionary<uint, int> _dynamisStacks = new Dictionary<uint, int>();
            private List<uint> _distants = new List<uint>();
            private List<uint> _nears = new List<uint>();
            private List<uint> _firsts = new List<uint>();
            private List<uint> _seconds = new List<uint>();
            private bool _fired = false;

            internal AutomarkerPayload SecondPayload;

            public DynamisOmegaAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                SetupPresets();
                Signs.ApplyPreset("LPDU");
                Test = new Action(() => Signs.TestFunctionality(state, null, Timing));
            }

            private void SetupPresets()
            {
                Dictionary<string, AutomarkerSigns.SignEnum> pr;
                pr = new Dictionary<string, AutomarkerSigns.SignEnum>();
                pr["Monitor1"] = AutomarkerSigns.SignEnum.Bind1;
                pr["Monitor2"] = AutomarkerSigns.SignEnum.Bind2;
                pr["DistantWorld"] = AutomarkerSigns.SignEnum.Plus;
                pr["NearWorld"] = AutomarkerSigns.SignEnum.Triangle;
                pr["Bait1"] = AutomarkerSigns.SignEnum.Attack1;
                pr["Bait2"] = AutomarkerSigns.SignEnum.Attack2;
                pr["Bait3"] = AutomarkerSigns.SignEnum.Attack3;
                pr["Bait4"] = AutomarkerSigns.SignEnum.Attack4;
                Signs.Presets["LPDU"] = pr;
            }

            internal void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _dynamisStacks.Clear();
                _distants.Clear();
                _nears.Clear();
                _firsts.Clear();
                _seconds.Clear();
                _fired = false;
            }

            internal void FeedStatus(uint actorId, uint statusId, float duration, int stacks)
            {
                if (Active == false || _fired == true)
                {
                    return;
                }
                switch (statusId)
                {
                    case StatusDistantWorld:
                        _distants.Add(actorId);
                        break;
                    case StatusNearWorld:
                        _nears.Add(actorId);
                        break;
                    case StatusInLine1:
                        _firsts.Add(actorId);
                        break;
                    case StatusInLine2:
                        _seconds.Add(actorId);
                        break;
                    case StatusDynamis:
                        _dynamisStacks[actorId] = stacks;
                        break;
                    default:
                        return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1} with {2} stacks", statusId, actorId, stacks);
                ReadyForDecision();
            }

            internal void ReadyForDecision()
            {
                if (_distants.Count != 2 || _nears.Count != 2 || _firsts.Count != 2 || _seconds.Count != 2)
                {
                    Log(State.LogLevelEnum.Debug, null, "No ready for automarkers yet; {0} distants {1} nears {2} firsts {3} seconds",
                        _distants.Count, _nears.Count, _firsts.Count, _seconds.Count
                    );
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "All statuses registered, ready for automarkers");
                List<uint> firstMonitorsSelection = new List<uint>();
                var distant2nd = (from s in _seconds join d in _distants on s equals d select s).FirstOrDefault();
                var near2nd = (from s in _seconds join d in _nears on s equals d select s).FirstOrDefault();
                var distant1st = (from s in _firsts join d in _distants on s equals d select s).FirstOrDefault();
                var near1st = (from s in _firsts join d in _nears on s equals d select s).FirstOrDefault();
                if (distant2nd > 0 && _dynamisStacks[distant2nd] == 2)
                {
                    firstMonitorsSelection.Add(distant2nd);
                }
                if (near2nd > 0 && _dynamisStacks[near2nd] == 2)
                {
                    firstMonitorsSelection.Add(near2nd);
                }
                if (firstMonitorsSelection.Count < 2)
                {
                    firstMonitorsSelection.AddRange(
                        (from ix in _dynamisStacks
                         where ix.Value == 2
                         && ix.Key != distant1st && ix.Key != near1st
                         && ix.Key != distant2nd && ix.Key != near2nd
                         select ix.Key).Take(2 - firstMonitorsSelection.Count)
                    );
                }
                AutomarkerPayload ap1 = new AutomarkerPayload();
                ap1.assignments[Signs.Roles["Monitor1"]] = _state.GetActorById(firstMonitorsSelection[0]);
                ap1.assignments[Signs.Roles["Monitor2"]] = _state.GetActorById(firstMonitorsSelection[1]);
                ap1.assignments[Signs.Roles["DistantWorld"]] = _state.GetActorById(distant1st);
                ap1.assignments[Signs.Roles["NearWorld"]] = _state.GetActorById(near1st);
                var theRest1 = (from ix in _dynamisStacks
                                where firstMonitorsSelection.Contains(ix.Key) == false
                                && ix.Key != distant1st
                                && ix.Key != near1st
                                select ix.Key).Take(4).ToList();
                ap1.assignments[Signs.Roles["Bait1"]] = _state.GetActorById(theRest1[0]);
                ap1.assignments[Signs.Roles["Bait2"]] = _state.GetActorById(theRest1[1]);
                ap1.assignments[Signs.Roles["Bait3"]] = _state.GetActorById(theRest1[2]);
                ap1.assignments[Signs.Roles["Bait4"]] = _state.GetActorById(theRest1[3]);
                _dynamisStacks[distant1st] = _dynamisStacks[distant1st] + 1;
                _dynamisStacks[near1st] = _dynamisStacks[near1st] + 1;
                _dynamisStacks[theRest1[0]] = _dynamisStacks[theRest1[0]] + 1;
                _dynamisStacks[theRest1[1]] = _dynamisStacks[theRest1[1]] + 1;
                _dynamisStacks[theRest1[2]] = _dynamisStacks[theRest1[2]] + 1;
                _dynamisStacks[theRest1[3]] = _dynamisStacks[theRest1[3]] + 1;
                AutomarkerPayload ap2 = new AutomarkerPayload();
                var threeStacks = (from ix in _dynamisStacks
                                   where ix.Value == 3
                                   select ix.Key).Take(2).ToList();
                Log(State.LogLevelEnum.Debug, null, "SET 1 -- m1 {0} m2 {1} distant {2} near {3} spreads {4} {5} {6} {7}",
                    firstMonitorsSelection[0], firstMonitorsSelection[1], distant1st, near1st, theRest1[0], theRest1[1], theRest1[2], theRest1[3]
                );
                ap2.assignments[Signs.Roles["Monitor1"]] = _state.GetActorById(threeStacks[0]);
                ap2.assignments[Signs.Roles["Monitor2"]] = _state.GetActorById(threeStacks[1]);
                ap2.assignments[Signs.Roles["DistantWorld"]] = _state.GetActorById(distant2nd);
                ap2.assignments[Signs.Roles["NearWorld"]] = _state.GetActorById(near2nd);
                var theRest2 = (from ix in _dynamisStacks
                                where threeStacks.Contains(ix.Key) == false
                                && ix.Key != distant2nd
                                && ix.Key != near2nd
                                select ix.Key).Take(4).ToList();
                ap2.assignments[Signs.Roles["Bait1"]] = _state.GetActorById(theRest2[0]);
                ap2.assignments[Signs.Roles["Bait2"]] = _state.GetActorById(theRest2[1]);
                ap2.assignments[Signs.Roles["Bait3"]] = _state.GetActorById(theRest2[2]);
                ap2.assignments[Signs.Roles["Bait4"]] = _state.GetActorById(theRest2[3]);
                SecondPayload = ap2;
                _fired = true;
                _state.ExecuteAutomarkers(ap1, Timing);
            }

        }

        #endregion

        public UltOmegaProtocol(State st) : base(st)
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
            _state.OnHeadMarker += OnHeadMarker;
            _state.OnTether += OnTether;
        }

        private void OnStatusChange(uint src, uint dest, uint statusId, bool gained, float duration, int stacks)
        {
#if !SANS_GOETIA
            if (statusId == StatusMidGlitch || statusId == StatusRemoteGlitch)
            {
                _glitchTether.FeedStatus(gained == false ? 0 : statusId);
            }
#endif
            if (statusId == StatusStackSniper || statusId == StatusSpreadSniper)
            {
                _p3transAm.FeedStatus(dest, gained == false ? 0 : statusId);
            }
            if (statusId == StatusMonitorLeft || statusId == StatusMonitorRight)
            {
                _p3moniAm.FeedStatus(dest, gained == false ? 0 : statusId);
            }
            if (statusId == StatusDynamis)
            {
                if (gained == false)
                {
                    return;
                }
                _sigmaAm.FeedStatus(dest, statusId, duration, stacks);
                _omegaAm.FeedStatus(dest, statusId, duration, stacks);
            }
            else
            {
                switch (CurrentPhase)
                {
                    case PhaseEnum.P1_ProgramLoop:
                        if (statusId == StatusInLine1 || statusId == StatusInLine2 || statusId == StatusInLine3 || statusId == StatusInLine4)
                        {
                            _loopAm.FeedStatus(dest, statusId, gained);
                        }
                        break;
                    case PhaseEnum.P1_Pantokrator:
                        if (statusId == StatusInLine1 || statusId == StatusInLine2 || statusId == StatusInLine3 || statusId == StatusInLine4)
                        {
                            _pantoAm.FeedStatus(dest, statusId, gained);
                        }
                        break;
                    case PhaseEnum.P5_Delta:
                        if (gained == true)
                        {
                            _deltaAm.FeedStatus(dest, statusId, duration, stacks);
                        }
                        break;
                    case PhaseEnum.P5_Sigma:
                        if (gained == true)
                        {
                            _sigmaAm.FeedStatus(dest, statusId, duration, stacks);
                        }
                        break;
                    case PhaseEnum.P5_Omega:
                        if (gained == true)
                        {
                            _omegaAm.FeedStatus(dest, statusId, duration, stacks);
                        }
                        break;
                }
            }
        }

        private void OnCastBegin(uint src, uint dest, ushort actionId, float castTime, float rotation)
        {
            switch (actionId)
            {
                case AbilityProgramLoop:
                    CurrentPhase = PhaseEnum.P1_ProgramLoop;
                    _loopAm.Reset();
                    break;
                case AbilityPantokrator:
                    CurrentPhase = PhaseEnum.P1_Pantokrator;
                    _pantoAm.Reset();
                    break;
                case AbilityLaserShower:
                    CurrentPhase = PhaseEnum.P3_Transition;
                    _p3transAm.Reset();
                    _p3moniAm.Reset();
#if !SANS_GOETIA
                    _chibiOmega.StartLooking(120.0f);
#endif
                    break;
                case AbilityDynamisDelta:
                    CurrentPhase = PhaseEnum.P5_Delta;
                    _sigmaAm.Reset();
                    _omegaAm.Reset();
                    _deltaAm.Reset();
                    break;
                case AbilityDynamisSigma:
                    CurrentPhase = PhaseEnum.P5_Sigma;
                    break;
                case AbilityDynamisOmega:
                    CurrentPhase = PhaseEnum.P5_Omega;
                    break;
#if !SANS_GOETIA
                case AbilityBossMonitorDeltaLeft:
                case AbilityBossMonitorDeltaRight:
                    if (CurrentPhase == PhaseEnum.P5_Delta)
                    {
                        _deltaMonitor.FeedAction(dest, actionId);
                    }
                    if (CurrentPhase == PhaseEnum.P5_Omega)
                    {
                        _omegaMonitor.FeedAction(actionId);
                    }
                    break;
                case AbilityBossMonitorEast:
                case AbilityBossMonitorWest:
                    if (CurrentPhase == PhaseEnum.P3_Transition)
                    {
                        _hwMonitor.FeedAction(actionId);
                    }
                    break;
#endif
            }
        }

        private void OnAction(uint src, uint dest, ushort actionId)
        {
            switch (actionId)
            {
                case AbilityBossMonitorDeltaLeft:
                case AbilityBossMonitorDeltaRight:
                    if (CurrentPhase == PhaseEnum.P5_Delta)
                    {
#if !SANS_GOETIA
                        _deltaMonitor.FeedAction(dest, 0);
#endif
                    }
                    if (CurrentPhase == PhaseEnum.P5_Omega)
                    {
#if !SANS_GOETIA
                        _omegaMonitor.FeedAction(0);
#endif
                        AutomarkerPayload ap = new AutomarkerPayload() { Clear = true };
                        _state.ExecuteAutomarkers(_omegaAm.SecondPayload, _omegaAm.Timing);
                    }
                    break;
                case AbilityBossMonitorEast:
                case AbilityBossMonitorWest:
                    if (CurrentPhase == PhaseEnum.P3_Transition)
                    {
#if !SANS_GOETIA
                        _hwMonitor.FeedAction(0);
#endif
                        AutomarkerPayload ap = new AutomarkerPayload() { Clear = true };
                        _state.ExecuteAutomarkers(ap, _p3moniAm.Timing);
                    }
                    break;
                case AbilityHelloDistantWorldBig:
                    if (CurrentPhase == PhaseEnum.P5_Delta)
                    {
                        AutomarkerPayload ap = new AutomarkerPayload() { Clear = true };
                        _state.ExecuteAutomarkers(ap, _deltaAm.Timing);
                    }
                    if (CurrentPhase == PhaseEnum.P5_Sigma)
                    {
                        AutomarkerPayload ap = new AutomarkerPayload() { Clear = true };
                        _state.ExecuteAutomarkers(ap, _sigmaAm.Timing);
                    }
                    break;
            }
        }

        private void OnHeadMarker(uint dest, uint markerId)
        {
            if (_sawFirstHeadMarker == false)
            {
                _sawFirstHeadMarker = true;
                _firstHeadMarker = markerId - 23;
            }
            uint realMarkerId = markerId - _firstHeadMarker;
            if (CurrentPhase == PhaseEnum.P5_Sigma)
            {
                _sigmaAm.FeedHeadmarker(dest, realMarkerId);
            }
        }

        private void OnTether(uint src, uint dest, uint tetherId)
        {
#if !SANS_GOETIA
            if (tetherId == 222)
            {
                _glitchTether.FeedTether(src, dest);
            }
#endif
        }

        private void UnsubscribeFromEvents()
        {
            _state.OnHeadMarker -= OnHeadMarker;
            _state.OnStatusChange -= OnStatusChange;
            _state.OnAction -= OnAction;
            _state.OnCastBegin -= OnCastBegin;
        }

        private void OnCombatChange(bool inCombat)
        {
            if (inCombat == true)
            {
                CurrentPhase = PhaseEnum.P1_Start;
                _sawFirstHeadMarker = false;
                _firstHeadMarker = 0;
                SubscribeToEvents();
            }
            else
            {
                UnsubscribeFromEvents();
            }
        }

        private void OnZoneChange(ushort newZone)
        {
            // normal modes included for some easier testing
            bool newZoneOk = (newZone == 800 || newZone == 804 || newZone == 1122);
            if (newZoneOk == true && ZoneOk == false)
            {
                Log(State.LogLevelEnum.Info, null, "Content available");
#if !SANS_GOETIA
                _chibiOmega = (ChibiOmega)Items["ChibiOmega"];
                _chibiOmega.StartLooking(10.0f);
                _glitchTether = (GlitchTether)Items["GlitchTether"];
                _hwMonitor = (HelloWorldDrawBossMonitor)Items["HelloWorldDrawBossMonitor"];
                _deltaMonitor = (DynamisDeltaDrawBossMonitor)Items["DynamisDeltaDrawBossMonitor"];
                _omegaMonitor = (DynamisOmegaDrawBossMonitor)Items["DynamisOmegaDrawBossMonitor"];
#endif
                _loopAm = (ProgramLoopAM)Items["ProgramLoopAM"];
                _pantoAm = (PantokratorAM)Items["PantokratorAM"];
                _p3transAm = (P3TransitionAM)Items["P3TransitionAM"];
                _p3moniAm = (P3MonitorAM)Items["P3MonitorAM"];
                _deltaAm = (DynamisDeltaAM)Items["DynamisDeltaAM"];
                _sigmaAm = (DynamisSigmaAM)Items["DynamisSigmaAM"];
                _omegaAm = (DynamisOmegaAM)Items["DynamisOmegaAM"];
                _state.OnCombatChange += OnCombatChange;
                _state.OnDirectorUpdate += OnDirectorUpdate;
            }
            else if (newZoneOk == false && ZoneOk == true)
            {
                Log(State.LogLevelEnum.Info, null, "Content unavailable");
                _state.OnDirectorUpdate -= OnDirectorUpdate;
                _state.OnCombatChange -= OnCombatChange;
            }
            ZoneOk = newZoneOk;
        }

        private void OnDirectorUpdate(uint param1, uint param2, uint param3, uint param4)
        {
#if !SANS_GOETIA
            if (param2 == 0x4000000F)
            {
                _chibiOmega.StartLooking(10.0f);
            }
            if (param2 == 0x40000006)
            {
                _chibiOmega.StopLooking();
            }
#endif
        }

    }

}
