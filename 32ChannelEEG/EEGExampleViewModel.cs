using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BrainCommon;
using BrainNetwork.BrainDeviceProtocol;
using DataAccess;
using SciChart.Charting.Common.Helpers;
using SciChart.Core.Helpers;
using SciChart.Core.Utility;
using SciChart.Examples.ExternalDependencies.Common;

namespace SciChart.Examples.Examples.CreateRealtimeChart.EEGChannelsDemo
{
    public class EEGExampleViewModel : BaseViewModel
    {
        private ObservableCollection<EEGChannelViewModel> _channelViewModels;

        private readonly IList<Color> _colors = new[]
        {
            Colors.White, Colors.Yellow, Color.FromArgb(255, 0, 128, 128), Color.FromArgb(255, 176, 196, 222),
            Color.FromArgb(255, 255, 182, 193), Colors.Purple, Color.FromArgb(255, 245, 222, 179),
            Color.FromArgb(255, 173, 216, 230),
            Color.FromArgb(255, 250, 128, 114), Color.FromArgb(255, 144, 238, 144), Colors.Orange,
            Color.FromArgb(255, 192, 192, 192),
            Color.FromArgb(255, 255, 99, 71), Color.FromArgb(255, 205, 133, 63), Color.FromArgb(255, 64, 224, 208),
            Color.FromArgb(255, 244, 164, 96)
        };

        private readonly FasterRandom _random = new FasterRandom();
        private int ChannelCount = 32; // Number of channels to render
        private int Size = 1000; // Size of each channel in points (FIFO Buffer)
        private int _size = 1000;
        private volatile int _currentSize = 0;
        private uint _timerInterval = 20; // Interval of the timer to generate data in ms        
        private int _bufferSize = 15; // Number of points to append to each channel each timer tick
        private Timer _timer;
        private object _syncRoot = new object();

        // X, Y buffers used to buffer data into the Scichart instances in blocks of BufferSize
        private double[] xBuffer;
        private double[] yBuffer;
        
        private bool _running;
        private bool _isReset;

        private readonly ActionCommand _startCommand;
        private readonly ActionCommand _stopCommand;
        private readonly ActionCommand _resetCommand;
        private TimedMethod _startDelegate;

        public ObservableCollection<EEGChannelViewModel> ChannelViewModels
        {
            get { return _channelViewModels; }
            set
            {
                _channelViewModels = value;
                OnPropertyChanged("ChannelViewModels");
            }
        }

        public ICommand StartCommand
        {
            get { return _startCommand; }
        }

        public ICommand StopCommand
        {
            get { return _stopCommand; }
        }

        public ICommand ResetCommand
        {
            get { return _resetCommand; }
        }

        public int PointCount
        {
            get { return _currentSize * ChannelCount; }
        }

        public double TimerInterval
        {
            get { return _timerInterval; }
            set
            {
                _timerInterval = (uint) value;
                OnPropertyChanged("TimerInterval");
                Stop();
            }
        }

        public double BufferSize
        {
            get { return _bufferSize; }
            set
            {
                _bufferSize = (int) value;
                OnPropertyChanged("BufferSize");
                Stop();
            }
        }

        public double TimerZoom
        {
            get { return _size; }
            set
            {
                _size = (int) value;
                OnPropertyChanged("TimerZoom");
                Stop();
            }
        }

        public bool IsReset
        {
            get { return _isReset; }
            set
            {
                _isReset = value;

                _startCommand.RaiseCanExecuteChanged();
                _stopCommand.RaiseCanExecuteChanged();
                _resetCommand.RaiseCanExecuteChanged();

                OnPropertyChanged("IsReset");
            }
        }

        public bool IsRunning
        {
            get { return _running; }
            set
            {
                _running = value;

                _startCommand.RaiseCanExecuteChanged();
                _stopCommand.RaiseCanExecuteChanged();
                _resetCommand.RaiseCanExecuteChanged();

                OnPropertyChanged("IsRunning");
            }
        }

        private void Start()
        {
            if (_channelViewModels == null || _channelViewModels.Count == 0 || Size != _size)
            {
                Reset();
            }

            if (!IsRunning)
            {
                IsRunning = true;
                IsReset = false;
                xBuffer = new double[_bufferSize];
                yBuffer = new double[_bufferSize];
                _timer = new Timer(_timerInterval);
                _timer.Elapsed += OnTick;
                _timer.AutoReset = true;
                _timer.Start();
            }
        }

        private void Stop()
        {
            if (IsRunning)
            {
                _timer.Stop();
                IsRunning = false;
            }
        }

        private void Reset()
        {
            Stop();
            Size = _size;
            // Initialize N EEGChannelViewModels. Each of these will be represented as a single channel
            // of the EEG on the view. One channel = one SciChartSurface instance
            ChannelViewModels = new ObservableCollection<EEGChannelViewModel>();
            for (int i = 0; i < ChannelCount; i++)
            {
                var channelViewModel = new EEGChannelViewModel(Size, _colors[i % 16],0) {ChannelName = "Channel " + i};
                ChannelViewModels.Add(channelViewModel);
            }

            IsReset = true;
        }

        private void OnTick(object sender, EventArgs e)
        {
            // Ensure only one timer Tick processed at a time
            lock (_syncRoot)
            {
                for (int i = 0; i < _channelViewModels.Count; i++)
                {
                    // Get the dataseries created for this channel
                    var channel = _channelViewModels[i];
                    var dataseries = channel.ChannelDataSeries;

                    // Preload previous value with k-1 sample, or 0.0 if the count is zero
                    double xValue = dataseries.Count > 0 ? dataseries.XValues[dataseries.Count - 1] : 0.0;

                    // Add points 10 at a time for efficiency   
                    for (int j = 0; j < BufferSize; j++)
                    {
                        // Generate a new X,Y value in the random walk
                        xValue = xValue + 1;
                        double yValue = _random.NextDouble();

                        xBuffer[j] = xValue;
                        yBuffer[j] = yValue;
                    }

                    // Append block of values
                    dataseries.Append(xBuffer, yBuffer);

                    // For reporting current size to GUI
                    _currentSize = dataseries.Count;
                }
            }
        }

        #region brain device

        private BrainDevState _currentState;
        private int _pakNum;
        private Dispatcher _uithread;
        private DevCommandSender _devCtl;

        public EEGExampleViewModel()
        {
            _startCommand = new ActionCommand(StartAsync, () => !IsRunning);
            _stopCommand = new ActionCommand(StopDevCmd, () => IsRunning);
            _resetCommand = new ActionCommand(ResetDevCmd, () => !IsRunning && !IsReset);

            _uithread = Dispatcher.CurrentDispatcher;
            _currentState = default(BrainDevState);
        }
        
        private void CheckUpdate(object sender, ElapsedEventArgs e)
        {
            lock (_syncRoot)
            {
                while (cache.TryDequeue(out var intArr))
                {
                    UpdateChannelBuffer(intArr);
                }
                
                if (!IsRunning || _channelViewModels == null) return;
                for (var i = 0; i < _channelViewModels.Count; i++)
                {
                    // Get the dataseries created for this channel
                    var channel = _channelViewModels[i];
                    channel.FlushBuf();
                    // For reporting current size to GUI
                    _currentSize = channel.ChannelDataSeries.Count;
                }
            }
        }

        private void UpdateChannelBuffer(double[] voltageArr)
        {
            if (_channelViewModels == null) return;
            var passTimes = BrainDevState.PassTimeMs(_currentState.SampleRate, _pakNum);
            _pakNum++;
            for (var i = 0; i < _channelViewModels.Count; i++)
            {
                // Get the dataseries created for this channel
                var channel = _channelViewModels[i];
                channel.BufferChannelData(passTimes, voltageArr[i]);
                // For reporting current size to GUI
                //_currentSize = channel.ChannelDataSeries.Count;
            }
        }

        private void Disconnect()
        {
            BrainDeviceManager.DisConnect();
            _running = false;
            _devCtl = null;
        }

        private void StartAsync()
        {
            Task.Factory.StartNew(StartDevCmd).ContinueWith((t)=>
            {
                UpdateRuningStates();
            },TaskScheduler.FromCurrentSynchronizationContext());
        }
        
        private async void StartDevCmd()
        {
            if (!_running)
            {
                _running = true;
                if (_devCtl != null)
                {
                    _isReset = false;
                    _pakNum = 0;
                    if (!await StartSampleAsync(_devCtl))
                    {
                        Disconnect();
                        return;
                    }
                }
                else
                {
                    var sender = await ConnectDevAsync();
                    if (sender == null)
                    {
                        Disconnect();
                        return;
                    }

                    if (!await StartSampleAsync(sender))
                    {
                        Disconnect();
                        return;
                    }
                    _devCtl = sender;
                    _isReset = false;
                    _pakNum = 0;
                }
            }
        }

        private void UpdateRuningStates()
        {
            IsReset = _isReset;
            IsRunning = _running;
            if (_running)
            {
                _timer = new Timer(_timerInterval);
                _timer.Elapsed += CheckUpdate;
                _timer.AutoReset = true;
                _timer.Start();
            }
            else
            {
                _timer?.Stop();
            }
        }

        private async void StopDevCmd()
        {
            if (IsRunning)
            {
                IsRunning = false;
                _timer.Stop();
                if (_devCtl != null)
                    await _devCtl.Stop();
            }
        }

        private void ResetDevCmd()
        {
            StopDevCmd();

            CreateChannelParts();
            IsReset = true;
            _pakNum = 0;
        }

        private void CreateChannelParts()
        {
            // Initialize N EEGChannelViewModels. Each of these will be represented as a single channel
            // of the EEG on the view. One channel = one SciChartSurface instance
            var colorsCount = _colors.Count;
            var count = BrainDevState.SampleCountPer20ms(_currentState.SampleRate);
            ChannelViewModels = new ObservableCollection<EEGChannelViewModel>();
            for (int i = 0; i < ChannelCount; i++)
            {
                var channelViewModel =
                    new EEGChannelViewModel(Size, _colors[i % colorsCount],count) {ChannelName = "Channel " + (i+1)};

                ChannelViewModels.Add(channelViewModel);
            }
        }

        private ConcurrentQueue<double[]> cache = new ConcurrentQueue<double[]>();
        private async Task<DevCommandSender> ConnectDevAsync()
        {
            try
            {
                BrainDeviceManager.Init();
                //TODO config IP and port
                var sender = await BrainDeviceManager.Connnect("192.168.0.101", 8088);
                //var sender = await BrainDeviceManager.Connnect("127.0.0.1", 9211);
                //TODO config vRef (default = 4.5f)

                //保证设备参数正常才继续跑逻辑
                BrainDeviceManager.BrainDeviceState.Subscribe(ss =>
                {
                    var reCreateChannels = _currentState.ChannelCount != ss.ChannelCount;
                    _currentState = ss;
                    if (reCreateChannels)
                    {
                        ChannelCount = _currentState.ChannelCount;
                        _uithread.InvokeAsync(CreateChannelParts);
                    }
                    //var pmax = 4.5f * 2 / _currentState.Gain;
                    //YVisibleRange = new DoubleRange(-pmax, pmax);
                    AppLogger.Debug($"Brain Device State Changed Detected: {ss}");
                }, () => { AppLogger.Debug("device stop detected"); });
                BrainDeviceManager.SampleDataStream.Subscribe(tuple =>
                {
                    var (order, datas, arr) = tuple;
                    var buf = datas.Array;
                    if (buf != null)
                    {
                        var startIdx = datas.Offset;
                        var voltageArr = new double[datas.Count];
                        for (var i = 0; i < datas.Count; i++)
                        {
                            voltageArr[i] = BitDataConverter.Calculatevoltage(buf[startIdx + i], 4.5f, _currentState.Gain);
                        }
                        cache.Enqueue(voltageArr);
                    }

//                    var copyArr = datas.CopyToArray();
//                    if (copyArr != null)
//                        _viewStream.OnNext(copyArr);
                    //Console.Write($" {order} ");
                    //AppLogger.Debug($"order:{order}");
                    //AppLogger.Debug($"converted values:{datas.Show()}");
                    //AppLogger.Debug($"original datas:{arr.Show()}");
                }, () =>
                {
                    _devCtl = null;
                    _uithread.InvokeAsync(ResetDevCmd);
                    AppLogger.Debug("device sampling stream closed detected");
                });

                var cmdResult = await sender.QueryParam();
                AppLogger.Debug("QueryParam result:" + cmdResult);
                if (cmdResult != CommandError.Success)
                {
                    AppLogger.Error("Failed to QueryParam, stop");
                    BrainDeviceManager.DisConnect();
                    return null;
                }
                //cmdResult = await sender.SetSampleRate(SampleRateEnum.SPS_2k);
                //AppLogger.Debug("SetSampleRate result:" + cmdResult);
                return sender;
            }
            catch (Exception)
            {
                return null;
            }
            /*
            cmdResult = await sender.SetFilter(false);
            AppLogger.Debug("SetFilter result:"+cmdResult);
            
            cmdResult = await sender.SetTrap(TrapSettingEnum.NoTrap);
            AppLogger.Debug("SetTrap result:"+cmdResult);
            
            cmdResult = await sender.SetSampleRate(SampleRateEnum.SPS_2k);
            AppLogger.Debug("SetSampleRate result:"+cmdResult);
            
            cmdResult = await sender.QueryParam();
            AppLogger.Debug("QueryParam result:"+cmdResult);
            */
        }
        private FileResource currentFileResource;
        private async Task<bool> StartSampleAsync(DevCommandSender sender)
        {
            currentFileResource?.Dispose();
            currentFileResource = null;
            FileResource fs = null;
            try
            {
                //TODO config device ID
                fs = new FileResource(_currentState, 19801983, 1, BrainDeviceManager.BufMgr);
                fs.StartRecord(BrainDeviceManager.SampleDataStream);
                var cmdResult = await sender.Start();
                if (cmdResult != CommandError.Success)
                {
                    AppLogger.Error("Failed to start sampler");
                    return false;
                }
                currentFileResource = fs;
                return true;
            }
            catch (Exception)
            {
                fs?.Dispose();
                return false;
            }
        }

        #endregion
    }
}