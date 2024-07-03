using Dalamud.Game.ClientState.Objects.Types;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Lemegeton.Core
{

    internal class Party
    {

        public class PartyMember
        {

            public int Index { get; set; }
            public string Name { get; set; }
            public IGameObject GameObject { get; set; } = null;
            public ulong ObjectId { get; set; } = 0;
            public int Selection { get; set; } = 0;

            public uint Job
            {
                get
                {
                    return GameObject != null ? ((IBattleChara)GameObject).ClassJob.Id : 0;
                }
            }

            public float X
            {
                get
                {
                    return GameObject != null ? GameObject.Position.X : 0.0f;
                }
            }

            public float Y
            {
                get
                {
                    return GameObject != null ? GameObject.Position.Y : 0.0f;
                }
            }

            public float Z
            {
                get
                {
                    return GameObject != null ? GameObject.Position.Z : 0.0f;
                }
            }

        }

        public List<PartyMember> Members { get; set; } = new List<PartyMember>();

        public PartyMember GetByIndex(int index)
        {
            foreach (PartyMember pm in Members)
            {
                if (pm.Index == index)
                {
                    return pm;
                }
            }
            return null;
        }

        public PartyMember GetByActorId(uint actorId)
        {
            foreach (PartyMember pm in Members)
            {
                if (pm.ObjectId == actorId)
                {
                    return pm;
                }
            }
            return null;
        }

        public List<PartyMember> GetByActorIds(IEnumerable<uint> actorIds)
        {
            return (from ix in Members join jx in actorIds on ix.ObjectId equals jx select ix).ToList();
        }

    }

}
