using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Timers;
using System.Windows.Input;
using System.Windows.Media;
using SciChart.Charting.Common.Helpers;
using SciChart.Core.Helpers;
using SciChart.Core.Utility;
using SciChart.Examples.ExternalDependencies.Common;

namespace SciChart.Examples.Examples.CreateRealtimeChart.EEGChannelsDemo
{
    public partial class EEGExampleViewModel : BaseViewModel
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
        private uint _timerInterval = 10; // Interval of the timer to generate data in ms        
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
                var channelViewModel = new EEGChannelViewModel(Size, _colors[i % 16],i) {ChannelName = "Channel " + i};
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

    }
}