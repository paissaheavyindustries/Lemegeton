using Dalamud.Game.Gui;
using Dalamud.Game.Network;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Common.Lua;
using Lemegeton.PacketHeaders;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Formats.Asn1;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using static FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentMJIFarmManagement;
using static Lemegeton.Content.UltOmegaProtocol;
using static Lemegeton.Core.State;
using static Lumina.Data.Parsing.Uld.UldRoot;

namespace Lemegeton.Core
{

    internal class NetworkDecoder
    {

        internal class StatusTracker
        {

            public delegate void StatusGainDelegate(uint srcActorId, uint actorId, uint statusId, float duration, int stacks);
            public delegate void StatusLoseDelegate(uint actorId, uint statusId);

            public event StatusGainDelegate OnStatusGain;
            public event StatusLoseDelegate OnStatusLose;

            internal struct Entry
            {

                public uint srcActorId;
                public uint actorId;
                public uint statusId;
                public float duration;
                public int stacks;
                public ulong runNumber;

            }

            public Dictionary<uint, Dictionary<uint, Entry>> entries = new Dictionary<uint, Dictionary<uint, Entry>>();
            public ulong runNumber = 1;

            private void ApplyStatus(Entry e)
            {
                lock (this)
                {
                    Dictionary<uint, Entry> actor;
                    Entry exentry;
                    e.runNumber = runNumber;
                    if (entries.TryGetValue(e.actorId, out actor) == false)
                    {
                        actor = new Dictionary<uint, Entry>();
                        entries[e.actorId] = actor;
                    }
                    if (actor.TryGetValue(e.statusId, out exentry) == false)
                    {
                        actor[e.statusId] = e;
                        OnStatusGain?.Invoke(e.srcActorId, e.actorId, e.statusId, e.duration, e.stacks);
                    }
                    else
                    {
                        if (e.stacks != exentry.stacks || e.duration > exentry.duration)
                        {
                            actor[e.statusId] = e;
                            OnStatusGain?.Invoke(e.srcActorId, e.actorId, e.statusId, e.duration, e.stacks);
                        }
                        else
                        {
                            actor[e.statusId] = e;
                        }
                    }
                }
            }

            public void ApplyStatus(IEnumerable<Entry> newEntries)
            {
                lock (this)
                {
                    foreach (Entry e in newEntries)
                    {
                        ApplyStatus(e);
                    }
                }
            }

            public void ReplaceStatusForActor(uint actorId, IEnumerable<Entry> newEntries)
            {
                lock (this)
                {
                    Dictionary<uint, Entry> actor;
                    if (newEntries == null)
                    {
                        if (entries.TryGetValue(actorId, out actor) == true)
                        {
                            foreach (KeyValuePair<uint, Entry> kp in actor)
                            {
                                OnStatusLose?.Invoke(actorId, kp.Value.statusId);
                            }
                            entries.Remove(actorId);
                        }
                    }
                    else
                    {
                        runNumber++;
                        ApplyStatus(newEntries);
                        if (entries.TryGetValue(actorId, out actor) == true)
                        {
                            List<Entry> toRem = (from ix in actor.Values where ix.runNumber != runNumber select ix).ToList();
                            foreach (Entry e in toRem)
                            {
                                actor.Remove(e.statusId);
                                OnStatusLose?.Invoke(actorId, e.statusId);
                            }
                        }
                    }
                }
            }

        }

        internal class OpcodeList 
        {

            internal ushort StatusEffectList = 0;
            internal ushort StatusEffectList2 = 0;
            internal ushort StatusEffectList3 = 0;
            internal ushort BossStatusEffectList = 0;
            internal ushort Ability1 = 0;
            internal ushort Ability8 = 0;
            internal ushort Ability16 = 0;
            internal ushort Ability24 = 0;
            internal ushort Ability32 = 0;
            internal ushort ActorCast = 0;
            internal ushort EffectResult = 0;
            internal ushort ActorControl = 0;
            internal ushort ActorControlSelf = 0;
            internal ushort ActorControlTarget = 0;
            internal ushort MapEffect = 0;
            internal ushort EventPlay = 0;
            internal ushort EventPlay64 = 0;

        }

        internal enum DirectorTypeEnum : uint
        {
            Init = 0x40000001,
            FadeOut = 0x40000005,
            BarrierDown = 0x40000006,
        }

        internal OpcodeList Opcodes;
        internal State _st;
        internal StatusTracker _tracker;

        private List<string> _opcodeRegions = null;
        private Blueprint.Region _nextOpcodeRegion = null;
        internal Blueprint.Region _currentOpcodeRegion = null;
        private Blueprint _blueprint = null;

        public NetworkDecoder(State st)
        {
            _st = st;
            _tracker = new StatusTracker();
            _tracker.OnStatusGain += _tracker_OnStatusGain;
            _tracker.OnStatusLose += _tracker_OnStatusLose;
        }

        internal string GetOpcodeVersion()
        {
            if (_currentOpcodeRegion == null)
            {
                return null;
            }
            return _currentOpcodeRegion.Version;
        }

        internal List<Blueprint.Region.Warning> GetOpcodeWarnings()
        {
            if (_currentOpcodeRegion == null)
            {
                return null;
            }
            return _currentOpcodeRegion.Warnings;
        }

        internal ushort GetOpcodeForRegion(Blueprint.Region region, string id)
        {
            if (region.OpcodeLookup.TryGetValue(id, out Blueprint.Region.Opcode val) == true)
            {
                return val.Id;
            }
            return 0;
        }

        internal void SetOpcodes(Blueprint.Region region)
        {
            Opcodes = new OpcodeList();
            Opcodes.StatusEffectList = GetOpcodeForRegion(region, "StatusEffectList");
            Opcodes.StatusEffectList2 = GetOpcodeForRegion(region, "StatusEffectList2");
            Opcodes.StatusEffectList3 = GetOpcodeForRegion(region, "StatusEffectList3");
            Opcodes.BossStatusEffectList = GetOpcodeForRegion(region, "BossStatusEffectList");
            Opcodes.Ability1 = GetOpcodeForRegion(region, "Ability1");
            Opcodes.Ability8 = GetOpcodeForRegion(region, "Ability8");
            Opcodes.Ability16 = GetOpcodeForRegion(region, "Ability16");
            Opcodes.Ability24 = GetOpcodeForRegion(region, "Ability24");
            Opcodes.Ability32 = GetOpcodeForRegion(region, "Ability32");
            Opcodes.ActorCast = GetOpcodeForRegion(region, "ActorCast");
            Opcodes.EffectResult = GetOpcodeForRegion(region, "EffectResult");
            Opcodes.MapEffect = GetOpcodeForRegion(region, "MapEffect");
            Opcodes.EventPlay = GetOpcodeForRegion(region, "EventPlay");
            Opcodes.EventPlay64 = GetOpcodeForRegion(region, "EventPlay64");
            Opcodes.ActorControl = GetOpcodeForRegion(region, "ActorControl");
            Opcodes.ActorControlSelf = GetOpcodeForRegion(region, "ActorControlSelf");
            Opcodes.ActorControlTarget = GetOpcodeForRegion(region, "ActorControlTarget");
            _st.Log(State.LogLevelEnum.Debug, null, "Opcodes set to: {0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15} {16} DebugFlags: {17} DebugInstances: {18}",
                Opcodes.StatusEffectList, Opcodes.StatusEffectList2, Opcodes.StatusEffectList3, Opcodes.BossStatusEffectList,
                Opcodes.Ability1, Opcodes.Ability8, Opcodes.Ability16, Opcodes.Ability24, Opcodes.Ability32,
                Opcodes.ActorCast, Opcodes.EffectResult, Opcodes.MapEffect, Opcodes.EventPlay, Opcodes.EventPlay64,
                Opcodes.ActorControl, Opcodes.ActorControlSelf, Opcodes.ActorControlTarget,
                region.DebugFlags, region.DebugInstanceData
            );
        }

        internal static void SetOpcode(IDictionary<string, ushort> src, string name, out ushort opcode)
        {
            ushort temp;
            if (src.TryGetValue(name, out temp) == true)
            {
                opcode = temp;
            }
            else
            {
                opcode = 0;
            }
        }

        internal void DecodeActorControl(ActorControlCategory category, uint sourceActorId, uint targetActorId, uint param1, uint param2, uint param3, uint param4)
        {
            switch (category)
            {
                case ActorControlCategory.Headmarker:
                    // param1 = headmarker id
                    _st.InvokeHeadmarker(targetActorId, param1);
                    break;
                case ActorControlCategory.Tether:
                    // param2 = tether type
                    // param3 = tether buddy id
                    _st.InvokeTether(targetActorId, param3, param2);
                    break;
                case ActorControlCategory.Director:
                    if (param2 == (uint)DirectorTypeEnum.FadeOut)
                    {
                        _st._runInstance++;
                        if (_st.cfg.RemoveMarkersAfterWipe == true)
                        {
                            _st._suppressCombatEndMarkRemoval = true;
                        }
                    }
                    if (param2 == (uint)DirectorTypeEnum.BarrierDown)
                    {
                        if (_st.cfg.RemoveMarkersAfterWipe == true)
                        {
                            _st.Log(State.LogLevelEnum.Debug, null, "Wiped, removing markers");
                            _st.ClearAutoMarkers();
                        }
                        _st.AutoselectTimeline(_st.cs.TerritoryType);
                    }
                    if (param2 == (uint)DirectorTypeEnum.Init)
                    {
                        _st.AutoselectTimeline(_st.cs.TerritoryType);
                    }
                    _st.InvokeDirectorUpdate(param1, param2, param3, param4);
                    break;
                case ActorControlCategory.Sign:
                    AutomarkerSigns.SignEnum sign = AutomarkerSigns.SignEnum.None;
                    switch (param1)
                    {
                        case 0: sign = AutomarkerSigns.SignEnum.Attack1; break;
                        case 1: sign = AutomarkerSigns.SignEnum.Attack2; break;
                        case 2: sign = AutomarkerSigns.SignEnum.Attack3; break;
                        case 3: sign = AutomarkerSigns.SignEnum.Attack4; break;
                        case 4: sign = AutomarkerSigns.SignEnum.Attack5; break;
                        case 5: sign = AutomarkerSigns.SignEnum.Bind1; break;
                        case 6: sign = AutomarkerSigns.SignEnum.Bind2; break;
                        case 7: sign = AutomarkerSigns.SignEnum.Bind3; break;
                        case 8: sign = AutomarkerSigns.SignEnum.Ignore1; break;
                        case 9: sign = AutomarkerSigns.SignEnum.Ignore2; break;
                        case 10: sign = AutomarkerSigns.SignEnum.Square; break;
                        case 11: sign = AutomarkerSigns.SignEnum.Circle; break;
                        case 12: sign = AutomarkerSigns.SignEnum.Plus; break;
                        case 13: sign = AutomarkerSigns.SignEnum.Triangle; break;
                        case 14: sign = AutomarkerSigns.SignEnum.Attack6; break;
                        case 15: sign = AutomarkerSigns.SignEnum.Attack7; break;
                        case 16: sign = AutomarkerSigns.SignEnum.Attack8; break;
                        default:
                            _st.Log(State.LogLevelEnum.Debug, null, "Unknown Sign actor control from {0:X} to {1:X}, p1: {2}, p2: {3}, p3: {4}, p4: {5}", category, sourceActorId, targetActorId, param1, param2, param3, param4);
                            break;
                    }
                    _st.AddMarkerHistory(
                        _st.GetActorById(param2),
                        (targetActorId != 0xE0000000) ? _st.GetActorById(targetActorId) : null,
                        false,
                        sign
                    );
                    break;
                default:
                    _st.InvokeActorControl((ushort)category, sourceActorId, targetActorId, param1, param2, param3, param4);
                    break;
            }
        }

        internal unsafe void HandleStatusEffectList(StatusEffectListEntry* ae1, StatusEffectListEntry* ae2, uint targetActorId)
        {            
            List<StatusTracker.Entry> entries = new List<StatusTracker.Entry>();
            List<string> temp = new List<string>();
            ushort seed = ae1[29].statusId;
            temp.Add(string.Format("[{0}/{1:X}]", seed, targetActorId));
            for (int i = 0; i < 30; i++)
            {
                temp.Add(string.Format("{0}={1}", i, ae1[i].statusId - seed));
                if (ae1[i].statusId - seed > 0)
                {
                    entries.Add(
                        new StatusTracker.Entry() { srcActorId = ae1[i].srcActorId, actorId = targetActorId, statusId = (uint)ae1[i].statusId - seed, duration = Math.Abs(ae1[i].duration), stacks = ae1[i].stacks }
                    );
                }
            }
            if (ae2 != null)
            {
                for (int i = 0; i < 30; i++)
                {
                    temp.Add(string.Format("{0}={1}", i, ae2[i].statusId - seed));
                    if (ae2[i].statusId - seed > 0)
                    {
                        entries.Add(
                            new StatusTracker.Entry() { srcActorId = ae2[i].srcActorId, actorId = targetActorId, statusId = (uint)ae2[i].statusId - seed, duration = Math.Abs(ae2[i].duration), stacks = ae2[i].stacks }
                        );
                    }
                }
            }
            if ((_currentOpcodeRegion?.DebugFlags & Blueprint.DebugFlags.StatusEffects) != 0 && _currentOpcodeRegion.DebugInstances.Contains(_st._territoryCurrent))
            {
                _st.Log(State.LogLevelEnum.Debug, null, "SEL: {0}", string.Join(", ", temp));
            }
            _tracker.ReplaceStatusForActor(targetActorId, entries.Count > 0 ? entries : null);
        }

        internal unsafe void Decode(nint dataPtr, ushort opCode, uint sourceActorId, uint targetActorId)
        {
            if ((_currentOpcodeRegion?.DebugFlags & Blueprint.DebugFlags.Opcodes) != 0 && _currentOpcodeRegion.DebugInstances.Contains(_st._territoryCurrent))
            {
                _st.Log(State.LogLevelEnum.Debug, null, "Opcode: {0} / {1:X} (source: {2:X8}, target: {3:X8})", opCode, opCode, sourceActorId, targetActorId);
            }
            if (opCode == Opcodes.ActorCast)
            {                
                ActorCast ac = Marshal.PtrToStructure<ActorCast>(dataPtr);
                _st.InvokeCastBegin(targetActorId, ac.targetId, ac.actionId, ac.castTime, ac.rotation);
            }
            else if (opCode == Opcodes.ActorControl)
            {
                ActorControl ac = Marshal.PtrToStructure<ActorControl>(dataPtr);
                DecodeActorControl(ac.category, sourceActorId, targetActorId, ac.param1, ac.param2, ac.param3, ac.param4);
            }
            else if (opCode == Opcodes.ActorControlSelf)
            {
                ActorControlSelf ac = Marshal.PtrToStructure<ActorControlSelf>(dataPtr);
                DecodeActorControl(ac.category, sourceActorId, targetActorId, ac.param1, ac.param2, ac.param3, ac.param4);
            }
            else if (opCode == Opcodes.ActorControlTarget)
            {
                ActorControlTarget ac = Marshal.PtrToStructure<ActorControlTarget>(dataPtr);
                DecodeActorControl(ac.category, sourceActorId, ac.targetId, ac.param1, ac.param2, ac.param3, ac.param4);
            }
            else if (opCode == Opcodes.Ability1)
            {
                Ability1 ac = Marshal.PtrToStructure<Ability1>(dataPtr);
                _st.InvokeAction(targetActorId, (uint)ac.targetId[0], ac.actionId);
            }
            else if (opCode == Opcodes.Ability8)
            {
                Ability8 ac = Marshal.PtrToStructure<Ability8>(dataPtr);
                for (int i = 0; i < 8; i++)
                {
                    _st.InvokeAction(targetActorId, (uint)ac.targetId[i], ac.actionId);
                }
            }
            else if (opCode == Opcodes.Ability16)
            {
                Ability16 ac = Marshal.PtrToStructure<Ability16>(dataPtr);
                for (int i = 0; i < 16; i++)
                {
                    _st.InvokeAction(targetActorId, (uint)ac.targetId[i], ac.actionId);
                }
            }
            else if (opCode == Opcodes.Ability24)
            {
                Ability24 ac = Marshal.PtrToStructure<Ability24>(dataPtr);
                for (int i = 0; i < 24; i++)
                {
                    _st.InvokeAction(targetActorId, (uint)ac.targetId[i], ac.actionId);
                }
            }
            else if (opCode == Opcodes.Ability32)
            {
                Ability32 ac = Marshal.PtrToStructure<Ability32>(dataPtr);
                for (int i = 0; i < 32; i++)
                {
                    _st.InvokeAction(targetActorId, (uint)ac.targetId[i], ac.actionId);
                }
            }
            else if (opCode == Opcodes.EffectResult)
            {
                EffectResult ac = Marshal.PtrToStructure<EffectResult>(dataPtr);
                List<string> temp = new List<string>();
                EffectResultEntry* ae = (EffectResultEntry * )(dataPtr + 28);
                temp.Add(string.Format("[{0:X}]", targetActorId));
                List<StatusTracker.Entry> entries = new List<StatusTracker.Entry>();
                for (int i = 0; i < ac.entryCount; i++)
                {
                    if (ae[i].statusId > 0)
                    {
                        temp.Add(string.Format("{0}={1}", i, ae[i].statusId));
                        entries.Add(
                            new StatusTracker.Entry() { srcActorId = ae[i].srcActorId, actorId = targetActorId, statusId = ae[i].statusId, duration = Math.Abs(ae[i].duration), stacks = ae[i].stacks }
                        );
                    }
                }
                if ((_currentOpcodeRegion?.DebugFlags & Blueprint.DebugFlags.StatusEffects) != 0 && _currentOpcodeRegion.DebugInstances.Contains(_st._territoryCurrent))
                {
                    _st.Log(State.LogLevelEnum.Debug, null, "ER: {0}", string.Join(", ", temp));
                }
                if (entries.Count > 0)
                {
                    _tracker.ApplyStatus(entries);
                }
            }
            else if (opCode == Opcodes.StatusEffectList)
            {
                StatusEffectList ac = Marshal.PtrToStructure<StatusEffectList>(dataPtr);
                HandleStatusEffectList((StatusEffectListEntry*)(dataPtr + 20), null, targetActorId);
            }
            else if (opCode == Opcodes.StatusEffectList2)
            {
                StatusEffectList2 ac = Marshal.PtrToStructure<StatusEffectList2>(dataPtr);
                HandleStatusEffectList((StatusEffectListEntry*)(dataPtr + 24), null, targetActorId);
            }
            else if (opCode == Opcodes.StatusEffectList3)
            {
                StatusEffectList3 ac = Marshal.PtrToStructure<StatusEffectList3>(dataPtr);
                HandleStatusEffectList((StatusEffectListEntry*)(dataPtr + 0), null, targetActorId);
            }
            else if (opCode == Opcodes.BossStatusEffectList)
            {
                StatusEffectList ac = Marshal.PtrToStructure<StatusEffectList>(dataPtr);
                HandleStatusEffectList((StatusEffectListEntry*)(dataPtr + 0), (StatusEffectListEntry*)(dataPtr + 380), targetActorId);                
            }
            else if (opCode == Opcodes.MapEffect)
            {
                byte[] bytes = new byte[11];
                Marshal.Copy(dataPtr, bytes, 0, bytes.Length);
                _st.InvokeMapEffect(bytes);
            }
            else if (opCode == Opcodes.EventPlay)
            {
                EventPlay ac = Marshal.PtrToStructure<EventPlay>(dataPtr);
                _st.InvokeEventPlay((uint)ac.actorId, ac.eventId, ac.scene, ac.flags, ac.param1, ac.param2, ac.param3, ac.param4);
            }
            else if (opCode == Opcodes.EventPlay64)
            {
                _st.InvokeEventPlay64();
            }
            else if (_st.cfg.LogUnhandledOpcodes == true)
            {
                _st.Log(State.LogLevelEnum.Debug, null, "Unhandled opcode: {0} (source: {1:X8}, target: {2:X8})", opCode, sourceActorId, targetActorId);
            }
        }

        internal IEnumerable<string> GetOpcodeRegions()
        {
            return _opcodeRegions;
        }

        internal Blueprint GetBlueprintFromURI(string uri)
        {
            try
            {
                XmlDocument doc;
                Uri u = new Uri(uri);
                if (u.IsFile == true)
                {
                    doc = new XmlDocument();
                    doc.Load(uri);
                }
                else
                {
                    using HttpClient http = new HttpClient();
                    using HttpRequestMessage req = new HttpRequestMessage()
                    {
                        Method = HttpMethod.Get,
                        RequestUri = u
                    };
                    using HttpResponseMessage resp = http.Send(req);
                    if (resp.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        _st.Log(State.LogLevelEnum.Error, null, "Couldn't load blueprint from {0}, response code was: {1}", uri, resp.StatusCode);
                        return null;
                    }
                    using StreamReader sr = new StreamReader(resp.Content.ReadAsStream());
                    string data = sr.ReadToEnd();
                    doc = new XmlDocument();
                    doc.LoadXml(data);
                }
                return (Blueprint)_st.plug.DeserializeXml<Blueprint>(doc);
            }
            catch (Exception ex)
            {
                _st.Log(State.LogLevelEnum.Error, ex, "Couldn't load retrieve blueprint from {0}", uri);
            }
            return null;
        }

        internal bool GetOpcodes(bool fallback)
        {
            try
            {
                bool fromBackup = false;
                _st.Log(State.LogLevelEnum.Debug, null, "Loading blueprint from {0}", _st.cfg.OpcodeUrl);
                Blueprint bp = GetBlueprintFromURI(_st.cfg.OpcodeUrl);
                if (bp == null && fallback == true)
                {
                    try
                    {
                        string temp = Path.GetTempPath();
                        string tempfile = Path.Combine(temp, "lemegeton_blueprint.xml");
                        _st.Log(State.LogLevelEnum.Debug, null, "Loading blueprint backup from {0}", tempfile);
                        XmlDocument tempdoc = new XmlDocument();
                        tempdoc.Load(tempfile);
                        bp = (Blueprint)_st.plug.DeserializeXml<Blueprint>(tempdoc);
                        _st.Log(State.LogLevelEnum.Debug, null, "Blueprint backup loaded");
                        fromBackup = true;
                    }
                    catch (Exception ex)
                    {
                        _st.Log(State.LogLevelEnum.Error, ex, "Couldn't load blueprint backup");
                    }
                }
                if (bp != null)
                {
                    bp.BuildLookups();
                    _blueprint = bp;
                    if (fromBackup == false)
                    {
                        try
                        {
                            string temp = Path.GetTempPath();
                            string tempfile = Path.Combine(temp, "lemegeton_blueprint.xml");
                            XmlDocument tempdoc = _st.plug.SerializeXml<Blueprint>(bp);
                            File.WriteAllText(tempfile, tempdoc.OuterXml);
                            _st.Log(State.LogLevelEnum.Debug, null, "Blueprint backup saved to {0}", tempfile);
                        }
                        catch (Exception ex)
                        {
                            _st.Log(State.LogLevelEnum.Error, ex, "Couldn't save blueprint backup");
                        }
                    }
                    _opcodeRegions = new List<string>(from ix in bp.RegionLookup select ix.Key);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _st.Log(State.LogLevelEnum.Error, ex, "Couldn't retrieve blueprint");
            }
            return false;
        }

        internal void SetOpcodeRegion(string name)
        {
            if (_blueprint.RegionLookup.ContainsKey(name) == true)
            {
                Blueprint.Region r = _blueprint.RegionLookup[name];
                _st.Log(State.LogLevelEnum.Info, null, "Setting opcode region to {0} ({1})", r.Name, r.Version);
                if (String.Compare(r.Version, _st.GameVersion) != 0)
                {
                    _st.Log(State.LogLevelEnum.Warning, null, "Opcode version {0} and game version {1} differ, things may be broken", r.Version, _st.GameVersion);
                }
                _nextOpcodeRegion = r;
                return;
            }
            string defName = "EN/DE/FR/JP";
            if (_blueprint.RegionLookup.ContainsKey(defName) == true)
            {
                Blueprint.Region r = _blueprint.RegionLookup[defName];
                _st.Log(State.LogLevelEnum.Warning, null, "Couldn't set opcode region to {0}, defaulting to {1} ({2})", name, r.Name, r.Version);
                _nextOpcodeRegion = r;
                return;
            }
            Blueprint.Region reg = _blueprint.Regions.First();
            _st.Log(State.LogLevelEnum.Warning, null, "Couldn't set opcode region to {0}, defaulting to first found {1} ({2})", name, reg.Name, reg.Version);
            _nextOpcodeRegion = reg;
        }

        internal void NetworkMessageReceived(nint dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
        {
            if (direction != NetworkMessageDirection.ZoneDown)
            {
                _st.LastNetworkTrafficUp = DateTime.Now;
                return;
            }
            _st.LastNetworkTrafficDown = DateTime.Now;
            if (_nextOpcodeRegion != null)
            {                
                SetOpcodes(_nextOpcodeRegion);
                _currentOpcodeRegion = _nextOpcodeRegion;
                _nextOpcodeRegion = null;
            }
            Decode(dataPtr, opCode, sourceActorId, targetActorId);
        }

        private void _tracker_OnStatusGain(uint srcActorId, uint actorId, uint statusId, float duration, int stacks)
        {            
            _st.InvokeStatusChange(srcActorId, actorId, statusId, true, duration, stacks);
        }

        private void _tracker_OnStatusLose(uint actorId, uint statusId)
        {
            _st.InvokeStatusChange(0, actorId, statusId, false, 0.0f, 0);
        }

    }

}
