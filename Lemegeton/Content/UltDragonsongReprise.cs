using System;
using Lemegeton.Core;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using ImGuiNET;
using System.Collections;
using static Lemegeton.Content.UltDragonsongReprise;
using System.Numerics;
using static Lemegeton.Content.Overlays.DotTracker;
using Dalamud.Interface.Internal;
using GameObjectPtr = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using Dalamud.Interface.Utility;
using System.Data.SqlTypes;
using static Lemegeton.Core.State;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;

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

        private const int NameNidhogg = 3458;
        private const int NameHraesvelgr = 4954;

        private bool ZoneOk = false;
        private bool _subbed = false;

        private MeteorAM _meteorAm;
        private ChainLightningAm _chainLightningAm;
        private DothAM _dothAm;
        private WrothAM _wrothAm;
        private DoubleDragons _doubleDragons;

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

            private List<ulong> _meteors = new List<ulong>();
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

        #region DoubleDragons

        public class DoubleDragons : Core.ContentItem
        {

            public ulong _idHraesvelgr = 0;
            public ulong _idNidhogg = 0;

            public override FeaturesEnum Features => FeaturesEnum.Drawing;

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public System.Action Test { get; set; }

            public DoubleDragons(State state) : base(state)
            {
                Enabled = false;
                Test = new System.Action(() => TestFunctionality());
            }

            public void TestFunctionality()
            {
                if (_idHraesvelgr > 0 || _idNidhogg > 0)
                {
                    _idHraesvelgr = 0;
                    _idNidhogg = 0;
                    return;
                }
                _state.InvokeZoneChange(968);
                IGameObject me = _state.cs.LocalPlayer as IGameObject;
                _idNidhogg = me.GameObjectId;
                _idHraesvelgr = me.GameObjectId;
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
                    _idNidhogg = go.GameObjectId;
                    Log(State.LogLevelEnum.Debug, null, "Testing from {0} to {1}", me, go);
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Testing on self");
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _idHraesvelgr = 0;
                _idNidhogg = 0;
            }

            protected override bool ExecutionImplementation()
            {
                ImDrawListPtr draw;
                if (_idHraesvelgr == 0 || _idNidhogg == 0)
                {
                    return false;
                }
                if (_state.StartDrawing(out draw) == false)
                {
                    return false;
                }
                IGameObject goH = _state.GetActorById(_idHraesvelgr);
                if (goH == null)
                {
                    return false;
                }
                IGameObject goN = _state.GetActorById(_idNidhogg);
                if (goN == null)
                {
                    return false;
                }
                ICharacter chH = (ICharacter)goH;
                ICharacter chN = (ICharacter)goN;
                ISharedImmediateTexture bws, tws;
                float x = 200.0f;
                float y = 300.0f;
                float w = 250.0f;
                float hhp = (float)chH.CurrentHp / (float)chH.MaxHp * 100.0f;
                float nhp = (float)chN.CurrentHp / (float)chN.MaxHp * 100.0f;
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
                tws = _state.plug._ui.GetMiscIcon(UserInterface.MiscIconEnum.DarkDragon);
                IDalamudTextureWrap tw = tws.GetWrapOrEmpty();
                bws = _state.plug._ui.GetMiscIcon(UserInterface.MiscIconEnum.LightCircle);
                IDalamudTextureWrap bw = tws.GetWrapOrEmpty();
                draw.AddImage(
                    bw.ImGuiHandle,
                    new Vector2(x, y),
                    new Vector2(x + tw.Width, y + tw.Height),
                    new Vector2(1.0f, 0.0f),
                    new Vector2(0.0f, 1.0f),
                    ImGui.GetColorU32(hcol)
                );
                draw.AddImage(
                    bw.ImGuiHandle,
                    new Vector2(x + w - tw.Width, y),
                    new Vector2(x + w, y + tw.Height),
                    new Vector2(1.0f, 0.0f),
                    new Vector2(0.0f, 1.0f),
                    ImGui.GetColorU32(ncol)
                );
                draw.AddImage(
                    tw.ImGuiHandle,
                    new Vector2(x, y),
                    new Vector2(x + tw.Width, y + tw.Height),
                    new Vector2(1.0f, 0.0f),
                    new Vector2(0.0f, 1.0f)
                );
                tws = _state.plug._ui.GetMiscIcon(UserInterface.MiscIconEnum.LightDragon);
                tw = tws.GetWrapOrEmpty();
                draw.AddImage(
                    tw.ImGuiHandle,
                    new Vector2(x + w - tw.Width, y),
                    new Vector2(x + w, y + tw.Height),
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
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(x + (tw.Width / 2.0f) - (sz.X / 2.0f) + 1.0f, y + tw.Height + 1.0f), ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 1.0f)), temp);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(x + (tw.Width / 2.0f) - (sz.X / 2.0f), y + tw.Height), ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)), temp);
                temp = String.Format("{0:0.0} %", nhp);
                sz = ImGui.CalcTextSize(temp);
                scale = textSize / sz.Y;
                sz = new Vector2(sz.X * scale, sz.Y * scale);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(x + w - (tw.Width / 2.0f) - (sz.X / 2.0f) + 1.0f, y + tw.Height + 1.0f), ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 1.0f)), temp);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(x + w - (tw.Width / 2.0f) - (sz.X / 2.0f), y + tw.Height), ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)), temp);
                temp = chH.Name.ToString();
                sz = ImGui.CalcTextSize(temp);
                scale = textSize / sz.Y;
                sz = new Vector2(sz.X * scale, sz.Y * scale);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(x + (tw.Width / 2.0f) - (sz.X / 2.0f) + 1.0f, y - sz.Y + 1.0f), ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 1.0f)), temp);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(x + (tw.Width / 2.0f) - (sz.X / 2.0f), y - sz.Y), ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)), temp);
                temp = chN.Name.ToString();
                sz = ImGui.CalcTextSize(temp);
                scale = textSize / sz.Y;
                sz = new Vector2(sz.X * scale, sz.Y * scale);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(x + w - (tw.Width / 2.0f) - (sz.X / 2.0f) + 1.0f, y - sz.Y + 1.0f), ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 1.0f)), temp);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(x + w - (tw.Width / 2.0f) - (sz.X / 2.0f), y - sz.Y), ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)), temp);
                temp = String.Format("{0:0.0} %", dhp);
                textSize = 40.0f;
                sz = ImGui.CalcTextSize(temp);
                scale = textSize / sz.Y;
                sz = new Vector2(sz.X * scale, sz.Y * scale);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(x + (w / 2.0f) - (sz.X / 2.0f) + 1.0f, y + (tw.Height / 2.0f) - (sz.Y / 2.0f) + 1.0f), ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 1.0f)), temp);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(x + (w / 2.0f) - (sz.X / 2.0f), y + (tw.Height / 2.0f) - (sz.Y / 2.0f)), ImGui.GetColorU32(wcol), temp);
                return true;
            }

        }

        #endregion

        public UltDragonsongReprise(State st) : base(st)
        {
            st.OnZoneChange += OnZoneChange;
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
                _state.OnStatusChange += OnStatusChange;
                _state.OnCombatantAdded += OnCombatantAdded;
                _state.OnCombatantRemoved += OnCombatantRemoved;
                _state.OnCombatChange += OnCombatChange;
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
                _state.OnStatusChange -= OnStatusChange;
                _state.OnCombatantAdded -= OnCombatantAdded;
                _state.OnCombatantRemoved -= OnCombatantRemoved;
                _state.OnCombatChange -= OnCombatChange;
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

        private void OnCombatantRemoved(ulong actorId, nint addr)
        {
            if (actorId == _doubleDragons._idNidhogg && actorId > 0)
            {
                Log(State.LogLevelEnum.Debug, null, "Nidhogg is gone");
                _doubleDragons._idNidhogg = 0;
            }
            if (actorId == _doubleDragons._idHraesvelgr && actorId > 0)
            {
                Log(State.LogLevelEnum.Debug, null, "Hraesvelgr is gone");
                _doubleDragons._idHraesvelgr = 0;
            }
        }

        private void OnCombatantAdded(Dalamud.Game.ClientState.Objects.Types.IGameObject go)
        {
            if (go is ICharacter)
            {                
                ICharacter ch = go as ICharacter;
                if (ch.MaxHp != 4535368)
                {
                    return;
                }
                if (ch.NameId == NameNidhogg)
                {
                    Log(State.LogLevelEnum.Debug, null, "Nidhogg found as {0:X8}", go.GameObjectId);
                    _doubleDragons._idNidhogg = go.GameObjectId;
                }
                else if (ch.NameId == NameHraesvelgr)
                {
                    Log(State.LogLevelEnum.Debug, null, "Hraesvelgr found as {0:X8}", go.GameObjectId);
                    _doubleDragons._idHraesvelgr = go.GameObjectId;
                }
            }
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

        private void OnCombatChange(bool inCombat)
        {
            Reset();
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
                _doubleDragons = (DoubleDragons)Items["DoubleDragons"];
                SubscribeToEvents();
                LogItems();
            }
            else if (newZoneOk == false && ZoneOk == true)
            {
                Log(State.LogLevelEnum.Info, null, "Content unavailable");
                UnsubscribeFromEvents();
            }
            ZoneOk = newZoneOk;
        }

    }

}
