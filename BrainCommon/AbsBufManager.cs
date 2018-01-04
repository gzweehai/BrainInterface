using System;
using System.Collections.Generic;
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

        public SyncByteBufManager(long maxBufferPoolSize, int maxBufferSize)
        {
            _instance = BufferManager.CreateBufferManager(maxBufferPoolSize, maxBufferSize);
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

    public abstract class AbsBufManager<T> : IAbsBufManager<T> where T : struct
    {
        private readonly List<T[]>[] buffers = new List<T[]>[27];
        private const int log_min = 5;
        private long max_pool_size;
        private int max_size;
        private int hits;
        private int miss;

        private static int log2(uint n)
        {
            var num = 0;
            if (n >= 65536U)
            {
                n >>= 16;
                num += 16;
            }
            if (n >= 256U)
            {
                n >>= 8;
                num += 8;
            }
            if (n >= 16U)
            {
                n >>= 4;
                num += 4;
            }
            if (n >= 4U)
            {
                n >>= 2;
                num += 2;
            }
            if (n >= 2U)
                ++num;
            if ((int) n != 0)
                return num;
            return -1;
        }

        protected AbsBufManager(long maxBufferPoolSize, int maxBufferSize)
        {
            max_pool_size = maxBufferPoolSize;
            max_size = maxBufferSize;
        }

        public void Clear()
        {
            foreach (var buffer in buffers)
            {
                buffer?.Clear();
            }
            Array.Clear(buffers, 0, buffers.Length);
        }

        public void ReturnBuffer(T[] buffer)
        {
            if (buffer == null)
                return;
            var index = log2((uint) buffer.Length);
            if (index > log_min)
                index -= log_min;
            (buffers[index] ?? (buffers[index] = new List<T[]>())).Add(buffer);
        }

        public T[] TakeBuffer(int bufferSize)
        {
            if (bufferSize < 0 || max_size >= 0 && bufferSize > max_size)
                throw new ArgumentOutOfRangeException();
            var index = log2((uint) bufferSize);
            if (index > log_min)
                index -= log_min;
            var buffer = buffers[index];
            if (buffer == null || buffer.Count == 0)
                return new T[bufferSize];
            foreach (var numArray in buffer)
            {
                if (numArray.Length >= bufferSize)
                {
                    hits = hits + 1;
                    buffer.Remove(numArray);
                    return numArray;
                }
            }
            return new T[bufferSize];
        }
    }
}