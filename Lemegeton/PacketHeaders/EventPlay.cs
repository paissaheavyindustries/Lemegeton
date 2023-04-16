using System.Runtime.InteropServices;

namespace Lemegeton.PacketHeaders
{

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    internal struct EventPlay
    {

        [FieldOffset(0)] public ulong actorId;
        [FieldOffset(8)] public uint eventId;
        [FieldOffset(12)] public ushort scene;
        [FieldOffset(16)] public uint flags;
        [FieldOffset(20)] public uint param1;
        [FieldOffset(24)] public ushort param2;
        [FieldOffset(28)] public byte param3;
        [FieldOffset(29)] public uint param4;

    }

}
