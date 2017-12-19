using System;
using System.IO;
using BrainCommon;
using BrainNetwork.BrainDeviceProtocol;

namespace DataAccess
{
    /// <summary>
    /// format version 1
    /// </summary>
    public static class SampleDataFileFormat
    {
        public const int Md5Len = 16;
        public const int Md5StartIndex = 1 + sizeof(uint) + sizeof(long) + BrainDevState.StoreSize;
        public const int CntStartIndex = Md5StartIndex + Md5Len;

        public static (int, byte[]) Header(SyncBufManager bufferManager, byte version, uint devId, long startTick,
            BrainDevState state)
        {
            var buf = bufferManager.TakeBuffer(CntStartIndex);
            buf[0] = version;
            var ind = 1;
            unsafe
            {
                WritePrimitive((byte*) &devId, sizeof(uint), buf, ref ind);
                WritePrimitive((byte*) &startTick, sizeof(long), buf, ref ind);
                buf[ind++] = state.DevCode;
                buf[ind++] = state.ChannelCount;
                buf[ind++] = state.Gain;
                buf[ind++] = (byte) state.SampleRate;
                buf[ind++] = (byte) state.TrapOption;
                buf[ind++] = state.EnalbeFilter ? (byte) 1 : (byte) 0;
                for (var i = ind; i < CntStartIndex; i++)
                {
                    buf[i] = 0;
                }
            }
            return (CntStartIndex, buf);
        }

        //version 1 (byte version, uint devId, long startTick,BrainDevState state,byte[] MD5Hash)
        public static (byte, uint, long, BrainDevState, byte[]) ReadHeader(this FileStream fs,
            SyncBufManager bufferManager)
        {
            var buf = bufferManager.TakeBuffer(CntStartIndex);
            var count = fs.Read(buf, 0, CntStartIndex);
            if (count < CntStartIndex) throw new Exception("Invalid Sample Data File Format");
            var version = buf[0];
            if (version != 1) throw new Exception("not supported sample data format, only version 1 is supported");
            var ind = 1;

            uint devId;
            long startTick;
            var state = default(BrainDevState);
            unsafe
            {
                ReadPrimitive((byte*) &devId, sizeof(uint), buf, ref ind);
                ReadPrimitive((byte*) &startTick, sizeof(long), buf, ref ind);
                state.DevCode = buf[ind++];
                state.ChannelCount = buf[ind++];
                state.Gain = buf[ind++];
                state.SampleRate = (SampleRateEnum) buf[ind++];
                state.TrapOption = (TrapSettingEnum) buf[ind++];
                state.EnalbeFilter = buf[ind++] == 1;
            }
            var md5Buf = new byte[Md5Len];
            Buffer.BlockCopy(buf, ind, md5Buf, 0, Md5Len);
            bufferManager.ReturnBuffer(buf);
            return (version, devId, startTick, state, md5Buf);
        }

        private static unsafe void WritePrimitive(byte* p, int count, byte[] buf, ref int ind)
        {
            for (var i = 0; i < count; ++i)
            {
                buf[ind] = *p;
                p++;
                ind++;
            }
        }

        private static unsafe void ReadPrimitive(byte* p, int count, byte[] buf, ref int ind)
        {
            for (var i = 0; i < count; ++i)
            {
                *p = buf[ind];
                p++;
                ind++;
            }
        }
    }
}