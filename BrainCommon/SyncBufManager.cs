﻿namespace BrainCommon
{
    public class SyncBufManager
    {
        private IAbsBufManager<int> _intBufMgr;
        private IAbsBufManager<byte> _bufferManager;
        private IAbsBufManager<double> _doubleBufMgr;

        public static SyncBufManager Create(long maxBufferPoolSize, int maxBufferSize, int maxIntBufSize)
        {
            return new SyncBufManager(maxBufferPoolSize, maxBufferSize, maxIntBufSize);
        }

        private SyncBufManager(long maxBufferPoolSize, int maxBufferSize, int maxIntBufSize)
        {
            _intBufMgr = new SyncAbsBufManager<int>(IntBufManager.Create(maxBufferPoolSize, maxIntBufSize));
            _doubleBufMgr = new SyncAbsBufManager<double>(DoubleBufManager.Create(maxBufferPoolSize, maxIntBufSize));
            _bufferManager = new SyncByteBufManager(maxBufferPoolSize, maxBufferSize);
        }

        public void Clear()
        {
            _intBufMgr.Clear();
            _doubleBufMgr.Clear();
            _bufferManager.Clear();
        }

        public void ReturnBuffer(double[] buffer)
        {
            _doubleBufMgr.ReturnBuffer(buffer);
        }

        public double[] TakeDoubleBuf(int bufferSize)
        {
            return _doubleBufMgr.TakeBuffer(bufferSize);
        }

        public void ReturnBuffer(int[] buffer)
        {
            _intBufMgr.ReturnBuffer(buffer);
        }

        public int[] TakeIntBuf(int bufferSize)
        {
            return _intBufMgr.TakeBuffer(bufferSize);
        }

        public void ReturnBuffer(byte[] buffer)
        {
            _bufferManager.ReturnBuffer(buffer);
        }

        public byte[] TakeBuffer(int bufferSize)
        {
            return _bufferManager.TakeBuffer(bufferSize);
        }
    }
}