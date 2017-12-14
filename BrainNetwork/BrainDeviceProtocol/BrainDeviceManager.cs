using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BrainCommon;
using BrainNetwork.RxSocket.Common;
using BrainNetwork.RxSocket.Protocol;

namespace BrainNetwork.BrainDeviceProtocol
{
    public static class BrainDeviceManager
    {
        private static BufferManager bufferManager;
        private static ClientFrameEncoder encoder;
        private static ClientFrameDecoder decoder;
        private static IDisposable observerDisposable;
        private static CancellationTokenSource cts;

        public static void Init()
        {
            bufferManager = BufferManager.CreateBufferManager(2 << 16, 1024);
            encoder = new ClientFrameEncoder(0xA0, 0XC0);
            decoder = new ClientFrameDecoder(0xA0, 0XC0);
        }

        public static async Task<BrainNetwork.BrainDeviceProtocol.DevCommandSender> Connnect(string ip, int port)
        {
            var endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(endPoint);
            cts = new CancellationTokenSource();
            
            var frameClientSubject =
                socket.ToFrameClientSubject(encoder, decoder, bufferManager, cts.Token);

            observerDisposable =
                frameClientSubject
                    .ObserveOn(TaskPoolScheduler.Default)
                    .Subscribe(
                        managedBuffer =>
                        {
                            var segment = managedBuffer.Value;
                            if (!ReceivedDataProcessor.Instance.Process(segment) && segment.Array != null)
                                AppLogger.Debug(
                                    "Echo: " + Encoding.UTF8.GetString(segment.Array, segment.Offset,
                                        segment.Count));
                            managedBuffer.Dispose();
                        },
                        error =>
                        {
                            AppLogger.Error("Error: " + error.Message);
                            cts.Cancel();
                        },
                        () =>
                        {
                            AppLogger.Info("OnCompleted: Frame Protocol Receiver");
                            cts.Cancel();
                        });

            var cmdSender = new BrainNetwork.BrainDeviceProtocol.DevCommandSender(frameClientSubject, bufferManager);
            ReceivedDataProcessor.Instance.Sender = cmdSender;
            return cmdSender;
        }

        public static void DisConnect()
        {
            cts.Cancel();
            cts = null;
            observerDisposable.Dispose();
            observerDisposable = null;
        }
    }
}