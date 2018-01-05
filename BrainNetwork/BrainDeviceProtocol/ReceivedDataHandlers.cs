using System;
using System.Reactive.Subjects;
using System.Threading;
using BrainCommon;

namespace BrainNetwork.BrainDeviceProtocol
{
    public static partial class BrainDeviceManager
    {
        //sample data stream
        private static Subject<(byte,ArraySegment<int>,ArraySegment<byte>)> _dataStream;

        public static IObservable<(byte, ArraySegment<int>, ArraySegment<byte>)> SampleDataStream => _dataStream;
        
        //device state stream
        private static Subject<BrainDevState> _stateStream;

        public static IObservable<BrainDevState> BrainDeviceState => _stateStream;

        private static BrainDevState _devState;

        #region Commit State Operations
        private static int _stateLock=CASHelper.LockFree;

        private static void CommitSampleRate(SampleRateEnum sampleRate)
        {
            var changed = false;
            //spin lock
            while (Interlocked.CompareExchange(ref _stateLock, CASHelper.LockUsed, CASHelper.LockFree) != CASHelper.LockFree){}
            if (_devState.SampleRate != sampleRate)
            {
                changed = true;
                _devState.SampleRate = sampleRate;
            }
            //free lock
            Interlocked.CompareExchange(ref _stateLock, CASHelper.LockFree, CASHelper.LockUsed);
            if (changed)
                CommiteState();
        }

        private static void CommitTrapOpt(TrapSettingEnum trapOpt)
        {
            var changed = false;
            //spin lock
            while (Interlocked.CompareExchange(ref _stateLock, CASHelper.LockUsed, CASHelper.LockFree) != CASHelper.LockFree){}
            if (_devState.TrapOption != trapOpt)
            {
                changed = true;
                _devState.TrapOption = trapOpt;
            }
            //free lock
            Interlocked.CompareExchange(ref _stateLock, CASHelper.LockFree, CASHelper.LockUsed);
            if (changed)
                CommiteState();
        }

        private static void CommitEnableFiler(bool enalbeFilter)
        {
            var changed = false;
            //spin lock
            while (Interlocked.CompareExchange(ref _stateLock, CASHelper.LockUsed, CASHelper.LockFree) != CASHelper.LockFree){}
            if (_devState.EnalbeFilter != enalbeFilter)
            {
                changed = true;
                _devState.EnalbeFilter = enalbeFilter;
            }
            //free lock
            Interlocked.CompareExchange(ref _stateLock, CASHelper.LockFree, CASHelper.LockUsed);
            if (changed)
                CommiteState();
        }
        
        private static void CommitParam(byte devCode,byte chanCount,byte gain,SampleRateEnum sampleRate,TrapSettingEnum trapOpt,bool enalbeFilter)
        {
            var changed = false;
            //spin lock
            while (Interlocked.CompareExchange(ref _stateLock, CASHelper.LockUsed, CASHelper.LockFree) != CASHelper.LockFree){}
            if (_devState.DevCode != devCode)
            {
                changed = true;
                _devState.DevCode = devCode;
            }
            if (_devState.ChannelCount != chanCount)
            {
                changed = true;
                _devState.ChannelCount = chanCount;
            }
            if (_devState.Gain != gain)
            {
                changed = true;
                _devState.Gain = gain;
            }
            
            if (_devState.SampleRate != sampleRate)
            {
                changed = true;
                _devState.SampleRate = sampleRate;
            }
            if (_devState.TrapOption != trapOpt)
            {
                changed = true;
                _devState.TrapOption = trapOpt;
            }
            if (_devState.EnalbeFilter != enalbeFilter)
            {
                changed = true;
                _devState.EnalbeFilter = enalbeFilter;
            }
            //free lock
            Interlocked.CompareExchange(ref _stateLock, CASHelper.LockFree, CASHelper.LockUsed);
            if (changed)
                CommiteState();
        }
        
        private static void CommitStartStop(bool isStart)
        {
            var changed = false;
            //spin lock
            while (Interlocked.CompareExchange(ref _stateLock, CASHelper.LockUsed, CASHelper.LockFree) != CASHelper.LockFree){}
            if (_devState.IsStart != isStart)
            {
                changed = true;
                _devState.IsStart = isStart;
            }
            //free lock
            Interlocked.CompareExchange(ref _stateLock, CASHelper.LockFree, CASHelper.LockUsed);
            if (changed)
                CommiteState();
        }

        private static void CommiteState()
        {
#if DEBUG
            AppLogger.Debug($"CommiteState: {_devState}");
#endif
            _stateStream?.OnNext(_devState);
        }
        
        #endregion

        public class SampleDataHandler : IReceivedDataProcessor
        {
            public byte FuncId => (byte)DevCommandFuncId.StartStop;

            public void Process(ArraySegment<byte> data)
            {
                var count = data.Count;
                var buf = data.Array;
                const int leastLen = 1 + 1 + 3 + 3 + 3;
                if (buf == null || count < leastLen)
                {
                    AppLogger.Error($"corruted sample data,received len: {count}");
                    return;
                }

                var extraBlockCount = (count - leastLen + 2) / 3;
                var startIdx = data.Offset;
                startIdx++;
                var order = buf[startIdx];
                startIdx++;
                /*
                var chan1 = new ArraySegment<byte>(buf, startIdx, 3);
                startIdx += 3;
                var chan2 = new ArraySegment<byte>(buf, startIdx, 3);
                startIdx += 3;
                var chan3 = new ArraySegment<byte>(buf, startIdx, 3);
                startIdx += 3;

                AppLogger.Debug($"sample data received,order:{order},ch1:{chan1.Show()},ch2:{chan2.Show()},ch3:{chan3.Show()}");

                var endInd = data.Offset + count;
                var blocks = new List<ArraySegment<byte>>(extraBlockCount + 3);
                blocks.Add(chan1);
                blocks.Add(chan2);
                blocks.Add(chan3);

                if (extraBlockCount > 0)
                {
                    for (var i = 0; i < extraBlockCount; i++)
                    {
                        if (startIdx + 3 <= endInd)
                            blocks.Add(new ArraySegment<byte>(buf, startIdx, 3));
                        else
                            blocks.Add(new ArraySegment<byte>(buf, startIdx, data.Offset + count - startIdx));

                        startIdx += 3;
                    }
                    AppLogger.Debug($"extra {extraBlockCount} channel data:{blocks.Show()}");
                }
                */
                CommitStartStop(true);
                var dataSeg = new ArraySegment<byte>(buf, startIdx, (extraBlockCount + 3) * 3);
                var disIntBuf = BitDataConverter.ConvertFromPlatform(dataSeg,bufferManager);
                _dataStream?.OnNext((order,disIntBuf,dataSeg));
                bufferManager.ReturnBuffer(disIntBuf.Array);
            }
        }

        public class SetSampleRateHandler : IReceivedDataProcessor
        {
            public byte FuncId => (byte)DevCommandFuncId.SetSampleRate;

            public void Process(ArraySegment<byte> data)
            {
                var count = data.Count;
                var buf = data.Array;
                const int leastLen = 1 + 1;
                if (buf == null || count < leastLen)
                {
                    AppLogger.Error($"corruted SetSampleRate result,received len: {count}");
                    return;
                }

                var startIdx = data.Offset;
                var flag = buf[startIdx + 1];
                var success = flag == 0;
                AppLogger.Debug($"SetSampleRate success? {success}");
            }
        }

        public class SetTrapHandler : IReceivedDataProcessor
        {
            public byte FuncId => (byte)DevCommandFuncId.SetTrap;

            public void Process(ArraySegment<byte> data)
            {
                var count = data.Count;
                var buf = data.Array;
                const int leastLen = 1 + 1;
                if (buf == null || count < leastLen)
                {
                    AppLogger.Error($"corruted SetTrap result,received len: {count}");
                    return;
                }

                var startIdx = data.Offset;
                var flag = buf[startIdx + 1];
                var success = flag == 0;
                AppLogger.Debug($"SetTrap success? {success}");
            }
        }

        public class SetFilterHandler : IReceivedDataProcessor
        {
            public byte FuncId => (byte)DevCommandFuncId.SetFilter;

            public void Process(ArraySegment<byte> data)
            {
                var count = data.Count;
                var buf = data.Array;
                const int leastLen = 1 + 1;
                if (buf == null || count < leastLen)
                {
                    AppLogger.Error($"corruted SetFilter result,received len: {count}");
                    return;
                }

                var startIdx = data.Offset;
                var flag = buf[startIdx + 1];
                var success = flag == 0;
                AppLogger.Debug($"SetFilter success? {success}");
            }
        }

        public class QueryParamHandler : IReceivedDataProcessor
        {
            public byte FuncId => (byte)DevCommandFuncId.QueryParam;

            public void Process(ArraySegment<byte> data)
            {
                var count = data.Count;
                var buf = data.Array;
                const int leastLen = 1 + 6;
                if (buf == null || count < leastLen)
                {
                    AppLogger.Error($"corruted Query result,received len: {count}");
                    return;
                }

                var startIdx = data.Offset;
                //TODO range check
                CommitParam(buf[startIdx + 1],buf[startIdx + 2],buf[startIdx + 3],(SampleRateEnum) buf[startIdx + 4],(TrapSettingEnum) buf[startIdx + 5],buf[startIdx + 6] == 1);
            }
        }
    }
}