using System;
using System.ServiceModel.Channels;

namespace BrainCommon
{
    public class RecycleBuffer : IDisposable
    {
        private readonly byte[] _buf;
        private readonly BufferManager _mgr;

        public byte[] Buffer => _buf;
        
        public RecycleBuffer(int size, BufferManager mgr)
        {
            _buf = mgr.TakeBuffer(size);
            _mgr = mgr;
        }

        public void Dispose()
        {
            _mgr.ReturnBuffer(_buf);
        }
    }
}