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
        private const uint StatusMidGlitch = 0xD63;
        private const uint StatusRemoteGlitch = 0xD64;
        private const uint StatusStackSniper = 0xD62;
        private const uint StatusSpreadSniper = 0xD61;

        private const int HeadmarkerCircle = 0x1a0;
        private const int HeadmarkerSquare = 0x1a2;
        private const int HeadmarkerCross = 0x1a3;
        private const int HeadmarkerTriangle = 0x1a1;
        private const int HeadmarkerTarget = 0xf4;

        private bool ZoneOk = false;

        private ChibiOmega _chibiOmega;
        private GlitchTether _glitchTether;
        private ProgramLoopAM _loopAm;
        private PantokratorAM _pantoAm;
        private P3TransitionAM _p3transAm;
        private DynamisDeltaAM _deltaAm;
        private DynamisSigmaAM _sigmaAm;
        private DynamisOmegaAM _omegaAm;
        private DynamisDeltaDrawBossMonitor _deltaMonitor;
        private DynamisOmegaDrawBossMonitor _omegaMonitor;

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

        private PhaseEnum CurrentPhase { get; set; } = PhaseEnum.P1_Start;

        #region ChibiOmega

        public class ChibiOmega : Core.ContentItem
        {

            public override FeaturesEnum Features => FeaturesEnum.Hack;

            private bool _lookingForOmega = false;
            private DateTime _omegaFound = DateTime.MinValue;

            public ChibiOmega(State state) : base(state)
            {
                Enabled = false;
            }

            public void StartLooking()
            {
                _omegaFound = DateTime.MinValue;
                _lookingForOmega = true;
            }

            public void StopLooking()
            {
                _lookingForOmega = false;
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
                if (_omegaFound != DateTime.MinValue && DateTime.Now > _omegaFound.AddSeconds(7))
                {
                    StopLooking();
                    return;
                }
                foreach (GameObject go in _state.ot)
                {
                    if (go.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc && go is Character)
                    {
                        Character bc = (Character)go;
                        CharacterStruct* bcs = (CharacterStruct*)bc.Address;
                        if (bcs->ModelCharaId == 327 || (bcs->ModelCharaId == 3771 && bcs->Health == 8557964))
                        {
                            GameObjectStruct* gos = (GameObjectStruct*)go.Address;
                            bcs->ModelScale = 0.1f;
                            gos->Scale = 0.1f;
                            if (_omegaFound == DateTime.MinValue)
                            {
                                _state.Log(State.LogLevelEnum.Debug, null, "Omega found, ensmollening");
                                _omegaFound = DateTime.Now;
                            }
                            return;
                        }
                    }
                }
            }

        }

        #endregion

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
            [AttributeOrderNumber(1001)]
            public AutomarkerPrio Prio { get; set; }

            [DebugOption]
            [AttributeOrderNumber(2000)]
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
                Signs.SetRole("Tower1", AutomarkerSigns.SignEnum.Attack1, false);
                Signs.SetRole("Tower2", AutomarkerSigns.SignEnum.Attack2, false);
                Signs.SetRole("Tether1", AutomarkerSigns.SignEnum.Ignore1, false);
                Signs.SetRole("Tether2", AutomarkerSigns.SignEnum.Ignore2, false);
                Test = new Action(() => Signs.TestFunctionality(state, null));
            }

            internal void Reset()
            {
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
                _state.Log(State.LogLevelEnum.Debug, null, "Sending marker set {0}", index);
                switch (index)
                {
                    case 1:
                        {
                            AutomarkerPayload ap = new AutomarkerPayload();
                            ap.assignments[Signs.Roles["Tether1"]] = _thirdGo[0].GameObject;
                            ap.assignments[Signs.Roles["Tether2"]] = _thirdGo[1].GameObject;
                            ap.assignments[Signs.Roles["Tower1"]] = _firstGo[0].GameObject;
                            ap.assignments[Signs.Roles["Tower2"]] = _firstGo[1].GameObject;
                            _state.ExecuteAutomarkers(ap);
                        }
                        break;
                    case 2:
                        {
                            AutomarkerPayload ap = new AutomarkerPayload();
                            ap.assignments[Signs.Roles["Tether1"]] = _fourthGo[0].GameObject;
                            ap.assignments[Signs.Roles["Tether2"]] = _fourthGo[1].GameObject;
                            ap.assignments[Signs.Roles["Tower1"]] = _secondGo[0].GameObject;
                            ap.assignments[Signs.Roles["Tower2"]] = _secondGo[1].GameObject;
                            _state.ExecuteAutomarkers(ap);
                        }
                        break;
                    case 3:
                        {
                            AutomarkerPayload ap = new AutomarkerPayload();
                            ap.assignments[Signs.Roles["Tether1"]] = _firstGo[0].GameObject;
                            ap.assignments[Signs.Roles["Tether2"]] = _firstGo[1].GameObject;
                            ap.assignments[Signs.Roles["Tower1"]] = _thirdGo[0].GameObject;
                            ap.assignments[Signs.Roles["Tower2"]] = _thirdGo[1].GameObject;
                            _state.ExecuteAutomarkers(ap);
                        }
                        break;
                    case 4:
                        {
                            AutomarkerPayload ap = new AutomarkerPayload();
                            ap.assignments[Signs.Roles["Tether1"]] = _secondGo[0].GameObject;
                            ap.assignments[Signs.Roles["Tether2"]] = _secondGo[1].GameObject;
                            ap.assignments[Signs.Roles["Tower1"]] = _fourthGo[0].GameObject;
                            ap.assignments[Signs.Roles["Tower2"]] = _fourthGo[1].GameObject;
                            _state.ExecuteAutomarkers(ap);
                        }
                        break;
                    case 5:
                        {
                            AutomarkerPayload ap = new AutomarkerPayload();
                            ap.Clear = true;
                            _state.ExecuteAutomarkers(ap);
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
                            _state.Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1}", statusId, actorId);
                            _first.Add(actorId);
                            _symbols.Add(actorId);
                            break;
                        case StatusInLine2:
                            _state.Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1}", statusId, actorId);
                            _second.Add(actorId);
                            _symbols.Add(actorId);
                            break;
                        case StatusInLine3:
                            _state.Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1}", statusId, actorId);
                            _third.Add(actorId);
                            _symbols.Add(actorId);
                            break;
                        case StatusInLine4:
                            _state.Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1}", statusId, actorId);
                            _fourth.Add(actorId);
                            _symbols.Add(actorId);
                            break;
                    }
                    if (_symbols.Count == 8 && _fired == false)
                    {
                        _state.Log(State.LogLevelEnum.Debug, null, "All statuses registered, ready for automarkers");
                        PerformDecision();
                    }
                }
                else
                {
                    switch (statusId)
                    {
                        case StatusInLine1:
                            _state.Log(State.LogLevelEnum.Debug, null, "Lost status {0}", statusId);
                            SendMarkers(2);
                            break;
                        case StatusInLine2:
                            _state.Log(State.LogLevelEnum.Debug, null, "Lost status {0}", statusId);
                            SendMarkers(3);
                            break;
                        case StatusInLine3:
                            _state.Log(State.LogLevelEnum.Debug, null, "Lost status {0}", statusId);
                            SendMarkers(4);
                            break;
                        case StatusInLine4:
                            _state.Log(State.LogLevelEnum.Debug, null, "Lost status {0}", statusId);
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
            [AttributeOrderNumber(2000)]
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
                Signs = new AutomarkerSigns();
                Signs.SetRole("Beam1", AutomarkerSigns.SignEnum.Attack1, false);
                Signs.SetRole("Beam2", AutomarkerSigns.SignEnum.Attack2, false);
                Signs.SetRole("Missile1", AutomarkerSigns.SignEnum.Ignore1, false);
                Signs.SetRole("Missile2", AutomarkerSigns.SignEnum.Ignore2, false);
                Test = new Action(() => Signs.TestFunctionality(state, null));
            }

            internal void Reset()
            {
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
                _state.Log(State.LogLevelEnum.Debug, null, "Sending marker set {0}", index);
                switch (index)
                {
                    case 1:
                        {
                            AutomarkerPayload ap = new AutomarkerPayload();
                            ap.assignments[Signs.Roles["Beam1"]] = _thirdGo[0].GameObject;
                            ap.assignments[Signs.Roles["Beam2"]] = _thirdGo[1].GameObject;
                            ap.assignments[Signs.Roles["Missile1"]] = _firstGo[0].GameObject;
                            ap.assignments[Signs.Roles["Missile2"]] = _firstGo[1].GameObject;
                            _state.ExecuteAutomarkers(ap);
                        }
                        break;
                    case 2:
                        {
                            AutomarkerPayload ap = new AutomarkerPayload();
                            ap.assignments[Signs.Roles["Beam1"]] = _fourthGo[0].GameObject;
                            ap.assignments[Signs.Roles["Beam2"]] = _fourthGo[1].GameObject;
                            ap.assignments[Signs.Roles["Missile1"]] = _secondGo[0].GameObject;
                            ap.assignments[Signs.Roles["Missile2"]] = _secondGo[1].GameObject;
                            _state.ExecuteAutomarkers(ap);
                        }
                        break;
                    case 3:
                        {
                            AutomarkerPayload ap = new AutomarkerPayload();
                            ap.assignments[Signs.Roles["Beam1"]] = _firstGo[0].GameObject;
                            ap.assignments[Signs.Roles["Beam2"]] = _firstGo[1].GameObject;
                            ap.assignments[Signs.Roles["Missile1"]] = _thirdGo[0].GameObject;
                            ap.assignments[Signs.Roles["Missile2"]] = _thirdGo[1].GameObject;
                            _state.ExecuteAutomarkers(ap);
                        }
                        break;
                    case 4:
                        {
                            AutomarkerPayload ap = new AutomarkerPayload();
                            ap.assignments[Signs.Roles["Beam1"]] = _secondGo[0].GameObject;
                            ap.assignments[Signs.Roles["Beam2"]] = _secondGo[1].GameObject;
                            ap.assignments[Signs.Roles["Missile1"]] = _fourthGo[0].GameObject;
                            ap.assignments[Signs.Roles["Missile2"]] = _fourthGo[1].GameObject;
                            _state.ExecuteAutomarkers(ap);
                        }
                        break;
                    case 5:
                        {
                            AutomarkerPayload ap = new AutomarkerPayload();
                            ap.Clear = true;
                            _state.ExecuteAutomarkers(ap);
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
                            _state.Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1}", statusId, actorId);
                            _first.Add(actorId);
                            _symbols.Add(actorId);
                            break;
                        case StatusInLine2:
                            _state.Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1}", statusId, actorId);
                            _second.Add(actorId);
                            _symbols.Add(actorId);
                            break;
                        case StatusInLine3:
                            _state.Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1}", statusId, actorId);
                            _third.Add(actorId);
                            _symbols.Add(actorId);
                            break;
                        case StatusInLine4:
                            _state.Log(State.LogLevelEnum.Debug, null, "Registered status {0} on {1}", statusId, actorId);
                            _fourth.Add(actorId);
                            _symbols.Add(actorId);
                            break;
                    }
                    if (_symbols.Count == 8 && _fired == false)
                    {
                        _state.Log(State.LogLevelEnum.Debug, null, "All statuses registered, ready for automarkers");
                        PerformDecision();
                    }
                }
                else
                {
                    switch (statusId)
                    {
                        case StatusInLine1:
                            _state.Log(State.LogLevelEnum.Debug, null, "Lost status {0}", statusId);
                            SendMarkers(2);
                            break;
                        case StatusInLine2:
                            _state.Log(State.LogLevelEnum.Debug, null, "Lost status {0}", statusId);
                            SendMarkers(3);
                            break;
                        case StatusInLine3:
                            _state.Log(State.LogLevelEnum.Debug, null, "Lost status {0}", statusId);
                            SendMarkers(4);
                            break;
                        case StatusInLine4:
                            _state.Log(State.LogLevelEnum.Debug, null, "Lost status {0}", statusId);
                            SendMarkers(5);
                            break;
                    }
                }
            }

        }

        #endregion

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

            private List<uint> _symbolSquare = new List<uint>();
            private List<uint> _symbolTriangle = new List<uint>();
            private List<uint> _symbolCircle = new List<uint>();
            private List<uint> _symbolCross = new List<uint>();
            private List<uint> _symbols = new List<uint>();

            private uint _partnerId = 0;
            private uint _currentDebuff = 0;
            private int _myHeadmarker = 0;

            public GlitchTether(State state) : base(state)
            {
                Test = new Action(() => TestFunctionality());
            }

            public void Reset()
            {
                _symbolSquare.Clear();
                _symbolTriangle.Clear();
                _symbolCircle.Clear();
                _symbolCross.Clear();
                _partnerId = 0;
                _currentDebuff = 0;
                _myHeadmarker = 0;
            }

            public void FeedHeadmarker(uint actorId, int headMarkerId)
            {
                uint myid = _state.cs.LocalPlayer.ObjectId;
                if (myid == actorId)
                {
                    _myHeadmarker = headMarkerId;
                }
                _symbols.Add(actorId);
                switch (headMarkerId)
                {
                    case HeadmarkerCircle:
                        _symbolCircle.Add(actorId);
                        break;
                    case HeadmarkerCross:
                        _symbolCross.Add(actorId);
                        break;
                    case HeadmarkerTriangle:
                        _symbolTriangle.Add(actorId);
                        break;
                    case HeadmarkerSquare:
                        _symbolSquare.Add(actorId);
                        break;
                }
                PerformDecision();
            }

            public void FeedStatus(uint statusId)
            {
                _currentDebuff = statusId;
                PerformDecision();
            }

            private void PerformDecision()
            {
                if (_currentDebuff == 0 || _symbols.Count != 8)
                {
                    return;
                }
                uint myid = _state.cs.LocalPlayer.ObjectId;
                switch (_myHeadmarker)
                {
                    case HeadmarkerCircle:
                        _partnerId = (from ix in _symbolCircle where ix != myid select ix).FirstOrDefault();
                        break;
                    case HeadmarkerCross:
                        _partnerId = (from ix in _symbolCross where ix != myid select ix).FirstOrDefault();
                        break;
                    case HeadmarkerTriangle:
                        _partnerId = (from ix in _symbolTriangle where ix != myid select ix).FirstOrDefault();
                        break;
                    case HeadmarkerSquare:
                        _partnerId = (from ix in _symbolSquare where ix != myid select ix).FirstOrDefault();
                        break;
                }
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
                    _state.Log(State.LogLevelEnum.Debug, null, "Testing from {0} to {1}", me, go);
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

        #region DynamisOmegaAM

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
            [AttributeOrderNumber(1001)]
            public AutomarkerPrio Prio { get; set; }

            [DebugOption]
            [AttributeOrderNumber(2000)]
            public Action Test { get; set; }

            private List<uint> _stacks = new List<uint>();
            private List<uint> _spreads = new List<uint>();
            private bool _signs = false;

            public P3TransitionAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Prio = new AutomarkerPrio();
                Prio.Priority = AutomarkerPrio.PrioTypeEnum.CongaX;
                Signs.SetRole("Stack1_1", AutomarkerSigns.SignEnum.Bind1, false);
                Signs.SetRole("Stack1_2", AutomarkerSigns.SignEnum.Ignore1, false);
                Signs.SetRole("Stack2_1", AutomarkerSigns.SignEnum.Bind2, false);
                Signs.SetRole("Stack2_2", AutomarkerSigns.SignEnum.Ignore2, false);
                Signs.SetRole("Spread1", AutomarkerSigns.SignEnum.Attack1, false);
                Signs.SetRole("Spread2", AutomarkerSigns.SignEnum.Attack2, false);
                Signs.SetRole("Spread3", AutomarkerSigns.SignEnum.Attack3, false);
                Signs.SetRole("Spread4", AutomarkerSigns.SignEnum.Attack4, false);
                Test = new Action(() => Signs.TestFunctionality(state, null));
            }

            internal void Reset()
            {
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
                            _signs = false;
                            AutomarkerPayload ap = new AutomarkerPayload();
                            ap.Clear = true;
                            _state.ExecuteAutomarkers(ap);
                        }
                        break;
                    case StatusStackSniper:
                        _stacks.Add(actorId);
                        break;
                    case StatusSpreadSniper:
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
                _signs = true;
            }

        }

        #endregion

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
            [AttributeOrderNumber(2000)]
            public Action Test { get; set; } 

            private Dictionary<uint, GameObject> _debuffs = new Dictionary<uint, GameObject>();

            public DynamisDeltaAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                SetupPresets();
                Signs.ApplyPreset("LPDU");
                Test = new Action(() => Signs.TestFunctionality(state, null));
            }

            private void SetupPresets()
            {
                Dictionary<string, AutomarkerSigns.SignEnum> pr;
                pr = new Dictionary<string, AutomarkerSigns.SignEnum>();
                pr["DistantWorld"] = AutomarkerSigns.SignEnum.Plus;
                pr["NearWorld"] = AutomarkerSigns.SignEnum.Triangle;
                Signs.Presets["LPDU"] = pr;
            }

            internal void Reset()
            {
                _debuffs.Clear();
            }

            internal void FeedStatus(uint actorId, uint statusId, float duration, int stacks)
            {
                if (Active == false || (statusId != StatusDistantWorld && statusId != StatusNearWorld))
                {
                    return;
                }
                _debuffs[statusId] = _state.GetActorById(actorId);
                if (_debuffs.Count == 2)
                {
                    AutomarkerPayload ap = new AutomarkerPayload();
                    ap.assignments[Signs.Roles["DistantWorld"]] = _debuffs[StatusDistantWorld];
                    ap.assignments[Signs.Roles["NearWorld"]] = _debuffs[StatusNearWorld];
                    _state.ExecuteAutomarkers(ap);
                }
            }

        }

        #endregion

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
                    if (pos.X > 90.0f)
                    {
                        omegaPos = DirectionsEnum.East;
                    }
                    if (pos.Y < 90.0f)
                    {
                        omegaPos = DirectionsEnum.North;
                    }
                    if (pos.Y > 90.0f)
                    {
                        omegaPos = DirectionsEnum.South;
                    }
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
                        monitors = _currentAction == AbilityBossMonitorDeltaLeft ? DirectionsEnum.North : DirectionsEnum.South;
                        break;
                    case DirectionsEnum.West:
                        monitors = _currentAction == AbilityBossMonitorDeltaLeft ? DirectionsEnum.South : DirectionsEnum.North;
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
            [AttributeOrderNumber(2000)]
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
                SetupPresets();
                Signs.ApplyPreset("LPDU");
                Test = new Action(() => Signs.TestFunctionality(state, null));
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
            }

            internal void Reset()
            {
                _dynamisStacks.Clear();
                _debuffs.Clear();
                _psMarkers.Clear();
                _psCross.Clear();
                _psSquare.Clear();
                _psTriangle.Clear();
                _psCircle.Clear();
                _wavecannons.Clear();
            }

            internal void FeedHeadmarker(uint actorId, int headMarkerId)
            {
                if (Active == false)
                {
                    return;
                }
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
                    return;
                }
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
                ap.assignments[Signs.Roles["Arm1"]] = _state.GetActorById(theRest[0]);
                ap.assignments[Signs.Roles["Arm2"]] = _state.GetActorById(theRest[1]);
                ap.assignments[Signs.Roles["DistantFarBait"]] = _state.GetActorById(theRest[2]);
                _state.ExecuteAutomarkers(ap);
            }

        }

        #endregion

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
            [AttributeOrderNumber(2000)]
            public Action Test { get; set; }

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
                SetupPresets();
                Signs.ApplyPreset("LPDU");
                Test = new Action(() => Signs.TestFunctionality(state, null));
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
                _dynamisStacks.Clear();
                _distants.Clear();
                _nears.Clear();
                _firsts.Clear();
                _seconds.Clear();
            }

            internal void FeedStatus(uint actorId, uint statusId, float duration, int stacks)
            {
                if (Active == false)
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
                ReadyForDecision();
            }

            internal void ReadyForDecision()
            {
                if (_distants.Count != 2 || _nears.Count != 2 || _firsts.Count != 2 || _seconds.Count != 2)
                {
                    return;
                }
                List<uint> firstMonitorsSelection = new List<uint>();
                var distant2nd = (from s in _seconds join d in _distants on s equals d select s).FirstOrDefault();
                var near2nd = (from s in _seconds join d in _distants on s equals d select s).FirstOrDefault();
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
                        (from ix in _dynamisStacks where ix.Value == 2 && ix.Key != distant2nd && ix.Key != near2nd select ix.Key).Take(2 - firstMonitorsSelection.Count)
                    );
                }
                var distant1st = (from s in _seconds join d in _distants on s equals d select s).FirstOrDefault();
                var near1st = (from s in _seconds join d in _distants on s equals d select s).FirstOrDefault();
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
                _state.ExecuteAutomarkers(ap1);
            }

        }

        #endregion

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
                Test = new Action(() => TestFunctionality());
            }

            public void FeedAction(uint actionId)
            {
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
                    case AbilityBossMonitorWest:
                        x1 = 80.0f;
                        x2 = 100.0f;
                        break;
                    case AbilityBossMonitorEast:
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
        }

        private void OnStatusChange(uint src, uint dest, uint statusId, bool gained, float duration, int stacks)
        {
            if (statusId == StatusMidGlitch || statusId == StatusRemoteGlitch)
            {
                _glitchTether.FeedStatus(gained == false ? 0 : statusId);
            }
            if (statusId == StatusStackSniper || statusId == StatusSpreadSniper)
            {
                _p3transAm.FeedStatus(dest, gained == false ? 0 : statusId);
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
                    break;
                case AbilityDynamisDelta:
                    CurrentPhase = PhaseEnum.P5_Delta;
                    _deltaAm.Reset();
                    break;
                case AbilityDynamisSigma:
                    CurrentPhase = PhaseEnum.P5_Sigma;
                    _sigmaAm.Reset();
                    break;
                case AbilityDynamisOmega:
                    CurrentPhase = PhaseEnum.P5_Omega;
                    _omegaAm.Reset();
                    break;
                case AbilityBossMonitorDeltaLeft:
                case AbilityBossMonitorDeltaRight:
                    if (CurrentPhase == PhaseEnum.P5_Delta)
                    {
                        _deltaMonitor.FeedAction(dest, actionId);
                    }
                    break;
                case AbilityBossMonitorEast:
                case AbilityBossMonitorWest:
                    if (CurrentPhase == PhaseEnum.P5_Omega)
                    {
                        _omegaMonitor.FeedAction(actionId);
                    }
                    break;
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
                        _deltaMonitor.FeedAction(dest, 0);
                    }
                    break;
                case AbilityBossMonitorEast:
                case AbilityBossMonitorWest:
                    if (CurrentPhase == PhaseEnum.P5_Omega)
                    {
                        _omegaMonitor.FeedAction(0);
                        AutomarkerPayload ap = new AutomarkerPayload() { Clear = true };
                        _state.ExecuteAutomarkers(ap);
                        _state.ExecuteAutomarkers(_omegaAm.SecondPayload);
                    }
                    break;
                case AbilityHelloDistantWorldBig:
                    if (CurrentPhase == PhaseEnum.P5_Delta)
                    {
                        AutomarkerPayload ap = new AutomarkerPayload() { Clear = true };
                        _state.ExecuteAutomarkers(ap);
                    }
                    if (CurrentPhase == PhaseEnum.P5_Sigma)
                    {
                        AutomarkerPayload ap = new AutomarkerPayload() { Clear = true };
                        _state.ExecuteAutomarkers(ap);
                    }
                    break;
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
            bool newZoneOk = (newZone == 1122);
            if (newZoneOk == true && ZoneOk == false)
            {
                _state.Log(State.LogLevelEnum.Info, null, "Content {0} available", GetType().Name);
                _chibiOmega = (ChibiOmega)Items["ChibiOmega"];
                _chibiOmega.StartLooking();
                _loopAm = (ProgramLoopAM)Items["ProgramLoopAM"];
                _pantoAm = (PantokratorAM)Items["PantokratorAM"];
                _glitchTether = (GlitchTether)Items["GlitchTether"];
                _p3transAm = (P3TransitionAM)Items["P3TransitionAM"];
                _deltaAm = (DynamisDeltaAM)Items["DynamisDeltaAM"];
                _sigmaAm = (DynamisSigmaAM)Items["DynamisSigmaAM"];
                _omegaAm = (DynamisOmegaAM)Items["DynamisOmegaAM"];
                _deltaMonitor = (DynamisDeltaDrawBossMonitor)Items["DynamisDeltaDrawBossMonitor"];
                _omegaMonitor = (DynamisOmegaDrawBossMonitor)Items["DynamisOmegaDrawBossMonitor"];
                _state.OnCombatChange += OnCombatChange;
                _state.OnDirectorUpdate += OnDirectorUpdate;
            }
            else if (newZoneOk == false && ZoneOk == true)
            {
                _state.Log(State.LogLevelEnum.Info, null, "Content {0} unavailable", GetType().Name);
                _state.OnDirectorUpdate -= OnDirectorUpdate;
                _state.OnCombatChange -= OnCombatChange;
            }
            ZoneOk = newZoneOk;
        }

        private void OnDirectorUpdate(uint param1, uint param2, uint param3, uint param4)
        {
            if (param2 == 0x4000000F)
            {
                _chibiOmega.StartLooking();
            }
            if (param2 == 0x40000006)
            {
                _chibiOmega.StopLooking();
            }
        }

    }

}
