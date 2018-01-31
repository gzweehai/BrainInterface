using System;
using System.IO;
using System.Reactive.Disposables;
using System.Security.Cryptography;
using System.Threading;
using BrainCommon;
using BrainNetwork.BrainDeviceProtocol;

namespace DataAccess
{
    public class FileResource : IDisposable
    {
        private BrainDevState _state;
        private uint _devId;
        private byte _version;
        private string _rid;
        private IDisposable _unsubscribe;
        private FileStream _fHandler;
        private SyncBufManager _bufferManager;
        private long _startTick;
        private MD5 _hasher;

        public string ResourceId => _rid;

        /// <summary>
        /// 按照时间、设备状态等数据创建放大器采样数据文件，
        /// 写入文件头（格式定义在SampleDataFileFormat），
        /// 关闭时写入MD5校验值
        /// </summary>
        /// <param name="state"></param>
        /// <param name="devId"></param>
        /// <param name="version"></param>
        /// <param name="bufferManager"></param>
        public FileResource(BrainDevState state, uint devId, byte version, SyncBufManager bufferManager)
        {
            _state = state;
            _devId = devId;
            _version = version;
            _bufferManager = bufferManager;
        }

        public void StartRecord(IObservable<(byte, ArraySegment<int>, ArraySegment<byte>)> dataStream)
        {
            //only the first one success
            var tmp = Interlocked.Exchange(ref _unsubscribe, Disposable.Empty);
            if (tmp != null) throw new Exception("This File Resource is used");
            _unsubscribe = dataStream.Subscribe(OnDataPush, OnErr, OnComplete);
            _hasher = MD5.Create();
        }

        private void OnDataPush((byte, ArraySegment<int>, ArraySegment<byte>) tuple)
        {
            var (_, _, raw) = tuple;
            if (raw.Array == null) return;
            var isFirst = false;
            if (_rid == null)
            {
                _startTick = DateTime.UtcNow.Ticks;
                _rid = _devId + "." + _startTick + ".bin";
                _fHandler = File.Create(_rid, 1024, FileOptions.Asynchronous|FileOptions.RandomAccess);
                isFirst = true;
                AppLogger.Debug($"FileResource Created:{_rid}");
            }

            //copy data is needed because I use async writing
            var count = raw.Count;
            var buf = _bufferManager.TakeBuffer(count);
            Buffer.BlockCopy(raw.Array, raw.Offset, buf, 0, count);
            if (isFirst)
            {
                var (hs, header) = SampleDataFileFormat.Header(_bufferManager, _version, _devId, _startTick, _state);
                _fHandler.WriteAsync(header, 0, hs).ContinueWith(state0 =>
                {
                    //Console.WriteLine(header.Show(hs));
                    _bufferManager.ReturnBuffer(header);
                    _fHandler.WriteAsync(buf, 0, count).ContinueWith(state =>
                    {
                        //Console.WriteLine(buf.Show(count));
                        _hasher.TransformBlock(buf, 0, count, null, 0);
                        _bufferManager.ReturnBuffer(buf);
                    });
                });
            }
            else
            {
                _fHandler.WriteAsync(buf, 0, count).ContinueWith(state => { _bufferManager.ReturnBuffer(buf); });
            }
        }

        private void OnComplete()
        {
            Dispose();
            AppLogger.Debug($"FileResource Complete:{_rid}");
        }

        private void OnErr(Exception e)
        {
            Dispose();
            AppLogger.Warning($"FileResource error,{_rid} : {e.Message}");
        }

        private static readonly byte[] Emptyblock = new byte[0];
        public void Dispose()
        {
            var tmp = Interlocked.Exchange(ref _unsubscribe, Disposable.Empty);
            if (tmp == null || Equals(tmp, Disposable.Empty)) return;
            
            tmp?.Dispose();
            var t = Interlocked.Exchange(ref _fHandler, null);
            _hasher.TransformFinalBlock(Emptyblock, 0, 0);
            var hash = _hasher.Hash;
            _hasher.Clear();
            _hasher = null;
            if (hash.Length > SampleDataFileFormat.Md5Len)
            {
                AppLogger.Warning($"MD5 hash length ({hash.Length}) is greater than predefined length {SampleDataFileFormat.Md5Len}");
            }
            if (t != null)
            {
                t.Position = SampleDataFileFormat.Md5StartIndex;
                t.WriteAsync(hash,0,SampleDataFileFormat.Md5Len).ContinueWith(ss =>
                {
                    t.Close();
                });
                //Console.WriteLine(hash.Show(SampleDataFileFormat.Md5Len));
            }
            AppLogger.Debug($"FileResource Dispose:{_rid}");
        }
    }
}