using System;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;
using BrainCommon;
using BrainNetwork.RxSocket.Common;


namespace BrainSimulator
{
    internal static class ServerStub
    {
        private static Random _r= new Random();
        private static BufferManager bmgr = BufferManager.CreateBufferManager(64, 1024);
        private static ArraySegment<byte> _frameHeader=new ArraySegment<byte>(new byte[]{0xA0});
        private static ArraySegment<byte> _frameTail=new ArraySegment<byte>(new byte[]{0XC0});

        public static void Main(string[] args)
        {
            var endpoint = ProgramArgs.Parse(args, new[] { "127.0.0.1:9211" }).EndPoint;

            var cts = new CancellationTokenSource();

            endpoint.ToListenerObservable(10)
                .ObserveOn(TaskPoolScheduler.Default)
                .Subscribe(
                    client =>
                        client.ToClientObservable(1024, SocketFlags.None)
                            .Subscribe(async buf =>
                            {
                                await OnReceived(client, buf);
                            }, cts.Token),
                    error => Console.WriteLine("Error: " + error.Message),
                    () => Console.WriteLine("OnCompleted"),
                    cts.Token);

            Console.WriteLine("Press <ENTER> to quit");
            Console.ReadLine();

            cts.Cancel();
        }

        /*client.ToClientObserver(1024, SocketFlags.None)*/
        private static async Task OnReceived(Socket socket,ArraySegment<byte> buffer)
        {
            var buf = buffer.Array;
            var count = buffer.Count;
            if (count > 2 && buf != null && buf[0] == 0xA0 && buf[count-1] ==0xC0)
            {
                var funcId = buf[1];
                switch (funcId)
                {
                    case 1 :
                        await HandlerStartStop(socket,buffer);
                        return;
                    case 11 :
                        await SetSampleRate(socket,buffer);
                        return;
                    case 12 :
                        await SetTrap(socket,buffer);
                        return;
                    case 13 :
                        await SetFilter(socket,buffer);
                        return;
                    case 21 :
                        await QueryParam(socket,buffer);
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
                    await SendSampleData(socket);
                }
            }
        }
        
        private static async Task SendSampleData(Socket socket)
        {
            var buf = bmgr.TakeBuffer(2+3*3);
            _r.NextBytes(buf);
            buf[0] = 1;
            buf[1] = 0xff;
            buf[2] = 0;
            buf[2 + 3] = 1;
            buf[2 + 3 + 3] = 2;
            await SendWithHeadTail(socket,buf);
            bmgr.ReturnBuffer(buf);
        }

        private static async Task SetSampleRate(Socket socket, ArraySegment<byte> buffer)
        {
            var buf = bmgr.TakeBuffer(2);
            buf[1] = (byte) _r.Next();
            buf[0] = 11;
            await SendWithHeadTail(socket,buf);
            bmgr.ReturnBuffer(buf);
        }

        private static async Task SetTrap(Socket socket, ArraySegment<byte> buffer)
        {
            var buf = bmgr.TakeBuffer(2);
            buf[1] = (byte) _r.Next();
            buf[0] = 12;
            await SendWithHeadTail(socket,buf);
            bmgr.ReturnBuffer(buf);
        }

        private static async Task SetFilter(Socket socket, ArraySegment<byte> buffer)
        {
            var buf = bmgr.TakeBuffer(2);
            buf[1] = (byte) _r.Next();
            buf[0] = 13;
            await SendWithHeadTail(socket,buf);
            bmgr.ReturnBuffer(buf);
        }

        private static async Task QueryParam(Socket socket, ArraySegment<byte> buffer)
        {
            //TODO temp echo back
            await SimpleSend(socket, buffer);
        }

        private static async Task SendWithHeadTail(Socket socket,byte[] buf)
        {
            var size = buf.Length+2;
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
    }}
