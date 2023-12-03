using System;
using Lemegeton.Core;
using System.Collections.Generic;
using System.Linq;

namespace Lemegeton.Content
{

    internal class UltDragonsongReprise : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.None;
        
        private const int StatusEntangledFlames = 2759;
        private const int StatusSpreadingFlames = 2758;
        private const int StatusThunderstruck = 2833;
        private const int StatusPrey = 562;
        private const int StatusDoom = 2976;

        private bool ZoneOk = false;

        private MeteorAM _meteorAm;
        private ChainLightningAm _chainLightningAm;
        private DothAM _dothAm;
        private WrothAM _wrothAm;

        #region MeteorAM

        public class MeteorAM : Automarker
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

            private List<uint> _meteors = new List<uint>();
            private bool _fired = false;

            public MeteorAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Prio = new AutomarkerPrio();
                Prio.Priority = AutomarkerPrio.PrioTypeEnum.Job;
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Signs.SetRole("Meteor1", AutomarkerSigns.SignEnum.Ignore1, false);
                Signs.SetRole("Meteor2", AutomarkerSigns.SignEnum.Ignore2, false);
                Signs.SetRole("MeteorRole1", AutomarkerSigns.SignEnum.Bind1, false);
                Signs.SetRole("MeteorRole2", AutomarkerSigns.SignEnum.Bind2, false);
                Signs.SetRole("NonMeteor1", AutomarkerSigns.SignEnum.Attack1, false);
                Signs.SetRole("NonMeteor2", AutomarkerSigns.SignEnum.Attack2, false);
                Signs.SetRole("NonMeteor3", AutomarkerSigns.SignEnum.Attack3, false);
                Signs.SetRole("NonMeteor4", AutomarkerSigns.SignEnum.Attack4, false);
                Test = new System.Action(() => Signs.TestFunctionality(state, Prio, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _fired = false;
                _meteors.Clear();
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
                        Log(State.LogLevelEnum.Debug, null, "Clearing automarkers", statusId);
                        Reset();
                        _state.ClearAutoMarkers();
                    }
                    return;
                }
                _meteors.Add(actorId);
                if (_meteors.Count < 2)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Ready for automarkers");
                Party pty = _state.GetPartyMembers();
                List<Party.PartyMember> _meteorsGo = new List<Party.PartyMember>(
                    from ix in pty.Members join jx in _meteors on ix.ObjectId equals jx select ix
                );
                List<Party.PartyMember> _meteorRoleGo, _nonMeteorGo;
                AutomarkerPrio.PrioTrinityEnum role = AutomarkerPrio.JobToTrinity(_meteorsGo[0].Job);
                if (role != AutomarkerPrio.PrioTrinityEnum.DPS)
                {
                    _meteorRoleGo = new List<Party.PartyMember>(
                        from ix in pty.Members where 
                            AutomarkerPrio.JobToTrinity(ix.Job) != AutomarkerPrio.PrioTrinityEnum.DPS
                            && _meteors.Contains(ix.ObjectId) == false
                            select ix
                    );
                    _nonMeteorGo = new List<Party.PartyMember>(
                        from ix in pty.Members
                        where AutomarkerPrio.JobToTrinity(ix.Job) == AutomarkerPrio.PrioTrinityEnum.DPS
                        select ix
                    );
                }
                else
                {
                    _meteorRoleGo = new List<Party.PartyMember>(
                        from ix in pty.Members
                        where
                            AutomarkerPrio.JobToTrinity(ix.Job) == AutomarkerPrio.PrioTrinityEnum.DPS
                            && _meteors.Contains(ix.ObjectId) == false
                        select ix
                    );
                    _nonMeteorGo = new List<Party.PartyMember>(
                        from ix in pty.Members
                        where AutomarkerPrio.JobToTrinity(ix.Job) != AutomarkerPrio.PrioTrinityEnum.DPS
                        select ix
                    );
                }
                Prio.SortByPriority(_meteorsGo);
                Prio.SortByPriority(_meteorRoleGo);
                Prio.SortByPriority(_nonMeteorGo);
                AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                ap.Assign(Signs.Roles["Meteor1"], _meteorsGo[0].GameObject);
                ap.Assign(Signs.Roles["Meteor2"], _meteorsGo[1].GameObject);
                ap.Assign(Signs.Roles["MeteorRole1"], _meteorRoleGo[0].GameObject);
                ap.Assign(Signs.Roles["MeteorRole2"], _meteorRoleGo[1].GameObject);
                ap.Assign(Signs.Roles["NonMeteor1"], _nonMeteorGo[0].GameObject);
                ap.Assign(Signs.Roles["NonMeteor2"], _nonMeteorGo[1].GameObject);
                ap.Assign(Signs.Roles["NonMeteor3"], _nonMeteorGo[2].GameObject);
                ap.Assign(Signs.Roles["NonMeteor4"], _nonMeteorGo[3].GameObject);
                _fired = true;
                _state.ExecuteAutomarkers(ap, Timing);
            }

        }

        #endregion

        #region ChainLightningAm

        public class ChainLightningAm : Automarker
        {

            [AttributeOrderNumber(1000)]
            public AutomarkerSigns Signs { get; set; }

            [DebugOption]
            [AttributeOrderNumber(2500)]
            public AutomarkerTiming Timing { get; set; }

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public System.Action Test { get; set; }

            private List<uint> _lightnings = new List<uint>();
            private bool _fired = false;

            public ChainLightningAm(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Signs.SetRole("Lightning1", AutomarkerSigns.SignEnum.Ignore1, false);
                Signs.SetRole("Lightning2", AutomarkerSigns.SignEnum.Ignore2, false);
                Test = new System.Action(() => Signs.TestFunctionality(state, null, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _fired = false;
                _lightnings.Clear();
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
                        Log(State.LogLevelEnum.Debug, null, "Clearing automarkers", statusId);
                        Reset();
                        _state.ClearAutoMarkers();
                    }
                    return;
                }
                _lightnings.Add(actorId);
                if (_lightnings.Count < 2)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Ready for automarkers");
                Party pty = _state.GetPartyMembers();
                List<Party.PartyMember> _lightningsGo = new List<Party.PartyMember>(
                    from ix in pty.Members join jx in _lightnings on ix.ObjectId equals jx select ix
                );
                AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                ap.Assign(Signs.Roles["Lightning1"], _lightningsGo[0].GameObject);
                ap.Assign(Signs.Roles["Lightning2"], _lightningsGo[1].GameObject);
                _fired = true;
                _state.ExecuteAutomarkers(ap, Timing);
            }

        }

        #endregion

        #region DothAM

        public class DothAM : Automarker
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

            private List<uint> _dooms = new List<uint>();
            private bool _fired = false;

            public DothAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Signs.SetRole("Doom1", AutomarkerSigns.SignEnum.Ignore1, false);
                Signs.SetRole("Doom2", AutomarkerSigns.SignEnum.Bind1, false);
                Signs.SetRole("Doom3", AutomarkerSigns.SignEnum.Bind2, false);
                Signs.SetRole("Doom4", AutomarkerSigns.SignEnum.Ignore2, false);
                Signs.SetRole("NonDoom1", AutomarkerSigns.SignEnum.Attack1, false);
                Signs.SetRole("NonDoom2", AutomarkerSigns.SignEnum.Attack2, false);
                Signs.SetRole("NonDoom3", AutomarkerSigns.SignEnum.Attack3, false);
                Signs.SetRole("NonDoom4", AutomarkerSigns.SignEnum.Attack4, false);
                Prio = new AutomarkerPrio();
                Prio.Priority = AutomarkerPrio.PrioTypeEnum.CongaX;
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                Test = new System.Action(() => Signs.TestFunctionality(state, Prio, Timing, SelfMarkOnly, AsSoftmarker));
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _dooms.Clear();
                _fired = false;
            }

            internal void FeedStatus(uint actorId, uint statusId, bool gained)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered status {0} {1} for {2:X}", statusId, gained, actorId);
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
                if (statusId == StatusDoom)
                {
                    _dooms.Add(actorId);
                }
                if (_dooms.Count < 4)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Ready for automarkers");
                Party pty = _state.GetPartyMembers();
                List<Party.PartyMember> _doomsGo = new List<Party.PartyMember>(
                    from ix in pty.Members join jx in _dooms on ix.ObjectId equals jx select ix
                );
                List<Party.PartyMember> _nonDoomsGo = new List<Party.PartyMember>(
                    from ix in pty.Members where _doomsGo.Contains(ix) == false select ix
                );
                Prio.SortByPriority(_doomsGo);
                Prio.SortByPriority(_nonDoomsGo);                
                AutomarkerPayload ap = new AutomarkerPayload(_state, SelfMarkOnly, AsSoftmarker);
                ap.Assign(Signs.Roles["Doom1"], _doomsGo[0].GameObject);
                ap.Assign(Signs.Roles["Doom2"], _doomsGo[1].GameObject);
                ap.Assign(Signs.Roles["Doom3"], _doomsGo[2].GameObject);
                ap.Assign(Signs.Roles["Doom4"], _doomsGo[3].GameObject);
                ap.Assign(Signs.Roles["NonDoom1"], _nonDoomsGo[0].GameObject);
                ap.Assign(Signs.Roles["NonDoom2"], _nonDoomsGo[1].GameObject);
                ap.Assign(Signs.Roles["NonDoom3"], _nonDoomsGo[2].GameObject);
                ap.Assign(Signs.Roles["NonDoom4"], _nonDoomsGo[3].GameObject);
                _state.ExecuteAutomarkers(ap, Timing);
                _fired = true;
            }

        }

        #endregion

        #region WrothAM

        public class WrothAM : Automarker
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

            private List<uint> _spreads = new List<uint>();
            private List<uint> _stacks = new List<uint>();
            private bool _fired = false;

            public WrothAM(State state) : base(state)
            {
                Enabled = false;
                Signs = new AutomarkerSigns();
                Prio = new AutomarkerPrio();
                Prio.Priority = AutomarkerPrio.PrioTypeEnum.Role;
                Prio._prioByRole.Clear();
                Prio._prioByRole.Add(AutomarkerPrio.PrioRoleEnum.Tank);
                Prio._prioByRole.Add(AutomarkerPrio.PrioRoleEnum.Healer);
                Prio._prioByRole.Add(AutomarkerPrio.PrioRoleEnum.Ranged);
                Prio._prioByRole.Add(AutomarkerPrio.PrioRoleEnum.Caster);
                Prio._prioByRole.Add(AutomarkerPrio.PrioRoleEnum.Melee);
                Timing = new AutomarkerTiming() { TimingType = AutomarkerTiming.TimingTypeEnum.Inherit, Parent = state.cfg.DefaultAutomarkerTiming };
                SetupPresets();
                Signs.ApplyPreset("LPDU");
                Test = new System.Action(() => Signs.TestFunctionality(state, Prio, Timing, SelfMarkOnly, AsSoftmarker));
            }

            private void SetupPresets()
            {
                AutomarkerSigns.Preset pr;
                pr = new AutomarkerSigns.Preset() { Builtin = true };
                pr.Name = "LPDU";
                pr.Roles["Stack1_1"] = AutomarkerSigns.SignEnum.Bind1;
                pr.Roles["Stack1_2"] = AutomarkerSigns.SignEnum.Bind2;
                pr.Roles["Stack2_1"] = AutomarkerSigns.SignEnum.Ignore1;
                pr.Roles["Stack2_2"] = AutomarkerSigns.SignEnum.Ignore2;
                pr.Roles["Spread1"] = AutomarkerSigns.SignEnum.Attack4;
                pr.Roles["Spread2"] = AutomarkerSigns.SignEnum.Attack3;
                pr.Roles["Spread3"] = AutomarkerSigns.SignEnum.Attack2;
                pr.Roles["Spread4"] = AutomarkerSigns.SignEnum.Attack1;
                Signs.AddPreset(pr);
                pr = new AutomarkerSigns.Preset() { Builtin = true };
                pr.Name = "ElementalDC";
                pr.Roles["Stack1_1"] = AutomarkerSigns.SignEnum.Ignore1;
                pr.Roles["Stack1_2"] = AutomarkerSigns.SignEnum.Ignore2;
                pr.Roles["Stack2_1"] = AutomarkerSigns.SignEnum.Bind1;
                pr.Roles["Stack2_2"] = AutomarkerSigns.SignEnum.Bind2;
                pr.Roles["Spread1"] = AutomarkerSigns.SignEnum.Attack4;
                pr.Roles["Spread2"] = AutomarkerSigns.SignEnum.Attack3;
                pr.Roles["Spread3"] = AutomarkerSigns.SignEnum.Attack2;
                pr.Roles["Spread4"] = AutomarkerSigns.SignEnum.Attack1;
                Signs.AddPreset(pr);
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _spreads.Clear();
                _stacks.Clear();
                _fired = false;
            }

            internal void FeedStatus(uint actorId, uint statusId, bool gained)
            {
                if (Active == false)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Registered status {0} {1} for {2:X}", statusId, gained, actorId);
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
                if (statusId == StatusEntangledFlames)
                {
                    _stacks.Add(actorId);
                }
                if (statusId == StatusSpreadingFlames)
                { 
                    _spreads.Add(actorId);
                }
                if (_stacks.Count < 2 || _spreads.Count < 4)
                {
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Ready for automarkers");
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

        public UltDragonsongReprise(State st) : base(st)
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
            _state.OnStatusChange += OnStatusChange;
        }

        private void OnStatusChange(uint src, uint dest, uint statusId, bool gained, float duration, int stacks)
        {
            if (statusId == StatusPrey)
            {
                _meteorAm.FeedStatus(dest, statusId, gained);
            }
            if (statusId == StatusDoom)
            {
                _dothAm.FeedStatus(dest, statusId, gained);
            }
            if (statusId == StatusEntangledFlames || statusId == StatusSpreadingFlames)
            {
                _wrothAm.FeedStatus(dest, statusId, gained);
            }
            if (statusId == StatusThunderstruck)
            {
                _chainLightningAm.FeedStatus(dest, statusId, gained);
            }
        }

        private void UnsubscribeFromEvents()
        {
            _state.OnStatusChange -= OnStatusChange;
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
            bool newZoneOk = (newZone == 968);
            if (newZoneOk == true && ZoneOk == false)
            {
                Log(State.LogLevelEnum.Info, null, "Content available");
                _meteorAm = (MeteorAM)Items["MeteorAM"];                
                _chainLightningAm = (ChainLightningAm)Items["ChainLightningAm"];
                _dothAm = (DothAM)Items["DothAM"];
                _wrothAm = (WrothAM)Items["WrothAM"];
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
