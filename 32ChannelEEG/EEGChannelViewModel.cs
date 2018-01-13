using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Media;
using BrainCommon;
using BrainNetwork.BrainDeviceProtocol;
using SciChart.Charting.Model.DataSeries;
using SciChart.Core.Framework;
using SciChart.Examples.ExternalDependencies.Common;

namespace SciChart.Examples.Examples.CreateRealtimeChart.EEGChannelsDemo
{
    public class EEGChannelViewModel : BaseViewModel
    {
        private readonly int _size;
        private Color _color;
        private IXyDataSeries<double, double> _channelDataSeries;
        private List<(double,double)> xyBuffer;
        private double _lastX;
        private volatile bool _pause;
        private volatile bool _isvisible;
        private IUpdateSuspender _updateCtl;
        private readonly List<(double, double)> _emptyList = new List<(double, double)>(0);

        public EEGChannelViewModel(int size, Color color, int count)
        {
            _size = size;
            Stroke = color;
            // Add an empty First In First Out series. When the data reaches capacity (int size) then old samples
            // will be pushed out of the series and new appended to the end. This gives the appearance of 
            // a scrolling chart window
            ChannelDataSeries = new XyDataSeries<double, double>() { FifoCapacity = _size };

            // Pre-fill with NaN up to size. This stops the stretching effect when Fifo series are filled with AutoRange
            for (int i = 0; i < _size; i++)
                ChannelDataSeries.Append(i, double.NaN);

            if (count > 0)
            {
                xyBuffer = new List<(double, double)>(count);
                //_channelDataSeries.AcceptsUnsortedData = true;
            }
            SaveLastX();
        }

        private void SaveLastX()
        {
            _lastX = _channelDataSeries.HasValues ? (double)_channelDataSeries.XMax+0.01 : 0;
        }

        public string ChannelName { get; set; }

        public Color Stroke 
        { 
            get { return _color; }
            set 
            { 
                _color = value;
                OnPropertyChanged("Stroke");
            }
        }

        public IXyDataSeries<double, double> ChannelDataSeries
        {
            get { return _channelDataSeries; }
            set
            {
                _channelDataSeries = value;
                OnPropertyChanged("ChannelDataSeries");
            }
        }

        public void Reset()
        {
            //TODO fill empty data to beatify Chart
            _channelDataSeries.Clear();
            _pause = false;
            _lastX = 0;
            var empty = new List<(double, double)>();
            Interlocked.Exchange(ref xyBuffer, empty);
        }

        public void BufferChannelData(float passTimes, double voltage)
        {
            if (_pause) return;
            var item = (_lastX+passTimes, voltage);
            var local = Interlocked.Exchange(ref xyBuffer, _emptyList);
            local.Add(item);
            Interlocked.Exchange(ref xyBuffer, local);
        }

        public void PauseX()
        {
            _pause = true;
            SaveLastX();
            var empty = new List<(double, double)>();
            Interlocked.Exchange(ref xyBuffer, empty);
        }

        public void Resume()
        {
            _pause = false;
        }

        public void OptVisible(bool isVisible)
        {
            _isvisible = isVisible;
            if (_isvisible)
            {
                if (_updateCtl != null)
                {
                    var tmp = _updateCtl;
                    _updateCtl = null;
                    tmp.Dispose();
                }
                else
                    AppLogger.Debug("_updateCtl is null");
            }
            else
            {
                if (_updateCtl == null)
                    _updateCtl = _channelDataSeries.SuspendUpdates();
            }
        }

        public void FlushBuf(float sampleUnitTime)
        {
            var empty = new List<(double, double)>();
            var tryCount = 10;
            var local = Interlocked.Exchange(ref xyBuffer, empty);

            while (local.Count <= 0 && tryCount>0)
            {
                tryCount--;
                local = Interlocked.Exchange(ref xyBuffer, empty);
            }
            if (local.Count <= 0) return;

            if (_isvisible)
            {
                using (_channelDataSeries.SuspendUpdates())
                {
                    UpdateSeriesData(local, sampleUnitTime);
                }
            }
            else
            {
                UpdateSeriesData(local, sampleUnitTime);
            }
            local.Clear();
        }

        private void UpdateSeriesData(List<(double, double)> local, float sampleUnitTime)
        {
            var count = local.Count;
            for (var i = 0; i < count; i++)
            {
                var xy = local[i];
                var xMax = (double)_channelDataSeries.XMax;
                if (xMax >= xy.Item1)
                {
                    AppLogger.Error($"{ChannelName} skip data: {xMax} >= {xy.Item1},{(double)_channelDataSeries.YMax},{xy.Item2}");
                    /*
                    var index = _channelDataSeries.FindIndex(xy.Item1 - sampleUnitTime);
                    if (index >= 0)
                    {
                        _channelDataSeries.Insert(index+1, xy.Item1, xy.Item2);//FIFO series does not allow insert
                    }
                    */
                    continue;
                }
                _channelDataSeries.Append(xy.Item1, xy.Item2);
            }
        }
    }
}