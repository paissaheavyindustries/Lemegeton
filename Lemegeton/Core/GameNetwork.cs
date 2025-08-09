using Dalamud.Game.Network;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Network;
using Serilog;
using System;
using System.Runtime.InteropServices;

namespace Lemegeton.Core
{

    internal class GameNetwork : IDisposable
    {

        public delegate void OnNetworkMessageDelegate(nint dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction);

        public event OnNetworkMessageDelegate? NetworkMessage;

        private Hook<PacketDispatcher.Delegates.OnReceivePacket> processZonePacketDownHook;

        public GameNetwork(IGameInteropProvider io)
        {
            unsafe
            {
                var onReceivePacketAddress = (nint)PacketDispatcher.StaticVirtualTablePointer->OnReceivePacket;                
                processZonePacketDownHook = io.HookFromAddress<PacketDispatcher.Delegates.OnReceivePacket>(onReceivePacketAddress, this.ProcessZonePacketDownDetour);
                processZonePacketDownHook.Enable();
            }
        }

        public void Dispose()
        {
            if (processZonePacketDownHook != null)
            {
                processZonePacketDownHook.Dispose();
                processZonePacketDownHook = null;
            }
        }

        private unsafe void ProcessZonePacketDownDetour(PacketDispatcher* dispatcher, uint targetId, IntPtr dataPtr)
        {

            // Go back 0x10 to get back to the start of the packet header
            dataPtr -= 0x10;

            foreach (var d in Delegate.EnumerateInvocationList(this.NetworkMessage))
            {
                try
                {
                    d.Invoke(
                        dataPtr + 0x20,
                        (ushort)Marshal.ReadInt16(dataPtr, 0x12),
                        0,
                        targetId,
                        NetworkMessageDirection.ZoneDown);
                }
                catch (Exception ex)
                {
                    string header;
                    try
                    {
                        var data = new byte[32];
                        Marshal.Copy(dataPtr, data, 0, 32);
                        header = BitConverter.ToString(data);
                    }
                    catch (Exception)
                    {
                        header = "failed";
                    }

                    Log.Error(ex, "Exception on ProcessZonePacketDown hook. Header: " + header);
                }
            }

            this.processZonePacketDownHook.Original(dispatcher, targetId, dataPtr + 0x10);            
        }

    }

}
