using System.Runtime.InteropServices;

namespace Lemegeton.PacketHeaders
{

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct EffectResultEntry
    {

        [FieldOffset(2)] public ushort statusId;
        [FieldOffset(4)] public ushort stacks;
        [FieldOffset(8)] public float duration;
        [FieldOffset(12)] public uint srcActorId;

    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public unsafe struct EffectResult
    {

        [FieldOffset(25)] public byte entryCount;
        //[FieldOffset(28)] public fixed byte entries[4 * 4 * 4];

    }

}
