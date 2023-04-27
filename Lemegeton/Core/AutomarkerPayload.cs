using Dalamud.Game.ClientState.Objects.Types;
using System.Collections.Generic;

namespace Lemegeton.Core
{

    internal class AutomarkerPayload
    {

        private State _st;
        public bool markSelfOnly { get; set; } = false;
        public bool softMarker { get; set; } = false;

        public Dictionary<AutomarkerSigns.SignEnum, List<GameObject>> assignments = new Dictionary<AutomarkerSigns.SignEnum, List<GameObject>>();

        public AutomarkerPayload(State st, bool selfOnly, bool soft)
        {
            _st = st;
            markSelfOnly = selfOnly;
            softMarker = (_st.cfg.AutomarkerSoft == true || soft == true);
        }

        public void Assign(AutomarkerSigns.SignEnum sign, GameObject go)
        {
            if (markSelfOnly == true)
            {
                if (go.ObjectId != _st.cs.LocalPlayer.ObjectId)
                {
                    return;
                }
            }
            if (sign != AutomarkerSigns.SignEnum.AttackNext && sign != AutomarkerSigns.SignEnum.BindNext && sign != AutomarkerSigns.SignEnum.IgnoreNext)
            {
                assignments[sign] = new List<GameObject>(new GameObject[] { go });
            }
            else
            {
                if (assignments.ContainsKey(sign) == false)
                {
                    assignments[sign] = new List<GameObject>();
                }
                assignments[sign].Add(go);
            }
        }

    }

}
