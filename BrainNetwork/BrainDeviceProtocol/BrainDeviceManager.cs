using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
        private static Socket socket;

        public static SyncBufManager BufMgr => bufferManager;

        static BrainDeviceManager()
        {
            bufferManager = SyncBufManager.Create(2 << 16, 128, 32);
            encoder = new ClientFrameEncoder(0xA0, 0XC0);
            decoder = new FixedLenFrameDecoder(0xA0, 0XC0);
            _dataStream = new Subject<(byte, ArraySegment<int>, ArraySegment<byte>)>();
            _stateStream = new Subject<BrainDevState>();
            AppDomain.CurrentDomain.ProcessExit += ProcessExit;
        }

        public static void Init()
        {
            //_stateStream.OnNext(_devState);
        }

        private static void ProcessExit(object sender, EventArgs e)
        {
            DisConnect();
        }

        //TODO use rx instead!
        public static event Action OnConnected;
        
        public static async Task<DevCommandSender> Connnect(string ip, int port)
        {
            DisConnect();
            var endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
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
                                AppLogger.Warning($"Not Process data: {segment.Show()}");
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

            var cmdSender = new DevCommandSender(frameClientSubject, bufferManager,ClientConfig.ChangingConfig);
            ReceivedDataProcessor.Instance.Sender = cmdSender;
            OnConnected?.Invoke();
            return cmdSender;
        }

        public static void DisConnect()
        {
            var tmpCts=Interlocked.Exchange(ref cts, null);
            if (tmpCts == null) return;
            CommitStartStop(false);
            tmpCts.Cancel();
            var tmpObs=Interlocked.Exchange(ref observerDisposable, null);
            tmpObs?.Dispose();
            var tmpDataS=Interlocked.Exchange(ref _dataStream, null);
            tmpDataS?.OnCompleted();
            var tmpStateS=Interlocked.Exchange(ref _stateStream, null);
            tmpStateS?.OnCompleted();
            bufferManager.Clear();
            var tmp = Interlocked.Exchange(ref socket, null);
            try
            {
                tmp?.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            AppLogger.Debug("BrainDeviceManager.DisConnect");
        }
    }
}