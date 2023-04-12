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
using Dalamud;
using System.ComponentModel;
using System.Globalization;
using FFXIVClientStructs.Havok;
using Dalamud.Interface;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Xml.Serialization;

namespace Lemegeton
{

    public sealed class Plugin : IDalamudPlugin
    {

        public string VersionInfo => "v0.9";
        public string Name => "Lemegeton";

        private State _state = new State();
        private Thread _mainThread = null;
        private ManualResetEvent _stopEvent = new ManualResetEvent(false);
        private float _adjusterX = 0.0f;
        private DateTime _loaded = DateTime.Now;
        private bool _aboutProg = false;
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
        private DateTime _lemmyShortcutPopped;
        private Rectangle _lastSeen = new Rectangle();

        private string[] _aboutScroller = new string[] {
            "LEMEGETON",
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
            [RequiredVersion("1.0")] PartyList partylist
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
                plug = this
            };
            InitializeContent();
            _state.Initialize();
            InitializeLanguage();
            LoadConfig();
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
            Log(State.LogLevelEnum.Info, "Language chandged to {0}", I18n.CurrentLanguage.LanguageName);            
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
                List<Tuple<PropertyInfo, int>> props = GetConfigurableProperties(cc, false);
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
                    List<Tuple<PropertyInfo, int>> props = GetConfigurableProperties(content.Value, false);
                    AddPropertiesToNode((XmlElement)nc, content.Value, props);
                }
                foreach (KeyValuePair<string, Core.ContentItem> contentItem in content.Value.Items)
                {
                    XmlNode nci = doc.CreateElement(contentItem.Key);
                    nc.AppendChild(nci);
                    {
                        List<Tuple<PropertyInfo, int>> props = GetConfigurableProperties(contentItem.Value, false);
                        AddPropertiesToNode((XmlElement)nci, contentItem.Value, props);
                    }
                }
            }
            return n;
        }

        private void DeserializeAttributesToObject(XmlNode n, object o)
        {
            List<Tuple<PropertyInfo, int>> props = GetConfigurableProperties(o, false);
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
                        pi.SetValue(o, v);
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
                        pi.SetValue(o, v);
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
                        pi.SetValue(o, v);
                    }
                    else if (pi.PropertyType == typeof(AutomarkerSigns))
                    {
                        AutomarkerSigns v = (AutomarkerSigns)pi.GetValue(o);
                        v.Deserialize(attr.Value);
                    }
                    else if (pi.PropertyType == typeof(AutomarkerPrio))
                    {
                        AutomarkerPrio v = (AutomarkerPrio)pi.GetValue(o);
                        v.Deserialize(attr.Value);
                    }
                    else if (pi.PropertyType == typeof(Action))
                    {
                    }
                    else if (pi.PropertyType.IsSubclassOf(typeof(CustomPropertyInterface)))
                    {
                        CustomPropertyInterface v = (CustomPropertyInterface)pi.GetValue(o);
                        v.Deserialize(attr.Value);
                    }
                    else
                    {
                        object val = Convert.ChangeType(attr.Value, pi.PropertyType);
                        pi.SetValue(o, val);
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
            ClientLanguage c;
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
            _state.Log(State.LogLevelEnum.Info, null, "Loading configuration");
            _state.cfg = _state.pi.GetPluginConfig() as Config ?? new Config();
            DeserializeProperties(_state.cfg.PropertyBlob);
            _state.Log(State.LogLevelEnum.Info, null, "Configuration loaded");
        }

        public void SaveConfig()
        {
            _state.Log(State.LogLevelEnum.Info, null, "Saving configuration");
            _state.cfg.PropertyBlob = SerializeProperties();
            _state.pi.SavePluginConfig(_state.cfg);
            _state.Log(State.LogLevelEnum.Info, null, "Configuration saved");
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
        }

        private void InitializeLanguage()
        {
            foreach (Type type in Assembly.GetAssembly(typeof(Core.Language)).GetTypes().Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(Core.Language))))
            {
                //Log(LogLevelEnum.Debug, "Creating Language {0}", type.Name.ToString());
                Core.Language c = (Core.Language)Activator.CreateInstance(type);
                I18n.AddLanguage(c);
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

        private List<Tuple<PropertyInfo, int>> GetConfigurableProperties(object o, bool disregardDebug)
        {
            PropertyInfo[] props = o.GetType().GetProperties();
            List<Tuple<PropertyInfo, int>> result = new List<Tuple<PropertyInfo, int>>();
            foreach (PropertyInfo pi in props)
            {
                CustomAttributeData debugAttr = null, confAttr = null;
                foreach (CustomAttributeData cad in pi.CustomAttributes)
                {
                    if (cad.AttributeType == typeof(DebugOption))
                    {
                        debugAttr = cad;
                    }
                    if (cad.AttributeType == typeof(AttributeOrderNumber))
                    {
                        confAttr = cad;
                    }
                }
                if (confAttr != null)
                {
                    if (disregardDebug == true && debugAttr != null && _state.cfg.DebugOptions == false)
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

        private void RenderAutomarkerPrioTrinity(AutomarkerPrio amp)
        {
            Vector2 maxsize = ImGui.GetContentRegionAvail();
            float x = 0.0f, y = 0.0f;
            Vector2 curpos = ImGui.GetCursorPos();
            ImGuiStylePtr style = ImGui.GetStyle();
            int perRow = (int)Math.Floor((maxsize.X - 5.0f) / 160.0f);
            int itemx = 0, itemy = 0;
            bool moved = false;
            Vector2 screenpos = ImGui.GetCursorScreenPos();
            for (int i = 0; i < amp._prioByTrinity.Count; i++)
            {
                AutomarkerPrio.PrioTrinityEnum p = amp._prioByTrinity[i];
                string temp = I18n.Translate("Trinity/" + p.ToString());
                Vector2 curItem = new Vector2(curpos.X + x, curpos.Y + y);
                ImGui.SetCursorPos(curItem);
                ImGui.Selectable("##" + temp, true, ImGuiSelectableFlags.None, new Vector2(150, 40));
                x += 160.0f + style.ItemSpacing.X;
                if (ImGui.IsItemHovered() == true && ImGui.IsItemActive() == false)
                {
                    ImGui.BeginTooltip();
                    ImGui.Text(I18n.Translate("Misc/DragToReorderPrio"));
                    ImGui.EndTooltip();
                }
                if (ImGui.IsItemActive() == true && ImGui.IsItemHovered() == false && moved == false)
                {
                    Vector2 mpos = ImGui.GetMousePos();
                    int xpos = (int)Math.Floor((mpos.X - screenpos.X) / 160.0f);
                    int ypos = (int)Math.Floor((mpos.Y - screenpos.Y) / 50.0f);
                    int ipos = (ypos * perRow) + xpos;
                    if (i != ipos && ipos >= 0 && ipos < amp._prioByTrinity.Count)
                    {
                        amp._prioByTrinity[i] = amp._prioByTrinity[ipos];
                        amp._prioByTrinity[ipos] = p;
                        moved = true;
                    }
                }
                itemx++;
                if (x > maxsize.X - 160)
                {
                    x = 0.0f;
                    y += 50.0f;
                    itemy++;
                    itemx = 1;
                }
                else if (i < amp._prioByTrinity.Count - 1)
                {
                    ImGui.SameLine();
                }
                Vector2 pt = ImGui.GetCursorPos();
                ImGui.SetCursorPos(new Vector2(curItem.X + 120, curItem.Y + 5));
                ImGui.Image(_trinity[p].ImGuiHandle, new Vector2(30, 30));
                ImGui.SetCursorPos(new Vector2(curItem.X + 3, curItem.Y + 20.0f - ImGui.GetFontSize() / 2.0f));
                ImGui.Text((i + 1).ToString());
                ImGui.SetCursorPos(new Vector2(curItem.X + 115 - ImGui.CalcTextSize(temp).X, curItem.Y + 20.0f - ImGui.GetFontSize() / 2.0f));
                ImGui.Text(temp);
                ImGui.SetCursorPos(pt);
            }
        }

        private void RenderAutomarkerPrioRole(AutomarkerPrio amp)
        {
            Vector2 maxsize = ImGui.GetContentRegionAvail();
            float x = 0.0f, y = 0.0f;
            Vector2 curpos = ImGui.GetCursorPos();
            ImGuiStylePtr style = ImGui.GetStyle();
            int perRow = (int)Math.Floor((maxsize.X - 5.0f) / 160.0f);
            int itemx = 0, itemy = 0;
            bool moved = false;
            Vector2 screenpos = ImGui.GetCursorScreenPos();
            for (int i = 0; i < amp._prioByRole.Count; i++)
            {
                AutomarkerPrio.PrioRoleEnum p = amp._prioByRole[i];
                string temp = I18n.Translate("Role/" + p.ToString());
                Vector2 curItem = new Vector2(curpos.X + x, curpos.Y + y);
                ImGui.SetCursorPos(curItem);
                ImGui.Selectable("##" + temp, true, ImGuiSelectableFlags.None, new Vector2(150, 40));
                x += 160.0f + style.ItemSpacing.X;
                if (ImGui.IsItemHovered() == true && ImGui.IsItemActive() == false)
                {
                    ImGui.BeginTooltip();
                    ImGui.Text(I18n.Translate("Misc/DragToReorderPrio"));
                    ImGui.EndTooltip();
                }
                if (ImGui.IsItemActive() == true && ImGui.IsItemHovered() == false && moved == false)
                {
                    Vector2 mpos = ImGui.GetMousePos();
                    int xpos = (int)Math.Floor((mpos.X - screenpos.X) / 160.0f);
                    int ypos = (int)Math.Floor((mpos.Y - screenpos.Y) / 50.0f);
                    int ipos = (ypos * perRow) + xpos;
                    if (i != ipos && ipos >= 0 && ipos < amp._prioByRole.Count)
                    {
                        amp._prioByRole[i] = amp._prioByRole[ipos];
                        amp._prioByRole[ipos] = p;
                        moved = true;
                    }
                }
                itemx++;
                if (x > maxsize.X - 160)
                {
                    x = 0.0f;
                    y += 50.0f;
                    itemy++;
                    itemx = 1;
                }
                else if (i < amp._prioByRole.Count - 1)
                {
                    ImGui.SameLine();
                }
                Vector2 pt = ImGui.GetCursorPos();
                ImGui.SetCursorPos(new Vector2(curItem.X + 120, curItem.Y + 5));
                ImGui.Image(_roles[p].ImGuiHandle, new Vector2(30, 30));
                ImGui.SetCursorPos(new Vector2(curItem.X + 3, curItem.Y + 20.0f - ImGui.GetFontSize() / 2.0f));
                ImGui.Text((i + 1).ToString());
                ImGui.SetCursorPos(new Vector2(curItem.X + 115 - ImGui.CalcTextSize(temp).X, curItem.Y + 20.0f - ImGui.GetFontSize() / 2.0f));
                ImGui.Text(temp);
                ImGui.SetCursorPos(pt);
            }
        }

        private void RenderAutomarkerPrioJob(AutomarkerPrio amp)
        {
            Vector2 maxsize = ImGui.GetContentRegionAvail();
            float x = 0.0f, y = 0.0f;
            Vector2 curpos = ImGui.GetCursorPos();
            ImGuiStylePtr style = ImGui.GetStyle();
            int perRow = (int)Math.Floor((maxsize.X - 5.0f) / 160.0f);
            int itemx = 0, itemy = 0;
            bool moved = false;
            Vector2 screenpos = ImGui.GetCursorScreenPos();
            for (int i = 0; i < amp._prioByJob.Count; i++)
            {
                AutomarkerPrio.PrioJobEnum p = amp._prioByJob[i];
                string temp = I18n.Translate("Job/" + p.ToString());
                Vector2 curItem = new Vector2(curpos.X + x, curpos.Y + y);
                ImGui.SetCursorPos(curItem);
                ImGui.Selectable("##" + temp, true, ImGuiSelectableFlags.None, new Vector2(150, 40));
                x += 160.0f + style.ItemSpacing.X;
                if (ImGui.IsItemHovered() == true && ImGui.IsItemActive() == false)
                {
                    ImGui.BeginTooltip();
                    ImGui.Text(I18n.Translate("Misc/DragToReorderPrio"));
                    ImGui.EndTooltip();
                }
                if (ImGui.IsItemActive() == true && ImGui.IsItemHovered() == false && moved == false)
                {
                    Vector2 mpos = ImGui.GetMousePos();
                    int xpos = (int)Math.Floor((mpos.X - screenpos.X) / 160.0f);                    
                    int ypos = (int)Math.Floor((mpos.Y - screenpos.Y) / 50.0f);
                    int ipos = (ypos * perRow) + xpos;
                    if (i != ipos && ipos >= 0 && ipos < amp._prioByJob.Count)
                    {
                        amp._prioByJob[i] = amp._prioByJob[ipos];
                        amp._prioByJob[ipos] = p;
                        moved = true;
                    }
                }
                itemx++;
                if (x > maxsize.X -  160)
                {
                    x = 0.0f;
                    y += 50.0f;
                    itemy++;
                    itemx = 1;
                }
                else if (i < amp._prioByJob.Count - 1)
                {
                    ImGui.SameLine();
                }
                Vector2 pt = ImGui.GetCursorPos();
                ImGui.SetCursorPos(new Vector2(curItem.X + 120, curItem.Y + 5));
                ImGui.Image(_jobs[p].ImGuiHandle, new Vector2(30, 30));
                ImGui.SetCursorPos(new Vector2(curItem.X + 3, curItem.Y + 20.0f - ImGui.GetFontSize() / 2.0f));
                ImGui.Text((i + 1).ToString());
                ImGui.SetCursorPos(new Vector2(curItem.X + 115 - ImGui.CalcTextSize(temp).X, curItem.Y + 20.0f - ImGui.GetFontSize() / 2.0f));
                ImGui.Text(temp);
                ImGui.SetCursorPos(pt);
            }
        }

        private void RenderAutomarkerPrioPlayer(AutomarkerPrio amp)
        {
            if (ImGui.Button(I18n.Translate("Automarker/PrioType/Player/FillFromPartyList")) == true)
            {
                Party pty = _state.GetPartyMembers();
                amp._prioByPlayer.Clear();
                amp._prioByPlayer.AddRange(from ix in pty.Members orderby ix.Index ascending select ix.Name);
            }
            Vector2 maxsize = ImGui.GetContentRegionAvail();
            float x = 0.0f, y = 0.0f;
            Vector2 curpos = ImGui.GetCursorPos();
            ImGuiStylePtr style = ImGui.GetStyle();
            int perRow = (int)Math.Floor((maxsize.X - 5.0f) / 160.0f);
            int itemx = 0, itemy = 0;
            bool moved = false;
            Vector2 screenpos = ImGui.GetCursorScreenPos();
            for (int i = 0; i < amp._prioByPlayer.Count; i++)
            {
                string p = amp._prioByPlayer[i];
                Vector2 curItem = new Vector2(curpos.X + x, curpos.Y + y);
                ImGui.SetCursorPos(curItem);
                ImGui.Selectable("##" + p, true, ImGuiSelectableFlags.None, new Vector2(150, 40));
                x += 160.0f + style.ItemSpacing.X;
                if (ImGui.IsItemHovered() == true && ImGui.IsItemActive() == false)
                {
                    ImGui.BeginTooltip();
                    ImGui.Text(I18n.Translate("Misc/DragToReorderPrio"));
                    ImGui.EndTooltip();
                }
                if (ImGui.IsItemActive() == true && ImGui.IsItemHovered() == false && moved == false)
                {
                    Vector2 mpos = ImGui.GetMousePos();
                    int xpos = (int)Math.Floor((mpos.X - screenpos.X) / 160.0f);
                    int ypos = (int)Math.Floor((mpos.Y - screenpos.Y) / 50.0f);
                    int ipos = (ypos * perRow) + xpos;
                    if (i != ipos && ipos >= 0 && ipos < amp._prioByPlayer.Count)
                    {
                        amp._prioByPlayer[i] = amp._prioByPlayer[ipos];
                        amp._prioByPlayer[ipos] = p;
                        moved = true;
                    }
                }
                itemx++;
                if (x > maxsize.X - 160)
                {
                    x = 0.0f;
                    y += 50.0f;
                    itemy++;
                    itemx = 1;
                }
                else if (i < amp._prioByPlayer.Count - 1)
                {
                    ImGui.SameLine();
                }
                Vector2 pt = ImGui.GetCursorPos();
                ImGui.SetCursorPos(new Vector2(curItem.X + 3, curItem.Y + 20.0f - ImGui.GetFontSize() / 2.0f));
                ImGui.Text((i + 1).ToString());
                ImGui.SetCursorPos(new Vector2(curItem.X + 145 - ImGui.CalcTextSize(p).X, curItem.Y + 20.0f - ImGui.GetFontSize() / 2.0f));
                ImGui.Text(p);
                ImGui.SetCursorPos(pt);
            }
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

        private void RenderAutomarkerSigns(string path, PropertyInfo pi, object o)
        {
            AutomarkerSigns ams = (AutomarkerSigns)pi.GetValue(o);
            string proptr = I18n.Translate(path + "/" + pi.Name);
            ImGui.Text(proptr + Environment.NewLine + Environment.NewLine);
            bool manualSetting = true;
            if (ams.Presets.Count > 0)
            {
                string manualpr = I18n.Translate("Automarker/ManualPreset");
                string prval = ams.SelectedPreset;
                string selname = null;
                manualSetting = (ams.SelectedPreset == null);
                if (prval != null)
                {
                    selname = I18n.Translate(path + "/" + pi.Name + "/Presets/" + prval);
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
                    foreach(KeyValuePair<string, Dictionary<string, AutomarkerSigns.SignEnum>> kp in ams.Presets)
                    {
                        proptr = I18n.Translate(path + "/" + pi.Name + "/Presets/" + kp.Key);
                        if (ImGui.Selectable(proptr, String.Compare(proptr, selname) == 0) == true)
                        {
                            ams.ApplyPreset(kp.Key);
                        }
                    }
                    ImGui.EndCombo();
                }
                ImGui.PopItemWidth();
                ImGui.Text(Environment.NewLine);
            }
            if (manualSetting == false)
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
                proptr = I18n.Translate(path + "/" + pi.Name + "/" + kp.Key);
                string signtr = I18n.Translate("Signs/" + kp.Value);
                ImGui.SetCursorPos(new Vector2(curPos.X + (250 * x), curPos.Y + (80 * y)));
                ImGui.BeginGroup();
                x++;
                if (curPos.X + (250 * (x + 1)) >= wid)
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
                if (ImGui.BeginCombo("##" + proptr, signtr) == true)
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
            if (manualSetting == false)
            {
                ImGui.EndDisabled();
            }
        }

        private void RenderProperties(string path, object o)
        {
            List<Tuple<PropertyInfo, int>> props = GetConfigurableProperties(o, true);
            bool firstProp = true;
            int lastAon = 0;
            ImGuiStylePtr style = ImGui.GetStyle();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + style.ItemSpacing.Y);
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
                    bool temp = (bool)pi.GetValue(o);
                    if (ImGui.Checkbox(proptr, ref temp) == true)
                    {
                        pi.SetValue(o, temp);
                    }
                }
                if (pi.PropertyType == typeof(string))
                {
                    string temp = (string)pi.GetValue(o);
                    ImGui.Text(proptr);
                    if (ImGui.InputText("##" + proptr, ref temp, 2048) == true)
                    {
                        pi.SetValue(o, temp);
                    }
                }
                else if (pi.PropertyType == typeof(Vector3))
                {
                    Vector3 temp = (Vector3)pi.GetValue(o);
                    if (ImGui.ColorEdit3(proptr, ref temp, ImGuiColorEditFlags.NoInputs) == true)
                    {
                        pi.SetValue(o, temp);
                    }
                }
                else if (pi.PropertyType == typeof(int))
                {
                    ImGui.PushItemWidth(250);
                    int temp = (int)pi.GetValue(o);
                    if (ImGui.DragInt(proptr, ref temp, 1) == true)
                    {
                        pi.SetValue(o, temp);
                    }
                    ImGui.PopItemWidth();
                }
                else if (pi.PropertyType == typeof(float))
                {
                    ImGui.PushItemWidth(250);
                    float temp = (float)pi.GetValue(o);
                    if (ImGui.DragFloat(proptr, ref temp, 0.01f, 0.1f) == true)
                    {
                        pi.SetValue(o, temp);
                    }
                    ImGui.PopItemWidth();
                }
                else if (pi.PropertyType == typeof(Vector4))
                {
                    Vector4 temp = (Vector4)pi.GetValue(o);
                    if (ImGui.ColorEdit4(proptr, ref temp, ImGuiColorEditFlags.NoInputs) == true)
                    {
                        pi.SetValue(o, temp);
                    }
                }
                else if (pi.PropertyType == typeof(AutomarkerSigns))
                {
                    RenderAutomarkerSigns(path, pi, o);
                }
                else if (pi.PropertyType == typeof(AutomarkerPrio))
                {
                    RenderAutomarkerPrio(path, pi, o);
                }
                else if (pi.PropertyType == typeof(Action))
                {
                    Action act = (Action)pi.GetValue(o);
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
                    CustomPropertyInterface cpi = (CustomPropertyInterface)pi.GetValue(o);
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
                                    if ((contentItem.Value.Features & ContentModule.FeaturesEnum.Hack) != 0)
                                    {
                                        float time = (float)((DateTime.Now - _loaded).TotalMilliseconds / 600.0);
                                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.5f + 0.5f * (float)Math.Abs(Math.Cos(time)), 0.0f, 1.0f));
                                        ImGui.TextWrapped(I18n.Translate("Misc/RiskyFeature"));
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
            _state.NumFeaturesHack = 0;
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

        private void DrawSoftmarkers()
        {
            ImDrawListPtr draw;
            foreach (KeyValuePair<AutomarkerSigns.SignEnum, uint> kp in _state.SoftMarkers)
            {
                if (kp.Value == 0)
                {
                    continue;
                }
                GameObject go = _state.GetActorById(kp.Value);
                if (go == null)
                {
                    continue;
                }
                if (_state.StartDrawing(out draw) == false)
                {
                    return;
                }
                Vector3 temp = TranslateToScreen(go.Position.X, go.Position.Y + 2.0f, go.Position.Z);
                Vector2 pt = new Vector2(temp.X, temp.Y);
                TextureWrap tw = _signs[kp.Key];
                float mul = (float)Math.Abs(Math.Cos(DateTime.Now.Millisecond / 1000.0f * Math.PI));
                float calcWidth = tw.Width * 2.0f;
                float calcHeight = tw.Height * 2.0f;
                pt.X -= calcWidth / 2.0f;
                pt.Y -= (calcHeight + (calcHeight * mul)) / 2.0f;
                ImGui.SetCursorPos(pt);
                draw.AddImage(tw.ImGuiHandle, new Vector2(pt.X, pt.Y), new Vector2(pt.X + calcWidth, pt.Y + calcHeight));
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
            if (ImGui.Begin("Lemegeton (Beta)", ref open, ImGuiWindowFlags.NoCollapse) == false)
            {
                ImGui.End();
                ImGui.PopStyleColor(3);
                return;
            }
            if (open == false)
            {
                _state.cfg.Opened = false;
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
                    bool qtHacks = _state.cfg.QuickToggleHacks;
                    if (ImGui.Checkbox(I18n.Translate("MainMenu/Settings/QuickToggles/Hacks"), ref qtHacks) == true)
                    {
                        _state.cfg.QuickToggleHacks = qtHacks;
                    }
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
                            if (ImGui.Selectable(kp.Key, kp.Key == _state.cfg.Language) == true)
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
                    ImGui.Unindent(30.0f);
                    ImGui.PopID();
                }
                if (ImGui.CollapsingHeader(I18n.Translate("MainMenu/Settings/AutomarkerSettings")) == true)
                {
                    ImGui.PushID("AutomarkerSettings");
                    ImGui.Indent(30.0f);
                    ImGui.TextWrapped(I18n.Translate("MainMenu/Settings/AutomarkersInitialApplicationDelay"));
                    float autoIniMin = _state.cfg.AutomarkerIniDelayMin;
                    float autoIniMax = _state.cfg.AutomarkerIniDelayMax;
                    if (ImGui.DragFloatRange2(I18n.Translate("MainMenu/Settings/AutomarkerSeconds") + "##MainMenu/Settings/AutomarkersInitialApplicationDelay", ref autoIniMin, ref autoIniMax, 0.01f, 0.0f, 2.0f) == true)
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
                    ImGui.TextWrapped(Environment.NewLine + I18n.Translate("MainMenu/Settings/AutomarkersSoftDesc") + Environment.NewLine + Environment.NewLine);
                    bool autoSoft = _state.cfg.AutomarkerSoft;
                    if (ImGui.Checkbox(I18n.Translate("MainMenu/Settings/AutomarkersSoft"), ref autoSoft) == true)
                    {
                        _state.cfg.AutomarkerSoft = autoSoft;
                    }
                    ImGui.Unindent(30.0f);
                    ImGui.PopID();
                }
                if (ImGui.CollapsingHeader(I18n.Translate("MainMenu/Settings/OpcodeSettings")) == true)
                {
                    ImGui.PushID("OpcodeSettings");
                    ImGui.Indent(30.0f);
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
                    ImGui.Unindent(30.0f);
                    ImGui.PopID();
                }
                if (ImGui.CollapsingHeader(I18n.Translate("MainMenu/Settings/DebugSettings")) == true)
                {
                    ImGui.PushID("DebugSettings");
                    ImGui.Indent(30.0f);
                    bool debugOpts = _state.cfg.DebugOptions;
                    if (ImGui.Checkbox(I18n.Translate("MainMenu/Settings/DebugOptions"), ref debugOpts) == true)
                    {
                        _state.cfg.DebugOptions = debugOpts;
                    }
                    bool debugLogMarkers = _state.cfg.DebugOnlyLogAutomarkers;
                    if (ImGui.Checkbox(I18n.Translate("MainMenu/Settings/DebugOnlyLogAutomarkers"), ref debugLogMarkers) == true)
                    {
                        _state.cfg.DebugOnlyLogAutomarkers = debugLogMarkers;
                    }
                    if (ImGui.CollapsingHeader(I18n.Translate("MainMenu/Settings/DebugSettings/DelegateDebug")) == true)
                    {
                        ImGui.PushID("DelegateDebug");
                        ImGui.Indent(30.0f);
                        ImGui.PushItemWidth(100.0f);
                        RenderMethodCall(_state.InvokeZoneChange);
                        RenderMethodCall(_state.InvokeCombatChange);
                        RenderMethodCall(_state.InvokeCastBegin);
                        RenderMethodCall(_state.InvokeAction);
                        RenderMethodCall(_state.InvokeHeadmarker);
                        RenderMethodCall(_state.InvokeDirectorUpdate);
                        RenderMethodCall(_state.InvokeMapEffect);
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
                maxp = (now.Month == 4 && now.Day == 1) ? 5 : 10;
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
                int curtext = (int)Math.Floor(secs / delay) % _aboutScroller.Count();
                string curstr = _aboutScroller[curtext].Replace("$auto", _state.cfg.AutomarkersServed.ToString());
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
            ImGui.Text(VersionInfo + " - " + _state.GameVersion);
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
                    p.StartInfo.FileName = @"https://github.com/paissaheavyindustries";
                    p.Start();
                });
                tx.Start();
            }
            ImGui.SameLine();
            _adjusterX += ImGui.GetContentRegionAvail().X;
            ImGui.PopStyleColor(3);            
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
                ImGui.SameLine();
                i++;
            }
            if (ImGui.Button(">>##" + del.Method.Name) == true)
            {
                List<object> conversions = new List<object>();
                int k = 0;
                foreach (ParameterInfo p in pis)
                {
                    if (p.ParameterType == typeof(byte[]))
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
                temp = _state.NumFeaturesHack.ToString();
                ImGui.InputText(I18n.Translate("Status/NumFeaturesHack"), ref temp, 64, ImGuiInputTextFlags.ReadOnly);
                ImGui.PopItemWidth();
                ImGui.EndTable();
            }
            ImGui.BeginDisabled();
            ImGui.CollapsingHeader("  " + I18n.Translate("Status/ImpactToFunctionality"), ImGuiTreeNodeFlags.Leaf);
            ImGui.EndDisabled();
            ImGui.BeginChildFrame(1, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y));
            if (_state.NumFeaturesHack > 0 && _state.cfg.QuickToggleHacks == true && _state.cfg.NagAboutStreaming == true)
            {
                complaints.Add(I18n.Translate("Status/WarnHacksActive"));
            }
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
                _state.Log(LogLevelEnum.Error, ex, "Couldn't deserialize {0} due to exception: {1}", typeof(T).Name, ex.Message);
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
            WaitHandle[] wh = new WaitHandle[1];
            wh[0] = p._stopEvent;
            int timeout = 0;
            int tries = 0;
            try
            {
                while (true)
                {
                    switch (WaitHandle.WaitAny(wh, timeout))
                    {
                        case 0:
                            return;
                        case WaitHandle.WaitTimeout:
                            if (_state.PrepareInternals(tries >= 5) == true)
                            {
                                timeout = Timeout.Infinite;
                            }
                            else
                            {
                                timeout = 10000;
                                tries++;
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