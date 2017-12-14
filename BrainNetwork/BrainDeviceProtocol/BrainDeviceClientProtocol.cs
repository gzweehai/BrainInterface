using System;
using System.Collections.Generic;
using System.Net.Sockets;
using BrainCommon;
using BrainNetwork.RxSocket.Protocol;

namespace BrainNetwork.BrainDeviceProtocol
{
    public class ClientFrameDecoder : AbsSimpleDecoder
    {
        public override SocketFlags ReceivedFlags => SocketFlags.None;
        public override int BufferSize => 1024;

        private readonly byte FrameHeader;
        private readonly byte FrameTail;

        public ClientFrameDecoder(byte frameHeader, byte frameTail)
        {
            FrameHeader = frameHeader;
            FrameTail = frameTail;
        }

        public override object InitState()
        {
            return new FrameDecodeState();
        }

        public override int LookupSize(object state)
        {
            return 4;
        }

        public override (bool, int) CheckFinished(object state, byte[] buffer, int received)
        {
            if (received <= 2) return (false, 0);
            if (!(state is FrameDecodeState frameState)) throw new ArgumentException("incorrect state object");

            if (frameState.CommandId == -1)
            {
                var header = buffer[0];
                if (FrameHeader != header) throw new ArgumentException($"incorrect frame header {header.Show()}");
                var cmd = buffer[1];
                if (cmd <= 0) throw new ArgumentException($"incorrect command id {cmd}");
                frameState.CommandId = cmd;
                frameState.LastCheckCount = 2;
            }

            for (var i = frameState.LastCheckCount; i < received; i++)
            {
                if (FrameTail == buffer[i])
                {
                    frameState.LastCheckCount = i + 1;
                    return (true, received - frameState.LastCheckCount);
                }
            }
            frameState.LastCheckCount = received;
            return (false, 0);
        }

        protected override ArraySegment<byte> BuildFrameImpl(object state, byte[] buffer, int receiveLen,
            int leftoverCount)
        {
            //skip incorrect frame data
            if (receiveLen <= 2) return new ArraySegment<byte>(buffer, 0, 0);
            if (FrameHeader != buffer[0]) return new ArraySegment<byte>(buffer, 0, 0);
            if (buffer[1] <= 0) return new ArraySegment<byte>(buffer, 0, 0);
            var frameEndIdx = receiveLen - leftoverCount - 1;
            if (FrameTail != buffer[frameEndIdx]) return new ArraySegment<byte>(buffer, 0, 0);

            if (!(state is FrameDecodeState frameState)) throw new ArgumentException("incorrect state object");
            if (frameEndIdx != frameState.LastCheckCount - 1) throw new ArgumentException("incorrect state object");
            if (frameState.CommandId < 0) throw new ArgumentException("incorrect state object");

            return new ArraySegment<byte>(buffer, 1, frameEndIdx - 1);
        }

        protected override void ResetState(object state)
        {
            if (!(state is FrameDecodeState frameState)) throw new ArgumentException("incorrect state object");
            frameState.CommandId = -1;
            frameState.LastCheckCount = 0;
        }

        private class FrameDecodeState
        {
            public int CommandId = -1;
            public int LastCheckCount = 0;
        }
    }

    public class ClientFrameEncoder : ISimpleFrameEncoder
    {
        private readonly byte FrameHeader;
        private readonly byte FrameTail;
        private readonly ArraySegment<byte> FrameHeaderSeg;
        private readonly ArraySegment<byte> FrameTailSeg;

        public ClientFrameEncoder(byte frameHeader, byte frameTail)
        {
            FrameHeader = frameHeader;
            FrameTail = frameTail;
            var buf = new byte[] {FrameHeader};
            FrameHeaderSeg = new ArraySegment<byte>(buf, 0, 1);
            buf = new byte[] {FrameTail};
            FrameTailSeg = new ArraySegment<byte>(buf, 0, 1);
        }

        public IList<ArraySegment<byte>> EncoderSendFrame(ArraySegment<byte> data)
        {
            if (data.Array == null) throw new ArgumentException("ArraySegment contains no data");
            return new[]
            {
                FrameHeaderSeg,
                new ArraySegment<byte>(data.Array, 0, data.Count),
                FrameTailSeg
            };
        }

        public SocketFlags SendFlags => SocketFlags.None;
    }
}