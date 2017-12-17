using System;
using System.ServiceModel.Channels;

namespace BrainCommon
{
    public class RecycleBuffer : IDisposable
    {
        private readonly byte[] _buf;
        private readonly int _size;
        private readonly BufferManager _mgr;

        public byte[] Buffer => _buf;
        
        public RecycleBuffer(int size, BufferManager mgr)
        {
            _size = size;
            _buf = mgr.TakeBuffer(size);
            _mgr = mgr;
        }

        public void Dispose()
        {
            _mgr.ReturnBuffer(_buf);
        }

        public DisposableValue<ArraySegment<byte>> AsDisposableValue()
        {
            return DisposableValue.Create(new ArraySegment<byte>(_buf, 0, _size), this);
        }
    }
}