using System;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using BrainCommon;
using BrainNetwork.BrainDeviceProtocol;
using MathNet.Filtering.FIR;
using SciChart.Charting.Model.DataSeries;
using SciChart_50ChannelEEG;
using static SciChart.Examples.Examples.CreateRealtimeChart.EEGChannelsDemo.EEGExampleViewModel;
using Timer = System.Timers.Timer;

namespace SciChart.Examples.Examples.SeeFeaturedApplication.ECGMonitor
{
    public partial class ECGMonitorViewModel
    {
        private IXyDataSeries<double, double> _filterSeries;
        private DisposableCollector _unsubscriber;
        private List<(double, double)> xyBuffer;
        private double _lastX;
        private volatile bool _pause;
        private int _selectedChannelIndex=-1;
        private double _timerInterval=10;
        private int _updatingTag = CASHelper.LockFree;
        private OnlineFirFilter _filter;
        private List<(double, double)> filterBuffer;
        private SampleRateEnum _sampleRate = (SampleRateEnum )(-1);
        private int _cutoffLow = 5;
        private int _cutoffHigh = 100;
        private int _filterHalfOrder = 2;
        private readonly List<(double, double)> _emptyList = new List<(double, double)>(0);

        private void SaveLastX()
        {
            _lastX = _series0.HasValues ? (double)_series0.XMax : 0;
        }

        public IXyDataSeries<double, double> SingleFilterDataSeries
        {
            get { return _filterSeries; }
            set
            {
                _filterSeries = value;
                OnPropertyChanged("SingleFilterDataSeries");
            }
        }

        public ECGMonitorViewModel(IObservable<(double, float)> channelDataStream,
            IObservable<(ChannelViewState, int, IXyDataSeries<double, double>)> channelStateStream,
            IObservable<BrainDevState> stateStream)
        {
            _series0 = new XyDataSeries<double, double>() { FifoCapacity = 1000 };
            _filterSeries = new XyDataSeries<double, double>() { FifoCapacity = 1000 };
            xyBuffer = new List<(double, double)>();
            filterBuffer = new List<(double, double)>();
            var cfg = ClientConfig.GetConfig();
            _cutoffLow = cfg.LowRate;
            _cutoffHigh = cfg.HighRate;
            _filterHalfOrder = cfg.FilterHalfOrder;
            _unsubscriber += channelDataStream.Subscribe(UpdateChannelData);
            _unsubscriber += channelStateStream.Subscribe(UpdateChannelViewState);
            _unsubscriber += stateStream.Subscribe(UpdateDevState);
        }

        internal void SetBandwith(int lowRate, int highRate,int filterHalfOrder)
        {
            if (_cutoffLow == lowRate && _cutoffHigh == highRate && _filterHalfOrder == filterHalfOrder) return;
            if (lowRate > highRate)
            {
                ViewWinUtils.ShowDefaultDialog("Low Rate is greater than High Rate!");
                return;
            }
            var cfg = ClientConfig.GetConfig();
            _cutoffLow = cfg.LowRate = lowRate;
            _cutoffHigh = cfg.HighRate = highRate;
            _filterHalfOrder = cfg.FilterHalfOrder = filterHalfOrder;
            CreateFilter();
        }


        public void OnClosing()
        {
            _unsubscriber.Dispose();
        }

        private void UpdateDevState(BrainDevState st)
        {
            //if (st.ChannelCount)
            if (_sampleRate == st.SampleRate) return;
            _sampleRate = st.SampleRate;
            CreateFilter();
        }

        private void CreateFilter()
        {
            var rate = BrainDevState.SampleCountPer1Sec(_sampleRate);
            var firCoef = _cutoffLow <= 0
                ? FirCoefficients.LowPass(rate, _cutoffHigh, _filterHalfOrder)
                : FirCoefficients.BandPass(rate, _cutoffLow, _cutoffHigh, _filterHalfOrder);
            var local = new OnlineFirFilter(firCoef);
            if (_series0.HasValues)
            {
                var yValues = _series0.YValues;
                var copyY = new double[yValues.Count];
                yValues.CopyTo(copyY, 0);
                var samples = local.ProcessSamples(copyY);
            }
            _filter = local;
        }

        private void UpdateChannelData((double,float) data)
        {
            if (_pause) return;
            var (voltage, passTimes) = data;
            var item = (_lastX + passTimes, voltage);
            var item2 = item;
            if (_filter != null)
            {
                voltage = _filter.ProcessSample(voltage);
                item2 = (_lastX + passTimes, voltage);
            }
            var local = Interlocked.Exchange(ref xyBuffer, _emptyList);
            var filterLocal = Interlocked.Exchange(ref filterBuffer, _emptyList);
            local.Add(item);
            filterLocal.Add(item2);
            Interlocked.Exchange(ref filterBuffer, filterLocal);
            Interlocked.Exchange(ref xyBuffer, local);
        }

        private void UpdateChannelViewState((ChannelViewState, int, IXyDataSeries<double, double>) vstate)
        {
            var (vs, index,channelDataSeries) = vstate;
            var channelChanged = false;
            if (index != _selectedChannelIndex)
            {
                _selectedChannelIndex = index;
                channelChanged = true;
            }
            switch (vs)
            {
                case ChannelViewState.Pause:
                    Pause();
                    CopyChannelData(channelChanged, channelDataSeries);
                    break;
                case ChannelViewState.Reset:
                    Reset();
                    break;
                case ChannelViewState.Running:
                    Resume();
                    CopyChannelData(channelChanged, channelDataSeries);
                    break;
            }
        }

        private void CopyChannelData(bool channelChanged, IXyDataSeries<double, double> channelDataSeries)
        {
            if (!channelChanged) return;

            /* although I can copy the data from original data series, the effect is not good,
             * because original data series might contain less data since delay update timing
             * instead I just clear the series views
            if (channelDataSeries != null)
            {
                Reset();
                Pause();
                var xvalues = channelDataSeries.XValues;
                var xcopy = new double[xvalues.Count];
                var ycopy = new double[xvalues.Count];
                xvalues.CopyTo(xcopy, 0);
                channelDataSeries.YValues.CopyTo(ycopy, 0);
                _series0.Clear();
                _series0.Append(xcopy, ycopy);
                SaveLastX();
                Resume();
            }
            else
            {
                _series0.Clear();
            }
            */

            _series0.Clear();
            _filterSeries.Clear();
        }

        private void Reset()
        {
            _pause = false;
            _timer?.Stop();
            _series0.Clear();
            _filterSeries.Clear();
            /*for (int i = 0; i < _size; i++)
                _series0.Append(i, double.NaN);*/
            Interlocked.Exchange(ref xyBuffer, new List<(double, double)>());
            Interlocked.Exchange(ref filterBuffer, new List<(double, double)>());
            SaveLastX();
        }

        private void Pause()
        {
            _pause = true;
            _timer?.Stop();
            SaveLastX();
            Interlocked.Exchange(ref xyBuffer, new List<(double, double)>());
            Interlocked.Exchange(ref filterBuffer, new List<(double, double)>());
        }

        private void Resume()
        {
            _pause = false;
            if (!(_timer != null && _timer.Enabled))
            {
                _timer = new Timer(_timerInterval);
                _timer.Elapsed += FlushData;
                _timer.AutoReset = true;
                _timer.Start();
            }
        }

        private void FlushData(object sender, ElapsedEventArgs e)
        {
            var tag = Interlocked.Exchange(ref _updatingTag, CASHelper.LockUsed);
            if (tag == CASHelper.LockUsed) return;

            var local = Interlocked.Exchange(ref xyBuffer, new List<(double, double)>());
            var filterLocal = Interlocked.Exchange(ref filterBuffer, new List<(double, double)>());
            try
            {
                if (local.Count > 0)
                {
                    double[] x = new double[local.Count], y = new double[local.Count];
                    for (int i = 0; i < local.Count; i++)
                    {
                        x[i] = local[i].Item1;
                        y[i] = local[i].Item2;
                    }
                    using (_series0.SuspendUpdates())
                    {
                        _series0.Append(x, y);
                    }

                    if (filterLocal.Count != local.Count)
                    {
                        x = new double[filterLocal.Count];
                        y = new double[filterLocal.Count];
                    }
                    for (int i = 0; i < filterLocal.Count; i++)
                    {
                        x[i] = filterLocal[i].Item1;
                        y[i] = filterLocal[i].Item2;
                    }
                    using (_filterSeries.SuspendUpdates())
                    {
                        _filterSeries.Append(x, y);
                    }
                }
            }
            finally
            {
                Interlocked.Exchange(ref _updatingTag, CASHelper.LockFree);
                local.Clear();
            }
        }
    }
}