namespace BrainCommon
{
  public class IntBufManager:AbsBufManager<int>
  {
    public static IntBufManager Create(long maxBufferPoolSize, int maxBufferSize)
    {
      return new IntBufManager(maxBufferPoolSize, maxBufferSize);
    }

    private IntBufManager(long maxBufferPoolSize, int maxBufferSize) : base(maxBufferPoolSize, maxBufferSize)
    {
    }
  }
}