using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BrainCommon;
using BrainNetwork.BrainDeviceProtocol;
using BrainNetwork.RxSocket.Common;
using BrainNetwork.RxSocket.Protocol;

namespace BrainProtocolTester
{
    internal static class BrainDevProtocolTestProgram
    {
        static void Main(string[] args)
        {
            Console.WriteLine(BitDataConverter.ConvertFrom(0x7f, 0xff, 0xff));
            Console.WriteLine(BitDataConverter.ConvertFrom(0x00, 0x00, 0x01));
            Console.WriteLine(BitDataConverter.ConvertFrom(0x00, 0x00, 0x00));
            Console.WriteLine(BitDataConverter.ConvertFrom(0xff, 0xff, 0xff));
            Console.WriteLine(BitDataConverter.ConvertFrom(0x80, 0x00, 0x01));
            Console.ReadLine();
            
            //OldTest(args);
            try
            {
                TestBrainDeviceManager();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static async void TestBrainDeviceManager()
        {
            BrainDeviceManager.Init();
            var sender = await BrainDeviceManager.Connnect("127.0.0.1", 9211);
            var cmdResult = await sender.QueryParam();
            AppLogger.Debug("QueryParam result:"+cmdResult);
            
            cmdResult = await sender.SetFilter(true);
            AppLogger.Debug("SetFilter result:"+cmdResult);
            
            cmdResult = await sender.SetTrap(TrapSettingEnum.Trap_50);
            AppLogger.Debug("SetTrap result:"+cmdResult);
            
            cmdResult = await sender.SetSampleRate(SampleRateEnum.SPS_250);
            AppLogger.Debug("SetSampleRate result:"+cmdResult);
            
            cmdResult = await sender.QueryParam();
            AppLogger.Debug("QueryParam result:"+cmdResult);
            
            cmdResult = await sender.Start();
            if (cmdResult != CommandError.Success)
            {
                AppLogger.Error("Failed to start sampler");
            }
            else
            {
                await Task.Delay(1000);
                await sender.Stop();
            }
            
            BrainDeviceManager.DisConnect();
            Console.ReadLine();
        }
        
        private static void OldTest(string[] args)
        {
            var endpoint = ProgramArgs.Parse(args, new[] {"127.0.0.1:9211"}).EndPoint;

            var cts = new CancellationTokenSource();
            var bufferManager = BufferManager.CreateBufferManager(2 << 16, 1024);
            var encoder = new ClientFrameEncoder(0xA0, 0XC0);
            var decoder = new ClientFrameDecoder(0xA0, 0XC0);

            endpoint.ToConnectObservable()
                .ObserveOn(TaskPoolScheduler.Default)
                .Subscribe(socket =>
                    {
                        var frameClientSubject =
                            socket.ToFrameClientSubject(encoder, decoder, bufferManager, cts.Token);

                        var observerDisposable =
                            frameClientSubject
                                .ObserveOn(TaskPoolScheduler.Default)
                                .Subscribe(
                                    managedBuffer =>
                                    {
                                        var segment = managedBuffer.Value;
                                        if (!ReceivedDataProcessor.Instance.Process(segment) && segment.Array != null)
                                            Console.WriteLine(
                                                "Echo: " + Encoding.UTF8.GetString(segment.Array, segment.Offset,
                                                    segment.Count));
                                        managedBuffer.Dispose();
                                    },
                                    error =>
                                    {
                                        Console.WriteLine("Error: " + error.Message);
                                        cts.Cancel();
                                    },
                                    () =>
                                    {
                                        Console.WriteLine("OnCompleted: Frame Protocol Receiver");
                                        cts.Cancel();
                                    });

                        var cmdSender = new DevCommandSender(frameClientSubject, bufferManager);

                        Console.In.ToLineObservable("exit")
                            .Subscribe(
                                line =>
                                {
                                    if (string.IsNullOrEmpty(line)) return;
                                    if (line == "start")
                                    {
                                        cmdSender.Start();
                                        return;
                                    }
                                    if (line == "stop")
                                    {
                                        cmdSender.Stop();
                                        return;
                                    }
                                    var writeBuffer = Encoding.UTF8.GetBytes(line);
                                    frameClientSubject.OnNext(
                                        DisposableValue.Create(new ArraySegment<byte>(writeBuffer), Disposable.Empty));
                                },
                                error =>
                                {
                                    Console.WriteLine("Error: " + error.Message);
                                    cts.Cancel();
                                },
                                () =>
                                {
                                    Console.WriteLine("OnCompleted: LineReader");
                                    cts.Cancel();
                                });

                        cts.Token.WaitHandle.WaitOne();
                        observerDisposable.Dispose();
                    },
                    error =>
                    {
                        Console.WriteLine("Failed to connect: " + error.Message);
                        cts.Cancel();
                    },
                    cts.Token);

            cts.Token.WaitHandle.WaitOne();
        }
    }
}
