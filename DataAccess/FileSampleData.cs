using System;
using System.IO;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using BrainCommon;
using BrainNetwork.BrainDeviceProtocol;

namespace DataAccess
{
    public class FileSampleData : IDisposable
    {
        private string _filename;

        private SyncBufManager _bufMgr
            ;

        private Subject<(byte, ArraySegment<int>, ArraySegment<byte>)> _dataStream;
        private FileStream _fs;
        private int _version;
        private uint _devId;
        private long _startTick;
        private BrainDevState _devParam;
        private byte[] _hash;
        

        private byte _order;
        private uint _currentlCount;
        private CancellationTokenSource _cts;

        public IObservable<(byte, ArraySegment<int>, ArraySegment<byte>)> DataStream => _dataStream;

        public FileSampleData(string filename, SyncBufManager bufMgr)
        {
            _filename = filename;
            _bufMgr = bufMgr;
            _dataStream = new Subject<(byte, ArraySegment<int>, ArraySegment<byte>)>();
        }

        public void Start()
        {
            try
            {
                _fs = File.OpenRead(_filename);
                Load();
                _cts = new CancellationTokenSource();
                StartSampling();
            }
            catch (Exception e)
            {
                _dataStream.OnError(e);
                _fs?.Close();
                throw;
            }
        }

        private async Task StartSampling()
        {
            var isComplete = await Read1Block();
            if (isComplete) return;
            NextRound(_cts.Token);
        }
        
        private async Task<bool> Read1Block()
        {
            var bufferSize = _devParam.ChannelCount * 3;
            var buf = _bufMgr.TakeBuffer(bufferSize);
            try
            {
                var readCount = await _fs.ReadAsync(buf, 0, bufferSize);
                if (readCount < bufferSize)
                {
                    if (readCount != 0)
                    {
                        AppLogger.Warning("truncated data");
                    }
                    _dataStream.OnCompleted();
                    return true;
                }
                var arraySegment = new ArraySegment<byte>(buf,0,bufferSize);
                var converted = BitDataConverter.FastConvertFrom(arraySegment, _bufMgr);
                _dataStream.OnNext((_order++, converted, arraySegment));
                _currentlCount++;
                _bufMgr.ReturnBuffer(converted.Array);
            }
            finally
            {
                _bufMgr.ReturnBuffer(buf);
            }
            return false;
        }
        
        private async Task NextRound(CancellationToken ctsToken)
        {
            try
            {
                await Task.Delay(20,ctsToken);
            }
            catch (Exception)
            {
                // ignored
            }
            if (ctsToken.IsCancellationRequested) return;
            var count = BrainDevState.SampleCountPer20ms(_devParam.SampleRate);
            for (var i = 0; i < count; i++)
            {
                var isComplete = await Read1Block();
                if (isComplete) return;
                if (ctsToken.IsCancellationRequested) return;
            }
            NextRound(ctsToken);
        }

        private void Load()
        {
            (_version, _devId, _startTick, _devParam, _hash) = _fs.ReadHeader(_bufMgr);
        }

        public void Pause()
        {
        }

        public void Resume()
        {
        }

        public void JumpTo()
        {
        }

        public void Reset()
        {
        }

        public void Stop()
        {
            _cts?.Cancel();
            _dataStream.OnCompleted();
        }

        public void Dispose()
        {
            _dataStream.Dispose();
            _fs?.Close();
        }
    }
}