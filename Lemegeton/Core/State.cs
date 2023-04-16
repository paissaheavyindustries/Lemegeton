using Dalamud.Logging;
using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Network;
using Dalamud.Interface;
using Dalamud.Plugin;
using ImGuiNET;
using System.Numerics;
using System;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Game.ClientState.Party;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using System.IO;

namespace Lemegeton.Core
{

    public sealed class State
    {

        internal enum LogLevelEnum
        {
            Error,
            Warning,
            Info,
            Debug
        }

        internal DalamudPluginInterface pi { get; init; }
        internal GameNetwork gn { get; init; }
        internal ChatGui cg { get; init; }
        internal CommandManager cm { get; init; }
        internal ObjectTable ot { get; init; }
        internal GameGui gg { get; init; }
        internal ClientState cs { get; init; }
        internal DataManager dm { get; init; }
        internal Condition cd { get; init; }
        internal Framework fw { get; init; }
        internal SigScanner ss { get; init; }
        internal PartyList pl { get; init; }

        internal bool StatusGotOpcodes { get; set; } = false;
        internal bool StatusMarkingFuncAvailable { get; set; } = false;
        internal bool StatusPostCommandAvailable { get; set; } = false;
        internal DateTime LastNetworkTrafficUp { get; set; } = DateTime.MinValue;
        internal DateTime LastNetworkTrafficDown { get; set; } = DateTime.MinValue;
        internal int NumFeaturesAutomarker { get; set; } = 0;
        internal int NumFeaturesDrawing { get; set; } = 0;
        internal int NumFeaturesSound { get; set; } = 0;
        #if !SANS_GOETIA
        internal int NumFeaturesHack { get; set; } = 0;
        #endif
        internal string GameVersion { get; set; } = "(unknown)";

        internal Plugin plug;
        internal Config cfg;
        internal SigLocator _sig;
        internal NetworkDecoder _dec;

        private Dictionary<string, nint> _sigs = new Dictionary<string, nint>();
        private delegate char MarkingFunctionDelegate(nint ctrl, byte markId, uint actorId);
        private MarkingFunctionDelegate _markingFuncPtr = null;
        private delegate void PostCommandDelegate(IntPtr ui, IntPtr cmd, IntPtr unk1, byte unk2);
        private PostCommandDelegate _postCmdFuncptr = null;
        public Dictionary<AutomarkerSigns.SignEnum, uint> SoftMarkers = new Dictionary<AutomarkerSigns.SignEnum, uint>();

        private bool _drawingReady = false;
        private int _drawingStarts = 0;
        private ImDrawListPtr _drawListPtr;
        private bool _listening = false;
        public bool _inCombat = false;
        private ulong _run = 0;

        private Dictionary<nint, ulong> _objectsSeen = new Dictionary<nint, ulong>();
        private Dictionary<nint, uint> _objectsToActors = new Dictionary<nint, uint>();

        internal delegate void ZoneChangeDelegate(ushort newZone);
        internal event ZoneChangeDelegate OnZoneChange;

        internal delegate void CombatChangeDelegate(bool inCombat);
        internal event CombatChangeDelegate OnCombatChange;

        internal delegate void CastBeginDelegate(uint src, uint dest, ushort actionId, float castTime, float rotation);
        internal event CastBeginDelegate OnCastBegin;

        internal delegate void ActionDelegate(uint src, uint dest, ushort actionId);
        internal event ActionDelegate OnAction;

        internal delegate void StatusChangeDelegate(uint src, uint dest, uint statusId, bool gained, float duration, int stacks);
        internal event StatusChangeDelegate OnStatusChange;

        internal delegate void HeadMarkerDelegate(uint dest, uint markerId);
        internal event HeadMarkerDelegate OnHeadMarker;

        internal delegate void TetherDelegate(uint src, uint dest, uint tetherId);
        internal event TetherDelegate OnTether;

        internal delegate void DirectorUpdateDelegate(uint param1, uint param2, uint param3, uint param4);
        internal event DirectorUpdateDelegate OnDirectorUpdate;

        internal delegate void MapEffectDelegate(byte[] data);
        internal event MapEffectDelegate OnMapEffect;

        internal delegate void CombatantAddedDelegate(GameObject go);
        internal event CombatantAddedDelegate OnCombatantAdded;

        internal delegate void CombatantRemovedDelegate(uint actorId, nint addr);
        internal event CombatantRemovedDelegate OnCombatantRemoved;

        internal delegate void EventPlayDelegate(uint actorId, uint eventId, ushort scene, uint flags, uint param1, ushort param2, byte param3, uint param4);
        internal event EventPlayDelegate OnEventPlay;

        internal void InvokeZoneChange(ushort newZone)
        {
            OnZoneChange?.Invoke(newZone);
        }

        internal void InvokeCombatChange(bool inCombat)
        {
            OnCombatChange?.Invoke(inCombat);
        }

        internal void InvokeCastBegin(uint src, uint dest, ushort actionId, float castTime, float rotation)
        {
            OnCastBegin?.Invoke(src, dest, actionId, castTime, rotation);
        }

        internal void InvokeAction(uint src, uint dest, ushort actionId)
        {
            OnAction?.Invoke(src, dest, actionId);
        }

        internal void InvokeMapEffect(byte[] data)
        {
            OnMapEffect?.Invoke(data);
        }

        internal void InvokeHeadmarker(uint dest, uint markerId)
        {
            OnHeadMarker?.Invoke(dest, markerId);
        }

        internal void InvokeTether(uint src, uint dest, uint tetherId)
        {
            OnTether?.Invoke(src, dest, tetherId);
        }

        internal void InvokeDirectorUpdate(uint param1, uint param2, uint param3, uint param4)
        {
            OnDirectorUpdate?.Invoke(param1, param2, param3, param4);
        }

        internal void InvokeStatusChange(uint src, uint dest, uint statusId, bool gained, float duration, int stacks)
        {
            OnStatusChange?.Invoke(src, dest, statusId, gained, duration, stacks);
        }

        internal void InvokeCombatantAdded(GameObject go)
        {
            OnCombatantAdded?.Invoke(go);
        }

        internal void InvokeCombatantRemoved(uint actorId, nint addr)
        {
            OnCombatantRemoved?.Invoke(actorId, addr);
        }

        internal void InvokeEventPlay(uint actorId, uint eventId, ushort scene, uint flags, uint param1, ushort param2, byte param3, uint param4)
        {
            OnEventPlay?.Invoke(actorId, eventId, scene, flags, param1, param2, param3, param4);
        }

        public State()
        {
        }

        public void Initialize()
        {
            GetGameVersion();
            _sig = new SigLocator(this);
            _dec = new NetworkDecoder(this);
            cd.ConditionChange += Cd_ConditionChange;
            cm.AddHandler("/lemmy", new CommandInfo(OnCommand)
            {
                HelpMessage = "Open Lemegeton configuration"
            });
            cs.TerritoryChanged += Cs_TerritoryChanged;
            pi.UiBuilder.OpenConfigUi += UiBuilder_OpenConfigUi;
            Cs_TerritoryChanged(null, cs.TerritoryType);
        }

        private void GetGameVersion()
        {
            FileInfo fi = new FileInfo(Process.GetCurrentProcess().MainModule.FileName);
            DirectoryInfo di = fi.Directory;
            string fullproc = Path.Combine(di.FullName, "ffxivgame.ver");
            if (File.Exists(fullproc) == true)
            {
                GameVersion = File.ReadAllText(fullproc).Trim();
                Log(LogLevelEnum.Debug, null, "Game version is {0}", GameVersion);
            }
            else
            {
                Log(LogLevelEnum.Debug, null, "file {0} doesn't exist", fullproc);
            }
        }

        private void UiBuilder_OpenConfigUi()
        {
            cfg.Opened = true;
        }

        public void Uninitialize()
        {
            if (_listening == true)
            {
                gn.NetworkMessage -= _dec.NetworkMessageReceived;
            }
            cs.TerritoryChanged -= Cs_TerritoryChanged;
            pi.UiBuilder.OpenConfigUi -= UiBuilder_OpenConfigUi;
            cm.RemoveHandler("/lemmy");
            cd.ConditionChange -= Cd_ConditionChange;
        }

        private void Cs_TerritoryChanged(object sender, ushort e)
        {
            InvokeZoneChange(e);
        }

        private void Cd_ConditionChange(ConditionFlag flag, bool value)
        {
            if (flag == ConditionFlag.InCombat)
            {
                _inCombat = value;
                if (value == false && cfg.RemoveMarkersAfterCombatEnd == true)
                {
                    Log(LogLevelEnum.Debug, null, "Combat ended, removing markers");
                    AutomarkerPayload ap = new AutomarkerPayload() { Clear = true };
                    ExecuteAutomarkers(ap, cfg.DefaultAutomarkerTiming);
                }
                InvokeCombatChange(value);
            }
        }

        private void OnCommand(string command, string args)
        {
            cfg.Opened = true;
        }

        internal bool PrepareInternals(bool fallback)
        {
            if (_dec.GetOpcodes(fallback) == true)
            {
                StatusGotOpcodes = true;
                _dec.SetOpcodeRegion(cfg.OpcodeRegion);
                _listening = true;
                gn.NetworkMessage += _dec.NetworkMessageReceived;
                GetSignatures();
                return true;
            }            
            return false;
        }

        internal bool StartDrawing(out ImDrawListPtr drawList)
        {
            if (cfg.QuickToggleOverlays == false)
            {
                return false;
            }
            StackTrace stackTrace = new StackTrace();
            MethodBase methodBase = stackTrace.GetFrame(1).GetMethod();           

            if (_drawingReady == true)
            {
                _drawingStarts++;
                drawList = _drawListPtr;
                return true;
            }
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
            ImGuiHelpers.ForceNextWindowMainViewport();
            ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(0, 0));
            ImGui.Begin("LemegetonCanvas",
                ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground);
            ImGui.SetWindowSize(ImGui.GetIO().DisplaySize);
            _drawListPtr = ImGui.GetWindowDrawList();
            drawList = _drawListPtr;
            _drawingReady = true;
            _drawingStarts++;
            return true;
        }

        internal void EndDrawing()
        {
            if (_drawingReady == true)
            {
                ImGui.End();
                ImGui.PopStyleVar();
                _drawingReady = false;
                _drawingStarts = 0;
            }
        }

        internal ImDrawListPtr GetDrawList()
        {
            return _drawListPtr;
        }

        internal void Log(LogLevelEnum level, Exception ex, string message, params object[] args)
        {
            switch (level)
            {
                case LogLevelEnum.Error:
                    PluginLog.Error(ex, message, args);
                    break;
                case LogLevelEnum.Warning:
                    PluginLog.Warning(ex, message, args);
                    break;
                case LogLevelEnum.Info:
                    PluginLog.Information(ex, message, args);
                    break;
                case LogLevelEnum.Debug:
                    PluginLog.Debug(ex, message, args);
                    break;
            }
        }

        internal void TrackObjects()
        {
            _run++;
            List<GameObject> newobjs = new List<GameObject>();
            Dictionary<nint, uint> repobjs = new Dictionary<nint, uint>();
            int numcurrent = _objectsSeen.Count;
            int numobjs = ot.Length;
            foreach (GameObject go in ot)
            {
                if (
                    (go.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc)
                    &&
                    (go.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player)
                    &&
                    (go.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventObj)
                )
                {
                    continue;
                }
                if (_objectsSeen.ContainsKey(go.Address) == false)
                {
                    newobjs.Add(go);
                    _objectsToActors[go.Address] = go.ObjectId;
                }
                else if (_objectsToActors[go.Address] != go.ObjectId)
                {
                    repobjs[go.Address] = _objectsToActors[go.Address];
                    newobjs.Add(go);
                    _objectsToActors[go.Address] = go.ObjectId;
                }
                _objectsSeen[go.Address] = _run;
            }
            if (numcurrent != numobjs)
            {
                foreach (KeyValuePair<nint, ulong> kp in _objectsSeen)
                {
                    if (kp.Value != _run)
                    {
                        nint addr = kp.Key;
                        InvokeCombatantRemoved(_objectsToActors[addr], addr);
                        _objectsSeen.Remove(addr);
                        _objectsToActors.Remove(addr);
                    }
                }                
            }
            if (repobjs.Count > 0) 
            {
                foreach (KeyValuePair<nint, uint> kp in repobjs)
                {
                    InvokeCombatantRemoved(kp.Value, kp.Key);
                }
            }
            if (newobjs.Count > 0)
            {
                foreach (GameObject go in newobjs)
                {
                    InvokeCombatantAdded(go);
                }
            }
        }

        internal GameObject GetActorById(uint id)
        {
            GameObject go = ot.SearchById(id);
            return go;
        }

        internal GameObject GetActorByName(string name)
        {            
            foreach (GameObject go in ot)
            {
                if (String.Compare(name, go.Name.ToString()) == 0)
                {
                    return go;
                }
            }            
            return null;
        }        

        internal void AttachTaskToTaskChain(Task parent, Task task)
        {
            if (parent != null)
            {
                parent.ContinueWith(new Action<Task>((tx) =>
                {
                    if (parent.IsCompleted == true && parent.IsFaulted == false && parent.IsCanceled == false)
                    {
                        task.Start();
                    }
                    else
                    {
                        Log(LogLevelEnum.Error, parent.Exception, "Exception occurred: {0}", parent.Exception.Message);
                    }
                }));
            }
        }

        internal void ExecuteAutomarkers(AutomarkerPayload ap, AutomarkerTiming at)
        {
            if (ap.Clear == true)
            {
                Log(LogLevelEnum.Debug, null, "Clearing automarkers");
                Party pty = GetPartyMembers();
                foreach (Party.PartyMember pm in pty.Members)
                {
                    PerformMarking(pm.GameObject, AutomarkerSigns.SignEnum.None);
                }
                return;
            }
            Task first = null, prev = null, tx = null;
            Log(LogLevelEnum.Debug, null, "Executing automarker payload for {0} roles", ap.assignments.Count);
            foreach (KeyValuePair<AutomarkerSigns.SignEnum, GameObject> kp in ap.assignments)
            {
                int delay = first == null ? at.SampleInitialTime() : at.SampleSubsequentTime();
                Log(LogLevelEnum.Debug, null, "After {0} ms, mark actor {1} with {2}", delay, kp.Value, kp.Key);
                tx = new Task(() =>
                {
                    if (delay > 0)
                    {
                        Thread.Sleep(delay);
                    }
                    PerformMarking(kp.Value, kp.Key);
                });
                if (first == null)
                {
                    first = tx;
                }
                AttachTaskToTaskChain(prev, tx);
                prev = tx;
            }
            if (first != null)
            {
                first.Start();
            }
        }

        public unsafe void SubmitCommand(string cmd)
        {
            if (_postCmdFuncptr == null)
            {
                return;
            }
            AtkUnitBase* ptr = (AtkUnitBase*)gg.GetAddonByName("ChatLog", 1);
            if (ptr != null && ptr->IsVisible == true)
            {
                IntPtr uiModule = gg.GetUIModule();
                if (uiModule != IntPtr.Zero)
                {
                    using (Command payload = new Command(cmd))
                    {
                        IntPtr p = Marshal.AllocHGlobal(32);
                        try
                        {
                            Marshal.StructureToPtr(payload, p, false);
                            Log(LogLevelEnum.Debug, null, "Executing command {0}", cmd);
                            _postCmdFuncptr(uiModule, p, IntPtr.Zero, 0);
                        }
                        catch (Exception)
                        {
                        }
                        Marshal.FreeHGlobal(p);
                    }
                }
            }
        }

        internal void PerformMarking(GameObject go, AutomarkerSigns.SignEnum sign)
        {
            if (go == null)
            {
                return;
            }
            if (cfg.AutomarkerSoft == true)
            {
                PerformSoftMarking(go.ObjectId, sign);
                return;
            }
            if (_markingFuncPtr != null && cfg.AutomarkerCommands == false)
            {
                PerformMarking(go.ObjectId, sign);
                return;
            }
            string cmd = "/mk ";
            switch (sign)
            {
                case AutomarkerSigns.SignEnum.None: cmd += "off"; break;
                case AutomarkerSigns.SignEnum.Attack1: cmd += "attack1"; break;
                case AutomarkerSigns.SignEnum.Attack2: cmd += "attack2"; break;
                case AutomarkerSigns.SignEnum.Attack3: cmd += "attack3"; break;
                case AutomarkerSigns.SignEnum.Attack4: cmd += "attack4"; break;
                case AutomarkerSigns.SignEnum.Attack5: cmd += "attack5"; break;
                case AutomarkerSigns.SignEnum.Bind1: cmd += "bind1"; break;
                case AutomarkerSigns.SignEnum.Bind2: cmd += "bind2"; break;
                case AutomarkerSigns.SignEnum.Bind3: cmd += "bind3"; break;
                case AutomarkerSigns.SignEnum.Ignore1: cmd += "ignore1"; break;
                case AutomarkerSigns.SignEnum.Ignore2: cmd += "ignore2"; break;
                case AutomarkerSigns.SignEnum.Circle: cmd += "circle"; break;
                case AutomarkerSigns.SignEnum.Plus: cmd += "cross"; break;
                case AutomarkerSigns.SignEnum.Square: cmd += "square"; break;
                case AutomarkerSigns.SignEnum.Triangle: cmd += "triangle"; break;
            }
            string name = go.Name.ToString();
            int index = GetPartyMemberIndex(name);
            if (index > 0)
            {
                Log(LogLevelEnum.Debug, null, "Using command to mark actor {0} in party spot {1} with {2}", name, index, sign);
                cmd += " <" + index + ">";
                if (sign != AutomarkerSigns.SignEnum.None)
                {
                    cfg.AutomarkersServed++;
                }
                if (cfg.DebugOnlyLogAutomarkers == false)
                {
                    SubmitCommand(cmd);
                }
            }
        }

        internal unsafe Party GetPartyMembers()
        {
            AddonPartyList* apl = (AddonPartyList*)gg.GetAddonByName("_PartyList", 1);
            IntPtr pla = gg.FindAgentInterface(apl);
            Party pty = new Party();
            Dictionary<string, GameObject> party = new Dictionary<string, GameObject>();
            foreach (PartyMember pm in pl)
            {
                party[pm.Name.ToString()] = pm.GameObject;                
            }
            for (int i = 0; i < apl->MemberCount; i++)
            {                
                IntPtr p = (pla + (0x14ca + 0xd8 * i));
                string fullname = Marshal.PtrToStringUTF8(p);
                GameObject go = null;
                if (party.ContainsKey(fullname) == true)
                {
                    go = party[fullname];
                }
                else if (String.Compare(fullname, cs.LocalPlayer.Name.ToString()) == 0)
                {
                    go = cs.LocalPlayer as GameObject;
                }
                else
                {
                    go = GetActorByName(fullname);
                    
                }
                pty.Members.Add(new Party.PartyMember() { Index = i + 1, Name = fullname, ObjectId = go != null ? go.ObjectId : 0, GameObject = go });
            }
            return pty;
        }

        internal unsafe int GetPartyMemberIndex(string name)
        {
            Party pty = GetPartyMembers();
            foreach (Party.PartyMember pm in pty.Members)
            {
                if (String.Compare(pm.Name, name) == 0)
                {
                    return pm.Index;
                }
            }
            return 0;
        }

        internal void ClearAllMarking()
        {
            AutomarkerPayload ap = new AutomarkerPayload();
            ap.Clear = true;
            ExecuteAutomarkers(ap, cfg.DefaultAutomarkerTiming);
        }

        internal void AssignRandomSelections(Party pty, int numSelections)
        {
            numSelections = (int)Math.Min(pty.Members.Count, numSelections);
            Random r = new Random();
            while (numSelections > 0)
            {
                var temp = from ix in pty.Members where ix.Selection == 0 select ix;
                var pm = temp.ElementAt(r.Next(0, temp.Count()));
                pm.Selection = numSelections;
                numSelections--;
            }
        }

        internal void PerformMarking(string name, AutomarkerSigns.SignEnum sign)
        {
            PerformMarking(GetActorByName(name), sign);
        }

        internal void PerformMarking(uint actorId, AutomarkerSigns.SignEnum sign)
        {
            if (cfg.AutomarkerSoft == true)
            {
                PerformSoftMarking(actorId, sign);
                return;
            }
            if (_markingFuncPtr != null && cfg.AutomarkerCommands == false)
            {
                bool removal = false;
                if (sign == AutomarkerSigns.SignEnum.None)
                {
                    Log(LogLevelEnum.Debug, null, "Need to figure out current sign on actor {0}", actorId);
                    sign = GetCurrentMarker(actorId);
                    Log(LogLevelEnum.Debug, null, "Current sign on actor {0} is {1}", actorId, sign);
                    removal = true;
                }
                if (sign != AutomarkerSigns.SignEnum.None)
                {
                    Log(LogLevelEnum.Debug, null, "Using function pointer to mark actor {0} with {1}", actorId, sign);
                    if (removal == false)
                    {
                        cfg.AutomarkersServed++;
                    }
                    if (cfg.DebugOnlyLogAutomarkers == false)
                    {
                        _markingFuncPtr(_sigs["MarkingCtrl"], (byte)sign, actorId);
                    }
                }
                return;
            }
            PerformMarking(GetActorById(actorId), sign);
        }

        internal void PerformSoftMarking(uint actorId, AutomarkerSigns.SignEnum sign)
        {
            AutomarkerSigns.SignEnum prev = AutomarkerSigns.SignEnum.None;
            foreach (KeyValuePair<AutomarkerSigns.SignEnum, uint> kp in SoftMarkers)
            {
                if (kp.Value == actorId)
                {
                    prev = kp.Key;
                    Log(LogLevelEnum.Debug, null, "Removing soft mark {0} from actor {1}", kp.Key, kp.Value);
                    if (cfg.DebugOnlyLogAutomarkers == false)
                    {
                        SoftMarkers[kp.Key] = 0;
                    }
                }
            }
            if (sign == AutomarkerSigns.SignEnum.None || sign == prev)
            {
                return;
            }
            Log(LogLevelEnum.Debug, null, "Using soft marking to mark actor {0} with {1}", actorId, sign);
            cfg.AutomarkersServed++;
            if (cfg.DebugOnlyLogAutomarkers == false)
            {
                SoftMarkers[sign] = actorId;
            }
        }

        internal unsafe AutomarkerSigns.SignEnum GetCurrentMarker(uint actorId)
        {
            UIModule* ui = (UIModule*)gg.GetUIModule();
            if (ui == null)
            {
                Log(LogLevelEnum.Warning, null, "Couldn't get UIModule");
                return AutomarkerSigns.SignEnum.None;
            }
            UI3DModule* ui3d = ui->GetUI3DModule();
            if (ui3d == null)
            {
                Log(LogLevelEnum.Warning, null, "Couldn't get UI3DModule");
                return AutomarkerSigns.SignEnum.None;
            }
            AddonNamePlate* np = (AddonNamePlate*)gg.GetAddonByName("NamePlate", 1);
            if (ui3d == null)
            {
                Log(LogLevelEnum.Warning, null, "Couldn't get AddonNamePlate");
                return AutomarkerSigns.SignEnum.None;
            }
            for (int i = 0; i < ui3d->NamePlateObjectInfoCount; i++)
            {
                var o = ((UI3DModule.ObjectInfo**)ui3d->NamePlateObjectInfoPointerArray)[i];
                if (o == null || o->GameObject == null || o->GameObject->ObjectID != actorId)
                {
                    continue;
                }
                AddonNamePlate.NamePlateObject npo = np->NamePlateObjectArray[o->NamePlateIndex];
                if (npo.IsPlayerCharacter == true)
                {
                    AtkImageNode* ain = npo.ImageNode2;
                    if (ain == null)
                    {
                        return AutomarkerSigns.SignEnum.None;
                    }
                    AtkUldPartsList* upl = ain->PartsList;
                    if (upl == null)
                    {
                        return AutomarkerSigns.SignEnum.None;
                    }
                    for (int j = 0; j < upl->PartCount; j++)
                    {
                        AtkUldPart* p = &upl->Parts[j];
                        if (p->UldAsset == null)
                        {
                            continue;
                        }
                        if (p->UldAsset->AtkTexture.Resource == null)
                        {
                            continue;
                        }
                        switch (p->UldAsset->AtkTexture.Resource->IconID)
                        {
                            case 61301: return AutomarkerSigns.SignEnum.Attack1;
                            case 61302: return AutomarkerSigns.SignEnum.Attack2;
                            case 61303: return AutomarkerSigns.SignEnum.Attack3;
                            case 61304: return AutomarkerSigns.SignEnum.Attack4;
                            case 61305: return AutomarkerSigns.SignEnum.Attack5;
                            case 61311: return AutomarkerSigns.SignEnum.Bind1;
                            case 61312: return AutomarkerSigns.SignEnum.Bind2;
                            case 61313: return AutomarkerSigns.SignEnum.Bind3;
                            case 61321: return AutomarkerSigns.SignEnum.Ignore1;
                            case 61322: return AutomarkerSigns.SignEnum.Ignore2;
                            case 61331: return AutomarkerSigns.SignEnum.Square;
                            case 61332: return AutomarkerSigns.SignEnum.Circle;
                            case 61333: return AutomarkerSigns.SignEnum.Plus;
                            case 61334: return AutomarkerSigns.SignEnum.Triangle;
                        }
                    }
                    return AutomarkerSigns.SignEnum.None;
                }
            }
            Log(LogLevelEnum.Warning, null, "Couldn't find nameplate for actor {0}", actorId);
            return AutomarkerSigns.SignEnum.None;
        }

        internal void GetSignatures()
        {
            nint addr;
            addr = _sig.ScanText("48 89 5C 24 10 48 89 6C 24 18 57 48 83 EC 20 8D 42");
            if (addr != IntPtr.Zero)
            {
                _sigs["MarkingFunc"] = addr;
                Log(LogLevelEnum.Debug, null, "Marking function found at {0}", addr.ToString("X"));
                addr = _sig.OldGetStaticAddressFromSig("48 8B 94 24 ? ? ? ? 48 8D 0D ? ? ? ? 41 B0 01");
                if (addr != IntPtr.Zero)
                {
                    _sigs["MarkingCtrl"] = addr;
                    Log(LogLevelEnum.Debug, null, "Marking controller found at {0}", addr.ToString("X"));
                    _markingFuncPtr = Marshal.GetDelegateForFunctionPointer<MarkingFunctionDelegate>(_sigs["MarkingFunc"]);
                    StatusMarkingFuncAvailable = true;
                }
                else
                {
                    Log(LogLevelEnum.Warning, null, "Marking controller not found");
                }
            }
            else
            {
                Log(LogLevelEnum.Warning, null, "Marking function not found");
            }
            if (_markingFuncPtr != null)
            {
                Log(LogLevelEnum.Info, null, "Marking by function pointer available");
            }
            else
            {
                Log(LogLevelEnum.Warning, null, "Marking by function pointer not available, falling back to command injection");
            }
            // sig from saltycog/ffxiv-startup-commands
            addr = _sig.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 45 84 C9");
            if (addr != IntPtr.Zero)
            {
                _postCmdFuncptr = Marshal.GetDelegateForFunctionPointer<PostCommandDelegate>(addr);
                Log(LogLevelEnum.Debug, null, "Command post function found at {0}", addr.ToString("X"));
                StatusPostCommandAvailable = true;
            }
            else
            {
                Log(LogLevelEnum.Warning, null, "Command post function not found");
            }
        }

    }

}
