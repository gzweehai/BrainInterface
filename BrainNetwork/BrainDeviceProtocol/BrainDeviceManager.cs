using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security.Cryptography;
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
        private static SyncBufManager bufferManager;
        private static ClientFrameEncoder encoder;
        private static FixedLenFrameDecoder decoder;
        private static IDisposable observerDisposable;
        private static CancellationTokenSource cts;
        private static MD5 _hasher;
        
        public static SyncBufManager BufMgr => bufferManager;
        
        public static void Init()
        {
            bufferManager = SyncBufManager.Create(2 << 16, 128,32);
            encoder = new ClientFrameEncoder(0xA0, 0XC0);
            decoder = new FixedLenFrameDecoder(0xA0, 0XC0);
            _dataStream = new Subject<(byte, ArraySegment<int>, ArraySegment<byte>)>();
            _stateStream = new Subject<BrainDevState>();
            _stateStream.OnNext(_devState);
            _hasher = MD5.Create();
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
            _hasher.Clear();
            AppLogger.Debug("BrainDeviceManager.DisConnect");
        }

        public static void HashBlock(byte[] buf, int count)
        {
            _hasher.TransformBlock(buf, 0, count, null, 0);
        }

        private static readonly byte[] Emptyblock = new byte[0];
        public static byte[] CloseHash()
        {
            _hasher.TransformFinalBlock(Emptyblock, 0, 0);
            var result = _hasher.Hash;
            _hasher.Clear();
            return result;
        }
    }
}