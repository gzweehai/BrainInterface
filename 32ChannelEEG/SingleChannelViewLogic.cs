using System;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using BrainCommon;
using SciChart.Charting.Model.DataSeries;
using static SciChart.Examples.Examples.CreateRealtimeChart.EEGChannelsDemo.EEGExampleViewModel;
using Timer = System.Timers.Timer;

namespace SciChart.Examples.Examples.SeeFeaturedApplication.ECGMonitor
{
    public partial class ECGMonitorViewModel
    {
        private DisposableCollector _unsubscriber;
        private List<(double, double)> xyBuffer;
        private double _lastX;
        private volatile bool _pause;
        private int _selectedChannelIndex;
        private double _timerInterval=10;
        private int _updatingTag = CASHelper.LockFree;
        private readonly List<(double, double)> _emptyList = new List<(double, double)>(0);

        private void SaveLastX()
        {
            _lastX = _series0.HasValues ? (double)_series0.XMax : 0;
        }

        public ECGMonitorViewModel(IObservable<(double, float)> channelDataStream,
            IObservable<(ChannelViewState, int)> channelStateStream)
        {
            _series0 = new XyDataSeries<double, double>() { FifoCapacity = 5000 };
            xyBuffer = new List<(double, double)>();
            _unsubscriber += channelDataStream.Subscribe(UpdateChannelData);
            _unsubscriber += channelStateStream.Subscribe(UpdateChannelViewState);
        }

        public void OnClosing()
        {
            _unsubscriber.Dispose();
        }
        
        private void UpdateChannelData((double,float) data)
        {
            if (_pause) return;
            var (voltage, passTimes) = data;
            var item = (_lastX + passTimes, voltage);
            var local = Interlocked.Exchange(ref xyBuffer, _emptyList);
            local.Add(item);
            Interlocked.Exchange(ref xyBuffer, local);
        }

        private void UpdateChannelViewState((ChannelViewState, int) vstate)
        {
            ChannelViewState vs;
            (vs, _selectedChannelIndex) = vstate;
            switch (vs)
            {
                case ChannelViewState.Pause:
                    Pause();
                    break;
                case ChannelViewState.Reset:
                    Reset();
                    break;
                case ChannelViewState.Running:
                    Resume();
                    break;
            }
        }

        private void Reset()
        {
            _pause = false;
            _timer?.Stop();
            _series0.Clear();
            /*for (int i = 0; i < _size; i++)
                _series0.Append(i, double.NaN);*/
            Interlocked.Exchange(ref xyBuffer, new List<(double, double)>());
            SaveLastX();
        }

        private void Pause()
        {
            _pause = true;
            _timer?.Stop();
            SaveLastX();
            Interlocked.Exchange(ref xyBuffer, new List<(double, double)>());
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
            }
            Interlocked.Exchange(ref _updatingTag, CASHelper.LockFree);
            local.Clear();
        }
    }
}