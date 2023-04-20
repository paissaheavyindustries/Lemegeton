using Dalamud.Game.ClientState.Objects.Types;
using System.Collections.Generic;

namespace Lemegeton.Core
{

    internal class AutomarkerPayload
    {

        public Dictionary<AutomarkerSigns.SignEnum, GameObject> assignments = new Dictionary<AutomarkerSigns.SignEnum, GameObject>();

    }

}
