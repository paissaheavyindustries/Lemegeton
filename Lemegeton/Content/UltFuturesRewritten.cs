using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Textures;
using ImGuiNET;
using Lemegeton.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using GameObjectPtr = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using static Lemegeton.Core.AutomarkerPrio;
using static Lemegeton.Core.State;
using static Lemegeton.Content.UltDragonsongReprise;
using static Lemegeton.Content.UltFuturesRewritten;

namespace Lemegeton.Content
{

    internal class UltFuturesRewritten : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

        private bool ZoneOk = false;
        private bool _subbed = false;

        private const int AbilityBoundOfFaith = 40165;
        private const int AbilityQuadrupleSlap = 40191;
        private const int AbilityUltimateRelativity = 40266;
        private const int AbilityDarkWater = 40270;
        private const int AbilityMemorysEnd = 40300;
        private const int AbilityCrystallizeTime = 40240;
        private const int AbilityQuicken = 40294;
        private const int AbilitySlow = 40295;
        private const int AbilityLonging = 40241;
        private const int AbilityTidalLight = 40251;

        private const int StatusPrey = 1051;
        private const int StatusChains = 4157;
        private const int StatusWeightOfLight = 4159;
        private const int StatusDarkBlizzard = 2462;
        private const int StatusDarkFire = 2455;
        private const int StatusRewind = 2459;
        private const int StatusDarkWater = 2461;
        private const int StatusDarkEruption = 2460;
        private const int StatusUnholyDarkness = 2454;
        private const int StatusWyrmfang = 3264;
        private const int StatusWyrmclaw = 3263;
        private const int StatusDarkAero = 2463;

        private const int TetherFire = 249;
        private const int TetherLightning = 287;

        private const int NameRyne = 12809;
        private const int NameGaia = 9832;

        private FallOfFaithAM _fallFaithAm;
        private LightRampantAM _lightRampantAm;
        private UltimateRelativityAM _ultRelAm;
        private DarkWaterAM _darkWaterAm;
        private DoubleTrouble _doubleTrouble;
        private CrystallizeTimeAM _crystallizeTimeAM;

#if !SANS_GOETIA
        private CTIndicator _ctIndicator;
#endif

        private enum PhaseEnum
        {
            P1_Start,
            P1_Faith,
            P2_Shiva,
            P3_Oracle,
            P3_DarkWater,
            P4_Both,
            P4_Crystallize,
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
                BoxJP
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
                            List<Party.PartyMember> _firsts = new List<Party.PartyMember>();
                            AutomarkerPrio Prio1 = new AutomarkerPrio() { Priority = PrioTypeEnum.Clockspots, StartingFrom = PrioDirectionEnum.NE };
                            Prio1.SortByPriority(_weightsGo);
                            _firsts.Add(_weightsGo[0]);
                            Prio1.StartingFrom = PrioDirectionEnum.N;
                            Prio1.SortByPriority(_weightsGo);
                            _firsts.Add(_weightsGo[0]);
                            Prio1.StartingFrom = PrioDirectionEnum.NW;
                            Prio1.SortByPriority(_weightsGo);
                            _firsts.Add(_weightsGo[0]);
                            int fc = (from fx in _firsts where fx == _weightsGo[0] select fx).Count();
                            if (fc < 2)
                            {
                                Log(State.LogLevelEnum.Debug, null, "Weight swap");
                                _weightsGo.Reverse();
                            }
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
                            Log(State.LogLevelEnum.Debug, null, "Final prio: {0}", String.Join(",", from cx in _finalPrio select cx.Name));
                        }
                        break;
                    case StratEnum.Box:
                    case StratEnum.BoxJP:
                        {
                            List<Party.PartyMember> _topGo = new List<Party.PartyMember>(
                                from ix in pty.Members
                                where
                                    AutomarkerPrio.JobToArchetype(ix.Job) == PrioArchetypeEnum.Support
                                    &&
                                    _chainsGo.Contains(ix) == true
                                select ix
                            );
                            _topGo.Sort((a, b) => a.X.CompareTo(b.X));                            
                            Log(State.LogLevelEnum.Debug, null, "Top prio: {0}", String.Join(",", from cx in _topGo select cx.Name));
                            List<Party.PartyMember> _botGo = new List<Party.PartyMember>(
                                from ix in pty.Members
                                where
                                    AutomarkerPrio.JobToArchetype(ix.Job) == PrioArchetypeEnum.DPS
                                    &&
                                    _chainsGo.Contains(ix) == true
                                select ix
                            );
                            _botGo.Sort((a, b) => a.X.CompareTo(b.X));
                            Log(State.LogLevelEnum.Debug, null, "Bottom prio: {0}", String.Join(",", from cx in _botGo select cx.Name));
                            if (_botGo.Count == 4)
                            {
                                if (_strat == StratEnum.Box)
                                {
                                    // rotate CW
                                    Party.PartyMember pm = _botGo[0];
                                    _botGo.RemoveAt(0);
                                    _topGo.Insert(0, pm);
                                }
                                else
                                {
                                    // NE/SE swap
                                    Party.PartyMember pm = _botGo[3];
                                    _botGo.RemoveAt(3);
                                    _topGo.Add(pm);
                                }
                                Log(State.LogLevelEnum.Debug, null, "New top prio: {0}", String.Join(",", from cx in _topGo select cx.Name));
                            }
                            else if (_topGo.Count == 4)
                            {
                                if (_strat == StratEnum.Box)
                                {
                                    // rotate CW
                                    Party.PartyMember pm = _topGo[3];
                                    _topGo.RemoveAt(3);
                                    _botGo.Add(pm);
                                }
                                else
                                {
                                    // NE/SE swap
                                    Party.PartyMember pm = _topGo[3];
                                    _topGo.RemoveAt(3);
                                    _botGo.Add(pm);
                                }
                                Log(State.LogLevelEnum.Debug, null, "New bottom prio: {0}", String.Join(",", from cx in _botGo select cx.Name));
                            }
                            ap.Assign(Signs.Roles["TowerN"], _botGo[1]);
                            ap.Assign(Signs.Roles["TowerS"], _topGo[1]);
                            if (_strat == StratEnum.Box)
                            {
                                // NW/NE swap
                                ap.Assign(Signs.Roles["TowerNW"], _topGo[2]);
                                ap.Assign(Signs.Roles["TowerNE"], _topGo[0]);
                                ap.Assign(Signs.Roles["TowerSW"], _botGo[2]);
                                ap.Assign(Signs.Roles["TowerSE"], _botGo[0]);
                            }
                            else
                            {
                                // SW/SE swap
                                ap.Assign(Signs.Roles["TowerNW"], _topGo[0]);
                                ap.Assign(Signs.Roles["TowerNE"], _topGo[2]);
                                ap.Assign(Signs.Roles["TowerSW"], _botGo[2]);
                                ap.Assign(Signs.Roles["TowerSE"], _botGo[0]);
                            }
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

        #region UltimateRelativityAM

        public class UltimateRelativityAM : Automarker
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
            private uint _support10 = 0;
            private uint _support20 = 0;
            private List<uint> _support30 = new List<uint>();
            private uint _supportIce = 0;
            private List<uint> _dps10 = new List<uint>();
            private uint _dps20 = 0;
            private uint _dps30 = 0;
            private uint _dpsIce = 0;

            public UltimateRelativityAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Prio = new AutomarkerPrio();
                Prio.Priority = AutomarkerPrio.PrioTypeEnum.PartyListOrder;
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Signs.SetRole("Support10", AutomarkerSigns.SignEnum.Attack1, false);
                Signs.SetRole("Support20", AutomarkerSigns.SignEnum.Attack2, false);
                Signs.SetRole("Support30_1", AutomarkerSigns.SignEnum.Attack3, false);
                Signs.SetRole("Support30_2", AutomarkerSigns.SignEnum.Attack4, false);
                Signs.SetRole("SupportIce", AutomarkerSigns.SignEnum.Attack1, false);
                Signs.SetRole("Dps10_1", AutomarkerSigns.SignEnum.Ignore1, false);
                Signs.SetRole("Dps10_2", AutomarkerSigns.SignEnum.Ignore2, false);
                Signs.SetRole("Dps20", AutomarkerSigns.SignEnum.Bind2, false);
                Signs.SetRole("Dps30", AutomarkerSigns.SignEnum.Bind3, false);
                Signs.SetRole("DpsIce", AutomarkerSigns.SignEnum.Bind3, false);
                Test = new System.Action(() => Signs.TestFunctionality(state, Prio, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _fired = false;
                _support10 = 0;
                _support20 = 0;
                _support30.Clear();
                _supportIce = 0;
                _dps10.Clear();
                _dps20 = 0;
                _dps30 = 0;
                _dpsIce = 0;
            }

        internal void FeedStatus(uint statusId, float duration, uint actor, bool gained)
            {
                if (Active == false)
                {
                    return;
                }
                if (statusId == StatusRewind)
                {
                    if (gained == false && _fired == true)
                    {
                        _state.ClearAutoMarkers();
                        _fired = false;
                    }
                    return;
                }
                IGameObject go = _state.GetActorById(actor);                
                PrioArchetypeEnum a = AutomarkerPrio.JobToArchetype(((IBattleChara)go).ClassJob.RowId);
                switch (statusId)
                {
                    case StatusDarkBlizzard:
                        if (a == PrioArchetypeEnum.Support)
                        {
                            _supportIce = actor;
                        }
                        else
                        {
                            _dpsIce = actor;
                        }
                        break;
                    case StatusDarkFire:
                        if (duration >= 10 && duration <= 12)
                        {
                            if (a == PrioArchetypeEnum.Support)
                            {
                                _support10 = actor;
                            }
                            else
                            {
                                _dps10.Add(actor);
                            }
                        }
                        if (duration >= 20 && duration <= 22)
                        {
                            if (a == PrioArchetypeEnum.Support)
                            {
                                _support20 = actor;
                            }
                            else
                            {
                                _dps20 = actor;
                            }
                        }
                        if (duration >= 30 && duration <= 32)
                        {
                            if (a == PrioArchetypeEnum.Support)
                            {
                                _support30.Add(actor);
                            }
                            else
                            {
                                _dps30 = actor;
                            }
                        }
                        break;
                }
                if (
                    _dps10.Count == 2 && _support30.Count == 2
                    && (_supportIce != 0 || _dpsIce != 0)
                    && (_support10 != 0 || _dps30 != 0)
                    && _support20 != 0 && _dps20 != 0
                )
                {
                    Log(State.LogLevelEnum.Debug, null, string.Format("Ready for markers"));
                }
            }

        }

        #endregion

        #region DarkWaterAM

        public class DarkWaterAM : Automarker
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

            public List<uint> _water10 = new List<uint>();
            public List<uint> _water29 = new List<uint>();
            public List<uint> _water38 = new List<uint>();

            public DarkWaterAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Prio = new AutomarkerPrio();
                Prio.Priority = AutomarkerPrio.PrioTypeEnum.Role;
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Signs.SetRole("Water10_1", AutomarkerSigns.SignEnum.Attack1, false);
                Signs.SetRole("Water10_2", AutomarkerSigns.SignEnum.Attack2, false);
                Signs.SetRole("Water29_1", AutomarkerSigns.SignEnum.Bind1, false);
                Signs.SetRole("Water29_2", AutomarkerSigns.SignEnum.Bind2, false);
                Signs.SetRole("Water38_1", AutomarkerSigns.SignEnum.Ignore1, false);
                Signs.SetRole("Water38_2", AutomarkerSigns.SignEnum.Ignore2, false);
                Signs.SetRole("Nothing_1", AutomarkerSigns.SignEnum.None, false);
                Signs.SetRole("Nothing_2", AutomarkerSigns.SignEnum.None, false);
                Test = new System.Action(() => Signs.TestFunctionality(state, Prio, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _fired = false;
                _water10.Clear();
                _water29.Clear();
                _water38.Clear();
            }

            internal void FeedStatus(float duration, uint actor, bool gained)
            {
                if (Active == false)
                {
                    return;
                }
                if (gained == false)
                {
                    bool soft = _state.cfg.AutomarkerSoft == true || AsSoftmarker == true;
                    _state.ClearMarkerOn(actor, soft == false, soft == true);
                    return;
                }
                if (duration >= 9 && duration <= 11)
                {
                    _water10.Add(actor);
                }
                if (duration >= 28 && duration <= 30)
                {
                    _water29.Add(actor);
                }
                if (duration >= 37 && duration <= 39)
                {
                    _water38.Add(actor);
                }
                if (_water10.Count != 2 || _water29.Count != 2 || _water38.Count != 2)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Ready for automarkers");
                AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                Party pty = _state.GetPartyMembers();
                List<Party.PartyMember> waters10 = pty.GetByActorIds(_water10);
                List<Party.PartyMember> waters29 = pty.GetByActorIds(_water29);
                List<Party.PartyMember> waters38 = pty.GetByActorIds(_water38);
                List<Party.PartyMember> nothings = (from px in pty.Members where waters10.Contains(px) == false && waters29.Contains(px) == false && waters38.Contains(px) == false select px).ToList();
                Prio.SortByPriority(waters10);
                Prio.SortByPriority(waters29);
                Prio.SortByPriority(waters38);
                Prio.SortByPriority(nothings);
                ap.Assign(Signs.Roles["Water10_1"], waters10[0].GameObject);
                ap.Assign(Signs.Roles["Water10_2"], waters10[1].GameObject);
                ap.Assign(Signs.Roles["Water29_1"], waters29[0].GameObject);
                ap.Assign(Signs.Roles["Water29_2"], waters29[1].GameObject);
                ap.Assign(Signs.Roles["Water38_1"], waters38[0].GameObject);
                ap.Assign(Signs.Roles["Water38_2"], waters38[1].GameObject);
                ap.Assign(Signs.Roles["Nothing_1"], nothings[0].GameObject);
                ap.Assign(Signs.Roles["Nothing_2"], nothings[1].GameObject);
                _state.ExecuteAutomarkers(ap, Timing);
                _fired = true;
            }

        }

        #endregion

        #region DoubleTrouble

        public class DoubleTrouble : Core.ContentItem
        {

            public ulong _idRyne = 0;
            public ulong _idGaia = 0;

            public override FeaturesEnum Features => FeaturesEnum.Drawing;

            [AttributeOrderNumber(2000)]
            public Overlay Area { get; set; }

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public System.Action Test { get; set; }

            public DoubleTrouble(State state) : base(state)
            {
                Enabled = false;
                Area = new Overlay();
                Area.Renderer += Render;
                Test = new System.Action(() => TestFunctionality());
            }

            public void TestFunctionality()
            {
                if (_idRyne > 0 || _idGaia > 0)
                {
                    _idRyne = 0;
                    _idGaia = 0;
                    return;
                }
                _state.InvokeZoneChange(1238);
                IGameObject me = _state.cs.LocalPlayer as IGameObject;
                _idRyne = me.GameObjectId;
                _idGaia = me.GameObjectId;
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
                    _idGaia = go.GameObjectId;
                    Log(State.LogLevelEnum.Debug, null, "Testing from {0} to {1}", me, go);
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Testing on self");
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _idRyne = 0;
                _idGaia = 0;
            }

            protected void Render(ImDrawListPtr draw, bool configuring)
            {
                IGameObject goR = configuring == true ? _state.cs.LocalPlayer : _state.GetActorById(_idRyne);
                if (goR == null)
                {
                    return;
                }
                IGameObject goG = configuring == true ? _state.cs.LocalPlayer : _state.GetActorById(_idGaia);
                if (goG == null)
                {
                    return;
                }
                ICharacter chR = (ICharacter)goR;
                ICharacter chG = (ICharacter)goG;
                ISharedImmediateTexture bws, tws;
                float x = Area.X;
                float y = Area.Y;
                float w = Area.Width;
                float h = Area.Height;
                int pad = Area.Padding;
                float hhp = (float)chR.CurrentHp / (float)chR.MaxHp * 100.0f;
                float nhp = (float)chG.CurrentHp / (float)chG.MaxHp * 100.0f;
                float dhp = Math.Abs(hhp - nhp);
                Vector4 hcol, ncol, wcol;
                hcol = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
                ncol = hcol;
                wcol = hcol;
                float time = (float)(DateTime.Now - DateTime.Today).TotalMilliseconds / 200.0f;
                if (dhp > 3.0f)
                {
                    float ang = (float)Math.Abs(Math.Cos(time * 2.0f));
                    if (hhp > nhp)
                    {
                        hcol = new Vector4(1.0f, 0.0f, 0.0f, ang);
                    }
                    else
                    {
                        ncol = new Vector4(1.0f, 0.0f, 0.0f, ang);
                    }
                    wcol = new Vector4(1.0f, ang, 0.0f, 1.0f);
                }
                else if (dhp > 2.0f)
                {
                    float ang = (float)Math.Abs(Math.Cos(time));
                    if (hhp > nhp)
                    {
                        hcol = new Vector4(1.0f, 0.5f, 0.0f, ang);
                    }
                    else
                    {
                        ncol = new Vector4(1.0f, 0.5f, 0.0f, ang);
                    }
                    wcol = new Vector4(1.0f, 0.5f + (ang * 0.5f), 0.0f, 1.0f);
                }
                else if (dhp > 1.0f)
                {
                    if (hhp > nhp)
                    {
                        hcol = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
                    }
                    else
                    {
                        ncol = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
                    }
                    wcol = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
                }
                draw.AddRectFilled(
                    new Vector2(x, y),
                    new Vector2(x + w, y + h),
                    ImGui.GetColorU32(Area.BackgroundColor)
                );
                tws = _state.plug._ui.GetMiscIcon(UserInterface.MiscIconEnum.Ryne);
                IDalamudTextureWrap tw = tws.GetWrapOrEmpty();
                bws = _state.plug._ui.GetMiscIcon(UserInterface.MiscIconEnum.LightCircle);
                IDalamudTextureWrap bw = bws.GetWrapOrEmpty();
                draw.AddImage(
                    bw.ImGuiHandle,
                    new Vector2(x + pad, y + (h / 2.0f) - (tw.Height / 2.0f)),
                    new Vector2(x + tw.Width + pad, y + (h / 2.0f) + (tw.Height / 2.0f)),
                    new Vector2(1.0f, 0.0f),
                    new Vector2(0.0f, 1.0f),
                    ImGui.GetColorU32(hcol)
                );
                draw.AddImage(
                    bw.ImGuiHandle,
                    new Vector2(x + w - tw.Width - pad, y + (h / 2.0f) - (tw.Height / 2.0f)),
                    new Vector2(x + w - pad, y + (h / 2.0f) + (tw.Height / 2.0f)),
                    new Vector2(1.0f, 0.0f),
                    new Vector2(0.0f, 1.0f),
                    ImGui.GetColorU32(ncol)
                );
                draw.AddImage(
                    tw.ImGuiHandle,
                    new Vector2(x + pad, y + (h / 2.0f) - (tw.Height / 2.0f)),
                    new Vector2(x + tw.Width + pad, y + (h / 2.0f) + (tw.Height / 2.0f)),
                    new Vector2(1.0f, 0.0f),
                    new Vector2(0.0f, 1.0f)
                );
                tws = _state.plug._ui.GetMiscIcon(UserInterface.MiscIconEnum.Gaia);
                tw = tws.GetWrapOrEmpty();
                draw.AddImage(
                    tw.ImGuiHandle,
                    new Vector2(x + w - tw.Width - pad, y + (h / 2.0f) - (tw.Height / 2.0f)),
                    new Vector2(x + w - pad, y + (h / 2.0f) + (tw.Height / 2.0f)),
                    new Vector2(0.0f, 0.0f),
                    new Vector2(1.0f, 1.0f)
                );
                float textSize = 24.0f, scale;
                string temp;
                Vector2 sz;
                temp = String.Format("{0:0.0} %", hhp);
                sz = ImGui.CalcTextSize(temp);
                scale = textSize / sz.Y;
                sz = new Vector2(sz.X * scale, sz.Y * scale);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(Area.Padding + x + (tw.Width / 2.0f) - (sz.X / 2.0f) + 1.0f, -Area.Padding + y + Area.Height - sz.Y + 1.0f), ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 1.0f)), temp);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(Area.Padding + x + (tw.Width / 2.0f) - (sz.X / 2.0f), -Area.Padding + y + Area.Height - sz.Y), ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)), temp);
                temp = String.Format("{0:0.0} %", nhp);
                sz = ImGui.CalcTextSize(temp);
                scale = textSize / sz.Y;
                sz = new Vector2(sz.X * scale, sz.Y * scale);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(-Area.Padding + x + w - (tw.Width / 2.0f) - (sz.X / 2.0f) + 1.0f, -Area.Padding + y + Area.Height - sz.Y + 1.0f), ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 1.0f)), temp);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(-Area.Padding + x + w - (tw.Width / 2.0f) - (sz.X / 2.0f), -Area.Padding + y + Area.Height - sz.Y), ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)), temp);
                temp = chR.Name.ToString();
                sz = ImGui.CalcTextSize(temp);
                scale = textSize / sz.Y;
                sz = new Vector2(sz.X * scale, sz.Y * scale);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(Area.Padding + x + 1.0f, Area.Padding + y + 1.0f), ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 1.0f)), temp);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(Area.Padding + x, Area.Padding + y), ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)), temp);
                temp = chG.Name.ToString();
                sz = ImGui.CalcTextSize(temp);
                scale = textSize / sz.Y;
                sz = new Vector2(sz.X * scale, sz.Y * scale);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(-Area.Padding + x + Area.Width - sz.X + 1.0f, Area.Padding + y + 1.0f), ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 1.0f)), temp);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(-Area.Padding + x + Area.Width - sz.X, Area.Padding + y), ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)), temp);
                temp = String.Format("{0:0.0} %", dhp);
                textSize = 40.0f;
                sz = ImGui.CalcTextSize(temp);
                scale = textSize / sz.Y;
                sz = new Vector2(sz.X * scale, sz.Y * scale);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(x + (w / 2.0f) - (sz.X / 2.0f) + 1.0f, y + (h / 2.0f) - (sz.Y / 2.0f) + 1.0f), ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 1.0f)), temp);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(x + (w / 2.0f) - (sz.X / 2.0f), y + (h / 2.0f) - (sz.Y / 2.0f)), ImGui.GetColorU32(wcol), temp);
            }

            protected override bool ExecutionImplementation()
            {
                ImDrawListPtr draw;
                if (_idRyne == 0 || _idGaia == 0)
                {
                    return false;
                }
                if (_state.StartDrawing(out draw) == false)
                {
                    return false;
                }
                Render(draw, false);
                return true;
            }

        }

        #endregion

        #region CrystallizeTimeAM

        public class CrystallizeTimeAM : Automarker
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

            private uint _water = 0;
            private uint _earth = 0;
            private uint _fire = 0;
            private List<uint> _aero = new List<uint>();
            private List<uint> _blizzard = new List<uint>();
            private List<uint> _red = new List<uint>();
            private List<uint> _blue = new List<uint>();

            private bool _fired = false;

            public CrystallizeTimeAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Prio = new AutomarkerPrio();
                Prio.Priority = AutomarkerPrio.PrioTypeEnum.CongaX;
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Signs.SetRole("Aero1", AutomarkerSigns.SignEnum.Ignore1, false);
                Signs.SetRole("Aero2", AutomarkerSigns.SignEnum.Ignore2, false);
                Signs.SetRole("RedBlizzard1", AutomarkerSigns.SignEnum.Bind1, false);
                Signs.SetRole("RedBlizzard2", AutomarkerSigns.SignEnum.Bind2, false);
                Signs.SetRole("Fire", AutomarkerSigns.SignEnum.Attack1, false);
                Signs.SetRole("BlueBlizzard", AutomarkerSigns.SignEnum.Attack2, false);
                Signs.SetRole("Water", AutomarkerSigns.SignEnum.Attack3, false);
                Signs.SetRole("Earth", AutomarkerSigns.SignEnum.Attack4, false);
                Test = new System.Action(() => Signs.TestFunctionality(state, Prio, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _fired = false;
                _water = 0;
                _earth = 0;
                _fire = 0;
                _aero.Clear();
                _blizzard.Clear();
                _red.Clear();
                _blue.Clear();
            }

            internal void FeedStatus(uint statusId, uint actor, bool gained)
            {
                if (Active == false)
                {
                    return;
                }
                if (gained == false)
                {
                    if (_fired == true && statusId == StatusRewind)
                    {
                        _state.ClearAutoMarkers();
                        Reset();
                    }
                    return;
                }
                switch (statusId)
                {
                    case StatusDarkAero:
                        _aero.Add(actor);
                        break;
                    case StatusDarkBlizzard:
                        _blizzard.Add(actor);
                        break;
                    case StatusWyrmclaw:
                        _red.Add(actor);
                        break;
                    case StatusWyrmfang:
                        _blue.Add(actor);
                        break;
                    case StatusDarkWater:
                        _water = actor;
                        break;
                    case StatusUnholyDarkness:
                        _earth = actor;
                        break;
                    case StatusDarkEruption:
                        _fire = actor;
                        break;
                }
                if (_aero.Count != 2 || _blizzard.Count != 3 || _red.Count != 4 || _blue.Count != 4 || _water == 0 || _earth == 0 || _fire == 0)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Ready for automarkers");
                AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                Party pty = _state.GetPartyMembers();
                List<Party.PartyMember> aeros = (from px in pty.Members where _aero.Contains((uint)px.ObjectId) == true select px).ToList();
                List<Party.PartyMember> redices = (from px in pty.Members where _blizzard.Contains((uint)px.ObjectId) == true && _red.Contains((uint)px.ObjectId) == true select px).ToList();
                Prio.SortByPriority(aeros);
                Prio.SortByPriority(redices);
                ap.Assign(Signs.Roles["Aero1"], aeros[0].GameObject);
                ap.Assign(Signs.Roles["Aero2"], aeros[1].GameObject);
                ap.Assign(Signs.Roles["RedBlizzard1"], redices[0].GameObject);
                ap.Assign(Signs.Roles["RedBlizzard2"], redices[1].GameObject);
                ap.Assign(Signs.Roles["Fire"], (from px in pty.Members where px.ObjectId == _fire select px).FirstOrDefault().GameObject);
                ap.Assign(Signs.Roles["BlueBlizzard"], (from px in pty.Members where _blizzard.Contains((uint)px.ObjectId) == true && _blue.Contains((uint)px.ObjectId) == true select px).FirstOrDefault().GameObject);
                ap.Assign(Signs.Roles["Water"], (from px in pty.Members where px.ObjectId == _water select px).FirstOrDefault().GameObject);
                ap.Assign(Signs.Roles["Earth"], (from px in pty.Members where px.ObjectId == _earth select px).FirstOrDefault().GameObject);
                _state.ExecuteAutomarkers(ap, Timing);
                _fired = true;
            }

        }

        #endregion

#if !SANS_GOETIA
        #region CTIndicator

        public class CTIndicator : Core.ContentItem
        {

            public override FeaturesEnum Features => FeaturesEnum.Drawing;

            [AttributeOrderNumber(1000)]
            public bool Hourglasses { get; set; } = true;
            [AttributeOrderNumber(1001)]
            public bool Cleanses { get; set; } = true;
            [AttributeOrderNumber(1001)]
            public bool Corner { get; set; } = true;

            [AttributeOrderNumber(1500)]
            public Vector4 ExplodingHourglassColor { get; set; } = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);

            [DebugOption]
            [AttributeOrderNumber(2000)]
            public System.Action Test { get; set; }

            private List<Tuple<float, float, DateTime>> _hourglasses = new List<Tuple<float, float, DateTime>>();
            private List<Tuple<float, float, DateTime>> _cleanses = new List<Tuple<float, float, DateTime>>();
            private bool _addedMid = false;
            private int _northSouth = 0;
            private int _westEast = 0;
            private Random r = new Random();
            private DateTime _cornerStart = DateTime.MinValue;

            public CTIndicator(State state) : base(state)
            {
                Enabled = false;
                Test = new System.Action(() => TestFunctionality());
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _hourglasses.Clear();
                _cleanses.Clear();
                _northSouth = 0;
                _westEast = 0;
                _addedMid = false;
                _cornerStart = DateTime.MinValue;
            }

            public void FeedAction(int actionId, ulong dest)
            {
                switch (actionId)
                {
                    case AbilitySlow:
                        {
                            IGameObject go = _state.GetActorById(dest);
                            Log(State.LogLevelEnum.Debug, null, "Registered slow hourglass {0:X} at {1},{2}",  dest, go.Position.X, go.Position.Z);
                            _hourglasses.Add(new Tuple<float, float, DateTime>(go.Position.X, go.Position.Z, DateTime.Now.AddSeconds(12.0)));
                            if (_addedMid == false)
                            {
                                float xd = go.Position.X - 100.0f;
                                float zd = go.Position.Z - 100.0f;
                                Log(State.LogLevelEnum.Debug, null, "Adding mid to {0},{1} and {2},{3}", 100.0f - xd, go.Position.Z, go.Position.X, 100.0f - zd);
                                _hourglasses.Add(new Tuple<float, float, DateTime>(100.0f - xd, go.Position.Z, DateTime.Now.AddSeconds(7.0)));
                                _hourglasses.Add(new Tuple<float, float, DateTime>(go.Position.X, 100.0f - zd, DateTime.Now.AddSeconds(7.0)));
                                _addedMid = true;
                            }
                        }
                        break;
                    case AbilityQuicken:
                        {
                            IGameObject go = _state.GetActorById(dest);
                            Log(State.LogLevelEnum.Debug, null, "Registered fast hourglass {0:X} at {1},{2}", dest, go.Position.X, go.Position.Z);
                            _hourglasses.Add(new Tuple<float, float, DateTime>(go.Position.X, go.Position.Z, DateTime.Now.AddSeconds(2.0)));
                        }
                        break;
                    case AbilityLonging:
                        {
                            IGameObject go = _state.GetActorById(dest);
                            Log(State.LogLevelEnum.Debug, null, "Registered cleanse {0:X} at {1},{2}", dest, go.Position.X, go.Position.Z);
                            _cleanses.Add(new Tuple<float, float, DateTime>(go.Position.X, go.Position.Z, DateTime.Now));
                        }
                        break;
                    case AbilityTidalLight:
                        {
                            IGameObject go = _state.GetActorById(dest);
                            if (go.Position.X < 95.0)
                            {
                                _westEast = 1;
                            }
                            if (go.Position.X > 105.0)
                            {
                                _westEast = 2;
                            }
                            if (go.Position.Z < 95.0)
                            {
                                _northSouth = 1;
                            }
                            if (go.Position.Z > 105.0)
                            {
                                _northSouth = 2;
                            }
                        }
                        break;
                }
            }

            public void TestFunctionality()
            {
                if (_westEast != 0)
                {
                    Reset();
                }
                else
                {
                    Reset();
                    _state.InvokeZoneChange(1238);
                    IGameObject me = _state.cs.LocalPlayer as IGameObject;
                    _hourglasses.Add(new Tuple<float, float, DateTime>(100.0f, 89.0f, DateTime.Now.AddSeconds(2.0)));
                    _hourglasses.Add(new Tuple<float, float, DateTime>(100.0f, 111.0f, DateTime.Now.AddSeconds(2.0)));
                    _hourglasses.Add(new Tuple<float, float, DateTime>(90.47372f, 94.5f, DateTime.Now.AddSeconds(12.0)));
                    float xd = 90.47372f - 100.0f;
                    float zd = 94.5f - 100.0f;
                    _hourglasses.Add(new Tuple<float, float, DateTime>(100.0f - xd, 94.5f, DateTime.Now.AddSeconds(7.0)));
                    _hourglasses.Add(new Tuple<float, float, DateTime>(90.47372f, 100.0f - zd, DateTime.Now.AddSeconds(7.0)));
                    _hourglasses.Add(new Tuple<float, float, DateTime>(109.5263f, 105.5f, DateTime.Now.AddSeconds(12.0)));
                    _cleanses.Add(new Tuple<float, float, DateTime>(105f, 100.0f, DateTime.Now));
                    _cleanses.Add(new Tuple<float, float, DateTime>(95f, 100.0f, DateTime.Now));
                    _cleanses.Add(new Tuple<float, float, DateTime>(102f, 103.0f, DateTime.Now.AddSeconds(5.0)));
                    _cleanses.Add(new Tuple<float, float, DateTime>(98f, 103.0f, DateTime.Now.AddSeconds(5.0)));
                    _westEast = r.Next(1, 3);
                    _northSouth = r.Next(1, 3);
                    Log(State.LogLevelEnum.Debug, null, "Testing");
                }
            }

            private void HighlightHourglasses()
            {
                ImDrawListPtr draw;
                if (_hourglasses.Count == 0)
                {
                    return;
                }
                if (_state.StartDrawing(out draw) == false)
                {
                    return;
                }
                DateTime dt = DateTime.Now;
                float radius = 12.0f;
                Vector4 hcol = new Vector4(
                    ExplodingHourglassColor.X,
                    ExplodingHourglassColor.Y,
                    ExplodingHourglassColor.Z,
                    ExplodingHourglassColor.W * 0.2f
                );
                foreach (var hg in _hourglasses)
                {
                    if (dt > hg.Item3 || hg.Item3.AddSeconds(-5.0) > dt)
                    {
                        continue;
                    }
                    float radx = radius * (1.0f - (float)((hg.Item3 - dt).TotalSeconds / 5.0));
                    for (int i = 0; i <= 48; i++)
                    {
                        Vector3 mauw = _state.plug._ui.TranslateToScreen(
                            hg.Item1 + (radx * Math.Sin((Math.PI / 24.0) * i)),
                            0.0,
                            hg.Item2 + (radx * Math.Cos((Math.PI / 24.0) * i))
                        );
                        draw.PathLineTo(new Vector2(mauw.X, mauw.Y));
                    }
                    draw.PathFillConvex(ImGui.GetColorU32(hcol));
                    for (int i = 0; i <= 48; i++)
                    {
                        Vector3 mauw = _state.plug._ui.TranslateToScreen(
                            hg.Item1 + (radius * Math.Sin((Math.PI / 24.0) * i)),
                            0.0,
                            hg.Item2 + (radius * Math.Cos((Math.PI / 24.0) * i))
                        );
                        draw.PathLineTo(new Vector2(mauw.X, mauw.Y));
                    }
                    draw.PathStroke(
                        ImGui.GetColorU32(ExplodingHourglassColor),
                        ImDrawFlags.None,
                        4.0f
                    );
                }
            }

            private void HighlightCleanses()
            {
                ImDrawListPtr draw;
                if (_cleanses.Count == 0)
                {
                    return;
                }
                if (_state.StartDrawing(out draw) == false)
                {
                    return;
                }
                DateTime dt = DateTime.Now;                
                foreach (var cl in _cleanses)
                {
                    if (dt < cl.Item3 || cl.Item3.AddSeconds(17.0) < dt)
                    {
                        continue;
                    }
                    float time = (float)(cl.Item3.AddSeconds(17.0) - dt).TotalSeconds / 17.0f;
                    float radius = 1.0f + (float)Math.Abs(Math.Cos((cl.Item3.AddSeconds(17.0) - dt).TotalSeconds * 3.0f) * 0.2f);
                    for (int i = 0; i <= 12; i++)
                    {
                        Vector3 mauw = _state.plug._ui.TranslateToScreen(
                            cl.Item1 + (radius * Math.Sin((Math.PI / 6.0) * i)),
                            0.0,
                            cl.Item2 + (radius * Math.Cos((Math.PI / 6.0) * i))
                        );
                        draw.PathLineTo(new Vector2(mauw.X, mauw.Y));
                    }
                    draw.PathStroke(
                        ImGui.GetColorU32(new Vector4(1.0f, time, 0.0f, 1.0f)),
                        ImDrawFlags.None,
                        4.0f
                    );
                }
            }

            private void HighlightCorner()
            {
                ImDrawListPtr draw;
                if (_westEast == 0 || _northSouth == 0)
                {
                    return;
                }
                if (_state.StartDrawing(out draw) == false)
                {
                    return;
                }
                if (_cornerStart == DateTime.MinValue)
                {
                    _cornerStart = DateTime.Now;
                }
                if (_cornerStart.AddSeconds(10.0) < DateTime.Now)
                {
                    _westEast = 0;
                    _northSouth = 0;
                    return;
                }
                float x = _westEast == 1 ? 85.0f : 115.0f;
                float z = _northSouth == 1 ? 85.0f : 115.0f;
                Vector3 start = _state.plug._ui.TranslateToScreen(_state.cs.LocalPlayer.Position.X, 0.0, _state.cs.LocalPlayer.Position.Z);
                Vector3 end = _state.plug._ui.TranslateToScreen(x, 0.0, z);
                draw.PathLineTo(new Vector2(start.X, start.Y));
                draw.PathLineTo(new Vector2(end.X, end.Y));
                draw.PathStroke(
                    ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 0.0f, 1.0f)),
                    ImDrawFlags.None,
                    4.0f
                );
            }

            protected override bool ExecutionImplementation()
            {
                if (Hourglasses == true)
                {
                    HighlightHourglasses();
                }
                if (Cleanses == true)
                {
                    HighlightCleanses();
                }
                if (Corner == true)
                {
                    HighlightCorner();
                }
                return true;
            }

        }

        #endregion
#endif

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
                case AbilityUltimateRelativity:
                    CurrentPhase = PhaseEnum.P3_Oracle;
                    break;
                case AbilityDarkWater:
                    CurrentPhase = PhaseEnum.P3_DarkWater;
                    break;
                case AbilityMemorysEnd:
                    CurrentPhase = PhaseEnum.P4_Both;
                    break;
                case AbilityCrystallizeTime:
                    CurrentPhase = PhaseEnum.P4_Crystallize;
                    break;
#if !SANS_GOETIA
                case AbilityTidalLight:
                    _ctIndicator.FeedAction(actionId, src);
                    break;
#endif
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
                _state.OnStatusChange += OnStatusChange;
                _state.OnCombatantAdded += OnCombatantAdded;
                _state.OnCombatantRemoved += OnCombatantRemoved;
                _state.OnAction += OnAction;
            }
        }

        private void OnAction(uint src, uint dest, ushort actionId)
        {
            switch (actionId)
            {
#if !SANS_GOETIA
                case AbilityQuicken:
                    if (CurrentPhase == PhaseEnum.P4_Crystallize)
                    {
                        _ctIndicator.FeedAction(actionId, dest);
                    }
                    break;
                case AbilitySlow:
                    if (CurrentPhase == PhaseEnum.P4_Crystallize)
                    {
                        _ctIndicator.FeedAction(actionId, dest);
                    }
                    break;
                case AbilityLonging:
                    if (dest > 0)
                    {
                        _ctIndicator.FeedAction(actionId, src);
                    }
                    break;
#endif
            }
        }

        private void OnStatusChange(uint src, uint dest, uint statusId, bool gained, float duration, int stacks)
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
                case StatusDarkFire:
                case StatusRewind:
                case StatusDarkBlizzard:
                    if (CurrentPhase == PhaseEnum.P3_Oracle)
                    {
                        _ultRelAm.FeedStatus(statusId, duration, dest, gained);
                    }
                    if (CurrentPhase == PhaseEnum.P4_Crystallize && statusId != StatusDarkFire)
                    {
                        _crystallizeTimeAM.FeedStatus(statusId, dest, gained);
                    }
                    break;
                case StatusDarkWater:
                    if (CurrentPhase == PhaseEnum.P3_DarkWater)
                    {
                        _darkWaterAm.FeedStatus(duration, dest, gained);
                    }
                    if (CurrentPhase == PhaseEnum.P4_Crystallize)
                    {
                        _crystallizeTimeAM.FeedStatus(statusId, dest, gained);
                    }
                    break;
                case StatusUnholyDarkness:
                case StatusDarkEruption:
                case StatusDarkAero:
                case StatusWyrmclaw:
                case StatusWyrmfang:
                    if (CurrentPhase == PhaseEnum.P4_Crystallize)
                    {
                        _crystallizeTimeAM.FeedStatus(statusId, dest, gained);
                    }
                    break;
            }
        }

        private void OnCombatantRemoved(ulong actorId, nint addr)
        {
            if (CurrentPhase == PhaseEnum.P4_Both || CurrentPhase == PhaseEnum.P4_Crystallize)
            {
                if (actorId == _doubleTrouble._idRyne && actorId > 0)
                {
                    Log(State.LogLevelEnum.Debug, null, "Ryne is gone");
                    _doubleTrouble._idRyne = 0;
                }
                if (actorId == _doubleTrouble._idGaia && actorId > 0)
                {
                    Log(State.LogLevelEnum.Debug, null, "Gaia is gone");
                    _doubleTrouble._idGaia = 0;
                }
            }
        }

        private void OnCombatantAdded(Dalamud.Game.ClientState.Objects.Types.IGameObject go)
        {
            if (CurrentPhase == PhaseEnum.P4_Both || CurrentPhase == PhaseEnum.P4_Crystallize)
            {
                if (go is ICharacter)
                {
                    ICharacter ch = go as ICharacter;
                    if (ch.MaxHp != 11943869)
                    {
                        return;
                    }
                    if (ch.NameId == NameRyne)
                    {
                        Log(State.LogLevelEnum.Debug, null, "Ryne found as {0:X8}", go.GameObjectId);
                        _doubleTrouble._idRyne = go.GameObjectId;
                    }
                    else if (ch.NameId == NameGaia)
                    {
                        Log(State.LogLevelEnum.Debug, null, "Gaia found as {0:X8}", go.GameObjectId);
                        _doubleTrouble._idGaia = go.GameObjectId;
                    }
                }
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
                _state.OnCombatantAdded -= OnCombatantAdded;
                _state.OnCombatantRemoved -= OnCombatantRemoved;
                _state.OnAction -= OnAction;
                _state.OnStatusChange -= OnStatusChange;
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
                _ultRelAm = (UltimateRelativityAM)Items["UltimateRelativityAM"];
                _darkWaterAm = (DarkWaterAM)Items["DarkWaterAM"];
                _doubleTrouble = (DoubleTrouble)Items["DoubleTrouble"];
#if !SANS_GOETIA
                _ctIndicator = (CTIndicator)Items["CTIndicator"];
#endif                
                _crystallizeTimeAM = (CrystallizeTimeAM)Items["CrystallizeTimeAM"];
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
