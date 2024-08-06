using Dalamud.Game.ClientState.Objects.Types;
using System.Collections.Generic;

namespace Lemegeton.Core
{

    internal class AutomarkerPayload
    {

        private State _st;
        public bool markSelfOnly { get; set; } = false;
        public bool softMarker { get; set; } = false;

        public Dictionary<AutomarkerSigns.SignEnum, List<IGameObject>> assignments = new Dictionary<AutomarkerSigns.SignEnum, List<IGameObject>>();

        public AutomarkerPayload(State st, bool selfOnly, bool soft)
        {
            _st = st;
            markSelfOnly = selfOnly;
            softMarker = (_st.cfg.AutomarkerSoft == true || soft == true);
        }

        public void Assign(AutomarkerSigns.SignEnum sign, Party.PartyMember pm)
        {
            Assign(sign, pm.GameObject);
        }
        
        public void Assign(AutomarkerSigns.SignEnum sign, IGameObject go)
        {
            if (markSelfOnly == true)
            {
                if (go.GameObjectId != _st.cs.LocalPlayer.GameObjectId)
                {
                    return;
                }
            }
            if (sign != AutomarkerSigns.SignEnum.AttackNext && sign != AutomarkerSigns.SignEnum.BindNext && sign != AutomarkerSigns.SignEnum.IgnoreNext)
            {
                assignments[sign] = new List<IGameObject>(new IGameObject[] { go });
            }
            else
            {
                if (assignments.ContainsKey(sign) == false)
                {
                    assignments[sign] = new List<IGameObject>();
                }
                assignments[sign].Add(go);
            }
        }

    }

}
