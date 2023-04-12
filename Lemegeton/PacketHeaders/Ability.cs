using System.Runtime.InteropServices;

namespace Lemegeton.PacketHeaders
{

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    internal unsafe struct Ability1
    {

        [FieldOffset(28)] public ushort actionId;
        [FieldOffset(48 + (16 * 1 * 4))] public fixed ulong targetId[1];

    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    internal unsafe struct Ability8
    {

        [FieldOffset(28)] public ushort actionId;
        [FieldOffset(48 + (16 * 8 * 4))] public fixed ulong targetId[8];

    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    internal unsafe struct Ability16
    {

        [FieldOffset(28)] public ushort actionId;
        [FieldOffset(48 + (16 * 16 * 4))] public fixed ulong targetId[16];

    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    internal unsafe struct Ability24
    {

        [FieldOffset(28)] public ushort actionId;
        [FieldOffset(48 + (16 * 24 * 4))] public fixed ulong targetId[24];

    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    internal unsafe struct Ability32
    {

        [FieldOffset(28)] public ushort actionId;
        [FieldOffset(48 + (16 * 32 * 4))] public fixed ulong targetId[32];

    }

}
