using System;
using Dalamud.Game.ClientState.Objects.Types;
using Lemegeton.Core;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using ImGuiNET;
using Character = Dalamud.Game.ClientState.Objects.Types.ICharacter;
using CharacterStruct = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using GameObjectStruct = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using GameObjectPtr = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using static Lemegeton.Core.State;

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

        private bool ZoneOk = false;
        private bool _subbed = false;
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
        private P2SynergyAM _p2synergyAm;
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
            P5_Omega2nd,
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
                foreach (IGameObject go in _state.ot)
                {
                    if (go.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc && go is ICharacter)
                    {
                        ICharacter bc = (ICharacter)go;
                        CharacterStruct* bcs = (CharacterStruct*)bc.Address;
                        CharacterData cd = bcs->CharacterData;
                        if (
                            // normal mode beetle, useful for testing
                            (bcs->ModelContainer.ModelCharaId == 327 && ApplyP1 == true)
                            ||
                            // p1 beetle omega
                            (bcs->ModelContainer.ModelCharaId == 3771 && cd.Health == 8557964 && ApplyP1 == true)
                        )
                        {
                            GameObjectStruct* gos = (GameObjectStruct*)go.Address;
                            float scale = SizeP1.CurrentValue / 100.0f;
                            cd.ModelScale = scale;
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
                            (bcs->ModelContainer.ModelCharaId == 327 && ApplyP3 == true)
                            ||
                            // p3 not-really-final omega
                            (bcs->ModelContainer.ModelCharaId == 3775 && cd.Health == 11125976 && ApplyP3 == true)
                        )
                        {
                            GameObjectStruct* gos = (GameObjectStruct*)go.Address;
                            float scale = SizeP3.CurrentValue / 100.0f;
                            cd.ModelScale = scale;
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

        public class ProgramLoopAM : Automarker
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
                Test = new System.Action(() => Signs.TestFunctionality(state, Prio, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
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
                _firstGo = pty.GetByActorIds(_first);
                _secondGo = pty.GetByActorIds(_second);
                _thirdGo = pty.GetByActorIds(_third);
                _fourthGo = pty.GetByActorIds(_fourth);
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
                            AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                            ap.Assign(Signs.Roles["Tether1"], _thirdGo[0].GameObject);
                            ap.Assign(Signs.Roles["Tether2"], _thirdGo[1].GameObject);
                            ap.Assign(Signs.Roles["Tower1"], _firstGo[0].GameObject);
                            ap.Assign(Signs.Roles["Tower2"], _firstGo[1].GameObject);
                            _state.ExecuteAutomarkers(ap, Timing);
                        }
                        break;
                    case 2:
                        {
                            AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                            ap.Assign(Signs.Roles["Tether1"], _fourthGo[0].GameObject);
                            ap.Assign(Signs.Roles["Tether2"], _fourthGo[1].GameObject);
                            ap.Assign(Signs.Roles["Tower1"], _secondGo[0].GameObject);
                            ap.Assign(Signs.Roles["Tower2"], _secondGo[1].GameObject);
                            _state.ExecuteAutomarkers(ap, Timing);
                        }
                        break;
                    case 3:
                        {
                            AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                            ap.Assign(Signs.Roles["Tether1"], _firstGo[0].GameObject);
                            ap.Assign(Signs.Roles["Tether2"], _firstGo[1].GameObject);
                            ap.Assign(Signs.Roles["Tower1"], _thirdGo[0].GameObject);
                            ap.Assign(Signs.Roles["Tower2"], _thirdGo[1].GameObject);
                            _state.ExecuteAutomarkers(ap, Timing);
                        }
                        break;
                    case 4:
                        {
                            AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                            ap.Assign(Signs.Roles["Tether1"], _secondGo[0].GameObject);
                            ap.Assign(Signs.Roles["Tether2"], _secondGo[1].GameObject);
                            ap.Assign(Signs.Roles["Tower1"], _fourthGo[0].GameObject);
                            ap.Assign(Signs.Roles["Tower2"], _fourthGo[1].GameObject);
                            _state.ExecuteAutomarkers(ap, Timing);
                        }
                        break;
                    case 5:
                        {
                            _state.ClearAutoMarkers();
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
                Log(State.LogLevelEnum.Debug, null, "Registered status {0} {1} on {2:X}", statusId, gained, actorId);
                if (gained == true)
                {
                    switch (statusId)
                    {
                        case StatusInLine1:
                            _first.Add(actorId);
                            _symbols.Add(actorId);
                            break;
                        case StatusInLine2:
                            _second.Add(actorId);
                            _symbols.Add(actorId);
                            break;
                        case StatusInLine3:
                            _third.Add(actorId);
                            _symbols.Add(actorId);
                            break;
                        case StatusInLine4:
                            _fourth.Add(actorId);
                            _symbols.Add(actorId);
                            break;
                    }
                    if (_fired == false && _symbols.Count == 8)
                    {
                        Log(State.LogLevelEnum.Debug, null, "Ready for automarkers");
                        PerformDecision();
                    }
                    return;
                }
                switch (statusId)
                {
                    case StatusInLine1:
                        SendMarkers(2);
                        break;
                    case StatusInLine2:
                        SendMarkers(3);
                        break;
                    case StatusInLine3:
                        SendMarkers(4);
                        break;
                    case StatusInLine4:
                        SendMarkers(5);
                        break;
                }
            }

        }

        #endregion

        #region PantokratorAM

        public class PantokratorAM : Automarker
        {

            [AttributeOrderNumber(1000)]
            public AutomarkerSigns Signs { get; set; }

            [DebugOption]
            [AttributeOrderNumber(2500)]
            public AutomarkerTiming Timing { get; set; }

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public System.Action Test { get; set; }

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
                Test = new System.Action(() => Signs.TestFunctionality(state, null, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
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
                _firstGo = pty.GetByActorIds(_first);
                _secondGo = pty.GetByActorIds(_second);
                _thirdGo = pty.GetByActorIds(_third);
                _fourthGo = pty.GetByActorIds(_fourth);
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
                            AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                            ap.Assign(Signs.Roles["Beam1"], _thirdGo[0].GameObject);
                            ap.Assign(Signs.Roles["Beam2"], _thirdGo[1].GameObject);
                            ap.Assign(Signs.Roles["Missile1"], _firstGo[0].GameObject);
                            ap.Assign(Signs.Roles["Missile2"], _firstGo[1].GameObject);
                            _state.ExecuteAutomarkers(ap, Timing);
                        }
                        break;
                    case 2:
                        {
                            AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                            ap.Assign(Signs.Roles["Beam1"], _fourthGo[0].GameObject);
                            ap.Assign(Signs.Roles["Beam2"], _fourthGo[1].GameObject);
                            ap.Assign(Signs.Roles["Missile1"], _secondGo[0].GameObject);
                            ap.Assign(Signs.Roles["Missile2"], _secondGo[1].GameObject);
                            _state.ExecuteAutomarkers(ap, Timing);
                        }
                        break;
                    case 3:
                        {
                            AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                            ap.Assign(Signs.Roles["Beam1"], _firstGo[0].GameObject);
                            ap.Assign(Signs.Roles["Beam2"], _firstGo[1].GameObject);
                            ap.Assign(Signs.Roles["Missile1"], _thirdGo[0].GameObject);
                            ap.Assign(Signs.Roles["Missile2"], _thirdGo[1].GameObject);
                            _state.ExecuteAutomarkers(ap, Timing);
                        }
                        break;
                    case 4:
                        {
                            AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                            ap.Assign(Signs.Roles["Beam1"], _secondGo[0].GameObject);
                            ap.Assign(Signs.Roles["Beam2"], _secondGo[1].GameObject);
                            ap.Assign(Signs.Roles["Missile1"], _fourthGo[0].GameObject);
                            ap.Assign(Signs.Roles["Missile2"], _fourthGo[1].GameObject);
                            _state.ExecuteAutomarkers(ap, Timing);
                        }
                        break;
                    case 5:
                        {
                            _state.ClearAutoMarkers();
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
                Log(State.LogLevelEnum.Debug, null, "Registered status {0} {1} on {2:X}", statusId, gained, actorId);
                if (gained == true)
                {
                    switch (statusId)
                    {
                        case StatusInLine1:
                            _first.Add(actorId);
                            _symbols.Add(actorId);
                            break;
                        case StatusInLine2:
                            _second.Add(actorId);
                            _symbols.Add(actorId);
                            break;
                        case StatusInLine3:
                            _third.Add(actorId);
                            _symbols.Add(actorId);
                            break;
                        case StatusInLine4:
                            _fourth.Add(actorId);
                            _symbols.Add(actorId);
                            break;
                    }
                    if (_fired == false && _symbols.Count == 8)
                    {
                        Log(State.LogLevelEnum.Debug, null, "Ready for automarkers");
                        PerformDecision();
                    }
                    return;
                }
                switch (statusId)
                {
                    case StatusInLine1:
                        SendMarkers(2);
                        break;
                    case StatusInLine2:
                        SendMarkers(3);
                        break;
                    case StatusInLine3:
                        SendMarkers(4);
                        break;
                    case StatusInLine4:
                        SendMarkers(5);
                        break;
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
            public System.Action Test { get; set; }

            private ulong _partnerId = 0;
            private uint _currentDebuff = 0;

            public GlitchTether(State state) : base(state)
            {
                Enabled = false;
                Test = new System.Action(() => TestFunctionality());
            }

            public void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _partnerId = 0;
                _currentDebuff = 0;
            }

            public void FeedTether(uint actorId1, uint actorId2)
            {
                ulong myid = _state.cs.LocalPlayer.GameObjectId;
                if (actorId1 == myid || actorId2 == myid)
                {
                    Log(State.LogLevelEnum.Debug, null, "Registered tether between {0:X} and {1:X}", actorId1, actorId2);
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
                IGameObject me = _state.cs.LocalPlayer as IGameObject;
                foreach (IGameObject go in _state.ot)
                {
                    if (go.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player && go.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc)
                    {
                        continue;
                    }
                    if (go.GameObjectId == me.GameObjectId)
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
                    _partnerId = go.GameObjectId;
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
                IGameObject go = _state.GetActorById(_partnerId);
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
                    t1 = _state.plug._ui.TranslateToScreen(p2.X, p2.Y, p2.Z);
                    t2 = _state.plug._ui.TranslateToScreen(p2.X + (Math.Cos(anglexz) * minDist), p2.Y + (angley * minDist), p2.Z + (Math.Sin(anglexz) * minDist));
                    draw.AddLine(
                        new Vector2(t1.X, t1.Y),
                        new Vector2(t2.X, t2.Y),
                        ImGui.GetColorU32(TetherNokColor),
                        3.0f
                    );
                    t1 = _state.plug._ui.TranslateToScreen(p2.X + (Math.Cos(anglexz) * minDist), p2.Y + (angley * minDist), p2.Z + (Math.Sin(anglexz) * minDist));
                    t2 = _state.plug._ui.TranslateToScreen(p2.X + (Math.Cos(anglexz) * maxDist), p2.Y + (angley * maxDist), p2.Z + (Math.Sin(anglexz) * maxDist));
                    draw.AddLine(
                        new Vector2(t1.X, t1.Y),
                        new Vector2(t2.X, t2.Y),
                        ImGui.GetColorU32(distOk == true ? TetherOkColor : TetherSafeColor),
                        6.0f
                    );
                    float distm = Math.Max(dist, maxDist + 5.0f);
                    t1 = _state.plug._ui.TranslateToScreen(p2.X + (Math.Cos(anglexz) * maxDist), p2.Y + (angley * maxDist), p2.Z + (Math.Sin(anglexz) * maxDist));
                    t2 = _state.plug._ui.TranslateToScreen(p2.X + (Math.Cos(anglexz) * distm), p2.Y + (angley * distm), p2.Z + (Math.Sin(anglexz) * distm));
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
                    t1 = _state.plug._ui.TranslateToScreen(p2.X, p2.Y, p2.Z);
                    t2 = _state.plug._ui.TranslateToScreen(p2.X + (Math.Cos(anglexz) * minDist), p2.Y + (angley * minDist), p2.Z + (Math.Sin(anglexz) * minDist));
                    draw.AddLine(
                        new Vector2(t1.X, t1.Y),
                        new Vector2(t2.X, t2.Y),
                        ImGui.GetColorU32(TetherNokColor),
                        3.0f
                    );
                    float distm = Math.Max(dist, minDist + 5.0f);
                    t1 = _state.plug._ui.TranslateToScreen(p2.X + (Math.Cos(anglexz) * minDist), p2.Y + (angley * minDist), p2.Z + (Math.Sin(anglexz) * minDist));
                    t2 = _state.plug._ui.TranslateToScreen(p2.X + (Math.Cos(anglexz) * distm), p2.Y + (angley * distm), p2.Z + (Math.Sin(anglexz) * distm));
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

        #region P2SynergyAM

        public class P2SynergyAM : Automarker
        {

            [AttributeOrderNumber(1000)]
            public AutomarkerSigns Signs { get; set; }

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

            private List<uint> _psCross = new List<uint>();
            private List<uint> _psSquare = new List<uint>();
            private List<uint> _psCircle = new List<uint>();
            private List<uint> _psTriangle = new List<uint>();
            private bool _fired = false;
            private uint _statusId = 0;

            public P2SynergyAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Signs2 = new AutomarkerSigns();
                Prio = new AutomarkerPrio();
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Prio.Priority = AutomarkerPrio.PrioTypeEnum.PartyListOrder;
                SetupPresets();
                Signs.ApplyPreset("BPOG - GPOB");
                Signs2.ApplyPreset("BPOG - BPOG");
                Test = new System.Action(() => Signs.TestFunctionality(state, Prio, Timing, SelfMarkOnly, AsSoftmarker));
            }

            private void SetupPresets()
            {
                AutomarkerSigns.Preset pr;
                pr = new AutomarkerSigns.Preset() { Builtin = true };
                pr.Name = "BPOG - GPOB";
                pr.Roles["CrossL"] = AutomarkerSigns.SignEnum.Plus;
                pr.Roles["CrossR"] = AutomarkerSigns.SignEnum.Attack4;
                pr.Roles["SquareL"] = AutomarkerSigns.SignEnum.Square;
                pr.Roles["SquareR"] = AutomarkerSigns.SignEnum.Attack2;
                pr.Roles["CircleL"] = AutomarkerSigns.SignEnum.Circle;
                pr.Roles["CircleR"] = AutomarkerSigns.SignEnum.Attack3;
                pr.Roles["TriangleL"] = AutomarkerSigns.SignEnum.Triangle;
                pr.Roles["TriangleR"] = AutomarkerSigns.SignEnum.Attack1;
                Signs.AddPreset(pr);
                pr = new AutomarkerSigns.Preset() { Builtin = true };
                pr.Name = "BPOG - GOPB";
                pr.Roles["CrossL"] = AutomarkerSigns.SignEnum.Plus;
                pr.Roles["CrossR"] = AutomarkerSigns.SignEnum.Attack4;
                pr.Roles["SquareL"] = AutomarkerSigns.SignEnum.Square;
                pr.Roles["SquareR"] = AutomarkerSigns.SignEnum.Attack3;
                pr.Roles["CircleL"] = AutomarkerSigns.SignEnum.Circle;
                pr.Roles["CircleR"] = AutomarkerSigns.SignEnum.Attack2;
                pr.Roles["TriangleL"] = AutomarkerSigns.SignEnum.Triangle;
                pr.Roles["TriangleR"] = AutomarkerSigns.SignEnum.Attack1;
                Signs.AddPreset(pr);
                pr = new AutomarkerSigns.Preset() { Builtin = true };
                pr.Name = "BPOG - BPOG";
                pr.Roles["CrossL"] = AutomarkerSigns.SignEnum.Plus;
                pr.Roles["CrossR"] = AutomarkerSigns.SignEnum.Attack1;
                pr.Roles["SquareL"] = AutomarkerSigns.SignEnum.Square;
                pr.Roles["SquareR"] = AutomarkerSigns.SignEnum.Attack2;
                pr.Roles["CircleL"] = AutomarkerSigns.SignEnum.Circle;
                pr.Roles["CircleR"] = AutomarkerSigns.SignEnum.Attack3;
                pr.Roles["TriangleL"] = AutomarkerSigns.SignEnum.Triangle;
                pr.Roles["TriangleR"] = AutomarkerSigns.SignEnum.Attack4;
                Signs2.AddPreset(pr);
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _fired = false;
                _psCross.Clear();
                _psSquare.Clear();
                _psCircle.Clear();
                _psTriangle.Clear();
                _statusId = 0;
            }

            internal void FeedStatus(uint statusId)
            {
                if (Active == false)
                {
                    return;
                }
                if (_fired == true)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered status {0}", statusId);
                _statusId = statusId;
                if (_psCross.Count != 2 || _psSquare.Count != 2 || _psCircle.Count != 2 || _psTriangle.Count != 2 || _statusId == 0)
                {
                    return;
                }
                ReadyForDecision();
            }

            internal void FeedHeadmarker(uint actorId, uint headMarkerId)
            {
                if (Active == false)
                {
                    return;
                }
                if (_fired == true)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered headMarkerId {0} on {1:X}", headMarkerId, actorId);
                switch (headMarkerId)
                {
                    case HeadmarkerCross:
                        _psCross.Add(actorId);
                        break;
                    case HeadmarkerSquare:
                        _psSquare.Add(actorId);
                        break;
                    case HeadmarkerCircle:
                        _psCircle.Add(actorId);
                        break;
                    case HeadmarkerTriangle:
                        _psTriangle.Add(actorId);
                        break;
                }
                if (_psCross.Count != 2 || _psSquare.Count != 2 || _psCircle.Count != 2 || _psTriangle.Count != 2 || _statusId == 0)
                {
                    return;
                }
                ReadyForDecision();
            }

            internal void ReadyForDecision()
            {
                AutomarkerSigns ams;
                Log(State.LogLevelEnum.Debug, null, "Ready for automarkers");
                if (_statusId == StatusRemoteGlitch)
                {
                    ams = Signs;
                }
                else
                {
                    ams = Signs2;
                }
                Party pty = _state.GetPartyMembers();
                List<Party.PartyMember> _psCrossGo = pty.GetByActorIds(_psCross);
                List<Party.PartyMember> _psSquareGo = pty.GetByActorIds(_psSquare);
                List<Party.PartyMember> _psCircleGo = pty.GetByActorIds(_psCircle);
                List<Party.PartyMember> _psTriangleGo = pty.GetByActorIds(_psTriangle);
                Prio.SortByPriority(_psCrossGo);
                Prio.SortByPriority(_psSquareGo);
                Prio.SortByPriority(_psCircleGo);
                Prio.SortByPriority(_psTriangleGo);
                AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                ap.Assign(ams.Roles["CrossL"], _psCrossGo[0].GameObject);
                ap.Assign(ams.Roles["CrossR"], _psCrossGo[1].GameObject);
                ap.Assign(ams.Roles["SquareL"], _psSquareGo[0].GameObject);
                ap.Assign(ams.Roles["SquareR"], _psSquareGo[1].GameObject);
                ap.Assign(ams.Roles["CircleL"], _psCircleGo[0].GameObject);
                ap.Assign(ams.Roles["CircleR"], _psCircleGo[1].GameObject);
                ap.Assign(ams.Roles["TriangleL"], _psTriangleGo[0].GameObject);
                ap.Assign(ams.Roles["TriangleR"], _psTriangleGo[1].GameObject);
                _state.ExecuteAutomarkers(ap, Timing);
                _fired = true;
            }

        }

        #endregion

        #region P3TransitionAM

        public class P3TransitionAM : Automarker
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

            private List<uint> _stacks = new List<uint>();
            private List<uint> _spreads = new List<uint>();
            private bool _fired = false;

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
                Test = new System.Action(() => Signs.TestFunctionality(state, Prio, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _fired = false;
                _stacks.Clear();
                _spreads.Clear();
            }

            internal void FeedStatus(uint actorId, uint statusId, bool gained)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered status {0} {1} on {2:X}", statusId, gained, actorId);
                if (gained == false)
                {
                    if (_fired == true)
                    {
                        Log(State.LogLevelEnum.Debug, null, "Clearing automarkers");
                        Reset();
                        _state.ClearAutoMarkers();
                    }
                    return;
                }
                switch (statusId)
                {
                    case StatusStackSniper:
                        _stacks.Add(actorId);
                        break;
                    case StatusSpreadSniper:
                        _spreads.Add(actorId);
                        break;
                }
                if (_stacks.Count != 2 || _spreads.Count != 4)
                {
                    return;
                }
                ReadyForDecision();
            }

            internal void ReadyForDecision()
            {
                Log(State.LogLevelEnum.Debug, null, "Ready for automarkers");
                Party pty = _state.GetPartyMembers();
                List<Party.PartyMember> _stacksGo = pty.GetByActorIds(_stacks);
                List<Party.PartyMember> _spreadsGo = pty.GetByActorIds(_spreads);
                List<Party.PartyMember> _unmarkedGo = new List<Party.PartyMember>(
                    from ix in pty.Members where _stacksGo.Contains(ix) == false && _spreadsGo.Contains(ix) == false select ix
                );
                Prio.SortByPriority(_stacksGo);
                Prio.SortByPriority(_spreadsGo);
                Prio.SortByPriority(_unmarkedGo);
                AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                ap.Assign(Signs.Roles["Stack1_1"], _stacksGo[0].GameObject);
                ap.Assign(Signs.Roles["Stack1_2"], _unmarkedGo[0].GameObject);
                ap.Assign(Signs.Roles["Stack2_1"], _stacksGo[1].GameObject);
                ap.Assign(Signs.Roles["Stack2_2"], _unmarkedGo[1].GameObject);
                ap.Assign(Signs.Roles["Spread1"], _spreadsGo[0].GameObject);
                ap.Assign(Signs.Roles["Spread2"], _spreadsGo[1].GameObject);
                ap.Assign(Signs.Roles["Spread3"], _spreadsGo[2].GameObject);
                ap.Assign(Signs.Roles["Spread4"], _spreadsGo[3].GameObject);
                _state.ExecuteAutomarkers(ap, Timing);
                _fired = true;
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

        public class P3MonitorAM : Automarker
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

            private List<uint> _monitors = new List<uint>();
            private bool _fired = false;

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
                Test = new System.Action(() => Signs.TestFunctionality(state, Prio, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _fired = false;
                _monitors.Clear();
            }

            internal void FeedStatus(uint actorId, uint statusId, bool gained)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered status {0} {1} on {2:X}", statusId, gained, actorId);
                if (gained == false)
                {
                    if (_fired == true)
                    {
                        Log(State.LogLevelEnum.Debug, null, "Clearing automarkers");
                        Reset();
                        _state.ClearAutoMarkers();
                    }
                    return;
                }
                _monitors.Add(actorId);
                if (_monitors.Count < 3)
                {
                    return;
                }
                ReadyForDecision();
            }

            internal void ReadyForDecision()
            {
                Log(State.LogLevelEnum.Debug, null, "Ready for automarkers");
                Party pty = _state.GetPartyMembers();
                List<Party.PartyMember> _monitorsGo = pty.GetByActorIds(_monitors);
                List<Party.PartyMember> _unmarkedGo = new List<Party.PartyMember>(
                    from ix in pty.Members where _monitorsGo.Contains(ix) == false select ix
                );
                Prio.SortByPriority(_monitorsGo);
                Prio.SortByPriority(_unmarkedGo);
                AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                ap.Assign(Signs.Roles["Monitor1"], _monitorsGo[0].GameObject);
                ap.Assign(Signs.Roles["Monitor2"], _monitorsGo[1].GameObject);
                ap.Assign(Signs.Roles["Monitor3"], _monitorsGo[2].GameObject);
                ap.Assign(Signs.Roles["None1"], _unmarkedGo[0].GameObject);
                ap.Assign(Signs.Roles["None2"], _unmarkedGo[1].GameObject);
                ap.Assign(Signs.Roles["None3"], _unmarkedGo[2].GameObject);
                ap.Assign(Signs.Roles["None4"], _unmarkedGo[3].GameObject);
                ap.Assign(Signs.Roles["None5"], _unmarkedGo[4].GameObject);
                _state.ExecuteAutomarkers(ap, Timing);
                _fired = true;
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
            public System.Action Test { get; set; }

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
                Test = new System.Action(() => TestFunctionality());
            }

            public void FeedAction(ulong actorId, uint actionId)
            {
                if (actionId > 0)
                {
                    IGameObject omegachan = _state.GetActorById(actorId);
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
                    Log(State.LogLevelEnum.Debug, null, "Omega is {0} (pos: {1},{2},{3}) -> {4}", omegachan, pos.X, pos.Y, pos.Z, omegaPos);
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
                FeedAction(_state.cs.LocalPlayer.GameObjectId, (uint)(test == 0 ? AbilityBossMonitorDeltaLeft : AbilityBossMonitorDeltaRight));
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
                Vector3 v1 = _state.plug._ui.TranslateToScreen(t1.X, t1.Y, t1.Z);
                Vector3 v2 = _state.plug._ui.TranslateToScreen(t2.X, t2.Y, t2.Z);
                Vector3 v3 = _state.plug._ui.TranslateToScreen(t3.X, t3.Y, t3.Z);
                Vector3 v4 = _state.plug._ui.TranslateToScreen(t4.X, t4.Y, t4.Z);
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

        public class DynamisDeltaAM : Automarker
        {

            [AttributeOrderNumber(1000)]
            public AutomarkerSigns Signs { get; set; }

            [DebugOption]
            [AttributeOrderNumber(2500)]
            public AutomarkerTiming Timing { get; set; }

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public System.Action Test { get; set; }

            private Dictionary<uint, IGameObject> _debuffs = [];

            private HashSet<(uint, uint)> _tethers = [];

            public DynamisDeltaAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                SetupPresets();
                Signs.ApplyPreset("LPDU");
                Test = new System.Action(() => Signs.TestFunctionality(state, null, Timing, SelfMarkOnly, AsSoftmarker));
            }

            private void SetupPresets()
            {
                AutomarkerSigns.Preset pr;
                pr = new AutomarkerSigns.Preset() { Builtin = true };
                pr.Name = "LPDU";
                pr.Roles["DistantWorld"] = AutomarkerSigns.SignEnum.Plus;
                pr.Roles["NearWorld"] = AutomarkerSigns.SignEnum.Triangle;
                pr.Roles["CloseTether1"] = AutomarkerSigns.SignEnum.None;
                pr.Roles["CloseTether2"] = AutomarkerSigns.SignEnum.None;
                pr.Roles["CloseTether3"] = AutomarkerSigns.SignEnum.None;
                pr.Roles["CloseTether4"] = AutomarkerSigns.SignEnum.None;
                Signs.AddPreset(pr);
                pr = new AutomarkerSigns.Preset() { Builtin = true };
                pr.Name = "ElementalDC";
                pr.Roles["DistantWorld"] = AutomarkerSigns.SignEnum.Ignore2;
                pr.Roles["NearWorld"] = AutomarkerSigns.SignEnum.Ignore1;
                pr.Roles["CloseTether1"] = AutomarkerSigns.SignEnum.None;
                pr.Roles["CloseTether2"] = AutomarkerSigns.SignEnum.None;
                pr.Roles["CloseTether3"] = AutomarkerSigns.SignEnum.None;
                pr.Roles["CloseTether4"] = AutomarkerSigns.SignEnum.None;
                Signs.AddPreset(pr);
                pr = new AutomarkerSigns.Preset() { Builtin = true };
                pr.Name = "MLM";
                pr.Roles["DistantWorld"] = AutomarkerSigns.SignEnum.Bind1;
                pr.Roles["NearWorld"] = AutomarkerSigns.SignEnum.Bind2;
                pr.Roles["CloseTether1"] = AutomarkerSigns.SignEnum.Attack1;
                pr.Roles["CloseTether2"] = AutomarkerSigns.SignEnum.Attack2;
                pr.Roles["CloseTether3"] = AutomarkerSigns.SignEnum.Attack3;
                pr.Roles["CloseTether4"] = AutomarkerSigns.SignEnum.Attack4;
                Signs.AddPreset(pr);
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _debuffs.Clear();
            }

            internal void FeedStatus(uint actorId, uint statusId)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1:X}", statusId, actorId);
                _debuffs[statusId] = _state.GetActorById(actorId);
                if (_debuffs.Count == 2 && _tethers.Count == 2)
                {
                    ReadyForDecision();
                }
            }

            internal void FeedTether(uint actorId1, uint actorId2)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered tether between {0:X} and {1:X}", actorId1, actorId2);
                _tethers.Add(actorId1 < actorId2 ? (actorId1, actorId2) : (actorId2, actorId1));
                if (_debuffs.Count == 2 && _tethers.Count == 2)
                {
                    ReadyForDecision();
                }
            }

            internal void ReadyForDecision()
            {
                Log(State.LogLevelEnum.Debug, null, "Ready for automarkers");
                AutomarkerPayload ap = new(_state, SelfMarkOnly, AsSoftmarker);
                ap.Assign(Signs.Roles["DistantWorld"], _debuffs[StatusDistantWorld]);
                ap.Assign(Signs.Roles["NearWorld"], _debuffs[StatusNearWorld]);
                ap.Assign(Signs.Roles["CloseTether1"], _state.GetActorById(_tethers.ElementAt(0).Item1));
                ap.Assign(Signs.Roles["CloseTether2"], _state.GetActorById(_tethers.ElementAt(0).Item2));
                ap.Assign(Signs.Roles["CloseTether3"], _state.GetActorById(_tethers.ElementAt(1).Item1));
                ap.Assign(Signs.Roles["CloseTether4"], _state.GetActorById(_tethers.ElementAt(1).Item2));
                _state.ExecuteAutomarkers(ap, Timing);
            }
        }

        #endregion

        #region DynamisSigmaAM

        public class DynamisSigmaAM : Automarker
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

            private Dictionary<uint, int> _dynamisStacks = new Dictionary<uint, int>();
            private Dictionary<uint, uint> _debuffs = new Dictionary<uint, uint>();

            public DynamisSigmaAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Prio = new AutomarkerPrio() { Priority = AutomarkerPrio.PrioTypeEnum.PartyListOrder };
                Timing = new AutomarkerTiming()
                {
                    TimingType = AutomarkerTiming.TimingTypeEnum.Explicit,
                    Parent = state.cfg.DefaultAutomarkerTiming,
                    IniDelayMin = 28.0f,
                    IniDelayMax = 32.0f,
                    SubDelayMin = state.cfg.AutomarkerSubDelayMin,
                    SubDelayMax = state.cfg.AutomarkerSubDelayMax
                };
                SetupPresets();
                Signs.ApplyPreset("LPDU");
                Test = new System.Action(() => Signs.TestFunctionality(state, Prio, Timing, SelfMarkOnly, AsSoftmarker));
            }

            private void SetupPresets()
            {
                AutomarkerSigns.Preset pr;
                pr = new AutomarkerSigns.Preset() { Builtin = true };
                pr.Name = "LPDU";
                pr.Roles["Arm1"] = AutomarkerSigns.SignEnum.Bind1;
                pr.Roles["Arm2"] = AutomarkerSigns.SignEnum.Bind2;
                pr.Roles["DistantWorld"] = AutomarkerSigns.SignEnum.Plus;
                pr.Roles["NearWorld"] = AutomarkerSigns.SignEnum.Triangle;
                pr.Roles["DistantFarBait"] = AutomarkerSigns.SignEnum.Attack1;
                pr.Roles["DistantCloseBait"] = AutomarkerSigns.SignEnum.Attack4;
                pr.Roles["NearBait1"] = AutomarkerSigns.SignEnum.Attack2;
                pr.Roles["NearBait2"] = AutomarkerSigns.SignEnum.Attack3;
                Signs.AddPreset(pr);
                pr = new AutomarkerSigns.Preset() { Builtin = true };
                pr.Name = "ElementalDC";
                pr.Roles["Arm1"] = AutomarkerSigns.SignEnum.Attack1;
                pr.Roles["Arm2"] = AutomarkerSigns.SignEnum.Attack2;
                pr.Roles["DistantWorld"] = AutomarkerSigns.SignEnum.Ignore2;
                pr.Roles["NearWorld"] = AutomarkerSigns.SignEnum.Ignore1;
                pr.Roles["DistantFarBait"] = AutomarkerSigns.SignEnum.Attack3;
                pr.Roles["DistantCloseBait"] = AutomarkerSigns.SignEnum.Bind1;
                pr.Roles["NearBait1"] = AutomarkerSigns.SignEnum.Bind2;
                pr.Roles["NearBait2"] = AutomarkerSigns.SignEnum.Bind3;
                Signs.AddPreset(pr);
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _dynamisStacks.Clear();
                _debuffs.Clear();
            }

            internal void FeedStatus(uint actorId, uint statusId, int stacks)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1:X} with {2} stacks", statusId, actorId, stacks);
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
                if (_debuffs.Count != 2)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Ready for automarkers");
                AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                Party pty = _state.GetPartyMembers();
                uint distant = _debuffs[StatusDistantWorld];
                uint near = _debuffs[StatusNearWorld];
                ap.Assign(Signs.Roles["DistantWorld"], _state.GetActorById(distant));
                ap.Assign(Signs.Roles["NearWorld"], _state.GetActorById(near));
                var pplWithStack = (from ix in _dynamisStacks
                                    where ix.Value == 1
                                    && ix.Key != distant
                                    && ix.Key != near
                                    select ix.Key).ToList();
                List<Party.PartyMember> pplWithStackGo = pty.GetByActorIds(pplWithStack);
                Prio.SortByPriority(pplWithStackGo);
                ap.Assign(Signs.Roles["Arm1"], pplWithStackGo[0].GameObject);
                ap.Assign(Signs.Roles["Arm2"], pplWithStackGo[1].GameObject);
                ap.Assign(Signs.Roles["DistantFarBait"], pplWithStackGo[2].GameObject);
                var theRest = (from ix in pty.Members
                               where ix.ObjectId != pplWithStackGo[0].ObjectId
                               && ix.ObjectId != pplWithStackGo[1].ObjectId
                               && ix.ObjectId != pplWithStackGo[2].ObjectId
                               && ix.ObjectId != distant
                               && ix.ObjectId != near
                               select ix).Take(3).ToList();
                Prio.SortByPriority(theRest);
                ap.Assign(Signs.Roles["DistantCloseBait"], theRest[0].GameObject);
                ap.Assign(Signs.Roles["NearBait1"], theRest[1].GameObject);
                ap.Assign(Signs.Roles["NearBait2"], theRest[2].GameObject);
                _state.ExecuteAutomarkers(ap, Timing);
                _debuffs.Clear();
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
            public System.Action Test { get; set; }

            public uint _currentAction = 0;

            public DynamisOmegaDrawBossMonitor(State state) : base(state)
            {
                Enabled = false;
                Test = new System.Action(() => TestFunctionality());
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
                Vector3 v1 = _state.plug._ui.TranslateToScreen(t1.X, t1.Y, t1.Z);
                Vector3 v2 = _state.plug._ui.TranslateToScreen(t2.X, t2.Y, t2.Z);
                Vector3 v3 = _state.plug._ui.TranslateToScreen(t3.X, t3.Y, t3.Z);
                Vector3 v4 = _state.plug._ui.TranslateToScreen(t4.X, t4.Y, t4.Z);
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

        public class DynamisOmegaAM : Automarker
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

            private Dictionary<uint, int> _dynamisStacks = new Dictionary<uint, int>();
            private List<uint> _distants = new List<uint>();
            private List<uint> _nears = new List<uint>();
            private List<uint> _firsts = new List<uint>();
            private List<uint> _seconds = new List<uint>();

            internal AutomarkerPayload SecondPayload;

            public DynamisOmegaAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Prio = new AutomarkerPrio() { Priority = AutomarkerPrio.PrioTypeEnum.PartyListOrder };
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                SetupPresets();
                Signs.ApplyPreset("LPDU");
                Test = new System.Action(() => Signs.TestFunctionality(state, Prio, Timing, SelfMarkOnly, AsSoftmarker));
            }

            private void SetupPresets()
            {
                AutomarkerSigns.Preset pr = new AutomarkerSigns.Preset() { Builtin = true };
                pr.Name = "LPDU";
                pr.Roles["Monitor1"] = AutomarkerSigns.SignEnum.Bind1;
                pr.Roles["Monitor2"] = AutomarkerSigns.SignEnum.Bind2;
                pr.Roles["DistantWorld"] = AutomarkerSigns.SignEnum.Plus;
                pr.Roles["NearWorld"] = AutomarkerSigns.SignEnum.Triangle;
                pr.Roles["Bait1"] = AutomarkerSigns.SignEnum.Attack1;
                pr.Roles["Bait2"] = AutomarkerSigns.SignEnum.Attack2;
                pr.Roles["Bait3"] = AutomarkerSigns.SignEnum.Attack3;
                pr.Roles["Bait4"] = AutomarkerSigns.SignEnum.Attack4;
                Signs.AddPreset(pr);
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _dynamisStacks.Clear();
                _distants.Clear();
                _nears.Clear();
                _firsts.Clear();
                _seconds.Clear();
            }

            internal void FeedStatus(uint actorId, uint statusId, int stacks)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1:X} with {2} stacks", statusId, actorId, stacks);
                if (statusId == StatusDynamis)
                {
                    _dynamisStacks[actorId] = stacks;
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
                }
                if (_distants.Count != 2 || _nears.Count != 2 || _firsts.Count != 2 || _seconds.Count != 2)
                {
                    return;
                }
                ReadyForDecision();
            }

            internal void ReadyForDecision()
            {
                Log(State.LogLevelEnum.Debug, null, "Ready for automarkers");
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
                         where ix.Value >= 2
                         && ix.Key != distant1st && ix.Key != near1st
                         && ix.Key != distant2nd && ix.Key != near2nd
                         orderby ix.Value descending
                         select ix.Key).Take(2 - firstMonitorsSelection.Count)
                    );
                }
                Party pty = _state.GetPartyMembers();
                AutomarkerPayload ap1 = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                List<Party.PartyMember> firstMonitorsSelectionGo = pty.GetByActorIds(firstMonitorsSelection);
                Prio.SortByPriority(firstMonitorsSelectionGo);
                ap1.Assign(Signs.Roles["Monitor1"], firstMonitorsSelectionGo[0].GameObject);
                ap1.Assign(Signs.Roles["Monitor2"], firstMonitorsSelectionGo[1].GameObject);
                ap1.Assign(Signs.Roles["DistantWorld"], _state.GetActorById(distant1st));
                ap1.Assign(Signs.Roles["NearWorld"], _state.GetActorById(near1st));
                var theRest1 = (from ix in _dynamisStacks
                                where firstMonitorsSelection.Contains(ix.Key) == false
                                && ix.Key != distant1st
                                && ix.Key != near1st
                                select ix.Key).Take(4).ToList();
                List<Party.PartyMember> theRest1Go = pty.GetByActorIds(theRest1);
                Prio.SortByPriority(theRest1Go);
                ap1.Assign(Signs.Roles["Bait1"], theRest1Go[0].GameObject);
                ap1.Assign(Signs.Roles["Bait2"], theRest1Go[1].GameObject);
                ap1.Assign(Signs.Roles["Bait3"], theRest1Go[2].GameObject);
                ap1.Assign(Signs.Roles["Bait4"], theRest1Go[3].GameObject);
                _dynamisStacks[distant1st] = _dynamisStacks[distant1st] + 1;
                _dynamisStacks[near1st] = _dynamisStacks[near1st] + 1;
                _dynamisStacks[theRest1[0]] = _dynamisStacks[theRest1[0]] + 1;
                _dynamisStacks[theRest1[1]] = _dynamisStacks[theRest1[1]] + 1;
                _dynamisStacks[theRest1[2]] = _dynamisStacks[theRest1[2]] + 1;
                _dynamisStacks[theRest1[3]] = _dynamisStacks[theRest1[3]] + 1;
                AutomarkerPayload ap2 = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                var threeStacks = (from ix in _dynamisStacks
                                   where ix.Value == 3
                                   select ix.Key).Take(2).ToList();
                Log(State.LogLevelEnum.Debug, null, "SET 1 -- m1 {0:X} m2 {1:X} distant {2:X} near {3:X} spreads {4:X} {5:X} {6:X} {7:X}",
                    firstMonitorsSelection[0], firstMonitorsSelection[1], distant1st, near1st, theRest1[0], theRest1[1], theRest1[2], theRest1[3]
                );
                List<Party.PartyMember> threeStacksGo = pty.GetByActorIds(threeStacks);
                Prio.SortByPriority(threeStacksGo);
                ap2.Assign(Signs.Roles["Monitor1"], threeStacksGo[0].GameObject);
                ap2.Assign(Signs.Roles["Monitor2"], threeStacksGo[1].GameObject);
                ap2.Assign(Signs.Roles["DistantWorld"], _state.GetActorById(distant2nd));
                ap2.Assign(Signs.Roles["NearWorld"], _state.GetActorById(near2nd));
                var theRest2 = (from ix in _dynamisStacks
                                where threeStacks.Contains(ix.Key) == false
                                && ix.Key != distant2nd
                                && ix.Key != near2nd
                                select ix.Key).Take(4).ToList();
                List<Party.PartyMember> theRest2Go = pty.GetByActorIds(theRest2);
                Prio.SortByPriority(theRest2Go);
                ap2.Assign(Signs.Roles["Bait1"], theRest2Go[0].GameObject);
                ap2.Assign(Signs.Roles["Bait2"], theRest2Go[1].GameObject);
                ap2.Assign(Signs.Roles["Bait3"], theRest2Go[2].GameObject);
                ap2.Assign(Signs.Roles["Bait4"], theRest2Go[3].GameObject);
                Log(State.LogLevelEnum.Debug, null, "SET 2 -- m1 {0:X} m2 {1:X} distant {2:X} near {3:X} spreads {4:X} {5:X} {6:X} {7:X}",
                    threeStacks[0], threeStacks[1], distant2nd, near2nd, theRest2[0], theRest2[1], theRest2[2], theRest2[3]
                );
                SecondPayload = ap2;
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
            lock (this)
            {
                if (_subbed == true)
                {
                    return;
                }
                _subbed = true;
                Log(LogLevelEnum.Debug, null, "Subscribing to events");
                _state.OnCastBegin += OnCastBegin;
                _state.OnAction += OnAction;
                _state.OnStatusChange += OnStatusChange;
                _state.OnTether += OnTether;
                _state.OnHeadMarker += OnHeadMarker;
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
            if (CurrentPhase == PhaseEnum.P1_Pantokrator)
            {
                _p2synergyAm.FeedHeadmarker(dest, realMarkerId);
            }
        }

        private void OnStatusChange(uint src, uint dest, uint statusId, bool gained, float duration, int stacks)
        {
            if (statusId == StatusMidGlitch || statusId == StatusRemoteGlitch)
            {
#if !SANS_GOETIA
                _glitchTether.FeedStatus(gained == false ? 0 : statusId);
#endif
                if (CurrentPhase == PhaseEnum.P1_Pantokrator && gained == true)
                {
                    _p2synergyAm.FeedStatus(statusId);
                }
            }
            if (statusId == StatusStackSniper || statusId == StatusSpreadSniper)
            {
                _p3transAm.FeedStatus(dest, statusId, gained);
            }
            if (statusId == StatusMonitorLeft || statusId == StatusMonitorRight)
            {
                _p3moniAm.FeedStatus(dest, statusId, gained);
            }
            if (gained == true && statusId == StatusDynamis)
            {
                _sigmaAm.FeedStatus(dest, statusId, stacks);
                _omegaAm.FeedStatus(dest, statusId, stacks);
                return;
            }
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
                    if (gained == true && (statusId == StatusDistantWorld || statusId == StatusNearWorld))
                    {
                        _deltaAm.FeedStatus(dest, statusId);
                    }
                    break;
                case PhaseEnum.P5_Sigma:
                    if (gained == true && (statusId == StatusDistantWorld || statusId == StatusNearWorld))
                    {
                        _sigmaAm.FeedStatus(dest, statusId, stacks);
                    }
                    break;
                case PhaseEnum.P5_Omega:
                    if (gained == true && (statusId == StatusDistantWorld || statusId == StatusNearWorld || statusId == StatusInLine1 || statusId == StatusInLine2))
                    {
                        _omegaAm.FeedStatus(dest, statusId, stacks);
                    }
                    break;
            }
        }

        private void OnCastBegin(uint src, uint dest, ushort actionId, float castTime, float rotation)
        {
            switch (actionId)
            {
                case AbilityProgramLoop:
                    CurrentPhase = PhaseEnum.P1_ProgramLoop;
                    break;
                case AbilityPantokrator:
                    CurrentPhase = PhaseEnum.P1_Pantokrator;
                    break;
                case AbilityLaserShower:
                    CurrentPhase = PhaseEnum.P3_Transition;
#if !SANS_GOETIA
                    _chibiOmega.StartLooking(120.0f);
#endif
                    break;
                case AbilityDynamisDelta:
                    CurrentPhase = PhaseEnum.P5_Delta;
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
                    }
                    break;
                case AbilityBossMonitorEast:
                case AbilityBossMonitorWest:
                    if (CurrentPhase == PhaseEnum.P3_Transition)
                    {
#if !SANS_GOETIA
                        _hwMonitor.FeedAction(0);
#endif
                    }
                    break;
                case AbilityHelloDistantWorldBig:
                    if (CurrentPhase == PhaseEnum.P5_Delta && _deltaAm.Active == true)
                    {
                        _state.ClearAutoMarkers();
                    }
                    if (CurrentPhase == PhaseEnum.P5_Sigma && _sigmaAm.Active == true)
                    {
                        _state.ClearAutoMarkers();
                    }
                    if (CurrentPhase == PhaseEnum.P5_Omega2nd && _omegaAm.Active == true)
                    {
                        _state.ClearAutoMarkers();
                    }
                    if (CurrentPhase == PhaseEnum.P5_Omega)
                    {
                        if (_omegaAm.Active == true)
                        {
                            _state.ExecuteAutomarkers(_omegaAm.SecondPayload, _state.cfg.DefaultAutomarkerTiming);
                        }
                        CurrentPhase = PhaseEnum.P5_Omega2nd;
                    }
                    break;
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
            if (tetherId == 200)
            {
                _deltaAm.FeedTether(src, dest);
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
                _state.OnHeadMarker -= OnHeadMarker;
                _state.OnTether -= OnTether;
                _state.OnStatusChange -= OnStatusChange;
                _state.OnAction -= OnAction;
                _state.OnCastBegin -= OnCastBegin;
                _subbed = false;
            }
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
            // normal modes included for some easier testing
            bool newZoneOk = (newZone == 800 || newZone == 804 || newZone == 1122);
            if (newZoneOk == true && ZoneOk == false)
            {
                Log(State.LogLevelEnum.Info, null, "Content available");
                _sawFirstHeadMarker = false;
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
                _p2synergyAm = (P2SynergyAM)Items["P2SynergyAM"];
                _p3transAm = (P3TransitionAM)Items["P3TransitionAM"];
                _p3moniAm = (P3MonitorAM)Items["P3MonitorAM"];
                _deltaAm = (DynamisDeltaAM)Items["DynamisDeltaAM"];
                _sigmaAm = (DynamisSigmaAM)Items["DynamisSigmaAM"];
                _omegaAm = (DynamisOmegaAM)Items["DynamisOmegaAM"];
                _state.OnCombatChange += OnCombatChange;
                _state.OnDirectorUpdate += OnDirectorUpdate;
                LogItems();
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
