using System;
using System.Runtime;

namespace BrainCommon.Win
{
  public abstract class BufferManager
  {
    public abstract byte[] TakeBuffer(int bufferSize);

    public abstract void ReturnBuffer(byte[] buffer);

    public abstract void Clear();

    public static BufferManager CreateBufferManager(long maxBufferPoolSize, int maxBufferSize)
    {
      return (BufferManager) new BufferManager.WrappingBufferManager(InternalBufferManager.Create(maxBufferPoolSize, maxBufferSize));
    }

    internal static InternalBufferManager GetInternalBufferManager(BufferManager bufferManager)
    {
      if (bufferManager is BufferManager.WrappingBufferManager)
        return ((BufferManager.WrappingBufferManager) bufferManager).InternalBufferManager;
      return (InternalBufferManager) new BufferManager.WrappingInternalBufferManager(bufferManager);
    }

    protected BufferManager()
    {
    }

    private class WrappingBufferManager : BufferManager
    {
      private InternalBufferManager innerBufferManager;

      public WrappingBufferManager(InternalBufferManager innerBufferManager)
      {
        this.innerBufferManager = innerBufferManager;
      }

      public InternalBufferManager InternalBufferManager
      {
        get
        {
          return this.innerBufferManager;
        }
      }

      public override byte[] TakeBuffer(int bufferSize)
      {
        return this.innerBufferManager.TakeBuffer(bufferSize);
      }

      public override void ReturnBuffer(byte[] buffer)
      {
        this.innerBufferManager.ReturnBuffer(buffer);
      }

      public override void Clear()
      {
        this.innerBufferManager.Clear();
      }
    }

    private class WrappingInternalBufferManager : InternalBufferManager
    {
      private BufferManager innerBufferManager;

      public WrappingInternalBufferManager(BufferManager innerBufferManager)
      {
        this.innerBufferManager = innerBufferManager;
      }

      public override void Clear()
      {
        this.innerBufferManager.Clear();
      }

      public override void ReturnBuffer(byte[] buffer)
      {
        this.innerBufferManager.ReturnBuffer(buffer);
      }

      public override byte[] TakeBuffer(int bufferSize)
      {
        return this.innerBufferManager.TakeBuffer(bufferSize);
      }
    }
  }
}
