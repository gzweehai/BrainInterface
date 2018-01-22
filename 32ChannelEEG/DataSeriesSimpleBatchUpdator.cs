using BrainCommon;
using SciChart.Charting.Model.DataSeries;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace SciChart_50ChannelEEG
{
    public class DataSeriesSimpleBatchUpdator
    {
        private IXyDataSeries<double, double> _dataSeries;
        private Func<double, double> _filter;

        private List<(double, double)> xyBuffer;
        private double _lastX;
        private volatile bool _pause;
        private int _updatingTag = CASHelper.LockFree;
        private Timer _timer;
        private double _timerInterval = 10;
        private bool _enableTimer;

        private readonly List<(double, double)> _emptyList = new List<(double, double)>(0);

        public DataSeriesSimpleBatchUpdator(IXyDataSeries<double, double> dataSeries, Func<double, double> filter, bool enableTimmer = true)
        {
            _dataSeries = dataSeries;
            _filter = filter;
            _enableTimer = enableTimmer;
        }

        public DataSeriesSimpleBatchUpdator(IXyDataSeries<double, double> dataSeries, bool enableTimmer = true)
        {
            _dataSeries = dataSeries;
            _enableTimer = enableTimmer;
        }

        private void SaveLastX()
        {
            _lastX = _dataSeries.HasValues ? (double)_dataSeries.XMax : 0;
        }

        private void BufferData((double, float) data)
        {
            if (_pause) return;
            var (voltage, passTimes) = data;
            if (_filter != null)
                voltage = _filter(voltage);
            var item = (_lastX + passTimes, voltage);
            var local = Interlocked.Exchange(ref xyBuffer, _emptyList);
            local.Add(item);
            Interlocked.Exchange(ref xyBuffer, local);
        }

        private void Reset()
        {
            _pause = false;
            if (_enableTimer)
                _timer?.Stop();
            _dataSeries.Clear();
            Interlocked.Exchange(ref xyBuffer, new List<(double, double)>());
            SaveLastX();
        }

        private void Pause()
        {
            _pause = true;
            if (_enableTimer)
                _timer?.Stop();
            SaveLastX();
            Interlocked.Exchange(ref xyBuffer, new List<(double, double)>());
        }

        private void Resume()
        {
            _pause = false;
            if (_enableTimer && !(_timer != null && _timer.Enabled))
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
                using (_dataSeries.SuspendUpdates())
                {
                    _dataSeries.Append(x, y);
                }
            }
            Interlocked.Exchange(ref _updatingTag, CASHelper.LockFree);
            local.Clear();
        }
    }
}
