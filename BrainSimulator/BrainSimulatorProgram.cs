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


namespace BrainSimulator
{
    internal static class ServerStub
    {
        private const int SampleCount_250 = 20 * 250 / 1000;
        private const int SampleCount_500 = 20 * 500 / 1000;
        private const int SampleCount_1k = 20 * 1000 / 1000;
        private const int SampleCount_2k = 20 * 2000 / 1000;
        private static Random _r = new Random();
        private static BufferManager bmgr = BufferManager.CreateBufferManager(64, 1024);
        private static ArraySegment<byte> _frameHeader = new ArraySegment<byte>(new byte[] {0xA0});
        private static ArraySegment<byte> _frameTail = new ArraySegment<byte>(new byte[] {0XC0});

        public static void Main(string[] args)
        {
            var endpoint = ProgramArgs.Parse(args, new[] {"127.0.0.1:9211"}).EndPoint;

            var cts = new CancellationTokenSource();

            endpoint.ToListenerObservable(10)
                .ObserveOn(TaskPoolScheduler.Default)
                .Subscribe(
                    client =>
                        client.ToClientObservable(1024, SocketFlags.None)
                            .Subscribe(async buf => { await OnReceived(client, buf); },
                            err=>Console.WriteLine("socket receive error"+err.Message),
                            ()=> { }, cts.Token),
                    error => Console.WriteLine("Error: " + error.Message),
                    () => Console.WriteLine("OnCompleted"),
                    cts.Token);

            Console.WriteLine("Press <ENTER> to quit");
            Console.ReadLine();

            cts.Cancel();
        }

        private struct BrainState//naive server, no thread synchonization
        {
            public bool IsStart;
            public SampleRateEnum SampleRate;
            public TrapSettingEnum TrapOption;
            public bool EnableFilter;
            public byte SamplePacketOrder;
        }

        private static BrainState _brainState;

        private static async Task OnReceived(Socket socket, ArraySegment<byte> buffer)
        {
            var buf = buffer.Array;
            var count = buffer.Count;
            if (count > 2 && buf != null && buf[0] == 0xA0 && buf[count - 1] == 0xC0)
            {
                var funcId = buf[1];
                switch (funcId)
                {
                    case 1:
                        await HandlerStartStop(socket, buffer);
                        return;
                    case 11:
                        await SetSampleRate(socket, buffer);
                        return;
                    case 12:
                        await SetTrap(socket, buffer);
                        return;
                    case 13:
                        await SetFilter(socket, buffer);
                        return;
                    case 21:
                        await QueryParam(socket, buffer);
                        return;
                }
            }
            await SimpleSend(socket, buffer);
        }

        private static async Task HandlerStartStop(Socket socket, ArraySegment<byte> buffer)
        {
            if (buffer.Array != null)
            {
                var flag = buffer.Array[2];
                if (flag != 0)
                {
                    if (!_brainState.IsStart)
                    {
                        _brainState.IsStart = true;
                        //await SendSampleData(socket);//collect data then sent, not sent immediately
                        StartPeridSender(socket);
                    }
                }
                else
                {
                    _brainState.IsStart = false;
                }
            }
        }

        private static async void StartPeridSender(Socket socket)
        {
            while (true)
            {
                await Task.Delay(20);
                if (!_brainState.IsStart) return;

                var count = 1;
                switch (_brainState.SampleRate)
                {
                    case SampleRateEnum.SPS_250: //every 1000ms sample 250 times
                        count = SampleCount_250; //20ms -> sample counts
                        break;
                    case SampleRateEnum.SPS_500:
                        count = SampleCount_500; //20ms -> sample counts
                        break;
                    case SampleRateEnum.SPS_1k:
                        count = SampleCount_1k; //20ms -> sample counts
                        break;
                    case SampleRateEnum.SPS_2k:
                        count = SampleCount_2k; //20ms -> sample counts
                        break;
                }

                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        await SendSampleData(socket);
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return;//any exception will break the sending queues
                }
            }
        }

        const byte ChannelCount = 32;//must >= 3
        private static async Task SendSampleData(Socket socket)
        {
            var buf = bmgr.TakeBuffer(2 + ChannelCount * 3);
            _r.NextBytes(buf);
            buf[0] = 1;
            buf[1] = ++_brainState.SamplePacketOrder;
            buf[2] = 0;
            buf[2 + 3] = 1;
            buf[2 + 3 + 3] = 2;
            await SendWithHeadTail(socket, buf);
            bmgr.ReturnBuffer(buf);
        }

        private static async Task SetSampleRate(Socket socket, ArraySegment<byte> buffer)
        {
            var buf = bmgr.TakeBuffer(2);
            buf[0] = 11;
            if (buffer.Array != null)
            {
                var flag = (SampleRateEnum)buffer.Array[2];
                switch (flag)
                {
                    case SampleRateEnum.SPS_250:
                    case SampleRateEnum.SPS_500:
                    case SampleRateEnum.SPS_1k:
                    case SampleRateEnum.SPS_2k:
                        _brainState.IsStart = false;
                        _brainState.SampleRate = flag;
                        buf[1] = 0;
                        break;
                    default:
                        buf[1] = 1;
                        break;
                }
            }
            await SendWithHeadTail(socket, buf);
            bmgr.ReturnBuffer(buf);
        }

        private static async Task SetTrap(Socket socket, ArraySegment<byte> buffer)
        {
            var buf = bmgr.TakeBuffer(2);
            buf[1] = 1;
            buf[0] = 12;
            await SendWithHeadTail(socket, buf);
            bmgr.ReturnBuffer(buf);
        }

        private static async Task SetFilter(Socket socket, ArraySegment<byte> buffer)
        {
            var buf = bmgr.TakeBuffer(2);
            buf[1] = 1;
            buf[0] = 13;
            await SendWithHeadTail(socket, buf);
            bmgr.ReturnBuffer(buf);
        }

        private const byte DevCode = 0xFF;
        private static async Task QueryParam(Socket socket, ArraySegment<byte> buffer)
        {
            var buf = bmgr.TakeBuffer(6);
            buf[0] = 21;
            buf[1] = DevCode;
            buf[2] = ChannelCount;
            buf[3] = (byte)_brainState.SampleRate;
            buf[4] = (byte)_brainState.TrapOption;
            buf[5] = _brainState.EnableFilter ? (byte) 1 : (byte) 0;
            
            await SendWithHeadTail(socket, buf);
            bmgr.ReturnBuffer(buf);
        }

        private static async Task SendWithHeadTail(Socket socket, byte[] buf)
        {
            var size = buf.Length + 2;
            var sent = 0;
            while (sent < size)
            {
                var bytes = await socket.SendAsync(new[]
                {
                    _frameHeader,
                    new ArraySegment<byte>(buf),
                    _frameTail,
                }, SocketFlags.None);
                if (bytes == 0)
                    break;
                sent += bytes;
            }
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
        }
    }
}