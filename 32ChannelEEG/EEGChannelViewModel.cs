using System.Collections.Generic;
using System.Threading;
using System.Windows.Media;
using BrainCommon;
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
        private List<double> xBuffer;
        private List<double> yBuffer;
        private double _lastX;
        private bool _pause;
        private bool _isvisible;
        private IUpdateSuspender _updateCtl;

        public EEGChannelViewModel(int size, Color color, int count)
        {
            _size = size;
            Stroke = color;
            // Add an empty First In First Out series. When the data reaches capacity (int size) then old samples
            // will be pushed out of the series and new appended to the end. This gives the appearance of 
            // a scrolling chart window
            ChannelDataSeries = new XyDataSeries<double, double>() {FifoCapacity = _size};                

            // Pre-fill with NaN up to size. This stops the stretching effect when Fifo series are filled with AutoRange
            for(int i = 0; i < _size; i++)
                ChannelDataSeries.Append(i, double.NaN);

            if (count > 0)
            {
                xBuffer = new List<double>(count);
                yBuffer = new List<double>(count);
                //_channelDataSeries.AcceptsUnsortedData = true;
            }
            _lastX = _channelDataSeries.HasValues ? (double)_channelDataSeries.XMax : 0;
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
            xBuffer.Clear();
            yBuffer.Clear();
        }

        public void BufferChannelData(float passTimes, double voltage)
        {
            if (_pause) return;
            xBuffer.Add(passTimes);
            yBuffer.Add(voltage);
        }

        public void PauseX()
        {
            _lastX=_channelDataSeries.HasValues ?(double)_channelDataSeries.XMax:0;
            _pause = true;
            xBuffer.Clear();
            yBuffer.Clear();
        }

        public void Resume()
        {
            _pause = false;
        }

        public void OptVisible(bool isVisible)
        {
            _isvisible = isVisible;
            /*if (_isvisible)
            {
                if (_updateCtl != null)
                {
                    //Interlocked.Exchange
                    var tmp = _updateCtl;
                    _updateCtl = null;
                    //_channelDataSeries.ResumeUpdates(tmp);
                    tmp.Dispose();
                }
                else
                    AppLogger.Debug("_updateCtl is null");
            }
            else
                _updateCtl = _channelDataSeries.SuspendUpdates();
            */
        }

        public void FlushBuf()
        {
            if (xBuffer.Count > 0)
            {
                if (_isvisible)
                {
                    using (_channelDataSeries.SuspendUpdates())
                    {
                        int count = xBuffer.Count;
                        for (var i = 0; i < count; i++)
                        {
                            _channelDataSeries.Append(_lastX + xBuffer[i], yBuffer[i]);
                        }
                    }
                    xBuffer.Clear();
                    yBuffer.Clear();
                }
                /*else
                {
                    int count = xBuffer.Count;
                    for (var i = 0; i < count; i++)
                    {
                        _channelDataSeries.Append(_lastX + xBuffer[i], yBuffer[i]);
                    }
                }*/
            }
        }
    }
}