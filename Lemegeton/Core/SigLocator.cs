using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Lemegeton.Core
{

    internal class SigLocator
    {

        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_DOS_HEADER
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public char[] e_magic;       // Magic number
            public UInt16 e_cblp;    // Bytes on last page of file
            public UInt16 e_cp;      // Pages in file
            public UInt16 e_crlc;    // Relocations
            public UInt16 e_cparhdr;     // Size of header in paragraphs
            public UInt16 e_minalloc;    // Minimum extra paragraphs needed
            public UInt16 e_maxalloc;    // Maximum extra paragraphs needed
            public UInt16 e_ss;      // Initial (relative) SS value
            public UInt16 e_sp;      // Initial SP value
            public UInt16 e_csum;    // Checksum
            public UInt16 e_ip;      // Initial IP value
            public UInt16 e_cs;      // Initial (relative) CS value
            public UInt16 e_lfarlc;      // File address of relocation table
            public UInt16 e_ovno;    // Overlay number
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public UInt16[] e_res1;    // Reserved words
            public UInt16 e_oemid;       // OEM identifier (for e_oeminfo)
            public UInt16 e_oeminfo;     // OEM information; e_oemid specific
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public UInt16[] e_res2;    // Reserved words
            public Int32 e_lfanew;      // File address of new exe header

            private string _e_magic
            {
                get { return new string(e_magic); }
            }

            public bool isValid
            {
                get { return _e_magic == "MZ"; }
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct IMAGE_NT_HEADERS64
        {
            [FieldOffset(0)]
            public uint Signature;

            [FieldOffset(24)]
            public IMAGE_OPTIONAL_HEADER64 OptionalHeader;

        }

        [StructLayout(LayoutKind.Explicit)]
        public struct IMAGE_OPTIONAL_HEADER64
        {

            [FieldOffset(4)]
            public uint SizeOfCode;

            [FieldOffset(20)]
            public uint BaseOfCode;

        }

        private State _state;
        private uint _codeSize;
        private uint _codeBase;
        private int _dataLength;
        private byte[] _data;

        public SigLocator(State state)
        {
            _state = state;
            nint baseaddr = _state.ss.Module.BaseAddress;
            IMAGE_DOS_HEADER dos = Marshal.PtrToStructure<IMAGE_DOS_HEADER>(baseaddr);
            if (dos.isValid == true)
            {
                IMAGE_NT_HEADERS64 nt = Marshal.PtrToStructure<IMAGE_NT_HEADERS64>(baseaddr + dos.e_lfanew);                
                _codeSize = nt.OptionalHeader.SizeOfCode;
                _codeBase = nt.OptionalHeader.BaseOfCode;
                _dataLength = (int)(_codeBase + _codeSize);
                _data = new byte[_dataLength];
                Marshal.Copy(baseaddr, _data, 0, _dataLength);
            }
        }

        public nint ScanText(string signature)
        {
            if (_state.ss.TryScanText(signature, out nint addr) == true)
            {
                return addr;
            }
            return IntPtr.Zero;
        }

        public nint ScanData(string signature)
        {
            if (_state.ss.TryScanData(signature, out nint addr) == true)
            {
                return addr;
            }
            return IntPtr.Zero;
        }

        public nint ScanModule(string signature)
        {
            if (_state.ss.TryScanModule(signature, out nint addr) == true)
            {
                return addr;
            }
            return IntPtr.Zero;
        }

        public nint GetStaticAddressFromSig(string signature, int offset = 0)
        {
            if (_state.ss.TryGetStaticAddressFromSig(signature, out nint addr, offset) == true)
            {
                return addr;
            }
            return IntPtr.Zero;
        }

        public nint OldGetStaticAddressFromSig(string signature, int offset = 0)
        {
            IntPtr instrAddr = OldScanText(signature);
            if (instrAddr == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }
            instrAddr = IntPtr.Add(instrAddr, offset);
            nint bAddr = _state.ss.Module.BaseAddress;
            long num;
            do
            {
                instrAddr = IntPtr.Add(instrAddr, 1);
                num = Marshal.ReadInt32(instrAddr) + (long)instrAddr + 4 - bAddr;
            }
            while (!(num >= _state.ss.DataSectionOffset && num <= _state.ss.DataSectionOffset + _state.ss.DataSectionSize));
            return IntPtr.Add(instrAddr, Marshal.ReadInt32(instrAddr) + 4);
        }

        private IntPtr OldScanText(string pattern)
        {
            var results = FindPattern(pattern);
            if (results.Count != 1)
            {
                _state.Log(State.LogLevelEnum.Error, null, "Signature pattern returned {0} hits", results.Count);
                return 0;
            }
            var scanRet = results[0];
            var insnByte = Marshal.ReadByte(scanRet);
            if (insnByte == 0xE8 || insnByte == 0xE9)
            {
                var jumpOffset = Marshal.ReadInt32(IntPtr.Add(scanRet, 1));
                return IntPtr.Add(scanRet, 5 + jumpOffset);
            }
            return scanRet;
        }

        private List<IntPtr> FindPattern(string pattern)
        {
            var results = FindSignature(HexToPattern(pattern));
            nint baseaddr = _state.ss.Module.BaseAddress;
            for (int i = 0; i < results.Count; i++)
            {
                results[i] = baseaddr + (int)results[i];
            }
            return results;
        }

        private List<int> HexToPattern(string data)
        {
            List<int> bytes = new List<int>();
            for (int i = 0; i < data.Length - 1;)
            {
                if (data[i] == '?')
                {
                    if (data[i + 1] == '?')
                    {
                        i++;
                    }
                    i++;
                    bytes.Add(-1);
                    continue;
                }
                if (data[i] == ' ')
                {
                    i++;
                    continue;
                }
                string bh = data.Substring(i, 2);
                var b = byte.Parse(bh, NumberStyles.AllowHexSpecifier);
                bytes.Add(b);
                i += 2;
            }
            return bytes;
        }

        private List<IntPtr> FindSignature(List<int> pattern)
        {
            List<IntPtr> ret = new List<IntPtr>();
            uint plen = (uint)pattern.Count;
            var dataLength = _dataLength - plen;
            for (var i = _codeBase; i < dataLength; i++)
            {
                if (ByteMatch(_data, (int)i, pattern) == true)
                {
                    ret.Add((IntPtr)i);
                }
            }
            return ret;
        }

        private bool ByteMatch(byte[] bytes, int start, List<int> pattern)
        {
            for (int i = start, j = 0; j < pattern.Count; i++, j++)
            {
                if (pattern[j] == -1)
                {
                    continue;
                }
                if (bytes[i] != pattern[j])
                {
                    return false;
                }
            }
            return true;
        }

    }

}
