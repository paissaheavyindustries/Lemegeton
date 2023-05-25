using System;
using System.Collections.Generic;
using System.Linq;

namespace Lemegeton.Core
{

    public class AutomarkerSigns
    {

        public class Preset
        {

            public string Name { get; set; } = "New preset";
            public bool Builtin { get; set; } = true;
            public Dictionary<string, SignEnum> Roles { get; set; } = new Dictionary<string, SignEnum>();

            public string Serialize()
            {
                List<string> temp = new List<string>();
                temp.Add(string.Format("Name={0}", Plugin.Base64Encode(Name)));
                temp.Add(string.Format("Builtin={0}", Builtin));
                foreach (KeyValuePair<string, AutomarkerSigns.SignEnum> kp in Roles)
                {
                    temp.Add(string.Format("{0}={1}", kp.Key, kp.Value.ToString()));
                }
                return string.Join(";", temp);
            }

            public void Deserialize(string data)
            {
                string[] temp = data.Split(";");
                foreach (string t in temp)
                {
                    string[] item = t.Split("=", 2);
                    switch (item[0])
                    {
                        case "Name":
                            Name = Plugin.Base64Decode(item[1]);
                            break;
                        case "Builtin":
                            Builtin = bool.Parse(item[1]);
                            break;
                        default:
                            if (Roles.ContainsKey(item[0]) == true)
                            {
                                AutomarkerSigns.SignEnum sign = (AutomarkerSigns.SignEnum)Enum.Parse(typeof(AutomarkerSigns.SignEnum), item[1]);
                                Roles[item[0]] = sign;
                            }
                            break;
                    }
                }
            }

        }

        public enum SignEnum
        {
            None,
            Attack1, Attack2, Attack3, Attack4, Attack5,
            Attack6, Attack7, Attack8, 
            Bind1, Bind2, Bind3,
            Ignore1, Ignore2,
            Square, Circle, Plus, Triangle,
            AttackNext, BindNext, IgnoreNext,
        }

        public Dictionary<string, Preset> Presets { get; set; }
        public Dictionary<string, SignEnum> Roles { get; set; }
        public string SelectedPreset { get; set; } = null;
        
        public AutomarkerSigns()
        {
            Roles = new Dictionary<string, SignEnum>();
            Presets = new Dictionary<string, Preset>();
        }

        public static int GetSignIndex(SignEnum sign)
        {
            switch (sign)
            {
                default:
                case SignEnum.None: return 0;
                case SignEnum.Attack1: return 1;
                case SignEnum.Attack2: return 2;
                case SignEnum.Attack3: return 3;
                case SignEnum.Attack4: return 4;
                case SignEnum.Attack5: return 5;
                case SignEnum.Bind1: return 6;
                case SignEnum.Bind2: return 7;
                case SignEnum.Bind3: return 8;
                case SignEnum.Ignore1: return 9;
                case SignEnum.Ignore2: return 10;
                case SignEnum.Square: return 11;
                case SignEnum.Circle: return 12;
                case SignEnum.Plus: return 13;
                case SignEnum.Triangle: return 14;
                case SignEnum.Attack6: return 15;
                case SignEnum.Attack7: return 16;
                case SignEnum.Attack8: return 17;
            }
        }

        public void SetRole(string id, SignEnum sign, bool autoswap = true)
        {
            if (autoswap == true && sign != SignEnum.None && sign != SignEnum.AttackNext && sign != SignEnum.BindNext && sign != SignEnum.IgnoreNext)
            {
                var existing = (from ix in Roles where ix.Value == sign select ix.Key).FirstOrDefault();
                if (existing != null)
                {
                    if (Roles.ContainsKey(id) == true)
                    {
                        Roles[existing] = Roles[id];
                    }
                    else
                    {
                        Roles[existing] = SignEnum.None;
                    }
                }
            }
            Roles[id] = sign;
        }

        public void AddPreset(Preset preset)
        {
            if (Presets.ContainsKey(preset.Name) == true)
            {
                if (Presets[preset.Name].Builtin == true && preset.Builtin == false)
                {
                    return;
                }
            }
            Presets[preset.Name] = preset;
        }

        public void ApplyPreset(string id)
        {
            if (id == null || Presets.ContainsKey(id) == false)
            {
                SelectedPreset = null;
                return;
            }
            SelectedPreset = id;
            Preset pr = Presets[id];
            foreach (KeyValuePair<string, AutomarkerSigns.SignEnum> kp in pr.Roles)
            {
                SetRole(kp.Key, kp.Value, false);
            }
        }

        public void SavePreset(string id)
        {
            Preset pr = new Preset() { Builtin = false, Name = id };
            foreach (KeyValuePair<string, SignEnum> kp in Roles)
            {
                pr.Roles[kp.Key] = kp.Value;
            }
            AddPreset(pr);
        }

        public void DeletePreset(string id)
        {
            if (Presets.ContainsKey(id) == false)
            {
                return;
            }
            Preset pr = Presets[id];
            if (pr.Builtin == true)
            {
                return;
            }
            if (SelectedPreset == id)
            {
                ApplyPreset(null);
            }
            Presets.Remove(id);
        }

        public string Serialize()
        {
            List<string> temp = new List<string>();
            temp.Add(string.Format("SelectedPreset={0}", SelectedPreset));
            foreach (KeyValuePair<string, AutomarkerSigns.SignEnum> kp in Roles)
            {
                temp.Add(string.Format("{0}={1}", kp.Key, kp.Value.ToString()));
            }
            List<Preset> userpresets = (from ix in Presets.Values where ix.Builtin == false select ix).ToList();
            List<string> prser = new List<string>();
            foreach (Preset pr in userpresets)
            {
                prser.Add(pr.Serialize());
            }
            if (prser.Count > 0)
            {
                temp.Add(string.Format("UserPresets={0}", Plugin.Base64Encode(string.Join("|", prser))));
            }
            return string.Join(";", temp);
        }

        public void Deserialize(string data)
        {
            string[] items = data.Split(";");
            ApplyPreset(null);
            string applyPreset = null;
            string userPresetBlob = null;
            foreach (string item in items)
            {
                string[] kp = item.Split("=", 2);
                if (kp[0] == "SelectedPreset" && kp[1] != "")
                {
                    applyPreset = kp[1];
                }
                else if (kp[0] == "UserPresets" && kp[1] != "")
                {
                    userPresetBlob = kp[1];
                }
                else
                {
                    if (Roles.ContainsKey(kp[0]) == true)
                    {
                        AutomarkerSigns.SignEnum sign = (AutomarkerSigns.SignEnum)Enum.Parse(typeof(AutomarkerSigns.SignEnum), kp[1]);
                        SetRole(kp[0], sign, false);                        
                    }
                }
            }
            if (userPresetBlob != null)
            {
                string prser = Plugin.Base64Decode(userPresetBlob);
                string[] prs = prser.Split("|");
                foreach (string pr in prs)
                {
                    Preset preset = new Preset();
                    foreach (KeyValuePair<string, AutomarkerSigns.SignEnum> kp in Roles)
                    {
                        preset.Roles[kp.Key] = SignEnum.None;
                    }
                    preset.Deserialize(pr);
                    AddPreset(preset);
                }
            }
            if (applyPreset != null)
            {
                ApplyPreset(applyPreset);
            }
        }

        internal AutomarkerPayload TestFunctionality(State st, AutomarkerPrio amp, AutomarkerTiming at, bool selfOnly, bool soft)
        {            
            Party pty = st.GetPartyMembers();
            if (amp != null)
            {
                amp.SortByPriority(pty.Members);
                int i = 1;
                foreach (Party.PartyMember pm in pty.Members)
                {
                    pm.Selection = i++;
                }
            }
            else
            {
                st.AssignRandomSelections(pty, Roles.Count);
            }
            int roleId = 1;
            AutomarkerPayload ap = new AutomarkerPayload(st, selfOnly, soft);
            foreach (KeyValuePair<string, AutomarkerSigns.SignEnum> kp in Roles)
            {
                var player = (from px in pty.Members where px.Selection == roleId select px).FirstOrDefault();
                if (player != null)
                {
                    st.Log(State.LogLevelEnum.Debug, null, "Assigning test role {0} with sign {1} to {2}", kp.Key, kp.Value, player.GameObject);
                    ap.Assign(kp.Value, player.GameObject);
                }
                roleId++;
            }            
            st.ExecuteAutomarkers(ap, at);
            return ap;
        }

    }

}
