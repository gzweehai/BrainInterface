namespace BrainCommon
{
    public class DoubleBufManager: AbsBufManager<double>
    {
        public static DoubleBufManager Create(long maxBufferPoolSize, int maxBufferSize)
        {
            return new DoubleBufManager(maxBufferPoolSize, maxBufferSize);
        }

        private DoubleBufManager(long maxBufferPoolSize, int maxBufferSize) : base(maxBufferPoolSize, maxBufferSize)
        {
        }
    }
}