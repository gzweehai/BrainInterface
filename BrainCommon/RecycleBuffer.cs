using System;
using System.ServiceModel.Channels;

namespace BrainCommon
{
    public class RecycleBuffer : IDisposable
    {
        private byte[] _buf;
        private readonly int _size;
        private readonly SyncBufManager _mgr;

        public byte[] Buffer => _buf;
        
        public RecycleBuffer(int size, SyncBufManager mgr)
        {
            _size = size;
            _buf = mgr.TakeBuffer(size);
            _mgr = mgr;
        }

        public void Dispose()
        {
            _mgr.ReturnBuffer(_buf);
            _buf = null;
        }

        public DisposableValue<ArraySegment<byte>> AsDisposableValue()
        {
            return DisposableValue.Create(new ArraySegment<byte>(_buf, 0, _size), this);
        }
    }
    
    public class RecycleIntBuf : IDisposable
    {
        private int[] _buf;
        private readonly int _size;
        private readonly SyncBufManager _mgr;

        public int[] Buffer => _buf;
        
        public RecycleIntBuf(int size, SyncBufManager mgr)
        {
            _size = size;
            _buf = mgr.TakeIntBuf(size);
            _mgr = mgr;
        }

        public void Dispose()
        {
            _mgr.ReturnBuffer(_buf);
            _buf = null;
        }

        public DisposableValue<ArraySegment<int>> AsDisposableValue()
        {
            return DisposableValue.Create(new ArraySegment<int>(_buf, 0, _size), this);
        }
    }
}