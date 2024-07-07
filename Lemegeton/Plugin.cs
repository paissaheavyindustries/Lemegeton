using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Gui;
using Dalamud.Game.ClientState;
using Dalamud.Game.Network;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Dalamud.Game.Command;
using Lemegeton.Core;
using System.Reflection;
using Dalamud.Data;
using ImGuiScene;
using Dalamud.Game.ClientState.Objects.Types;
using System.Xml;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;
using Dalamud.Game;
using static Lemegeton.Core.State;
using System.Diagnostics;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Party;
using FFXIVClientStructs.FFXIV.Common.Math;
using System.Text;
using System.ComponentModel;
using System.Globalization;
using Dalamud.Interface;
using System.IO;
using System.Xml.Serialization;
using Dalamud.Configuration;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using static Lemegeton.Core.AutomarkerPrio;
using System.Net.Http;
using Lumina.Excel.GeneratedSheets;
using FFXIVClientStructs.FFXIV.Client.UI;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;

namespace Lemegeton
{

    public sealed class Plugin : IDalamudPlugin
    {

#if !SANS_GOETIA
        public string Name => "Lemegeton Goetia";
#else
        public string Name => "Lemegeton";
#endif
        public string Version = "1.0.3.8";

        internal class Downloadable
        {

            public string DownloadUrl { get; set; }
            public string LocalFile { get; set; }
            public object Object { get; set; }
            public DownloadableCompletionDelegate OnSuccess { get; set; } = null;
            public DownloadableCompletionDelegate OnFailure { get; set; } = null;

        }

        internal class ActionTypeItem
        {

            public Core.Action Instance;

        }

        internal delegate void DownloadableCompletionDelegate(Downloadable d);
        private List<Tuple<Core.Language, string>> _fontBuilderQueue = new List<Tuple<Core.Language, string>>();
        private List<Notification> Notifications = new List<Notification>();
        private List<Notification> NotificationsQueue = new List<Notification>();
        private List<ActionTypeItem> ActionTypes = new List<ActionTypeItem>();
        internal UserInterface _ui = new UserInterface();
        private State _state = new State();
        private Thread _mainThread = null;
        private ManualResetEvent _stopEvent = new ManualResetEvent(false);
        private AutoResetEvent _retryEvent = new AutoResetEvent(false);
        private AutoResetEvent _downloadRequestEvent = new AutoResetEvent(false);
        private float _adjusterX = 0.0f;
        private float _adjusterXtl = 0.0f;
        private float _adjusterYtl = 0.0f;
        private float _adjusterYtl2 = 0.0f;
        private float _adjusterYtl3 = 0.0f;
        private DateTime _loaded = DateTime.Now;
        private bool _aboutProg = false;
        private bool _softMarkerPreview = false;
        private bool _timelineSelectorOpened = false;
        private DateTime _aboutOpened;
        private Dictionary<Delegate, string[]> _delDebugInput = new Dictionary<Delegate, string[]>();
        private Queue<Downloadable> _downloadQueue = new Queue<Downloadable>();
        private bool _downloadPending = false;
        private string _downloadFilename = "";
        private string _timelineActionFilter = "";
        private string _timelineSelectorZoneFilter = "";
        private string _timelineSelectorFileFilter = "";
        private bool _newNotifications = false;
        private int _ttsCounter = 1;

        private List<Timeline> _lastEncounters = new List<Timeline>();

        private bool _movingShortcut = false;
        private Vector2 _movingMouse;

        private Timeline _selectedTimeline = null;
        private Timeline SelectedTimeline
        {
            get
            {
                return _selectedTimeline;
            }
            set
            {
                if (_selectedTimeline != value)
                {
                    _selectedTimeline = value;
                    _selectedProfile = _selectedTimeline != null ? _selectedTimeline.DefaultProfile : null;
                    SelectedEncounter = _selectedTimeline != null && _selectedTimeline.Encounters.Count > 0 ? _selectedTimeline.Encounters[0] : null;
                }
            }
        }

        private Timeline.Profile _selectedProfile = null;
        private Timeline.Profile SelectedProfile
        {
            get
            {
                return _selectedProfile;
            }
            set
            {
                if (_selectedProfile != value)
                {
                    _selectedProfile = value;
                    if (_selectedProfile == null)
                    {
                        SelectedEntry = null;
                    }
                }
            }
        }

        private Timeline.Encounter _selectedEncounter = null;
        private Timeline.Encounter SelectedEncounter
        {
            get
            {
                return _selectedEncounter;
            }
            set
            {
                if (_selectedEncounter != value)
                {
                    _selectedEncounter = value;
                    SelectedEntry = _selectedEncounter != null && _selectedEncounter.Entries.Count > 0 ? _selectedEncounter.Entries[0] : null;
                }
            }
        }

        private Timeline.Entry _selectedEntry = null;
        private Timeline.Entry SelectedEntry
        {
            get
            {
                return _selectedEntry;
            }
            set
            {
                if (_selectedEntry != value)
                {
                    _selectedEntry = value;                    
                    if (_selectedEntry != null && SelectedProfile != null)
                    {
                        Timeline.Entry.ProfileSettings pro = SelectedProfile.GetSettingsForEntry(_selectedEntry);
                        if (pro.Reactions.Count > 0)
                        {
                            SelectedReaction = pro.Reactions[0];
                        }
                        else
                        {
                            SelectedReaction = null;
                        }
                    }
                    else
                    {
                        SelectedReaction = null;
                    }
                }
            }
        }

        private Timeline.Reaction _selectedReaction = null;
        private Timeline.Reaction SelectedReaction
        {
            get
            {
                return _selectedReaction;
            }
            set
            {
                if (_selectedReaction != value)
                {
                    _selectedReaction = value;
                    if (_selectedReaction != null && _selectedReaction.ActionList.Count > 0)
                    {
                        _selectedAction = _selectedReaction.ActionList[0];
                    }
                    else
                    {
                        _selectedAction = null;
                    }
                }
            }
        }

        private Core.Action _selectedAction = null;

        private Dictionary<string, ImFontPtr> _fonts = new Dictionary<string, ImFontPtr>();
        public List<Core.ContentCategory> _content = new List<Core.ContentCategory>();
        public List<Core.ContentCategory> _other = new List<Core.ContentCategory>();
        private float _lemmyShortcutOpacity = 0.5f;
        private bool _lemmyShortcutJustPopped = true;
        private bool _drawingCallback = false;
        private string _configSnapshot = "";
        private DateTime _lemmyShortcutPopped;
        private Rectangle _lastSeen = new Rectangle();
        private List<string> _contribs = new List<string>();
        private string _newPresetName = "";
        private string _newProfileName = "";
        private string _newReactionName = "";
        private bool _renderTimelineOverlay = false;
        private bool _timelineOverlayConfig = false;
        private bool _renderNotificationOverlay = false;
        private bool _notificationOverlayConfig = false;

        private string[] _aboutScroller = new string[] {
#if !SANS_GOETIA
            "LEMEGETON GOETIA",
#else
            "LEMEGETON",
#endif
            "a Dalamud plogon",
            "and a FFXIV trainer / cracktro",
            "by Paissa Heavy Industries",
            "stay out of crime",
            "don't be a statistic",
            "and pet all cats",
            "all of them",
            "automarkers served to date: $auto",
        };

        public Plugin(
            IDalamudPluginInterface pluginInterface,
            IGameNetwork gameNetwork,
            IChatGui chatGui,
            ICommandManager commandManager,
            IObjectTable objectTable,
            IGameGui gameGui,
            IClientState clientState,
            IDataManager dataManager,
            ITextureProvider textureProvider,
            ICondition condition,
            IFramework framework,
            ISigScanner sigScanner,
            IPartyList partylist,
            ITargetManager targetManager,
            IPluginLog log,
            IGameInteropProvider interop
        )
        {
            _state = new State()
            {
                pi = pluginInterface,
                gn = gameNetwork,
                cg = chatGui,
                cm = commandManager,
                ot = objectTable,
                gg = gameGui,
                cs = clientState,
                dm = dataManager,
                tp = textureProvider,
                cd = condition,
                fw = framework,
                ss = sigScanner,
                pl = partylist,
                tm = targetManager,
                lo = log,
                io = interop,
                plug = this
            };
            _ui._state = _state;
            Log(LogLevelEnum.Info, "This is Lemegeton {0}", Version);
            LoadConfig();
            ApplyVersionChanges();
            I18n.OnFontDownload += I18n_OnFontDownload;
            InitializeLanguage();
            InitializeContent();            
            _state.Initialize();
            ApplyConfigToContent();
            ChangeLanguage(_state.cfg.Language);
            InitializeActionTypes();
            _ui.LoadTextures();
            //_state.pi.UiBuilder.FontAtlas.BuildFonts += UiBuilder_BuildFonts; todo
            _mainThread = new Thread(new ParameterizedThreadStart(MainThreadProc));
            _mainThread.Name = "Lemegeton main thread";
            _mainThread.Start(this);
            _state.cs.Login += Cs_Login;
            _state.cs.Logout += Cs_Logout;
            if (_state.cs.IsLoggedIn == true)
            {
                Cs_Login();
            }            
        }

        private void InitializeActionTypes()
        {
            foreach (Type type in Assembly.GetAssembly(typeof(Core.Action)).GetTypes().Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(Core.Action))))
            {
                try
                {
                    ActionTypeItem ati = new ActionTypeItem();
                    ati.Instance = (Core.Action)Activator.CreateInstance(type);
                    ActionTypes.Add(ati);
                }
                catch (Exception ex)
                {
                    Log(LogLevelEnum.Error, "Couldn't initialize Action type {0} due to exception: {1} at {2}", type.Name, ex.Message, ex.StackTrace);
                }
            }
            ActionTypes.Sort((a, b) => a.Instance.Describe().CompareTo(b.Instance.Describe()));
        }

        private void ApplyVersionChanges()
        {
            if (String.Compare(_state.cfg.PlogonVersion, Version) != 0)
            {
                Log(LogLevelEnum.Info, "Plugin version changed from {0} to {1}", _state.cfg.PlogonVersion, Version);
                BackupConfig();
                if (new Version(_state.cfg.PlogonVersion) < new Version("1.0.1.0"))
                {
                    // 1.0.1.0 - change tasks to always queue on framework thread
                    Log(LogLevelEnum.Info, "Applying config fixes for version 1.0.1.0");
                    _state.cfg.QueueFramework = true;
                }
                _state.cfg.PlogonVersion = Version;
            }
        }

        private void LoadFontFromDisk(Downloadable d)
        {
            Core.Language l = (Core.Language)d.Object;
            lock (_fontBuilderQueue)
            {
                _fontBuilderQueue.Add(new Tuple<Core.Language, string>(l, d.LocalFile));
                //_state.pi.UiBuilder.RebuildFonts(); todo
            }
        }

        private void I18n_OnFontDownload(Core.Language lang)
        {
            Log(LogLevelEnum.Debug, "Font download requested for {0} from {1}", lang.LanguageName, lang.FontDownload);
            Downloadable d = new Downloadable();
            d.Object = lang;
            d.DownloadUrl = lang.FontDownload;
            d.OnSuccess = LoadFontFromDisk;
            QueueForDownload(d);
        }

        private unsafe void UiBuilder_BuildFonts()
        {
            List<Tuple<Core.Language, string>> fonts = new List<Tuple<Core.Language, string>>();
            lock (_fontBuilderQueue)
            {
                fonts.AddRange(_fontBuilderQueue);
                _fontBuilderQueue.Clear();
            }
            foreach (Tuple<Core.Language, string> tp in fonts)
            {
                try
                {
                    nint range = 0;
                    switch (tp.Item1.GlyphRange)
                    {
                        case Core.Language.GlyphRangeEnum.ChineseSimplifiedCommon:
                            range = ImGui.GetIO().Fonts.GetGlyphRangesChineseSimplifiedCommon();
                            break;
                        case Core.Language.GlyphRangeEnum.ChineseFull:
                            range = ImGui.GetIO().Fonts.GetGlyphRangesChineseFull();
                            break;
                    }
                    ImFontPtr font = ImGui.GetIO().Fonts.AddFontFromFileTTF(
                        tp.Item2,
                        18.0f,
                        null,
                        range
                    );
                    Log(LogLevelEnum.Debug, "Font loaded from {0}, setting font to language {1}", tp.Item2, tp.Item1.LanguageName);
                    tp.Item1.Font = font;
                }
                catch (Exception ex)
                {
                    GenericExceptionHandler(ex);
                }
            }
        }

        private void Cs_Login()
        {
            lock (this)
            {
                if (_drawingCallback == false)
                {
                    _state.pi.UiBuilder.Draw += DrawUI;
                    _state.pi.UiBuilder.Draw += _ui._dialogManager.Draw;
                    _drawingCallback = true;
                }
            }
        }

        private void Cs_Logout()
        {
            lock (this)
            {
                if (_drawingCallback == true)
                {
                    _state.pi.UiBuilder.Draw -= _ui._dialogManager.Draw;
                    _state.pi.UiBuilder.Draw -= DrawUI;
                    _drawingCallback = false;
                }
            }
            SaveConfig();
        }

        public void Dispose()
        {
            if (_state._markingFuncHook != null)
            {
                _state._markingFuncHook.Disable();
                _state._markingFuncHook.Dispose();
                _state._markingFuncHook = null;
            }
            I18n.OnFontDownload -= I18n_OnFontDownload;
            _state.cs.Logout -= Cs_Logout;
            _state.cs.Login -= Cs_Login;
            Cs_Logout();
            _state.Uninitialize();
            _stopEvent.Set();
            //_state.pi.UiBuilder.BuildFonts -= UiBuilder_BuildFonts; todo            
            SaveConfig();
            if (_state.InvoqThreadNew != null)
            {
                _state.InvoqThreadNew.Dispose();
            }
            _mainThread.Join(1000);
            _downloadRequestEvent.Dispose();
            _stopEvent.Dispose();
            _retryEvent.Dispose();
        }

        internal void ChangeLanguage(string language)
        {
            Log(State.LogLevelEnum.Info, "Changing language to {0}", language);
            I18n.ChangeLanguage(language);
            Log(State.LogLevelEnum.Info, "Language changed to {0}", I18n.CurrentLanguage.LanguageName);
        }

        internal void GenericExceptionHandler(Exception ex)
        {
            _state.Log(State.LogLevelEnum.Error, ex, ex.Message);
        }

        internal void Log(State.LogLevelEnum level, string message, params object[] args)
        {
            _state.Log(level, null, message, args);
        }

        private void QueueForDownload(Downloadable d)
        {
            lock (_downloadQueue)
            {
                _downloadQueue.Enqueue(d);
                _downloadRequestEvent.Set();
            }
        }

        private void AddPropertiesToNode(XmlElement n, object o, List<Tuple<PropertyInfo, int>> props)
        {
            foreach (Tuple<PropertyInfo, int> prop in props)
            {
                PropertyInfo pi = prop.Item1;
                object pio = pi.GetValue(o);
                string val = pio != null ? pio.ToString() : "";
                if (pi.PropertyType == typeof(Vector2))
                {
                    Vector2 v = (Vector2)pi.GetValue(o);
                    val = String.Format("{0};{1}", v.X, v.Y);
                }
                else if (pi.PropertyType == typeof(Vector3))
                {
                    Vector3 v = (Vector3)pi.GetValue(o);
                    val = String.Format("{0};{1};{2}", v.X, v.Y, v.Z);
                }
                else if (pi.PropertyType == typeof(Vector4))
                {
                    Vector4 v = (Vector4)pi.GetValue(o);
                    val = String.Format("{0};{1};{2};{3}", v.X, v.Y, v.Z, v.W);
                }
                else if (pi.PropertyType == typeof(AutomarkerSigns))
                {
                    AutomarkerSigns v = (AutomarkerSigns)pi.GetValue(o);
                    val = v.Serialize();
                }
                else if (pi.PropertyType == typeof(AutomarkerPrio))
                {
                    AutomarkerPrio v = (AutomarkerPrio)pi.GetValue(o);
                    val = v.Serialize();
                }
                else if (pi.PropertyType == typeof(AutomarkerTiming))
                {
                    AutomarkerTiming v = (AutomarkerTiming)pi.GetValue(o);
                    val = v.Serialize();
                }
                else if (pi.PropertyType == typeof(Percentage))
                {
                    Percentage v = (Percentage)pi.GetValue(o);
                    val = v.Serialize();
                }
                else if (pi.PropertyType == typeof(FoodSelector))
                {
                    FoodSelector v = (FoodSelector)pi.GetValue(o);
                    val = v.Serialize();
                }
                else if (pi.PropertyType == typeof(System.Action))
                {
                    continue;
                }
                else if (pi.PropertyType.IsSubclassOf(typeof(CustomPropertyInterface)))
                {
                    CustomPropertyInterface v = (CustomPropertyInterface)pi.GetValue(o);
                    val = v.Serialize();
                }
                else
                {
                    object op = pi.GetValue(o);
                    val = op != null ? op.ToString() : "";
                }
                n.SetAttribute(pi.Name, val);
            }
        }

        private XmlNode SerializeContentCategory(XmlDocument doc, Core.ContentCategory cc)
        {
            XmlNode n = doc.CreateElement(cc.GetType().Name);
            {
                List<Tuple<PropertyInfo, int>> props = GetConfigurableProperties(cc, false, out bool foo);
                AddPropertiesToNode((XmlElement)n, cc, props);
            }
            if (cc.Subcategories.Count > 0)
            {
                XmlNode ns = doc.CreateElement("Subcategories");
                n.AppendChild(ns);
                foreach (KeyValuePair<string, Core.ContentCategory> subcats in cc.Subcategories)
                {
                    ns.AppendChild(SerializeContentCategory(doc, subcats.Value));
                }
            }
            foreach (KeyValuePair<string, Core.Content> content in cc.ContentItems)
            {
                XmlNode nc = doc.CreateElement(content.Key);
                n.AppendChild(nc);
                {
                    List<Tuple<PropertyInfo, int>> props = GetConfigurableProperties(content.Value, false, out bool foo);
                    AddPropertiesToNode((XmlElement)nc, content.Value, props);
                }
                foreach (KeyValuePair<string, Core.ContentItem> contentItem in content.Value.Items)
                {
                    XmlNode nci = doc.CreateElement(contentItem.Key);
                    nc.AppendChild(nci);
                    {
                        List<Tuple<PropertyInfo, int>> props = GetConfigurableProperties(contentItem.Value, false, out bool foo);
                        AddPropertiesToNode((XmlElement)nci, contentItem.Value, props);
                    }
                }
            }
            return n;
        }

        private void DeserializeAttributesToObject(XmlNode n, ContentModule cm)
        {
            List<Tuple<PropertyInfo, int>> props = GetConfigurableProperties(cm, false, out bool foo);
            foreach (XmlAttribute attr in n.Attributes)
            {
                var ix = (from cx in props where String.Compare(cx.Item1.Name, attr.Name) == 0 select cx).FirstOrDefault();
                if (ix != null)
                {
                    PropertyInfo pi = ix.Item1;
                    MethodInfo mi = pi.GetSetMethod();
                    if (mi == null || mi.IsPrivate == true)
                    {
                        continue;
                    }
                    if (pi.PropertyType == typeof(Vector2))
                    {
                        Vector2 v = new Vector2();
                        string[] cmps = attr.Value.Split(";");
                        {
                            if (float.TryParse(cmps[0], out float temp) == true)
                            {
                                v.X = temp;
                            }
                        }
                        {
                            if (float.TryParse(cmps[1], out float temp) == true)
                            {
                                v.Y = temp;
                            }
                        }
                        pi.SetValue(cm, v);
                    }
                    else if (pi.PropertyType == typeof(Vector3))
                    {
                        string[] cmps = attr.Value.Split(";");
                        Vector3 v = new Vector3();
                        {
                            if (float.TryParse(cmps[0], out float temp) == true)
                            {
                                v.X = temp;
                            }
                        }
                        {
                            if (float.TryParse(cmps[1], out float temp) == true)
                            {
                                v.Y = temp;
                            }
                        }
                        {
                            if (float.TryParse(cmps[2], out float temp) == true)
                            {
                                v.Z = temp;
                            }
                        }
                        pi.SetValue(cm, v);
                    }
                    else if (pi.PropertyType == typeof(Vector4))
                    {
                        string[] cmps = attr.Value.Split(";");
                        Vector4 v = new Vector4();
                        {
                            if (float.TryParse(cmps[0], out float temp) == true)
                            {
                                v.X = temp;
                            }
                        }
                        {
                            if (float.TryParse(cmps[1], out float temp) == true)
                            {
                                v.Y = temp;
                            }
                        }
                        {
                            if (float.TryParse(cmps[2], out float temp) == true)
                            {
                                v.Z = temp;
                            }
                        }
                        {
                            if (float.TryParse(cmps[3], out float temp) == true)
                            {
                                v.W = temp;
                            }
                        }
                        pi.SetValue(cm, v);
                    }
                    else if (pi.PropertyType == typeof(AutomarkerSigns))
                    {
                        AutomarkerSigns v = (AutomarkerSigns)pi.GetValue(cm);
                        v.Deserialize(attr.Value);
                    }
                    else if (pi.PropertyType == typeof(AutomarkerPrio))
                    {
                        AutomarkerPrio v = (AutomarkerPrio)pi.GetValue(cm);
                        v.Deserialize(attr.Value);
                    }
                    else if (pi.PropertyType == typeof(FoodSelector))
                    {
                        FoodSelector v = (FoodSelector)pi.GetValue(cm);
                        v.Deserialize(attr.Value);
                    }
                    else if (pi.PropertyType == typeof(AutomarkerTiming))
                    {
                        AutomarkerTiming v = (AutomarkerTiming)pi.GetValue(cm);
                        v.Deserialize(attr.Value);
                    }
                    else if (pi.PropertyType == typeof(Percentage))
                    {
                        Percentage v = (Percentage)pi.GetValue(cm);
                        v.Deserialize(attr.Value);
                    }
                    else if (pi.PropertyType == typeof(System.Action))
                    {
                    }
                    else if (pi.PropertyType.IsSubclassOf(typeof(CustomPropertyInterface)))
                    {
                        CustomPropertyInterface v = (CustomPropertyInterface)pi.GetValue(cm);
                        v.Deserialize(attr.Value);
                    }
                    else
                    {
                        object val = Convert.ChangeType(attr.Value, pi.PropertyType);
                        pi.SetValue(cm, val);
                    }
                }
            }
        }

        internal void AddNotification(Notification n)
        {
            lock (NotificationsQueue)
            {
                NotificationsQueue.Add(n);
                _newNotifications = true;
            }
        }

        internal void AddNotifications()
        {
            if (_newNotifications == true)
            {
                lock (NotificationsQueue)
                {
                    Notifications.AddRange(NotificationsQueue);
                    NotificationsQueue.Clear();
                    _newNotifications = false;
                }
            }
            List<Notification> delNotif = new List<Notification>();
            foreach (Notification n in Notifications)
            {
                if (DateTime.Now > n.SpawnTime.AddSeconds(n.TTL))
                {
                    delNotif.Add(n);
                    continue;
                }
            }
            foreach (Notification n in delNotif)
            {
                Notifications.Remove(n);
            }
        }

        internal static string Base64Encode(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(bytes);
        }

        internal static string Base64Decode(string text)
        {
            var bytes = Convert.FromBase64String(text);
            return Encoding.UTF8.GetString(bytes);
        }

        private void DeserializeContentCategory(XmlNode root, IEnumerable<Core.ContentCategory> cats)
        {
            foreach (XmlNode ncc in root.ChildNodes)
            {
                string namecc = ncc.Name;
                Core.ContentCategory cc = (from cx in cats where String.Compare(cx.GetType().Name, namecc) == 0 select cx).FirstOrDefault();
                if (cc != null)
                {
                    if (cc.Subcategories.Count > 0)
                    {
                        XmlNode nccs = ncc.SelectSingleNode("./Subcategories");
                        if (nccs != null)
                        {
                            DeserializeContentCategory(nccs, cc.Subcategories.Values);
                        }
                    }
                    DeserializeAttributesToObject(ncc, cc);
                    foreach (XmlNode nc in ncc.ChildNodes)
                    {
                        if (cc.ContentItems.ContainsKey(nc.Name) == true)
                        {
                            Core.Content c = cc.ContentItems[nc.Name];
                            DeserializeAttributesToObject(nc, c);
                            foreach (XmlNode nci in nc.ChildNodes)
                            {
                                if (c.Items.ContainsKey(nci.Name) == true)
                                {
                                    Core.ContentItem ci = c.Items[nci.Name];
                                    DeserializeAttributesToObject(nci, ci);
                                }
                            }
                        }
                    }
                }
            }
        }

        internal void DeserializeTimelineOverrides(string data)
        {
            if (data == null || data.Length == 0)
            {
                return;
            }
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(data);
            XmlNode root = doc.SelectSingleNode("/Lemegeton/TimelineOverrides");
            if (root == null)
            {
                return;
            }
            foreach (XmlNode ncc in root.ChildNodes)
            {
                try
                {
                    ushort territory = ushort.Parse(ncc.Attributes["Territory"].Value);
                    lock (_state.TimelineOverrides)
                    {
                        _state.TimelineOverrides[territory] = ncc.Attributes["Filename"].Value;
                    }
                }
                catch (Exception ex)
                {
                    GenericExceptionHandler(ex);
                }
            }
        }

        private void DeserializeProperties(string data)
        {
            if (data == null || data.Length == 0)
            {
                return;
            }
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(data);
            XmlNode root = doc.SelectSingleNode("/Lemegeton");
            foreach (XmlNode cc in root.ChildNodes)
            {
                if (cc.Name == "Content")
                {
                    DeserializeContentCategory(cc, _content);
                }
                else if (cc.Name == "Other")
                {
                    DeserializeContentCategory(cc, _other);
                }
                else if (cc.Name == "TimelineProfiles")
                {
                    foreach (XmlNode ncc in cc.ChildNodes)
                    {
                        ushort territory = ushort.Parse(ncc.Attributes["Territory"].Value);
                        lock (_state.AllTimelines)
                        {
                            if (_state.AllTimelines.TryGetValue(territory, out Timeline tl) == true)
                            {
                                string xml = Base64Decode(ncc.Attributes["Data"].Value);
                                Timeline.Profile pro = XmlSerializer<Timeline.Profile>.Deserialize(xml);
                                if (pro.Default == true)
                                {
                                    tl.DefaultProfile = pro;
                                }
                                else
                                {
                                    tl.AddProfile(pro, _state.cs.LocalPlayer != null ? _state.cs.LocalPlayer.ClassJob.Id : 0);
                                }
                            }
                        }
                    }
                }
            }
        }

        private string SerializeProperties()
        {
            XmlDocument doc = new XmlDocument();
            XmlNode root = doc.CreateElement("Lemegeton");
            doc.AppendChild(root);
            {
                XmlNode c = doc.CreateElement("Content");
                root.AppendChild(c);
                foreach (Core.ContentCategory cc in _content)
                {
                    c.AppendChild(SerializeContentCategory(doc, cc));
                }
            }
            {
                XmlNode c = doc.CreateElement("Other");
                root.AppendChild(c);
                foreach (Core.ContentCategory cc in _other)
                {
                    c.AppendChild(SerializeContentCategory(doc, cc));
                }
            }
            {
                XmlNode c = doc.CreateElement("TimelineProfiles");
                root.AppendChild(c);
                lock (_state.AllTimelines)
                {
                    foreach (KeyValuePair<ushort, Timeline> kp in _state.AllTimelines)
                    {
                        XmlNode cc = doc.CreateElement("Profile");
                        c.AppendChild(cc);
                        XmlAttribute a = doc.CreateAttribute("Territory");
                        a.Value = kp.Key.ToString();
                        cc.Attributes.Append(a);
                        a = doc.CreateAttribute("Data");
                        a.Value = Base64Encode(XmlSerializer<Timeline.Profile>.Serialize(kp.Value.DefaultProfile));
                        cc.Attributes.Append(a);
                        foreach (Timeline.Profile pro in kp.Value.Profiles)
                        {
                            cc = doc.CreateElement("Profile");
                            c.AppendChild(cc);
                            a = doc.CreateAttribute("Territory");
                            a.Value = kp.Key.ToString();
                            cc.Attributes.Append(a);
                            a = doc.CreateAttribute("Data");
                            a.Value = Base64Encode(XmlSerializer<Timeline.Profile>.Serialize(pro));
                            cc.Attributes.Append(a);
                        }
                    }
                }
            }
            {
                XmlNode c = doc.CreateElement("TimelineOverrides");
                root.AppendChild(c);
                lock (_state.TimelineOverrides)
                {
                    foreach (KeyValuePair<ushort, string> kp in _state.TimelineOverrides)
                    {
                        XmlNode cc = doc.CreateElement("Override");
                        c.AppendChild(cc);
                        XmlAttribute a = doc.CreateAttribute("Territory");
                        a.Value = kp.Key.ToString();
                        cc.Attributes.Append(a);
                        a = doc.CreateAttribute("Filename");
                        a.Value = kp.Value;
                        cc.Attributes.Append(a);
                    }
                }
            }
            return doc.OuterXml;
        }

        public void LoadConfig()
        {
            Log(State.LogLevelEnum.Info, "Loading configuration");
            _state.cfg = _state.pi.GetPluginConfig() as Config ?? new Config();
            Log(State.LogLevelEnum.Info, "Configuration loaded");
        }

        public void ApplyConfigToContent()
        {
            DeserializeProperties(_state.cfg.PropertyBlob);
        }

        public void SaveConfig()
        {
            Log(State.LogLevelEnum.Info, "Saving configuration");
            lock (this)
            {
                _state.cfg.PropertyBlob = SerializeProperties();
                _state.pi.SavePluginConfig(_state.cfg);
            }
            Log(State.LogLevelEnum.Info, "Configuration saved");
        }

        public string GenerateMD5Hash(string data)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] bytes = Encoding.ASCII.GetBytes(data);
                byte[] hash = md5.ComputeHash(bytes);
                return Convert.ToHexString(hash);
            }
        }

        public void BackupConfig()
        {
            string temp = Path.GetTempPath();
            string tempfile = Path.Combine(temp, String.Format("lemegeton_backup_{0}.json", DateTime.Now.ToString("yyyyMMdd_HHmmssfff")));
            string data = SerializeConfig();
            File.WriteAllText(tempfile, data);
            Log(State.LogLevelEnum.Info, "Configuration backup saved to {0}", tempfile);
        }

        public string SerializeConfig()
        {
            return JsonConvert.SerializeObject(_state.cfg, Newtonsoft.Json.Formatting.Indented);
        }

        public string SerializeConfigSnapshot()
        {
            string temp = SerializeConfig();
            int i = 80;
            temp = Base64Encode(temp);
            temp = GenerateMD5Hash(temp) + temp;
#if !SANS_GOETIA
            temp = "Lemegeton2_" + temp;
#else
            temp = "Lemegeton1_" + temp;
#endif
            while (i < temp.Length)
            {
                temp = temp.Insert(i, Environment.NewLine);
                i += 80;
            }
            return temp;
        }

        public IPluginConfiguration DeserializeConfigSnapshot(string data)
        {
            if (data.Length < 37)
            {
                Log(LogLevelEnum.Error, "Missing quite a lot of data on this snapshot!");
                return null;
            }
#if !SANS_GOETIA
            string mytag = "Lemegeton2_";
#else
            string mytag = "Lemegeton1_";
#endif
            string tag = data.Substring(0, mytag.Length);
            if (String.Compare(tag, mytag) != 0)
            {
                Log(LogLevelEnum.Error, "This config snapshot is from {0}, while this instance is {1}!", tag, mytag);
                return null;
            }
            string md5 = data.Substring(mytag.Length, 32);
            string blob = data.Substring(mytag.Length + 32);
            Regex rex = new Regex("[^A-Za-z0-9=+/]");
            blob = rex.Replace(blob, "");
            if (String.Compare(md5, GenerateMD5Hash(blob), true) != 0)
            {
                Log(LogLevelEnum.Error, "Snapshot does not seem to be intact!");
                return null;
            }
            blob = Base64Decode(blob);
            object o = JsonConvert.DeserializeObject<Config>(blob);
            return (IPluginConfiguration)o;
        }

        private void GetContribs(ContentModule cm)
        {
            if (cm.Author != "" && _contribs.Contains(cm.Author) == false)
            {
                _contribs.Add(cm.Author);
            }
            foreach (ContentModule child in cm.Children)
            {
                GetContribs(child);
            }
        }

        private void InitializeContent()
        {
            foreach (Type type in Assembly.GetAssembly(typeof(Core.ContentCategory)).GetTypes().Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(Core.ContentCategory))))
            {
                try
                {
                    //Log(LogLevelEnum.Debug, "Creating ContentCategory {0}", type.Name.ToString());
                    Core.ContentCategory c = (Core.ContentCategory)Activator.CreateInstance(type, new object[] { _state });
                    switch (c.ContentCategoryType)
                    {
                        case Core.ContentCategory.ContentCategoryTypeEnum.Content:
                            _content.Add(c);
                            break;
                        case Core.ContentCategory.ContentCategoryTypeEnum.Other:
                            _other.Add(c);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Log(LogLevelEnum.Error, "Couldn't initialize ContentCategory type {0} due to exception: {1} at {2}", type.Name, ex.Message, ex.StackTrace);
                }
            }
            foreach (Core.ContentCategory cc in _content)
            {
                GetContribs(cc);
            }
            foreach (Core.ContentCategory cc in _other)
            {
                GetContribs(cc);
            }
            if (_contribs.Count > 0)
            {
                _contribs.Sort();
                _contribs.Insert(0, "- CONTRIBUTORS -");
            }
        }

        private void InitializeLanguage()
        {
            foreach (Type type in Assembly.GetAssembly(typeof(Core.Language)).GetTypes().Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(Core.Language))))
            {

                try
                {
                    //Log(LogLevelEnum.Debug, "Creating Language {0}", type.Name.ToString());
                    Core.Language c = (Core.Language)Activator.CreateInstance(type, new object[] { _state });
                    I18n.AddLanguage(c);
                }
                catch (Exception ex)
                {
                    Log(LogLevelEnum.Error, "Couldn't initialize Language type {0} due to exception: {1} at {2}", type.Name, ex.Message, ex.StackTrace);
                }
            }
            Core.Language def = I18n.DefaultLanguage;
            foreach (var kp in I18n.RegisteredLanguages)
            {
                kp.Value.CalculateCoverage(def);
            }
        }

        private List<Tuple<PropertyInfo, int>> GetConfigurableProperties(ContentModule cm, bool disregardAdvanced, out bool hasDebug)
        {
            PropertyInfo[] props = cm.GetType().GetProperties();
            List<Tuple<PropertyInfo, int>> result = new List<Tuple<PropertyInfo, int>>();
            hasDebug = false;
            foreach (PropertyInfo pi in props)
            {
                CustomAttributeData debugAttr = null, confAttr = null;
                foreach (CustomAttributeData cad in pi.CustomAttributes)
                {
                    if (cad.AttributeType == typeof(DebugOption))
                    {
                        debugAttr = cad;
                        hasDebug = true;
                    }
                    if (cad.AttributeType == typeof(AttributeOrderNumber))
                    {
                        confAttr = cad;
                    }
                }
                if (confAttr != null)
                {
                    if (disregardAdvanced == true && debugAttr != null && _state.cfg.AdvancedOptions == false)
                    {
                        continue;
                    }
                    foreach (CustomAttributeTypedArgument cata in confAttr.ConstructorArguments)
                    {
                        int aon = (int)cata.Value;
                        result.Add(new Tuple<PropertyInfo, int>(pi, aon));
                    }
                }
            }
            List<Tuple<PropertyInfo, int>> temp = (from ix in result orderby ix.Item2 ascending select ix).ToList();
            return temp;
        }

        private void RenderAutomarkerPrioPlCustom(AutomarkerPrio amp)
        {
            if (ImGui.Button(I18n.Translate("Automarker/PrioType/PlCustom/FillFromCongaLine")) == true)
            {
                Party pty = _state.GetPartyMembers();
                AutomarkerPrio prio = new AutomarkerPrio() { Priority = PrioTypeEnum.CongaX };
                prio.SortByPriority(pty.Members);
                amp._prioByPlCustom.Clear();
                List<int> prios = new List<int>();
                prios.AddRange(from ix in pty.Members select ix.Index);
                for (int i = 1; i <= 8; i++)
                {
                    if (prios.Contains(i) == false)
                    {
                        prios.Add(i);
                    }
                }
                amp._prioByPlCustom.AddRange(prios);
            }
            _ui.RenderOrderableList<int>(amp._prioByPlCustom);
        }

        private void RenderAutomarkerPrioTrinity(AutomarkerPrio amp)
        {
            _ui.RenderOrderableList<PrioTrinityEnum>(amp._prioByTrinity);
        }

        private void RenderAutomarkerPrioRole(AutomarkerPrio amp)
        {
            _ui.RenderOrderableList<PrioRoleEnum>(amp._prioByRole);
        }

        private void RenderAutomarkerPrioJob(AutomarkerPrio amp)
        {
            _ui.RenderOrderableList<PrioJobEnum>(amp._prioByJob);
        }

        private void RenderAutomarkerPrioPlayer(AutomarkerPrio amp)
        {
            if (ImGui.Button(I18n.Translate("Automarker/PrioType/Player/FillFromPartyList")) == true)
            {
                Party pty = _state.GetPartyMembers();
                amp._prioByPlayer.Clear();
                amp._prioByPlayer.AddRange(from ix in pty.Members orderby ix.Index ascending select ix.Name);
            }
            ImGui.SameLine();
            if (ImGui.Button(I18n.Translate("Automarker/PrioType/Player/FillFromCongaLine")) == true)
            {
                Party pty = _state.GetPartyMembers();
                AutomarkerPrio prio = new AutomarkerPrio() { Priority = PrioTypeEnum.CongaX };
                prio.SortByPriority(pty.Members);
                amp._prioByPlayer.Clear();
                amp._prioByPlayer.AddRange(from ix in pty.Members select ix.Name);
            }
            _ui.RenderOrderableList<string>(amp._prioByPlayer);
        }

        private void RenderAutomarkerPrio(string path, PropertyInfo pi, object o)
        {
            AutomarkerPrio amp = (AutomarkerPrio)pi.GetValue(o);
            string proptr = I18n.Translate(path + "/" + pi.Name);
            ImGui.Text(proptr + Environment.NewLine + Environment.NewLine);
            ImGui.PushItemWidth(250);
            string selname = I18n.Translate("Automarker/PrioType/" + amp.Priority.ToString());
            if (ImGui.BeginCombo("##" + path + "/" + pi.Name, selname) == true)
            {
                foreach (AutomarkerPrio.PrioTypeEnum p in Enum.GetValues(typeof(AutomarkerPrio.PrioTypeEnum)))
                {
                    string name = I18n.Translate("Automarker/PrioType/" + p.ToString());
                    if (ImGui.Selectable(name, String.Compare(name, selname) == 0) == true)
                    {
                        amp.Priority = p;
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.PopItemWidth();
            switch (amp.Priority)
            {
                case AutomarkerPrio.PrioTypeEnum.PartyListOrder:
                    {
                        bool reverse = amp.Reversed;
                        if (ImGui.Checkbox(I18n.Translate("Automarker/PrioType/" + amp.Priority.ToString() + "/Reversed"), ref reverse) == true)
                        {
                            amp.Reversed = reverse;
                        }
                    }
                    break;
                case AutomarkerPrio.PrioTypeEnum.PartyListCustom:
                    ImGui.Text(Environment.NewLine);
                    RenderAutomarkerPrioPlCustom(amp);
                    break;
                case AutomarkerPrio.PrioTypeEnum.Alphabetic:
                    {
                        bool reverse = amp.Reversed;
                        if (ImGui.Checkbox(I18n.Translate("Automarker/PrioType/" + amp.Priority.ToString() + "/Reversed"), ref reverse) == true)
                        {
                            amp.Reversed = reverse;
                        }
                    }
                    break;
                case AutomarkerPrio.PrioTypeEnum.Trinity:
                    ImGui.Text(Environment.NewLine);
                    RenderAutomarkerPrioTrinity(amp);
                    break;
                case AutomarkerPrio.PrioTypeEnum.Role:
                    ImGui.Text(Environment.NewLine);
                    RenderAutomarkerPrioRole(amp);
                    break;
                case AutomarkerPrio.PrioTypeEnum.Job:
                    ImGui.Text(Environment.NewLine);
                    RenderAutomarkerPrioJob(amp);
                    break;
                case AutomarkerPrio.PrioTypeEnum.CongaX:
                    {
                        bool reverse = amp.Reversed;
                        if (ImGui.Checkbox(I18n.Translate("Automarker/PrioType/" + amp.Priority.ToString() + "/Reversed"), ref reverse) == true)
                        {
                            amp.Reversed = reverse;
                        }
                    }
                    break;
                case AutomarkerPrio.PrioTypeEnum.CongaY:
                    {
                        bool reverse = amp.Reversed;
                        if (ImGui.Checkbox(I18n.Translate("Automarker/PrioType/" + amp.Priority.ToString() + "/Reversed"), ref reverse) == true)
                        {
                            amp.Reversed = reverse;
                        }
                    }
                    break;
                case AutomarkerPrio.PrioTypeEnum.Player:
                    ImGui.Text(Environment.NewLine);
                    RenderAutomarkerPrioPlayer(amp);
                    break;
            }
        }

        private void RenderAutomarkerTiming(string path, PropertyInfo pi, object o)
        {
            AutomarkerTiming amt = (AutomarkerTiming)pi.GetValue(o);
            bool parent = false;
            path += pi.Name;
            bool inherited = amt.TimingType == AutomarkerTiming.TimingTypeEnum.Inherit;
            if (amt.Parent != null)
            {
                if (ImGui.Checkbox(I18n.Translate("Automarker/TimingType/InheritDesc") + "##Inh" + path, ref inherited) == true)
                {
                    amt.TimingType = inherited == true ? AutomarkerTiming.TimingTypeEnum.Inherit : AutomarkerTiming.TimingTypeEnum.Explicit;
                }
                if (amt.TimingType == AutomarkerTiming.TimingTypeEnum.Inherit)
                {
                    amt = amt.Parent;
                    ImGui.BeginDisabled();
                    parent = true;
                }
            }
            ImGui.TextWrapped((amt.Parent != null || parent == true ? Environment.NewLine : "") + I18n.Translate("MainMenu/Settings/AutomarkersInitialApplicationDelay"));
            float autoIniMin = amt.IniDelayMin;
            float autoIniMax = amt.IniDelayMax;
            if (ImGui.DragFloatRange2(I18n.Translate("MainMenu/Settings/AutomarkerSeconds") + "##Ini" + path, ref autoIniMin, ref autoIniMax, 0.1f, 0.0f, 60.0f) == true)
            {
                amt.IniDelayMin = autoIniMin;
                amt.IniDelayMax = autoIniMax;
            }
            ImGui.TextWrapped(Environment.NewLine + I18n.Translate("MainMenu/Settings/AutomarkersSubsequentApplicationDelay"));
            float autoSubMin = amt.SubDelayMin;
            float autoSubMax = amt.SubDelayMax;
            if (ImGui.DragFloatRange2(I18n.Translate("MainMenu/Settings/AutomarkerSeconds") + "##Sub" + path, ref autoSubMin, ref autoSubMax, 0.01f, 0.0f, 2.0f) == true)
            {
                amt.SubDelayMin = autoSubMin;
                amt.SubDelayMax = autoSubMax;
            }
            if (parent == true)
            {
                ImGui.EndDisabled();
            }
        }

        private void RenderPercentage(string path, PropertyInfo pi, object o)
        {
            Percentage pr = (Percentage)pi.GetValue(o);
            string proptr = I18n.Translate(path + "/" + pi.Name);
            ImGui.TextWrapped(proptr);
            float cur = pr.CurrentValue;
            ImGui.PushItemWidth(150.0f);
            if (ImGui.SliderFloat("%##" + proptr, ref cur, pr.MinValue, pr.MaxValue, "%.1f") == true)
            {
                pr.CurrentValue = cur;
            }
            ImGui.PopItemWidth();
        }

        private void RenderAutomarkerSigns(string path, PropertyInfo pi, object o)
        {
            AutomarkerSigns ams = (AutomarkerSigns)pi.GetValue(o);
            string proptr = I18n.Translate(path + "/" + pi.Name);
            ImGui.Text(proptr + Environment.NewLine + Environment.NewLine);
            bool manualSetting = true;
            bool customPreset = false;
            ContentModule cm = (ContentModule)o;
            if (ams.Presets.Count > 0 || cm._debugDisplayToggled == true)
            {
                string manualpr = I18n.Translate("Automarker/ManualPreset");
                string prval = ams.SelectedPreset;
                string selname = null;
                manualSetting = (ams.SelectedPreset == null);
                if (prval != null)
                {
                    AutomarkerSigns.Preset preset = null;
                    if (ams.SelectedPreset != null)
                    {
                        ams.Presets.TryGetValue(ams.SelectedPreset, out preset);
                    }
                    if (preset != null && preset.Builtin == false)
                    {
                        selname = preset.Name;
                    }
                    else
                    {
                        selname = I18n.Translate(path + "/" + pi.Name + "/Presets/" + prval);
                    }
                }
                else
                {
                    selname = manualpr;
                }
                ImGui.PushItemWidth(250);
                if (ImGui.BeginCombo("##" + path + "/" + pi.Name, selname) == true)
                {
                    if (ImGui.Selectable(manualpr, String.Compare(manualpr, selname) == 0) == true)
                    {
                        ams.ApplyPreset(null);
                    }
                    bool firstCustom = true;
                    foreach (KeyValuePair<string, AutomarkerSigns.Preset> kp in ams.Presets)
                    {
                        if (kp.Value.Builtin == true)
                        {
                            proptr = I18n.Translate(path + "/" + pi.Name + "/Presets/" + kp.Key);
                        }
                        else
                        {
                            proptr = kp.Key;
                            if (firstCustom == true)
                            {
                                ImGui.Separator();
                                ImGui.Separator();
                                firstCustom = false;
                            }
                        }
                        if (ImGui.Selectable(proptr, String.Compare(proptr, selname) == 0) == true)
                        {
                            ams.ApplyPreset(kp.Key);
                        }
                    }
                    ImGui.EndCombo();
                }
                ImGui.PopItemWidth();
                if (cm._debugDisplayToggled == true)
                {
                    AutomarkerSigns.Preset preset = null;
                    if (ams.SelectedPreset != null)
                    {
                        ams.Presets.TryGetValue(ams.SelectedPreset, out preset);
                    }
                    bool saveEnabled = preset == null || preset.Builtin == false;
                    bool trashEnabled = preset != null && preset.Builtin == false;
                    customPreset = saveEnabled;
                    ImGui.SameLine();
                    if (saveEnabled == false)
                    {
                        ImGui.BeginDisabled();
                    }
                    string popupname = path + "/" + pi.Name + "/SavePresetPopup";
                    if (UserInterface.IconButton(FontAwesomeIcon.Save, I18n.Translate("Misc/SavePreset")) == true)
                    {
                        if (preset != null)
                        {
                            Log(LogLevelEnum.Debug, "Saving old preset {0} for {1}", preset.Name, path);
                            ams.SavePreset(preset.Name);
                        }
                        else
                        {
                            _newPresetName = "";
                            ImGui.OpenPopup(popupname);
                        }
                    }
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 2.0f);
                    ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
                    if (ImGui.BeginPopup(popupname) == true)
                    {
                        ImGui.Text(I18n.Translate("Misc/SaveNewPresetAs"));
                        string prename = _newPresetName;
                        if (ImGui.InputText("##Pn" + popupname, ref prename, 256) == true)
                        {
                            _newPresetName = prename;
                        }
                        ImGui.SameLine();
                        bool goodname = _newPresetName.Trim().Length > 0;
                        if (goodname == false)
                        {
                            ImGui.BeginDisabled();
                        }
                        if (UserInterface.IconButton(FontAwesomeIcon.Save, I18n.Translate("Misc/SavePreset")) == true)
                        {
                            string prname = _newPresetName.Trim();
                            Log(LogLevelEnum.Debug, "Saving new preset {0} for {1}", prname, path);
                            ams.SavePreset(prname);
                            ImGui.CloseCurrentPopup();
                        }
                        if (goodname == false)
                        {
                            ImGui.EndDisabled();
                        }
                        ImGui.EndPopup();
                    }
                    ImGui.PopStyleColor();
                    ImGui.PopStyleVar();
                    if (saveEnabled == false)
                    {
                        ImGui.EndDisabled();
                    }
                    ImGui.SameLine();
                    if (trashEnabled == false)
                    {
                        ImGui.BeginDisabled();
                    }
                    if (UserInterface.IconButton(FontAwesomeIcon.Trash, I18n.Translate("Misc/DeletePreset")) == true)
                    {
                        Log(LogLevelEnum.Debug, "Deleting preset {0} from {1}", ams.SelectedPreset, path);
                        ams.DeletePreset(ams.SelectedPreset);
                    }
                    if (trashEnabled == false)
                    {
                        ImGui.EndDisabled();
                    }
                }
                ImGui.Text(Environment.NewLine);
            }
            if (manualSetting == false && customPreset == false)
            {
                ImGui.BeginDisabled();
            }
            ImGui.BeginGroup();
            ImGui.PushItemWidth(150);
            Vector2 curPos = ImGui.GetCursorPos();
            ImGuiStylePtr style = ImGui.GetStyle();
            float wid = ImGui.GetWindowWidth();
            int x = 0, y = 0;
            foreach (KeyValuePair<string, AutomarkerSigns.SignEnum> kp in ams.Roles)
            {
                string proppath = path + "/" + pi.Name + "/" + kp.Key;
                proptr = I18n.Translate(proppath);
                string signtr = I18n.Translate("Signs/" + kp.Value);
                ImGui.SetCursorPos(new Vector2(curPos.X + (220 * x), curPos.Y + (80 * y)));
                ImGui.BeginGroup();
                x++;
                if (curPos.X + (220 * (x + 1)) >= wid)
                {
                    x = 0;
                    y++;
                }
                ImGui.Text(proptr);
                if (kp.Value != AutomarkerSigns.SignEnum.None)
                {
                    IDalamudTextureWrap tw = _ui.GetSignIcon(kp.Value).GetWrapOrEmpty();
                    ImGui.Image(tw.ImGuiHandle, new Vector2(tw.Width, tw.Height), new Vector2(0.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
                    ImGui.SameLine();
                }
                else
                {
                    IDalamudTextureWrap tw = _ui.GetSignIcon(AutomarkerSigns.SignEnum.Attack1).GetWrapOrEmpty();
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + tw.Width + style.ItemSpacing.X);
                }
                if (ImGui.BeginCombo("##" + proppath, signtr) == true)
                {
                    foreach (string name in Enum.GetNames(typeof(AutomarkerSigns.SignEnum)))
                    {
                        string estr = I18n.Translate("Signs/" + name);
                        if (ImGui.Selectable(estr, String.Compare(signtr, estr) == 0) == true)
                        {
                            AutomarkerSigns.SignEnum newsign = (AutomarkerSigns.SignEnum)Enum.Parse(typeof(AutomarkerSigns.SignEnum), name);
                            ams.SetRole(kp.Key, newsign);
                        }
                    }
                    ImGui.EndCombo();
                }
                ImGui.EndGroup();
            }
            ImGui.PopItemWidth();
            ImGui.EndGroup();
            ImGui.SetCursorPos(new Vector2(curPos.X, ImGui.GetCursorPosY() + style.ItemSpacing.Y));
            if (manualSetting == false && customPreset == false)
            {
                ImGui.EndDisabled();
            }
        }

        private void RenderProperties(string path, ContentModule cm)
        {
            List<Tuple<PropertyInfo, int>> props = GetConfigurableProperties(cm, cm._debugDisplayToggled == false, out bool hasDebug);
            bool firstProp = true;
            int lastAon = 0;
            ImGuiStylePtr style = ImGui.GetStyle();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + style.ItemSpacing.Y);
            if (hasDebug == true && _state.cfg.AdvancedOptions == false)
            {
                Vector2 curpos = ImGui.GetCursorPos();
                ImGui.PushFont(UiBuilder.IconFont);
                string ico = FontAwesomeIcon.Cog.ToIconString();
                Vector2 sz = ImGui.CalcTextSize(ico);
                sz.X += style.ItemInnerSpacing.X * 2.0f;
                sz.Y += style.ItemInnerSpacing.Y * 2.0f;
                ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - sz.X);
                bool isToggled = cm._debugDisplayToggled;
                if (isToggled)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, style.Colors[(int)ImGuiCol.ButtonActive]);
                }
                if (ImGui.Button(FontAwesomeIcon.Cog.ToIconString(), sz) == true)
                {
                    cm._debugDisplayToggled = (cm._debugDisplayToggled == false);
                }
                ImGui.PopFont();
                if (ImGui.IsItemHovered() == true)
                {
                    ImGui.BeginTooltip();
                    ImGui.Text(isToggled == true ? I18n.Translate("Misc/HideAdvancedOptions") : I18n.Translate("Misc/ShowAdvancedOptions"));
                    ImGui.EndTooltip();
                }
                if (isToggled)
                {
                    ImGui.PopStyleColor();
                }
                ImGui.SetCursorPos(curpos);
            }
            foreach (Tuple<PropertyInfo, int> prop in props)
            {
                PropertyInfo pi = prop.Item1;
                if (firstProp == false)
                {
                    if (prop.Item2 > lastAon + 1)
                    {
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + style.ItemSpacing.Y);
                        ImGui.Separator();
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + style.ItemSpacing.Y);
                    }
                }
                firstProp = false;
                lastAon = prop.Item2;
                string proptr = I18n.Translate(path + "/" + pi.Name);
                MethodInfo mi = pi.GetSetMethod();
                if (mi == null || mi.IsPrivate == true)
                {
                    ImGui.BeginDisabled();
                }
                if (pi.PropertyType == typeof(bool))
                {
                    bool temp = (bool)pi.GetValue(cm);
                    if (ImGui.Checkbox(proptr, ref temp) == true)
                    {
                        pi.SetValue(cm, temp);
                    }
                }
                if (pi.PropertyType == typeof(string))
                {
                    string temp = (string)pi.GetValue(cm);
                    ImGui.Text(proptr);
                    if (ImGui.InputText("##" + proptr, ref temp, 2048) == true)
                    {
                        pi.SetValue(cm, temp);
                    }
                }
                else if (pi.PropertyType == typeof(Vector3))
                {
                    Vector3 temp = (Vector3)pi.GetValue(cm);
                    if (ImGui.ColorEdit3(proptr, ref temp, ImGuiColorEditFlags.NoInputs) == true)
                    {
                        pi.SetValue(cm, temp);
                    }
                }
                else if (pi.PropertyType == typeof(int))
                {
                    ImGui.PushItemWidth(250);
                    int temp = (int)pi.GetValue(cm);
                    if (ImGui.DragInt(proptr, ref temp, 1) == true)
                    {
                        pi.SetValue(cm, temp);
                    }
                    ImGui.PopItemWidth();
                }
                else if (pi.PropertyType == typeof(float))
                {
                    ImGui.PushItemWidth(250);
                    float temp = (float)pi.GetValue(cm);
                    if (ImGui.DragFloat(proptr, ref temp, 0.01f, 0.1f) == true)
                    {
                        pi.SetValue(cm, temp);
                    }
                    ImGui.PopItemWidth();
                }
                else if (pi.PropertyType == typeof(Vector4))
                {
                    Vector4 temp = (Vector4)pi.GetValue(cm);
                    if (ImGui.ColorEdit4(proptr, ref temp, ImGuiColorEditFlags.NoInputs) == true)
                    {
                        pi.SetValue(cm, temp);
                    }
                }
                else if (pi.PropertyType == typeof(AutomarkerTiming))
                {
                    RenderAutomarkerTiming(path, pi, cm);
                }
                else if (pi.PropertyType == typeof(AutomarkerSigns))
                {
                    RenderAutomarkerSigns(path, pi, cm);
                }
                else if (pi.PropertyType == typeof(AutomarkerPrio))
                {
                    RenderAutomarkerPrio(path, pi, cm);
                }
                else if (pi.PropertyType == typeof(Percentage))
                {
                    RenderPercentage(path, pi, cm);
                }
                else if (pi.PropertyType == typeof(FoodSelector))
                {
                    FoodSelector fs = (FoodSelector)pi.GetValue(cm);
                    fs.Render(path + "/" + pi.Name);
                }
                else if (pi.PropertyType == typeof(System.Action))
                {
                    System.Action act = (System.Action)pi.GetValue(cm);
                    if (act == null)
                    {
                        ImGui.BeginDisabled();
                    }
                    if (ImGui.Button(proptr) == true)
                    {
                        act();
                    }
                    if (act == null)
                    {
                        ImGui.EndDisabled();
                    }
                }
                else if (pi.PropertyType.IsSubclassOf(typeof(CustomPropertyInterface)))
                {
                    CustomPropertyInterface cpi = (CustomPropertyInterface)pi.GetValue(cm);
                    cpi.RenderEditor(path + "/" + pi.Name);
                }
                if (mi == null || mi.IsPrivate == true)
                {
                    ImGui.EndDisabled();
                }
            }
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + style.ItemSpacing.Y);
        }

        internal static string Capitalize(string str)
        {
            if (string.IsNullOrEmpty(str) == true)
            {
                return string.Empty;
            }
            char[] a = str.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        internal string GetActionName(uint key)
        {
            Lumina.Excel.GeneratedSheets.Action a = _state.dm.Excel.GetSheet<Lumina.Excel.GeneratedSheets.Action>().GetRow(key);
            string name = Capitalize(a.Name);
            if (name.Contains("_rsv_") == true)
            {
                name = I18n.Translate(String.Format("RSV/Ability_{0}", key));
            }
            return name;
        }

        internal string GetStatusName(uint key)
        {
            Lumina.Excel.GeneratedSheets.Status a = _state.dm.Excel.GetSheet<Lumina.Excel.GeneratedSheets.Status>().GetRow(key);
            string name = Capitalize(a.Name);
            if (name.Contains("_rsv_") == true)
            {
                name = I18n.Translate(String.Format("RSV/Status_{0}", key));
            }
            return name;
        }

        private string GetFullNameForTimelineEntry(Timeline.Entry e)
        {
            if (e.CachedName == null)
            {
                switch (e.Type)
                {
                    case Timeline.Entry.EntryTypeEnum.Ability:
                        {
                            List<string> temp = new List<string>();
                            foreach (uint key in e.KeyValues)
                            {
                                string name = GetActionName(key);
                                if (temp.Contains(name) == false)
                                {
                                    temp.Add(name);
                                }
                            }
                            e.CachedName = String.Join(" / ", temp);
                        }
                        break;
                    case Timeline.Entry.EntryTypeEnum.Spawn:
                        {
                            List<string> temp = new List<string>();
                            foreach (uint key in e.KeyValues)
                            {
                                Lumina.Excel.GeneratedSheets.BNpcName a = _state.dm.Excel.GetSheet<Lumina.Excel.GeneratedSheets.BNpcName>().GetRow(key);
                                string name = Capitalize(a.Singular) + " \xE0AF";
                                if (temp.Contains(name) == false)
                                {
                                    temp.Add(name);
                                }
                            }
                            e.CachedName = String.Join(" / ", temp);
                        }
                        break;
                    case Timeline.Entry.EntryTypeEnum.Targettable:
                        e.CachedName = I18n.Translate("Timelines/SpecialTags/Targettable");
                        break;
                    case Timeline.Entry.EntryTypeEnum.Untargettable:
                        e.CachedName = I18n.Translate("Timelines/SpecialTags/Untargettable");
                        break;
                    case Timeline.Entry.EntryTypeEnum.Timed:
                        e.CachedName = e.Description != null ? e.Description : "???";
                        break;
                    default:
                        e.CachedName = "???";
                        break;
                }
            }
            return e.CachedName;
        }

        private string GetRotatingNameForTimelineEntry(float time, Timeline.Entry e)
        {
            switch (e.Type)
            {
                case Timeline.Entry.EntryTypeEnum.Ability:
                    {
                        int index = (int)Math.Floor((DateTime.Now - _loaded).TotalSeconds / 2.0f) % e.KeyValues.Count;
                        return Capitalize(GetActionName(e.KeyValues[index]));
                    }
                case Timeline.Entry.EntryTypeEnum.Spawn:
                    {
                        int index = (int)Math.Floor((DateTime.Now - _loaded).TotalSeconds / 2.0f) % e.KeyValues.Count;
                        Lumina.Excel.GeneratedSheets.BNpcName a = _state.dm.Excel.GetSheet<Lumina.Excel.GeneratedSheets.BNpcName>().GetRow(e.KeyValues[index]);
                        return Capitalize(a.Singular + " \xE0AF");
                    }
                case Timeline.Entry.EntryTypeEnum.Targettable:
                    return I18n.Translate("Timelines/SpecialTags/Targettable");
                case Timeline.Entry.EntryTypeEnum.Untargettable:
                    return I18n.Translate("Timelines/SpecialTags/Untargettable");
                case Timeline.Entry.EntryTypeEnum.Timed:
                    return e.Description != null ? e.Description : "???";
                default:
                    break;
            }
            return "???";
        }

        internal string GetInstanceNameForTerritory(ushort territory)
        {
            TerritoryType tt = _state.dm.Excel.GetSheet<Lumina.Excel.GeneratedSheets.TerritoryType>().GetRow(territory);
            ContentFinderCondition cfc = _state.dm.Excel.GetSheet<Lumina.Excel.GeneratedSheets.ContentFinderCondition>().Where(x => x.TerritoryType.Value == tt).FirstOrDefault();
            if (cfc != null)
            {
                return cfc.Name;
            }
            PlaceName pn = _state.dm.Excel.GetSheet<Lumina.Excel.GeneratedSheets.PlaceName>().GetRow(tt.PlaceName.Row);
            if (pn != null)
            {
                return pn.Name;
            }
            return tt.Name;
        }

        private string GetInstanceNameForTimeline(Timeline tl)
        {
            if (tl.CachedName == null)
            {
                tl.CachedName = GetInstanceNameForTerritory(tl.Territory);
            }
            return tl.CachedName;
        }

        private void RenderTimelineContentHeader()
        {
            Timeline tl = SelectedTimeline;
            string ico;
            Vector2 fsp = ImGui.GetContentRegionAvail();
            ImGuiStylePtr style = ImGui.GetStyle();
            Timeline atl = _state._timeline;
            IDalamudTextureWrap tw;
            string statustext;
            _ui.RenderWarning(I18n.Translate("Misc/ExperimentalFeature"));
            Vector2 startpos = ImGui.GetCursorPos();
            if (atl != null)
            {
                tw = _ui.GetMiscIcon(UserInterface.MiscIconEnum.TimelineActive).GetWrapOrEmpty();                
                ImGui.Image(tw.ImGuiHandle, new Vector2(tw.Width, tw.Height));
                if (ImGui.IsItemHovered() == true)
                {
                    ImGui.BeginTooltip();
                    ImGui.Text(I18n.Translate("Timelines/Status/ActiveTimeline"));
                    ImGui.EndTooltip();
                }
                if (ImGui.IsItemClicked() == true)
                {
                    Timeline tlx = _state.GetTimeline(_state.cs.TerritoryType);
                    if (tlx != null)
                    {
                        if (tlx != SelectedTimeline)
                        {
                            SelectedTimeline = tlx;
                        }
                        SelectedProfile = tlx.DefaultProfile;
                    }
                    else
                    {
                        SelectedTimeline = null;
                    }
                }
                Vector2 after = ImGui.GetCursorPos();
                string tlname = GetInstanceNameForTimeline(atl);
                Vector2 sz = ImGui.CalcTextSize(tlname);
                ImGui.SetCursorPos(new Vector2(tw.Width + style.ItemSpacing.X, startpos.Y + (tw.Height / 2.0f) - (sz.Y / 2.0f)));
                ImGui.Text(tlname);
                foreach (Timeline.Profile p in atl.SelectedProfiles)
                {
                    ImGui.SameLine();
                    ImGui.SetCursorPosY(startpos.Y);
                    tw = _ui.GetMiscIcon(UserInterface.MiscIconEnum.ProfileActive).GetWrapOrEmpty();
                    float basex = ImGui.GetCursorPosX();
                    ImGui.Image(tw.ImGuiHandle, new Vector2(tw.Width, tw.Height));
                    if (ImGui.IsItemHovered() == true)
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text(I18n.Translate("Timelines/Status/ActiveProfile"));
                        ImGui.EndTooltip();
                    }
                    if (ImGui.IsItemClicked() == true)
                    {
                        Timeline tlx = _state.GetTimeline(_state.cs.TerritoryType);
                        if (tlx != null)
                        {
                            if (tlx != SelectedTimeline)
                            {
                                SelectedTimeline = tlx;
                            }
                            SelectedProfile = p;
                        }
                        else
                        {
                            SelectedTimeline = null;
                            SelectedProfile = null;
                        }
                    }
                    tlname = p.Name;
                    sz = ImGui.CalcTextSize(tlname);
                    ImGui.SetCursorPos(new Vector2(basex + tw.Width + style.ItemSpacing.X, startpos.Y + (tw.Height / 2.0f) - (sz.Y / 2.0f)));
                    ImGui.Text(tlname);
                }
                ImGui.SetCursorPos(after);
            }
            else
            {
                tw = _ui.GetMiscIcon(UserInterface.MiscIconEnum.TimelineInactive).GetWrapOrEmpty();
                statustext = I18n.Translate("Timelines/Status/InactiveTimeline");
                ImGui.Image(tw.ImGuiHandle, new Vector2(tw.Width, tw.Height));
                Vector2 after = ImGui.GetCursorPos();
                Vector2 sz = ImGui.CalcTextSize(statustext);
                ImGui.SetCursorPos(new Vector2(tw.Width + style.ItemSpacing.X, startpos.Y + (tw.Height / 2.0f) - (sz.Y / 2.0f)));
                ImGui.Text(statustext);
                ImGui.SetCursorPos(after);
            }
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + style.ItemSpacing.Y);
            ImGui.PushItemWidth(150.0f + _adjusterXtl);
            ImGui.Text(I18n.Translate("Timelines/Timeline"));
            ImGui.SameLine();
            string seltlname = tl != null ? GetInstanceNameForTimeline(tl) : I18n.Translate("Timelines/Timeline/None");
            if (ImGui.BeginCombo("##MainMenu/Timelines/Timelines", seltlname) == true)
            {
                string name = I18n.Translate("Timelines/Timeline/None");
                if (ImGui.Selectable(name, SelectedTimeline == null) == true)
                {
                    SelectedTimeline = null;
                }
                lock (_state.AllTimelines)
                {
                    foreach (KeyValuePair<ushort, Timeline> kp in _state.AllTimelines)
                    {
                        name = GetInstanceNameForTimeline(kp.Value);
                        if (ImGui.Selectable(name, kp.Value == SelectedTimeline) == true)
                        {
                            if (kp.Value != tl)
                            {
                                SelectedProfile = null;
                            }
                            tl = SelectedTimeline = kp.Value;
                        }
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.SameLine();
            if (tl == null)
            {
                ImGui.BeginDisabled();
            }
            if (UserInterface.IconButton(FontAwesomeIcon.SyncAlt, I18n.Translate("Timelines/ReloadTimeline")) == true)
            {
                Timeline ntl = _state.CheckTimelineReload(tl);
                if (ntl != tl)
                {
                    if (ntl.Territory == _state.cs.TerritoryType)
                    {
                        _state.AutoselectTimeline(ntl.Territory);
                    }
                    SelectedTimeline = ntl;
                }
            }
            if (tl == null)
            {
                ImGui.EndDisabled();
            }
            ImGui.SameLine();
            if (UserInterface.IconButton(FontAwesomeIcon.Edit, I18n.Translate("Timelines/TimelineSelector")) == true)
            {
                OpenTimelineSelector();
            }
            ImGui.SameLine();
            ImGui.Text(I18n.Translate("Timelines/Profile"));
            ImGui.SameLine();
            bool hasprofiles = tl != null && tl.Profiles.Count > 0;
            if (hasprofiles == false)
            {
                ImGui.BeginDisabled();
            }
            if (ImGui.BeginCombo("##MainMenu/Timelines/Profiles", SelectedProfile != null && SelectedProfile.Default == false ? SelectedProfile.Name : I18n.Translate("Timelines/Profile/Default")) == true)
            {
                string name = I18n.Translate("Timelines/Profile/Default");
                if (ImGui.Selectable(name, SelectedProfile == null || SelectedProfile.Default == true) == true)
                {
                    if (SelectedProfile != null && SelectedProfile != SelectedTimeline.DefaultProfile)
                    {
                        SelectedProfile = SelectedTimeline.DefaultProfile;
                    }
                }
                foreach (Timeline.Profile p in tl.Profiles)
                {
                    if (ImGui.Selectable(p.Name, SelectedProfile == p) == true)
                    {
                        if (SelectedProfile != p)
                        {
                            SelectedProfile = p;
                        }
                    }
                }
                ImGui.EndCombo();
            }
            if (hasprofiles == false)
            {
                ImGui.EndDisabled();
            }
            string createpopupname = "Timelines/CreateProfilePopup";
            string clonepopupname = "Timelines/CloneProfilePopup";
            ImGui.SameLine();
            if (tl == null)
            {
                ImGui.BeginDisabled();
            }
            if (UserInterface.IconButton(FontAwesomeIcon.Plus, I18n.Translate("Timelines/CreateProfile")) == true)
            {
                _newProfileName = "";
                ImGui.OpenPopup(createpopupname);
            }
            ImGui.SameLine();
            if (UserInterface.IconButton(FontAwesomeIcon.Copy, I18n.Translate("Timelines/CloneProfile")) == true)
            {
                string orname = "";
                if (_selectedProfile != null && _selectedProfile.Default == false)
                {
                    int i = 1;
                    do
                    {
                        orname = _selectedProfile.Name + " (" + i + ")";
                        i++;
                    } while ((from ix in SelectedTimeline.Profiles where String.Compare(ix.Name, orname) == 0 select ix).Count() > 0);
                }
                _newProfileName = orname;
                ImGui.OpenPopup(clonepopupname);
            }
            if (tl == null)
            {
                ImGui.EndDisabled();
            }
            ImGui.SameLine();
            if (SelectedProfile == null || SelectedProfile.Default == true)
            {
                ImGui.BeginDisabled();
            }
            if (UserInterface.IconButton(FontAwesomeIcon.Trash, I18n.Translate("Timelines/DeleteProfile")) == true)
            {
                Log(LogLevelEnum.Debug, "Deleting profile {0}", SelectedProfile.Name);
                SelectedTimeline.RemoveProfile(SelectedProfile, _state.cs.LocalPlayer != null ? _state.cs.LocalPlayer.ClassJob.Id : 0);
                SelectedProfile = SelectedTimeline.DefaultProfile;
            }
            if (SelectedProfile == null || SelectedProfile.Default == true)
            {
                ImGui.EndDisabled();
            }
            if (ImGui.BeginPopup(createpopupname) == true)
            {
                ImGui.Text(I18n.Translate("Timelines/CreateNewProfileAs"));
                string prename = _newProfileName;
                if (ImGui.InputText("##Pn" + createpopupname, ref prename, 256) == true)
                {
                    _newProfileName = prename;
                }
                ImGui.SameLine();
                bool goodname = _newProfileName.Trim().Length > 0;
                if (goodname == false)
                {
                    ImGui.BeginDisabled();
                }
                if (UserInterface.IconButton(FontAwesomeIcon.Plus, I18n.Translate("Timelines/CreateProfile")) == true)
                {
                    string prname = _newProfileName.Trim();
                    Log(LogLevelEnum.Debug, "Creating new blank profile as {0}", prname);
                    Timeline.Profile pro = new Timeline.Profile() { Name = prname };
                    tl.AddProfile(pro, _state.cs.LocalPlayer != null ? _state.cs.LocalPlayer.ClassJob.Id : 0);                    
                    SelectedProfile = pro;
                    ImGui.CloseCurrentPopup();
                }
                if (goodname == false)
                {
                    ImGui.EndDisabled();
                }
                ImGui.EndPopup();
            }
            if (ImGui.BeginPopup(clonepopupname) == true)
            {
                ImGui.Text(I18n.Translate("Timelines/CloneProfileAs"));
                string prename = _newProfileName;
                if (ImGui.InputText("##Pn" + clonepopupname, ref prename, 256) == true)
                {
                    _newProfileName = prename;
                }
                ImGui.SameLine();
                bool goodname = _newProfileName.Trim().Length > 0;
                if (goodname == false)
                {
                    ImGui.BeginDisabled();
                }
                if (UserInterface.IconButton(FontAwesomeIcon.Copy, I18n.Translate("Timelines/CloneProfile")) == true)
                {
                    Timeline.Profile orp = _selectedProfile != null ? _selectedProfile : SelectedTimeline.DefaultProfile;
                    string orname = orp.Default == false ? orp.Name : I18n.Translate("Timelines/Profile/Default");
                    string prname = _newProfileName.Trim();
                    Log(LogLevelEnum.Debug, "Cloning profile {0} as new profile {1}", orname, prname);
                    Timeline.Profile pro = orp.Duplicate();
                    pro.Default = false;
                    pro.Name = prname;
                    tl.AddProfile(pro, _state.cs.LocalPlayer != null ? _state.cs.LocalPlayer.ClassJob.Id : 0);
                    SelectedProfile = pro;
                    ImGui.CloseCurrentPopup();
                }
                if (goodname == false)
                {
                    ImGui.EndDisabled();
                }
                ImGui.EndPopup();
            }
            ImGui.PopItemWidth();
            ImGui.SameLine();
            _adjusterXtl += (float)Math.Floor((fsp.X - ImGui.GetCursorPosX()) / 2.0f);
            ImGui.Text("");
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + style.ItemSpacing.Y);
        }

        private void OpenTimelineSelector()
        {
            Log(LogLevelEnum.Debug, "Opening timeline selector");
            _timelineSelectorOpened = true;
        }

        private float RenderTimelineContentEvents()
        {
            float start = ImGui.GetCursorPosY();
            Vector2 avail = ImGui.GetContentRegionAvail();
            ImGuiStylePtr style = ImGui.GetStyle();
            float timelineColumnWidth = 200.0f;
            ImGui.PushItemWidth(timelineColumnWidth);
            string selname = SelectedEncounter != null ? I18n.Translate("Timelines/Encounter", SelectedEncounter.Id) : "";
            if (ImGui.BeginCombo("##TimelinesActionEncounter", selname) == true)
            {
                if (SelectedTimeline != null)
                {
                    foreach (Timeline.Encounter enc in SelectedTimeline.Encounters)
                    {
                        string trname = I18n.Translate("Timelines/Encounter", enc.Id);
                        if (ImGui.Selectable(trname, String.Compare(trname, selname) == 0) == true)
                        {
                            SelectedEncounter = enc;
                        }
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.PopItemWidth();
            if (ImGui.BeginListBox("##TimelinesActionListbox", new Vector2(timelineColumnWidth, avail.Y - _adjusterYtl)) == true)
            {
                if (SelectedEncounter != null)
                {
                    IEnumerable<Timeline.Entry> entries = SelectedEncounter.Entries;
                    if (_timelineActionFilter != null)
                    {
                        string tmp = _timelineActionFilter.Trim();
                        if (tmp.Length > 0)
                        {
                            entries = from ix in SelectedEncounter.Entries where GetFullNameForTimelineEntry(ix).Contains(tmp, StringComparison.InvariantCultureIgnoreCase) == true select ix;
                        }
                    }
                    float time = (float)((DateTime.Now - _loaded).TotalMilliseconds / 600.0);
                    float alpha = (float)Math.Abs(Math.Cos(time));
                    foreach (var item in entries)
                    {
                        string name = GetFullNameForTimelineEntry(item);
                        Vector2 pos1 = ImGui.GetCursorPos();
                        string tag = String.Format("{0:0}", item.StartTime) + "\t" + name;
                        if (ImGui.Selectable(tag + "##" + item.Id, item == SelectedEntry) == true)
                        {
                            SelectedEntry = item;
                        }
                        Vector2 psz = ImGui.CalcTextSize(tag);
                        if (psz.X > timelineColumnWidth && ImGui.IsItemHovered() == true)
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text(name);
                            ImGui.EndTooltip();
                        }
                        Vector2 pos2 = ImGui.GetCursorPos();
                        float posx = pos1.X + timelineColumnWidth - style.ItemSpacing.X;
                        Timeline.Entry.ProfileSettings pro = _selectedProfile.GetSettingsForEntry(item);
                        if (pro.ReactionsActive == true && pro.Reactions.Count > 0)
                        {
                            ImGui.PushFont(UiBuilder.IconFont);
                            string dico = FontAwesomeIcon.Bolt.ToIconString();
                            Vector2 icosz = ImGui.CalcTextSize(dico);
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.0f, 0.0f, 0.0f, alpha));
                            ImGui.SetCursorPos(new Vector2(posx - icosz.X + 1.0f, ((pos1.Y + pos2.Y) / 2.0f) - (icosz.Y / 2.0f)));
                            ImGui.Text(dico);
                            ImGui.PopStyleColor(1);
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.0f, 0.0f, 0.0f, alpha));
                            ImGui.SetCursorPos(new Vector2(posx - icosz.X - 1.0f, ((pos1.Y + pos2.Y) / 2.0f) - (icosz.Y / 2.0f)));
                            ImGui.Text(dico);
                            ImGui.PopStyleColor(1);
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 0.0f, alpha));
                            ImGui.SetCursorPos(new Vector2(posx - icosz.X, ((pos1.Y + pos2.Y) / 2.0f) - (icosz.Y / 2.0f)));
                            ImGui.Text(dico);
                            ImGui.PopStyleColor(1);
                            ImGui.PopFont();
                            posx -= icosz.X;
                        }
                        if (pro.ShowOverlay == false)
                        {
                            ImGui.PushFont(UiBuilder.IconFont);
                            string dico = FontAwesomeIcon.EyeSlash.ToIconString();
                            Vector2 icosz = ImGui.CalcTextSize(dico);
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.0f, 0.0f, 0.0f, alpha));
                            ImGui.SetCursorPos(new Vector2(posx - icosz.X + 1.0f, ((pos1.Y + pos2.Y) / 2.0f) - (icosz.Y / 2.0f)));
                            ImGui.Text(dico);
                            ImGui.PopStyleColor(1);
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.0f, 0.0f, 0.0f, alpha));
                            ImGui.SetCursorPos(new Vector2(posx - icosz.X - 1.0f, ((pos1.Y + pos2.Y) / 2.0f) - (icosz.Y / 2.0f)));
                            ImGui.Text(dico);
                            ImGui.PopStyleColor(1);
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.0f, 1.0f, 1.0f, alpha));
                            ImGui.SetCursorPos(new Vector2(posx - icosz.X, ((pos1.Y + pos2.Y) / 2.0f) - (icosz.Y / 2.0f)));
                            ImGui.Text(dico);
                            ImGui.PopStyleColor(1);
                            ImGui.PopFont();
                            posx -= icosz.X;
                        }
                        ImGui.SetCursorPos(pos2);
                    }
                }
                ImGui.EndListBox();
            }
            if (SelectedTimeline == null)
            {
                ImGui.BeginDisabled();
            }
            ImGui.PushFont(UiBuilder.IconFont);
            string ico = FontAwesomeIcon.Search.ToIconString();
            Vector2 sz = ImGui.CalcTextSize(ico);
            ImGui.Text(ico);
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.PushItemWidth(timelineColumnWidth - sz.X - style.ItemSpacing.X);
            if (ImGui.InputText("##Timelines/FilterBox", ref _timelineActionFilter, 256) == true)
            {
            }
            _adjusterYtl += ImGui.GetCursorPosY() - avail.Y;
            ImGui.PopItemWidth();
            if (SelectedTimeline == null)
            {
                ImGui.EndDisabled();
            }
            return timelineColumnWidth + style.ItemSpacing.X;
        }

        private void RenderTimelineProfileSettings()
        {
            bool isDefault = (SelectedProfile == null || SelectedProfile.Default == true);
            bool tlvisible = SelectedProfile != null ? SelectedProfile.ShowOverlay : false;
            if (isDefault == true)
            {
                _ui.RenderWarning(I18n.Translate("Timelines/ProfileSettings/DefaultApplied"));
                ImGui.Text("");
            }
            if (ImGui.Checkbox(I18n.Translate("Timelines/ProfileSettings/ShowOverlay"), ref tlvisible) == true)
            {
                SelectedProfile.ShowOverlay = tlvisible;
            }
            bool reactact = SelectedProfile != null ? SelectedProfile.ReactionsActive : false;
            if (ImGui.Checkbox(I18n.Translate("Timelines/ProfileSettings/ReactionsActive"), ref reactact) == true)
            {
                SelectedProfile.ReactionsActive = reactact;
            }
            ImGui.Text("");
            if (isDefault == false)
            {
                bool applych = false;
                bool applyalways = SelectedProfile != null ? SelectedProfile.ApplyAlways : false;
                if (ImGui.Checkbox(I18n.Translate("Timelines/ProfileSettings/ApplyAlways"), ref applyalways) == true)
                {
                    SelectedProfile.ApplyAlways = applyalways;
                    applych = true;
                }
                bool applypro = SelectedProfile != null ? SelectedProfile.ApplyAutomatically : false;
                if (ImGui.Checkbox(I18n.Translate("Timelines/ProfileSettings/ApplyProfileOnJob"), ref applypro) == true)
                {
                    SelectedProfile.ApplyAutomatically = applypro;
                    applych = true;
                }
                ImGui.Text("");
                if (SelectedProfile != null)
                {
                    ulong newmap = _ui.RenderJobSelector(SelectedProfile.ApplyOnJobs, true);
                    if (applych == true || newmap != SelectedProfile.ApplyOnJobs)
                    {
                        SelectedProfile.ApplyOnJobs = newmap;
                        if (_state._timeline != null)
                        {
                            _state._timeline.SelectProfiles(_state.cs.LocalPlayer != null ? _state.cs.LocalPlayer.ClassJob.Id : 0);
                        }
                    }
                }
                else
                {
                    _ui.RenderJobSelector(0xFFFFFFFFFFFFFFFF, true);
                }
            }
        }

        private void RenderTimelineEventSettings(float x)
        {
            ImGui.Indent(x);
            ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPosX(), 0.0f));
            Timeline.Entry.ProfileSettings pro = _selectedProfile != null ? _selectedProfile.GetSettingsForEntry(SelectedEntry) : null;
            bool tlvisible = pro != null ? pro.ShowOverlay : false;
            if (ImGui.Checkbox(I18n.Translate("Timelines/EventSettings/ShowOverlay"), ref tlvisible) == true)
            {
                pro.ShowOverlay = tlvisible;
            }
            bool reactact = pro != null ? pro.ReactionsActive : false;
            if (ImGui.Checkbox(I18n.Translate("Timelines/EventSettings/ReactionsActive"), ref reactact) == true)
            {
                pro.ReactionsActive = reactact;
            }
            ImGui.Text("");
            ImGui.BeginDisabled();
            ImGui.CollapsingHeader("  " + I18n.Translate("Timelines/Reactions"), ImGuiTreeNodeFlags.Leaf);
            ImGui.EndDisabled();
            float xsec = RenderTimelineReactionsList(out float y);
            if (SelectedReaction == null)
            {
                ImGui.BeginDisabled();
            }
            RenderTimelineActionsSettings(xsec, y);
            if (SelectedReaction == null)
            {
                ImGui.EndDisabled();
            }
            ImGui.Unindent(0.0f - x);
        }

        private float RenderTimelineReactionsList(out float ystart)
        {
            string createpopupname = "Timelines/CreateReactionPopup";
            string clonepopupname = "Timelines/CloneReactionPopup";
            Vector2 avail = ImGui.GetContentRegionAvail();
            float start = ImGui.GetCursorPosY();
            if (UserInterface.IconButton(FontAwesomeIcon.Plus, I18n.Translate("Timelines/Reactions/NewReaction")) == true)
            {
                _newReactionName = "";
                ImGui.OpenPopup(createpopupname);
            }
            ImGui.SameLine();
            if (SelectedReaction == null)
            {
                ImGui.BeginDisabled();
            }
            if (UserInterface.IconButton(FontAwesomeIcon.Copy, I18n.Translate("Timelines/Reactions/CloneReaction")) == true)
            {
                string orname = "";
                if (SelectedReaction != null)
                {
                    int i = 1;
                    Timeline.Entry.ProfileSettings pro = _selectedProfile != null ? _selectedProfile.GetSettingsForEntry(_selectedEntry) : null;
                    if (pro != null)
                    {
                        do
                        {
                            orname = SelectedReaction.Name + " (" + i + ")";
                            i++;
                        } while ((from ix in pro.Reactions where String.Compare(ix.Name, orname) == 0 select ix).Count() > 0);
                    }
                }
                _newReactionName = orname;
                ImGui.OpenPopup(clonepopupname);
            }
            ImGui.SameLine();
            if (UserInterface.IconButton(FontAwesomeIcon.Trash, I18n.Translate("Timelines/Reactions/DeleteReaction")) == true)
            {
                _selectedProfile.RemoveReactionFromEntry(_selectedEntry, SelectedReaction);
                SelectedReaction = null;
            }
            ImGui.SameLine();
            if (UserInterface.IconButton(FontAwesomeIcon.EllipsisH, I18n.Translate("Timelines/Reactions/TestReaction")) == true)
            {
                SelectedReaction.Execute(new Context() { State = _state });
            }
            if (SelectedReaction == null)
            {
                ImGui.EndDisabled();
            }
            float reactionColumnWidth = 200.0f;
            ystart = ImGui.GetCursorPosY();
            if (ImGui.BeginListBox("##Reactions", new Vector2(reactionColumnWidth, avail.Y - _adjusterYtl2)) == true)
            {
                Timeline.Entry.ProfileSettings pro = _selectedProfile != null ? _selectedProfile.GetSettingsForEntry(_selectedEntry) : null;
                if (pro != null)
                {
                    foreach (Timeline.Reaction r in pro.Reactions)
                    {
                        if (ImGui.Selectable(r.Name, SelectedReaction == r) == true)
                        {
                            SelectedReaction = r;
                        }
                        Vector2 psz = ImGui.CalcTextSize(r.Name);
                        if (psz.X > reactionColumnWidth && ImGui.IsItemHovered() == true)
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text(r.Name);
                            ImGui.EndTooltip();
                        }
                    }
                }
                ImGui.EndListBox();
            }
            if (ImGui.BeginPopup(createpopupname) == true)
            {
                ImGui.Text(I18n.Translate("Timelines/CreateNewReactionAs"));
                string prename = _newReactionName;
                if (ImGui.InputText("##Pn" + createpopupname, ref prename, 256) == true)
                {
                    _newReactionName = prename;
                }
                ImGui.SameLine();
                bool goodname = _newReactionName.Trim().Length > 0;
                if (goodname == false)
                {
                    ImGui.BeginDisabled();
                }
                if (UserInterface.IconButton(FontAwesomeIcon.Plus, I18n.Translate("Timelines/Reactions/NewReaction")) == true)
                {
                    string prname = _newReactionName.Trim();
                    Log(LogLevelEnum.Debug, "Creating new blank reaction as {0}", prname);
                    Timeline.Reaction r = new Timeline.Reaction();
                    r.Name = prname;
                    _selectedProfile.AddReactionForEntry(_selectedEntry, r);
                    SelectedReaction = r;
                    ImGui.CloseCurrentPopup();
                }
                if (goodname == false)
                {
                    ImGui.EndDisabled();
                }
                ImGui.EndPopup();
            }
            if (ImGui.BeginPopup(clonepopupname) == true)
            {
                ImGui.Text(I18n.Translate("Timelines/CloneReactionAs"));
                string prename = _newReactionName;
                if (ImGui.InputText("##Pn" + clonepopupname, ref prename, 256) == true)
                {
                    _newReactionName = prename;
                }
                ImGui.SameLine();
                bool goodname = _newReactionName.Trim().Length > 0;
                if (goodname == false)
                {
                    ImGui.BeginDisabled();
                }
                if (UserInterface.IconButton(FontAwesomeIcon.Copy, I18n.Translate("Timelines/Reactions/CloneReaction")) == true)
                {
                    Timeline.Reaction orp = SelectedReaction;
                    string orname = orp.Name;
                    string prname = _newReactionName.Trim();
                    Log(LogLevelEnum.Debug, "Cloning reaction {0} as new reaction {1}", orname, prname);
                    Timeline.Reaction r = orp.Duplicate();
                    r.Name = prname;
                    _selectedProfile.AddReactionForEntry(_selectedEntry, r);
                    SelectedReaction = r;
                    ImGui.CloseCurrentPopup();
                }
                if (goodname == false)
                {
                    ImGui.EndDisabled();
                }
                ImGui.EndPopup();
            }
            _adjusterYtl2 += ImGui.GetCursorPosY() - avail.Y - start;
            ImGuiStylePtr style = ImGui.GetStyle();
            return reactionColumnWidth + style.ItemSpacing.X;
        }

        private void RenderTimelineActionsList()
        {
            Vector2 avail = ImGui.GetContentRegionAvail();
            float start = ImGui.GetCursorPosY();
            if (UserInterface.IconButton(FontAwesomeIcon.Plus, I18n.Translate("Timelines/Actions/NewAction")) == true)
            {
                ImGui.OpenPopup("NewReactionAction");
            }
            ImGui.SameLine();
            if (_selectedAction == null)
            {
                ImGui.BeginDisabled();
            }
            if (UserInterface.IconButton(FontAwesomeIcon.Copy, I18n.Translate("Timelines/Actions/CloneAction")) == true)
            {
                Core.Action a = _selectedAction.Duplicate();
                a.Id = Guid.NewGuid();
                _selectedReaction.ActionList.Add(a);
                _selectedAction = a;
            }
            ImGui.SameLine();
            if (UserInterface.IconButton(FontAwesomeIcon.Trash, I18n.Translate("Timelines/Actions/DeleteAction")) == true)
            {
                if (_selectedReaction.ActionList.Contains(_selectedAction) == true)
                {
                    _selectedReaction.ActionList.Remove(_selectedAction);
                }
                _selectedAction = null;
            }
            ImGui.SameLine();
            if (UserInterface.IconButton(FontAwesomeIcon.EllipsisH, I18n.Translate("Timelines/Actions/TestAction")) == true)
            {
                _selectedAction.Execute(new Context() { State = _state });
            }
            if (_selectedAction == null)
            {
                ImGui.EndDisabled();
            }
            float actionColumnWidth = 200.0f;
            float mid = ImGui.GetCursorPosY();
            if (ImGui.BeginListBox("##Actions", new Vector2(actionColumnWidth, avail.Y - _adjusterYtl3)) == true)
            {
                if (_selectedReaction != null)
                {
                    foreach (Core.Action a in _selectedReaction.ActionList)
                    {
                        if (ImGui.Selectable(a.Describe() + "##" + a.Id, _selectedAction == a) == true)
                        {
                            _selectedAction = a;
                        }
                    }
                }
                ImGui.EndListBox();
            }
            _adjusterYtl3 += ImGui.GetCursorPosY() - avail.Y - start;
            ImGuiStylePtr style = ImGui.GetStyle();
            ImGui.Indent(actionColumnWidth + style.ItemSpacing.X);
            ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPosX(), mid));
            if (_selectedAction == null)
            {
                ImGui.BeginDisabled();
            }            
            RenderActionSettings(_selectedAction);
            if (_selectedAction == null)
            {
                ImGui.EndDisabled();
            }
            ImGui.Unindent(actionColumnWidth + style.ItemSpacing.X);
            if (ImGui.BeginPopup("NewReactionAction") == true)
            {
                ImGui.Text(I18n.Translate("Timelines/ActionTypes") + ":");
                if (ImGui.BeginListBox("##NewReactionActionLb", new Vector2(200.0f, 100.0f)) == true)
                {
                    foreach (ActionTypeItem ati in ActionTypes)
                    {
                        if (ImGui.Selectable(ati.Instance.Describe()) == true)
                        {
                            Core.Action a = ati.Instance.Duplicate();
                            _selectedReaction.ActionList.Add(a);
                            _selectedAction = a;
                            ImGui.CloseCurrentPopup();
                        }
                    }
                    ImGui.EndListBox();
                }
                ImGui.EndPopup();
            }
        }

        private void RenderTimelineActionsSettings(float x, float y)
        {
            ImGui.Indent(x);
            ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPosX(), y));
            ImGui.TextWrapped(I18n.Translate("Timelines/ReactionSettings/Triggers"));
            Vector2 avail = ImGui.GetContentRegionAvail();
            ImGui.PushItemWidth(avail.X);
            string selname = SelectedReaction != null ? I18n.Translate("Timelines/ReactionSettings/Triggers/" + SelectedReaction.Trigger.ToString()) : "";
            if (ImGui.BeginCombo("##SelTrigger" + (SelectedReaction != null ? SelectedReaction.Id.ToString() : ""), selname) == true)
            {
                foreach (Timeline.Reaction.ReactionTriggerEnum rt in Enum.GetValues(typeof(Timeline.Reaction.ReactionTriggerEnum)))             
                {
                    switch (rt)
                    {
                        case Timeline.Reaction.ReactionTriggerEnum.OnCastBegin:
                        case Timeline.Reaction.ReactionTriggerEnum.OnCastEnd:
                            if (SelectedEntry.Type != Timeline.Entry.EntryTypeEnum.Ability)
                            {
                                continue;
                            }
                            break;
                        case Timeline.Reaction.ReactionTriggerEnum.Targettable:
                            if (SelectedEntry.Type != Timeline.Entry.EntryTypeEnum.Targettable)
                            {
                                continue;
                            }
                            break;
                        case Timeline.Reaction.ReactionTriggerEnum.Untargettable:
                            if (SelectedEntry.Type != Timeline.Entry.EntryTypeEnum.Untargettable)
                            {
                                continue;
                            }
                            break;
                        case Timeline.Reaction.ReactionTriggerEnum.Spawn:
                            if (SelectedEntry.Type != Timeline.Entry.EntryTypeEnum.Spawn)
                            {
                                continue;
                            }
                            break;
                    }
                    string name = I18n.Translate("Timelines/ReactionSettings/Triggers/" + rt.ToString());
                    if (ImGui.Selectable(name, String.Compare(name, selname) == 0) == true)
                    {
                        SelectedReaction.Trigger = rt;
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.PopItemWidth();
            float ofsmin = SelectedReaction != null ? SelectedReaction.TimeWindowStart : 0.0f;
            float ofsmax = SelectedReaction != null ? SelectedReaction.TimeWindowEnd : 0.0f;
            if (SelectedReaction != null)
            {
                ImGui.TextWrapped(Environment.NewLine + I18n.Translate("Timelines/ReactionSettings/TimeOffset"));
                ImGui.PushItemWidth(200.0f);
                switch (SelectedReaction.Trigger)
                {
                    case Timeline.Reaction.ReactionTriggerEnum.Timed:
                        if (ImGui.DragFloatRange2(I18n.Translate("MainMenu/Settings/AutomarkerSeconds") + "##Timelines/ReactionSettings/TimeOffset", ref ofsmin, ref ofsmax, 0.1f, -30.0f, 30.0f) == true)
                        {
                            SelectedReaction.TimeWindowStart = ofsmin;
                            SelectedReaction.TimeWindowEnd = ofsmax;
                        }
                        break;
                    case Timeline.Reaction.ReactionTriggerEnum.OnCastEnd:
                    case Timeline.Reaction.ReactionTriggerEnum.OnCastBegin:
                    case Timeline.Reaction.ReactionTriggerEnum.Targettable:
                    case Timeline.Reaction.ReactionTriggerEnum.Untargettable:
                    case Timeline.Reaction.ReactionTriggerEnum.Spawn:
                        if (ofsmin < 0.0f)
                        {
                            ofsmin = 0.0f;
                            SelectedReaction.TimeWindowStart = 0.0f;
                        }
                        if (ofsmax < ofsmin)
                        {
                            ofsmax = ofsmin;
                            SelectedReaction.TimeWindowEnd = ofsmin;
                        }
                        if (ImGui.DragFloatRange2(I18n.Translate("MainMenu/Settings/AutomarkerSeconds") + "##Timelines/ReactionSettings/TimeOffset", ref ofsmin, ref ofsmax, 0.1f, 0.0f, 30.0f) == true)
                        {
                            SelectedReaction.TimeWindowStart = ofsmin;
                            SelectedReaction.TimeWindowEnd = ofsmax;
                        }
                        break;
                }
                ImGui.PopItemWidth();
            }
            ImGui.Text("");
            ImGui.BeginDisabled();
            ImGui.CollapsingHeader("  " + I18n.Translate("Timelines/Actions"), ImGuiTreeNodeFlags.Leaf);
            ImGui.EndDisabled();
            RenderTimelineActionsList();
            ImGui.Unindent(0.0f - x);
        }

        private void RenderIngameCommandActionSettings(Action.IngameCommand n)
        {
            string temp = n.Command;
            ImGui.TextWrapped(Environment.NewLine + I18n.Translate("Timelines/IngameCommandText"));
            if (ImGui.InputText("##IngameCommandText", ref temp, 256) == true)
            {
                n.Command = temp;
            }
        }

        private void RenderChatMessageActionSettings(Action.ChatMessage n)
        {
            string temp = n.Text;
            ImGui.TextWrapped(Environment.NewLine + I18n.Translate("Timelines/ChatMessageText"));
            if (ImGui.InputText("##ChatMessageText", ref temp, 256) == true)
            {
                n.Text = temp;
            }
            ImGui.TextWrapped(Environment.NewLine + I18n.Translate("Timelines/ChatMessageSeverity"));
            string selname = I18n.Translate("Timelines/ChatMessageSeverity/" + n.ChatSeverity.ToString());
            if (ImGui.BeginCombo("##ChatSeverity" + n.Id.ToString(), selname) == true)
            {
                foreach (Action.ChatMessage.ChatSeverityEnum cs in Enum.GetValues(typeof(Action.ChatMessage.ChatSeverityEnum)))
                {
                    string name = I18n.Translate("Timelines/ChatMessageSeverity/" + cs.ToString());
                    if (ImGui.Selectable(name, String.Compare(name, selname) == 0) == true)
                    {
                        n.ChatSeverity = cs;
                    }
                }
                ImGui.EndCombo();
            }
        }

        private void RenderNotificationActionSettings(Action.Notification n)
        {
            string temp = n.Text;
            ImGui.TextWrapped(Environment.NewLine + I18n.Translate("Timelines/NotificationText"));
            if (ImGui.InputText("##NotificationText", ref temp, 256) == true)
            {
                n.Text = temp;
            }
            bool tts = n.TTS;
            if (ImGui.Checkbox(I18n.Translate("Timelines/NotificationTTS"), ref tts) == true)
            {
                n.TTS = tts;
            }
            ImGui.TextWrapped(Environment.NewLine + I18n.Translate("Timelines/NotificationSeverity"));
            string selname = I18n.Translate("Timelines/NotificationSeverity/" + n.NotificationSeverity.ToString());
            if (ImGui.BeginCombo("##NotificationSeverity" + n.Id.ToString(), selname) == true)
            {
                foreach (Notification.NotificationSeverityEnum cs in Enum.GetValues(typeof(Notification.NotificationSeverityEnum)))
                {
                    string name = I18n.Translate("Timelines/NotificationSeverity/" + cs.ToString());
                    if (ImGui.Selectable(name, String.Compare(name, selname) == 0) == true)
                    {
                        n.NotificationSeverity = cs;
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.TextWrapped(Environment.NewLine + I18n.Translate("Timelines/NotificationSoundEffect"));
            selname = I18n.Translate("SoundEffect/" + n.SoundEffect.ToString());
            if (ImGui.BeginCombo("##NotificationSoundEffect" + _selectedAction.Id.ToString(), selname) == true)
            {
                foreach (SoundEffectEnum se in Enum.GetValues(typeof(SoundEffectEnum)))
                {
                    string name = I18n.Translate("SoundEffect/" + se.ToString());
                    if (ImGui.Selectable(name, String.Compare(name, selname) == 0) == true)
                    {
                        n.SoundEffect = se;
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.TextWrapped(Environment.NewLine + I18n.Translate("Timelines/NotificationTTL"));
            float ttl = n.TTL;
            if (ImGui.DragFloat("##NotificationTTL", ref ttl, 0.1f, 1.0f, 30.0f, "%.1f", ImGuiSliderFlags.AlwaysClamp) == true)
            {
                n.TTL = ttl;
            }            
        }

        private void RenderActionSettings(Core.Action a)
        {
            if (a == null)
            {
                return;
            }
            Vector2 avail = ImGui.GetContentRegionAvail();
            ImGui.PushItemWidth(avail.X);
            if (a is Action.Notification)
            {
                RenderNotificationActionSettings((Action.Notification)a);
            }
            if (a is Action.ChatMessage)
            {
                RenderChatMessageActionSettings((Action.ChatMessage)a);
            }
            if (a is Action.IngameCommand)
            {
                RenderIngameCommandActionSettings((Action.IngameCommand)a);
            }
            ImGui.PopItemWidth();
        }

        private void RenderTimelineContentSettings()
        {
            RenderTimelineContentHeader();
            Timeline tl = SelectedTimeline;
            if (tl == null)
            {
                ImGui.BeginDisabled();
            }
            ImGui.BeginTabBar("LemmyTimelineTabs");
            if (ImGui.BeginTabItem(I18n.Translate("Timelines/ProfileDetails")) == true)
            {
                ImGui.BeginChild("Timelines/ProfileDetails");
                RenderTimelineProfileSettings();
                ImGui.EndChild();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem(I18n.Translate("Timelines/EventDetails")) == true)
            {
                ImGui.BeginChild("Timelines/EventDetails");
                float topx = RenderTimelineContentEvents();
                RenderTimelineEventSettings(topx);
                ImGui.EndChild();
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
            if (tl == null)
            {
                ImGui.EndDisabled();
            }
        }

        private void RenderTimelineOverlaySettings()
        {
            _renderTimelineOverlay = true;
            _timelineOverlayConfig = true;
            _ui.RenderWarning(I18n.Translate("Timelines/PreviewActive"));
            ImGui.Text("");
            bool tlvisible = _state.cfg.TimelineOverlayVisible;
            if (ImGui.Checkbox(I18n.Translate("Timelines/Settings/ShowOverlay"), ref tlvisible) == true)
            {
                _state.cfg.TimelineOverlayVisible = tlvisible;
            }
            ImGui.Text("");
            Vector4 barcolor = _state.cfg.TimelineOverlayBarColor;
            if (ImGui.ColorEdit4(I18n.Translate("Timelines/Settings/BarColor"), ref barcolor, ImGuiColorEditFlags.NoInputs) == true)
            {
                _state.cfg.TimelineOverlayBarColor = barcolor;
            }
            Vector4 barcolortx = _state.cfg.TimelineOverlayBarTextColor;
            if (ImGui.ColorEdit4(I18n.Translate("Timelines/Settings/BarTextColor"), ref barcolortx, ImGuiColorEditFlags.NoInputs) == true)
            {
                _state.cfg.TimelineOverlayBarTextColor = barcolortx;
            }
            ImGui.Text("");
            Vector4 sooncolor = _state.cfg.TimelineOverlayBarSoonColor;
            if (ImGui.ColorEdit4(I18n.Translate("Timelines/Settings/BarSoonColor"), ref sooncolor, ImGuiColorEditFlags.NoInputs) == true)
            {
                _state.cfg.TimelineOverlayBarSoonColor = sooncolor;
            }
            Vector4 sooncolortx = _state.cfg.TimelineOverlayBarSoonTextColor;
            if (ImGui.ColorEdit4(I18n.Translate("Timelines/Settings/BarSoonTextColor"), ref sooncolortx, ImGuiColorEditFlags.NoInputs) == true)
            {
                _state.cfg.TimelineOverlayBarSoonTextColor = sooncolortx;
            }
            ImGui.Text("");
            Vector4 actcolor = _state.cfg.TimelineOverlayBarActiveColor;
            if (ImGui.ColorEdit4(I18n.Translate("Timelines/Settings/BarActiveColor"), ref actcolor, ImGuiColorEditFlags.NoInputs) == true)
            {
                _state.cfg.TimelineOverlayBarActiveColor = actcolor;
            }
            Vector4 actcolortx = _state.cfg.TimelineOverlayBarActiveTextColor;
            if (ImGui.ColorEdit4(I18n.Translate("Timelines/Settings/BarActiveTextColor"), ref actcolortx, ImGuiColorEditFlags.NoInputs) == true)
            {
                _state.cfg.TimelineOverlayBarActiveTextColor = actcolortx;
            }
            ImGui.Text("");
            Vector4 barbgcolor = _state.cfg.TimelineOverlayBarBgColor;
            if (ImGui.ColorEdit4(I18n.Translate("Timelines/Settings/BarBgColor"), ref barbgcolor, ImGuiColorEditFlags.NoInputs) == true)
            {
                _state.cfg.TimelineOverlayBarBgColor = barbgcolor;
            }
            ImGui.Text("");
            bool barhead = _state.cfg.TimelineOverlayBarHead;
            if (ImGui.Checkbox(I18n.Translate("Timelines/Settings/ShowBarHead"), ref barhead) == true)
            {
                _state.cfg.TimelineOverlayBarHead = barhead;
            }
            bool barname = _state.cfg.TimelineOverlayBarName;
            if (ImGui.Checkbox(I18n.Translate("Timelines/Settings/ShowBarName"), ref barname) == true)
            {
                _state.cfg.TimelineOverlayBarName = barname;
            }
            bool bartime = _state.cfg.TimelineOverlayBarTime;
            if (ImGui.Checkbox(I18n.Translate("Timelines/Settings/ShowBarTime"), ref bartime) == true)
            {
                _state.cfg.TimelineOverlayBarTime = bartime;
            }
            bool barcaps = _state.cfg.TimelineOverlayBarCaps;
            if (ImGui.Checkbox(I18n.Translate("Timelines/Settings/ShowBarCaps"), ref barcaps) == true)
            {
                _state.cfg.TimelineOverlayBarCaps = barcaps;
            }
            bool tldebug = _state.cfg.TimelineOverlayDebug;
            if (ImGui.Checkbox(I18n.Translate("Timelines/Settings/ShowDebug"), ref tldebug) == true)
            {
                _state.cfg.TimelineOverlayDebug = tldebug;
            }
            Vector2 sz = ImGui.GetContentRegionAvail();
            ImGui.PushItemWidth(sz.X);
            ImGui.Text(Environment.NewLine + I18n.Translate("Timelines/Settings/BarStyle"));
            Config.TimelineBarStyleEnum barstyle = _state.cfg.TimelineOverlayBarStyle;
            string styletr = I18n.Translate("Timelines/Settings/BarStyles/" + barstyle);
            if (ImGui.BeginCombo("##Timelines/Settings/BarStyle", styletr) == true)
            {
                foreach (string name in Enum.GetNames(typeof(Config.TimelineBarStyleEnum)))
                {
                    string estr = I18n.Translate("Timelines/Settings/BarStyles/" + name);
                    if (ImGui.Selectable(estr, String.Compare(styletr, estr) == 0) == true)
                    {
                        Config.TimelineBarStyleEnum newstyle = (Config.TimelineBarStyleEnum)Enum.Parse(typeof(Config.TimelineBarStyleEnum), name);
                        _state.cfg.TimelineOverlayBarStyle = newstyle;
                    }
                }
                ImGui.EndCombo();
            }
            float timethr = _state.cfg.TimelineOverlayBarTimeThreshold;
            ImGui.Text(Environment.NewLine + I18n.Translate("Timelines/Settings/TimeThreshold"));
            if (ImGui.SliderFloat("##TimeThreshold", ref timethr, 10.0f, 60.0f, "%.1f") == true)
            {
                _state.cfg.TimelineOverlayBarTimeThreshold = timethr;
            }
            float soonthr = _state.cfg.TimelineOverlayBarSoonThreshold;
            ImGui.Text(Environment.NewLine + I18n.Translate("Timelines/Settings/SoonThreshold"));
            if (ImGui.SliderFloat("##SoonThreshold", ref soonthr, 0.0f, 30.0f, "%.1f") == true)
            {
                _state.cfg.TimelineOverlayBarSoonThreshold = soonthr;
            }
            float pastthr = _state.cfg.TimelineOverlayBarPastThreshold;
            ImGui.Text(Environment.NewLine + I18n.Translate("Timelines/Settings/PastThreshold"));
            if (ImGui.SliderFloat("##PastThreshold", ref pastthr, 0.0f, 10.0f, "%.1f") == true)
            {
                _state.cfg.TimelineOverlayBarPastThreshold = pastthr;
            }
            float barheight = _state.cfg.TimelineOverlayBarHeight;
            ImGui.Text(Environment.NewLine + I18n.Translate("Timelines/Settings/BarHeight"));
            if (ImGui.SliderFloat("##BarHeight", ref barheight, 10.0f, 60.0f, "%.1f") == true)
            {
                _state.cfg.TimelineOverlayBarHeight = barheight;
            }
            float textscale = _state.cfg.TimelineOverlayBarTextScale;
            ImGui.Text(Environment.NewLine + I18n.Translate("Timelines/Settings/BarTextScale"));
            if (ImGui.SliderFloat("##BarTextScale", ref textscale, 0.5f, 3.0f, ((int)Math.Floor(textscale * 100.0f)).ToString() + " %%") == true)
            {
                _state.cfg.TimelineOverlayBarTextScale = textscale;
            }
            ImGui.PopItemWidth();
        }

        private void RenderNotificationOverlaySettings()
        {
            _renderNotificationOverlay = true;
            _notificationOverlayConfig = true;
            _ui.RenderWarning(I18n.Translate("Notifications/PreviewActive"));
            ImGui.Text("");
            bool olvisible = _state.cfg.NotificationOverlayVisible;
            if (ImGui.Checkbox(I18n.Translate("Notifications/Settings/ShowOverlay"), ref olvisible) == true)
            {
                _state.cfg.NotificationOverlayVisible = olvisible;
            }
            ImGui.Text("");
            Vector4 crittext = _state.cfg.NotificationOverlayCriticalTextColor;
            if (ImGui.ColorEdit4(I18n.Translate("Notifications/Settings/CriticalTextColor"), ref crittext, ImGuiColorEditFlags.NoInputs) == true)
            {
                _state.cfg.NotificationOverlayCriticalTextColor = crittext;
            }
            Vector4 critbg = _state.cfg.NotificationOverlayCriticalBgColor;
            if (ImGui.ColorEdit4(I18n.Translate("Notifications/Settings/CriticalBgColor"), ref critbg, ImGuiColorEditFlags.NoInputs) == true)
            {
                _state.cfg.NotificationOverlayCriticalBgColor = critbg;
            }
            ImGui.Text("");
            Vector4 imptext = _state.cfg.NotificationOverlayImportantTextColor;
            if (ImGui.ColorEdit4(I18n.Translate("Notifications/Settings/ImportantTextColor"), ref imptext, ImGuiColorEditFlags.NoInputs) == true)
            {
                _state.cfg.NotificationOverlayImportantTextColor = imptext;
            }
            Vector4 impbg = _state.cfg.NotificationOverlayImportantBgColor;
            if (ImGui.ColorEdit4(I18n.Translate("Notifications/Settings/ImportantBgColor"), ref impbg, ImGuiColorEditFlags.NoInputs) == true)
            {
                _state.cfg.NotificationOverlayImportantBgColor = impbg;
            }
            ImGui.Text("");
            Vector4 normtext = _state.cfg.NotificationOverlayNormalTextColor;
            if (ImGui.ColorEdit4(I18n.Translate("Notifications/Settings/NormalTextColor"), ref normtext, ImGuiColorEditFlags.NoInputs) == true)
            {
                _state.cfg.NotificationOverlayNormalTextColor = normtext;
            }
            Vector4 normbg = _state.cfg.NotificationOverlayNormalBgColor;
            if (ImGui.ColorEdit4(I18n.Translate("Notifications/Settings/NormalBgColor"), ref normbg, ImGuiColorEditFlags.NoInputs) == true)
            {
                _state.cfg.NotificationOverlayNormalBgColor = normbg;
            }
            Vector2 sz = ImGui.GetContentRegionAvail();
            bool ntorder = _state.cfg.NotificationOrderReverse;
            ImGui.Text("");
            if (ImGui.Checkbox(I18n.Translate("Notifications/Settings/NotificationOrderReverse"), ref ntorder) == true)
            {
                _state.cfg.NotificationOrderReverse = ntorder;
            }
            ImGui.PushItemWidth(sz.X);
            Config.TextAlignmentEnum alignm = _state.cfg.NotificationEntryAlignment;
            ImGui.Text(Environment.NewLine + I18n.Translate("Notifications/Settings/TextAlignments"));
            string styletr = I18n.Translate("Notifications/Settings/TextAlignments/" + alignm);
            if (ImGui.BeginCombo("##Notifications/Settings/BarStyle", styletr) == true)
            {
                foreach (string name in Enum.GetNames(typeof(Config.TextAlignmentEnum)))
                {
                    string estr = I18n.Translate("Notifications/Settings/TextAlignments/" + name);
                    if (ImGui.Selectable(estr, String.Compare(styletr, estr) == 0) == true)
                    {
                        Config.TextAlignmentEnum newalignm = (Config.TextAlignmentEnum)Enum.Parse(typeof(Config.TextAlignmentEnum), name);
                        _state.cfg.NotificationEntryAlignment = newalignm;
                    }
                }
                ImGui.EndCombo();
            }
            float ntmargin = _state.cfg.NotificationOverlayEntryMargin;
            ImGui.Text(Environment.NewLine + I18n.Translate("Notifications/Settings/EntryMargin"));
            if (ImGui.SliderFloat("##NotificationMargin", ref ntmargin, 0.0f, 60.0f, "%.0f") == true)
            {
                _state.cfg.NotificationOverlayEntryMargin = ntmargin;
            }
            float ntheight = _state.cfg.NotificationOverlayEntryHeight;
            ImGui.Text(Environment.NewLine + I18n.Translate("Notifications/Settings/EntryHeight"));
            if (ImGui.SliderFloat("##NotificationHeight", ref ntheight, 10.0f, 60.0f, "%.1f") == true)
            {
                _state.cfg.NotificationOverlayEntryHeight = ntheight;
            }
            float textscale = _state.cfg.NotificationOverlayTextScale;
            ImGui.Text(Environment.NewLine + I18n.Translate("Notifications/Settings/TextScale"));
            if (ImGui.SliderFloat("##NotificationTextScale", ref textscale, 0.5f, 3.0f, ((int)Math.Floor(textscale * 100.0f)).ToString() + " %%") == true)
            {
                _state.cfg.NotificationOverlayTextScale = textscale;
            }
            ImGui.PopItemWidth();
        }

        private float RenderTimelineDebug(ImDrawListPtr draw, float x, float y, float width, Timeline tl)
        {
            draw.AddRectFilled(
                new Vector2(x, y),
                new Vector2(x + width, y + _state.cfg.TimelineOverlayBarHeight),
                ImGui.GetColorU32(_state.cfg.TimelineOverlayBarBgColor)
            );
            string text = String.Format("E {0}\t{1}\tT {2:0.000}\tS {3:0.000}\tJ {4:0.000}", tl.CurrentEncounter != null ? tl.CurrentEncounter.Id.ToString() : "(null)", tl.Status, tl.CurrentTime, tl.AutoSync, tl.LastJumpPoint);
            ImFontPtr font = ImGui.GetFont();
            float fontsize = ImGui.GetFontSize();
            float scale = _state.cfg.TimelineOverlayBarTextScale;
            Vector2 sz;
            sz = ImGui.CalcTextSize(text);
            sz = new Vector2(sz.X * scale, sz.Y * scale);
            draw.AddText(
                font,
                fontsize * scale,
                new Vector2(x + 10.0f, y + (_state.cfg.TimelineOverlayBarHeight / 2.0f) - (sz.Y / 2.0f)),
                ImGui.GetColorU32(_state.cfg.TimelineOverlayBarTextColor),
                text
            );
            return _state.cfg.TimelineOverlayBarHeight;
        }

        private float RenderTimelineBar(ImDrawListPtr draw, float x, float y, float width, float lastSync, float currentTime, Timeline.Entry e)
        {
            float progress;
            float time;
            float ratio = 1.0f;
            float yoffset = _state.cfg.TimelineOverlayBarHeight;
            bool active = false;
            bool passed = false;
            Vector4 maincol, textcol;
            if (e.StartTime > currentTime)
            {
                float timethr = _state.cfg.TimelineOverlayBarTimeThreshold;
                if (currentTime - lastSync < timethr && lastSync > -1.0f)
                {
                    timethr = e.StartTime - lastSync;
                }
                time = e.StartTime - currentTime;
                if (time < _state.cfg.TimelineOverlayBarSoonThreshold && currentTime > 0.0f)
                {
                    maincol = _state.cfg.TimelineOverlayBarSoonColor;
                    textcol = _state.cfg.TimelineOverlayBarSoonTextColor;
                }
                else
                {
                    maincol = _state.cfg.TimelineOverlayBarColor;
                    textcol = _state.cfg.TimelineOverlayBarTextColor;
                }
                progress = Math.Clamp(1.0f - (time / timethr), 0.0f, 1.0f) * 100.0f;
            }
            else
            {
                time = e.Duration - (currentTime - e.StartTime);
                passed = (time < 0.0f);
                active = (passed == false);
                if (passed == true)
                {
                    progress = 0.0f;
                    ratio = 1.0f - (Math.Abs(time) / _state.cfg.TimelineOverlayBarPastThreshold);
                    yoffset *= ratio;
                }
                else
                {
                    progress = Math.Clamp(time / e.Duration, 0.0f, 1.0f) * -100.0f;
                }
                maincol = _state.cfg.TimelineOverlayBarActiveColor;
                textcol = _state.cfg.TimelineOverlayBarActiveTextColor;
            }
            float x1, x2, x3;
            if (progress < 0.0f)
            {
                x1 = x + width;
                x2 = x1 - (width * progress / -100.0f);
                x3 = x;
            }
            else
            {
                x1 = x;
                x2 = x1 + (width * progress / 100.0f);
                x3 = x1 + width;
            }
            draw.AddRectFilled(
                new Vector2(x2, y),
                new Vector2(x3, y + _state.cfg.TimelineOverlayBarHeight),
                ImGui.GetColorU32(new Vector4(
                    _state.cfg.TimelineOverlayBarBgColor.X,
                    _state.cfg.TimelineOverlayBarBgColor.Y,
                    _state.cfg.TimelineOverlayBarBgColor.Z,
                    _state.cfg.TimelineOverlayBarBgColor.W * ratio
                ))
            );
            switch (_state.cfg.TimelineOverlayBarStyle)
            {
                case Config.TimelineBarStyleEnum.Solid:
                    {
                        draw.AddRectFilled(
                            new Vector2(x1, y),
                            new Vector2(x2, y + _state.cfg.TimelineOverlayBarHeight),
                            ImGui.GetColorU32(maincol)
                        );
                    }
                    break;
                case Config.TimelineBarStyleEnum.Fancy:
                    {
                        Vector4 shadow = new Vector4(
                            Math.Clamp(maincol.X * 0.5f, 0.0f, 1.0f),
                            Math.Clamp(maincol.Y * 0.5f, 0.0f, 1.0f),
                            Math.Clamp(maincol.Z * 0.5f, 0.0f, 1.0f),
                            maincol.W
                        );
                        Vector4 hilite = new Vector4(
                            Math.Clamp(maincol.X + 0.2f, 0.0f, 1.0f),
                            Math.Clamp(maincol.Y + 0.2f, 0.0f, 1.0f),
                            Math.Clamp(maincol.Z + 0.2f, 0.0f, 1.0f),
                            maincol.W
                        );
                        float yt = y + (_state.cfg.TimelineOverlayBarHeight * 0.3f);
                        draw.AddRectFilledMultiColor(
                            new Vector2(x1, y),
                            new Vector2(x2, yt),
                            ImGui.GetColorU32(maincol),
                            ImGui.GetColorU32(maincol),
                            ImGui.GetColorU32(hilite),
                            ImGui.GetColorU32(hilite)
                        );
                        draw.AddRectFilledMultiColor(
                            new Vector2(x1, yt),
                            new Vector2(x2, y + _state.cfg.TimelineOverlayBarHeight),
                            ImGui.GetColorU32(hilite),
                            ImGui.GetColorU32(hilite),
                            ImGui.GetColorU32(shadow),
                            ImGui.GetColorU32(shadow)
                        );
                    }
                    break;
            }
            if (_state.cfg.TimelineOverlayBarHead == true && passed == false)
            {
                float xh = x2 - 15.0f;
                float xs = x2 + 15.0f;
                if (progress < 0.0f)
                {
                    xh = x2 + 15.0f;
                    xs = x2 - 15.0f;
                }
                Vector4 hilite = new Vector4(
                    Math.Clamp(maincol.X + 0.5f, 0.0f, 1.0f),
                    Math.Clamp(maincol.Y + 0.5f, 0.0f, 1.0f),
                    Math.Clamp(maincol.Z + 0.5f, 0.0f, 1.0f),
                    maincol.W
                );
                draw.AddRectFilledMultiColor(
                    new Vector2(xh, y),
                    new Vector2(x2, y + _state.cfg.TimelineOverlayBarHeight),
                    ImGui.GetColorU32(new Vector4(hilite.X, hilite.Y, hilite.Z, 0.0f)),
                    ImGui.GetColorU32(new Vector4(hilite.X, hilite.Y, hilite.Z, 0.5f)),
                    ImGui.GetColorU32(new Vector4(hilite.X, hilite.Y, hilite.Z, 0.5f)),
                    ImGui.GetColorU32(new Vector4(hilite.X, hilite.Y, hilite.Z, 0.0f))
                );
                draw.AddRectFilledMultiColor(
                    new Vector2(xs, y),
                    new Vector2(x2, y + _state.cfg.TimelineOverlayBarHeight),
                    ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 0.0f)),
                    ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 0.5f)),
                    ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 0.5f)),
                    ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 0.0f))
                );
            }
            draw.AddRect(
                new Vector2(x, y), 
                new Vector2(x + width, y + _state.cfg.TimelineOverlayBarHeight), 
                ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, ratio)), 
                0.0f, ImDrawFlags.None, 1.0f
            );
            ImFontPtr font = ImGui.GetFont();
            float fontsize = ImGui.GetFontSize();
            float scale = _state.cfg.TimelineOverlayBarTextScale;
            Vector2 sz;
            if (_state.cfg.TimelineOverlayBarName == true)
            {
                float trans = textcol.W;
                float xofs = 0.0f;
                string text = GetRotatingNameForTimelineEntry(currentTime, e);
                if (e.KeyValues.Count > 1)
                {
                    float ttime = ((float)(DateTime.Now - _loaded).TotalSeconds / 2.0f) % 1.0f;
                    if (ttime < 0.2f)
                    {
                        trans = 1.0f - ((0.2f - ttime) / 0.2f);
                        xofs = (1.0f - trans) * (width / 10.0f);
                    }
                    if (ttime > 0.8f)
                    {
                        trans = (1.0f - ttime) / 0.2f;
                        xofs = (1.0f - trans) * (width / -10.0f);
                    }
                }
                if (passed == true)
                {
                    trans *= ratio;
                    xofs += (1.0f - ratio) * (width / -10.0f);
                }
                if (_state.cfg.TimelineOverlayBarCaps == true)
                {
                    text = text.ToUpper();
                }
                sz = ImGui.CalcTextSize(text);
                sz = new Vector2(sz.X * scale, sz.Y * scale);
                draw.AddText(
                    font,
                    fontsize * scale,
                    new Vector2(x + 11.0f + xofs, y + (_state.cfg.TimelineOverlayBarHeight / 2.0f) - (sz.Y / 2.0f) + 1.0f),
                    ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, trans)),
                    text
                );
                draw.AddText(
                    font,
                    fontsize * scale,
                    new Vector2(x + 10.0f + xofs, y + (_state.cfg.TimelineOverlayBarHeight / 2.0f) - (sz.Y / 2.0f)),
                    ImGui.GetColorU32(new Vector4(textcol.X, textcol.Y, textcol.Z, trans)),
                    text
                );
            }
            if (_state.cfg.TimelineOverlayBarTime == true && passed == false)
            {
                string temp = String.Format("{0:0.0}", time);
                sz = ImGui.CalcTextSize(temp);
                sz = new Vector2(sz.X * scale, sz.Y * scale);
                draw.AddText(
                    font,
                    fontsize * scale,
                    new Vector2(x + width - (sz.X + 11.0f), y + (_state.cfg.TimelineOverlayBarHeight / 2.0f) - (sz.Y / 2.0f) + 1.0f),
                    ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, textcol.W)),
                    temp
                );
                draw.AddText(
                    font,
                    fontsize * scale,
                    new Vector2(x + width - (sz.X + 10.0f), y + (_state.cfg.TimelineOverlayBarHeight / 2.0f) - (sz.Y / 2.0f)),
                    ImGui.GetColorU32(textcol),
                    temp
                );
            }
            return yoffset;
        }

        private void RenderTimelineOverlay()
        {
            Timeline st = _state._timeline;
            if (st == null && _timelineOverlayConfig == false)
            { 
                return;
            }
            ImGuiWindowFlags flax = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBackground;
            if (_timelineOverlayConfig == false)
            {
                flax |= ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;
            }
            ImGui.SetNextWindowPos(new Vector2(
                _state.cfg.TimelineOverlayLocation.Left,
                _state.cfg.TimelineOverlayLocation.Top),
                ImGuiCond.FirstUseEver
            );
            ImGui.SetNextWindowSize(new Vector2(
                _state.cfg.TimelineOverlayLocation.Right - _state.cfg.TimelineOverlayLocation.Left,
                _state.cfg.TimelineOverlayLocation.Bottom - _state.cfg.TimelineOverlayLocation.Top),
                ImGuiCond.FirstUseEver
            );
            if (ImGui.Begin("TimelineOverlay", flax) == true)
            {
                UserInterface.KeepWindowInSight();
                Vector2 pt = ImGui.GetWindowPos();
                Vector2 szy = ImGui.GetWindowSize();
                ImDrawListPtr draw = ImGui.GetWindowDrawList();
                if (_timelineOverlayConfig == true)
                {
                    Timeline.Entry e = new Timeline.Entry();
                    float time = (float)((DateTime.Now - _loaded).TotalMilliseconds / 600.0);
                    float y = 0.0f;
                    float bar = 0.0f;
                    int barnum = 1;
                    e.StartTime = _state.cfg.TimelineOverlayBarTimeThreshold;
                    e.Duration = 10.0f;
                    e.Type = Timeline.Entry.EntryTypeEnum.Timed;
                    float curTime;
                    if (_state.cfg.TimelineOverlayDebug == true && st != null)
                    {
                        y += RenderTimelineDebug(draw, pt.X, pt.Y, _state.cfg.TimelineOverlayLocation.Right - _state.cfg.TimelineOverlayLocation.Left, st);
                    }
                    while (y < _state.cfg.TimelineOverlayLocation.Bottom - _state.cfg.TimelineOverlayLocation.Top - _state.cfg.TimelineOverlayBarHeight)
                    {
                        float maxtime = e.StartTime + e.Duration + _state.cfg.TimelineOverlayBarPastThreshold;
                        curTime = (float)(((DateTime.Now - _loaded).TotalSeconds * 4.0f) + bar) % maxtime;
                        e.Description = I18n.Translate("Timelines/PreviewTestAbilityName") + " " + barnum;
                        y += RenderTimelineBar(
                            draw, pt.X, pt.Y + y, _state.cfg.TimelineOverlayLocation.Right - _state.cfg.TimelineOverlayLocation.Left,
                            -9999.0f, curTime, e
                        );                        
                        bar += 1.5f;
                        barnum++;
                    }
                    draw.AddRect(
                        new Vector2(_state.cfg.TimelineOverlayLocation.Left + 2.0f, _state.cfg.TimelineOverlayLocation.Top + 2.0f),
                        new Vector2(_state.cfg.TimelineOverlayLocation.Right - 2.0f, _state.cfg.TimelineOverlayLocation.Bottom - 2.0f),
                        ImGui.GetColorU32(new Vector4(1.0f, (float)Math.Abs(Math.Cos(time)), 0.0f, 1.0f)), 
                        0.0f, ImDrawFlags.None, 5.0f
                    );
                }
                else
                {
                    IEnumerable<Timeline.Entry> entries = st.PeekEntries(10, _state.cfg.TimelineOverlayBarPastThreshold, _state.cfg.TimelineOverlayBarTimeThreshold);
                    float y = 0.0f;
                    if (_state.cfg.TimelineOverlayDebug == true)
                    {
                        y += RenderTimelineDebug(draw, pt.X, pt.Y, _state.cfg.TimelineOverlayLocation.Right - _state.cfg.TimelineOverlayLocation.Left, st);
                    }
                    foreach (Timeline.Entry e in entries)
                    {
                        if (y >= _state.cfg.TimelineOverlayLocation.Bottom - _state.cfg.TimelineOverlayLocation.Top - _state.cfg.TimelineOverlayBarHeight)
                        {
                            break;
                        }
                        y += RenderTimelineBar(
                            draw, pt.X, pt.Y + y, _state.cfg.TimelineOverlayLocation.Right - _state.cfg.TimelineOverlayLocation.Left,
                            st.LastJumpPoint, st.CurrentTime, e
                        );
                    }
                }
                _state.cfg.TimelineOverlayLocation = new Rectangle() { Left = pt.X, Top = pt.Y, Right = pt.X + szy.X, Bottom = pt.Y + szy.Y };
                ImGui.End();
            }
        }

        private float RenderNotification(ImDrawListPtr draw, Notification n, float y)
        {
            float tm = (float)(DateTime.Now - n.SpawnTime).TotalSeconds;
            float alpha, xofs = 0.0f;
            float fadelengthin = 0.3f;
            float fadelengthout = 0.5f;
            float yscale = 1.0f;
            float glintx = 0.0f;
            if (tm < fadelengthin)
            {
                alpha = tm / fadelengthin;
                xofs = (1.0f - alpha) * ((_state.cfg.NotificationOverlayLocation.Right - _state.cfg.NotificationOverlayLocation.Left) / 10.0f);
            }
            else if (tm > n.TTL - fadelengthout)
            {
                alpha = (n.TTL - tm) / fadelengthout;
                xofs = (1.0f - alpha) * ((_state.cfg.NotificationOverlayLocation.Right - _state.cfg.NotificationOverlayLocation.Left) / 10.0f);
                yscale = alpha;
            }
            else
            {
                alpha = 1.0f;
            }
            Vector4 textcol = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            Vector4 bgcol = textcol;
            switch (n.Severity)
            {
                case Notification.NotificationSeverityEnum.Critical:
                    textcol = new Vector4(
                        _state.cfg.NotificationOverlayCriticalTextColor.X,
                        _state.cfg.NotificationOverlayCriticalTextColor.Y,
                        _state.cfg.NotificationOverlayCriticalTextColor.Z,
                        _state.cfg.NotificationOverlayCriticalTextColor.W * alpha
                    );
                    bgcol = new Vector4(
                        _state.cfg.NotificationOverlayCriticalBgColor.X,
                        _state.cfg.NotificationOverlayCriticalBgColor.Y,
                        _state.cfg.NotificationOverlayCriticalBgColor.Z,
                        _state.cfg.NotificationOverlayCriticalBgColor.W * alpha
                    );
                    break;
                case Notification.NotificationSeverityEnum.Important:
                    textcol = new Vector4(
                        _state.cfg.NotificationOverlayImportantTextColor.X,
                        _state.cfg.NotificationOverlayImportantTextColor.Y,
                        _state.cfg.NotificationOverlayImportantTextColor.Z,
                        _state.cfg.NotificationOverlayImportantTextColor.W * alpha
                    );
                    bgcol = new Vector4(
                        _state.cfg.NotificationOverlayImportantBgColor.X,
                        _state.cfg.NotificationOverlayImportantBgColor.Y,
                        _state.cfg.NotificationOverlayImportantBgColor.Z,
                        _state.cfg.NotificationOverlayImportantBgColor.W * alpha
                    );
                    break;
                case Notification.NotificationSeverityEnum.Normal:
                    textcol = new Vector4(
                        _state.cfg.NotificationOverlayNormalTextColor.X,
                        _state.cfg.NotificationOverlayNormalTextColor.Y,
                        _state.cfg.NotificationOverlayNormalTextColor.Z,
                        _state.cfg.NotificationOverlayNormalTextColor.W * alpha
                    );
                    bgcol = new Vector4(
                        _state.cfg.NotificationOverlayNormalBgColor.X,
                        _state.cfg.NotificationOverlayNormalBgColor.Y,
                        _state.cfg.NotificationOverlayNormalBgColor.Z,
                        _state.cfg.NotificationOverlayNormalBgColor.W * alpha
                    );
                    break;
            }
            float x = _state.cfg.NotificationOverlayLocation.Left;
            string text = n.ctx != null ? n.ctx.ParseText(n.Notif, n.Text) : n.Text;
            Vector2 sz = ImGui.CalcTextSize(text);
            float scale = _state.cfg.NotificationOverlayTextScale;
            float x1 = _state.cfg.NotificationOverlayLocation.Left;
            float x2 = _state.cfg.NotificationOverlayLocation.Left + ((_state.cfg.NotificationOverlayLocation.Right - _state.cfg.NotificationOverlayLocation.Left) * 0.25f);
            float x3 = _state.cfg.NotificationOverlayLocation.Right - ((_state.cfg.NotificationOverlayLocation.Right - _state.cfg.NotificationOverlayLocation.Left) * 0.25f);
            float x4 = _state.cfg.NotificationOverlayLocation.Right;
            switch (_state.cfg.NotificationEntryAlignment)
            {
                case Config.TextAlignmentEnum.Left:
                    x2 = x1;
                    x -= xofs;
                    x += _state.cfg.NotificationOverlayEntryMargin;
                    glintx = x1 + ((_state.cfg.NotificationOverlayLocation.Right - _state.cfg.NotificationOverlayLocation.Left) * alpha);
                    break;
                case Config.TextAlignmentEnum.Center:
                    x += ((_state.cfg.NotificationOverlayLocation.Right - _state.cfg.NotificationOverlayLocation.Left) / 2.0f) - ((sz.X * scale) / 2.0f);
                    glintx = ((x1 + x4) / 2.0f) + ((x4 - x1) / 2.0f * alpha);
                    break;
                case Config.TextAlignmentEnum.Right:
                    x += xofs;
                    x += (_state.cfg.NotificationOverlayLocation.Right - _state.cfg.NotificationOverlayLocation.Left) - (sz.X * scale);
                    x -= _state.cfg.NotificationOverlayEntryMargin;
                    x3 = x4;
                    glintx = x4 - ((_state.cfg.NotificationOverlayLocation.Right - _state.cfg.NotificationOverlayLocation.Left) * alpha);
                    break;
            }
            float fontSize = ImGui.GetFontSize() * scale;
            Vector4 bgfade = new Vector4(bgcol.X, bgcol.Y, bgcol.Z, 0.0f);
            if (_state.cfg.NotificationEntryAlignment != Config.TextAlignmentEnum.Left)
            {
                draw.AddRectFilledMultiColor(
                    new Vector2(x2, _state.cfg.NotificationOverlayLocation.Top + y),
                    new Vector2(x1, _state.cfg.NotificationOverlayLocation.Top + y + _state.cfg.NotificationOverlayEntryHeight),
                    ImGui.GetColorU32(bgfade),
                    ImGui.GetColorU32(bgfade),
                    ImGui.GetColorU32(bgfade),
                    ImGui.GetColorU32(bgcol)
                );
            }
            draw.AddRectFilledMultiColor(
                new Vector2(x2, _state.cfg.NotificationOverlayLocation.Top + y),
                new Vector2(x3, _state.cfg.NotificationOverlayLocation.Top + y + _state.cfg.NotificationOverlayEntryHeight),
                ImGui.GetColorU32(bgfade),
                ImGui.GetColorU32(bgfade),
                ImGui.GetColorU32(bgcol),
                ImGui.GetColorU32(bgcol)
            );
            if (_state.cfg.NotificationEntryAlignment != Config.TextAlignmentEnum.Right)
            {
                draw.AddRectFilledMultiColor(
                    new Vector2(x3, _state.cfg.NotificationOverlayLocation.Top + y),
                    new Vector2(x4, _state.cfg.NotificationOverlayLocation.Top + y + _state.cfg.NotificationOverlayEntryHeight),
                    ImGui.GetColorU32(bgfade),
                    ImGui.GetColorU32(bgfade),
                    ImGui.GetColorU32(bgfade),
                    ImGui.GetColorU32(bgcol)
                );
            }
            Vector4 txs = new Vector4(0.0f, 0.0f, 0.0f, textcol.W * alpha);
            draw.AddText(
                ImGui.GetFont(),
                fontSize,
                new Vector2(x - 1.0f, _state.cfg.NotificationOverlayLocation.Top + y + (_state.cfg.NotificationOverlayEntryHeight / 2.0f) - (sz.Y * scale / 2.0f)),
                ImGui.GetColorU32(txs),
                text
            );
            draw.AddText(
                ImGui.GetFont(),
                fontSize,
                new Vector2(x + 1.0f, _state.cfg.NotificationOverlayLocation.Top + y + (_state.cfg.NotificationOverlayEntryHeight / 2.0f) - (sz.Y * scale / 2.0f)),
                ImGui.GetColorU32(txs),
                text
            );
            draw.AddText(
                ImGui.GetFont(),
                fontSize,
                new Vector2(x, _state.cfg.NotificationOverlayLocation.Top + y + (_state.cfg.NotificationOverlayEntryHeight / 2.0f) - (sz.Y * scale / 2.0f)), 
                ImGui.GetColorU32(textcol),
                text
            );
            if (tm < fadelengthin)
            {
                if (_state.cfg.NotificationEntryAlignment == Config.TextAlignmentEnum.Center)
                {
                    float revx = glintx - ((_state.cfg.NotificationOverlayLocation.Right - _state.cfg.NotificationOverlayLocation.Left) * alpha);
                    draw.AddLine(
                        new Vector2(revx, _state.cfg.NotificationOverlayLocation.Top + y),
                        new Vector2(revx, _state.cfg.NotificationOverlayLocation.Top + y + _state.cfg.NotificationOverlayEntryHeight),
                        ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f - alpha, 1.0f - alpha)),
                        20.0f
                    );
                    draw.AddLine(
                        new Vector2(glintx, _state.cfg.NotificationOverlayLocation.Top + y),
                        new Vector2(glintx, _state.cfg.NotificationOverlayLocation.Top + y + _state.cfg.NotificationOverlayEntryHeight),
                        ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f - alpha, 1.0f - alpha)),
                        20.0f
                    );
                }
                else
                {
                    draw.AddLine(
                        new Vector2(glintx, _state.cfg.NotificationOverlayLocation.Top + y),
                        new Vector2(glintx, _state.cfg.NotificationOverlayLocation.Top + y + _state.cfg.NotificationOverlayEntryHeight),
                        ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f - alpha, 1.0f - alpha)),
                        40.0f
                    );
                }
            }
            return yscale * _state.cfg.NotificationOverlayEntryHeight;
        }

        private void TextToSpeech(string text)
        {
            try
            {
                text = text.Trim();
                if (text.Length == 0)
                {
                    return;
                }
                int temp;
                lock (this)
                {
                    temp = _ttsCounter;
                    _ttsCounter++;
                    if (_ttsCounter > 9)
                    {
                        _ttsCounter = 1;
                    }
                }
                string cmd =
                    $@"Add-Type -AssemblyName System.Speech; 
                    $speak = New-Object System.Speech.Synthesis.SpeechSynthesizer;
                    $speak.Rate = {_state.cfg.TTSSpeed};
                    $speak.Volume = {_state.cfg.TTSVolume};
                    $speak.Speak(""{text}"");";
                string path = Path.Combine(Path.GetTempPath(), String.Format("Lemegeton_ttsscript_{0}.ps1", _ttsCounter));
                using (StreamWriter sw = new StreamWriter(path, false, Encoding.UTF8))
                {
                    sw.Write(cmd);
                    ProcessStartInfo psi = new ProcessStartInfo()
                    {
                        FileName = @"powershell.exe",
                        LoadUserProfile = false,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        Arguments = $"-executionpolicy bypass -File {path}",
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    using Process p = Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                _state.Log(LogLevelEnum.Error, ex, "Exception during TTS");
            }
        }

        private void RenderNotificationOverlay()
        {            
            ImGuiWindowFlags flax = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBackground;
            if (_notificationOverlayConfig == false)
            {
                flax |= ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;
            }
            ImGui.SetNextWindowPos(new Vector2(
                _state.cfg.NotificationOverlayLocation.Left,
                _state.cfg.NotificationOverlayLocation.Top),
                ImGuiCond.FirstUseEver
            );
            ImGui.SetNextWindowSize(new Vector2(
                _state.cfg.NotificationOverlayLocation.Right - _state.cfg.NotificationOverlayLocation.Left,
                _state.cfg.NotificationOverlayLocation.Bottom - _state.cfg.NotificationOverlayLocation.Top),
                ImGuiCond.FirstUseEver
            );
            if (ImGui.Begin("NotificationOverlay", flax) == true)
            {
                UserInterface.KeepWindowInSight();
                Vector2 pt = ImGui.GetWindowPos();
                Vector2 szy = ImGui.GetWindowSize();
                ImDrawListPtr draw = ImGui.GetWindowDrawList();                
                if (_notificationOverlayConfig == true)
                {
                    Notification n = new Notification();
                    float y = 0.0f;
                    n.TTL = 5.0f;
                    int barnum = 1;
                    DateTime dt = DateTime.Now;
                    int ea = Enum.GetValues(typeof(Notification.NotificationSeverityEnum)).Length;
                    while (y < _state.cfg.NotificationOverlayLocation.Bottom - _state.cfg.NotificationOverlayLocation.Top - _state.cfg.NotificationOverlayEntryHeight)
                    {
                        double ptime = ((dt - _loaded).TotalSeconds + (barnum * 0.5)) % n.TTL;
                        Notification.NotificationSeverityEnum cs = (Notification.NotificationSeverityEnum)((barnum - 1) % ea);
                        n.Severity = cs;
                        n.Text = I18n.Translate("Notifications/TestNotification", I18n.Translate("Timelines/NotificationSeverity/" + cs));
                        n.SpawnTime = dt.AddSeconds(0.0f - ptime);
                        RenderNotification(draw, n, y);
                        y += _state.cfg.NotificationOverlayEntryHeight;
                        barnum++;
                    }
                    float time = (float)((DateTime.Now - _loaded).TotalMilliseconds / 600.0);
                    draw.AddRect(
                        new Vector2(_state.cfg.NotificationOverlayLocation.Left + 2.0f, _state.cfg.NotificationOverlayLocation.Top + 2.0f),
                        new Vector2(_state.cfg.NotificationOverlayLocation.Right - 2.0f, _state.cfg.NotificationOverlayLocation.Bottom - 2.0f),
                        ImGui.GetColorU32(new Vector4(1.0f, (float)Math.Abs(Math.Cos(time)), 0.0f, 1.0f)),
                        0.0f, ImDrawFlags.None, 5.0f
                    );
                }
                else
                {
                    float y = 0.0f;
                    IEnumerable<Notification> notifs;
                    if (_state.cfg.NotificationOrderReverse == true)
                    {
                        notifs = Notifications; 
                    }
                    else
                    {
                        notifs = Notifications.Reverse<Notification>();
                    }
                    foreach (Notification n in notifs)
                    {
                        if (y >= _state.cfg.NotificationOverlayLocation.Bottom - _state.cfg.NotificationOverlayLocation.Top - _state.cfg.NotificationOverlayEntryHeight)
                        {
                            break;
                        }
                        if (n.FirstDisplay == true)
                        {
                            n.FirstDisplay = false;                            
                            if (_state.cfg.QuickToggleSound == true)
                            {
                                if ((n.TTS == true || _state.cfg.TTSAllNotifications == true) && _state.cfg.TTSEnabled == true)
                                {
                                    TextToSpeech(n.ctx != null ? n.ctx.ParseText(n.Notif, n.Text) : n.Text);
                                }
                                if (n.SoundEffect != SoundEffectEnum.None)
                                {
                                    UIModule.PlayChatSoundEffect((uint)n.SoundEffect);
                                }
                            }
                        }
                        if (n.Text != "")
                        {
                            y += RenderNotification(draw, n, y);
                        }
                        else
                        {
                            n.TTL = 0.0f;
                        }
                    }
                }
                _state.cfg.NotificationOverlayLocation = new Rectangle() { Left = pt.X, Top = pt.Y, Right = pt.X + szy.X, Bottom = pt.Y + szy.Y };
                ImGui.End();
            }
        }

        private void RenderTimelineTab()
        {
            Vector2 fsz = ImGui.GetContentRegionAvail();
            ImGui.BeginChild("LemmyTimelineFrame", fsz);
            RenderTimelineContentSettings();
            ImGui.EndChild();
        }

        private void RenderContentTab(IEnumerable<Core.ContentCategory> contentCats)
        {
            // ------- content category -------
            foreach (Core.ContentCategory contentCat in contentCats)
            {
                string name1 = contentCat.GetType().Name;
                string path = "Content/" + name1;
                Vector2 a1 = ImGui.GetCursorPos();
                if (ImGui.CollapsingHeader("    " + I18n.Translate(path)) == true)
                {
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right) == true)
                    {
                        contentCat.Enabled = (contentCat.Enabled == false);
                    }
                    ImGui.PushID(contentCat.GetType().Name);
                    ImGui.Indent(30.0f);
                    RenderProperties(path, contentCat);
                    if (contentCat.ContentItems.Count == 0 && contentCat.Subcategories.Count == 0)
                    {
                        ImGui.TextColored(new Vector4(1.0f, 1.0f, 0.0f, 1.0f), I18n.Translate("Content/missing"));
                    }
                    RenderContentTab(contentCat.Subcategories.Values);
                    // ------- content -------
                    foreach (KeyValuePair<string, Core.Content> content in contentCat.ContentItems)
                    {
                        path = "Content/" + name1 + "/" + content.Key;
                        Vector2 a2 = ImGui.GetCursorPos();
                        if (ImGui.CollapsingHeader("    " + I18n.Translate(path)) == true)
                        {
                            if (ImGui.IsItemClicked(ImGuiMouseButton.Right) == true)
                            {
                                content.Value.Enabled = (content.Value.Enabled == false);
                            }
                            ImGui.PushID(content.Key);
                            ImGui.Indent(30.0f);
                            RenderProperties(path, content.Value);
                            if (content.Value.Items.Count == 0)
                            {
                                ImGui.TextColored(new Vector4(1.0f, 1.0f, 0.0f, 1.0f), I18n.Translate("Content/missing"));
                            }
                            // ------- content item -------
                            foreach (KeyValuePair<string, Core.ContentItem> contentItem in content.Value.Items)
                            {
                                path = "Content/" + name1 + "/" + content.Key + "/" + contentItem.Key;
                                Vector2 a3 = ImGui.GetCursorPos();
                                if (ImGui.CollapsingHeader("    " + I18n.Translate(path)) == true)
                                {
                                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right) == true)
                                    {
                                        contentItem.Value.Enabled = (contentItem.Value.Enabled == false);
                                    }
                                    ImGui.PushID(contentItem.Key);
                                    ImGui.Indent(30.0f);
#if !SANS_GOETIA
                                    if ((contentItem.Value.Features & ContentModule.FeaturesEnum.Hack) != 0)
                                    {
                                        float time = (float)((DateTime.Now - _loaded).TotalMilliseconds / 600.0);
                                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.5f + 0.5f * (float)Math.Abs(Math.Cos(time)), 0.0f, 1.0f));
                                        ImGui.TextWrapped(I18n.Translate("Misc/RiskyFeature"));
                                        ImGui.PopStyleColor();
                                        ImGui.Separator();
                                    }
#endif
                                    if ((contentItem.Value.Features & ContentModule.FeaturesEnum.Experimental) != 0)
                                    {
                                        float time = (float)((DateTime.Now - _loaded).TotalMilliseconds / 600.0);
                                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f + 0.5f * (float)Math.Abs(Math.Cos(time)), 1.0f, 0.0f, 1.0f));
                                        ImGui.TextWrapped(I18n.Translate("Misc/ExperimentalFeature"));
                                        ImGui.PopStyleColor();
                                        ImGui.Separator();
                                    }
                                    RenderProperties(path, contentItem.Value);
                                    ImGui.Unindent(30.0f);
                                    ImGui.PopID();
                                }
                                else
                                {
                                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right) == true)
                                    {
                                        contentItem.Value.Enabled = (contentItem.Value.Enabled == false);
                                    }
                                }
                                Vector2 b3 = ImGui.GetCursorPos();
                                ImGui.SetCursorPos(new Vector2(a3.X + 20, a3.Y + 2));
                                IDalamudTextureWrap t3 = (contentItem.Value.Enabled == true ?
                                    (contentItem.Value.Active == true ? _ui.GetMiscIcon(UserInterface.MiscIconEnum.BlueDiamond) : _ui.GetMiscIcon(UserInterface.MiscIconEnum.PurpleDiamond))
                                    : _ui.GetMiscIcon(UserInterface.MiscIconEnum.RedDiamond)
                                ).GetWrapOrEmpty();
                                ImGui.Image(t3.ImGuiHandle, new Vector2(20, 20));
                                ImGui.SetCursorPos(b3);
                            }
                            ImGui.Unindent(30.0f);
                            ImGui.PopID();
                        }
                        else
                        {
                            if (ImGui.IsItemClicked(ImGuiMouseButton.Right) == true)
                            {
                                content.Value.Enabled = (content.Value.Enabled == false);
                            }
                        }
                        Vector2 b2 = ImGui.GetCursorPos();
                        ImGui.SetCursorPos(new Vector2(a2.X + 20, a2.Y + 2));
                        IDalamudTextureWrap t2 = (content.Value.Enabled == true ?
                            (content.Value.Active == true ? _ui.GetMiscIcon(UserInterface.MiscIconEnum.BlueDiamond) : _ui.GetMiscIcon(UserInterface.MiscIconEnum.PurpleDiamond))
                            : _ui.GetMiscIcon(UserInterface.MiscIconEnum.RedDiamond)
                        ).GetWrapOrEmpty();
                        ImGui.Image(t2.ImGuiHandle, new Vector2(20, 20));
                        ImGui.SetCursorPos(b2);
                    }
                    ImGui.Unindent(30.0f);
                    ImGui.PopID();
                }
                else
                {
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right) == true)
                    {
                        contentCat.Enabled = (contentCat.Enabled == false);
                    }
                }
                Vector2 b1 = ImGui.GetCursorPos();
                ImGui.SetCursorPos(new Vector2(a1.X + 20, a1.Y + 2));

                IDalamudTextureWrap t1 = (contentCat.Enabled == true ? 
                    (contentCat.Active == true ? _ui.GetMiscIcon(UserInterface.MiscIconEnum.BlueDiamond) : _ui.GetMiscIcon(UserInterface.MiscIconEnum.PurpleDiamond))
                    : _ui.GetMiscIcon(UserInterface.MiscIconEnum.RedDiamond)
                ).GetWrapOrEmpty();
                ImGui.Image(t1.ImGuiHandle, new Vector2(20, 20));
                ImGui.SetCursorPos(b1);
            }
        }

        private void DrawUI()
        {
            _renderTimelineOverlay = false;
            _timelineOverlayConfig = false;
            _renderNotificationOverlay = false;
            _notificationOverlayConfig = false;
            if (_state._timeline != null)            
            {
                _renderTimelineOverlay = _state._timeline.IsOverlayVisible();
            }
            AddNotifications();
            if (Notifications.Count > 0)
            {
                _renderNotificationOverlay = true;
            }
            _state.TrackObjects();
            _softMarkerPreview = false;
            ImFontPtr? font = I18n.GetFont();
            if (font != null)
            {
                ImGui.PushFont((ImFontPtr)font);
            }
            DrawMainWindow();
            if (font != null)
            {
                ImGui.PopFont();
            }
            DrawContent();
            DrawSoftmarkers();
            if (_renderTimelineOverlay == true && _state.cfg.QuickToggleOverlays == true && _state.cfg.TimelineOverlayVisible == true)
            {
                RenderTimelineOverlay();
            }
            if (_renderNotificationOverlay == true && _state.cfg.QuickToggleOverlays == true && _state.cfg.NotificationOverlayVisible == true)
            {
                RenderNotificationOverlay();
            }
            _state.EndDrawing();
        }

        private void DrawContent()
        {
            _state.NumFeaturesAutomarker = 0;
            _state.NumFeaturesDrawing = _state.cfg.TimelineOverlayVisible == true ? 1 : 0;
            _state.NumFeaturesSound = 0;
#if !SANS_GOETIA
            _state.NumFeaturesHack = 0;
            _state.NumFeaturesAutomation = 0;
#endif
            if (_state.cs.LocalPlayer != null)
            {
                foreach (Core.ContentCategory cc in _content)
                {
                    cc.Execute();
                }
                foreach (Core.ContentCategory cc in _other)
                {
                    cc.Execute();
                }
            }
        }

        private void DrawSoftmarkerOn(AutomarkerSigns.SignEnum sign, ulong actorId)
        {
            if (actorId == 0 || sign == AutomarkerSigns.SignEnum.None)
            {
                return;
            }
            IGameObject go = _state.GetActorById(actorId);
            if (go == null)
            {
                return;
            }
            if (_state.StartDrawing(out ImDrawListPtr draw) == false)
            {
                return;
            }
            float mul = (float)Math.Abs(Math.Cos(DateTime.Now.Millisecond / 1000.0f * Math.PI));
            Vector3 temp = _ui.TranslateToScreen(
                go.Position.X + _state.cfg.SoftmarkerOffsetWorldX,
                go.Position.Y + _state.cfg.SoftmarkerOffsetWorldY + (_state.cfg.SoftmarkerBounce == true ? (0.5f * mul * _state.cfg.SoftmarkerScale) : 0.0f),
                go.Position.Z + _state.cfg.SoftmarkerOffsetWorldZ
            );
            Vector2 pt = new Vector2(
                temp.X + _state.cfg.SoftmarkerOffsetScreenX,
                temp.Y + _state.cfg.SoftmarkerOffsetScreenY
            );
            IDalamudTextureWrap tw = _ui.GetSignIcon(sign).GetWrapOrEmpty();
            float calcWidth = tw.Width * 2.0f * _state.cfg.SoftmarkerScale;
            float calcHeight = tw.Height * 2.0f * _state.cfg.SoftmarkerScale;
            pt.X -= calcWidth / 2.0f;
            pt.Y -= calcHeight;
            ImGui.SetCursorPos(pt);            
            draw.AddImage(
                tw.ImGuiHandle,
                new Vector2(pt.X, pt.Y),
                new Vector2(pt.X + calcWidth, pt.Y + calcHeight),
                new Vector2(0.0f, 0.0f),
                new Vector2(1.0f, 1.0f),
                ImGui.GetColorU32(new Vector4(
                    _state.cfg.SoftmarkerTint.X,
                    _state.cfg.SoftmarkerTint.Y,
                    _state.cfg.SoftmarkerTint.Z,
                    _state.cfg.SoftmarkerTint.W * (_state.cfg.SoftmarkerBlink == true ? (1.0f * mul) : 1.0f)
                ))
            );
        }

        private void DrawSoftmarkers()
        {
            if (_state.cfg.QuickToggleOverlays == false)
            {
                return;
            }
            if (_softMarkerPreview == true)
            {
                AutomarkerSigns.SignEnum sign;
                int num = (int)Math.Ceiling((DateTime.Now - _loaded).TotalSeconds % 8);
                if (num >= 0 && num <= 2)
                {
                    sign = AutomarkerSigns.SignEnum.Attack1;
                }
                else if (num > 2 && num <= 4)
                {
                    sign = AutomarkerSigns.SignEnum.Ignore1;
                }
                else if (num > 4 && num <= 6)
                {
                    sign = AutomarkerSigns.SignEnum.Bind1;
                }
                else
                {
                    sign = AutomarkerSigns.SignEnum.Plus;
                }
                DrawSoftmarkerOn(sign, _state.cs.LocalPlayer.GameObjectId);
            }
            else
            {
                foreach (KeyValuePair<AutomarkerSigns.SignEnum, ulong> kp in _state.SoftMarkers)
                {
                    DrawSoftmarkerOn(kp.Key, kp.Value);
                }
            }
        }

        private void DrawLemmyShortcut()
        {
            Vector2 winSize;
            if (ImGui.Begin("LemegetonShortcut", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove) == true)
            {
                DateTime now = DateTime.Now;
                if (_lemmyShortcutJustPopped == true)
                {
                    _lemmyShortcutPopped = now.AddSeconds(2);
                    _lemmyShortcutJustPopped = false;
                }
                IDalamudTextureWrap tw = _ui.GetMiscIcon(UserInterface.MiscIconEnum.Lemegeton).GetWrapOrEmpty();
                winSize = new Vector2(tw.Width + 10, tw.Height + 10);
                ImGui.SetWindowSize(winSize);
                ImGui.SetCursorPos(new Vector2(5, 5));
                if (now < _lemmyShortcutPopped)
                {
                    float time = (float)(_lemmyShortcutPopped - now).TotalMilliseconds / 200.0f;
                    _lemmyShortcutOpacity = (float)Math.Abs(Math.Cos(time));
                }
                ImGui.Image(tw.ImGuiHandle, new Vector2(tw.Width, tw.Height), new Vector2(0.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector4(1.0f, 1.0f, 1.0f, _lemmyShortcutOpacity));
                if (ImGui.IsItemHovered() == true)
                {
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Left) == true)
                    {
                        _state.cfg.Opened = true;
                    }
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right) == true)
                    {
                        _movingShortcut = true;
                        _movingMouse = ImGui.GetMousePos();
                    }
                    if (_movingShortcut == false)
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text(I18n.Translate("Misc/OpenShortcut"));
                        ImGui.EndTooltip();
                    }
                    _lemmyShortcutOpacity += (1.0f - _lemmyShortcutOpacity) / 10.0f;
                }
                else
                {
                    _lemmyShortcutOpacity -= (_lemmyShortcutOpacity - 0.5f) / 10.0f;
                }
                if (_movingShortcut == true)
                {
                    if (ImGui.IsMouseDown(ImGuiMouseButton.Right) == true)
                    {
                        Vector2 curmouse = ImGui.GetMousePos();
                        Vector2 delta = new Vector2(curmouse.X - _movingMouse.X, curmouse.Y - _movingMouse.Y);
                        _movingMouse = curmouse;
                        Vector2 cpos = ImGui.GetWindowPos();
                        ImGui.SetWindowPos("LemegetonShortcut", new Vector2(cpos.X + delta.X, cpos.Y + delta.Y));
                    }
                    else
                    {
                        _movingShortcut = false;
                    }
                }
                else
                {
                    UserInterface.KeepWindowInSight();
                }
                ImGui.End();
            }
        }

        private void RenderSettingsTab()
        {
            if (ImGui.CollapsingHeader(I18n.Translate("MainMenu/Settings/QuickToggles")) == true)
            {
                ImGui.PushID("QuickToggles");
                ImGui.Indent(30.0f);
                ImGui.TextWrapped(I18n.Translate("MainMenu/Settings/QuickToggles/Info") + Environment.NewLine + Environment.NewLine);
                bool qtAutomarker = _state.cfg.QuickToggleAutomarkers;
                if (ImGui.Checkbox(I18n.Translate("MainMenu/Settings/QuickToggles/Automarkers"), ref qtAutomarker) == true)
                {
                    _state.cfg.QuickToggleAutomarkers = qtAutomarker;
                }
                bool qtOverlays = _state.cfg.QuickToggleOverlays;
                if (ImGui.Checkbox(I18n.Translate("MainMenu/Settings/QuickToggles/Overlays"), ref qtOverlays) == true)
                {
                    _state.cfg.QuickToggleOverlays = qtOverlays;
                }
                bool qtSound = _state.cfg.QuickToggleSound;
                if (ImGui.Checkbox(I18n.Translate("MainMenu/Settings/QuickToggles/Sound"), ref qtSound) == true)
                {
                    _state.cfg.QuickToggleSound = qtSound;
                }
#if !SANS_GOETIA
                    bool qtHacks = _state.cfg.QuickToggleHacks;
                    if (ImGui.Checkbox(I18n.Translate("MainMenu/Settings/QuickToggles/Hacks"), ref qtHacks) == true)
                    {
                        _state.cfg.QuickToggleHacks = qtHacks;
                    }
                    bool qtAutomation = _state.cfg.QuickToggleAutomation;
                    if (ImGui.Checkbox(I18n.Translate("MainMenu/Settings/QuickToggles/Automation"), ref qtAutomation) == true)
                    {
                        _state.cfg.QuickToggleAutomation = qtAutomation;
                    }
#endif
                ImGui.Unindent(30.0f);
                ImGui.PopID();
            }
            if (ImGui.CollapsingHeader(I18n.Translate("MainMenu/Settings/UiSettings")) == true)
            {
                ImGui.PushID("UiSettings");
                ImGui.Indent(30.0f);
                ImGui.Text(I18n.Translate("MainMenu/Settings/Language"));
                if (ImGui.BeginCombo("##MainMenu/Settings/Language", _state.cfg.Language) == true)
                {
                    foreach (KeyValuePair<string, Core.Language> kp in I18n.RegisteredLanguages)
                    {
                        if (ImGui.Selectable(kp.Key + " (" + (int)Math.Floor(kp.Value.Coverage * 100.0f) + " %)", kp.Key == _state.cfg.Language) == true)
                        {
                            ChangeLanguage(kp.Key);
                            _state.cfg.Language = kp.Key;
                        }
                    }
                    ImGui.EndCombo();
                }
                bool shortcut = _state.cfg.ShowShortcut;
                ImGui.Text(Environment.NewLine);
                if (ImGui.Checkbox(I18n.Translate("MainMenu/Settings/ShowShortcut"), ref shortcut) == true)
                {
                    _state.cfg.ShowShortcut = shortcut;
                }
                bool streamNag = _state.cfg.NagAboutStreaming;
                if (ImGui.Checkbox(I18n.Translate("MainMenu/Settings/NagAboutStreaming"), ref streamNag) == true)
                {
                    _state.cfg.NagAboutStreaming = streamNag;
                }
                bool advOpts = _state.cfg.AdvancedOptions;
                if (ImGui.Checkbox(I18n.Translate("MainMenu/Settings/AdvancedOptions"), ref advOpts) == true)
                {
                    _state.cfg.AdvancedOptions = advOpts;
                }
                ImGui.Unindent(30.0f);
                ImGui.PopID();
            }
            if (ImGui.CollapsingHeader(I18n.Translate("MainMenu/Settings/AutomarkerSettings")) == true)
            {
                ImGui.PushID("AutomarkerSettings");
                ImGui.Indent(30.0f);
                bool remCombat = _state.cfg.RemoveMarkersAfterCombatEnd;
                if (ImGui.Checkbox(I18n.Translate("MainMenu/Settings/RemoveMarkersAfterCombatEnd"), ref remCombat) == true)
                {
                    _state.cfg.RemoveMarkersAfterCombatEnd = remCombat;
                }
                bool remWipe = _state.cfg.RemoveMarkersAfterWipe;
                if (ImGui.Checkbox(I18n.Translate("MainMenu/Settings/RemoveMarkersAfterWipe"), ref remWipe) == true)
                {
                    _state.cfg.RemoveMarkersAfterWipe = remWipe;
                }
                ImGui.Text(Environment.NewLine);
                if (ImGui.Button(I18n.Translate("MainMenu/Settings/RemoveAutomarkers")) == true)
                {
                    _state.ClearAutoMarkers();
                }
                ImGui.TextWrapped(Environment.NewLine + I18n.Translate("MainMenu/Settings/AutomarkersInitialApplicationDelay"));
                float autoIniMin = _state.cfg.AutomarkerIniDelayMin;
                float autoIniMax = _state.cfg.AutomarkerIniDelayMax;
                if (ImGui.DragFloatRange2(I18n.Translate("MainMenu/Settings/AutomarkerSeconds") + "##MainMenu/Settings/AutomarkersInitialApplicationDelay", ref autoIniMin, ref autoIniMax, 0.01f, 0.0f, 60.0f) == true)
                {
                    _state.cfg.AutomarkerIniDelayMin = autoIniMin;
                    _state.cfg.AutomarkerIniDelayMax = autoIniMax;
                }
                ImGui.TextWrapped(Environment.NewLine + I18n.Translate("MainMenu/Settings/AutomarkersSubsequentApplicationDelay"));
                float autoSubMin = _state.cfg.AutomarkerSubDelayMin;
                float autoSubMax = _state.cfg.AutomarkerSubDelayMax;
                if (ImGui.DragFloatRange2(I18n.Translate("MainMenu/Settings/AutomarkerSeconds") + "##MainMenu/Settings/AutomarkersSubsequentApplicationDelay", ref autoSubMin, ref autoSubMax, 0.01f, 0.0f, 2.0f) == true)
                {
                    _state.cfg.AutomarkerSubDelayMin = autoSubMin;
                    _state.cfg.AutomarkerSubDelayMax = autoSubMax;
                }
                ImGui.Text(Environment.NewLine);
                bool autoCmd = _state.cfg.AutomarkerCommands;
                if (ImGui.Checkbox(I18n.Translate("MainMenu/Settings/AutomarkersCommands"), ref autoCmd) == true)
                {
                    _state.cfg.AutomarkerCommands = autoCmd;
                }
                bool debugLogMarkers = _state.cfg.DebugOnlyLogAutomarkers;
                if (ImGui.Checkbox(I18n.Translate("MainMenu/Settings/DebugOnlyLogAutomarkers"), ref debugLogMarkers) == true)
                {
                    _state.cfg.DebugOnlyLogAutomarkers = debugLogMarkers;
                }
                if (_state.cfg.AutomarkerSoft == true && _state.cfg.QuickToggleOverlays == false)
                {
                    ImGui.TextWrapped(Environment.NewLine + I18n.Translate("MainMenu/Settings/AutomarkersSoftDesc") + Environment.NewLine);
                    float time = (float)((DateTime.Now - _loaded).TotalMilliseconds / 600.0);
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f + 0.5f * (float)Math.Abs(Math.Cos(time)), 1.0f, 0.0f, 1.0f));
                    ImGui.TextWrapped(Environment.NewLine + I18n.Translate("MainMenu/Settings/AutomarkersSoftPermsMissing",
                        I18n.Translate("MainMenu/Settings/QuickToggles/Overlays"),
                        I18n.Translate("MainMenu/Settings/QuickToggles"),
                        I18n.Translate("MainMenu/Settings")
                    ) + Environment.NewLine + Environment.NewLine);
                    ImGui.PopStyleColor();
                }
                else
                {
                    ImGui.TextWrapped(Environment.NewLine + I18n.Translate("MainMenu/Settings/AutomarkersSoftDesc") + Environment.NewLine + Environment.NewLine);
                }
                bool autoSoft = _state.cfg.AutomarkerSoft;
                if (ImGui.Checkbox(I18n.Translate("MainMenu/Settings/AutomarkersSoft"), ref autoSoft) == true)
                {
                    _state.cfg.AutomarkerSoft = autoSoft;
                }
                if (ImGui.CollapsingHeader(I18n.Translate("MainMenu/Settings/SoftmarkerSettings")) == true)
                {
                    _softMarkerPreview = true;
                    ImGui.PushID("SoftmarkerSettings");
                    ImGui.Indent(30.0f);
                    _ui.RenderWarning(I18n.Translate("MainMenu/Settings/SoftmarkerPreviewActive"));
                    ImGui.Text("");
                    Vector4 smcolor = _state.cfg.SoftmarkerTint;
                    if (ImGui.ColorEdit4(I18n.Translate("MainMenu/Settings/SoftmarkerTint"), ref smcolor, ImGuiColorEditFlags.NoInputs) == true)
                    {
                        _state.cfg.SoftmarkerTint = smcolor;
                    }
                    bool smbounce = _state.cfg.SoftmarkerBounce;
                    if (ImGui.Checkbox(I18n.Translate("MainMenu/Settings/SoftmarkerBounce"), ref smbounce) == true)
                    {
                        _state.cfg.SoftmarkerBounce = smbounce;
                    }
                    bool smblink = _state.cfg.SoftmarkerBlink;
                    if (ImGui.Checkbox(I18n.Translate("MainMenu/Settings/SoftmarkerBlink"), ref smblink) == true)
                    {
                        _state.cfg.SoftmarkerBlink = smblink;
                    }
                    ImGui.PushItemWidth(200.0f);
                    ImGui.TextWrapped(Environment.NewLine + I18n.Translate("MainMenu/Settings/SoftmarkerScaling"));
                    float smscale = _state.cfg.SoftmarkerScale * 100.0f;
                    if (ImGui.DragFloat("%##SmTint", ref smscale, 1.0f, 50.0f, 300.0f, "%.0f", ImGuiSliderFlags.AlwaysClamp) == true)
                    {
                        _state.cfg.SoftmarkerScale = smscale / 100.0f;
                    }
                    ImGui.TextWrapped(Environment.NewLine + I18n.Translate("MainMenu/Settings/SoftmarkerOffsetWorld"));
                    float smofsworldx = _state.cfg.SoftmarkerOffsetWorldX;
                    if (ImGui.DragFloat("X##SmWorldX", ref smofsworldx, 0.01f, -5.0f, 5.0f, "%.2f", ImGuiSliderFlags.AlwaysClamp) == true)
                    {
                        _state.cfg.SoftmarkerOffsetWorldX = smofsworldx;
                    }
                    float smofsworldy = _state.cfg.SoftmarkerOffsetWorldY;
                    if (ImGui.DragFloat("Y##SmWorldY", ref smofsworldy, 0.01f, -5.0f, 5.0f, "%.2f", ImGuiSliderFlags.AlwaysClamp) == true)
                    {
                        _state.cfg.SoftmarkerOffsetWorldY = smofsworldy;
                    }
                    float smofsworldz = _state.cfg.SoftmarkerOffsetWorldZ;
                    if (ImGui.DragFloat("Z##SmWorldZ", ref smofsworldz, 0.01f, -5.0f, 5.0f, "%.2f", ImGuiSliderFlags.AlwaysClamp) == true)
                    {
                        _state.cfg.SoftmarkerOffsetWorldZ = smofsworldz;
                    }
                    ImGui.TextWrapped(Environment.NewLine + I18n.Translate("MainMenu/Settings/SoftmarkerOffsetScreen"));
                    float smofsscreenx = _state.cfg.SoftmarkerOffsetScreenX;
                    if (ImGui.DragFloat("X##SmScreenX", ref smofsscreenx, 1.0f, -300.0f, 300.0f, "%.0f", ImGuiSliderFlags.AlwaysClamp) == true)
                    {
                        _state.cfg.SoftmarkerOffsetScreenX = smofsscreenx;
                    }
                    float smofsscreeny = _state.cfg.SoftmarkerOffsetScreenY;
                    if (ImGui.DragFloat("Y##SmScreenY", ref smofsscreeny, 1.0f, -300.0f, 300.0f, "%.0f", ImGuiSliderFlags.AlwaysClamp) == true)
                    {
                        _state.cfg.SoftmarkerOffsetScreenY = smofsscreeny;
                    }
                    ImGui.PopItemWidth();
                    ImGui.Unindent(30.0f);
                    ImGui.PopID();
                }
                if (ImGui.CollapsingHeader(I18n.Translate("MainMenu/Settings/AutomarkerHistory")) == true)
                {
                    ImGui.PushID("AutomarkerHistory");
                    ImGui.Indent(30.0f);
                    ImGui.TextWrapped(I18n.Translate("MainMenu/Settings/AutomarkerHistoryIntro") + Environment.NewLine + Environment.NewLine);
                    lock (_state.MarkerHistory)
                    {
                        if (_state.MarkerHistory.Count == 0)
                        {
                            ImGui.BeginDisabled();
                            ImGui.BeginChildFrame(1, new Vector2(ImGui.GetContentRegionAvail().X, 150.0f));
                            ImGui.EndChildFrame();
                            ImGui.EndDisabled();
                        }
                        else
                        {
                            ImGui.BeginChildFrame(1, new Vector2(ImGui.GetContentRegionAvail().X, 150.0f));
                            foreach (MarkerApplication ma in _state.MarkerHistory)
                            {
                                string label = String.Format("[{0} - {1}] {2} -> {3}: {4}",
                                    ma.Timestamp,
                                    ma.Location,
                                    ma.Source,
                                    ma.Destination,
                                    I18n.Translate(String.Format("Signs/{0}", ma.Sign.ToString())) + (ma.SoftMarker == true ? " " + I18n.Translate("MainMenu/Settings/AutomarkerHistorySoftmarker") : "")
                                ); ;
                                ImGui.Selectable(label, false, ImGuiSelectableFlags.Disabled);
                            }
                            ImGui.EndChildFrame();
                        }
                    }
                    ImGui.Unindent(30.0f);
                    ImGui.PopID();
                }
                ImGui.Unindent(30.0f);
                ImGui.PopID();
            }
            if (ImGui.CollapsingHeader(I18n.Translate("MainMenu/Settings/TimelineSettings")) == true)
            {
                ImGui.PushID("TimelineSettings");
                ImGui.Indent(30.0f);
                bool tlv = _state.cfg.TimelineTabVisible;
                if (ImGui.Checkbox(I18n.Translate("MainMenu/Settings/TimelineTabVisible"), ref tlv) == true)
                {
                    _state.cfg.TimelineTabVisible = tlv;
                }
                bool tla = _state.cfg.TimelineLocalAllowed;
                if (ImGui.Checkbox(I18n.Translate("MainMenu/Settings/TimelineLocalAllowed"), ref tla) == true)
                {
                    _state.cfg.TimelineLocalAllowed = tla;
                }
                string temp = _state.cfg.TimelineLocalFolder;
                ImGui.Text(Environment.NewLine + I18n.Translate("MainMenu/Settings/TimelineLocalFolder"));
                if (ImGui.InputText("##MainMenu/Settings/TimelineLocalFolder", ref temp, 256) == true)
                {
                    _state.cfg.TimelineLocalFolder = temp;
                }
                ImGui.Text("");
                if (ImGui.Button(I18n.Translate("MainMenu/Settings/TimelineLocalReload")) == true)
                {
                    Log(LogLevelEnum.Debug, "Reloading all local timelines");
                    _state.LoadLocalTimelines(0);
                }
                if (ImGui.CollapsingHeader(I18n.Translate("MainMenu/Settings/TimelineOverlaySettings")) == true)
                {
                    ImGui.PushID("TimelineOverlaySettings");
                    ImGui.Indent(30.0f);
                    RenderTimelineOverlaySettings();
                    ImGui.Unindent(30.0f);
                    ImGui.PopID();
                }
                ImGui.Unindent(30.0f);
                ImGui.PopID();
            }
            if (ImGui.CollapsingHeader(I18n.Translate("MainMenu/Settings/NotificationOverlaySettings")) == true)
            {
                ImGui.PushID("NotificationOverlaySettings");
                ImGui.Indent(30.0f);
                RenderNotificationOverlaySettings();
                ImGui.Unindent(30.0f);
                ImGui.PopID();
            }
            if (ImGui.CollapsingHeader(I18n.Translate("MainMenu/Settings/TtsSettings")) == true)
            {
                ImGui.PushID("TtsSettings");
                ImGui.Indent(30.0f);
                bool ttse = _state.cfg.TTSEnabled;
                if (ImGui.Checkbox(I18n.Translate("TextToSpeech/Settings/TTSEnabled"), ref ttse) == true)
                {
                    _state.cfg.TTSEnabled = ttse;
                }
                bool ttsall = _state.cfg.TTSAllNotifications;
                if (ImGui.Checkbox(I18n.Translate("TextToSpeech/Settings/TTSAllNotifications"), ref ttsall) == true)
                {
                    _state.cfg.TTSAllNotifications = ttsall;
                }
                int ttsvol = _state.cfg.TTSVolume;
                ImGui.Text(Environment.NewLine + I18n.Translate("TextToSpeech/Settings/TTSVolume"));
                if (ImGui.SliderInt("##TextToSpeechVolume", ref ttsvol, 0, 100) == true)
                {
                    _state.cfg.TTSVolume = ttsvol;
                }
                int ttsspeed = _state.cfg.TTSSpeed;
                ImGui.Text(Environment.NewLine + I18n.Translate("TextToSpeech/Settings/TTSSpeed"));
                if (ImGui.SliderInt("##TextToSpeechSpeed", ref ttsspeed, -10, 10) == true)
                {
                    _state.cfg.TTSSpeed = ttsspeed;
                }
                ImGui.Unindent(30.0f);
                ImGui.PopID();
            }
            if (ImGui.CollapsingHeader(I18n.Translate("MainMenu/Settings/OpcodeSettings")) == true)
            {
                ImGui.PushID("OpcodeSettings");
                ImGui.Indent(30.0f);
                bool qtLog = _state.cfg.LogUnhandledOpcodes;
                if (ImGui.Checkbox(I18n.Translate("MainMenu/Settings/LogUnhandledOpcodes"), ref qtLog) == true)
                {
                    _state.cfg.LogUnhandledOpcodes = qtLog;
                }
                string temp = _state.cfg.OpcodeUrl;
                ImGui.Text(I18n.Translate("MainMenu/Settings/OpcodeUrl"));
                if (ImGui.InputText("##MainMenu/Settings/OpcodeUrl", ref temp, 256) == true)
                {
                    _state.cfg.OpcodeUrl = temp;
                }
                ImGui.Text(I18n.Translate("MainMenu/Settings/OpcodeRegion"));
                IEnumerable<string> ops = _state._dec.GetOpcodeRegions();
                if (ops == null)
                {
                    ImGui.BeginDisabled();
                }
                if (ImGui.BeginCombo("##MainMenu/Settings/OpcodeRegion", _state.cfg.OpcodeRegion) == true)
                {
                    if (ops != null)
                    {
                        foreach (string op in ops)
                        {
                            if (ImGui.Selectable(op, op == _state.cfg.OpcodeRegion) == true)
                            {
                                _state._dec.SetOpcodeRegion(op);
                                _state.cfg.OpcodeRegion = op;
                            }
                        }
                    }
                    ImGui.EndCombo();
                }
                if (ops == null)
                {
                    ImGui.EndDisabled();
                }
                if (ImGui.Button(I18n.Translate("MainMenu/Settings/OpcodeReload")) == true)
                {
                    Log(LogLevelEnum.Debug, "Triggering opcode reload");
                    _retryEvent.Set();
                }
                ImGui.Unindent(30.0f);
                ImGui.PopID();
            }
            if (ImGui.CollapsingHeader(I18n.Translate("MainMenu/Settings/DebugSettings")) == true)
            {
                ImGui.PushID("DebugSettings");
                ImGui.Indent(30.0f);
                bool qFrame = _state.cfg.QueueFramework;
                if (ImGui.Checkbox(I18n.Translate("MainMenu/Settings/DebugSettings/QueueFramework"), ref qFrame) == true)
                {
                    _state.cfg.QueueFramework = qFrame;
                }
                if (ImGui.CollapsingHeader(I18n.Translate("MainMenu/Settings/DebugSettings/Config")) == true)
                {
                    ImGui.PushID("Config");
                    ImGui.Indent(30.0f);
                    if (ImGui.Button(I18n.Translate("MainMenu/Settings/DebugSettings/LoadConfig")) == true)
                    {
                        LoadConfig();
                        ApplyConfigToContent();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button(I18n.Translate("MainMenu/Settings/DebugSettings/SaveConfig")) == true)
                    {
                        SaveConfig();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button(I18n.Translate("MainMenu/Settings/DebugSettings/BackupConfig")) == true)
                    {
                        BackupConfig();
                    }
                    ImGui.Separator();
                    if (ImGui.Button(I18n.Translate("MainMenu/Settings/DebugSettings/ExportConfig")) == true)
                    {
                        _configSnapshot = SerializeConfigSnapshot();
                        Log(LogLevelEnum.Debug, "Config snapshot generated");
                    }
                    ImGui.InputTextMultiline("##ConfigSnapshot", ref _configSnapshot, 100000, new Vector2(ImGui.GetContentRegionAvail().X, 200.0f), ImGuiInputTextFlags.AutoSelectAll);
                    bool isempty = _configSnapshot.Trim().Length == 0;
                    if (isempty == true)
                    {
                        ImGui.BeginDisabled();
                    }
                    if (ImGui.Button(I18n.Translate("MainMenu/Settings/DebugSettings/CopyToClipboard")) == true)
                    {
                        ImGui.SetClipboardText(_configSnapshot);
                        Log(LogLevelEnum.Debug, "Config snapshot copied to clipboard");
                    }
                    if (isempty == true)
                    {
                        ImGui.EndDisabled();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button(I18n.Translate("MainMenu/Settings/DebugSettings/PasteFromClipboard")) == true)
                    {
                        _configSnapshot = ImGui.GetClipboardText();
                        Log(LogLevelEnum.Debug, "Config snapshot pasted from clipboard");
                    }
                    ImGui.SameLine();
                    if (isempty == true)
                    {
                        ImGui.BeginDisabled();
                    }
                    if (ImGui.Button(I18n.Translate("MainMenu/Settings/DebugSettings/ImportConfig")) == true)
                    {
                        IPluginConfiguration imp = DeserializeConfigSnapshot(_configSnapshot);
                        if (imp != null)
                        {
                            _state.cfg = (Config)imp;
                            Log(LogLevelEnum.Debug, "Config snapshot imported");
                        }
                        else
                        {
                            Log(LogLevelEnum.Error, "Couldn't import config snapshot");
                        }
                    }
                    if (isempty == true)
                    {
                        ImGui.EndDisabled();
                    }
                    ImGui.Unindent(30.0f);
                    ImGui.PopID();
                }
                if (ImGui.CollapsingHeader(I18n.Translate("MainMenu/Settings/DebugSettings/DelegateDebug")) == true)
                {
                    ImGui.PushID("DelegateDebug");
                    ImGui.Indent(30.0f);
                    ImGui.PushItemWidth(100.0f);
                    RenderMethodCall(_state.InvokeCombatantAdded);
                    RenderMethodCall(_state.InvokeCombatantRemoved);
                    RenderMethodCall(_state.InvokeZoneChange);
                    RenderMethodCall(_state.InvokeCombatChange);
                    RenderMethodCall(_state.InvokeCastBegin);
                    RenderMethodCall(_state.InvokeAction);
                    RenderMethodCall(_state.InvokeHeadmarker);
                    RenderMethodCall(_state.InvokeStatusChange);
                    RenderMethodCall(_state.InvokeTether);
                    RenderMethodCall(_state.InvokeDirectorUpdate);
                    RenderMethodCall(_state.InvokeActorControl);
                    RenderMethodCall(_state.InvokeMapEffect);
                    RenderMethodCall(_state.InvokeEventPlay);
                    ImGui.PopItemWidth();
                    ImGui.Unindent(30.0f);
                    ImGui.PopID();
                }
                ImGui.Unindent(30.0f);
                ImGui.PopID();
            }
        }

        private bool BeginAboutTab(bool forceOpen)
        {
            if (forceOpen == true)
            {
                bool temp = true;
                return ImGui.BeginTabItem(I18n.Translate("MainMenu/About"), ref temp, ImGuiTabItemFlags.SetSelected);
            }
            else
            {
                return ImGui.BeginTabItem(I18n.Translate("MainMenu/About"));
            }
        }

        private void RenderAboutTab()
        {
            Vector2 cnv = ImGui.GetContentRegionAvail();
            float sz = Math.Min(cnv.X, cnv.Y) - 20.0f;
            Vector2 pos = ImGui.GetWindowPos();
            float basex = pos.X + (cnv.X / 2.0f);
            float basey = pos.Y + (cnv.Y / 2.0f);
            float time = (float)((DateTime.Now - _loaded).TotalMilliseconds / 1200.0);
            int maxp = 100;
            float rim = sz / 20.0f;
            float thicc = sz / 50.0f;
            float delay = 4.0f;
            DateTime now = DateTime.Now;
            double secs = (now - _aboutOpened).TotalSeconds;
            double curtime = secs % delay;
            double transer = curtime < 1.0f ? 1.0f - curtime : (curtime > 3.0f ? curtime - 3.0f : 0.0f);
            float mul = sz / 200.0f;
            rim = Math.Clamp(rim, 2.0f, 20.0f);
            thicc = Math.Clamp(thicc, 2.0f, 5.0f);
            mul = Math.Clamp(mul, 0.5f, 5.0f);
            ImDrawListPtr draw = ImGui.GetWindowDrawList();
#if !SANS_GOETIA
                maxp = 5;
#else
            maxp = 10;
#endif
            float inner = (sz / 2.0f) - rim - 10.0f;
            for (int i = 0; i < maxp; i++)
            {
                float ag1 = time + ((float)Math.PI * 2.0f / maxp * (float)i);
                float ag2 = time + ((float)Math.PI * 2.0f / maxp * (float)(i + 2));
                draw.AddLine(
                    new Vector2(basex + (float)Math.Cos(ag1) * inner, basey + (float)Math.Sin(ag1) * inner),
                    new Vector2(basex + (float)Math.Cos(ag2) * inner, basey + (float)Math.Sin(ag2) * inner),
                    ImGui.GetColorU32(new Vector4(0.3f, 0.0f, 0.0f, 1.0f)),
                    thicc
                );
            }
            draw.AddCircle(
                new Vector2(basex, basey),
                (sz / 2.0f) - 10.0f,
                ImGui.GetColorU32(new Vector4(0.5f, 0.0f, 0.0f, 1.0f)),
                32,
                thicc
            );
            draw.AddCircle(
                new Vector2(basex, basey),
                inner,
                ImGui.GetColorU32(new Vector4(0.5f, 0.0f, 0.0f, 1.0f)),
                32,
                thicc
            );
            maxp = 100;
            for (int i = 0; i < maxp; i++)
            {
                float ang = (float)(Math.PI * 2.0 / maxp * (float)i) + (float)Math.Cos(i);
                float dis = (sz / 2.0f) * (float)Math.Cos((i + time / (float)maxp) + time);
                float col = (float)Math.Abs(Math.Cos(ang + time));
                draw.AddCircleFilled(
                    new Vector2(
                        basex + ((float)Math.Cos(ang) * dis),
                        basey + ((float)Math.Sin(ang) * dis)
                    ),
                    (thicc * 2.0f) + (thicc * (float)Math.Cos((i + time / (float)maxp) + time)),
                    ImGui.GetColorU32(new Vector4(col, dis / sz / 2.0f, 0.0f, col)),
                    32
                );
            }
            int maxscroller = _aboutScroller.Count();
            int curtext = (int)Math.Floor(secs / delay) % (maxscroller + _contribs.Count);
            string curstr;
            if (curtext < maxscroller)
            {
                curstr = _aboutScroller[curtext].Replace("$auto", _state.cfg.AutomarkersServed.ToString());
            }
            else
            {
                curstr = _contribs[curtext - maxscroller];
            }
            Vector2 tsz = ImGui.CalcTextSize(curstr);
            tsz.X *= mul;
            tsz.Y *= mul;
            float curX = basex - tsz.X / 2.0f;
            int nc = 0;
            foreach (char c in curstr)
            {
                string glyph = c.ToString();
                Vector2 tsg = ImGui.CalcTextSize(glyph);
                float bonk = Math.Abs((float)Math.Cos((nc / 2.0f) + (time * 2.0f)));
                draw.AddText(
                    ImGui.GetFont(),
                    ImGui.GetFontSize() * mul,
                    new Vector2(
                        curX + (mul * (10.0f * (float)transer) * 3.0f * (float)Math.Cos((nc / 4.0f) + (time * 2.0f))),
                        basey - (tsz.Y / 2.0f) + ((float)Math.Cos((nc / 2.0f) + (time * 2.0f))) * (mul * 5.0f)
                    ),
                    ImGui.GetColorU32(new Vector4(1.0f, 1.0f - (float)transer, (1.0f - bonk) * (1.0f - (float)transer), 1.0f - (float)transer)),
                    glyph
                );
                curX += tsg.X * mul;
                nc++;
            }
        }

        private void RenderFooter()
        {
            ImGui.Separator();
            Vector2 fp = ImGui.GetCursorPos();
            ImGui.SetCursorPosY(fp.Y + 2);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.3f, 0.3f, 0.3f, 1.0f));
            ImGui.Text("v" + Version + " - " + _state.GameVersion);
            ImGui.PopStyleColor();
            ImGui.SetCursorPos(new Vector2(_adjusterX, fp.Y));
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.496f, 0.058f, 0.323f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.496f, 0.058f, 0.323f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.4f, 0.4f, 0.4f, 1.0f));
            if (ImGui.Button("Discord") == true)
            {
                Task tx = new Task(() =>
                {
                    Process p = new Process();
                    p.StartInfo.UseShellExecute = true;
                    p.StartInfo.FileName = @"https://discord.gg/6f9MY55";
                    p.Start();
                });
                tx.Start();
            }
            ImGui.SameLine();
            if (ImGui.Button("GitHub") == true)
            {
                Task tx = new Task(() =>
                {
                    Process p = new Process();
                    p.StartInfo.UseShellExecute = true;
                    p.StartInfo.FileName = @"https://github.com/paissaheavyindustries/Lemegeton";
                    p.Start();
                });
                tx.Start();
            }
            ImGui.SameLine();
            _adjusterX += ImGui.GetContentRegionAvail().X;
            ImGui.PopStyleColor(3);
        }

        private void DrawMainWindow()
        {
            if (_state.cfg.Opened == false)
            {
                if (_state.cfg.ShowShortcut == true)
                {
                    DrawLemmyShortcut();
                }
                return;
            }
            bool aboutForceOpen = false;
            if (_state.cfg.FirstRun == true)
            {
                ImGui.SetNextWindowSize(new Vector2(500.0f, 400.0f));
                ImGui.SetNextWindowPos(new Vector2(100.0f, 100.0f));
                aboutForceOpen = true;
                _state.cfg.FirstRun = false;
            }
            _lemmyShortcutJustPopped = true;
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.496f, 0.058f, 0.323f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.TabActive, new Vector4(0.496f, 0.058f, 0.323f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.TabHovered, new Vector4(0.4f, 0.4f, 0.4f, 1.0f));
            bool open = true;
            ImGui.SetNextWindowSize(new Vector2(500, 500), ImGuiCond.FirstUseEver);
            if (ImGui.Begin(Name, ref open, ImGuiWindowFlags.NoCollapse) == false)
            {
                ImGui.End();
                ImGui.PopStyleColor(3);
                return;
            }
            if (open == false)
            {
                _state.cfg.Opened = false;
                SaveConfig();
                ImGui.End();
                ImGui.PopStyleColor(3);
                _ui.ClearDialog();
                return;
            }
            Vector2 c1 = ImGui.GetWindowPos();
            Vector2 c2 = ImGui.GetWindowSize();
            _lastSeen.Left = c1.X;
            _lastSeen.Top = c1.Y;
            _lastSeen.Right = c1.X + c2.X;
            _lastSeen.Top = c1.Y + c2.Y;
            ImGuiStylePtr style = ImGui.GetStyle();
            Vector2 fsz = ImGui.GetContentRegionAvail();
            fsz.Y -= ImGui.GetTextLineHeight() + (style.ItemSpacing.Y * 2) + style.WindowPadding.Y;
            bool dling = _downloadPending;
            string dlfn = _downloadFilename;
            if (dling == true)
            {
                Vector2 tenp = ImGui.GetCursorPos();
                float time = (float)((DateTime.Now - _loaded).TotalMilliseconds / 600.0);
                IDalamudTextureWrap tw = _ui.GetMiscIcon(UserInterface.MiscIconEnum.Exclamation).GetWrapOrEmpty();
                ImGui.Image(
                    tw.ImGuiHandle, new Vector2(tw.Width, tw.Height),
                    new Vector2(0, 0), new Vector2(1, 1),
                    new Vector4(1.0f, 1.0f, 1.0f, 0.5f + 0.5f * (float)Math.Abs(Math.Cos(time)))
                );
                Vector2 anp1 = ImGui.GetCursorPos();
                ImGui.SetCursorPos(new Vector2(tenp.X + tw.Width + 10, tenp.Y));
                ImGui.TextWrapped("Downloading, please wait.." + Environment.NewLine + (dlfn != null ? dlfn : ""));
                Vector2 anp2 = ImGui.GetCursorPos();
                ImGui.SetCursorPos(new Vector2(tenp.X, Math.Max(anp1.Y, anp2.Y)));
                ImGui.Separator();
                fsz.Y -= ImGui.GetCursorPosY() - tenp.Y;
                ImGui.BeginDisabled();
            }
            ImGui.BeginChild("LemmyFrame", fsz);
            ImGui.BeginTabBar("Lemmytabs");
            // status
            if (ImGui.BeginTabItem(I18n.Translate("MainMenu/Status")) == true)
            {
                ImGui.BeginChild("MainMenu/Status");
                RenderStatusTab();
                ImGui.EndChild();
                ImGui.EndTabItem();
            }
            // timeline
            if (_state.cfg.TimelineTabVisible == true)
            {
                if (ImGui.BeginTabItem(I18n.Translate("MainMenu/Timelines")) == true)
                {
                    ImGui.BeginChild("MainMenu/Timelines");
                    RenderTimelineTab();
                    ImGui.EndChild();
                    ImGui.EndTabItem();
                }
            }
            // content
            if (ImGui.BeginTabItem(I18n.Translate("MainMenu/Content")) == true)
            {
                ImGui.BeginChild("MainMenu/Content");
                RenderContentTab(_content);
                ImGui.EndChild();
                ImGui.EndTabItem();
            }
            // custom
            if (ImGui.BeginTabItem(I18n.Translate("MainMenu/Other")) == true)
            {
                ImGui.BeginChild("MainMenu/Other");
                RenderContentTab(_other);
                ImGui.EndChild();
                ImGui.EndTabItem();
            }
            // settings
            if (ImGui.BeginTabItem(I18n.Translate("MainMenu/Settings")) == true)
            {
                ImGui.BeginChild("MainMenu/Settings");
                RenderSettingsTab();
                ImGui.EndChild();
                ImGui.EndTabItem();
            }
            // about
            if (BeginAboutTab(aboutForceOpen) == true)
            {
                if (_aboutProg == false)
                {
                    _aboutOpened = DateTime.Now;
                    _aboutProg = true;
                }
                ImGui.BeginChild("MainMenu/About");
                RenderAboutTab();
                ImGui.EndChild();
                ImGui.EndTabItem();
            }
            else
            {
                _aboutProg = false;
            }
            ImGui.EndTabBar();
            ImGui.EndChild();
            if (dling == true)
            {
                ImGui.EndDisabled();
            }
            RenderFooter();
            UserInterface.KeepWindowInSight();
            ImGui.End();
            if (_timelineSelectorOpened == true)
            {
                DrawTimelineSelectorWindow();
            }
            ImGui.PopStyleColor(3);
        }

        private void DrawTimelineSelectorWindow()
        {
            bool open = true;
            ImGui.SetNextWindowSize(new Vector2(500, 500), ImGuiCond.FirstUseEver);
            if (ImGui.Begin(I18n.Translate("Timelines/TimelineSelector/WindowTitle"), ref open, ImGuiWindowFlags.NoCollapse) == false)
            {
                ImGui.End();                
                return;
            }
            if (open == false)
            {
                Log(LogLevelEnum.Debug, "Closing timeline selector");
                _timelineSelectorOpened = false;
                ImGui.End();
                _ui.ClearDialog();
                return;
            }
            _ui.RenderWarning(I18n.Translate("Timelines/TimelineSelector/SelectionInfo"));
            ImGui.BeginTable("Lemegeton_TimelineSelector", 4, ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY);
            ImGui.TableSetupScrollFreeze(0, 2);
            ImGui.TableSetupColumn(I18n.Translate("Timelines/TimelineSelector/ColZoneID"), ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort);
            ImGui.TableSetupColumn(I18n.Translate("Timelines/TimelineSelector/ColZoneName"), ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort);
            ImGui.TableSetupColumn(I18n.Translate("Timelines/TimelineSelector/ColZoneFile"), ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.NoSort);
            ImGui.TableSetupColumn("##Lemegeton_TimelineSelector_actions", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort | ImGuiTableColumnFlags.NoResize);
            ImGui.TableHeadersRow();
            Dictionary<ushort, Timeline> tlcopy;
            lock (_state.AllTimelines)
            {
                tlcopy = new Dictionary<ushort, Timeline>(_state.AllTimelines);
            }
            Dictionary<ushort, string> zonenames = new Dictionary<ushort, string>();
            foreach (ushort t in tlcopy.Keys)
            {
                zonenames[t] = GetInstanceNameForTerritory(t);
            }
            var temp = zonenames.ToList();
            temp.Sort((a, b) => a.Value.CompareTo(b.Value));
            ImGuiStylePtr style = ImGui.GetStyle();
            float itemsp = style.ItemSpacing.X;
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            Vector2 avail3 = ImGui.GetContentRegionAvail();
            ImGui.Text(" ");
            ImGui.TableSetColumnIndex(1);
            Vector2 avail1 = ImGui.GetContentRegionAvail();
            ImGui.PushFont(UiBuilder.IconFont);
            string ico = FontAwesomeIcon.Search.ToIconString();
            Vector2 sz = ImGui.CalcTextSize(ico);
            ImGui.Text(ico);
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.PushItemWidth(avail1.X - sz.X - itemsp);
            if (ImGui.InputText("##TimelineSelector/ZoneFilterBox", ref _timelineSelectorZoneFilter, 256) == true)
            {
            }
            ImGui.PopItemWidth();
            ImGui.TableSetColumnIndex(2);
            Vector2 avail2 = ImGui.GetContentRegionAvail();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text(ico);
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.PushItemWidth(avail2.X - sz.X - itemsp);
            if (ImGui.InputText("##TimelineSelector/FileFilterBox", ref _timelineSelectorFileFilter, 256) == true)
            {
            }
            ImGui.PopItemWidth();
            ImGui.TableSetColumnIndex(3);
            ImGui.Text(" ");
            temp = (from tx in temp where
                    (
                        _timelineSelectorZoneFilter == ""
                        ||
                        tx.Value.Contains(_timelineSelectorZoneFilter, StringComparison.InvariantCultureIgnoreCase)
                        ||
                        tx.Key.ToString().Contains(_timelineSelectorZoneFilter, StringComparison.InvariantCultureIgnoreCase))
                    &&
                    (_timelineSelectorFileFilter == "" || tlcopy[tx.Key].Filename.Contains(_timelineSelectorFileFilter, StringComparison.InvariantCultureIgnoreCase))
                    select tx).ToList();
            foreach (var zn in temp)
            {                
                ImGui.TableNextRow();
                bool overridden;
                lock (_state.TimelineOverrides)
                {
                    overridden = _state.TimelineOverrides.ContainsKey(zn.Key);
                }
                for (int col = 0; col < 4; col++)
                {
                    ImGui.TableSetColumnIndex(col);
                    switch (col)
                    {
                        case 0:
                            {
                                string txt = zn.Key.ToString();
                                Vector2 psz = ImGui.CalcTextSize(txt);
                                ImGui.Text(txt);
                                if (psz.X > avail3.X && ImGui.IsItemHovered() == true)
                                {
                                    ImGui.BeginTooltip();
                                    ImGui.Text(txt);
                                    ImGui.EndTooltip();
                                }
                            }
                            break;
                        case 1:
                            {
                                string txt = zn.Value;
                                Vector2 psz = ImGui.CalcTextSize(txt);
                                ImGui.Text(txt);
                                if (psz.X > avail1.X && ImGui.IsItemHovered() == true)
                                {
                                    ImGui.BeginTooltip();
                                    ImGui.Text(txt);
                                    ImGui.EndTooltip();
                                }
                            }
                            break;
                        case 2:
                            {
                                if (overridden == false)
                                {
                                    ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(new Vector4(0.5f, 1.0f, 0.5f, 0.7f)));
                                    UserInterface.IconText(FontAwesomeIcon.Sync, I18n.Translate("Timelines/TimelineSelector/SelTypeAutomatic"));
                                    ImGui.PopStyleColor();
                                }
                                else
                                {
                                    ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(new Vector4(1.0f, 0.5f, 0.5f, 0.7f)));
                                    UserInterface.IconText(FontAwesomeIcon.Edit, I18n.Translate("Timelines/TimelineSelector/SelTypeOverride"));
                                    ImGui.PopStyleColor();
                                }
                                ImGui.SameLine();
                                string txt = tlcopy[zn.Key].Filename;
                                Vector2 psz = ImGui.CalcTextSize(txt);
                                if (ImGui.Selectable(txt, false) == true)
                                {
                                    System.Diagnostics.Process.Start("explorer.exe", String.Format("/select, \"{0}\"", txt));
                                }
                                if (psz.X > avail2.X && ImGui.IsItemHovered() == true)
                                {
                                    ImGui.BeginTooltip();
                                    ImGui.Text(txt);
                                    ImGui.EndTooltip();
                                }
                            }
                            break;
                        case 3:
                            {
                                if (UserInterface.IconButton(FontAwesomeIcon.FolderOpen, I18n.Translate("Timelines/TimelineSelector/ChangeTimelineFile") + "##" + zn.Key) == true)
                                {
                                    string sp = Path.GetDirectoryName(tlcopy[zn.Key].Filename);
                                    Log(LogLevelEnum.Debug, "Opening timeline file selection dialog to {0}", sp);
                                    _ui.OpenFileDialog(I18n.Translate("Timelines/TimelineSelector/SelectTimelineFile", zn.Value), ".xml", sp, (ok, filename) =>
                                    {
                                        if (ok == true)
                                        {
                                            SetTimelineOverrideToFile(zn.Key, filename.First());
                                        }
                                    });
                                }
                                if (overridden == true)
                                {
                                    ImGui.SameLine();
                                    if (UserInterface.IconButton(FontAwesomeIcon.Trash, I18n.Translate("Timelines/TimelineSelector/DeleteOverride") + "##" + zn.Key) == true)
                                    {
                                        RemoveTimelineOverrideFrom(zn.Key);
                                    }
                                }
                            }
                            break;
                    }
                }                
            }
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.Text("");
            ImGui.TableSetColumnIndex(1);
            ImGui.Text("");
            ImGui.TableSetColumnIndex(2);
            ImGui.Text("");
            ImGui.TableSetColumnIndex(3);
            if (UserInterface.IconButton(FontAwesomeIcon.Plus, I18n.Translate("Timelines/TimelineSelector/AddTimelineFile")) == true)
            {
                string sp = _state.cfg.TimelineLocalFolder;
                Log(LogLevelEnum.Debug, "Opening timeline file selection dialog to {0}", sp);
                _ui.OpenFileDialog(I18n.Translate("Timelines/TimelineSelector/SelectTimelineFileUnspecified"), ".xml", sp, (ok, filename) =>
                {
                    if (ok == true)
                    {
                        string fn = filename.First();
                        Timeline tl = _state.LoadTimeline(fn);
                        if (tl != null)
                        {
                            SetTimelineOverrideToFile(tl.Territory, fn);
                        }
                    }
                });
            }
            ImGui.EndTable();
            UserInterface.KeepWindowInSight();
            ImGui.End();
        }

        private void RemoveTimelineOverrideFrom(ushort territory)
        {
            Log(LogLevelEnum.Debug, "Removing timeline override from territory {0}", territory);
            lock (_state.TimelineOverrides)
            {
                if (_state.TimelineOverrides.ContainsKey(territory) == false)
                {
                    return;
                }
                _state.TimelineOverrides.Remove(territory);
            }
            lock (_state.AllTimelines)
            {
                if (_state.AllTimelines.ContainsKey(territory) == true)
                {
                    _state.AllTimelines.Remove(territory);
                }
            }
            if (_state.cfg.TimelineLocalAllowed == true)
            {
                _state.LoadLocalTimelines(territory);
            }
        }

        private void SetTimelineOverrideToFile(ushort territory, string filename)
        {
            Log(LogLevelEnum.Debug, "Setting timeline override for territory {0} to {1}", territory, filename);
            Timeline tl = _state.LoadTimeline(filename);
            if (tl == null)
            {
                Log(LogLevelEnum.Warning, "Timeline file {0} could not be loaded as override for territory {1}", filename, territory);
                return;
            }
            if (tl.Territory != territory)
            {
                Log(LogLevelEnum.Warning, "Timeline file {0} is meant for another territory {1}, not territory {2}", filename, tl.Territory, territory);
                return;
            }
            lock (_state.AllTimelines)
            {
                _state.AllTimelines[territory] = tl;
            }
            lock (_state.TimelineOverrides)
            {
                _state.TimelineOverrides[territory] = filename;
            }
            if (territory == _state.cs.TerritoryType)
            {
                _state.Cs_TerritoryChanged(territory);
            }
        }

        private void RenderMethodCall(Delegate del)
        {
            Type delt = del.GetType();
            ImGui.Text(del.Method.Name);
            MethodInfo mi = delt.GetMethod("Invoke");
            ParameterInfo[] pis = mi.GetParameters();
            if (_delDebugInput.ContainsKey(del) == false)
            {
                int j = 0;
                _delDebugInput[del] = new string[pis.Count()];
                foreach (ParameterInfo p in pis)
                {
                    string defValue = "";
                    if (p.ParameterType == typeof(bool))
                    {
                        defValue = "false";
                    }
                    if (
                        (p.ParameterType == typeof(float) || p.ParameterType == typeof(double))
                        ||
                        (p.ParameterType == typeof(ushort) || p.ParameterType == typeof(short))
                        ||
                        (p.ParameterType == typeof(uint) || p.ParameterType == typeof(int))
                        ||
                        (p.ParameterType == typeof(ulong) || p.ParameterType == typeof(long))
                    )
                    {
                        defValue = "0";
                    }
                    _delDebugInput[del][j] = defValue;
                    j++;
                }
            }
            int i = 0;
            foreach (ParameterInfo p in pis)
            {
                string pname = "##" + del.Method.Name + "/" + p.Name;
                if (p.ParameterType == typeof(bool))
                {
                    if (ImGui.BeginCombo(pname, _delDebugInput[del][i]) == true)
                    {
                        if (ImGui.Selectable("true", String.Compare("true", _delDebugInput[del][i]) == 0) == true)
                        {
                            _delDebugInput[del][i] = "true";
                        }
                        if (ImGui.Selectable("false", String.Compare("false", _delDebugInput[del][i]) == 0) == true)
                        {
                            _delDebugInput[del][i] = "false";
                        }
                        ImGui.EndCombo();
                    }
                }
                else
                {
                    ImGuiInputTextFlags flags = ImGuiInputTextFlags.None;
                    if (p.ParameterType == typeof(float) || p.ParameterType == typeof(double))
                    {
                        flags |= ImGuiInputTextFlags.CharsScientific;
                    }
                    else if (
                        (p.ParameterType == typeof(ushort) || p.ParameterType == typeof(short))
                        ||
                        (p.ParameterType == typeof(uint) || p.ParameterType == typeof(int))
                        ||
                        (p.ParameterType == typeof(ulong) || p.ParameterType == typeof(long))
                    )
                    {
                        flags |= ImGuiInputTextFlags.CharsDecimal;
                    }
                    else if (
                        (p.ParameterType == typeof(byte) || p.ParameterType == typeof(byte[]))
                    )
                    {
                        flags |= ImGuiInputTextFlags.CharsHexadecimal;
                    }
                    ImGui.InputText(pname, ref _delDebugInput[del][i], 256, flags);
                }
                if (ImGui.IsItemHovered() == true)
                {
                    ImGui.BeginTooltip();
                    ImGui.Text(p.ParameterType.Name);
                    ImGui.EndTooltip();
                }
                ImGui.SameLine();
                i++;
            }
            if (ImGui.Button(">>##" + del.Method.Name) == true)
            {
                List<object> conversions = new List<object>();
                int k = 0;
                foreach (ParameterInfo p in pis)
                {
                    if (p.ParameterType == typeof(IGameObject))
                    {
                        string temp = _delDebugInput[del][k];
                        uint actorId = uint.Parse(temp, System.Globalization.NumberStyles.HexNumber);
                        IGameObject go = _state.GetActorById(actorId);
                        conversions.Add(go);
                    }
                    else if (p.ParameterType == typeof(IntPtr))
                    {
                        string temp = _delDebugInput[del][k];
                        nint addr = nint.Parse(temp, System.Globalization.NumberStyles.HexNumber);
                        conversions.Add(new IntPtr(addr));
                    }
                    else if (p.ParameterType == typeof(byte[]))
                    {
                        string temp = _delDebugInput[del][k];
                        if ((temp.Length % 2) != 0)
                        {
                            temp = "0" + temp;
                        }
                        int chars = temp.Length;
                        byte[] buf = new byte[chars / 2];
                        for (int l = 0; l < chars; l += 2)
                        {
                            buf[l / 2] = Convert.ToByte(temp.Substring(l, 2), 16);
                        }
                        conversions.Add(buf);
                    }
                    else
                    {
                        TypeConverter tc = TypeDescriptor.GetConverter(p.ParameterType);
                        object o = tc.ConvertFromString(null, CultureInfo.InvariantCulture, _delDebugInput[del][k]);
                        conversions.Add(o);
                    }
                    k++;
                }
                del.Method.Invoke(_state, conversions.Count > 0 ? conversions.ToArray() : null);
            }
        }

        private void RenderStatusTab()
        {
            IDalamudTextureWrap tw;
            bool tfer;
            ImGuiStylePtr style = ImGui.GetStyle();
            float textofsx = (style.ItemSpacing.X / 2.0f);
            float textofsy = 0.0f;
            List<string> complaints = new List<string>();
            if (ImGui.BeginTable("Table" + I18n.Translate("Status/AtAGlance"), 2) == true)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                {
                    ImGui.BeginDisabled();
                    ImGui.CollapsingHeader("  " + I18n.Translate("Status/AtAGlance"), ImGuiTreeNodeFlags.Leaf);
                    ImGui.EndDisabled();
                    {
                        tw = (_state.StatusGotOpcodes == true ? _ui.GetMiscIcon(UserInterface.MiscIconEnum.Smiley) : _ui.GetMiscIcon(UserInterface.MiscIconEnum.RedCross)).GetWrapOrEmpty();
                        ImGui.Image(tw.ImGuiHandle, new Vector2(tw.Width, tw.Height));
                        ImGui.SameLine();
                        string txt = I18n.Translate("Status/StatusGotOpcodes" + _state.StatusGotOpcodes);
                        Vector2 th = ImGui.CalcTextSize(txt);
                        textofsy = ((tw.Height - th.Y) / 2.0f);
                        ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPosX() - textofsx, ImGui.GetCursorPosY() + textofsy));
                        ImGui.TextWrapped(txt);
                        if (_state.StatusGotOpcodes == false)
                        {
                            complaints.Add(I18n.Translate("Status/WarnNoOpcodes"));
                        }
                        else
                        {
                            string opcv = _state._dec.GetOpcodeVersion();
                            if (opcv != null && _state.GameVersion != opcv)
                            {
                                complaints.Add(I18n.Translate("Status/WarnOpcodesVersion", opcv, _state.GameVersion));
                            }
                        }
                    }
                    {
                        tw = (_state.StatusMarkingFuncAvailable == true ? _ui.GetMiscIcon(UserInterface.MiscIconEnum.Smiley) : _ui.GetMiscIcon(UserInterface.MiscIconEnum.RedCross)).GetWrapOrEmpty();
                        ImGui.Image(tw.ImGuiHandle, new Vector2(tw.Width, tw.Height));
                        ImGui.SameLine();
                        ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPosX() - textofsx, ImGui.GetCursorPosY() + textofsy));
                        ImGui.TextWrapped(I18n.Translate("Status/StatusMarkingFuncAvailable" + _state.StatusMarkingFuncAvailable));
                        tw = (_state.StatusPostCommandAvailable == true ? _ui.GetMiscIcon(UserInterface.MiscIconEnum.Smiley) : _ui.GetMiscIcon(UserInterface.MiscIconEnum.RedCross)).GetWrapOrEmpty();
                        ImGui.Image(tw.ImGuiHandle, new Vector2(tw.Width, tw.Height));
                        ImGui.SameLine();
                        ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPosX() - textofsx, ImGui.GetCursorPosY() + textofsy));
                        ImGui.TextWrapped(I18n.Translate("Status/StatusPostCommandAvailable" + _state.StatusPostCommandAvailable));
                        if (_state.StatusMarkingFuncAvailable == false)
                        {
                            if (_state.StatusPostCommandAvailable == false)
                            {
                                complaints.Add(I18n.Translate("Status/WarnAutomarkersBroken"));
                            }
                            else
                            {
                                complaints.Add(I18n.Translate("Status/WarnAutomarkersSemibroken"));
                            }
                        }
                        else
                        {
                            if (_state.StatusPostCommandAvailable == false)
                            {
                                complaints.Add(I18n.Translate("Status/WarnCommandPostBroken"));
                            }
                        }
                    }
                    {
                        tfer = _state.LastNetworkTrafficDown != DateTime.MinValue && (DateTime.Now - _state.LastNetworkTrafficDown).TotalSeconds < 60.0;
                        tw = (tfer == true ? _ui.GetMiscIcon(UserInterface.MiscIconEnum.Connected) : _ui.GetMiscIcon(UserInterface.MiscIconEnum.Disconnected)).GetWrapOrEmpty();
                        ImGui.Image(tw.ImGuiHandle, new Vector2(tw.Width, tw.Height));
                        ImGui.SameLine();
                        ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPosX() - textofsx, ImGui.GetCursorPosY() + textofsy));
                        ImGui.TextWrapped(I18n.Translate("Status/StatusNetworkTrafficDown" + tfer));
                        if (tfer == false)
                        {
                            complaints.Add(I18n.Translate("Status/WarnNoTrafficDown"));
                        }
                    }
                    {
                        tfer = _state.LastNetworkTrafficUp != DateTime.MinValue && (DateTime.Now - _state.LastNetworkTrafficUp).TotalSeconds < 60.0;
                        tw = (tfer == true ? _ui.GetMiscIcon(UserInterface.MiscIconEnum.Connected) : _ui.GetMiscIcon(UserInterface.MiscIconEnum.Disconnected)).GetWrapOrEmpty();
                        ImGui.Image(tw.ImGuiHandle, new Vector2(tw.Width, tw.Height));
                        ImGui.SameLine();
                        ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPosX() - textofsx, ImGui.GetCursorPosY() + textofsy));
                        ImGui.TextWrapped(I18n.Translate("Status/StatusNetworkTrafficUp" + tfer));
                        if (tfer == false)
                        {
                            complaints.Add(I18n.Translate("Status/WarnNoTrafficUp"));
                        }
                    }
                }
                ImGui.TableSetColumnIndex(1);
                ImGui.BeginDisabled();
                ImGui.CollapsingHeader("  " + I18n.Translate("Status/CurrentlyActive"), ImGuiTreeNodeFlags.Leaf);
                ImGui.EndDisabled();
                ImGui.PushItemWidth(50.0f);
                string temp = _state.NumFeaturesAutomarker.ToString();
                ImGui.InputText(I18n.Translate("Status/NumFeaturesAutomarker"), ref temp, 64, ImGuiInputTextFlags.ReadOnly);
                temp = _state.NumFeaturesDrawing.ToString();
                ImGui.InputText(I18n.Translate("Status/NumFeaturesDrawing"), ref temp, 64, ImGuiInputTextFlags.ReadOnly);
                temp = _state.NumFeaturesSound.ToString();
                ImGui.InputText(I18n.Translate("Status/NumFeaturesSound"), ref temp, 64, ImGuiInputTextFlags.ReadOnly);
#if !SANS_GOETIA
                temp = _state.NumFeaturesHack.ToString();
                ImGui.InputText(I18n.Translate("Status/NumFeaturesHack"), ref temp, 64, ImGuiInputTextFlags.ReadOnly);
                temp = _state.NumFeaturesAutomation.ToString();
                ImGui.InputText(I18n.Translate("Status/NumFeaturesAutomation"), ref temp, 64, ImGuiInputTextFlags.ReadOnly);
#endif
                ImGui.PopItemWidth();
                ImGui.EndTable();
            }
            ImGui.BeginDisabled();
            ImGui.CollapsingHeader("  " + I18n.Translate("Status/ImpactToFunctionality"), ImGuiTreeNodeFlags.Leaf);
            ImGui.EndDisabled();
            ImGui.BeginChildFrame(1, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y));

            List<Blueprint.Region.Warning> warns = _state._dec.GetOpcodeWarnings();
            if (warns != null && warns.Count > 0)
            {
                foreach (Blueprint.Region.Warning w in warns)
                {
                    complaints.Add(w.Text);
                }
            }
#if !SANS_GOETIA
            if (_state.NumFeaturesHack > 0 && _state.cfg.QuickToggleHacks == true && _state.cfg.NagAboutStreaming == true)
            {
                complaints.Add(I18n.Translate("Status/WarnHacksActive"));
            }
            if (_state.NumFeaturesAutomation > 0 && _state.cfg.QuickToggleAutomation == true && _state.cfg.NagAboutStreaming == true)
            {
                complaints.Add(I18n.Translate("Status/WarnAutomationsActive"));
            }
#endif
            if (_state.NumFeaturesDrawing > 0 && _state.cfg.QuickToggleOverlays == true && _state.cfg.NagAboutStreaming == true)
            {
                complaints.Add(I18n.Translate("Status/WarnDrawsActive"));
            }
            if (_state.NumFeaturesAutomarker > 0 && _state.cfg.QuickToggleAutomarkers == false)
            {
                complaints.Add(I18n.Translate("Status/WarnAutomarkersQuickDisabled"));
            }
            if (_state.NumFeaturesDrawing > 0 && _state.cfg.QuickToggleOverlays == false)
            {
                complaints.Add(I18n.Translate("Status/WarnOverlaysQuickDisabled"));
            }
            if (_state.NumFeaturesSound > 0 && _state.cfg.QuickToggleSound == false)
            {
                complaints.Add(I18n.Translate("Status/WarnSoundQuickDisabled"));
            }
#if !SANS_GOETIA
            if (_state.NumFeaturesHack > 0 && _state.cfg.QuickToggleHacks == false)
            {
                complaints.Add(I18n.Translate("Status/WarnHackQuickDisabled"));
            }
            if (_state.NumFeaturesAutomation > 0 && _state.cfg.QuickToggleAutomation == false)
            {
                complaints.Add(I18n.Translate("Status/WarnAutomationQuickDisabled"));
            }
#endif
            if (complaints.Count == 0)
            {
                tw = _ui.GetMiscIcon(UserInterface.MiscIconEnum.Smiley).GetWrapOrEmpty();
                ImGui.Image(tw.ImGuiHandle, new Vector2(tw.Width, tw.Height));
                ImGui.SameLine();
                ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPosX() - textofsx, ImGui.GetCursorPosY() + textofsy));
                ImGui.TextWrapped(I18n.Translate("Status/AllIsWell"));
            }
            else
            {
                tw = _ui.GetMiscIcon(UserInterface.MiscIconEnum.RedCross).GetWrapOrEmpty();
                int i = 0;
                foreach (string c in complaints)
                {
                    ImGui.Image(tw.ImGuiHandle, new Vector2(tw.Width, tw.Height));
                    ImGui.SameLine();
                    ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPosX() - textofsx, ImGui.GetCursorPosY() + textofsy));
                    ImGui.TextWrapped(c);
                    if (++i < complaints.Count())
                    {
                        ImGui.Separator();
                    }
                }
            }
            ImGui.EndChildFrame();
        }

        internal object DeserializeXml<T>(XmlDocument doc)
        {
            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(T));
                byte[] buf = UTF8Encoding.UTF8.GetBytes(doc.OuterXml);
                using (MemoryStream ms = new MemoryStream(buf))
                {
                    T o = (T)xs.Deserialize(ms);
                    return o;
                }
            }
            catch (Exception ex)
            {
                GenericExceptionHandler(ex);
            }
            return null;
        }

        internal XmlDocument SerializeXml<T>(object o)
        {
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            XmlSerializer xs = new XmlSerializer(typeof(T));
            string temp = "";
            using (MemoryStream ms = new MemoryStream())
            {
                xs.Serialize(ms, o, ns);
                ms.Position = 0;
                using (StreamReader sr = new StreamReader(ms))
                {
                    temp = sr.ReadToEnd();
                }
            }
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(temp);
            return doc;
        }

        internal void ProcessDownloadQueue()
        {
            Downloadable d = null;
            do
            {
                lock (_downloadQueue)
                {
                    if (_downloadQueue.Count > 0)
                    {
                        d = _downloadQueue.Dequeue();
                    }
                    else
                    {
                        d = null;
                    }
                }
                if (d == null)
                {
                    continue;
                }
                _downloadPending = true;
                _downloadFilename = d.DownloadUrl;
                try
                {
                    string localfn = DownloadFileFromUri(d.DownloadUrl);
                    if (localfn == null)
                    {
                        d.OnFailure?.Invoke(d);
                    }
                    else
                    {
                        d.LocalFile = localfn;
                        d.OnSuccess?.Invoke(d);
                    }
                }
                catch (Exception ex)
                {
                    GenericExceptionHandler(ex);
                }
            }
            while (d != null);
            _downloadPending = false;
            _downloadFilename = "";
        }

        internal string DownloadFileFromUri(string uri)
        {
            try
            {
                Uri u = new Uri(uri);
                Log(LogLevelEnum.Debug, "Downloading file from URI {0}", uri);
                if (u.IsFile == true)
                {
                    Log(LogLevelEnum.Debug, "Local file, exists as {0}", u.LocalPath);
                    return u.LocalPath;
                }
                else
                {
                    string md5 = GenerateMD5Hash(uri);
                    string fileext = Path.GetExtension(u.AbsoluteUri);
                    string temp = Path.GetTempPath();
                    string dlfile = Path.Combine(temp, md5 + fileext);
                    if (File.Exists(dlfile) == true)
                    {
                        Log(LogLevelEnum.Debug, "Download {0} already exists as {1}", uri, dlfile);
                        return dlfile;
                    }
                    Log(LogLevelEnum.Debug, "Downloading from URI {0}..", uri);
                    using HttpClient http = new HttpClient();
                    using HttpRequestMessage req = new HttpRequestMessage()
                    {
                        Method = HttpMethod.Get,
                        RequestUri = u
                    };
                    using HttpResponseMessage resp = http.Send(req);
                    if (resp.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        Log(State.LogLevelEnum.Error, null, "Couldn't load file from {0}, response code was: {1}", uri, resp.StatusCode);
                        return null;
                    }
                    byte[] data;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        resp.Content.ReadAsStream().CopyTo(ms);
                        data = ms.ToArray();
                    }
                    Log(LogLevelEnum.Debug, "Downloaded {0}, writing to {1}", uri, dlfile);
                    File.WriteAllBytes(dlfile, data);
                    Log(LogLevelEnum.Debug, "Downloaded {0} as {1}", uri, dlfile);
                    return dlfile;
                }
            }
            catch (Exception ex)
            {
                GenericExceptionHandler(ex);
            }
            return null;
        }

        public void MainThreadProc(object o)
        {
            Plugin p = (Plugin)o;
            WaitHandle[] wh = new WaitHandle[4];
            wh[0] = p._stopEvent;
            wh[1] = _state.InvoqThreadNew;
            wh[2] = p._downloadRequestEvent;
            wh[3] = p._retryEvent;
            int timeout = 0;
            int tries = 0;
            bool ready = false;
            try
            {
                while (true)
                {
                    switch (WaitHandle.WaitAny(wh, timeout))
                    {
                        case 0:
                            return;
                        case 1:
                            timeout = _state.ProcessInvocations(_state.InvoqThread);
                            break;
                        case 2:
                            ProcessDownloadQueue();
                            break;
                        case 3:
                            Log(LogLevelEnum.Debug, "Going to reload opcodes");
                            _state.UnprepareInternals();
                            ready = false;
                            tries = 0;
                            timeout = 0;
                            break;
                        case WaitHandle.WaitTimeout:
                            if (ready == false)
                            {
                                if (_state.PrepareInternals(tries >= 5) == true)
                                {
                                    ready = true;
                                }
                                else
                                {
                                    timeout = 10000;
                                    tries++;
                                }
                            }
                            else
                            {
                                timeout = _state.ProcessInvocations(_state.InvoqThread);
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                p.Log(LogLevelEnum.Error, "Exception in thread proc: {0} at {1}", ex.Message, ex.StackTrace);
            }
        }

    }

}