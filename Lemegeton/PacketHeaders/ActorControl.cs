using System.Runtime.InteropServices;

namespace Lemegeton.PacketHeaders
{

    public enum ActorControlCategory : ushort
    {
        GainStatus = 20, // unreliable for anything serious
        LoseStatus = 21, // unreliable for anything serious
        Headmarker = 34,
        Tether = 35,
        Director = 109,
        Sign = 502,
    };

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct ActorControl
    {

        [FieldOffset(0)] public ActorControlCategory category;
        [FieldOffset(4)] public uint param1;
        [FieldOffset(8)] public uint param2;
        [FieldOffset(12)] public uint param3;
        [FieldOffset(16)] public uint param4;

    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    internal struct ActorControlSelf
    {

        [FieldOffset(0)] public ActorControlCategory category;
        [FieldOffset(4)] public uint param1;
        [FieldOffset(8)] public uint param2;
        [FieldOffset(12)] public uint param3;
        [FieldOffset(16)] public uint param4;
        [FieldOffset(20)] public uint param5;
        [FieldOffset(24)] public uint param6;

    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    internal struct ActorControlTarget
    {

        [FieldOffset(0)] public ActorControlCategory category;
        [FieldOffset(4)] public uint param1;
        [FieldOffset(8)] public uint param2;
        [FieldOffset(12)] public uint param3;
        [FieldOffset(16)] public uint param4;
        [FieldOffset(24)] public uint targetId;

    }

}
