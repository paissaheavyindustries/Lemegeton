using Lemegeton.Action;
using System;
using System.Text.RegularExpressions;
using static Lemegeton.Core.Timeline.Entry;

namespace Lemegeton.Core
{

    public class Context
    {

        // ${...}
        internal static Regex rex = new Regex(@"\$\{(?<id>[^${}]*)\}");

        public State State { get; set; } = null;

        public DateTime Created { get; set; } = DateTime.Now;
        public string SourceName { get; set; } = "(source name)";
        public string DestName { get; set; } = "(destination name)";
        public string EffectName { get; set; } = "(effect name)";

        internal Context Duplicate()
        {
            return (Context)MemberwiseClone();
        }

        internal string ParseExpressions(Action a, string text)
        {
            int i = 0;
            while (true)
            {
                Match m = rex.Match(text);
                if (m.Success == false)
                {
                    break;
                }
                string val = "";
                switch (m.Groups["id"].Value.ToLower())
                {
                    case "_triggeredtime":
                        val = Created.ToString();
                        break;
                    case "_currenttime":
                        val = DateTime.Now.ToString();
                        break;
                    case "_since":
                        val = Math.Floor((DateTime.Now - Created).TotalSeconds).ToString();
                        break;
                    case "_sincems":
                        val = Math.Floor((DateTime.Now - Created).TotalMilliseconds).ToString();
                        break;
                    case "_ttl":
                        if (a is Lemegeton.Action.Notification)
                        {
                            Lemegeton.Action.Notification n = (Lemegeton.Action.Notification)a;
                            val = (Math.Ceiling(n.TTL) - Math.Floor((DateTime.Now - Created).TotalSeconds)).ToString();
                        }
                        else
                        {
                            val = "0";
                        }
                        break;
                    case "_ttlms":
                        if (a is Lemegeton.Action.Notification)
                        {
                            Lemegeton.Action.Notification n = (Lemegeton.Action.Notification)a;
                            val = (Math.Ceiling(n.TTL * 1000.0f) - Math.Floor((DateTime.Now - Created).TotalMilliseconds)).ToString();
                        }
                        else
                        {
                            val = "0";
                        }
                        break;
                    case "_effect":
                        val = EffectName;
                        break;
                    case "_src":
                        val = SourceName;
                        break;
                    case "_dest":
                        val = DestName;
                        break;
                }
                text = text.Substring(0, m.Index) + val + text.Substring(m.Index + m.Length);
                i++;
                if (i > 100)
                {
                    break;
                }
            }
            return text;
        }

        internal string ParseText(Action a, string text)
        {
            return ParseExpressions(a, text);
        }

    }

}
