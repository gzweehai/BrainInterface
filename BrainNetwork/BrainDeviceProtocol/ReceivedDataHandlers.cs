using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using BrainCommon;

namespace BrainNetwork.BrainDeviceProtocol
{
    public static partial class BrainDeviceManager
    {
        //sample data stream
        private static Subject<List<ArraySegment<byte>>> _dataStream;

        //device state stream
        private static Subject<BrainDevState> _stateStream;

        private static BrainDevState _devState;

        public class SampleDataHandler : IReceivedDataProcessor
        {
            public byte FuncId => 1;

            public void Process(ArraySegment<byte> data)
            {
                var count = data.Count;
                var buf = data.Array;
                const int leastLen = 1 + 1 + 3 + 3 + 3;
                if (buf == null || count < leastLen)
                {
                    AppLogger.Error("corruted sample data");
                    return;
                }

                var startIdx = data.Offset;
                startIdx++;
                var order = buf[startIdx];
                startIdx++;
                var chan1 = new ArraySegment<byte>(buf, startIdx, 3);
                startIdx += 3;
                var chan2 = new ArraySegment<byte>(buf, startIdx, 3);
                startIdx += 3;
                var chan3 = new ArraySegment<byte>(buf, startIdx, 3);
                startIdx += 3;

                AppLogger.Debug(
                    $"sample data received,order:{order},ch1:{chan1.Show()},ch2:{chan2.Show()},ch3:{chan3.Show()}");

                var endInd = data.Offset + count;
                var extraBlockCount = (count - leastLen + 2) / 3;
                var blocks = new List<ArraySegment<byte>>(extraBlockCount + 3)
                {
                    [0] = chan1,
                    [1] = chan2,
                    [2] = chan3
                };
                if (extraBlockCount > 0)
                {
                    for (var i = 0; i < extraBlockCount; i++)
                    {
                        if (startIdx + 3 <= endInd)
                            blocks[i + 3] = new ArraySegment<byte>(buf, startIdx, 3);
                        else
                            blocks[i + 3] = new ArraySegment<byte>(buf, startIdx, data.Offset + count - startIdx);

                        startIdx += 3;
                    }
                    AppLogger.Debug($"extra {extraBlockCount} channel data:{blocks.Show()}");
                }
                _dataStream?.OnNext(blocks);
            }
        }

        public class SetSampleRateHandler : IReceivedDataProcessor
        {
            public byte FuncId => 11;

            public void Process(ArraySegment<byte> data)
            {
                var count = data.Count;
                var buf = data.Array;
                const int leastLen = 1 + 1;
                if (buf == null || count < leastLen)
                {
                    AppLogger.Error("corruted SetSampleRate result");
                    return;
                }

                var startIdx = data.Offset;
                var flag = buf[startIdx + 1];
                var success = flag == 0;
                AppLogger.Debug(success);
            }
        }

        public class SetTrapHandler : IReceivedDataProcessor
        {
            public byte FuncId => 12;

            public void Process(ArraySegment<byte> data)
            {
                var count = data.Count;
                var buf = data.Array;
                const int leastLen = 1 + 1;
                if (buf == null || count < leastLen)
                {
                    AppLogger.Error("corruted SetTrap result");
                    return;
                }

                var startIdx = data.Offset;
                var flag = buf[startIdx + 1];
                var success = flag == 0;
                AppLogger.Debug(success);
            }
        }

        public class SetFilterHandler : IReceivedDataProcessor
        {
            public byte FuncId => 13;

            public void Process(ArraySegment<byte> data)
            {
                var count = data.Count;
                var buf = data.Array;
                const int leastLen = 1 + 1;
                if (buf == null || count < leastLen)
                {
                    AppLogger.Error("corruted SetFilter result");
                    return;
                }

                var startIdx = data.Offset;
                var flag = buf[startIdx + 1];
                var success = flag == 0;
                AppLogger.Debug(success);
            }
        }

        public class QueryParamHandler : IReceivedDataProcessor
        {
            public byte FuncId => 21;

            public void Process(ArraySegment<byte> data)
            {
                var count = data.Count;
                var buf = data.Array;
                const int leastLen = 1 + 5;
                if (buf == null || count < leastLen)
                {
                    AppLogger.Error("corruted Query result");
                    return;
                }

                var startIdx = data.Offset;
                _devState.DevCode = buf[startIdx + 1];
                _devState.ChannelCount = buf[startIdx + 2];
                //TODO range check
                _devState.SampleRate = (SampleRateEnum) buf[startIdx + 3];
                _devState.TrapOption = (TrapSettingEnum) buf[startIdx + 4];
                _devState.EnalbeFilter = buf[startIdx + 5] == 1;
                AppLogger.Debug(data.Show());
                
                AppLogger.Debug(_devState.ToString());
                _stateStream.OnNext(_devState);
            }
        }
    }
}