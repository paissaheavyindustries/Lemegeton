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
using Dalamud.Game.ClientState.Conditions;
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

namespace Lemegeton
{

    public sealed class Plugin : IDalamudPlugin
    {

#if !SANS_GOETIA
        public string Name => "Lemegeton Goetia";
#else
        public string Name => "Lemegeton";
#endif
        public string Version = "v1.0.0.13";

        private State _state = new State();
        private Thread _mainThread = null;
        private ManualResetEvent _stopEvent = new ManualResetEvent(false);
        private AutoResetEvent _retryEvent = new AutoResetEvent(false);
        private float _adjusterX = 0.0f;
        private DateTime _loaded = DateTime.Now;
        private bool _aboutProg = false;
        private bool _softMarkerPreview = false;
        private DateTime _aboutOpened;
        private Dictionary<Delegate, string[]> _delDebugInput = new Dictionary<Delegate, string[]>();

        private Dictionary<int, TextureWrap> _misc = new Dictionary<int, TextureWrap>();
        private Dictionary<AutomarkerSigns.SignEnum, TextureWrap> _signs = new Dictionary<AutomarkerSigns.SignEnum, TextureWrap>();
        private Dictionary<AutomarkerPrio.PrioRoleEnum, TextureWrap> _roles = new Dictionary<AutomarkerPrio.PrioRoleEnum, TextureWrap>();
        private Dictionary<AutomarkerPrio.PrioTrinityEnum, TextureWrap> _trinity = new Dictionary<AutomarkerPrio.PrioTrinityEnum, TextureWrap>();
        private Dictionary<AutomarkerPrio.PrioJobEnum, TextureWrap> _jobs = new Dictionary<AutomarkerPrio.PrioJobEnum, TextureWrap>();
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

        private object _dragObject = null;
        private bool _isDragging = false;
        private int _dragStartItem = 0;
        private int _dragEndItem = 0;

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
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] GameNetwork gameNetwork,
            [RequiredVersion("1.0")] ChatGui chatGui,
            [RequiredVersion("1.0")] CommandManager commandManager,
            [RequiredVersion("1.0")] ObjectTable objectTable,
            [RequiredVersion("1.0")] GameGui gameGui,
            [RequiredVersion("1.0")] ClientState clientState,
            [RequiredVersion("1.0")] DataManager dataManager,
            [RequiredVersion("1.0")] Condition condition,
            [RequiredVersion("1.0")] Framework framework,
            [RequiredVersion("1.0")] SigScanner sigScanner,
            [RequiredVersion("1.0")] PartyList partylist,
            [RequiredVersion("1.0")] TargetManager targetManager
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
                cd = condition,
                fw = framework,
                ss = sigScanner,
                pl = partylist,
                tm = targetManager,
                plug = this
            };            
            LoadConfig();
            InitializeContent();
            _state.Initialize();
            InitializeLanguage();
            ApplyConfigToContent();
            ChangeLanguage(_state.cfg.Language);
            LoadTextures();            
            _mainThread = new Thread(new ParameterizedThreadStart(MainThreadProc));
            _mainThread.Name = "Lemegeton main thread";
            _mainThread.Start(this);
            _state.cs.Login += Cs_Login;
            _state.cs.Logout += Cs_Logout;            
            if (_state.cs.IsLoggedIn == true)
            {
                Cs_Login(null, null);
            }
        }

        private void Cs_Login(object sender, EventArgs e)
        {
            lock (this)
            {
                if (_drawingCallback == false)
                {
                    _state.pi.UiBuilder.Draw += DrawUI;
                    _drawingCallback = true;
                }
            }
        }

        private void Cs_Logout(object sender, EventArgs e)
        {
            lock (this)
            {
                if (_drawingCallback == true)
                {
                    _state.pi.UiBuilder.Draw -= DrawUI;
                    _drawingCallback = false;
                }
            }
            SaveConfig();
        }

        public void Dispose()
        {
            _state.cs.Logout -= Cs_Logout;
            _state.cs.Login -= Cs_Login;
            Cs_Logout(null, null);
            _state.Uninitialize();
            _stopEvent.Set();            
            UnloadTextures();
            SaveConfig();
            if (_state.InvoqThreadNew != null)
            {
                _state.InvoqThreadNew.Dispose();
            }
            _mainThread.Join(1000);
            _stopEvent.Dispose();
            _retryEvent.Dispose();
        }

        internal TextureWrap GetJobIcon(uint jobId)
        {
            AutomarkerPrio.PrioJobEnum job = (AutomarkerPrio.PrioJobEnum)jobId;
            if (_jobs.ContainsKey(job) == true)
            {
                return _jobs[job];
            }
            return null;
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

        private void LoadTextures()
        {
            _misc[1] = GetTexture(33237);
            _misc[2] = GetTexture(63937);
            _misc[3] = GetTexture(63939);
            _misc[4] = GetTexture(63938);
            _misc[5] = GetTexture(61551);
            _misc[6] = GetTexture(61552);
            _misc[7] = GetTexture(61555);
            _misc[8] = GetTexture(61553);
            _misc[11] = GetTexture(66162);
            _misc[12] = GetTexture(66163);
            _misc[13] = GetTexture(66164);
            _misc[14] = GetTexture(66165);
            _misc[15] = GetTexture(66166);
            _misc[16] = GetTexture(66167);
            _misc[17] = GetTexture(66168);
            _misc[18] = GetTexture(66169);
            _signs[AutomarkerSigns.SignEnum.Attack1] = GetTexture(61201);
            _signs[AutomarkerSigns.SignEnum.Attack2] = GetTexture(61202);
            _signs[AutomarkerSigns.SignEnum.Attack3] = GetTexture(61203);
            _signs[AutomarkerSigns.SignEnum.Attack4] = GetTexture(61204);
            _signs[AutomarkerSigns.SignEnum.Attack5] = GetTexture(61205);
            _signs[AutomarkerSigns.SignEnum.Bind1] = GetTexture(61211);
            _signs[AutomarkerSigns.SignEnum.Bind2] = GetTexture(61212);
            _signs[AutomarkerSigns.SignEnum.Bind3] = GetTexture(61213);
            _signs[AutomarkerSigns.SignEnum.Ignore1] = GetTexture(61221);
            _signs[AutomarkerSigns.SignEnum.Ignore2] = GetTexture(61222);
            _signs[AutomarkerSigns.SignEnum.Square] = GetTexture(61231);
            _signs[AutomarkerSigns.SignEnum.Circle] = GetTexture(61232);
            _signs[AutomarkerSigns.SignEnum.Plus] = GetTexture(61233);
            _signs[AutomarkerSigns.SignEnum.Triangle] = GetTexture(61234);
            _signs[AutomarkerSigns.SignEnum.AttackNext] = _signs[AutomarkerSigns.SignEnum.Attack1];
            _signs[AutomarkerSigns.SignEnum.BindNext] = _signs[AutomarkerSigns.SignEnum.Bind1];
            _signs[AutomarkerSigns.SignEnum.IgnoreNext] = _signs[AutomarkerSigns.SignEnum.Ignore1];
            _trinity[AutomarkerPrio.PrioTrinityEnum.Tank] = GetTexture(62581);
            _trinity[AutomarkerPrio.PrioTrinityEnum.Healer] = GetTexture(62582);
            _trinity[AutomarkerPrio.PrioTrinityEnum.DPS] = GetTexture(62583);
            _roles[AutomarkerPrio.PrioRoleEnum.Tank] = GetTexture(62581);
            _roles[AutomarkerPrio.PrioRoleEnum.Healer] = GetTexture(62582);
            _roles[AutomarkerPrio.PrioRoleEnum.Melee] = GetTexture(62584);
            _roles[AutomarkerPrio.PrioRoleEnum.Ranged] = GetTexture(62586);
            _roles[AutomarkerPrio.PrioRoleEnum.Caster] = GetTexture(62587);            
            _jobs[AutomarkerPrio.PrioJobEnum.PLD] = GetTexture(62119);
            _jobs[AutomarkerPrio.PrioJobEnum.WAR] = GetTexture(62121);
            _jobs[AutomarkerPrio.PrioJobEnum.DRK] = GetTexture(62132);
            _jobs[AutomarkerPrio.PrioJobEnum.GNB] = GetTexture(62137);
            _jobs[AutomarkerPrio.PrioJobEnum.WHM] = GetTexture(62124);
            _jobs[AutomarkerPrio.PrioJobEnum.SCH] = GetTexture(62128);
            _jobs[AutomarkerPrio.PrioJobEnum.AST] = GetTexture(62133); 
            _jobs[AutomarkerPrio.PrioJobEnum.SGE] = GetTexture(62140);
            _jobs[AutomarkerPrio.PrioJobEnum.MNK] = GetTexture(62120);
            _jobs[AutomarkerPrio.PrioJobEnum.DRG] = GetTexture(62122);
            _jobs[AutomarkerPrio.PrioJobEnum.NIN] = GetTexture(62130);
            _jobs[AutomarkerPrio.PrioJobEnum.SAM] = GetTexture(62134);
            _jobs[AutomarkerPrio.PrioJobEnum.RPR] = GetTexture(62139);
            _jobs[AutomarkerPrio.PrioJobEnum.BRD] = GetTexture(62123);
            _jobs[AutomarkerPrio.PrioJobEnum.MCH] = GetTexture(62131);
            _jobs[AutomarkerPrio.PrioJobEnum.DNC] = GetTexture(62138);
            _jobs[AutomarkerPrio.PrioJobEnum.BLM] = GetTexture(62125);
            _jobs[AutomarkerPrio.PrioJobEnum.SMN] = GetTexture(62127);
            _jobs[AutomarkerPrio.PrioJobEnum.RDM] = GetTexture(62135);
        }

        private void UnloadTextures()
        {
            foreach (KeyValuePair<AutomarkerSigns.SignEnum, TextureWrap> kp in _signs)
            {
                if (kp.Value != null)
                {
                    kp.Value.Dispose();
                }
            }
            _signs.Clear();
            foreach (KeyValuePair<int, TextureWrap> kp in _misc)
            {
                if (kp.Value != null)
                {
                    kp.Value.Dispose();
                }
            }
            _misc.Clear();
            foreach (KeyValuePair<AutomarkerPrio.PrioTrinityEnum, TextureWrap> kp in _trinity)
            {
                if (kp.Value != null)
                {
                    kp.Value.Dispose();
                }
            }
            _trinity.Clear();
            foreach (KeyValuePair<AutomarkerPrio.PrioRoleEnum, TextureWrap> kp in _roles)
            {
                if (kp.Value != null)
                {
                    kp.Value.Dispose();
                }
            }
            _roles.Clear();
            foreach (KeyValuePair<AutomarkerPrio.PrioJobEnum, TextureWrap> kp in _jobs)
            {
                if (kp.Value != null)
                {
                    kp.Value.Dispose();
                }
            }
            _jobs.Clear();
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
                else if (pi.PropertyType == typeof(Action))
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
                    else if (pi.PropertyType == typeof(Action))
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
            Log(State.LogLevelEnum.Debug, "Configuration backup saved to {0}", tempfile);
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
            string tag = data.Substring(0, 10);
#if !SANS_GOETIA
            string mytag = "Lemegeton2_";
#else
            string mytag = "Lemegeton1_";
#endif
            if (String.Compare(tag, mytag) != 0)
            {
                Log(LogLevelEnum.Error, "This config snapshot is from {0}, while this instance is {1}!", tag, mytag);
                return null;
            }
            string md5 = data.Substring(10, 32);
            string blob = data.Substring(42);
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
                //Log(LogLevelEnum.Debug, "Creating Language {0}", type.Name.ToString());
                Core.Language c = (Core.Language)Activator.CreateInstance(type);
                I18n.AddLanguage(c);
            }
            Core.Language def = I18n.DefaultLanguage;
            foreach (var kp in I18n.RegisteredLanguages)
            {
                kp.Value.CalculateCoverage(def);
            }
        }


        internal Vector3 TranslateToScreen(double x, double y, double z)
        {
            Vector2 tenp;
            _state.gg.WorldToScreen(
                new Vector3((float)x, (float)y, (float)z),
                out tenp
            );
            return new Vector3(tenp.X, tenp.Y, (float)z);
        }

        internal TextureWrap? GetTexture(uint id)
        {
            return _state.dm.GetImGuiTextureIcon(id);
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


        private string TranslateOrderableItem<T>(T item)
        {
            if (item is PrioRoleEnum)
            {
                PrioRoleEnum tmp = (PrioRoleEnum)Convert.ChangeType(item, typeof(PrioRoleEnum));
                return I18n.Translate("Role/" + tmp.ToString());
            }
            if (item is PrioTrinityEnum)
            {
                PrioTrinityEnum tmp = (PrioTrinityEnum)Convert.ChangeType(item, typeof(PrioTrinityEnum));
                return I18n.Translate("Trinity/" + tmp.ToString());
            }
            if (item is PrioJobEnum)
            {
                PrioJobEnum tmp = (PrioJobEnum)Convert.ChangeType(item, typeof(PrioJobEnum));
                return I18n.Translate("Job/" + tmp.ToString());
            }
            if (item is int)
            {
                return I18n.Translate("Automarker/PrioType/PartyMember");
            }
            if (item is string)
            {
                return item.ToString();
            }
            return "???";
        }

        private TextureWrap? RetrieveOrderableIcon<T>(T item)
        {
            if (item is PrioRoleEnum)
            {
                PrioRoleEnum tmp = (PrioRoleEnum)Convert.ChangeType(item, typeof(PrioRoleEnum));
                return _roles[tmp];
            }
            if (item is PrioTrinityEnum)
            {
                PrioTrinityEnum tmp = (PrioTrinityEnum)Convert.ChangeType(item, typeof(PrioTrinityEnum));
                return _trinity[tmp];
            }
            if (item is PrioJobEnum)
            {
                PrioJobEnum tmp = (PrioJobEnum)Convert.ChangeType(item, typeof(PrioJobEnum));
                return _jobs[tmp];
            }
            if (item is int)
            {
                int tmp = (int)Convert.ChangeType(item, typeof(int));
                return _misc[10 + tmp];
            }
            if (item is string)
            {
                return null;
            }
            return null;
        }

        private void RenderOrderableItem<T>(T item, int index, float x, float y)
        {
            Vector2 btnsize = new Vector2(160, 50);
            Vector2 icosize = new Vector2(30, 30);
            Vector2 pt = ImGui.GetCursorPos();
            string text = TranslateOrderableItem(item);
            TextureWrap? icon = RetrieveOrderableIcon(item);
            float icospace = 0.0f;
            if (icon != null)
            {
                ImGui.SetCursorPos(new Vector2(x + (btnsize.X - icosize.X) - 5.0f, y + (btnsize.Y / 2.0f) - (icosize.Y / 2.0f)));
                ImGui.Image(icon.ImGuiHandle, icosize);
                icospace = icosize.X;
            }
            ImGui.SetCursorPos(new Vector2(x + 5.0f, y + (btnsize.Y / 2.0f) - ImGui.GetFontSize() / 2.0f));
            ImGui.Text((index + 1).ToString());
            ImGui.SetCursorPos(new Vector2(x + (btnsize.X - icospace - 10.0f) - ImGui.CalcTextSize(text).X, y + (btnsize.Y / 2.0f) - (ImGui.GetFontSize() / 2.0f)));
            ImGui.Text(text);
            ImGui.SetCursorPos(pt);
        }

        private void RenderOrderableList<T>(List<T> items)
        {
            Vector2 maxsize = ImGui.GetContentRegionAvail();
            Vector2 btnsize = new Vector2(160, 50);
            Vector2 margin = new Vector2(10, 10);
            Vector2 icosize = new Vector2(30, 30);
            float x = 0.0f, y = 0.0f;
            Vector2 curpos = ImGui.GetCursorPos();
            ImGuiStylePtr style = ImGui.GetStyle();
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0.0f, 0.0f));
            int perRow = (int)Math.Floor(maxsize.X / (btnsize.X + margin.X));
            perRow = Math.Clamp(perRow, 1, items.Count > 0 ? items.Count : 1);
            int itemx = 1, itemy = 1;
            bool isStillDragging = false;
            Vector2 screenpos = ImGui.GetCursorScreenPos();
            float time = (float)((DateTime.Now - _loaded).TotalMilliseconds / 300.0);
            Vector4 hilite = new Vector4(1.0f, 1.0f, 0.5f + 0.5f * (float)Math.Abs(Math.Cos(time)), 0.5f + 0.5f * (float)Math.Abs(Math.Cos(time)));
            for (int i = 0; i < items.Count; i++)
            {
                T p = items[i];
                string temp = "##OrderableList_" + i;
                Vector2 curItem = new Vector2(curpos.X + x, curpos.Y + y);
                ImGui.SetCursorPos(curItem);
                ImGui.Selectable(temp, true, ImGuiSelectableFlags.None, btnsize);
                x += btnsize.X + margin.X;
                if (ImGui.IsItemHovered() == true && ImGui.IsItemActive() == false)
                {
                    ImGui.BeginTooltip();
                    ImGui.Text(I18n.Translate("Misc/DragToReorderPrio"));
                    ImGui.EndTooltip();
                }
                if (ImGui.IsItemActive() == true)
                {
                    isStillDragging = true;
                    if (_isDragging == false)
                    {
                        _isDragging = true;
                        _dragObject = items;
                        _dragStartItem = i;
                    }
                    ImDrawListPtr dl = ImGui.GetWindowDrawList();
                    if (ImGui.IsItemHovered() == false)
                    {
                        Vector2 mpos = ImGui.GetMousePos();
                        int xpos = (int)Math.Floor((mpos.X - screenpos.X) / (btnsize.X + margin.X));
                        int ypos = (int)Math.Floor((mpos.Y - screenpos.Y) / (btnsize.Y + margin.Y));
                        xpos = Math.Clamp(xpos, 0, perRow - 1);
                        ypos = Math.Clamp(ypos, 0, (int)Math.Ceiling((float)items.Count / perRow) - 1);
                        int xtpos = (int)Math.Floor((mpos.X - screenpos.X + (btnsize.X / 2.0f)) / (btnsize.X + margin.X));
                        int jpos, ipos = (ypos * perRow) + xpos;
                        ipos = Math.Clamp(ipos, 0, items.Count);
                        int lim = perRow * (int)Math.Floor((float)ipos / perRow);
                        lim = items.Count - lim;
                        xpos = Math.Clamp(xpos, 0, lim - 1);
                        ipos = (ypos * perRow) + xpos;
                        if (i != ipos)
                        {
                            jpos = (xtpos > xpos) ? 1 : -1;
                            float cpx = (xpos * (btnsize.X + margin.X) + screenpos.X);
                            float cpy = (ypos * (btnsize.Y + margin.Y) + screenpos.Y);
                            if (jpos < 0)
                            {
                                dl.AddLine(new Vector2(cpx, cpy), new Vector2(cpx, cpy + btnsize.Y), ImGui.GetColorU32(hilite), 3.0f);
                                dl.AddTriangleFilled(
                                    new Vector2(cpx, cpy),
                                    new Vector2(cpx + 10.0f, cpy),
                                    new Vector2(cpx, cpy + 10.0f),
                                    ImGui.GetColorU32(hilite)
                                );
                                dl.AddTriangleFilled(
                                    new Vector2(cpx, cpy + btnsize.Y),
                                    new Vector2(cpx + 10.0f, cpy + btnsize.Y),
                                    new Vector2(cpx, cpy + btnsize.Y - 10.0f),
                                    ImGui.GetColorU32(hilite)
                                );
                                _dragEndItem = ipos;
                            }
                            else
                            {
                                dl.AddLine(new Vector2(cpx + btnsize.X, cpy), new Vector2(cpx + btnsize.X, cpy + btnsize.Y), ImGui.GetColorU32(hilite), 3.0f);
                                dl.AddTriangleFilled(
                                    new Vector2(cpx + btnsize.X, cpy),
                                    new Vector2(cpx + btnsize.X - 10.0f, cpy),
                                    new Vector2(cpx + btnsize.X, cpy + 10.0f),
                                    ImGui.GetColorU32(hilite)
                                );
                                dl.AddTriangleFilled(
                                    new Vector2(cpx + btnsize.X, cpy + btnsize.Y),
                                    new Vector2(cpx + btnsize.X - 10.0f, cpy + btnsize.Y),
                                    new Vector2(cpx + btnsize.X, cpy + btnsize.Y - 10.0f),
                                    ImGui.GetColorU32(hilite)
                                );
                                _dragEndItem = ipos + 1;
                            }
                        }
                    }
                    else
                    {
                        _dragEndItem = _dragStartItem;
                    }
                    {
                        Vector2 cpos = Vector2.Add(ImGui.GetWindowPos(), curItem);
                        cpos = Vector2.Subtract(cpos, new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY()));
                        dl.AddRect(cpos, new Vector2(cpos.X + btnsize.X, cpos.Y + btnsize.Y), ImGui.GetColorU32(hilite), 0.0f, ImDrawFlags.None, 2.0f);
                    }
                }
                itemx++;
                if (itemx > perRow)
                {
                    x = 0.0f;
                    y += btnsize.Y + margin.Y;
                    itemy++;
                    itemx = 1;
                }
                else if (i < items.Count - 1)
                {
                    ImGui.SameLine();
                }
                RenderOrderableItem(p, i, curItem.X, curItem.Y);
            }
            ImGui.PopStyleVar();
            if (isStillDragging == false && _dragObject == items)
            {
                _dragEndItem = Math.Clamp(_dragEndItem, 0, items.Count);
                _isDragging = false;
                if (_dragStartItem != _dragEndItem)
                {
                    var tmp = items[_dragStartItem];
                    items.Remove(tmp);
                    items.Insert(_dragEndItem >= _dragStartItem ? _dragEndItem - 1 : _dragEndItem, tmp);
                }
                _dragStartItem = 0;
                _dragEndItem = 0;
                _dragObject = null;
            }
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
            RenderOrderableList<int>(amp._prioByPlCustom);
        }

        private void RenderAutomarkerPrioTrinity(AutomarkerPrio amp)
        {
            RenderOrderableList<PrioTrinityEnum>(amp._prioByTrinity);
        }

        private void RenderAutomarkerPrioRole(AutomarkerPrio amp)
        {
            RenderOrderableList<PrioRoleEnum>(amp._prioByRole);
        }

        private void RenderAutomarkerPrioJob(AutomarkerPrio amp)
        {
            RenderOrderableList<PrioJobEnum>(amp._prioByJob);
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
            RenderOrderableList<string>(amp._prioByPlayer);
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
                    foreach(KeyValuePair<string, AutomarkerSigns.Preset> kp in ams.Presets)
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
                    string ico;
                    if (saveEnabled == false)
                    {
                        ImGui.BeginDisabled();
                    }
                    ImGui.PushFont(UiBuilder.IconFont);
                    ico = FontAwesomeIcon.Save.ToIconString();
                    string popupname = path + "/" + pi.Name + "/SavePresetPopup";
                    if (ImGui.Button(ico) == true)
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
                    ImGui.PopFont();
                    if (ImGui.IsItemHovered() == true && ImGui.IsItemActive() == false)
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text(I18n.Translate("Misc/SavePreset"));
                        ImGui.EndTooltip();
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
                        ImGui.PushFont(UiBuilder.IconFont);
                        ico = FontAwesomeIcon.Save.ToIconString();
                        if (goodname == false)
                        {
                            ImGui.BeginDisabled();
                        }
                        if (ImGui.Button(ico) == true)
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
                        ImGui.PopFont();
                        if (ImGui.IsItemHovered() == true && ImGui.IsItemActive() == false)
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text(I18n.Translate("Misc/SavePreset"));
                            ImGui.EndTooltip();
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
                    ImGui.PushFont(UiBuilder.IconFont);
                    ico = FontAwesomeIcon.Trash.ToIconString();
                    if (ImGui.Button(ico) == true)
                    {
                        Log(LogLevelEnum.Debug, "Deleting preset {0} from {1}", ams.SelectedPreset, path);
                        ams.DeletePreset(ams.SelectedPreset);
                    }
                    ImGui.PopFont();
                    if (ImGui.IsItemHovered() == true && ImGui.IsItemActive() == false)
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text(I18n.Translate("Misc/DeletePreset"));
                        ImGui.EndTooltip();
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
                    TextureWrap tw = _signs[kp.Value];
                    ImGui.Image(tw.ImGuiHandle, new Vector2(tw.Width, tw.Height), new Vector2(0.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
                    ImGui.SameLine();
                }
                else
                {
                    TextureWrap tw = _signs[AutomarkerSigns.SignEnum.Attack1];                    
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
                else if (pi.PropertyType == typeof(Action))
                {
                    Action act = (Action)pi.GetValue(cm);
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
                                TextureWrap t3 = contentItem.Value.Enabled == true ? (contentItem.Value.Active == true ? _misc[2] : _misc[3]) : _misc[4];
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
                        TextureWrap t2 = content.Value.Enabled == true ? (content.Value.Active == true ? _misc[2] : _misc[3]) : _misc[4];
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
                TextureWrap t1 = contentCat.Enabled == true ? (contentCat.Active == true ? _misc[2] : _misc[3]) : _misc[4];
                ImGui.Image(t1.ImGuiHandle, new Vector2(20, 20));
                ImGui.SetCursorPos(b1);
            }
        }

        private void DrawUI()
        {
            _state.TrackObjects();
            _softMarkerPreview = false;
            DrawConfig();
            DrawContent();
            DrawSoftmarkers();
            _state.EndDrawing();
        }

        private void DrawContent()
        {
            _state.NumFeaturesAutomarker = 0;
            _state.NumFeaturesDrawing = 0;
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

        private void DrawSoftmarkerOn(AutomarkerSigns.SignEnum sign, uint actorId)
        {
            if (actorId == 0 || sign == AutomarkerSigns.SignEnum.None)
            {
                return;
            }
            GameObject go = _state.GetActorById(actorId);
            if (go == null)
            {
                return;
            }
            if (_state.StartDrawing(out ImDrawListPtr draw) == false)
            {
                return;
            }
            float mul = (float)Math.Abs(Math.Cos(DateTime.Now.Millisecond / 1000.0f * Math.PI));
            Vector3 temp = TranslateToScreen(
                go.Position.X + _state.cfg.SoftmarkerOffsetWorldX, 
                go.Position.Y + _state.cfg.SoftmarkerOffsetWorldY + (_state.cfg.SoftmarkerBounce == true ? (0.5f * mul * _state.cfg.SoftmarkerScale) : 0.0f), 
                go.Position.Z + _state.cfg.SoftmarkerOffsetWorldZ
            );
            Vector2 pt = new Vector2(
                temp.X + _state.cfg.SoftmarkerOffsetScreenX, 
                temp.Y + _state.cfg.SoftmarkerOffsetScreenY
            );
            TextureWrap tw = _signs[sign];
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
                DrawSoftmarkerOn(sign, _state.cs.LocalPlayer.ObjectId);
            }
            else
            {
                foreach (KeyValuePair<AutomarkerSigns.SignEnum, uint> kp in _state.SoftMarkers)
                {
                    DrawSoftmarkerOn(kp.Key, kp.Value);
                }
            }
        }

        private bool _movingShortcut = false;
        private Vector2 _movingMouse;

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
                TextureWrap tw = _misc[1];
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
                    Vector2 pt = ImGui.GetWindowPos();
                    bool moved = false;
                    Vector2 sz = ImGui.GetIO().DisplaySize;
                    if (pt.X < 0)
                    {
                        pt.X += (0.0f - pt.X) / 5.0f;
                        moved = true;
                    }
                    if (pt.Y < 0)
                    {
                        pt.Y += (0.0f - pt.Y) / 5.0f;
                        moved = true;
                    }
                    if (pt.X + tw.Width + 10 > sz.X)
                    {
                        pt.X -= ((pt.X + tw.Width + 10) - sz.X) / 5.0f;
                        moved = true;
                    }
                    if (pt.Y + tw.Height + 10 > sz.Y)
                    {
                        pt.Y -= ((pt.Y + tw.Height + 10) - sz.Y) / 5.0f;
                        moved = true;
                    }
                    if (moved == true)
                    {
                        ImGui.SetWindowPos(pt);
                    }
                }
                ImGui.End();
            }
        }

        private bool DrawAboutTab(bool forceOpen)
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

        private void DrawConfig()
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
            ImGui.BeginChild("LemmyFrame", fsz);
            ImGui.BeginTabBar("Lemmytabs");
            // status
            if (ImGui.BeginTabItem(I18n.Translate("MainMenu/Status")) == true)
            {
                ImGui.BeginChild("MainMenu/Status"); 
                RenderStatus();
                ImGui.EndChild();
                ImGui.EndTabItem();
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
                        ImGui.TextWrapped(I18n.Translate("MainMenu/Settings/SoftmarkerPreviewActive") + Environment.NewLine + Environment.NewLine);
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
                        RenderMethodCall(_state.InvokeMapEffect);
                        RenderMethodCall(_state.InvokeEventPlay);
                        ImGui.PopItemWidth();
                        ImGui.Unindent(30.0f);
                        ImGui.PopID();
                    }
                    ImGui.Unindent(30.0f);
                    ImGui.PopID();
                }
                ImGui.EndChild();
                ImGui.EndTabItem();
            }
            // about
            if (DrawAboutTab(aboutForceOpen) == true)
            {
                if (_aboutProg == false)
                {
                    _aboutOpened = DateTime.Now;
                    _aboutProg = true;
                }
                ImGui.BeginChild("MainMenu/About");
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
                ImGui.EndChild();
                ImGui.EndTabItem();
            }
            else
            {
                _aboutProg = false;
            }
            ImGui.EndTabBar();
            ImGui.EndChild();
            ImGui.Separator();
            Vector2 fp = ImGui.GetCursorPos();
            ImGui.SetCursorPosY(fp.Y + 2);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.3f, 0.3f, 0.3f, 1.0f));
            ImGui.Text(Version + " - " + _state.GameVersion);
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
            Vector2 pt = ImGui.GetWindowPos();
            Vector2 szy = ImGui.GetWindowSize();
            bool moved = false;
            Vector2 szx = ImGui.GetIO().DisplaySize;
            if (szy.X > szx.X || szy.Y > szx.Y)
            {
                szy.X = Math.Min(szy.X, szx.X);
                szy.Y = Math.Min(szy.Y, szx.Y);
                ImGui.SetWindowSize(szy);
            }
            if (pt.X < 0)
            {
                pt.X += (0.0f - pt.X) / 5.0f;
                moved = true;
            }
            if (pt.Y < 0)
            {
                pt.Y += (0.0f - pt.Y) / 5.0f;
                moved = true;
            }
            if (pt.X + szy.X > szx.X)
            {
                pt.X -= ((pt.X + szy.X) - szx.X) / 5.0f;
                moved = true;
            }
            if (pt.Y + szy.Y > szx.Y)
            {
                pt.Y -= ((pt.Y + szy.Y) - szx.Y) / 5.0f;
                moved = true;
            }
            if (moved == true)
            {
                ImGui.SetWindowPos(pt);
            }
            ImGui.End();
            ImGui.PopStyleColor(3);
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
                    if (p.ParameterType == typeof(GameObject))
                    {
                        string temp = _delDebugInput[del][k];
                        uint actorId = uint.Parse(temp, System.Globalization.NumberStyles.HexNumber);
                        GameObject go = _state.GetActorById(actorId);
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

        private void RenderStatus()
        {
            TextureWrap tw;
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
                        tw = _state.StatusGotOpcodes == true ? _misc[5] : _misc[6];
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
                        tw = _state.StatusMarkingFuncAvailable == true ? _misc[5] : _misc[6];
                        ImGui.Image(tw.ImGuiHandle, new Vector2(tw.Width, tw.Height));
                        ImGui.SameLine();
                        ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPosX() - textofsx, ImGui.GetCursorPosY() + textofsy));
                        ImGui.TextWrapped(I18n.Translate("Status/StatusMarkingFuncAvailable" + _state.StatusMarkingFuncAvailable));
                        tw = _state.StatusPostCommandAvailable == true ? _misc[5] : _misc[6];
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
                        tw = tfer == true ? _misc[7] : _misc[8];
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
                        tw = tfer == true ? _misc[7] : _misc[8];
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
                tw = _misc[5];
                ImGui.Image(tw.ImGuiHandle, new Vector2(tw.Width, tw.Height));
                ImGui.SameLine();
                ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPosX() - textofsx, ImGui.GetCursorPosY() + textofsy));
                ImGui.TextWrapped(I18n.Translate("Status/AllIsWell"));
            }
            else
            {
                tw = _misc[6];
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

        public void MainThreadProc(object o)
        {
            Plugin p = (Plugin)o;
            WaitHandle[] wh = new WaitHandle[3];
            wh[0] = p._stopEvent;
            wh[1] = _state.InvoqThreadNew;
            wh[2] = p._retryEvent;
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