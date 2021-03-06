﻿using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BrainCommon;
using BrainNetwork.BrainDeviceProtocol;
using BrainNetwork.RxSocket.Common;
using BrainNetwork.RxSocket.Protocol;
using DataAccess;
using MathNet.Filtering;
using MathNet.Filtering.FIR;
using MathNet.Filtering.Median;
using WaveletStudio;
using WaveletStudio.Functions;
using FilterType = BrainCommon.FilterType;

namespace BrainProtocolTester
{
    internal static class BrainDevProtocolTestProgram
    {
        static async Task Main(string[] args)
        {
            /*
            BitDataConverter.TestByteOrder();
            BitDataConverter.TestConvertPerformance();
            Console.WriteLine(BitDataConverter.ConvertFrom(0x7f, 0xff, 0xff, 4.5f, 1));
            Console.WriteLine(BitDataConverter.ConvertTo(BitDataConverter.ConvertFrom(0x7f, 0xff, 0xff)));
            Console.WriteLine(BitDataConverter.ConvertFrom(0x00, 0x00, 0x01, 4.5f, 1));
            Console.WriteLine(BitDataConverter.ConvertTo(BitDataConverter.ConvertFrom(0x00, 0x00, 0x01)));
            Console.WriteLine(BitDataConverter.ConvertFrom(0x00, 0x00, 0x00, 4.5f, 1));
            Console.WriteLine(BitDataConverter.ConvertTo(BitDataConverter.ConvertFrom(0x00, 0x00, 0x00)));
            Console.WriteLine(BitDataConverter.ConvertFrom(0xff, 0xff, 0xff, 4.5f, 1));
            Console.WriteLine(BitDataConverter.ConvertTo(BitDataConverter.ConvertFrom(0xff, 0xff, 0xff)));
            Console.WriteLine(BitDataConverter.ConvertFrom(0x80, 0x00, 0x01, 4.5f, 1));
            Console.WriteLine(BitDataConverter.ConvertTo(BitDataConverter.ConvertFrom(0x80, 0x00, 0x01)));
            Console.WriteLine(BitDataConverter.ConvertFrom(0x80, 0x00, 0x00, 4.5f, 1));
            Console.WriteLine(BitDataConverter.ConvertTo(BitDataConverter.ConvertFrom(0x80, 0x00, 0x00)));
            Console.ReadLine();
            */
            
            //OldTest(args);
            
            try
            {
                await TestBrainDeviceManager();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.ReadLine();
        }

        private static async Task TestBrainDeviceManager()
        {
            var cfg = ClientConfig.GetConfig();
            var lowFilter = new LowPassFilter() {LowPassRate = 10};
            var highFilter = new HighPassFilter() {HighPassRate = 10000};
            var bPassFilter = new BandPassFilter() {LowCutoffRate = 20, HighCutoffRate = 30};
            var bStopFilter = new BandStopFilter() {LowPassRate = 35, HighPassRate = 38};
            var mFilter = new MedianFilter() {HalfMedianWindowSize = 7};
            var bandFilter = new BandPassStopFilter
            {
                BandFilterList = new List<BandFilter> {lowFilter, highFilter, bPassFilter, bStopFilter}
            };
            bandFilter.Disable = true;
            var allFilter = new FilterTypeList() {Filters = new List<FilterType> {bandFilter,mFilter}};
            var filterParameters = new Dictionary<string, string>
            {
                {FilterTypeList.FIRhalfOrderOptionName, 10.ToString()}
            };
            cfg.FilterLst = allFilter;
            IOnlineFilter filter = null;
            WaveletReconstruction waveletRec = null;

            cfg.WaveletRecCfg = new WaveletReconstructionConfig
            {
                AvgLevel = 8,
                ConvolutionMode = ConvolutionModeEnum.Normal,
                ExtensionMode = SignalExtension.ExtensionMode.SymmetricHalfPoint,
                Level = 8,
                MotherWaveletName = "db5",
                WindowSize = 15
            };
            cfg.WriteToFile(ClientConfig.DefaultConfigFileName);
            
            
            BrainDeviceManager.Init();
            var sender = await BrainDeviceManager.Connnect("127.0.0.1", 9211);
            BrainDevState currentState = default(BrainDevState);
            //保证设备参数正常才继续跑逻辑
            BrainDeviceManager.BrainDeviceState.Subscribe(ss =>
            {
                currentState = ss;
                filterParameters.Update(
                    FilterTypeList.SampleRateOptionName,
                    BrainDevState.SampleCountPer1Sec(ss.SampleRate).ToString()
                    );
                //AppLogger.Debug($"Brain Device State Changed Detected: {ss}");
            }, () =>
            {
                AppLogger.Debug("device stop detected");
            });
            int totalReceived = 0;
            var medianFilter = OnlineFirFilter.CreateDenoise(7);
            var fastMedianFilter = new OnlineFastMedianFilter(3);
            
            BrainDeviceManager.SampleDataStream.Subscribe(tuple =>
            {
                var (order, datas, arr) = tuple;
                //Console.Write($" {order} ");
                var passTimes=BrainDevState.PassTimeMs(currentState.SampleRate, totalReceived)/1000;
                var intArr = datas.CopyToArray();
                var val = intArr[0];
                var voltage = BitDataConverter.Calculatevoltage(val,4.5f, currentState.Gain);
                totalReceived++;
                var m1=medianFilter.ProcessSample(voltage);
                var m2 = fastMedianFilter.ProcessSample(voltage);
                AppLogger.Debug($"passTimes:{passTimes},val:{val},voltage:{voltage},median filter:{m1},fast:{m2},{m1==m2}");
                if (filter == null)
                {
                    filter=allFilter.CreateFilter(filterParameters);
                    waveletRec = cfg.WaveletRecCfg.Create(
                        BrainDevState.SampleCountPer1Sec(currentState.SampleRate));
                    waveletRec.ReconstructionStream.Subscribe(reconstruct =>
                    {
                        var (vol, tim) = reconstruct;
                        AppLogger.Debug($"wavelet reconstruction:{vol} at {tim}");
                    });
                }
                waveletRec?.BufferData((voltage,passTimes));
                AppLogger.Debug($"filter processed:{filter.ProcessSample(voltage)}");
                //AppLogger.Debug($"order:{order}");
                //AppLogger.Debug($"converted values:{datas.Show()}");
                //AppLogger.Debug($"original datas:{arr.Show()}");
            }, () =>
            {
                AppLogger.Debug("device sampling stream closed detected");
            });
            
            var cmdResult = await sender.QueryParam();
            AppLogger.Debug("QueryParam result:"+cmdResult);
            if (cmdResult != CommandError.Success)
            {
                AppLogger.Error("Failed to QueryParam, stop");
                BrainDeviceManager.DisConnect();
                return;
            }
            
            
            cmdResult = await sender.SetFilter(true);
            AppLogger.Debug("SetFilter result:"+cmdResult);
            
            cmdResult = await sender.SetTrap(TrapSettingEnum.Trap_50);
            AppLogger.Debug("SetTrap result:"+cmdResult);
            
            cmdResult = await sender.SetSampleRate(SampleRateEnum.SPS_2k);
            AppLogger.Debug("SetSampleRate result:"+cmdResult);
            
            cmdResult = await sender.QueryParam();
            AppLogger.Debug("QueryParam result:"+cmdResult);

            cmdResult = await sender.QueryFaultState();
            AppLogger.Debug("QueryFaultState result:"+cmdResult);
            
            cmdResult = await sender.TestSingleImpedance(1);
            AppLogger.Debug("TestSingleImpedance result:"+cmdResult);
            
            cmdResult = await sender.TestMultiImpedance(30);
            AppLogger.Debug("TestMultiImpedance result:"+cmdResult);

            Console.ReadLine();
            var fs = new FileResource(currentState, 19801983, 1, BrainDeviceManager.BufMgr);
            fs.StartRecord(BrainDeviceManager.SampleDataStream);
            cmdResult = await sender.Start();
            if (cmdResult != CommandError.Success)
            {
                AppLogger.Error("Failed to start sampler");
            }
            else
            {
                AppLogger.Debug($"start receive sample data");
                await Task.Delay(1000*10);
                AppLogger.Debug($"stoping");
                await sender.Stop();
                AppLogger.Debug($"stop receive sample data");
                await Task.Delay(1000);
            }
            
            BrainDeviceManager.DisConnect();
            fs.Dispose();
            
            var readf = new FileSampleData(fs.ResourceId,BrainDeviceManager.BufMgr);
            Console.WriteLine($"expecte to read {totalReceived} blocks");
            Console.WriteLine($"start reading saved sampling data");
            int readCount = 0;
            readf.DataStream.Subscribe(tuple =>
            {
                var (order, datas, arr) = tuple;
                Console.Write($" {order} ");
                readCount++;
                //AppLogger.Debug($"order:{order}");
                //AppLogger.Debug($"converted values:{datas.Show()}");
                //AppLogger.Debug($"original datas:{arr.Show()}");
            }, () =>
            {
                AppLogger.Debug($"read sampling data file end,count :{readCount},expected count:{totalReceived}");
            });
            readf.Start();
            await Task.Delay(1000*10);
            Console.Write($"wait complete");            
        }
        
        private static void OldTest(string[] args)
        {
            var endpoint = ProgramArgs.Parse(args, new[] {"127.0.0.1:9211"}).EndPoint;

            var cts = new CancellationTokenSource();
            var bufferManager = SyncBufManager.Create(2 << 16, 128,32);
            var encoder = new ClientFrameEncoder(0xA0, 0XC0);
            var decoder = new FixedLenFrameDecoder(0xA0, 0XC0);

            endpoint.ToConnectObservable()
                .ObserveOn(TaskPoolScheduler.Default)
                .Subscribe(socket =>
                    {
                        var frameClientSubject =
                            socket.ToFixedLenFrameSubject(encoder, decoder, bufferManager, cts.Token);

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

                        var cmdSender = new DevCommandSender(frameClientSubject, bufferManager, null);

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
