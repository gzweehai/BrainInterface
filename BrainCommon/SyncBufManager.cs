using System.ServiceModel.Channels;
using System.Threading;

namespace BrainCommon
{
    public class SyncBufManager
    {
        private IntBufManager _intBufMgr;
        private BufferManager _bufferManager;

        public static SyncBufManager Create(long maxBufferPoolSize, int maxBufferSize,int maxIntBufSize)
        {
            return new SyncBufManager(maxBufferPoolSize, maxBufferSize,maxIntBufSize);
        }

        private SyncBufManager(long maxBufferPoolSize, int maxBufferSize,int maxIntBufSize)
        {
            _intBufMgr = IntBufManager.Create(maxBufferPoolSize,maxIntBufSize);
            _bufferManager=BufferManager.CreateBufferManager(maxBufferPoolSize,maxBufferSize);
        }

        public void Clear()
        {
            IntBufManager local;
            while ((local = Interlocked.Exchange(ref _intBufMgr, null)) == null)
            {
            }
            local.Clear();
            Interlocked.Exchange(ref _intBufMgr, local);
            
            BufferManager local1;
            while ((local1 = Interlocked.Exchange(ref _bufferManager, null)) == null)
            {
            }
            local1.Clear();
            Interlocked.Exchange(ref _bufferManager, local1);
        }

        public void ReturnBuffer(int[] buffer)
        {
            IntBufManager local;
            while ((local = Interlocked.Exchange(ref _intBufMgr, null)) == null)
            {
            }
            local.ReturnBuffer(buffer);
            Interlocked.Exchange(ref _intBufMgr, local);
        }

        public int[] TakeIntBuf(int bufferSize)
        {
            IntBufManager local;
            while ((local = Interlocked.Exchange(ref _intBufMgr, null)) == null)
            {
            }
            var result = local.TakeBuffer(bufferSize);
            Interlocked.Exchange(ref _intBufMgr, local);
            return result;
        }
        
        public void ReturnBuffer(byte[] buffer)
        {
            BufferManager local;
            while ((local = Interlocked.Exchange(ref _bufferManager, null)) == null)
            {
            }
            local.ReturnBuffer(buffer);
            Interlocked.Exchange(ref _bufferManager, local);
        }

        public byte[] TakeBuffer(int bufferSize)
        {
            BufferManager local;
            while ((local = Interlocked.Exchange(ref _bufferManager, null)) == null)
            {
            }
            var result = local.TakeBuffer(bufferSize);
            Interlocked.Exchange(ref _bufferManager, local);
            return result;
        }
    }
}