using Dalamud.Game.ClientState.Objects.Types;
using System.Collections.Generic;

namespace Lemegeton.Core
{

    internal class Party
    {

        public class PartyMember
        {

            public int Index { get; set; }
            public string Name { get; set; }
            public GameObject GameObject { get; set; } = null;
            public uint ObjectId { get; set; } = 0;
            public int Selection { get; set; } = 0;

            public uint Job
            {
                get
                {
                    return GameObject != null ? ((BattleChara)GameObject).ClassJob.Id : 0;
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

    }

}
