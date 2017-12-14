using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using BrainNetwork.RxSocket.Protocol;

namespace BrainNetwork.RxSocket.Protocol
{
    //TODO multi frame protocol support
    public interface ISimpleFrameDecoder
    {
        SocketFlags ReceivedFlags { get; }

        int BufferSize { get; }

        object InitState();

        int LookupSize(object state);

        /// <summary>
        /// check whether a frame is received completely, and whether next frame's data is received
        /// </summary>
        /// <param name="state"></param>
        /// <param name="buffer"></param>
        /// <param name="received"></param>
        /// <returns>flag a frame is complete, and leftover data counts for the next frame</returns>
        (bool, int) CheckFinished(object state, byte[] buffer, int received);

        ArraySegment<byte> BuildFrame(object state, byte[] bufferArray, int receiveLen, int leftoverCount);

        DropFrameStrategyEnum CheckDropFrame(object state, byte[] bufferArray, int leftoverCount);
    }

    public enum DropFrameStrategyEnum
    {
        DropAndClose,
        KeepAndContinue,
        DropAndRestart,
    }

    public abstract class AbsSimpleDecoder : ISimpleFrameDecoder
    {
        public abstract SocketFlags ReceivedFlags { get; }
        public abstract int BufferSize { get; }
        public abstract object InitState();
        public abstract int LookupSize(object state);
        public abstract (bool, int) CheckFinished(object state, byte[] buffer, int received);

        protected abstract ArraySegment<byte> BuildFrameImpl(object state, byte[] bufferArray, int receiveLen,
            int leftoverCount);

        protected abstract void ResetState(object state);

        public ArraySegment<byte> BuildFrame(object state, byte[] bufferArray, int receiveLen, int leftoverCount)
        {
            try
            {
                return BuildFrameImpl(state, bufferArray, receiveLen, leftoverCount);
            }
            finally
            {
                ResetState(state);
            }
        }

        public DropFrameStrategyEnum CheckDropFrame(object state, byte[] bufferArray, int leftoverCount)
        {
            return DropFrameStrategyEnum.DropAndClose;
        }
    }

    public interface ISimpleFrameEncoder
    {
        IList<ArraySegment<byte>> EncoderSendFrame(ArraySegment<byte> data);
        SocketFlags SendFlags { get; }
    }
}

namespace BrainNetwork.RxSocket.Common
{
    public static partial class SocketExtensions
    {
        /// <summary>
        /// return received counts and leftover counts start at index of received count
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="state">decoder state</param>
        /// <param name="buffer"></param>
        /// <param name="startIdx">received data start to fill at this index</param>
        /// <param name="decoder"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<(int, int)> ReceiveCompletelyAsync(this Socket socket, object state,
            byte[] buffer, int startIdx, ISimpleFrameDecoder decoder, CancellationToken token)
        {
            var received = 0;
            var leftover = 0;
            var stopReceive = false;
            var socketFlags = decoder.ReceivedFlags;

            //check left over buffer
            if (startIdx > 0)
                (stopReceive, leftover) = decoder.CheckFinished(state, buffer, startIdx);

            while (!stopReceive)
            {
                token.ThrowIfCancellationRequested();

                if (startIdx + received >= buffer.Length) //overflow checking
                    return (-1, 0);

                var lookupZise = decoder.LookupSize(state);
                var bytes = await socket.ReceiveAsync(buffer, startIdx + received, lookupZise, socketFlags);
                if (bytes == 0)
                {
                    return (startIdx + received, leftover);
                }
                received += bytes;
                (stopReceive, leftover) = decoder.CheckFinished(state, buffer, startIdx + received);
            }

            return (startIdx + received, leftover);
        }
    }
}