using System;
using System.Net.Sockets;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.ServiceModel.Channels;
using System.Threading;
using BrainCommon;
using BrainNetwork.RxSocket.Common;

namespace BrainNetwork.RxSocket.Protocol
{
    public static class FrameProtocolExtensions
    {
        public static ISubject<DisposableValue<ArraySegment<byte>>, DisposableValue<ArraySegment<byte>>>
            ToFixedLenFrameSubject(this Socket socket, ISimpleFrameEncoder encoder, IFixedLenFrameDecoder decoder,
                SyncBufManager bufferManager, CancellationToken token)
        {
            return Subject.Create<DisposableValue<ArraySegment<byte>>, DisposableValue<ArraySegment<byte>>>(
                socket.ToFrameClientObserver(encoder, token,bufferManager),
                socket.ToFixedLenFrameObservable(bufferManager, decoder));
        }

        public static IObserver<DisposableValue<ArraySegment<byte>>> ToFrameClientObserver(this Socket socket,
            ISimpleFrameEncoder encoder, CancellationToken token,SyncBufManager bufMgr)
        {
            return Observer.Create<DisposableValue<ArraySegment<byte>>>(async disposableBuffer =>
            {
                await socket.SendCompletelyAsync(
                    encoder.EncoderSendFrame(disposableBuffer.Value),
                    encoder.SendFlags,
                    token,bufMgr);
            });
        }

        public static IObservable<DisposableValue<ArraySegment<byte>>> ToFixedLenFrameObservable(this Socket socket,
            SyncBufManager bufferManager, IFixedLenFrameDecoder decoder)
        {
            return Observable.Create<DisposableValue<ArraySegment<byte>>>(async (observer, token) =>
            {
                var receivedFlag = decoder.ReceivedFlags;
                var decoderLenByteCount = decoder.LenByteCount;
                var hc = 1 + decoderLenByteCount;
                var decoderHeader = decoder.Header;
                var decoderTail = decoder.Tail;
                var headerBuffer = new byte[hc];

                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        if (await socket.ReceiveCompletelyAsync(headerBuffer, hc, receivedFlag, token) != hc)
                            break;
                        if (headerBuffer[0] != decoderHeader)
                        {
                            AppLogger.Warning($"corruted packet: invalid frame header");
                            break;
                        }
                        var length = LengthBitConverter.FromByte(headerBuffer, 1, decoderLenByteCount) - 1;

                        var buffer = bufferManager.TakeBuffer(length);
                        if (await socket.ReceiveCompletelyAsync(buffer, length, receivedFlag, token) != length)
                            break;

                        var arraySegment = new ArraySegment<byte>(buffer, 0, length);
                        observer.OnNext(
                            new DisposableValue<ArraySegment<byte>>(arraySegment,
                                Disposable.Create(() => bufferManager.ReturnBuffer(buffer))));

                        if (await socket.ReceiveAsync(headerBuffer, 0, 1, receivedFlag) == 0)
                            break;
                        if (headerBuffer[0] != decoderTail)
                        {
                            AppLogger.Warning($"corruted packet: invalid frame tail,"+ headerBuffer[0].Show());
                            break;
                        }
                    }

                    observer.OnCompleted();
                }
                catch (Exception error)
                {
                    observer.OnError(error);
                }
            });
        }

        #region dynamic frame
        public static ISubject<DisposableValue<ArraySegment<byte>>, DisposableValue<ArraySegment<byte>>>
            ToDynamicFrameSubject(this Socket socket, ISimpleFrameEncoder encoder, IDynamicFrameDecoder decoder,
                SyncBufManager bufferManager, CancellationToken token)
        {
            return Subject.Create<DisposableValue<ArraySegment<byte>>, DisposableValue<ArraySegment<byte>>>(
                socket.ToFrameClientObserver(encoder, token,bufferManager),
                socket.ToDynamicFrameObservable(bufferManager, decoder));
        }

        public static IObservable<DisposableValue<ArraySegment<byte>>> ToDynamicFrameObservable(this Socket socket,
            SyncBufManager bufferManager, IDynamicFrameDecoder decoder)
        {
            return Observable.Create<DisposableValue<ArraySegment<byte>>>(async (observer, token) =>
            {
                try
                {
                    var state = decoder.InitState();
                    byte[] leftoverBuf = null;
                    //rider suggestion is buggy: this variable must declare outside the loop in the case when receive length is zero
                    int leftoverCount = 0; 
                    while (!token.IsCancellationRequested)
                    {
                        byte[] bufferArray;
                        int startIdx;
                        if (leftoverBuf != null)
                        {
                            bufferArray = leftoverBuf;
                            startIdx = leftoverCount;
                            leftoverBuf = null;
                        }
                        else
                        {
                            bufferArray = bufferManager.TakeBuffer(decoder.BufferSize);
                            startIdx = 0;
                        }
                        var pair = await socket.ReceiveDynamicFrame(state, bufferArray, startIdx, decoder, token);
                        var receiveLen = pair.Item1;
                        leftoverCount = pair.Item2;
                        if (receiveLen == 0) //no data received, and leftoverCount should be zero
                        {
                            var dropFrameStrategy = decoder.CheckDropFrame(state, bufferArray, startIdx);
                            switch (dropFrameStrategy)
                            {
                                case DropFrameStrategyEnum.DropAndClose:
                                    //reclaim buffer array
                                    bufferManager.ReturnBuffer(bufferArray);
                                    break;
                                case DropFrameStrategyEnum.DropAndRestart:
                                    //reclaim buffer array
                                    bufferManager.ReturnBuffer(bufferArray);
                                    continue;
                                case DropFrameStrategyEnum.KeepAndContinue:
                                    //keep last received data
                                    leftoverBuf = bufferArray;
                                    leftoverCount = startIdx;
                                    continue;
                            }
                            if (dropFrameStrategy == DropFrameStrategyEnum.DropAndClose)
                                break;
                        }
                        if (receiveLen == -1) //overflow,TODO support extensible frame in future
                        {
                            //reclaim buffer array
                            bufferManager.ReturnBuffer(bufferArray);
                            break;
                        }
                        //copy leftover
                        if (leftoverCount > 0)
                        {
                            leftoverBuf = bufferManager.TakeBuffer(decoder.BufferSize);
                            Buffer.BlockCopy(bufferArray, receiveLen - leftoverCount, leftoverBuf, 0, leftoverCount);
                        }

                        var arraySegment = decoder.BuildFrame(state, bufferArray, receiveLen, leftoverCount);
                        observer.OnNext(
                            new DisposableValue<ArraySegment<byte>>(arraySegment,
                                Disposable.Create(() => bufferManager.ReturnBuffer(bufferArray))));
                    }

                    observer.OnCompleted();

                    socket.Close();
                }
                catch (Exception error)
                {
                    observer.OnError(error);
                }
            });
        }
        #endregion
    }
}