using System;
using Lemegeton.Core;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using System.Numerics;
using GameObjectPtr = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using System.Drawing;
using static Lemegeton.Core.State;

namespace Lemegeton.Content
{

    internal class DtCritMerchantsTale : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

        private const int AbilityBurningGleam = 45548;

        private bool ZoneOk = false;
        private bool _subbed = false;
        private bool _sawFirstHeadMarker = false;
        private bool _sawLeadHook = false;
        private uint _firstHeadMarker = 0;

        private ParisCurse _parisCurse;

        private enum PhaseEnum
        {
            Pari,
        }

        private PhaseEnum _CurrentPhase = PhaseEnum.Pari;
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

        #region Pari's Curse

        public class ParisCurse : Core.ContentItem
        {

            public override FeaturesEnum Features => FeaturesEnum.Drawing;

            [AttributeOrderNumber(1000)]
            public Vector4 GleamColor { get; set; } = new Vector4(1.0f, 0.0f, 0.0f, 0.15f);

            [DebugOption]
            [AttributeOrderNumber(2000)]
            public System.Action Test { get; set; }
            
            private List<ulong> _fieryBaubles = new List<ulong>();

            public ParisCurse(State state) : base(state)
            {
                Enabled = false;
                Test = new System.Action(() => TestFunctionality());
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");                
                _fieryBaubles.Clear();
            }

            public void TestFunctionality()
            {
                if (_fieryBaubles.Count > 0)
                {
                    Reset();
                    return;
                }
                _state.InvokeZoneChange(1317);
                _fieryBaubles.Add(_state.ot.LocalPlayer.GameObjectId);
            }

            internal void FeedFieryBauble(IGameObject go)
            {
                if (Active == false)
                {
                    return;
                }
                _fieryBaubles.Add(go.GameObjectId);
                Log(State.LogLevelEnum.Debug, null, "Registered fiery bauble {0}", go.GameObjectId);
            }

            protected override bool ExecutionImplementation()
            {
                ImDrawListPtr draw;
                if (_fieryBaubles.Count == 0)
                {
                    return false;
                }
                if (_state.StartDrawing(out draw) == false)
                {
                    return false;
                }
                foreach (ulong id in _fieryBaubles)
                {
                    IGameObject go = _state.GetActorById(id);
                    if (go == null)
                    {
                        Reset();
                        return true;
                    }
                    float x = go.Position.X;
                    float y = go.Position.Y;
                    float z = go.Position.Z;
                    Vector3 p1 = new Vector3(x - 5.0f, y, z - 35.0f);
                    Vector3 p2 = new Vector3(x + 5.0f, y, z - 35.0f);
                    Vector3 p3 = new Vector3(x + 5.0f, y, z + 35.0f);
                    Vector3 p4 = new Vector3(x - 5.0f, y, z + 35.0f);
                    Vector3 t1 = _state.plug._ui.TranslateToScreen(p1.X, p1.Y, p1.Z);
                    Vector3 t2 = _state.plug._ui.TranslateToScreen(p2.X, p2.Y, p2.Z);
                    Vector3 t3 = _state.plug._ui.TranslateToScreen(p3.X, p3.Y, p3.Z);
                    Vector3 t4 = _state.plug._ui.TranslateToScreen(p4.X, p4.Y, p4.Z);
                    draw.AddQuadFilled(
                        new Vector2(t1.X, t1.Y),
                        new Vector2(t2.X, t2.Y),
                        new Vector2(t3.X, t3.Y),
                        new Vector2(t4.X, t4.Y),
                        ImGui.GetColorU32(GleamColor)
                    );
                    p1 = new Vector3(x - 35.0f, y, z - 5.0f);
                    p2 = new Vector3(x + 35.0f, y, z - 5.0f);
                    p3 = new Vector3(x + 35.0f, y, z + 5.0f);
                    p4 = new Vector3(x - 35.0f, y, z + 5.0f);
                    t1 = _state.plug._ui.TranslateToScreen(p1.X, p1.Y, p1.Z);
                    t2 = _state.plug._ui.TranslateToScreen(p2.X, p2.Y, p2.Z);
                    t3 = _state.plug._ui.TranslateToScreen(p3.X, p3.Y, p3.Z);
                    t4 = _state.plug._ui.TranslateToScreen(p4.X, p4.Y, p4.Z);
                    draw.AddQuadFilled(
                        new Vector2(t1.X, t1.Y),
                        new Vector2(t2.X, t2.Y),
                        new Vector2(t3.X, t3.Y),
                        new Vector2(t4.X, t4.Y),
                        ImGui.GetColorU32(GleamColor)
                    );
                }
                return true;
            }

        }

        #endregion

        public DtCritMerchantsTale(State st) : base(st)
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
                _state.OnCastBegin += _state_OnCastBegin;
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
                _state.OnCastBegin -= _state_OnCastBegin;
                _subbed = false;
            }
        }

        private void _state_OnCastBegin(uint src, uint dest, ushort actionId, float castTime, float rotation)
        {
            switch (actionId)
            {
                case AbilityBurningGleam:
                    break;
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

        private void OnZoneChange(uint newZone)
        {
            bool newZoneOk = (newZone == 1317);
            if (newZoneOk == true && ZoneOk == false)
            {
                Log(State.LogLevelEnum.Info, null, "Content available");
                _parisCurse = (ParisCurse)Items["ParisCurse"];
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
