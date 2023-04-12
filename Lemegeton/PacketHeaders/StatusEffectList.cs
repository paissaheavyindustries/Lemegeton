using System.Runtime.InteropServices;

namespace Lemegeton.PacketHeaders
{

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct StatusEffectListEntry
    {
        [FieldOffset(0)] public ushort statusId;
        [FieldOffset(2)] public ushort stacks;
        [FieldOffset(4)] public float duration;
        [FieldOffset(8)] public uint srcActorId;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public unsafe struct StatusEffectList
    {

        //[FieldOffset(20)] public fixed byte entries[30 * 3 * 4];

    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public unsafe struct StatusEffectList2
    {

        //[FieldOffset(24)] public fixed byte entries[30 * 3 * 4];

    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public unsafe struct StatusEffectList3
    {

        //[FieldOffset(0)] public fixed byte entries[30 * 3 * 4];

    }

}
