using System.Collections.Generic;
using System.Threading;

namespace BrainCommon.Win
{
  internal abstract class InternalBufferManager
  {
    public abstract byte[] TakeBuffer(int bufferSize);

    public abstract void ReturnBuffer(byte[] buffer);

    public abstract void Clear();

    public static InternalBufferManager Create(long maxBufferPoolSize, int maxBufferSize)
    {
      if (maxBufferPoolSize == 0L)
        return (InternalBufferManager) InternalBufferManager.GCBufferManager.Value;
      return (InternalBufferManager) new InternalBufferManager.PooledBufferManager(maxBufferPoolSize, maxBufferSize);
    }

    private class PooledBufferManager : InternalBufferManager
    {
      private const int minBufferSize = 128;
      private const int maxMissesBeforeTuning = 8;
      private const int initialBufferCount = 1;
      private readonly object tuningLock;
      private int[] bufferSizes;
      private InternalBufferManager.PooledBufferManager.BufferPool[] bufferPools;
      private long memoryLimit;
      private long remainingMemory;
      private bool areQuotasBeingTuned;
      private int totalMisses;

      public PooledBufferManager(long maxMemoryToPool, int maxBufferSize)
      {
        this.tuningLock = new object();
        this.memoryLimit = maxMemoryToPool;
        this.remainingMemory = maxMemoryToPool;
        List<InternalBufferManager.PooledBufferManager.BufferPool> bufferPoolList = new List<InternalBufferManager.PooledBufferManager.BufferPool>();
        int bufferSize = 128;
        while (true)
        {
          long num1 = this.remainingMemory / (long) bufferSize;
          int limit = num1 > (long) int.MaxValue ? int.MaxValue : (int) num1;
          if (limit > 1)
            limit = 1;
          bufferPoolList.Add(InternalBufferManager.PooledBufferManager.BufferPool.CreatePool(bufferSize, limit));
          this.remainingMemory -= (long) limit * (long) bufferSize;
          if (bufferSize < maxBufferSize)
          {
            long num2 = (long) bufferSize * 2L;
            bufferSize = num2 <= (long) maxBufferSize ? (int) num2 : maxBufferSize;
          }
          else
            break;
        }
        this.bufferPools = bufferPoolList.ToArray();
        this.bufferSizes = new int[this.bufferPools.Length];
        for (int index = 0; index < this.bufferPools.Length; ++index)
          this.bufferSizes[index] = this.bufferPools[index].BufferSize;
      }

      public override void Clear()
      {
        for (int index = 0; index < this.bufferPools.Length; ++index)
          this.bufferPools[index].Clear();
      }

      private void ChangeQuota(ref InternalBufferManager.PooledBufferManager.BufferPool bufferPool, int delta)
      {
        InternalBufferManager.PooledBufferManager.BufferPool bufferPool1 = bufferPool;
        int limit = bufferPool1.Limit + delta;
        InternalBufferManager.PooledBufferManager.BufferPool pool = InternalBufferManager.PooledBufferManager.BufferPool.CreatePool(bufferPool1.BufferSize, limit);
        for (int index = 0; index < limit; ++index)
        {
          byte[] buffer = bufferPool1.Take();
          if (buffer != null)
          {
            pool.Return(buffer);
            pool.IncrementCount();
          }
          else
            break;
        }
        this.remainingMemory -= (long) (bufferPool1.BufferSize * delta);
        bufferPool = pool;
      }

      private void DecreaseQuota(ref InternalBufferManager.PooledBufferManager.BufferPool bufferPool)
      {
        this.ChangeQuota(ref bufferPool, -1);
      }

      private int FindMostExcessivePool()
      {
        long num1 = 0;
        int num2 = -1;
        for (int index = 0; index < this.bufferPools.Length; ++index)
        {
          InternalBufferManager.PooledBufferManager.BufferPool bufferPool = this.bufferPools[index];
          if (bufferPool.Peak < bufferPool.Limit)
          {
            long num3 = (long) (bufferPool.Limit - bufferPool.Peak) * (long) bufferPool.BufferSize;
            if (num3 > num1)
            {
              num2 = index;
              num1 = num3;
            }
          }
        }
        return num2;
      }

      private int FindMostStarvedPool()
      {
        long num1 = 0;
        int num2 = -1;
        for (int index = 0; index < this.bufferPools.Length; ++index)
        {
          InternalBufferManager.PooledBufferManager.BufferPool bufferPool = this.bufferPools[index];
          if (bufferPool.Peak == bufferPool.Limit)
          {
            long num3 = (long) bufferPool.Misses * (long) bufferPool.BufferSize;
            if (num3 > num1)
            {
              num2 = index;
              num1 = num3;
            }
          }
        }
        return num2;
      }

      private InternalBufferManager.PooledBufferManager.BufferPool FindPool(int desiredBufferSize)
      {
        for (int index = 0; index < this.bufferSizes.Length; ++index)
        {
          if (desiredBufferSize <= this.bufferSizes[index])
            return this.bufferPools[index];
        }
        return (InternalBufferManager.PooledBufferManager.BufferPool) null;
      }

      private void IncreaseQuota(ref InternalBufferManager.PooledBufferManager.BufferPool bufferPool)
      {
        this.ChangeQuota(ref bufferPool, 1);
      }

      public override void ReturnBuffer(byte[] buffer)
      {
        InternalBufferManager.PooledBufferManager.BufferPool pool = this.FindPool(buffer.Length);
        if (pool == null)
          return;
        if (buffer.Length != pool.BufferSize)
          throw Fx.Exception.Argument(nameof (buffer), InternalSR.BufferIsNotRightSizeForBufferManager);
        if (!pool.Return(buffer))
          return;
        pool.IncrementCount();
      }

      public override byte[] TakeBuffer(int bufferSize)
      {
        InternalBufferManager.PooledBufferManager.BufferPool pool = this.FindPool(bufferSize);
        byte[] numArray1;
        if (pool != null)
        {
          byte[] numArray2 = pool.Take();
          if (numArray2 != null)
          {
            pool.DecrementCount();
            numArray1 = numArray2;
          }
          else
          {
            if (pool.Peak == pool.Limit)
            {
              ++pool.Misses;
              if (++this.totalMisses >= 8)
                this.TuneQuotas();
            }
            numArray1 = Fx.AllocateByteArray(pool.BufferSize);
          }
        }
        else
        {
          numArray1 = Fx.AllocateByteArray(bufferSize);
        }
        return numArray1;
      }

      private void TuneQuotas()
      {
        if (this.areQuotasBeingTuned)
          return;
        bool lockTaken = false;
        try
        {
          Monitor.TryEnter(this.tuningLock, ref lockTaken);
          if (!lockTaken || this.areQuotasBeingTuned)
            return;
          this.areQuotasBeingTuned = true;
        }
        finally
        {
          if (lockTaken)
            Monitor.Exit(this.tuningLock);
        }
        int mostStarvedPool = this.FindMostStarvedPool();
        if (mostStarvedPool >= 0)
        {
          InternalBufferManager.PooledBufferManager.BufferPool bufferPool = this.bufferPools[mostStarvedPool];
          if (this.remainingMemory < (long) bufferPool.BufferSize)
          {
            int mostExcessivePool = this.FindMostExcessivePool();
            if (mostExcessivePool >= 0)
              this.DecreaseQuota(ref this.bufferPools[mostExcessivePool]);
          }
          if (this.remainingMemory >= (long) bufferPool.BufferSize)
            this.IncreaseQuota(ref this.bufferPools[mostStarvedPool]);
        }
        for (int index = 0; index < this.bufferPools.Length; ++index)
          this.bufferPools[index].Misses = 0;
        this.totalMisses = 0;
        this.areQuotasBeingTuned = false;
      }

      private abstract class BufferPool
      {
        private int bufferSize;
        private int count;
        private int limit;
        private int misses;
        private int peak;

        public BufferPool(int bufferSize, int limit)
        {
          this.bufferSize = bufferSize;
          this.limit = limit;
        }

        public int BufferSize
        {
          get
          {
            return this.bufferSize;
          }
        }

        public int Limit
        {
          get
          {
            return this.limit;
          }
        }

        public int Misses
        {
          get
          {
            return this.misses;
          }
          set
          {
            this.misses = value;
          }
        }

        public int Peak
        {
          get
          {
            return this.peak;
          }
        }

        public void Clear()
        {
          this.OnClear();
          this.count = 0;
        }

        public void DecrementCount()
        {
          int num = this.count - 1;
          if (num < 0)
            return;
          this.count = num;
        }

        public void IncrementCount()
        {
          int num = this.count + 1;
          if (num > this.limit)
            return;
          this.count = num;
          if (num <= this.peak)
            return;
          this.peak = num;
        }

        internal abstract byte[] Take();

        internal abstract bool Return(byte[] buffer);

        internal abstract void OnClear();

        internal static InternalBufferManager.PooledBufferManager.BufferPool CreatePool(int bufferSize, int limit)
        {
          if (bufferSize < 85000)
            return (InternalBufferManager.PooledBufferManager.BufferPool) new InternalBufferManager.PooledBufferManager.BufferPool.SynchronizedBufferPool(bufferSize, limit);
          return (InternalBufferManager.PooledBufferManager.BufferPool) new InternalBufferManager.PooledBufferManager.BufferPool.LargeBufferPool(bufferSize, limit);
        }

        private class SynchronizedBufferPool : InternalBufferManager.PooledBufferManager.BufferPool
        {
          private SynchronizedPool<byte[]> innerPool;

          internal SynchronizedBufferPool(int bufferSize, int limit)
            : base(bufferSize, limit)
          {
            this.innerPool = new SynchronizedPool<byte[]>(limit);
          }

          internal override void OnClear()
          {
            this.innerPool.Clear();
          }

          internal override byte[] Take()
          {
            return this.innerPool.Take();
          }

          internal override bool Return(byte[] buffer)
          {
            return this.innerPool.Return(buffer);
          }
        }

        private class LargeBufferPool : InternalBufferManager.PooledBufferManager.BufferPool
        {
          private Stack<byte[]> items;

          internal LargeBufferPool(int bufferSize, int limit)
            : base(bufferSize, limit)
          {
            this.items = new Stack<byte[]>(limit);
          }

          private object ThisLock
          {
            get
            {
              return (object) this.items;
            }
          }

          internal override void OnClear()
          {
            lock (this.ThisLock)
              this.items.Clear();
          }

          internal override byte[] Take()
          {
            lock (this.ThisLock)
            {
              if (this.items.Count > 0)
                return this.items.Pop();
            }
            return (byte[]) null;
          }

          internal override bool Return(byte[] buffer)
          {
            lock (this.ThisLock)
            {
              if (this.items.Count < this.Limit)
              {
                this.items.Push(buffer);
                return true;
              }
            }
            return false;
          }
        }
      }
    }

    private class GCBufferManager : InternalBufferManager
    {
      private static InternalBufferManager.GCBufferManager value = new InternalBufferManager.GCBufferManager();

      private GCBufferManager()
      {
      }

      public static InternalBufferManager.GCBufferManager Value
      {
        get
        {
          return InternalBufferManager.GCBufferManager.value;
        }
      }

      public override void Clear()
      {
      }

      public override byte[] TakeBuffer(int bufferSize)
      {
        return Fx.AllocateByteArray(bufferSize);
      }

      public override void ReturnBuffer(byte[] buffer)
      {
      }
    }
  }
}
