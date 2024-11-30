using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;
using Lemegeton.Core;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using static FFXIVClientStructs.FFXIV.Client.Game.Character.VfxContainer;
using static Lemegeton.Content.Overlays;
using static Lemegeton.Content.Overlays.DotTracker;
using static Lemegeton.Core.AutomarkerPrio;
using static Lemegeton.Core.State;

namespace Lemegeton.Content
{

    internal class UltFuturesRewritten : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

        private bool ZoneOk = false;
        private bool _subbed = false;

        private const int AbilityBoundOfFaith = 40165;
        private const int AbilityQuadrupleSlap = 40191;
        
        private const int StatusPrey = 1051;
        private const int StatusChains = 4157;
        private const int StatusWeightOfLight = 4159;

        private const int TetherFire = 249;
        private const int TetherLightning = 287;

        private FallOfFaithAM _fallFaithAm;
        private LightRampantAM _lightRampantAm;

        private enum PhaseEnum
        {
            P1_Start,
            P1_Faith,
            P2_Shiva,
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

        #region FallOfFaithAM

        public class FallOfFaithAM : Automarker
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
            private List<ulong> _tethers = new List<ulong>();
            private int _preysLost = 0;

            public FallOfFaithAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Prio = new AutomarkerPrio();
                Prio.Priority = AutomarkerPrio.PrioTypeEnum.CongaX;
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Signs.SetRole("Tether1", AutomarkerSigns.SignEnum.Attack1, false);
                Signs.SetRole("Tether2", AutomarkerSigns.SignEnum.Attack2, false);
                Signs.SetRole("Tether3", AutomarkerSigns.SignEnum.Attack3, false);
                Signs.SetRole("Tether4", AutomarkerSigns.SignEnum.Attack4, false);
                Signs.SetRole("Overflow1", AutomarkerSigns.SignEnum.Bind1, false);
                Signs.SetRole("Overflow2", AutomarkerSigns.SignEnum.Bind2, false);
                Signs.SetRole("Overflow3", AutomarkerSigns.SignEnum.Ignore1, false);
                Signs.SetRole("Overflow4", AutomarkerSigns.SignEnum.Ignore2, false);
                Test = new System.Action(() => Signs.TestFunctionality(state, Prio, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _fired = false;
                _preysLost = 0;
                _tethers.Clear();
            }

            internal void FeedStatus(uint actor)
            {
                if (Active == false)
                {
                    return;
                }
                if (_tethers.Count > 0)
                {
                    _preysLost++;
                    if (_preysLost == 4)
                    {
                        _state.ClearAutoMarkers();
                    }
                }
            }

            internal void FeedTether(uint src, uint dest, uint tetherId)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered tether {0} from {1:X} on {2:X}", tetherId, src, dest);
                _tethers.Add((ulong)dest);
                AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                int tethernum = _tethers.Count;
                ap.Assign(Signs.Roles["Tether" + tethernum], _state.GetActorById(dest));
                if (tethernum == 4)
                {
                    Party pty = _state.GetPartyMembers();
                    List<Party.PartyMember> _overflowGo = new List<Party.PartyMember>(
                        from ix in pty.Members where _tethers.Contains(ix.ObjectId) == false select ix
                    );
                    Prio.SortByPriority(_overflowGo);
                    ap.Assign(Signs.Roles["Overflow1"], _overflowGo[0].GameObject);
                    ap.Assign(Signs.Roles["Overflow2"], _overflowGo[1].GameObject);
                    ap.Assign(Signs.Roles["Overflow3"], _overflowGo[2].GameObject);
                    ap.Assign(Signs.Roles["Overflow4"], _overflowGo[3].GameObject);
                }
                _state.ExecuteAutomarkers(ap, Timing);
            }

        }

        #endregion

        #region LightRampantAM

        public class LightRampantAM : Automarker
        {

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
                                _lr._strat = (StratEnum)stratid;
                                break;
                        }
                    }
                }

                public override string Serialize()
                {
                    return string.Format("Strategy={0}", (int)_lr._strat);
                }

                public override void RenderEditor(string path)
                {
                    ImGui.TextWrapped(I18n.Translate(path) + Environment.NewLine + Environment.NewLine);
                    string selname = I18n.Translate(path + "/" + _lr._strat.ToString());
                    if (ImGui.BeginCombo("##" + path, selname) == true)
                    {
                        foreach (LightRampantAM.StratEnum p in Enum.GetValues(typeof(LightRampantAM.StratEnum)))
                        {
                            string name = I18n.Translate(path + "/" + p.ToString());
                            if (ImGui.Selectable(name, String.Compare(name, selname) == 0) == true)
                            {
                                _lr._strat = p;
                            }
                        }
                        ImGui.EndCombo();
                    }
                }

                private LightRampantAM _lr;

                public StrategyWidget(LightRampantAM lr)
                {
                    _lr = lr;
                }

            }

            public enum StratEnum
            {
                Generic,
                AB1234,
                Box,
            }

            [AttributeOrderNumber(1000)]
            public StrategyWidget Strategy { get; set; }

            [AttributeOrderNumber(1010)]
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
            private List<IGameObject> _chaingang = new List<IGameObject>();
            private List<IGameObject> _weightgang = new List<IGameObject>();
            internal StratEnum _strat { get; set; } = StratEnum.AB1234;

            public LightRampantAM(State state) : base(state)
            {
                Enabled = false;
                Strategy = new StrategyWidget(this);
                Signs = new AutomarkerSigns();
                Prio = new AutomarkerPrio();
                Prio.Priority = AutomarkerPrio.PrioTypeEnum.Clockspots;
                Prio.StartingFrom = AutomarkerPrio.PrioDirectionEnum.NW;
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Signs.SetRole("TowerNW", AutomarkerSigns.SignEnum.Attack1, false);
                Signs.SetRole("TowerN", AutomarkerSigns.SignEnum.Triangle, false);
                Signs.SetRole("TowerNE", AutomarkerSigns.SignEnum.Attack2, false);
                Signs.SetRole("TowerSW", AutomarkerSigns.SignEnum.Attack3, false);
                Signs.SetRole("TowerS", AutomarkerSigns.SignEnum.Square, false);
                Signs.SetRole("TowerSE", AutomarkerSigns.SignEnum.Attack4, false);
                Signs.SetRole("Puddle1", AutomarkerSigns.SignEnum.Ignore1, false);
                Signs.SetRole("Puddle2", AutomarkerSigns.SignEnum.Ignore2, false);
                Test = new System.Action(() => Signs.TestFunctionality(state, Prio, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _fired = false;                
                _chaingang.Clear();
                _weightgang.Clear();
            }

            internal void FeedStatus(uint statusId, uint actor, bool gained)
            {
                if (Active == false)
                {
                    return;
                }
                if (gained == false)
                {
                    if (_fired == true && statusId == StatusChains)
                    {
                        _state.ClearAutoMarkers();
                        Reset();
                    }
                    return;
                }
                IGameObject go = _state.GetActorById(actor);
                switch (statusId)
                {
                    case StatusChains:
                        Log(State.LogLevelEnum.Debug, null, "Registered chain on {0}", go);
                        _chaingang.Add(go);
                        break;
                    case StatusWeightOfLight:
                        Log(State.LogLevelEnum.Debug, null, "Registered weight on {0}", go);
                        _weightgang.Add(go);
                        break;
                }
                if (_chaingang.Count != 6)
                {
                    return;
                }
                if (_strat == StratEnum.AB1234)
                {
                    if (_weightgang.Count != 2)
                    {
                        return;
                    }
                }
                Log(State.LogLevelEnum.Debug, null, string.Format("Ready for {0} markers", _strat));
                Party pty = _state.GetPartyMembers();
                List<Party.PartyMember> _chainsGo = new List<Party.PartyMember>(
                    from ix in pty.Members where _chaingang.Contains(ix.GameObject) == true select ix
                );
                List<Party.PartyMember> _puddlesGo = new List<Party.PartyMember>(
                    from ix in pty.Members where _chainsGo.Contains(ix) == false select ix
                );
                Prio.SortByPriority(_chainsGo);
                Log(State.LogLevelEnum.Debug, null, "Chain prio: {0}", String.Join(",", from cx in _chainsGo select cx.Name));
                Prio.SortByPriority(_puddlesGo);
                Log(State.LogLevelEnum.Debug, null, "Puddle prio: {0}", String.Join(",", from cx in _puddlesGo select cx.Name));
                AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                switch (_strat)
                {
                    default:
                    case StratEnum.Generic:
                        {
                            List<string> towers;
                            towers = new List<string>() { "TowerNW", "TowerS", "TowerNE", "TowerSW", "TowerN", "TowerSE", };
                            for (int i = 0; i < 6; i++)
                            {
                                if (_chaingang[i] == _chainsGo[0].GameObject)
                                {
                                    int pi = i > 1 ? i - 2 : 5;
                                    int ni = i < 4 ? 1 + 2 : 0;
                                    int pip = 99;
                                    int nip = 99;
                                    int k = 0;
                                    int dir = 1;
                                    foreach (Party.PartyMember p in _chainsGo)
                                    {
                                        if (p.GameObject == _chaingang[i])
                                        {
                                            continue;
                                        }
                                        if (p == _chainsGo[pi])
                                        {
                                            Log(State.LogLevelEnum.Debug, null, "Previous at index: {0}", k);
                                            pip = k;
                                        }
                                        if (p == _chainsGo[ni])
                                        {
                                            Log(State.LogLevelEnum.Debug, null, "Next at index: {0}", k);
                                            nip = k;
                                        }
                                        k++;
                                    }
                                    if (pip < nip)
                                    {
                                        dir = -1;
                                    }
                                    for (int j = 0; j < 6; j++)
                                    {
                                        ap.Assign(Signs.Roles[towers[j]], _chaingang[i]);
                                        if (dir == -1)
                                        {
                                            i--;
                                            if (i < 0)
                                            {
                                                i = 5;
                                            }
                                        }
                                        else
                                        {
                                            i++;
                                            if (i > 5)
                                            {
                                                i = 0;
                                            }
                                        }
                                    }
                                    i = 99;
                                }
                            }
                        }
                        break;
                    case StratEnum.AB1234:
                        {
                            List<Party.PartyMember> _weightsGo = new List<Party.PartyMember>(
                                from ix in pty.Members where _weightgang.Contains(ix.GameObject) == true select ix
                            );
                            Prio.SortByPriority(_weightsGo);
                            Log(State.LogLevelEnum.Debug, null, "Weight prio: {0}", String.Join(",", from cx in _weightsGo select cx.Name));
                            List<string> towers;
                            towers = new List<string>() { "TowerN", "TowerS", "TowerNW", "TowerNE", "TowerSW", "TowerSE", };
                            List<Party.PartyMember> _finalPrio = new List<Party.PartyMember>();
                            _finalPrio.AddRange(_weightsGo);
                            int iters = 0;
                            while (_chainsGo.Count > 0)
                            {
                                iters++;
                                if (iters > 100)
                                {
                                    Log(State.LogLevelEnum.Debug, null, "Failure with {0} iters", iters);
                                    break;
                                }
                                if (_weightsGo.Count > 0)
                                {
                                    if (_weightsGo.Contains(_chainsGo[0]) == true)
                                    {
                                        _weightsGo.Remove(_chainsGo[0]);
                                        _chainsGo.RemoveAt(0);
                                    }
                                    else
                                    {
                                        Party.PartyMember pm = _chainsGo[0];
                                        _chainsGo.RemoveAt(0);
                                        _chainsGo.Add(pm);
                                    }
                                    continue;
                                }
                                _finalPrio.Add(_chainsGo[0]);
                                _chainsGo.RemoveAt(0);
                            }
                            for (int i = 0; i < 6; i++)
                            {
                                ap.Assign(Signs.Roles[towers[i]], _finalPrio[i]);
                            }
                            Log(State.LogLevelEnum.Debug, null, "Final prio: {0}", String.Join(",", from cx in _weightsGo select cx.Name));
                        }
                        break;
                    case StratEnum.Box:
                        {
                            List<Party.PartyMember> _topGo = new List<Party.PartyMember>(
                                from ix in pty.Members where AutomarkerPrio.JobToArchetype(ix.Job) == PrioArchetypeEnum.Support select ix
                            );
                            Prio.SortByPriority(_topGo);
                            Log(State.LogLevelEnum.Debug, null, "Top prio: {0}", String.Join(",", from cx in _topGo select cx.Name));
                            List<Party.PartyMember> _botGo = new List<Party.PartyMember>(
                                from ix in pty.Members where AutomarkerPrio.JobToArchetype(ix.Job) == PrioArchetypeEnum.DPS select ix
                            );
                            Prio.SortByPriority(_botGo);
                            Log(State.LogLevelEnum.Debug, null, "Bottom prio: {0}", String.Join(",", from cx in _botGo select cx.Name));
                            if (_botGo.Count == 4)
                            {
                                Party.PartyMember pm = _botGo[3];
                                _botGo.RemoveAt(3);
                                _topGo.Insert(0, pm);
                                Log(State.LogLevelEnum.Debug, null, "New top prio: {0}", String.Join(",", from cx in _topGo select cx.Name));
                            }
                            if (_topGo.Count == 4)
                            {
                                Party.PartyMember pm = _topGo[3];
                                _topGo.RemoveAt(3);
                                _botGo.Insert(0, pm);
                                Log(State.LogLevelEnum.Debug, null, "New bottom prio: {0}", String.Join(",", from cx in _botGo select cx.Name));
                            }
                            ap.Assign(Signs.Roles["TowerNW"], _topGo[2]);
                            ap.Assign(Signs.Roles["TowerN"], _botGo[1]);
                            ap.Assign(Signs.Roles["TowerNE"], _topGo[0]);
                            ap.Assign(Signs.Roles["TowerSW"], _botGo[2]);
                            ap.Assign(Signs.Roles["TowerS"], _topGo[1]);
                            ap.Assign(Signs.Roles["TowerSE"], _botGo[0]);
                        }
                        break;
                }
                ap.Assign(Signs.Roles["Puddle1"], _puddlesGo[0].GameObject);
                ap.Assign(Signs.Roles["Puddle2"], _puddlesGo[1].GameObject);
                _state.ExecuteAutomarkers(ap, Timing);
                _fired = true;
            }

        }

        #endregion

        public UltFuturesRewritten(State st) : base(st)
        {
            st.OnZoneChange += OnZoneChange;
        }

        private void OnCastBegin(uint src, uint dest, ushort actionId, float castTime, float rotation)
        {
            switch (actionId)
            {
                case AbilityBoundOfFaith:
                    CurrentPhase = PhaseEnum.P1_Faith;
                    break;
                case AbilityQuadrupleSlap:
                    CurrentPhase = PhaseEnum.P2_Shiva;
                    break;
            }
        }

        private void OnTether(uint src, uint dest, uint tetherId)
        {       
            switch (CurrentPhase)
            {
                case PhaseEnum.P1_Faith:
                    if (tetherId == TetherFire || tetherId == TetherLightning)
                    {
                        _fallFaithAm.FeedTether(src, dest, tetherId);
                    }
                    break;
            }
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
                _state.OnTether += OnTether;
                _state.OnStatusChange += _state_OnStatusChange;
            }
        }

        private void _state_OnStatusChange(uint src, uint dest, uint statusId, bool gained, float duration, int stacks)
        {
            switch (statusId)
            {
                case StatusPrey:
                    if (CurrentPhase == PhaseEnum.P1_Faith && gained == false)
                    {
                        _fallFaithAm.FeedStatus(dest);
                    }
                    break;
                case StatusChains:
                    if (CurrentPhase == PhaseEnum.P2_Shiva)
                    {
                        _lightRampantAm.FeedStatus(statusId, dest, gained);
                    }
                    break;
                case StatusWeightOfLight:
                    if (CurrentPhase == PhaseEnum.P2_Shiva)
                    {
                        _lightRampantAm.FeedStatus(statusId, dest, gained);
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
                _state.OnTether -= OnTether;
                _state.OnCastBegin -= OnCastBegin;
                _subbed = false;
            }
        }

        protected override bool ExecutionImplementation()
        {
            if (ZoneOk == true)
            {
                return base.ExecutionImplementation();
            }
            return false;
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
            bool newZoneOk = (newZone == 1238);
            if (newZoneOk == true && ZoneOk == false)
            {
                Log(State.LogLevelEnum.Info, null, "Content available");                
                _fallFaithAm = (FallOfFaithAM)Items["FallOfFaithAM"];
                _lightRampantAm = (LightRampantAM)Items["LightRampantAM"];
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
