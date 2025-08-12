using Dalamud.Bindings.ImGui;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Game.Command;
using Dalamud.Game.NativeWrapper;
using Dalamud.Interface.Utility;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.Network;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Data;
using Lumina.Excel.Sheets;
using Lumina.Excel.Sheets.Experimental;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using static FFXIVClientStructs.FFXIV.Client.Game.Character.VfxContainer;
using BattleChara = Dalamud.Game.ClientState.Objects.Types.IBattleChara;
using Character = Dalamud.Game.ClientState.Objects.Types.ICharacter;
using CharacterStruct = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using DalamudGameObject = Dalamud.Game.ClientState.Objects.Types.IGameObject;
using GameObjectPtr = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using Item = Lumina.Excel.Sheets.Item;
using Status = Dalamud.Game.ClientState.Statuses.Status;

namespace Lemegeton.Core
{

    public sealed class State
    {

        public enum SoundEffectEnum
        {
            None,
            Se1,
            Se2,
            Se3,
            Se4,
            Se5,
            Se6,
            Se7,
            Se8,
            Se9,
            Se10,
            Se11,
            Se12,
            Se13,
            Se14,
            Se15,
            Se16,
        }

        internal enum LogLevelEnum
        {
            Error,
            Warning,
            Info,
            Debug
        }

        internal class ReactionExecution
        {

            internal Timeline.Reaction reaction { get; set; }
            internal Context ctx { get; set; }
            internal float when { get; set; }

            public ReactionExecution(Context ct, Timeline.Reaction r, float currentTime, float timeToEvent)
            {
                ctx = ct;
                reaction = r;
                when = currentTime + r.EffectiveTime + timeToEvent;
            }

            public void Execute()
            {
                reaction.Execute(ctx);
            }

        }

        internal class DeferredInvoke
        {
            
            public State State { get; set; }
            public Delegate Function { get; set; } = null;
            public string CommandText { get; set; }
            public object[] Params { get; set; }
            public DateTime FireAt { get; set; } = DateTime.MinValue;
            public uint ActorId { get; set; } = 0;

            private unsafe void InvokeCommand()
            {
                IGameGui gg = State.gg;
                AtkUnitBasePtr ptr = gg.GetAddonByName("ChatLog", 1);
                if (ptr.IsNull == false && ptr.IsVisible == true)
                {
                    IntPtr uiModule = gg.GetUIModule();
                    if (uiModule != IntPtr.Zero)
                    {
                        using (Command payload = new Command(CommandText))
                        {
                            IntPtr p = Marshal.AllocHGlobal(32);
                            try
                            {
                                Marshal.StructureToPtr(payload, p, false);
                                State.Log(LogLevelEnum.Debug, null, "Executing command {0}", CommandText);
                                State._postCmdFuncptr(uiModule, p, IntPtr.Zero, 0);
                            }
                            catch (Exception)
                            {
                            }
                            Marshal.FreeHGlobal(p);
                        }
                    }
                }
            }

            public bool CanInvoke()
            {
                return (DateTime.Now > FireAt);
            }

            public void Invoke()
            {
                try
                {
                    if (Function != null)
                    {
                        Function.DynamicInvoke(Params);                        
                    }
                    else
                    {
                        InvokeCommand();
                    }
                }
                catch (Exception ex)
                {
                    State.Log(LogLevelEnum.Error, null, "Exception in invoke: {0} {1}", ex.Message, ex.StackTrace);
                }
            }

        }

        internal class MarkerApplication
        {

            public DateTime Timestamp = DateTime.Now;
            public string Location { get; set; } = "?";
            public string Source { get; set; } = "?";
            public string Destination { get; set; } = "?";
            public bool SoftMarker { get; set; } = false;
            public AutomarkerSigns.SignEnum Sign { get; set; } = AutomarkerSigns.SignEnum.None;

        }

        [PluginService] public IDalamudPluginInterface pi { get; private set; }
        [PluginService] public IChatGui cg { get; private set; }
        [PluginService] public ICommandManager cm { get; private set; }
        [PluginService] public IObjectTable ot { get; private set; }
        [PluginService] public IGameGui gg { get; private set; }
        [PluginService] public IClientState cs { get; private set; }
        [PluginService] public IDataManager dm { get; private set; }
        [PluginService] public ITextureProvider tp { get; private set; }
        [PluginService] public ICondition cd { get; private set; }
        [PluginService] public IFramework fw { get; private set; }
        [PluginService] public ISigScanner ss { get; private set; }
        [PluginService] public IPartyList pl { get; private set; }
        [PluginService] public ITargetManager tm { get; private set; }
        [PluginService] public IPluginLog lo { get; private set; }
        [PluginService] public IGameInteropProvider io { get; private set; }

        internal GameNetwork gn = null;
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
        internal int NumFeaturesAutomation { get; set; } = 0;
#endif
        internal string GameVersion { get; set; } = "(unknown)";

        internal Plugin plug;
        internal Config cfg;
        internal SigLocator _sig;
        internal NetworkDecoder _dec;
        internal Timeline _timeline = null;
        internal List<ReactionExecution> ReactionQueue = new List<ReactionExecution>();

        private Dictionary<string, nint> _sigs = new Dictionary<string, nint>();
        internal delegate char MarkingFunctionDelegate(nint ctrl, byte markId, uint actorId);
        private MarkingFunctionDelegate _markingFuncPtr = null;
        internal Dalamud.Hooking.Hook<MarkingFunctionDelegate> _markingFuncHook = null;

        private delegate void PostCommandDelegate(IntPtr ui, IntPtr cmd, IntPtr unk1, byte unk2);
        private PostCommandDelegate _postCmdFuncptr = null;
        public Dictionary<AutomarkerSigns.SignEnum, ulong> SoftMarkers = new Dictionary<AutomarkerSigns.SignEnum, ulong>();
        internal Dictionary<ushort, Timeline> AllTimelines = new Dictionary<ushort, Timeline>();
        internal Dictionary<ushort, string> TimelineOverrides = new Dictionary<ushort, string>();
        internal List<Tuple<DateTime, string>>[] internalDebug;
        internal int internalDebugCursor = -1;

        internal List<MarkerApplication> MarkerHistory = new List<MarkerApplication>();

        private bool _markersApplied = false;
        internal bool _suppressCombatEndMarkRemoval = false;
        private bool _drawingReady = false;
        private int _drawingStarts = 0;
        private ImDrawListPtr _drawListPtr;
        private bool _listening = false;
        private bool _prevTargettable = false;
        private DateTime _tlUpdate = DateTime.Now;
        public bool _inCombat = false;
        private ulong _runObject = 0;
        internal ulong _runInstance = 1;
        internal List<DeferredInvoke> InvoqThread = new List<DeferredInvoke>();
        internal List<DeferredInvoke> InvoqFramework = new List<DeferredInvoke>();
        internal AutoResetEvent InvoqThreadNew = new AutoResetEvent(false);
        internal bool _newReactions = false;
        internal ushort _territoryCurrent = 0;
        internal ushort _territoryNext = 0;

        private Dictionary<nint, bool> _objectsInCombat = new Dictionary<nint, bool>();
        private Dictionary<nint, ulong> _objectsSeen = new Dictionary<nint, ulong>();
        private Dictionary<nint, ulong> _objectsToActors = new Dictionary<nint, ulong>();

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

        internal delegate void ActorControlDelegate(ushort category, uint sourceActorId, uint targetActorId, uint param1, uint param2, uint param3, uint param4);
        internal event ActorControlDelegate OnActorControl;

        internal delegate void MapEffectDelegate(byte[] data);
        internal event MapEffectDelegate OnMapEffect;

        internal delegate void CombatantAddedDelegate(IGameObject go);
        internal event CombatantAddedDelegate OnCombatantAdded;

        internal delegate void CombatantRemovedDelegate(ulong actorId, nint addr);
        internal event CombatantRemovedDelegate OnCombatantRemoved;

        internal delegate void EventPlayDelegate(uint actorId, uint eventId, ushort scene, uint flags, uint param1, ushort param2, byte param3, uint param4);
        internal event EventPlayDelegate OnEventPlay;

        internal delegate void EventPlay64Delegate();
        internal event EventPlay64Delegate OnEventPlay64;

        internal delegate void TargettableDelegate();
        internal event TargettableDelegate OnTargettable;
        internal event TargettableDelegate OnUntargettable;

        internal unsafe delegate StatusFlags StatusFlagGetterDelegate(BattleChara bc);
        internal StatusFlagGetterDelegate GetStatusFlags;

        internal string FormatZoneChange(ushort newZone)
        {
            string fmt = "InvokeZoneChange {0}";
            return String.Format(fmt, newZone);
        }

        internal void InvokeZoneChange(ushort newZone)
        {
            fw.RunOnFrameworkThread(() =>
            {
                InternalLog(FormatZoneChange(newZone));
                OnZoneChange?.Invoke(newZone);
            });
        }

        internal string FormatCombatChange(bool inCombat)
        {
            string fmt = "InvokeCombatChange {0}";
            return String.Format(fmt, inCombat);
        }

        internal void InvokeCombatChange(bool inCombat)
        {
            fw.RunOnFrameworkThread(() =>
            {
                InternalLog(FormatCombatChange(inCombat));
                OnCombatChange?.Invoke(inCombat);
            });
        }

        internal string FormatCastBegin(uint src, uint dest, ushort actionId, float castTime, float rotation)
        {
            string fmt = "InvokeCastBegin {0} -> {1}: {2} in {3} s";
            DalamudGameObject srcgo = GetActorById(src);
            DalamudGameObject dstgo = GetActorById(dest);
            return String.Format(fmt, FormatGameObject(srcgo), FormatGameObject(dstgo), FormatActionId(actionId), castTime);
        }

        internal void InvokeCastBegin(uint src, uint dest, ushort actionId, float castTime, float rotation)
        {
            Timeline tl = _timeline;
            if (tl != null)
            {
                tl.FeedEventCastBegin(this, GetActorById(src), GetActorById(dest), actionId, castTime);
            }
            fw.RunOnFrameworkThread(() =>
            {
                InternalLog(FormatCastBegin(src, dest, actionId, castTime, rotation));
                OnCastBegin?.Invoke(src, dest, actionId, castTime, rotation);
            });            
        }

        internal string FormatAction(uint src, uint dest, ushort actionId)
        {
            string fmt = "InvokeAction {0} -> {1}: {2}";
            DalamudGameObject srcgo = GetActorById(src);
            DalamudGameObject dstgo = GetActorById(dest);
            return String.Format(fmt, FormatGameObject(srcgo), FormatGameObject(dstgo), FormatActionId(actionId));
        }

        internal void InvokeAction(uint src, uint dest, ushort actionId)
        {
            Timeline tl = _timeline;
            if (tl != null)
            {
                tl.FeedEventCastEnd(this, GetActorById(src), GetActorById(dest), actionId);
            }
            fw.RunOnFrameworkThread(() =>
            {
                InternalLog(FormatAction(src, dest, actionId));
                OnAction?.Invoke(src, dest, actionId);
            });
        }

        internal string FormatMapEffect(byte[] data)
        {
            string fmt = "InvokeMapEffect: {0}";
            List<string> bytes = new List<string>();
            foreach (byte b in data)
            {
                bytes.Add(b.ToString("X2"));
            }
            return String.Format(fmt, String.Join(" ", bytes));
        }

        internal void InvokeMapEffect(byte[] data)
        {
            fw.RunOnFrameworkThread(() =>
            {
                InternalLog(FormatMapEffect(data));
                OnMapEffect?.Invoke(data);
            });
        }

        internal string FormatHeadmarker(uint dest, uint markerId)
        {
            string fmt = "InvokeHeadmarker {0}: {1}";
            DalamudGameObject dstgo = GetActorById(dest);
            return String.Format(fmt, FormatGameObject(dstgo), markerId);
        }

        internal void InvokeHeadmarker(uint dest, uint markerId)
        {
            fw.RunOnFrameworkThread(() =>
            {
                InternalLog(FormatHeadmarker(dest, markerId));
                OnHeadMarker?.Invoke(dest, markerId);
            });
        }

        internal string FormatTether(uint src, uint dest, uint tetherId)
        {
            string fmt = "InvokeTether {0} -> {1}: {2}";
            DalamudGameObject srcgo = GetActorById(src);
            DalamudGameObject dstgo = GetActorById(dest);
            return String.Format(fmt, FormatGameObject(srcgo), FormatGameObject(dstgo), tetherId);
        }

        internal void InvokeTether(uint src, uint dest, uint tetherId)
        {
            fw.RunOnFrameworkThread(() =>
            {
                InternalLog(FormatTether(src, dest, tetherId));
                OnTether?.Invoke(src, dest, tetherId);
            });
        }

        internal string FormatDirectorUpdate(uint param1, uint param2, uint param3, uint param4)
        {            
            string fmt = "InvokeDirectorUpdate: {0:X8} {1:X8} {2:X8} {3:X8}";
            return String.Format(fmt, param1, param2, param3, param4);
        }

        internal void InvokeDirectorUpdate(uint param1, uint param2, uint param3, uint param4)
        {
            fw.RunOnFrameworkThread(() =>
            {
                InternalLog(FormatDirectorUpdate(param1, param2, param3, param4));
                OnDirectorUpdate?.Invoke(param1, param2, param3, param4);
            });
        }

        internal string FormatActorControl(ushort category, uint sourceActorId, uint targetActorId, uint param1, uint param2, uint param3, uint param4)
        {
            string fmt = "InvokeActorControl: {0} {1} -> {2} {3:X8} {4:X8} {5:X8} {6:X8}";
            DalamudGameObject src = GetActorById(sourceActorId);
            DalamudGameObject dst = GetActorById(targetActorId);
            return String.Format(fmt, category, FormatGameObject(src), FormatGameObject(dst), param1, param2, param3, param4);
        }

        internal void InvokeActorControl(ushort category, uint sourceActorId, uint targetActorId, uint param1, uint param2, uint param3, uint param4)
        {
            fw.RunOnFrameworkThread(() =>
            {
                InternalLog(FormatActorControl(category, sourceActorId, targetActorId, param1, param2, param3, param4));
                OnActorControl?.Invoke(category, sourceActorId, targetActorId, param1, param2, param3, param4);
            });
        }

        internal string FormatStatusChange(uint src, uint dest, uint statusId, bool gained, float duration, int stacks)
        {
            string fmt = "InvokeStatusChange {0} -> {1}: {2} {3} for {4} s with {5} stacks";
            DalamudGameObject srcgo = GetActorById(src);
            DalamudGameObject dstgo = GetActorById(dest);
            return String.Format(fmt, FormatGameObject(srcgo), FormatGameObject(dstgo), gained == true ? "Gained" : "Lost", FormatStatusId(statusId), duration, stacks);
        }

        internal void InvokeStatusChange(uint src, uint dest, uint statusId, bool gained, float duration, int stacks)
        {
            fw.RunOnFrameworkThread(() =>
            {
                InternalLog(FormatStatusChange(src, dest, statusId, gained, duration, stacks));
                OnStatusChange?.Invoke(src, dest, statusId, gained, duration, stacks);
            });
        }

        internal string FormatCombatantAdded(IGameObject go)
        {
            string fmt = "CombatantAdded {0}";
            return String.Format(fmt, FormatGameObject(go));
        }

        internal void InvokeCombatantAdded(IGameObject go)
        {
            fw.RunOnFrameworkThread(() =>
            {
                InternalLog(FormatCombatantAdded(go));
                OnCombatantAdded?.Invoke(go);
            });
        }

        internal string FormatCombatantRemoved(ulong actorId, nint addr)
        {
            string fmt = "CombatantRemoved {0:X8} at {1}";
            return String.Format(fmt, actorId, addr);
        }

        internal void InvokeCombatantRemoved(ulong actorId, nint addr)
        {
            fw.RunOnFrameworkThread(() =>
            {
                InternalLog(FormatCombatantRemoved(actorId, addr));
                OnCombatantRemoved?.Invoke(actorId, addr);
            });
        }

        internal string FormatEventPlay(uint actorId, uint eventId, ushort scene, uint flags, uint param1, ushort param2, byte param3, uint param4)
        {
            DalamudGameObject actor = GetActorById(actorId);
            string fmt = "InvokeEventPlay: {0:X8} {1} {2} {3} {4} {5} {6} {7}";
            return String.Format(fmt, FormatGameObject(actor), eventId, scene, flags, param1, param2, param3, param4);
        }

        internal void InvokeEventPlay(uint actorId, uint eventId, ushort scene, uint flags, uint param1, ushort param2, byte param3, uint param4)
        {
            fw.RunOnFrameworkThread(() =>
            {
                InternalLog(FormatEventPlay(actorId, eventId, scene, flags, param1, param2, param3, param4));
                OnEventPlay?.Invoke(actorId, eventId, scene, flags, param1, param2, param3, param4);
            });
        }

        internal string FormatEventPlay64()
        {
            string fmt = "InvokeEventPlay64";
            return String.Format(fmt);
        }

        internal void InvokeEventPlay64()
        {
            fw.RunOnFrameworkThread(() =>
            {
                InternalLog(FormatEventPlay64());
                OnEventPlay64?.Invoke();
            });
        }

        internal string FormatTargettable()
        {
            string fmt = "Targettable";
            return String.Format(fmt);
        }

        internal void InvokeTargettable()
        {
            fw.RunOnFrameworkThread(() =>
            {
                InternalLog(FormatTargettable());
                OnTargettable?.Invoke();
            });
        }

        internal string FormatUntargettable()
        {
            string fmt = "Untargettable";
            return String.Format(fmt);
        }

        internal void InvokeUntargettable()
        {
            fw.RunOnFrameworkThread(() =>
            {
                InternalLog(FormatUntargettable());
                OnUntargettable?.Invoke();
            });
        }

        public State()
        {
            internalDebug = new List<Tuple<DateTime, string>>[60];
            for (int i = 0; i < 60; i++)
            {
                internalDebug[i] = new List<Tuple<DateTime, string>>();
            }
            GetStatusFlags = GetStatusFlags1;
        }

        internal void AddMarkerHistory(IGameObject source, IGameObject destination, bool soft, AutomarkerSigns.SignEnum sign)
        {
            fw.RunOnFrameworkThread(() =>
            {
                if (source == null)
                {
                    source = cs.LocalPlayer;
                }
                string location = plug.GetInstanceNameForTerritory(cs.TerritoryType);
                MarkerApplication ma = new MarkerApplication()
                {
                    Sign = sign,
                    SoftMarker = soft,
                    Location = location,
                    Source = source.Name.ToString(),
                    Destination = destination != null ? destination.Name.ToString() : I18n.Translate("MainMenu/Settings/AutomarkerHistoryRemoved")
                };
                lock (MarkerHistory)
                {
                    MarkerHistory.Insert(0, ma);
                    if (MarkerHistory.Count > 100)
                    {
                        MarkerHistory.RemoveAt(100);
                    }
                }
            });
        }

        public void PrepareFolder(string path)
        {
            if (Path.Exists(path) == true)
            {
                return;
            }
            Log(LogLevelEnum.Info, null, "Creating path {0}", path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
        }

        public void LoadLocalTimelines(ushort territory)
        {
            try
            {
                PrepareFolder(cfg.TimelineLocalFolder);
                var timelinefiles = Directory.GetFiles(cfg.TimelineLocalFolder, "*.timeline.xml").OrderBy(x => new FileInfo(x).LastWriteTime);
                Regex rex = new Regex("Lemegeton_(?<territory>[0-9]{1,})[^0-9]");                
                Dictionary<ushort, Timeline> tls = new Dictionary<ushort, Timeline>();
                foreach (string fn in timelinefiles)
                {
                    Match m = rex.Match(fn);
                    if (m.Success == false)
                    {
                        continue;
                    }
                    ushort t = ushort.Parse(m.Groups["territory"].Value);
                    if (tls.ContainsKey(t) == true || (territory != 0 && t != territory))
                    {
                        continue;
                    }
                    Timeline tlx = LoadTimeline(fn);
                    if (tlx != null)
                    {
                        tls[tlx.Territory] = tlx;
                    }
                }
                foreach (KeyValuePair<ushort, Timeline> kp in tls)
                {
                    lock (TimelineOverrides)
                    {
                        if (TimelineOverrides.ContainsKey(kp.Key))
                        {
                            Log(LogLevelEnum.Debug, null, "Timeline from {0} not set to territory {1}, manually overridden", kp.Value.Filename, kp.Key);
                            continue;
                        }
                    }
                    Log(LogLevelEnum.Debug, null, "Timeline from {0} automatically set to territory {1}", kp.Value.Filename, kp.Key);
                    lock (AllTimelines)
                    {
                        AllTimelines[kp.Key] = kp.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Log(LogLevelEnum.Error, ex, "Couldn't load timelines due to an exception");
            }
        }

        public void LoadOverriddenTimelines()
        {
            Dictionary<ushort, string> tlcopy;
            lock (TimelineOverrides)                
            {
                tlcopy = new Dictionary<ushort, string>(TimelineOverrides);
            }
            foreach (KeyValuePair<ushort, string> kp in tlcopy)
            {
                Timeline tlx = LoadTimeline(kp.Value);
                Log(LogLevelEnum.Debug, null, "Timeline override from {0} set to territory {1}", kp.Value, kp.Key);
                lock (AllTimelines)
                {
                    AllTimelines[kp.Key] = tlx;
                }
            }
        }

        public Timeline LoadTimeline(string filename)
        {
            try
            {                
                Log(LogLevelEnum.Debug, null, "Loading timeline from {0}", filename);
                FileInfo fi = new FileInfo(filename);
                string data = File.ReadAllText(filename);
                Timeline tl = XmlSerializer<Timeline>.Deserialize(data);
                tl.Filename = filename;
                tl.LastModified = fi.LastWriteTime;
                if (tl.Territory == 0)
                {
                    Log(LogLevelEnum.Debug, null, "No territory specified in timeline file {0}", filename);
                    tl = null;
                }
                return tl;
            }
            catch (Exception ex)
            {
                Log(LogLevelEnum.Error, ex, "Timeline load failed from file {0}, exception: {1}", filename, ex.Message);
            }
            return null;
        }

        public void UnloadTimeline(ushort territory)
        {
            lock (AllTimelines)
            {
                if (AllTimelines.TryGetValue(territory, out Timeline tl) == true)
                {
                    AllTimelines.Remove(territory);
                }
            }
        }

        public Timeline GetTimeline(ushort territory)
        {
            lock (AllTimelines)
            {
                if (AllTimelines.TryGetValue(territory, out Timeline tl) == true)
                {
                    return tl;
                }
            }
            return null;
        }

        public Timeline CheckTimelineReload(Timeline tl)
        {
            if (tl.Filename == null)
            {
                Log(LogLevelEnum.Debug, null, "Can't check reload for timeline for territory {0}, source file not specified", tl.Territory);
                return tl;
            }
            FileInfo fi = new FileInfo(tl.Filename);
            if (fi.Exists == false)
            {
                Log(LogLevelEnum.Debug, null, "Can't check reload for timeline for territory {0}, source file {1} doesn't exist anymore", tl.Territory, tl.LastModified);
                return tl;
            }
            if (fi.LastWriteTime > tl.LastModified)
            {
                Log(LogLevelEnum.Debug, null, "Timeline for territory {0} has changed since last load, reloading from {1}", tl.Territory, tl.LastModified);
                Timeline.Profile dpro = tl.DefaultProfile;
                List<Timeline.Profile> pros = new List<Timeline.Profile>(tl.Profiles);
                Timeline ntl = LoadTimeline(tl.Filename);                
                if (ntl != null && ntl.Territory == tl.Territory)
                {
                    ntl.Profiles.AddRange(pros);
                    ntl.DefaultProfile = dpro;
                    return ntl;
                }
            }
            else
            {
                Log(LogLevelEnum.Debug, null, "Timeline file {0} for territory {1} has not changed since last load", tl.Filename, tl.Territory);
            }
            return tl;
        }

        public void Initialize()
        {
            GetGameVersion();
            if (gn == null)
            {
                gn = new GameNetwork(io);
            }
            _sig = new SigLocator(this);
            _dec = new NetworkDecoder(this);
            fw.Update += FrameworkUpdate;
            cd.ConditionChange += Cd_ConditionChange;
            List<string> halp = new List<string>();
            halp.Add("Open Lemegeton configuration");
            halp.Add("/lemmy clear → Force clear all markers");
            halp.Add("/lemmy mark <sign> <target> → Places a softmarker; follows the same arguments as the ingame /mk command");
#if !SANS_GOETIA
            halp.Add("/lemmy <feature> <on|off|toggle> → Turn or toggle feature on/off, where feature can be: automarkers, drawing, sound, hacks, automation, softmarkers");
#else
            halp.Add("/lemmy <feature> <on|off|toggle> → Turn or toggle feature on/off, where feature can be: automarkers, drawing, sound, softmarkers");
#endif
            cm.AddHandler("/lemmy", new CommandInfo(OnCommand)
            {
                HelpMessage = string.Join("\n", halp),
            });
            cs.TerritoryChanged += Cs_TerritoryChanged;
            pi.UiBuilder.OpenMainUi += UiBuilder_OpenConfigUi;
            pi.UiBuilder.OpenConfigUi += UiBuilder_OpenConfigUi;
            plug.DeserializeTimelineOverrides(cfg.PropertyBlob);
            if (cfg.TimelineLocalAllowed == true)
            {
                LoadLocalTimelines(0);
            }
            LoadOverriddenTimelines();
            Cs_TerritoryChanged(cs.TerritoryType);
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
            if (gn != null)
            {
                if (_listening == true)
                {
                    gn.NetworkMessage -= _dec.NetworkMessageReceived;
                }
                gn.Dispose();
                gn = null;
            }
            cs.TerritoryChanged -= Cs_TerritoryChanged;
            pi.UiBuilder.OpenConfigUi -= UiBuilder_OpenConfigUi;
            pi.UiBuilder.OpenMainUi -= UiBuilder_OpenConfigUi;
            cm.RemoveHandler("/lemmy");
            cd.ConditionChange -= Cd_ConditionChange;
            fw.Update -= FrameworkUpdate;
        }

        private void FrameworkUpdate(IFramework framework)
        {
            /*
            if (_markingFuncHook == null)
            {
                if (_sigs.ContainsKey("MarkingFunc") == true)
                { 
                    _markingFuncHook = io.HookFromAddress<MarkingFunctionDelegate>(_sigs["MarkingFunc"], MarkingHook);
                    _markingFuncHook.Enable();
                    Log(LogLevelEnum.Info, null, "Marking function hooked");
                }
            }*/
            if (_territoryNext != _territoryCurrent)
            {
                Log(LogLevelEnum.Debug, null, "Territory changing from {0} to {1}", _territoryCurrent, _territoryNext);
                _timeline = null;
                _territoryCurrent = _territoryNext;                
                AutoselectTimeline(_territoryCurrent);
                InvokeZoneChange(_territoryCurrent);
            }
            if (_inCombat == true)
            {
                Timeline t = _timeline;
                if (t != null)
                {
                    float delta = (float)(DateTime.Now - _tlUpdate).TotalSeconds;
                    t.AdvanceTime(this, delta);
                }
            }
            _tlUpdate = DateTime.Now;
            ProcessInvocations(InvoqFramework);
            ProcessReactions(ReactionQueue);
        }

        internal void QueueReactionExecution(Context ctx, Timeline.Reaction r, float timeToEvent)
        {
            if (r.Fired == true)
            {
                return;
            }
            r.Fired = true;
            float ct = _timeline != null ? _timeline.CurrentTime : -1.0f;
            ReactionExecution re = new ReactionExecution(ctx, r, ct, timeToEvent);
            lock (ReactionQueue)
            {
                ReactionQueue.Add(re);
                ReactionQueue.Sort((a, b) => a.when.CompareTo(b.when));
                Log(LogLevelEnum.Debug, null, "Queued reaction {0} execution to {1} at tl time {2}", 
                    r.Name, 
                    re.when,
                    ct
                );
                _newReactions = true;
            }
        }

        internal void AutoselectTimeline(ushort territory)
        {
            _timeline = null;
            if (cs.LocalPlayer == null)
            {
                return;
            }
            Timeline tl = GetTimeline(territory);
            if (tl != null)
            {
                tl = CheckTimelineReload(tl);
                _timeline = tl;
                _timeline.Reset(this);
                int num = tl.SelectProfiles(cs.LocalPlayer.ClassJob.RowId);
                if (num > 0)
                {
                    Log(LogLevelEnum.Debug, null, "Timeline available for territory {0}, {1} selected profile(s)", territory, num);
                }
                else
                {
                    Log(LogLevelEnum.Debug, null, "Timeline available for territory {0}, no profile selected", territory);
                }
            }
            else
            {
                Log(LogLevelEnum.Debug, null, "No timeline available for territory {0}", territory);
            }            
            ClearReactionQueue();
        }

        internal void Cs_TerritoryChanged(ushort e)
        {
            _territoryNext = e;
        }

        private void Cd_ConditionChange(ConditionFlag flag, bool value)
        {
            if (flag == ConditionFlag.InCombat)
            {
                _inCombat = value;
                _runInstance++;
                Timeline t = _timeline;
                if (value == false)
                {
                    if (t != null)
                    {
                        int num = t.SelectProfiles(cs.LocalPlayer.ClassJob.RowId);
                        if (num > 0)
                        {
                            Log(LogLevelEnum.Debug, null, "Resetting timeline on combat end, {0} selected profile(s)", num);
                        }
                        else
                        {
                            Log(LogLevelEnum.Debug, null, "Resetting timeline on combat end");
                        }
                        t.Reset(this);
                        ClearReactionQueue();
                    }
                    if (cfg.RemoveMarkersAfterCombatEnd == true)
                    {
                        if (_suppressCombatEndMarkRemoval == false)
                        {
                            Log(LogLevelEnum.Debug, null, "Combat ended, removing markers");
                            ClearAutoMarkers();
                        }
                        else
                        {
                            Log(State.LogLevelEnum.Debug, null, "Not clearing marks on combat end, because wipe clear is also in effect");
                        }
                    }
                }
                if (t != null)
                {
                    if (value == true)
                    {
                        t.FeedCombatStart(this);
                    }
                    else
                    {
                        t.FeedCombatEnd(this);
                    }
                }
                _suppressCombatEndMarkRemoval = false;
                InvokeCombatChange(value);
            }
        }

        private void OnCommand(string command, string args)
        {
            string[] argsa = args.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (argsa.Count() > 0)
            {
                if (String.Compare(argsa[0], "clear", true) == 0)
                {
                    ClearAutoMarkers();
                }
                else if (String.Compare(argsa[0], "automarkers", true) == 0)
                {
                    if (argsa.Count() > 1)
                    {
                        if (String.Compare(argsa[1], "on", true) == 0)
                        {
                            cfg.QuickToggleAutomarkers = true;
                            cg.Print(I18n.Translate("Command/QuickToggleAutomarkers/On"));
                        }
                        else if (String.Compare(argsa[1], "off", true) == 0)
                        {
                            cfg.QuickToggleAutomarkers = false;
                            cg.Print(I18n.Translate("Command/QuickToggleAutomarkers/Off"));
                        }
                        else if (String.Compare(argsa[1], "toggle", true) == 0)
                        {
                            cfg.QuickToggleAutomarkers = (cfg.QuickToggleAutomarkers == false);
                            cg.Print(I18n.Translate("Command/QuickToggleAutomarkers/" + (cfg.QuickToggleAutomarkers == true ? "On" : "Off")));
                        }
                    }
                }
                else if (String.Compare(argsa[0], "drawing", true) == 0)
                {
                    if (argsa.Count() > 1)
                    {
                        if (String.Compare(argsa[1], "on", true) == 0)
                        {
                            cfg.QuickToggleOverlays = true;
                            cg.Print(I18n.Translate("Command/QuickToggleOverlays/On"));
                        }
                        else if (String.Compare(argsa[1], "off", true) == 0)
                        {
                            cfg.QuickToggleOverlays = false;
                            cg.Print(I18n.Translate("Command/QuickToggleOverlays/Off"));
                        }
                        else if (String.Compare(argsa[1], "toggle", true) == 0)
                        {
                            cfg.QuickToggleOverlays = (cfg.QuickToggleOverlays == false);
                            cg.Print(I18n.Translate("Command/QuickToggleOverlays/" + (cfg.QuickToggleOverlays == true ? "On" : "Off")));
                        }
                    }
                }
                else if (String.Compare(argsa[0], "sound", true) == 0)
                {
                    if (argsa.Count() > 1)
                    {
                        if (String.Compare(argsa[1], "on", true) == 0)
                        {
                            cfg.QuickToggleSound = true;
                            cg.Print(I18n.Translate("Command/QuickToggleSound/On"));
                        }
                        else if (String.Compare(argsa[1], "off", true) == 0)
                        {
                            cfg.QuickToggleSound = false;
                            cg.Print(I18n.Translate("Command/QuickToggleSound/Off"));
                        }
                        else if (String.Compare(argsa[1], "toggle", true) == 0)
                        {
                            cfg.QuickToggleSound = (cfg.QuickToggleSound == false);
                            cg.Print(I18n.Translate("Command/QuickToggleSound/" + (cfg.QuickToggleSound == true ? "On" : "Off")));
                        }
                    }
                }
                else if (String.Compare(argsa[0], "mark", true) == 0)
                {
                    if (argsa.Count() > 2)
                    {
                        PerformMarkingThroughCommand(argsa[1], argsa[2]);
                    }
                    else if (argsa.Count() > 1)
                    {
                        PerformMarkingThroughCommand(argsa[1], "<t>");
                    }
                }
#if !SANS_GOETIA
                else if (String.Compare(argsa[0], "hacks", true) == 0)
                {
                    if (argsa.Count() > 1)
                    {
                        if (String.Compare(argsa[1], "on", true) == 0)
                        {
                            cfg.QuickToggleHacks = true;
                            cg.Print(I18n.Translate("Command/QuickToggleHacks/On"));
                        }
                        else if (String.Compare(argsa[1], "off", true) == 0)
                        {
                            cfg.QuickToggleHacks = false;
                            cg.Print(I18n.Translate("Command/QuickToggleHacks/Off"));
                        }
                        else if (String.Compare(argsa[1], "toggle", true) == 0)
                        {
                            cfg.QuickToggleHacks = (cfg.QuickToggleHacks == false);
                            cg.Print(I18n.Translate("Command/QuickToggleHacks/" + (cfg.QuickToggleHacks == true ? "On" : "Off")));
                        }
                    }
                }
                else if (String.Compare(argsa[0], "automation", true) == 0)
                {
                    if (argsa.Count() > 1)
                    {
                        if (String.Compare(argsa[1], "on", true) == 0)
                        {
                            cfg.QuickToggleAutomation = true;
                            cg.Print(I18n.Translate("Command/QuickToggleAutomation/On"));
                        }
                        else if (String.Compare(argsa[1], "off", true) == 0)
                        {
                            cfg.QuickToggleAutomation = false;
                            cg.Print(I18n.Translate("Command/QuickToggleAutomation/Off"));
                        }
                        else if (String.Compare(argsa[1], "toggle", true) == 0)
                        {
                            cfg.QuickToggleAutomation = (cfg.QuickToggleAutomation == false);
                            cg.Print(I18n.Translate("Command/QuickToggleAutomation/" + (cfg.QuickToggleAutomation == true ? "On" : "Off")));
                        }
                    }
                }
#endif
                else if (String.Compare(argsa[0], "softmarkers", true) == 0)
                {
                    if (argsa.Count() > 1)
                    {
                        if (String.Compare(argsa[1], "on", true) == 0)
                        {
                            cfg.AutomarkerSoft = true;
                            cg.Print(I18n.Translate("Command/AutomarkerSoft/On"));
                        }
                        else if (String.Compare(argsa[1], "off", true) == 0)
                        {
                            cfg.AutomarkerSoft = false;
                            cg.Print(I18n.Translate("Command/AutomarkerSoft/Off"));
                        }
                        else if (String.Compare(argsa[1], "toggle", true) == 0)
                        {
                            cfg.AutomarkerSoft = (cfg.AutomarkerSoft == false);
                            cg.Print(I18n.Translate("Command/AutomarkerSoft/" + (cfg.AutomarkerSoft == true ? "On" : "Off")));
                        }
                    }
                }
            }
            else
            {
                cfg.Opened = true;
            }
        }

        internal void UnprepareInternals()
        {
            if (_listening == true)
            {
                gn.NetworkMessage -= _dec.NetworkMessageReceived;
                _listening = false;
            }
            StatusGotOpcodes = false;
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

        internal int ProcessInvocations(List<DeferredInvoke> queue)
        {
            DeferredInvoke di = null;
            lock (queue)
            {                
                if (queue.Count > 0)
                {                    
                    di = queue[0];
                    if (di.CanInvoke() == false)
                    {                        
                        return (int)(di.FireAt - DateTime.Now).TotalMilliseconds;
                    }
                    queue.RemoveAt(0);
                }
                else
                {
                    return Timeout.Infinite;
                }
            }            
            di.Invoke();
            return 0;
        }

        internal void ClearReactionQueue()
        {
            lock (ReactionQueue)
            {
                ReactionQueue.Clear();
            }
        }

        internal void ProcessReactions(List<ReactionExecution> queue)
        {
            if (_newReactions == false)
            {
                return;
            }
            List<ReactionExecution> res;
            Timeline tl = _timeline;
            if (tl != null)
            {
                lock (ReactionQueue)
                {
                    res = (from ix in ReactionQueue where ix.when <= tl.CurrentTime select ix).ToList();
                }
                if (res.Count > 0)
                {
                    foreach (ReactionExecution re in res)
                    {
                        re.Execute();
                    }
                    _newReactions = ReactionQueue.Count != res.Count;
                    lock (ReactionQueue)
                    {
                        foreach (ReactionExecution re in res)
                        {
                            ReactionQueue.Remove(re);
                        }
                    }
                }
            }
        }

        internal void QueueInvocation(DeferredInvoke di)
        {
            List<DeferredInvoke> queue = cfg.QueueFramework == true ? InvoqFramework : InvoqThread;
            AutoResetEvent ev = cfg.QueueFramework == true ? null : InvoqThreadNew;
            lock (queue)
            {
                queue.Add(di);
                queue.Sort((a, b) => a.FireAt.CompareTo(b.FireAt));
                if (ev != null)
                {
                    ev.Set();
                }
            }
        }

        internal bool StartDrawing(out ImDrawListPtr drawList)
        {
            if (cfg.QuickToggleOverlays == false)
            {
                drawList = null;
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

        internal string FormatGameObject(DalamudGameObject go)
        {
            return go == null ? "null" : String.Format("{0}({1} - {2}) at {3} pos {4},{5},{6} rot {7}", go.GameObjectId.ToString("X"), go.Name.ToString(), go.ObjectKind, go.Address.ToString("X"),
                go.Position.X, go.Position.Y, go.Position.Z, go.Rotation
            );
        }

        internal string FormatStatusId(uint statusId)
        {
            string name = plug.GetStatusName(statusId).Trim();
            return String.Format("{0} ({1})", statusId, name.Length > 0 ? name : "(null)");
        }

        internal string FormatActionId(uint actionId)
        {
            string name = plug.GetActionName(actionId).Trim();
            return String.Format("{0} ({1})", actionId, name.Length > 0 ? name : "(null)");
        }

        internal void InternalLog(string msg)
        {
            DateTime dt = DateTime.Now;
            int cm = dt.Minute;
            if (internalDebugCursor != cm)
            {
                internalDebugCursor = cm;
                internalDebug[(cm + 1) % 60].Clear();
            }
            lock (internalDebug[cm])
            {
                internalDebug[cm].Add(new Tuple<DateTime, string>(dt, msg));
            }
        }

        internal void Log(LogLevelEnum level, Exception ex, string message, params object[] args)
        {
            InternalLog(string.Format(message, args));
            switch (level)
            {
                case LogLevelEnum.Error:
                    lo.Error(ex, message, args);
                    break;
                case LogLevelEnum.Warning:
                    lo.Warning(ex, message, args);
                    break;
                case LogLevelEnum.Info:
                    lo.Information(ex, message, args);
                    break;
                case LogLevelEnum.Debug:
                    lo.Debug(ex, message, args);
                    break;
            }
        }

        internal unsafe List<InventoryItem> GetAllFoodInInventoryContainer(InventoryContainer* ic)
        {
            List<InventoryItem> items = new List<InventoryItem>();
            if (ic == null)
            {
                return items;
            }
            for (int i = 0; i < 35; i++)
            {
                FFXIVClientStructs.FFXIV.Client.Game.InventoryItem* ii = ic->GetInventorySlot(i);
                if (ii != null)
                {
                    var item = dm.Excel.GetSheet<Item>().GetRow(ii->ItemId);
                    if (item.ItemUICategory.RowId == 46)
                    {
                        items.Add(new InventoryItem() { Type = ic->Type, Slot = i, HQ = (ii->Flags & FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags.HighQuality) != 0, Item = item });
                    }
                }
            }
            return items;
        }

        internal unsafe InventoryItem FindItemInInventoryContainer(InventoryContainer* ic, uint itemId, bool hq)
        {            
            if (ic == null)
            {
                return null;
            }
            for (int i = 0; i < 35; i++)
            {
                FFXIVClientStructs.FFXIV.Client.Game.InventoryItem* ii = ic->GetInventorySlot(i);
                if (ii != null)
                {
                    bool isHq = (ii->Flags & FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags.HighQuality) != 0;
                    if (
                        (ii->ItemId == itemId)
                        &&
                        (
                            (isHq == true && hq == true)
                            ||
                            (isHq == false && hq == false)
                        )
                    )
                    {
                        var item = dm.Excel.GetSheet<Item>().GetRow(ii->ItemId);
                        return new InventoryItem() { Type = ic->Type, Slot = i, HQ = isHq, Item = item };
                    }
                }
            }
            return null;
        }

        internal unsafe InventoryItem FindItemInInventory(uint itemId, bool hq)
        {
            InventoryManager* im = InventoryManager.Instance();
            InventoryItem ii;
            ii = FindItemInInventoryContainer(im->GetInventoryContainer(InventoryType.Inventory1), itemId, hq);
            if (ii != null)
            {
                return ii;
            }
            ii = FindItemInInventoryContainer(im->GetInventoryContainer(InventoryType.Inventory2), itemId, hq);
            if (ii != null)
            {
                return ii;
            }
            ii = FindItemInInventoryContainer(im->GetInventoryContainer(InventoryType.Inventory3), itemId, hq);
            if (ii != null)
            {
                return ii;
            }
            ii = FindItemInInventoryContainer(im->GetInventoryContainer(InventoryType.Inventory4), itemId, hq);
            if (ii != null)
            {
                return ii;
            }
            return null;
        }

        internal unsafe List<InventoryItem> GetAllFoodInInventory()
        {
            List<InventoryItem> items = new List<InventoryItem>();
            InventoryManager* im = InventoryManager.Instance();
            items.AddRange(GetAllFoodInInventoryContainer(im->GetInventoryContainer(InventoryType.Inventory1)));
            items.AddRange(GetAllFoodInInventoryContainer(im->GetInventoryContainer(InventoryType.Inventory2)));
            items.AddRange(GetAllFoodInInventoryContainer(im->GetInventoryContainer(InventoryType.Inventory3)));
            items.AddRange(GetAllFoodInInventoryContainer(im->GetInventoryContainer(InventoryType.Inventory4)));
            items = items.GroupBy(x => new { x.Item.RowId, x.HQ }).Select(x => x.First()).ToList();
            items.Sort((a, b) =>
            {
                int ex = b.Item.LevelItem.RowId.CompareTo(a.Item.LevelItem.RowId);
                if (ex != 0)
                {
                    return ex;
                }
                ex = a.Item.Name.ToString().CompareTo(b.Item.Name.ToString());
                if (ex != 0)
                {
                    return ex;
                }
                ex = b.HQ.CompareTo(a.HQ);
                return ex;
            });
            return items;
        }

        internal bool CurrentlyWellFed(float forAtLeast)
        {
            StatusList sl = cs.LocalPlayer.StatusList;
            for (int i = 0; i < sl.Length; i++)
            {
                Status st = sl[i];
                if (st.StatusId == 48)
                {
                    return (st.RemainingTime >= forAtLeast);
                }
            }
            return false;
        }

        internal unsafe StatusFlags GetStatusFlags1(BattleChara bc)
        {
            try
            {
                return bc.StatusFlags;
            }
            catch (Exception)
            {
                Log(LogLevelEnum.Error, null, "Accessing status flags failed on method 1, falling back to method 2..");
                GetStatusFlags = GetStatusFlags2;
                return GetStatusFlags2(bc);
            }
        }

        // sometimes castinfo returns trash and dalamud throws an exception (on EN dalamud), workaround for that if the bug is still active
        internal unsafe StatusFlags GetStatusFlags2(BattleChara bc)
        {
            try
            {
                StatusFlags sf = StatusFlags.None;
                FFXIVClientStructs.FFXIV.Client.Game.Character.Character* Struct = (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)bc.Address;
                return (
                    (Struct->IsHostile ? StatusFlags.Hostile : StatusFlags.None)
                    |
                    (Struct->InCombat ? StatusFlags.InCombat : StatusFlags.None)
                    |
                    (Struct->IsWeaponDrawn ? StatusFlags.WeaponOut : StatusFlags.None)
                    |
                    (Struct->IsOffhandDrawn ? StatusFlags.OffhandOut : StatusFlags.None)
                    |
                    (Struct->IsPartyMember ? StatusFlags.PartyMember : StatusFlags.None)
                    |
                    (Struct->IsAllianceMember ? StatusFlags.AllianceMember : StatusFlags.None)
                    |
                    (Struct->IsFriend ? StatusFlags.Friend : StatusFlags.None)
                    |
                    (bc.CastActionId > 0 ? StatusFlags.IsCasting : StatusFlags.None)
                );
            }
            catch (Exception)
            {
                Log(LogLevelEnum.Error, null, "Accessing status flags failed on method 2, out of order..");
                GetStatusFlags = GetStatusFlagsOutOfOrder;
                return GetStatusFlagsOutOfOrder(bc);
            }
        }

        internal unsafe StatusFlags GetStatusFlagsOutOfOrder(BattleChara bc)
        {
            return StatusFlags.None;
        }

        internal void TrackObjects()
        {
            bool hostileTargettable = false;
            _runObject++;
            List<IGameObject> newobjs = new List<IGameObject>();
            Dictionary<nint, ulong> repobjs = new Dictionary<nint, ulong>();
            int numcurrent = _objectsSeen.Count;
            int numobjs = ot.Length;
            foreach (IGameObject go in ot)
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
                bool incombat = false;
                bool isavatar = false;
                if (go is Character)
                {
                    Character ch = (Character)go;
                    unsafe
                    {
                        CharacterStruct* chs = (CharacterStruct*)ch.Address;                        
                        isavatar = (chs->ModelContainer.ModelCharaId == 0);
                    }
                }
                if (go is BattleChara)
                {
                    BattleChara bc = (BattleChara)go;
                    incombat = (GetStatusFlags(bc) & Dalamud.Game.ClientState.Objects.Enums.StatusFlags.InCombat) != 0;
                    if (hostileTargettable == false && isavatar == false && (GetStatusFlags(bc) & Dalamud.Game.ClientState.Objects.Enums.StatusFlags.Hostile) != 0)
                    {
                        unsafe
                        {
                            GameObjectPtr* gop = (GameObjectPtr*)go.Address;
                            hostileTargettable = gop->GetIsTargetable();
                        }
                    }
                }
                if (_objectsSeen.ContainsKey(go.Address) == false)
                {
                    newobjs.Add(go);
                    _objectsToActors[go.Address] = go.GameObjectId;
                }
                else if (_objectsToActors[go.Address] != go.GameObjectId)
                {
                    repobjs[go.Address] = _objectsToActors[go.Address];
                    newobjs.Add(go);
                    _objectsToActors[go.Address] = go.GameObjectId;
                }
                _objectsSeen[go.Address] = _runObject;
                if (incombat == true)
                {
                    Timeline tl = _timeline;
                    if (_objectsInCombat.TryGetValue(go.Address, out bool oldcombat) == true)
                    {
                        if (oldcombat == false && tl != null)
                        {
                            tl.FeedPulled(this, go);
                        }
                    }
                    else if (tl != null)
                    {
                        tl.FeedPulled(this, go);
                    }
                }
                _objectsInCombat[go.Address] = incombat;
            }
            if (hostileTargettable != _prevTargettable)
            {
                _prevTargettable = hostileTargettable;
                Timeline tl = _timeline;
                if (hostileTargettable == true)
                {
                    InvokeTargettable();
                    if (tl != null)
                    {
                        tl.FeedEventTargettable(this);
                    }
                }
                else
                {
                    InvokeUntargettable();
                    if (tl != null)
                    {
                        tl.FeedEventUntargettable(this);
                    }
                }
            }
            if (numcurrent != numobjs)
            {
                foreach (KeyValuePair<nint, ulong> kp in _objectsSeen)
                {
                    if (kp.Value != _runObject)
                    {
                        nint addr = kp.Key;
                        InvokeCombatantRemoved(_objectsToActors[addr], addr);
                        _objectsSeen.Remove(addr);
                        _objectsInCombat.Remove(addr);
                        _objectsToActors.Remove(addr);
                    }
                }                
            }
            if (repobjs.Count > 0) 
            {
                foreach (KeyValuePair<nint, ulong> kp in repobjs)
                {
                    InvokeCombatantRemoved(kp.Value, kp.Key);
                }
            }
            if (newobjs.Count > 0)
            {
                Timeline tl = _timeline;
                foreach (IGameObject go in newobjs)
                {                    
                    if (tl != null)
                    {
                        tl.FeedNewCombatant(this, go);
                    }
                    InvokeCombatantAdded(go);
                }
            }
        }

        internal IGameObject GetActorById(ulong id)
        {
            IGameObject go = ot.SearchById(id);
            return go;
        }

        internal IGameObject GetActorByName(string name)
        {            
            foreach (IGameObject go in ot)
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

        internal void ClearAutoMarkers()
        {
            Log(LogLevelEnum.Debug, null, "Clearing automarkers, hard markers applied: {0}", _markersApplied);
            Party pty = GetPartyMembers();
            foreach (Party.PartyMember pm in pty.Members)
            {                
                ClearMarkerOn(pm.GameObject, _markersApplied, true);
            }
            _markersApplied = false;
        }

        internal void ExecuteAutomarkers(AutomarkerPayload ap, AutomarkerTiming at)
        {
            Task first = null, prev = null, tx = null;
            if (cfg.QuickToggleAutomarkers == false && ap.softMarker == false)
            {
                Log(LogLevelEnum.Debug, null, "Hard automarkers disabled");
                return;
            }
            if (cfg.QuickToggleOverlays == false && ap.softMarker == true)
            {
                Log(LogLevelEnum.Debug, null, "Soft automarkers disabled");
                return;
            }
            Log(LogLevelEnum.Debug, null, "Executing automarker payload for {0} roles, self mark: {1}, soft: {2}", ap.assignments.Count, ap.markSelfOnly, ap.softMarker);
            foreach (KeyValuePair<AutomarkerSigns.SignEnum, List<IGameObject>> kp in ap.assignments)
            {
                if (kp.Key == AutomarkerSigns.SignEnum.None)
                {
                    continue;
                }
                int delay;
                foreach (IGameObject go in kp.Value)
                {
                    delay = first == null ? at.SampleInitialTime() : at.SampleSubsequentTime();
                    Log(LogLevelEnum.Debug, null, "After {0} ms, mark actor {1:X} with {2} on instance {3}", delay,go, kp.Key, _runInstance);
                    tx = new Task(() =>
                    {
                        ulong run = _runInstance;
                        if (delay > 0)
                        {
                            Thread.Sleep(delay);
                        }
                        PerformMarking(run, go, kp.Key, ap.softMarker);
                    });
                    if (first == null)
                    {
                        first = tx;
                    }
                    AttachTaskToTaskChain(prev, tx);
                    prev = tx;
                }
            }
            if (first != null)
            {
                first.Start();
            }
        }

        internal IGameObject ParsePlaceholder(string target)
        {
            switch (target.ToLower())
            {
                case "<0>":
                case "<me>": { return cs.LocalPlayer; }
                case "<1>": { Party pty = GetPartyMembers(); return pty.GetByIndex(1)?.GameObject; }
                case "<2>": { Party pty = GetPartyMembers(); return pty.GetByIndex(2)?.GameObject; }
                case "<3>": { Party pty = GetPartyMembers(); return pty.GetByIndex(3)?.GameObject; }
                case "<4>": { Party pty = GetPartyMembers(); return pty.GetByIndex(4)?.GameObject; }
                case "<5>": { Party pty = GetPartyMembers(); return pty.GetByIndex(5)?.GameObject; }
                case "<6>": { Party pty = GetPartyMembers(); return pty.GetByIndex(6)?.GameObject; }
                case "<7>": { Party pty = GetPartyMembers(); return pty.GetByIndex(7)?.GameObject; }
                case "<8>": { Party pty = GetPartyMembers(); return pty.GetByIndex(8)?.GameObject; }
                case "<target>":
                case "<t>": { return cs.LocalPlayer.TargetObject; }
                case "<tt>":
                case "<t2t>": { return cs.LocalPlayer.TargetObject?.TargetObject; }
                case "<attack1>": { return GetSoftmarkHolder(AutomarkerSigns.SignEnum.Attack1);  }
                case "<attack2>": { return GetSoftmarkHolder(AutomarkerSigns.SignEnum.Attack2); }
                case "<attack3>": { return GetSoftmarkHolder(AutomarkerSigns.SignEnum.Attack3); }
                case "<attack4>": { return GetSoftmarkHolder(AutomarkerSigns.SignEnum.Attack4); }
                case "<attack5>": { return GetSoftmarkHolder(AutomarkerSigns.SignEnum.Attack5); }
                case "<bind1>": { return GetSoftmarkHolder(AutomarkerSigns.SignEnum.Bind1); }
                case "<bind2>": { return GetSoftmarkHolder(AutomarkerSigns.SignEnum.Bind2); }
                case "<bind3>": { return GetSoftmarkHolder(AutomarkerSigns.SignEnum.Bind3); }
                case "<stop1>":
                case "<ignore1>": { return GetSoftmarkHolder(AutomarkerSigns.SignEnum.Ignore1); }
                case "<stop2>":
                case "<ignore2>": { return GetSoftmarkHolder(AutomarkerSigns.SignEnum.Ignore2); }
                case "<circle>": { return GetSoftmarkHolder(AutomarkerSigns.SignEnum.Circle); }
                case "<square>": { return GetSoftmarkHolder(AutomarkerSigns.SignEnum.Square); }
                case "<plus>":
                case "<cross>": { return GetSoftmarkHolder(AutomarkerSigns.SignEnum.Plus); }
                case "<triangle>": { return GetSoftmarkHolder(AutomarkerSigns.SignEnum.Triangle); }
                case "<mo>":
                case "<mouse>": { return tm.MouseOverTarget; }
                case "<f>":
                case "<focus>": { return tm.FocusTarget; }
            }
            return null;
        }

        internal void PerformMarkingThroughCommand(string mark, string target)
        {
            AutomarkerSigns.SignEnum sign = AutomarkerSigns.SignEnum.None;
            switch (mark.ToLower())
            {
                case "attack1": sign = AutomarkerSigns.SignEnum.Attack1; break;
                case "attack2": sign = AutomarkerSigns.SignEnum.Attack2; break;
                case "attack3": sign = AutomarkerSigns.SignEnum.Attack3; break;
                case "attack4": sign = AutomarkerSigns.SignEnum.Attack4; break;
                case "attack5": sign = AutomarkerSigns.SignEnum.Attack5; break;
                case "bind1": sign = AutomarkerSigns.SignEnum.Bind1; break;
                case "bind2": sign = AutomarkerSigns.SignEnum.Bind2; break;
                case "bind3": sign = AutomarkerSigns.SignEnum.Bind3; break;
                case "stop1":
                case "stop2":
                case "ignore1": sign = AutomarkerSigns.SignEnum.Ignore1; break;
                case "ignore2": sign = AutomarkerSigns.SignEnum.Ignore2; break;
                case "attack": sign = AutomarkerSigns.SignEnum.AttackNext; break;
                case "bind": sign = AutomarkerSigns.SignEnum.BindNext; break;
                case "stop":
                case "ignore": sign = AutomarkerSigns.SignEnum.IgnoreNext; break;
                case "circle": sign = AutomarkerSigns.SignEnum.Circle; break;
                case "plus":
                case "cross": sign = AutomarkerSigns.SignEnum.Plus; break;
                case "square": sign = AutomarkerSigns.SignEnum.Square; break;
                case "triangle": sign = AutomarkerSigns.SignEnum.Triangle; break;
                case "off":
                case "clear":
                    IGameObject goc = ParsePlaceholder(target);
                    if (goc != null)
                    {
                        ClearMarkerOn(goc, false, true);
                    }
                    break;
            }
            if (sign == AutomarkerSigns.SignEnum.None)
            {
                return;
            }
            IGameObject go = ParsePlaceholder(target);
            if (go != null)
            {
                PerformMarking(0, go, sign, true);
            }            
        }

        internal unsafe Party GetPartyMembers()
        {
            Party pty = new Party();            
            Dictionary<string, int> idx = new Dictionary<string, int>();
            AgentHUD* ah = AgentHUD.Instance();            
            for (int i = 0; i < ah->PartyMemberCount; i++)
            {
                HudPartyMember pm = ah->PartyMembers[i];
                string temp = pm.Name.ToString();
                idx[temp] = pm.Index;
            }
            foreach (IPartyMember pm in pl)
            {
                string name = pm.Name.ToString();
                pty.Members.Add(new Party.PartyMember()
                {
                    Index = idx.ContainsKey(name) == true ? idx[name] + 1 : 0,
                    Name = pm.GameObject != null ? pm.GameObject.Name.ToString() : pm.Name.ToString(),
                    ObjectId = pm.GameObject != null ? pm.GameObject.GameObjectId : pm.ObjectId,
                    GameObject = pm.GameObject
                });
            }
            if (pty.Members.Count == 0)
            {                
                pty.Members.Add(new Party.PartyMember()
                {
                    Index = 1,
                    Name = cs.LocalPlayer.Name.ToString(),
                    ObjectId = cs.LocalPlayer.GameObjectId,
                    GameObject = cs.LocalPlayer
                });
            }
            return pty;
        }

        internal unsafe Alliance GetAllianceMembers()
        {
            Alliance al = new Alliance();
            return al;
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

        internal void ClearMarkerOn(string name, bool hard, bool soft)
        {
            ClearMarkerOn(GetActorByName(name), hard, soft);
        }

        internal void ClearMarkerOn(uint actorId, bool hard, bool soft)
        {
            ClearMarkerOn(GetActorById(actorId), hard, soft);
        }

        internal void ClearMarkerOn(IGameObject go, bool hard, bool soft)
        {
            if (go == null)
            {
                return;
            }
            if (soft == true)
            {
                foreach (KeyValuePair<AutomarkerSigns.SignEnum, ulong> kp in SoftMarkers)
                {
                    if (kp.Value == go.GameObjectId)
                    {
                        Log(LogLevelEnum.Debug, null, "Removing soft mark {0} from {1}", kp.Key, go);
                        if (cfg.DebugOnlyLogAutomarkers == false)
                        {
                            AddMarkerHistory(null, null, true, kp.Key);
                            SoftMarkers[kp.Key] = 0;
                        }
                    }
                }
            }
            if (hard == false || cfg.QuickToggleAutomarkers == false)
            {
                return;
            }
            bool markfunc = (StatusMarkingFuncAvailable == true && cfg.AutomarkerCommands == false);
            if (markfunc == true)
            {
                if (GetCurrentMarker(go.GameObjectId, out AutomarkerSigns.SignEnum marker) == true)
                {
                    if (marker != AutomarkerSigns.SignEnum.None)
                    {
                        Log(LogLevelEnum.Debug, null, "Using function pointer to remove mark on actor {0} by reassigning {1}", go, marker);
                        if (cfg.DebugOnlyLogAutomarkers == false)
                        {
                            DeferredInvoke di = new DeferredInvoke()
                            {
                                State = this,
                                Function = _markingFuncPtr,
                                Params = new object[] { _sigs["MarkingCtrl"], (byte)AutomarkerSigns.GetSignIndex(marker), (uint)go.GameObjectId }
                            };                            
                            QueueInvocation(di);
                        }
                    }
                    else
                    {
                        Log(LogLevelEnum.Debug, null, "Actor {0} is already unmarked", go);
                    }
                    return;
                }
                else
                {
                    if (_postCmdFuncptr != null)
                    {
                        Log(LogLevelEnum.Warning, null, "Couldn't determine marker on actor {0}, falling back to command", go);
                    }
                    else
                    {
                        Log(LogLevelEnum.Error, null, "Couldn't determine marker on actor {0}", go);
                    }
                }
            }
            if (_postCmdFuncptr != null)
            {
                int index = GetPartyMemberIndex(go.Name.ToString());
                if (index > 0)
                {
                    string cmd = "/mk off <" + index + ">";
                    Log(LogLevelEnum.Debug, null, "Using command to remove mark on actor {0} in party spot {1}", go, index);
                    if (cfg.DebugOnlyLogAutomarkers == false)
                    {
                        DeferredInvoke di = new DeferredInvoke()
                        {
                            State = this,
                            CommandText = cmd
                        };                        
                        QueueInvocation(di);
                    }
                    return;
                }
                else
                {
                    Log(LogLevelEnum.Warning, null, "Couldn't find actor {0} in party", go);
                }
            }
            Log(LogLevelEnum.Error, null, "Couldn't clear marker on actor {0}", go);
        }

        internal void PerformMarking(ulong run, string name, AutomarkerSigns.SignEnum sign, bool soft)
        {
            PerformMarking(run, GetActorByName(name), sign, soft);
        }

        internal void PerformMarking(ulong run, uint actorId, AutomarkerSigns.SignEnum sign, bool soft)
        {
            PerformMarking(run, GetActorById(actorId), sign, soft);
        }

        internal bool IsSoftmarkSet(AutomarkerSigns.SignEnum sign)
        {
            return ((SoftMarkers.ContainsKey(sign) == false || SoftMarkers[sign] == 0) == false);
        }

        internal IGameObject GetSoftmarkHolder(AutomarkerSigns.SignEnum sign)
        {
            if (SoftMarkers.TryGetValue(sign, out ulong actorId) == false)
            {
                return null;
            }
            return actorId > 0 ? GetActorById(actorId) : null;
        }

        internal void PerformMarking(ulong run, IGameObject go, AutomarkerSigns.SignEnum sign, bool soft)
        {
            if (go == null || (cfg.QuickToggleAutomarkers == false && soft == false) || (cfg.QuickToggleOverlays == false && soft == true))
            {
                return;
            }
            if (soft == true)
            {
                if (sign == AutomarkerSigns.SignEnum.AttackNext)
                {
                    sign = AutomarkerSigns.SignEnum.None;
                    if (IsSoftmarkSet(AutomarkerSigns.SignEnum.Attack1) == false) sign = AutomarkerSigns.SignEnum.Attack1;
                    else if (IsSoftmarkSet(AutomarkerSigns.SignEnum.Attack2) == false) sign = AutomarkerSigns.SignEnum.Attack2;
                    else if (IsSoftmarkSet(AutomarkerSigns.SignEnum.Attack3) == false) sign = AutomarkerSigns.SignEnum.Attack3;
                    else if (IsSoftmarkSet(AutomarkerSigns.SignEnum.Attack4) == false) sign = AutomarkerSigns.SignEnum.Attack4;
                    else if (IsSoftmarkSet(AutomarkerSigns.SignEnum.Attack5) == false) sign = AutomarkerSigns.SignEnum.Attack5;
                    else if (IsSoftmarkSet(AutomarkerSigns.SignEnum.Attack6) == false) sign = AutomarkerSigns.SignEnum.Attack6;
                    else if (IsSoftmarkSet(AutomarkerSigns.SignEnum.Attack7) == false) sign = AutomarkerSigns.SignEnum.Attack7;
                    else if (IsSoftmarkSet(AutomarkerSigns.SignEnum.Attack8) == false) sign = AutomarkerSigns.SignEnum.Attack8;
                }
                if (sign == AutomarkerSigns.SignEnum.BindNext)
                {
                    sign = AutomarkerSigns.SignEnum.None;
                    if (IsSoftmarkSet(AutomarkerSigns.SignEnum.Bind1) == false) sign = AutomarkerSigns.SignEnum.Bind1;
                    else if (IsSoftmarkSet(AutomarkerSigns.SignEnum.Bind2) == false) sign = AutomarkerSigns.SignEnum.Bind2;
                    else if (IsSoftmarkSet(AutomarkerSigns.SignEnum.Bind3) == false) sign = AutomarkerSigns.SignEnum.Bind3;
                }
                if (sign == AutomarkerSigns.SignEnum.IgnoreNext)
                {
                    sign = AutomarkerSigns.SignEnum.None;
                    if (IsSoftmarkSet(AutomarkerSigns.SignEnum.Ignore1) == false) sign = AutomarkerSigns.SignEnum.Ignore1;
                    else if (IsSoftmarkSet(AutomarkerSigns.SignEnum.Ignore2) == false) sign = AutomarkerSigns.SignEnum.Ignore2;
                }
                if (sign != AutomarkerSigns.SignEnum.None)
                {
                    foreach (KeyValuePair<AutomarkerSigns.SignEnum, ulong> kp in SoftMarkers)
                    {
                        if (kp.Key != sign && kp.Value == go.GameObjectId)
                        {
                            Log(LogLevelEnum.Debug, null, "Removing soft mark {0} from {1}", kp.Key, go);
                            if (cfg.DebugOnlyLogAutomarkers == false)
                            {
                                SoftMarkers[kp.Key] = 0;
                                AddMarkerHistory(null, null, true, kp.Key);
                            }
                        }
                    }
                    Log(LogLevelEnum.Debug, null, "Assigning soft mark {0} on actor {1}", sign, go);
                    cfg.AutomarkersServed++;
                    if (cfg.DebugOnlyLogAutomarkers == false)
                    {
                        SoftMarkers[sign] = go.GameObjectId;
                        AddMarkerHistory(null, go, true, sign);
                    }
                }
                return;
            }
            bool cleared = false;
            if (GetCurrentMarker(go.GameObjectId, out AutomarkerSigns.SignEnum marker) == false)
            {
                Log(LogLevelEnum.Warning, null, "Couldn't determine marker on actor {0}, clearing marker first", go);
                ClearMarkerOn(go, true, false);
                marker = AutomarkerSigns.SignEnum.None;
                cleared = true;
            }
            Log(LogLevelEnum.Debug, null, "Current marker on {0} is {1}, target is {2}", go, marker, sign);
            if (marker == sign)
            {
                return;
            }
            if (sign != AutomarkerSigns.SignEnum.AttackNext && sign != AutomarkerSigns.SignEnum.BindNext && sign != AutomarkerSigns.SignEnum.IgnoreNext)
            {
                bool markfunc = (StatusMarkingFuncAvailable == true && cfg.AutomarkerCommands == false);
                if (markfunc == true)
                {
                    Log(LogLevelEnum.Debug, null, "Using function pointer to assign mark {0} on actor {1}", sign, go);
                    cfg.AutomarkersServed++;
                    if (cfg.DebugOnlyLogAutomarkers == false)
                    {
                        _markersApplied = true;
                        DeferredInvoke di = new DeferredInvoke()
                        {
                            State = this,
                            Function = _markingFuncPtr,
                            Params = new object[] { _sigs["MarkingCtrl"], (byte)AutomarkerSigns.GetSignIndex(sign), (uint)go.GameObjectId },
                            FireAt = cleared == true ? DateTime.Now.AddMilliseconds(750) : DateTime.Now
                        };
                        QueueInvocation(di);
                    }
                    return;
                }
            }
            if (_postCmdFuncptr != null)
            {
                string cmd = "/mk ";
                switch (sign)
                {
                    case AutomarkerSigns.SignEnum.Attack1: cmd += "attack1"; break;
                    case AutomarkerSigns.SignEnum.Attack2: cmd += "attack2"; break;
                    case AutomarkerSigns.SignEnum.Attack3: cmd += "attack3"; break;
                    case AutomarkerSigns.SignEnum.Attack4: cmd += "attack4"; break;
                    case AutomarkerSigns.SignEnum.Attack5: cmd += "attack5"; break;
                    case AutomarkerSigns.SignEnum.Attack6: cmd += "attack6"; break;
                    case AutomarkerSigns.SignEnum.Attack7: cmd += "attack7"; break;
                    case AutomarkerSigns.SignEnum.Attack8: cmd += "attack8"; break;
                    case AutomarkerSigns.SignEnum.Bind1: cmd += "bind1"; break;
                    case AutomarkerSigns.SignEnum.Bind2: cmd += "bind2"; break;
                    case AutomarkerSigns.SignEnum.Bind3: cmd += "bind3"; break;
                    case AutomarkerSigns.SignEnum.Ignore1: cmd += "ignore1"; break;
                    case AutomarkerSigns.SignEnum.Ignore2: cmd += "ignore2"; break;
                    case AutomarkerSigns.SignEnum.Circle: cmd += "circle"; break;
                    case AutomarkerSigns.SignEnum.Plus: cmd += "cross"; break;
                    case AutomarkerSigns.SignEnum.Square: cmd += "square"; break;
                    case AutomarkerSigns.SignEnum.Triangle: cmd += "triangle"; break;
                    case AutomarkerSigns.SignEnum.AttackNext: cmd += "attack"; break;
                    case AutomarkerSigns.SignEnum.BindNext: cmd += "bind"; break;
                    case AutomarkerSigns.SignEnum.IgnoreNext: cmd += "ignore"; break;
                }
                int index = GetPartyMemberIndex(go.Name.ToString());
                if (index > 0)
                {
                    Log(LogLevelEnum.Debug, null, "Using command to mark actor {0} in party spot {1} with {2}", go, index, sign);
                    cmd += " <" + index + ">";
                    if (sign != AutomarkerSigns.SignEnum.None)
                    {
                        cfg.AutomarkersServed++;
                    }
                    if (cfg.DebugOnlyLogAutomarkers == false)
                    {
                        _markersApplied = true;
                        DeferredInvoke di = new DeferredInvoke()
                        {
                            State = this,
                            CommandText = cmd,
                            FireAt = cleared == true ? DateTime.Now.AddMilliseconds(750) : DateTime.Now
                        };
                        QueueInvocation(di);
                    }
                    return;
                }
                else
                {
                    Log(LogLevelEnum.Warning, null, "Couldn't find actor {0} in party", go);
                }
            }
            Log(LogLevelEnum.Error, null, "Couldn't mark actor {0} with {1}", go, sign);
        }

        internal unsafe bool GetCurrentMarker(ulong actorId, out AutomarkerSigns.SignEnum marker)
        {
            UIModule* ui = (UIModule*)gg.GetUIModule().Address;
            if (ui == null)
            {
                Log(LogLevelEnum.Warning, null, "Couldn't get UIModule");
                marker = AutomarkerSigns.SignEnum.None;
                return false;
            }
            UI3DModule* ui3d = ui->GetUI3DModule();
            if (ui3d == null)
            {
                Log(LogLevelEnum.Warning, null, "Couldn't get UI3DModule");
                marker = AutomarkerSigns.SignEnum.None;
                return false;
            }
            AddonNamePlate* np = (AddonNamePlate*)gg.GetAddonByName("NamePlate", 1).Address;
            if (np == null)
            {
                Log(LogLevelEnum.Warning, null, "Couldn't get AddonNamePlate");
                marker = AutomarkerSigns.SignEnum.None;
                return false;
            }
            try
            {
                for (int i = 0; i < ui3d->NamePlateObjectInfoCount; i++)
                {
                    var o = ui3d->NamePlateObjectInfoPointers[i];
                    if (o == null || o.Value->GameObject == null || o.Value->GameObject->GetGameObjectId() != actorId)
                    {
                        continue;
                    }
                    AddonNamePlate.NamePlateObject npo = np->NamePlateObjectArray[o.Value->NamePlateIndex];
                    foreach (AtkImageNode* ain in new AtkImageNode*[] { npo.MarkerIcon })
                    {
                        if (ain == null)
                        {
                            continue;
                        }
                        AtkUldPartsList* upl = ain->PartsList;
                        if (upl == null)
                        {
                            continue;
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
                            switch (p->UldAsset->AtkTexture.Resource->IconId)
                            {
                                case 61301: marker = AutomarkerSigns.SignEnum.Attack1; return true;
                                case 61302: marker = AutomarkerSigns.SignEnum.Attack2; return true;
                                case 61303: marker = AutomarkerSigns.SignEnum.Attack3; return true;
                                case 61304: marker = AutomarkerSigns.SignEnum.Attack4; return true;
                                case 61305: marker = AutomarkerSigns.SignEnum.Attack5; return true;
                                case 61306: marker = AutomarkerSigns.SignEnum.Attack6; return true;
                                case 61307: marker = AutomarkerSigns.SignEnum.Attack7; return true;
                                case 61308: marker = AutomarkerSigns.SignEnum.Attack8; return true;
                                case 61311: marker = AutomarkerSigns.SignEnum.Bind1; return true;
                                case 61312: marker = AutomarkerSigns.SignEnum.Bind2; return true;
                                case 61313: marker = AutomarkerSigns.SignEnum.Bind3; return true;
                                case 61321: marker = AutomarkerSigns.SignEnum.Ignore1; return true;
                                case 61322: marker = AutomarkerSigns.SignEnum.Ignore2; return true;
                                case 61331: marker = AutomarkerSigns.SignEnum.Square; return true;
                                case 61332: marker = AutomarkerSigns.SignEnum.Circle; return true;
                                case 61333: marker = AutomarkerSigns.SignEnum.Plus; return true;
                                case 61334: marker = AutomarkerSigns.SignEnum.Triangle; return true;
                            }
                        }
                    }
                    marker = AutomarkerSigns.SignEnum.None;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log(LogLevelEnum.Error, ex, "NamePlateObject failed");
            }
            if (_sigs.TryGetValue("MarkingCtrl", out nint addr) == true)
            {
                foreach (AutomarkerSigns.SignEnum val in Enum.GetValues(typeof(AutomarkerSigns.SignEnum)))
                {
                    if (val == AutomarkerSigns.SignEnum.AttackNext || val == AutomarkerSigns.SignEnum.BindNext || val == AutomarkerSigns.SignEnum.IgnoreNext)
                    {
                        continue;
                    }
                    nint offset = 8 * (1 + AutomarkerSigns.GetSignIndex(val));
                    int temp = Marshal.ReadInt32(addr + offset);
                    if (temp == (int)actorId)
                    {
                        marker = val;
                        return true;
                    }
                }
            }
            marker = AutomarkerSigns.SignEnum.None;
            return true;
        }

        internal void PostCommand(string cmd)
        {
            if (_postCmdFuncptr != null)
            {
                DeferredInvoke di = new DeferredInvoke()
                {
                    State = this,
                    CommandText = cmd
                };
                QueueInvocation(di);
            }
        }

        internal char MarkingHook(nint ctrl, byte markId, uint actorId)
        {
            if (_sigs.ContainsKey("MarkingCtrl") == false)
            {
                SetMarkingController(ctrl);
            }
            return _markingFuncHook.Original(ctrl, markId, actorId);
        }

        internal void SetMarkingController(nint addr)
        {
            _sigs["MarkingCtrl"] = addr;
            Log(LogLevelEnum.Debug, null, "Marking controller found at {0}", addr.ToString("X"));
            if (_markingFuncPtr != null)
            {
                if (_markingFuncHook != null)
                {
                    _markingFuncPtr = new MarkingFunctionDelegate(MarkingHook);
                    Log(LogLevelEnum.Info, null, "Marking by function pointer available (hooked)");
                }
                else
                {
                    Log(LogLevelEnum.Info, null, "Marking by function pointer available");
                }
                StatusMarkingFuncAvailable = true;                
            }
        }

        internal void GetSignatures()
        {
            nint addr1, addr2;
            addr1 = _sig.ScanText("48 89 5C 24 10 48 89 6C 24 18 57 48 83 EC 20 8D 42");
            if (addr1 != IntPtr.Zero)
            {
                _sigs["MarkingFunc"] = addr1;
                Log(LogLevelEnum.Debug, null, "Marking function found at {0}", addr1.ToString("X"));
                _markingFuncPtr = Marshal.GetDelegateForFunctionPointer<MarkingFunctionDelegate>(addr1);
            }
            else
            {
                Log(LogLevelEnum.Warning, null, "Marking function not found");
            }
            addr2 = _sig.OldGetStaticAddressFromSig("48 8B D0 48 8D 0D ? ? ? ? E8 ? ? ? ? 3B F8 48 8D 0D ? ? ? ? 8B D7");            
            if (addr2 != IntPtr.Zero)
            {
                SetMarkingController(addr2);                
            }
            else
            {
                Log(LogLevelEnum.Warning, null, "Marking controller not found");
            }
            if (addr1 == IntPtr.Zero || addr2 == IntPtr.Zero)
            {                
                Log(LogLevelEnum.Warning, null, "Marking by function pointer not available, falling back to command injection");
            }            
            addr1 = _sig.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 48 8B F2 48 8B F9 45 84 C9");
            if (addr1 != IntPtr.Zero)
            {
                _postCmdFuncptr = Marshal.GetDelegateForFunctionPointer<PostCommandDelegate>(addr1);
                Log(LogLevelEnum.Debug, null, "Command post function found at {0}", addr1.ToString("X"));
                StatusPostCommandAvailable = true;
            }
            else
            {
                Log(LogLevelEnum.Warning, null, "Command post function not found");
            }
        }

    }

}
