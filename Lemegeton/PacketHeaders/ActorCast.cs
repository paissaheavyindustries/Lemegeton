using System.Runtime.InteropServices;

namespace Lemegeton.PacketHeaders
{

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    internal struct ActorCast
    {

        [FieldOffset(0)] public ushort actionId;
        [FieldOffset(8)] public float castTime;
        [FieldOffset(12)] public uint targetId;
        [FieldOffset(16)] public float rotation;

    }

}
