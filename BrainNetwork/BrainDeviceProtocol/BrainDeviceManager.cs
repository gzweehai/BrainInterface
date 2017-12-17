using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BrainCommon;
using BrainNetwork.RxSocket.Common;
using BrainNetwork.RxSocket.Protocol;

namespace BrainNetwork.BrainDeviceProtocol
{
    public static partial class BrainDeviceManager
    {
        private static BufferManager bufferManager;
        private static ClientFrameEncoder encoder;
        private static FixedLenFrameDecoder decoder;
        private static IDisposable observerDisposable;
        private static CancellationTokenSource cts;

        public static void Init()
        {
            bufferManager = BufferManager.CreateBufferManager(2 << 16, 1024);
            encoder = new ClientFrameEncoder(0xA0, 0XC0);
            decoder = new FixedLenFrameDecoder(0xA0, 0XC0);
            _dataStream = new Subject<(byte, int[], ArraySegment<byte>)>();
            _stateStream = new Subject<BrainDevState>();
            _stateStream.OnNext(_devState);
        }

        public static async Task<DevCommandSender> Connnect(string ip, int port)
        {
            cts?.Cancel();
            var endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(endPoint);
            cts = new CancellationTokenSource();

            var frameClientSubject =
                socket.ToFixedLenFrameSubject(encoder, decoder, bufferManager, cts.Token);

            observerDisposable =
                frameClientSubject
                    .ObserveOn(TaskPoolScheduler.Default)
                    .Subscribe(
                        managedBuffer =>
                        {
                            var segment = managedBuffer.Value;
                            if (!ReceivedDataProcessor.Instance.Process(segment) && segment.Array != null)
                                AppLogger.Debug("Echo: " + segment.Show());
                            managedBuffer.Dispose();
                        },
                        error =>
                        {
                            AppLogger.Error("Error: " + error.Message);
                            DisConnect();
                        },
                        () =>
                        {
                            AppLogger.Info("OnCompleted: Frame Protocol Receiver");
                            DisConnect();
                        });

            var cmdSender = new DevCommandSender(frameClientSubject, bufferManager);
            ReceivedDataProcessor.Instance.Sender = cmdSender;
            return cmdSender;
        }

        public static void DisConnect()
        {
            CommitEnableFiler(false);
            cts?.Cancel();
            cts = null;
            observerDisposable?.Dispose();
            observerDisposable = null;
            _dataStream?.OnCompleted();
            _dataStream = null;
            _stateStream?.OnCompleted();
            _stateStream = null;
            AppLogger.Debug("BrainDeviceManager.DisConnect");
        }
    }
}