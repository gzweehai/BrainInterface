using System.ServiceModel.Channels;
using System.Threading;

namespace BrainCommon
{
    public interface IAbsBufManager<T> where T : struct
    {
        void Clear();
        void ReturnBuffer(T[] buffer);
        T[] TakeBuffer(int bufferSize);
    }

    public class SyncAbsBufManager<T> : IAbsBufManager<T> where T : struct
    {
        private AbsBufManager<T> _instance;

        public SyncAbsBufManager(AbsBufManager<T> instance)
        {
            _instance = instance;
        }

        public static SyncAbsBufManager<T> Create(long maxBufferPoolSize, int maxBufferSize)
        {
            var defaultBufMgr = new DefaultBufMgr(maxBufferPoolSize, maxBufferSize);
            return new SyncAbsBufManager<T>(defaultBufMgr);
        }

        private class DefaultBufMgr : AbsBufManager<T>
        {
            public DefaultBufMgr(long maxBufferPoolSize, int maxBufferSize) : base(maxBufferPoolSize, maxBufferSize)
            {
            }
        }

        public void Clear()
        {
            AbsBufManager<T> local;
            while ((local = Interlocked.Exchange(ref _instance, null)) == null)
            {
            }
            local.Clear();
            Interlocked.Exchange(ref _instance, local);
        }

        public void ReturnBuffer(T[] buffer)
        {
            AbsBufManager<T> local;
            while ((local = Interlocked.Exchange(ref _instance, null)) == null)
            {
            }
            local.ReturnBuffer(buffer);
            Interlocked.Exchange(ref _instance, local);
        }

        public T[] TakeBuffer(int bufferSize)
        {
            AbsBufManager<T> local;
            while ((local = Interlocked.Exchange(ref _instance, null)) == null)
            {
            }
            var result = local.TakeBuffer(bufferSize);
            Interlocked.Exchange(ref _instance, local);
            return result;
        }
    }

    public class SyncByteBufManager : IAbsBufManager<byte>
    {
        private BufferManager _instance;

        public SyncByteBufManager(BufferManager instance)
        {
            _instance = instance;
        }

        public static SyncByteBufManager Create(long maxBufferPoolSize, int maxBufferSize)
        {
            return new SyncByteBufManager(BufferManager.CreateBufferManager(maxBufferPoolSize, maxBufferSize));
        }

        public void Clear()
        {
            BufferManager local;
            while ((local = Interlocked.Exchange(ref _instance, null)) == null)
            {
            }
            local.Clear();
            Interlocked.Exchange(ref _instance, local);
        }

        public void ReturnBuffer(byte[] buffer)
        {
            BufferManager local;
            while ((local = Interlocked.Exchange(ref _instance, null)) == null)
            {
            }
            local.ReturnBuffer(buffer);
            Interlocked.Exchange(ref _instance, local);
        }

        public byte[] TakeBuffer(int bufferSize)
        {
            BufferManager local;
            while ((local = Interlocked.Exchange(ref _instance, null)) == null)
            {
            }
            var result = local.TakeBuffer(bufferSize);
            Interlocked.Exchange(ref _instance, local);
            return result;
        }
    }

}