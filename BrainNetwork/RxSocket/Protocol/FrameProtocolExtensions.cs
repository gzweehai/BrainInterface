using System;
using System.Net.Sockets;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.ServiceModel.Channels;
using System.Threading;
using BrainNetwork.RxSocket.Common;

namespace BrainNetwork.RxSocket.Protocol
{
    public static class FrameProtocolExtensions
    {
        public static ISubject<DisposableValue<ArraySegment<byte>>, DisposableValue<ArraySegment<byte>>>
            ToFrameClientSubject(this Socket socket, ISimpleFrameEncoder encoder, ISimpleFrameDecoder decoder,
                BufferManager bufferManager, CancellationToken token)
        {
            return Subject.Create<DisposableValue<ArraySegment<byte>>, DisposableValue<ArraySegment<byte>>>(socket.ToFrameClientObserver(encoder, token),
                socket.ToFrameClientObservable(bufferManager, decoder));
        }

        public static IObservable<DisposableValue<ArraySegment<byte>>> ToFrameClientObservable(this Socket socket,
            BufferManager bufferManager, ISimpleFrameDecoder decoder)
        {
            return Observable.Create<DisposableValue<ArraySegment<byte>>>(async (observer, token) =>
            {
                try
                {
                    var state = decoder.InitState();
                    byte[] leftoverBuf = null;
                    int leftoverCount = 0; //rider suggestion is buggy: this variable must declare outside the loop in the case when receive length is zero
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
                        var pair = await socket.ReceiveCompletelyAsync(state, bufferArray, startIdx, decoder, token);
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

                        var arraySegment = decoder.BuildFrame(state,bufferArray, receiveLen,leftoverCount);
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

        public static IObserver<DisposableValue<ArraySegment<byte>>> ToFrameClientObserver(this Socket socket,
            ISimpleFrameEncoder encoder, CancellationToken token)
        {
            return Observer.Create<DisposableValue<ArraySegment<byte>>>(async disposableBuffer =>
            {
                await socket.SendCompletelyAsync(
                    encoder.EncoderSendFrame(disposableBuffer.Value),
                    encoder.SendFlags, //SocketFlags.None,
                    token);
            });
        }
    }
}