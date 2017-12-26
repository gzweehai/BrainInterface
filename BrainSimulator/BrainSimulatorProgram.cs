using System;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;
using BrainCommon;
using BrainNetwork.BrainDeviceProtocol;
using BrainNetwork.RxSocket.Common;
using BrainNetwork.RxSocket.Protocol;

namespace BrainSimulator
{
    internal static class ServerStub
    {
        private static Random _r = new Random();
        private static BufferManager bmgr = BufferManager.CreateBufferManager(64, 1024);
        private static ArraySegment<byte> _frameHeader = new ArraySegment<byte>(new byte[] {0xA0});
        private static ArraySegment<byte> _frameTail = new ArraySegment<byte>(new byte[] {0XC0});

        public static void Main(string[] args)
        {
            var endpoint = ProgramArgs.Parse(args, new[] {"127.0.0.1:9211"}).EndPoint;
            var bufferManager = BufferManager.CreateBufferManager(2 << 16, 1024);
            var decoder = new DynamicFrameDecoder(0xA0, 0XC0);

            var cts = new CancellationTokenSource();

            endpoint.ToListenerObservable(10)
                .ObserveOn(TaskPoolScheduler.Default)
                .Subscribe(
                    client =>
                        //client.ToClientObservable(1024, SocketFlags.None)
                            client.ToDynamicFrameObservable(bufferManager, decoder)
                                .Subscribe(async buf => await OnReceived(client, buf, cts.Token),
                                    SocketReceiveError,
                                    ClientSocketClose, cts.Token),
                    error => Console.WriteLine("Error: " + error.Message),
                    () => Console.WriteLine("OnCompleted"),
                    cts.Token);

            Console.WriteLine("Press <ENTER> to quit");
            Console.ReadLine();

            cts.Cancel();
        }

        private static void SocketReceiveError(Exception err)
        {
            _brainState.Reset();
            Console.WriteLine("socket receive error:" + err.Message);
        }

        private static void ClientSocketClose()
        {
            _brainState.Reset();
            Console.WriteLine("client socket closed");
        }

        private struct BrainState //naive server, no thread synchonization
        {
            public bool IsStart;
            public SampleRateEnum SampleRate;
            public TrapSettingEnum TrapOption;
            public bool EnableFilter;
            public byte SamplePacketOrder;

            public BrainState(bool isStart)
            {
                IsStart = isStart;
                SampleRate = SampleRateEnum.SPS_250;
                TrapOption = TrapSettingEnum.NoTrap;
                EnableFilter = false;
                SamplePacketOrder = 0;
            }

            public void Reset()
            {
                IsStart = false;
                SamplePacketOrder = 0;
            }
        }

        private static BrainState _brainState = new BrainState(false);

        private static async Task OnReceived(Socket socket, DisposableValue<ArraySegment<byte>> arrSeg,
            CancellationToken ctsToken)
        {
            var buffer = arrSeg.Value;
            var buf = buffer.Array;
            var count = buffer.Count;
            if (count > 0 && buf != null)
            {
                var funcId = buf[buffer.Offset];
                AppLogger.Debug($"func:{funcId},{buffer.Show()}");
                switch (funcId)
                {
                    case 1:
                        HandlerStartStop(socket, buffer, ctsToken);
                        return;
                    case 11:
                        await SetSampleRate(socket, buffer, ctsToken);
                        return;
                    case 12:
                        await SetTrap(socket, buffer, ctsToken);
                        return;
                    case 13:
                        await SetFilter(socket, buffer, ctsToken);
                        return;
                    case 21:
                        await QueryParam(socket, buffer, ctsToken);
                        return;
                }
            }
            await SimpleSend(socket, buffer);
        }

        private static void HandlerStartStop(Socket socket, ArraySegment<byte> buffer, CancellationToken ctsToken)
        {
            if (buffer.Array != null)
            {
                var startInd = buffer.Offset;
                var flag = buffer.Array[startInd + 1];
                if (flag != 0)
                {
                    if (!_brainState.IsStart)
                    {
                        _brainState.IsStart = true;
                        //await SendSampleData(socket);//collect data then sent, not sent immediately
                        StartPeridSender(socket, ctsToken);
                    }
                }
                else
                {
                    _brainState.Reset();
                }
            }
        }

        private static async void StartPeridSender(Socket socket, CancellationToken ctsToken)
        {
            var sampleTimeTick = 0;
            while (true)
            {
                await Task.Delay(20);
                if (!_brainState.IsStart) return;

                var rate = _brainState.SampleRate;
                var count = BrainDevState.SampleCountPer20ms(rate);

                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        await SendSampleData(sampleTimeTick,rate,socket, ctsToken);
                        sampleTimeTick++;
                        if (!_brainState.IsStart) return;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return; //any exception will break the sending queues
                }
            }
        }

        const byte ChannelCount = 32; //must >= 3

        private static async Task SendSampleData(int sampleTimeTick, SampleRateEnum rate, Socket socket, CancellationToken ctsToken)
        {
            byte size = 2 + ChannelCount * 3;
            var buf = bmgr.TakeBuffer(size);
            _r.NextBytes(buf);
            var passTimes=BrainDevState.PassTimeMs(rate, sampleTimeTick);
            const float max = 4.5f / 72;
            var sampleValue= Math.Sin(passTimes * 2 /1000f * Math.PI)*max;
            var (b0, b1, b2) = BitDataConverter.ConvertTo(sampleValue);
            buf[0] = 1;
            buf[1] = _brainState.SamplePacketOrder++;
            buf[2] = b0;
            buf[2 + 1] = b1;
            buf[2 + 2] = b2;
            await SendWithHeadTail(socket, buf, size,ctsToken);
            bmgr.ReturnBuffer(buf);
        }

        private static async Task SetSampleRate(Socket socket, ArraySegment<byte> buffer, CancellationToken ctsToken)
        {
            var buf = bmgr.TakeBuffer(2);
            buf[0] = 11;
            if (buffer.Array != null)
            {
                var flag = (SampleRateEnum) buffer.Array[buffer.Offset + 1];
                switch (flag)
                {
                    case SampleRateEnum.SPS_250:
                    case SampleRateEnum.SPS_500:
                    case SampleRateEnum.SPS_1k:
                    case SampleRateEnum.SPS_2k:
                        _brainState.Reset();
                        _brainState.SampleRate = flag;
                        buf[1] = 0;
                        break;
                    default:
                        buf[1] = 1;
                        break;
                }
            }
            await SendWithHeadTail(socket, buf, 2,ctsToken);
            bmgr.ReturnBuffer(buf);
        }

        private static async Task SetTrap(Socket socket, ArraySegment<byte> buffer, CancellationToken ctsToken)
        {
            var buf = bmgr.TakeBuffer(2);
            buf[1] = 1;
            buf[0] = 12;
            await SendWithHeadTail(socket, buf, 2,ctsToken);
            bmgr.ReturnBuffer(buf);
        }

        private static async Task SetFilter(Socket socket, ArraySegment<byte> buffer, CancellationToken ctsToken)
        {
            var buf = bmgr.TakeBuffer(2);
            buf[1] = 1;
            buf[0] = 13;
            await SendWithHeadTail(socket, buf, 2,ctsToken);
            bmgr.ReturnBuffer(buf);
        }

        private const byte DevCode = 0xFF;

        private static async Task QueryParam(Socket socket, ArraySegment<byte> buffer, CancellationToken ctsToken)
        {
            var buf = bmgr.TakeBuffer(7);
            buf[0] = 21;
            buf[1] = DevCode;
            buf[2] = ChannelCount;
            buf[3] = 72;
            buf[4] = (byte) _brainState.SampleRate;
            buf[5] = (byte) _brainState.TrapOption;
            buf[6] = _brainState.EnableFilter ? (byte) 1 : (byte) 0;

            await SendWithHeadTail(socket, buf, 7,ctsToken);
            bmgr.ReturnBuffer(buf);
        }

        private static async Task SendWithHeadTail(Socket socket, byte[] buf, byte bufSize, CancellationToken ctsToken)
        {
            var size = buf.Length + 3;
            var lenByte = new ArraySegment<byte>(new[] {bufSize});
            var sent = await socket.SendCompletelyAsync(new[]
            {
                _frameHeader,
                lenByte,
                new ArraySegment<byte>(buf,0,bufSize),
                _frameTail,
            }, SocketFlags.None, ctsToken);
            //AppLogger.Debug($"sent,{sent},len:{buf.Length},{buf.Show()}");
        }

        private static async Task SimpleSend(Socket socket, ArraySegment<byte> buffer)
        {
            var buf = buffer.Array;
            var count = buffer.Count;
            var sent = 0;
            while (sent < count)
            {
                sent += await socket.SendAsync(buf, sent, count - sent, SocketFlags.None);
            }
            AppLogger.Debug($"SimpleSend:{sent},len:{buf.Length},{buf.Show()}");
        }
    }
}