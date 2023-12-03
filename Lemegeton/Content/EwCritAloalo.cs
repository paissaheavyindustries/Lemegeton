using System;
using Lemegeton.Core;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using ImGuiNET;
using Dalamud.Game.ClientState.Objects.Types;
using System.Numerics;
using System.Security.Cryptography;
using Dalamud.Interface.Animation;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Lemegeton.Content.EwCritAloalo;
using System.Drawing;

namespace Lemegeton.Content
{

    internal class EwCritAloalo : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.None;
        
        private const int HeadmarkerLalaCW = 484;
        private const int HeadmarkerLalaCCW = 485;
        private const int HeadmarkerPlayerCW = 493;
        private const int HeadmarkerPlayerCCW = 494;
        private const int StatusBackUnseen = 3727;
        private const int StatusFrontUnseen = 3726;
        private const int StatusLeftUnseen = 3729;
        private const int StatusRightUnseen = 3728;
        private const int StatusLalaThree = 3938;
        private const int StatusLalaFive = 3939;
        private const int StatusPlayerThree = 3721;
        private const int StatusPlayerFive = 3790;
        private const int AbilityArcaneBlightN = 34956;
        private const int AbilityArcaneBlightE = 34957;
        private const int AbilityArcaneBlightS = 34955;
        private const int AbilityArcaneBlightW = 34958;
        private const int AbilityPlanarTactics = 34968;
        private const int AbilityInfernoTheorem = 34990;
        private const int AbilityLockedAndLoaded = 35109;
        private const int AbilityMisload = 35110;
        private const int AbilityTrickReload = 35146;
        private const int AbilityTrapshooting1 = 36122;
        private const int AbilityTrapshooting2 = 35161;
        private const int AbilityTriggerHappy = 35147;

        private bool ZoneOk = false;

        private SpringCrystal _springCrystal;
        private LalaRotation _lalaRotation;
        private PlayerRotation _playerRotation;
        private PlayerMarch _playerMarch;
        private StaticeReload _staticeReload;

        private enum PhaseEnum
        {
            Lala_Inferno,
            Lala_Planar,
        }

        private PhaseEnum _CurrentPhase = PhaseEnum.Lala_Inferno;
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

        #region SpringCrystal

        public class SpringCrystal : Core.ContentItem
        {

            public override FeaturesEnum Features => FeaturesEnum.Drawing;

            [AttributeOrderNumber(1000)]
            public Vector4 SafeSpotColor { get; set; } = new Vector4(0.0f, 1.0f, 0.0f, 0.2f);

            [DebugOption]
            [AttributeOrderNumber(2000)]
            public System.Action Test { get; set; }

            internal uint _midCrystalId = 0;
            private List<PointF> _points = new List<PointF>();

            public SpringCrystal(State state) : base(state)
            {
                Enabled = false;
                Test = new System.Action(() => TestFunctionality());
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _midCrystalId = 0;
                _points.Clear();
            }

            public void TestFunctionality()
            {
                if (_midCrystalId > 0)
                {                    
                    Reset();
                    return;
                }
                _state.InvokeZoneChange(1179);
                _midCrystalId = _state.cs.LocalPlayer.ObjectId;
            }

            internal void FeedMidCrystal(GameObject go)
            {
                if (Active == false)
                {
                    return;
                }
                _midCrystalId = go.ObjectId;
                Log(State.LogLevelEnum.Debug, null, "Registered mid crystal {0}", go.ObjectId);
            }

            protected override bool ExecutionImplementation()
            {
                ImDrawListPtr draw;
                if (_midCrystalId == 0)
                {
                    return false;
                }
                if (_state.StartDrawing(out draw) == false)
                {
                    return false;
                }
                GameObject go = _state.GetActorById(_midCrystalId);
                if (go == null)
                {
                    Reset();
                    return true;
                }
                float y = go.Position.Y;
                if (_points.Count == 0)
                {
                    if ((float)Math.Abs(go.Rotation) > 0.1f)
                    {
                        // east-west
                        _points.Add(new PointF(-15.0f, go.Position.Z - 10.0f));
                        _points.Add(new PointF(-15.0f, go.Position.Z + 10.0f));
                        _points.Add(new PointF(15.0f, go.Position.Z - 10.0f));
                        _points.Add(new PointF(15.0f, go.Position.Z + 10.0f));
                    }
                    else
                    {
                        // north-south
                        _points.Add(new PointF(go.Position.X - 10.0f, -15.0f));
                        _points.Add(new PointF(go.Position.X + 10.0f, -15.0f));
                        _points.Add(new PointF(go.Position.X - 10.0f, 15.0f));
                        _points.Add(new PointF(go.Position.X + 10.0f, 15.0f));
                    }
                }
                foreach (PointF pt in _points)
                {
                    Vector3 p1 = new Vector3(pt.X - 5.0f, y, pt.Y - 5.0f);
                    Vector3 p2 = new Vector3(pt.X + 5.0f, y, pt.Y - 5.0f);
                    Vector3 p3 = new Vector3(pt.X + 5.0f, y, pt.Y + 5.0f);
                    Vector3 p4 = new Vector3(pt.X - 5.0f, y, pt.Y + 5.0f);
                    Vector3 t1 = _state.plug._ui.TranslateToScreen(p1.X, p1.Y, p1.Z);
                    Vector3 t2 = _state.plug._ui.TranslateToScreen(p2.X, p2.Y, p2.Z);
                    Vector3 t3 = _state.plug._ui.TranslateToScreen(p3.X, p3.Y, p3.Z);
                    Vector3 t4 = _state.plug._ui.TranslateToScreen(p4.X, p4.Y, p4.Z);
                    draw.AddQuadFilled(
                        new Vector2(t1.X, t1.Y), 
                        new Vector2(t2.X, t2.Y),
                        new Vector2(t3.X, t3.Y),
                        new Vector2(t4.X, t4.Y),
                        ImGui.GetColorU32(SafeSpotColor)
                    );
                }
                return true;
            }

        }

        #endregion

        #region LalaRotation

        public class LalaRotation : Core.ContentItem
        {

            public override FeaturesEnum Features => FeaturesEnum.Drawing;

            [AttributeOrderNumber(1000)]
            public Vector4 SafeZoneColor { get; set; } = new Vector4(0.0f, 1.0f, 0.0f, 0.5f);

            [DebugOption]
            [AttributeOrderNumber(2000)]
            public System.Action Test { get; set; }

            private enum SafeZoneEnum
            {
                None,
                North,
                East,
                South,
                West
            }

            private uint _lalaStatus = 0;
            private uint _lalaAction = 0;
            private uint _lalaHeadmarker = 0;
            private uint _lalaId = 0;

            public LalaRotation(State state) : base(state)
            {
                Enabled = false;
                Test = new System.Action(() => TestFunctionality());
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _lalaStatus = 0;
                _lalaHeadmarker = 0;
                _lalaAction = 0;
            }

            public void TestFunctionality()
            {
                if (_lalaId > 0)
                {
                    _lalaId = 0;
                    Reset();
                    return;
                }
                _state.InvokeZoneChange(1179);
                Random r = new Random();
                _lalaId = _state.cs.LocalPlayer.ObjectId;
                _lalaStatus = (uint)(r.Next(0, 2) == 0 ? StatusLalaThree : StatusLalaFive);
                _lalaHeadmarker = (uint)(r.Next(0, 2) == 0 ? HeadmarkerLalaCW : HeadmarkerLalaCCW);
                switch (r.Next(0, 4))
                {
                    case 0:
                        _lalaAction = AbilityArcaneBlightN;
                        break;
                    case 1:
                        _lalaAction = AbilityArcaneBlightE;
                        break;
                    case 2:
                        _lalaAction = AbilityArcaneBlightS;
                        break;
                    case 3:
                        _lalaAction = AbilityArcaneBlightW;
                        break;
                }
                Log(State.LogLevelEnum.Debug, null, "Testing with {0} {1} {2}", _lalaStatus, _lalaAction, _lalaHeadmarker);
            }

            internal void FeedStatus(uint statusId, bool gained)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered status {0} {1}", statusId, gained);
                if (gained == true)
                {
                    _lalaStatus = statusId;
                }
                else
                {
                    _lalaStatus = 0;
                    _lalaAction = 0;
                    _lalaHeadmarker = 0;
                }                
            }

            internal void FeedAction(uint actorId, uint actionId)
            {
                if (Active == false)
                {
                    return;
                }
                _lalaId = actorId;
                _lalaAction = actionId;
            }

            internal void FeedHeadmarker(uint actorId, uint headMarkerId)
            {
                if (Active == false)
                {
                    return;
                }
                _lalaId = actorId;
                _lalaHeadmarker = headMarkerId;                
            }

            protected override bool ExecutionImplementation()
            {
                ImDrawListPtr draw;
                if (_lalaHeadmarker == 0 || _lalaStatus == 0 || _lalaAction == 0)
                {
                    return false;
                }
                if (_state.StartDrawing(out draw) == false)
                {
                    return false;
                }
                SafeZoneEnum sz = SafeZoneEnum.None;
                if (
                    (_lalaAction == AbilityArcaneBlightE && _lalaHeadmarker == HeadmarkerLalaCW && _lalaStatus == StatusLalaThree)
                    ||
                    (_lalaAction == AbilityArcaneBlightE && _lalaHeadmarker == HeadmarkerLalaCCW && _lalaStatus == StatusLalaFive)
                    ||
                    (_lalaAction == AbilityArcaneBlightW && _lalaHeadmarker == HeadmarkerLalaCW && _lalaStatus == StatusLalaFive)
                    ||
                    (_lalaAction == AbilityArcaneBlightW && _lalaHeadmarker == HeadmarkerLalaCCW && _lalaStatus == StatusLalaThree)
                )
                {
                    sz = SafeZoneEnum.North;
                }
                if (
                    (_lalaAction == AbilityArcaneBlightE && _lalaHeadmarker == HeadmarkerLalaCW && _lalaStatus == StatusLalaFive)
                    ||
                    (_lalaAction == AbilityArcaneBlightE && _lalaHeadmarker == HeadmarkerLalaCCW && _lalaStatus == StatusLalaThree)
                    ||
                    (_lalaAction == AbilityArcaneBlightW && _lalaHeadmarker == HeadmarkerLalaCW && _lalaStatus == StatusLalaThree)
                    ||
                    (_lalaAction == AbilityArcaneBlightW && _lalaHeadmarker == HeadmarkerLalaCCW && _lalaStatus == StatusLalaFive)
                )
                {
                    sz = SafeZoneEnum.South;
                }
                if (
                    (_lalaAction == AbilityArcaneBlightN && _lalaHeadmarker == HeadmarkerLalaCW && _lalaStatus == StatusLalaThree)
                    ||
                    (_lalaAction == AbilityArcaneBlightN && _lalaHeadmarker == HeadmarkerLalaCCW && _lalaStatus == StatusLalaFive)
                    ||
                    (_lalaAction == AbilityArcaneBlightS && _lalaHeadmarker == HeadmarkerLalaCW && _lalaStatus == StatusLalaFive)
                    ||
                    (_lalaAction == AbilityArcaneBlightS && _lalaHeadmarker == HeadmarkerLalaCCW && _lalaStatus == StatusLalaThree)
                )
                {
                    sz = SafeZoneEnum.West;
                }
                if (
                    (_lalaAction == AbilityArcaneBlightN && _lalaHeadmarker == HeadmarkerLalaCW && _lalaStatus == StatusLalaFive)
                    ||
                    (_lalaAction == AbilityArcaneBlightN && _lalaHeadmarker == HeadmarkerLalaCCW && _lalaStatus == StatusLalaThree)
                    ||
                    (_lalaAction == AbilityArcaneBlightS && _lalaHeadmarker == HeadmarkerLalaCW && _lalaStatus == StatusLalaThree)
                    ||
                    (_lalaAction == AbilityArcaneBlightS && _lalaHeadmarker == HeadmarkerLalaCCW && _lalaStatus == StatusLalaFive)
                )
                {
                    sz = SafeZoneEnum.East;
                }
                if (sz == SafeZoneEnum.None)
                {
                    return true;
                }
                GameObject go = _state.GetActorById(_lalaId);
                Vector3 t1, t2;
                Vector3 p1 = go.Position;
                Vector3 p2, p3;
                switch (sz)
                {
                    case SafeZoneEnum.North:
                        p2 = new Vector3(p1.X - 10.0f, p1.Y, p1.Z - 10.0f);
                        p3 = new Vector3(p1.X + 10.0f, p1.Y, p1.Z - 10.0f);
                        break;
                    case SafeZoneEnum.South:
                        p2 = new Vector3(p1.X - 10.0f, p1.Y, p1.Z + 10.0f);
                        p3 = new Vector3(p1.X + 10.0f, p1.Y, p1.Z + 10.0f);
                        break;
                    case SafeZoneEnum.East:
                        p2 = new Vector3(p1.X + 10.0f, p1.Y, p1.Z - 10.0f);
                        p3 = new Vector3(p1.X + 10.0f, p1.Y, p1.Z + 10.0f);
                        break;
                    case SafeZoneEnum.West:
                        p2 = new Vector3(p1.X - 10.0f, p1.Y, p1.Z - 10.0f);
                        p3 = new Vector3(p1.X - 10.0f, p1.Y, p1.Z + 10.0f);
                        break;
                    default:
                        return true;
                }
                t1 = _state.plug._ui.TranslateToScreen(p1.X, p1.Y, p1.Z);
                t2 = _state.plug._ui.TranslateToScreen(p2.X, p2.Y, p2.Z);
                draw.AddLine(
                    new Vector2(t1.X, t1.Y),
                    new Vector2(t2.X, t2.Y),
                    ImGui.GetColorU32(SafeZoneColor),
                    3.0f
                );
                t2 = _state.plug._ui.TranslateToScreen(p3.X, p3.Y, p3.Z);
                draw.AddLine(
                    new Vector2(t1.X, t1.Y),
                    new Vector2(t2.X, t2.Y),
                    ImGui.GetColorU32(SafeZoneColor),
                    3.0f
                );
                return true;
            }

        }

        #endregion

        #region PlayerRotation

        public class PlayerRotation : Core.ContentItem
        {

            public override FeaturesEnum Features => FeaturesEnum.Drawing;

            [AttributeOrderNumber(1000)]
            public Vector4 IndicatorColor { get; set; } = new Vector4(1.0f, 1.0f, 1.0f, 0.5f);

            [DebugOption]
            [AttributeOrderNumber(2000)]
            public System.Action Test { get; set; }

            private enum DirectionEnum
            {
                None,
                Front,
                Left,
                Right,
                Back
            }

            private uint _playerTimes = 0;
            private uint _playerUnseen = 0;
            private uint _playerHeadmarker = 0;

            public PlayerRotation(State state) : base(state)
            {
                Enabled = false;
                Test = new System.Action(() => TestFunctionality());
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _playerTimes = 0;
                _playerUnseen = 0;
                _playerHeadmarker = 0;
            }

            public void TestFunctionality()
            {
                if (_playerUnseen > 0)
                {
                    Reset();
                    return;
                }
                _state.InvokeZoneChange(1179);
                Random r = new Random();
                _playerTimes = (uint)(r.Next(0, 2) == 0 ? StatusPlayerThree : StatusPlayerFive);
                _playerHeadmarker = (uint)(r.Next(0, 2) == 0 ? HeadmarkerPlayerCW : HeadmarkerPlayerCCW);
                switch (r.Next(0, 4))
                {
                    case 0:
                        _playerUnseen = StatusFrontUnseen;
                        break;
                    case 1:
                        _playerUnseen = StatusLeftUnseen;
                        break;
                    case 2:
                        _playerUnseen = StatusBackUnseen;
                        break;
                    case 3:
                        _playerUnseen = StatusRightUnseen;
                        break;
                }
                Log(State.LogLevelEnum.Debug, null, "Testing with {0} {1} {2}", _playerTimes, _playerUnseen, _playerHeadmarker);
            }

            internal void FeedStatus(uint actorId, uint statusId, bool gained)
            {
                if (Active == false)
                {
                    return;
                }
                if (actorId != _state.cs.LocalPlayer.ObjectId)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered status {0} {1} on {2:X}", statusId, gained);
                if (gained == true)
                {
                    switch (statusId)
                    {
                        case StatusFrontUnseen:
                        case StatusBackUnseen:
                        case StatusLeftUnseen:
                        case StatusRightUnseen:
                            _playerUnseen = statusId;
                            break;
                        case StatusPlayerThree:
                        case StatusPlayerFive:
                            _playerTimes = statusId;
                            break;
                    }
                }
                else
                {
                    _playerTimes = 0;
                    _playerUnseen = 0;
                    _playerHeadmarker = 0;
                }
            }

            internal void FeedHeadmarker(uint actorId, uint headMarkerId)
            {
                if (Active == false)
                {
                    return;
                }
                if (actorId != _state.cs.LocalPlayer.ObjectId)
                {
                    return;
                }
                _playerHeadmarker = headMarkerId;
            }

            protected override bool ExecutionImplementation()
            {
                ImDrawListPtr draw;
                if (_playerHeadmarker == 0 || _playerUnseen == 0 || _playerTimes == 0)
                {
                    return false;
                }
                if (_state.StartDrawing(out draw) == false)
                {
                    return false;
                }
                DirectionEnum sz = DirectionEnum.None;
                if (
                   (_playerUnseen == StatusRightUnseen && _playerHeadmarker == HeadmarkerPlayerCW && _playerTimes == StatusPlayerThree)
                   ||
                   (_playerUnseen == StatusRightUnseen && _playerHeadmarker == HeadmarkerPlayerCCW && _playerTimes == StatusPlayerFive)
                   ||
                   (_playerUnseen == StatusLeftUnseen && _playerHeadmarker == HeadmarkerPlayerCW && _playerTimes == StatusPlayerFive)
                   ||
                   (_playerUnseen == StatusLeftUnseen && _playerHeadmarker == HeadmarkerPlayerCCW && _playerTimes == StatusPlayerThree)
               )
                {
                    sz = DirectionEnum.Front;
                }
                if (
                    (_playerUnseen == StatusRightUnseen && _playerHeadmarker == HeadmarkerPlayerCW && _playerTimes == StatusPlayerFive)
                    ||
                    (_playerUnseen == StatusRightUnseen && _playerHeadmarker == HeadmarkerPlayerCCW && _playerTimes == StatusPlayerThree)
                    ||
                    (_playerUnseen == StatusLeftUnseen && _playerHeadmarker == HeadmarkerPlayerCW && _playerTimes == StatusPlayerThree)
                    ||
                    (_playerUnseen == StatusLeftUnseen && _playerHeadmarker == HeadmarkerPlayerCCW && _playerTimes == StatusPlayerFive)
                )
                {
                    sz = DirectionEnum.Back;
                }
                if (
                    (_playerUnseen == StatusFrontUnseen && _playerHeadmarker == HeadmarkerPlayerCW && _playerTimes == StatusPlayerThree)
                    ||
                    (_playerUnseen == StatusFrontUnseen && _playerHeadmarker == HeadmarkerPlayerCCW && _playerTimes == StatusPlayerFive)
                    ||
                    (_playerUnseen == StatusBackUnseen && _playerHeadmarker == HeadmarkerPlayerCW && _playerTimes == StatusPlayerFive)
                    ||
                    (_playerUnseen == StatusBackUnseen && _playerHeadmarker == HeadmarkerPlayerCCW && _playerTimes == StatusPlayerThree)
                )
                {
                    sz = DirectionEnum.Left;
                }
                if (
                    (_playerUnseen == StatusFrontUnseen && _playerHeadmarker == HeadmarkerPlayerCW && _playerTimes == StatusPlayerFive)
                    ||
                    (_playerUnseen == StatusFrontUnseen && _playerHeadmarker == HeadmarkerPlayerCCW && _playerTimes == StatusPlayerThree)
                    ||
                    (_playerUnseen == StatusBackUnseen && _playerHeadmarker == HeadmarkerPlayerCW && _playerTimes == StatusPlayerThree)
                    ||
                    (_playerUnseen == StatusBackUnseen && _playerHeadmarker == HeadmarkerPlayerCCW && _playerTimes == StatusPlayerFive)
                )
                {
                    sz = DirectionEnum.Right;
                }
                if (sz == DirectionEnum.None)
                {
                    return true;
                }
                GameObject go = _state.cs.LocalPlayer;
                Vector3 t1, t2;
                Vector3 p1 = go.Position;
                Vector3 p2, p3, p4;
                double heading = (go.Rotation * -1.0f) + (Math.PI / 2.0f);
                switch (sz)
                {
                    case DirectionEnum.Front:
                        break;
                    case DirectionEnum.Back:
                        heading += Math.PI;
                        break;
                    case DirectionEnum.Left:
                        heading -= (Math.PI / 2.0f);
                        break;
                    case DirectionEnum.Right:
                        heading += (Math.PI / 2.0f);
                        break;
                    default:
                        return true;
                }                
                float time = (float)(DateTime.Now - DateTime.Today).TotalMilliseconds / 200.0f;
                float ang = (float)Math.Abs(Math.Cos(time));
                float ex = 1.0f + (float)(Math.Sin(ang) * 0.5f);
                p2 = new Vector3(p1.X + (float)Math.Cos(heading) * ex, p1.Y, p1.Z + (float)Math.Sin(heading) * ex);
                p3 = new Vector3(p2.X + (float)Math.Cos(heading + Math.PI / 2.0f) * ex, p2.Y, p2.Z + (float)Math.Sin(heading + Math.PI / 2.0f) * ex);
                p4 = new Vector3(p2.X + (float)Math.Cos(heading - Math.PI / 2.0f) * ex, p2.Y, p2.Z + (float)Math.Sin(heading - Math.PI / 2.0f) * ex);
                t1 = _state.plug._ui.TranslateToScreen(p3.X, p3.Y, p3.Z);
                t2 = _state.plug._ui.TranslateToScreen(p4.X, p4.Y, p4.Z);
                draw.AddLine(
                    new Vector2(t1.X, t1.Y),
                    new Vector2(t2.X, t2.Y),
                    ImGui.GetColorU32(IndicatorColor),
                    3.0f
                );
                ex = 1.3f + (float)(Math.Sin(ang) * 0.5f);
                p2 = new Vector3(p1.X + (float)Math.Cos(heading) * ex, p1.Y, p1.Z + (float)Math.Sin(heading) * ex);
                p3 = new Vector3(p2.X + (float)Math.Cos(heading + Math.PI / 2.0f) * ex, p2.Y, p2.Z + (float)Math.Sin(heading + Math.PI / 2.0f) * ex);
                p4 = new Vector3(p2.X + (float)Math.Cos(heading - Math.PI / 2.0f) * ex, p2.Y, p2.Z + (float)Math.Sin(heading - Math.PI / 2.0f) * ex);
                t1 = _state.plug._ui.TranslateToScreen(p3.X, p3.Y, p3.Z);
                t2 = _state.plug._ui.TranslateToScreen(p4.X, p4.Y, p4.Z);
                draw.AddLine(
                    new Vector2(t1.X, t1.Y),
                    new Vector2(t2.X, t2.Y),
                    ImGui.GetColorU32(IndicatorColor),
                    3.0f
                );
                return true;
            }

        }

        #endregion

        #region PlayerMarch

        public class PlayerMarch : Core.ContentItem
        {

            public override FeaturesEnum Features => FeaturesEnum.Drawing;

            [AttributeOrderNumber(1000)]
            public Vector4 IndicatorColor { get; set; } = new Vector4(1.0f, 1.0f, 0.0f, 0.5f);

            [DebugOption]
            [AttributeOrderNumber(2000)]
            public System.Action Test { get; set; }

            private enum DirectionEnum
            {
                None,
                Front,
                Left,
                Right,
                Back
            }

            private uint _playerTimes = 0;
            private uint _playerHeadmarker = 0;

            public PlayerMarch(State state) : base(state)
            {
                Enabled = false;
                Test = new System.Action(() => TestFunctionality());
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _playerTimes = 0;
                _playerHeadmarker = 0;                
            }

            public void TestFunctionality()
            {
                if (_playerTimes > 0)
                {
                    Reset();                    
                    return;
                }
                _state.InvokeZoneChange(1179);
                Random r = new Random();
                _playerTimes = (uint)(r.Next(0, 2) == 0 ? StatusPlayerThree : StatusPlayerFive);
                _playerHeadmarker = (uint)(r.Next(0, 2) == 0 ? HeadmarkerPlayerCW : HeadmarkerPlayerCCW);
                Log(State.LogLevelEnum.Debug, null, "Testing with {0} {1}", _playerTimes, _playerHeadmarker);
            }

            internal void FeedStatus(uint actorId, uint statusId, bool gained)
            {
                if (Active == false)
                {
                    return;
                }
                if (actorId != _state.cs.LocalPlayer.ObjectId)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered status {0} {1} on {2:X}", statusId, gained, actorId);
                if (gained == true)
                {
                    switch (statusId)
                    {
                        case StatusPlayerThree:
                        case StatusPlayerFive:
                            _playerTimes = statusId;
                            break;
                    }                    
                }
                else
                {
                    _playerTimes = 0;                    
                    _playerHeadmarker = 0;                    
                }
            }

            internal void FeedHeadmarker(uint actorId, uint headMarkerId)
            {
                if (Active == false)
                {
                    return;
                }
                if (actorId != _state.cs.LocalPlayer.ObjectId)
                {
                    return;
                }
                _playerHeadmarker = headMarkerId;                
            }

            protected override bool ExecutionImplementation()
            {
                ImDrawListPtr draw;
                if (_playerTimes == 0 || _playerHeadmarker == 0)
                {
                    return false;
                }
                if (_state.StartDrawing(out draw) == false)
                {
                    return false;
                }
                DirectionEnum sz = DirectionEnum.None;
                if (
                   (_playerHeadmarker == HeadmarkerPlayerCW && _playerTimes == StatusPlayerThree)
                   ||
                   (_playerHeadmarker == HeadmarkerPlayerCCW && _playerTimes == StatusPlayerFive)
                )
                {
                    sz = DirectionEnum.Left;
                }
                if (
                   (_playerHeadmarker == HeadmarkerPlayerCCW && _playerTimes == StatusPlayerThree)
                   ||
                   (_playerHeadmarker == HeadmarkerPlayerCW && _playerTimes == StatusPlayerFive)
                )
                {
                    sz = DirectionEnum.Right;
                }
                if (sz == DirectionEnum.None)
                {
                    return true;
                }
                GameObject go = _state.cs.LocalPlayer;
                Vector3 t1, t2;
                Vector3 p1 = go.Position;
                Vector3 p2, p3, p4, p5;
                double heading = (go.Rotation * -1.0f) + (Math.PI / 2.0f);
                switch (sz)
                {
                    case DirectionEnum.Left:
                        heading -= (Math.PI / 2.0f);
                        break;
                    case DirectionEnum.Right:
                        heading += (Math.PI / 2.0f);
                        break;
                    default:
                        return true;
                }
                float time = (float)(DateTime.Now - DateTime.Today).TotalMilliseconds / 200.0f;
                float ang = (float)Math.Abs(Math.Cos(time));
                float ex = 1.0f + (float)(Math.Sin(ang) * 0.5f);
                p2 = new Vector3(p1.X + (float)Math.Cos(heading) * ex, p1.Y, p1.Z + (float)Math.Sin(heading) * ex);
                p3 = new Vector3(p1.X + (float)Math.Cos(heading) * (ex + 1.5f), p1.Y, p1.Z + (float)Math.Sin(heading) * (ex + 1.5f));
                heading += Math.PI;
                p4 = new Vector3(p3.X + (float)Math.Cos(heading - (Math.PI / 4.0f)) * 0.5f, p3.Y, p3.Z + (float)Math.Sin(heading - (Math.PI / 4.0f)) * 0.5f);
                p5 = new Vector3(p3.X + (float)Math.Cos(heading + (Math.PI / 4.0f)) * 0.5f, p3.Y, p3.Z + (float)Math.Sin(heading + (Math.PI / 4.0f)) * 0.5f);
                t1 = _state.plug._ui.TranslateToScreen(p2.X, p2.Y, p2.Z);
                t2 = _state.plug._ui.TranslateToScreen(p3.X, p3.Y, p3.Z);
                draw.AddLine(
                    new Vector2(t1.X, t1.Y),
                    new Vector2(t2.X, t2.Y),
                    ImGui.GetColorU32(IndicatorColor),
                    4.0f
                );
                t1 = _state.plug._ui.TranslateToScreen(p4.X, p4.Y, p4.Z);
                draw.AddLine(
                    new Vector2(t1.X, t1.Y),
                    new Vector2(t2.X, t2.Y),
                    ImGui.GetColorU32(IndicatorColor),
                    4.0f
                );
                t1 = _state.plug._ui.TranslateToScreen(p5.X, p5.Y, p5.Z);
                draw.AddLine(
                    new Vector2(t1.X, t1.Y),
                    new Vector2(t2.X, t2.Y),
                    ImGui.GetColorU32(IndicatorColor),
                    4.0f
                );
                return true;
            }

        }

        #endregion

        #region StaticeReload

        public class StaticeReload : Core.ContentItem
        {

            public override FeaturesEnum Features => FeaturesEnum.Drawing;

            [AttributeOrderNumber(1100)]
            public Vector4 LoadedColor { get; set; } = new Vector4(1.0f, 1.0f, 0.0f, 0.85f);
            [AttributeOrderNumber(1101)]
            public Vector4 MisloadedColor { get; set; } = new Vector4(1.0f, 0.0f, 0.0f, 0.85f);
            [AttributeOrderNumber(1101)]
            public Vector4 UnloadedColor { get; set; } = new Vector4(0.0f, 0.0f, 0.0f, 0.85f);

            [DebugOption]
            [AttributeOrderNumber(2000)]
            public System.Action Test { get; set; }

            private enum BulletStateEnum
            {
                Unknown,
                Misload,
                Load
            }

            private uint _staticeId = 0;
            private BulletStateEnum[] _bullets = new BulletStateEnum[8];
            private int _bulletIndex = 0;
            private int _bulletsSpent = 0;

            public StaticeReload(State state) : base(state)
            {
                Enabled = false;
                Test = new System.Action(() => TestFunctionality());
                ResetBullets();
            }

            private void ResetBullets()
            {
                for (int i = 0; i < 8; i++)
                {
                    _bullets[i] = BulletStateEnum.Unknown;
                }
                _bulletIndex = 0;
                _bulletsSpent = 0;
            }

            internal void EatBullets(int bullets)
            {
                if (Active == false)
                {
                    return;
                }
                _bulletsSpent += bullets;
                if (_bulletsSpent == 8)
                {
                    Reset();
                }
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _staticeId = 0;
                ResetBullets();
            }

            public void TestFunctionality()
            {
                if (_staticeId > 0)
                {
                    if (_bulletsSpent < 4)
                    {
                        EatBullets(4);
                    }
                    else
                    {
                        Reset();
                    }
                    return;
                }
                _staticeId = _state.cs.LocalPlayer.ObjectId;
                _state.InvokeZoneChange(1179);
                Random r = new Random();
                for (int i = 0; i < 8; i++)
                {
                    _bullets[i] = (BulletStateEnum)(1 + r.Next(0, 2));
                }
                Log(State.LogLevelEnum.Debug, null, "Testing");
            }

            internal void FeedAbility(uint actorId, uint abilityId)
            {
                if (Active == false)
                {
                    return;
                }
                if (abilityId == AbilityLockedAndLoaded)
                {
                    _staticeId = actorId;
                    _bullets[_bulletIndex] = BulletStateEnum.Load;
                    _bulletIndex++;
                }
                if (abilityId == AbilityMisload)
                {
                    _staticeId = actorId;
                    _bullets[_bulletIndex] = BulletStateEnum.Misload;
                    _bulletIndex++;
                }
                if (abilityId == AbilityTrickReload)
                {
                    Reset();
                    _staticeId = actorId;
                }
            }

            protected override bool ExecutionImplementation()
            {
                ImDrawListPtr draw;
                if (_staticeId == 0)
                {
                    return false;
                }
                if (_state.StartDrawing(out draw) == false)
                {
                    return false;
                }
                int bulletsToDraw = 8 - _bulletsSpent;
                GameObject go = _state.GetActorById(_staticeId);
                Vector3 pos = new Vector3(go.Position.X, go.Position.Y + 3.0f, go.Position.Z);
                Vector3 t1 = _state.plug._ui.TranslateToScreen(pos.X, pos.Y, pos.Z);
                float radius = 15.0f;
                float x = t1.X - (radius * 2.0f * 4.0f) + radius;
                for (int i = 0; i < 8; i++)
                {
                    BulletStateEnum bs = _bullets[i];
                    float yofs = (i == 0 || i == 7) ? radius * 1.5f : 0.0f;
                    float xofs;
                    switch (i)
                    {
                        case 0:
                            xofs = (radius * 0.5f);
                            break;
                        case 7:
                            xofs = (radius * -0.5f);
                            break;
                        default:
                            xofs = 0.0f;
                            break;
                    }
                    if (_bulletsSpent > i)
                    {
                        bs = BulletStateEnum.Unknown;
                    }
                    switch (bs)
                    {
                        case BulletStateEnum.Load:
                            draw.AddCircleFilled(new Vector2(x + xofs, t1.Y + yofs), radius, ImGui.GetColorU32(LoadedColor), 10);
                            break;
                        case BulletStateEnum.Misload:
                            draw.AddCircleFilled(new Vector2(x + xofs, t1.Y + yofs), radius, ImGui.GetColorU32(MisloadedColor), 10);
                            break;
                        case BulletStateEnum.Unknown:
                            draw.AddCircleFilled(new Vector2(x + xofs, t1.Y + yofs), radius, ImGui.GetColorU32(UnloadedColor), 10);
                            break;
                    }
                    x += radius * 2.0f;
                }
                return true;
            }

        }

        #endregion

        public EwCritAloalo(State st) : base(st)
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
            _state.OnHeadMarker += _state_OnHeadMarker;
            _state.OnStatusChange += _state_OnStatusChange;
            _state.OnCastBegin += _state_OnCastBegin;
            _state.OnCombatantAdded += _state_OnCombatantAdded;
            _state.OnCombatantRemoved += _state_OnCombatantRemoved;
            _state.OnAction += _state_OnAction;
        }

        private void _state_OnAction(uint src, uint dest, ushort actionId)
        {
            switch (actionId)
            {
                case AbilityLockedAndLoaded:
                case AbilityMisload:                
                    _staticeReload.FeedAbility(dest, actionId);
                    break;
                case AbilityTrapshooting1:
                case AbilityTrapshooting2:
                    _staticeReload.EatBullets(1);
                    break;
                case AbilityTriggerHappy:
                    _staticeReload.EatBullets(6);
                    break;
            }
        }

        private void _state_OnCombatantRemoved(uint actorId, nint addr)
        {
            if (actorId == _springCrystal._midCrystalId)
            {
                _springCrystal.Reset();
            }
        }

        private void _state_OnCombatantAdded(GameObject go)
        {
            if (go is Character)
            {
                Character ch = go as Character;
                if (ch.NameId == 12606 || ch.NameId == 12607)
                {
                    float x = Math.Abs(go.Position.X);
                    float z = Math.Abs(go.Position.Z);
                    if (x >= 4.0 && x <= 6.0 && z >= 4.0 && z <= 6.0)
                    {
                        _springCrystal.FeedMidCrystal(go);
                    }
                }
            }
        }

        private void _state_OnCastBegin(uint src, uint dest, ushort actionId, float castTime, float rotation)
        {
            switch (actionId)
            {
                case AbilityArcaneBlightN:
                case AbilityArcaneBlightE:
                case AbilityArcaneBlightS:
                case AbilityArcaneBlightW:
                    _lalaRotation.FeedAction(src, actionId);
                    break;
                case AbilityInfernoTheorem:
                    CurrentPhase = PhaseEnum.Lala_Inferno;
                    break;
                case AbilityPlanarTactics:
                    CurrentPhase = PhaseEnum.Lala_Planar;
                    break;
                case AbilityTrickReload:
                    _staticeReload.FeedAbility(dest, actionId);
                    break;
            }
        }

        private void _state_OnHeadMarker(uint dest, uint markerId)
        {
            switch (markerId)
            {
                case HeadmarkerLalaCW:
                case HeadmarkerLalaCCW:
                    _lalaRotation.FeedHeadmarker(dest, markerId);
                    break;
                case HeadmarkerPlayerCW:
                case HeadmarkerPlayerCCW:
                    if (CurrentPhase == PhaseEnum.Lala_Inferno)
                    {
                        _playerRotation.FeedHeadmarker(dest, markerId);
                    }
                    if (CurrentPhase == PhaseEnum.Lala_Planar)
                    {
                        _playerMarch.FeedHeadmarker(dest, markerId);
                    }
                    break;
            }
        }

        private void _state_OnStatusChange(uint src, uint dest, uint statusId, bool gained, float duration, int stacks)
        {
            switch (statusId)
            {
                case StatusLalaThree:
                case StatusLalaFive:
                    _lalaRotation.FeedStatus(statusId, gained);
                    break;
                case StatusPlayerThree:
                case StatusPlayerFive:
                case StatusFrontUnseen:
                case StatusBackUnseen:
                case StatusLeftUnseen:
                case StatusRightUnseen:                    
                    if (CurrentPhase == PhaseEnum.Lala_Inferno)
                    {
                        _playerRotation.FeedStatus(dest, statusId, gained);
                    }
                    if (CurrentPhase == PhaseEnum.Lala_Planar)
                    {
                        _playerMarch.FeedStatus(dest, statusId, gained);
                    }
                    break;
            }
        }

        private void UnsubscribeFromEvents()
        {
            _state.OnHeadMarker -= _state_OnHeadMarker;
            _state.OnStatusChange -= _state_OnStatusChange;
            _state.OnCastBegin -= _state_OnCastBegin;
            _state.OnCombatantAdded -= _state_OnCombatantAdded;
            _state.OnCombatantRemoved -= _state_OnCombatantRemoved;
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
            bool newZoneOk = (newZone == 1179 || newZone == 1180);
            if (newZoneOk == true && ZoneOk == false)
            {
                Log(State.LogLevelEnum.Info, null, "Content available");
                _springCrystal = (SpringCrystal)Items["SpringCrystal"];
                _lalaRotation = (LalaRotation)Items["LalaRotation"];
                _playerRotation = (PlayerRotation)Items["PlayerRotation"];
                _playerMarch = (PlayerMarch)Items["PlayerMarch"];
                _staticeReload = (StaticeReload)Items["StaticeReload"];
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
