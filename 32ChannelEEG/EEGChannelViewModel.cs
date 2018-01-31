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
        private List<(double,double)> xyBuffer;
        private double _lastX;
        private volatile bool _pause;
        private volatile bool _isvisible;
        private IUpdateSuspender _updateCtl;
        private int _updatingTag;
        private readonly List<(double, double)> _emptyList = new List<(double, double)>(0);
        public readonly int Index;

        public EEGChannelViewModel(int size, Color color,int index)
        {
            Index = index;
            _size = size;
            Stroke = color;
            // Add an empty First In First Out series. When the data reaches capacity (int size) then old samples
            // will be pushed out of the series and new appended to the end. This gives the appearance of 
            // a scrolling chart window
            ChannelDataSeries = new XyDataSeries<double, double>() { FifoCapacity = _size };

            // Pre-fill with NaN up to size. This stops the stretching effect when Fifo series are filled with AutoRange
            for (int i = 0; i < _size; i++)
                ChannelDataSeries.Append(i, double.NaN);

            xyBuffer = new List<(double, double)>();
            SaveLastX();
        }

        private void SaveLastX()
        {
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
            _pause = false;
            _channelDataSeries.Clear();
            for (int i = 0; i < _size; i++)
                ChannelDataSeries.Append(i, double.NaN);
            Interlocked.Exchange(ref xyBuffer, new List<(double, double)>());
            SaveLastX();
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
            Interlocked.Exchange(ref xyBuffer, new List<(double, double)>());
        }

        public void Resume()
        {
            _pause = false;
        }

        /// <summary>
        /// 当某个通道超出主窗口显示区域的时候，暂停该通道的数据更新，
        /// 减轻UI线程的负担
        /// </summary>
        /// <param name="isVisible"></param>
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
#if DEBUG
                    AppLogger.Debug($"{ChannelName}: resume update UI");
#endif
                }
            }
            else
            {
                if (_updateCtl == null)
                {
                    _updateCtl = _channelDataSeries.SuspendUpdates();
#if DEBUG
                    AppLogger.Debug($"{ChannelName}: pause update UI");
#endif
                }
            }
        }

        public void FlushBuf()
        {
            //通过CAS指令保证更新的线程安全
            var tag = Interlocked.Exchange(ref _updatingTag, CASHelper.LockUsed);
            if (tag == CASHelper.LockUsed) return;

            var local = Interlocked.Exchange(ref xyBuffer, new List<(double, double)>());

            if (local.Count > 0)
            {
                double[] x = new double[local.Count], y = new double[local.Count];
                for (int i =0;i< local.Count; i++)
                {
                    x[i] = local[i].Item1;
                    y[i] = local[i].Item2;
                }
                if (_isvisible)
                {
                    using (_channelDataSeries.SuspendUpdates())
                    {
                        _channelDataSeries.Append(x, y);
                    }
                }
                else
                {
                    _channelDataSeries.Append(x, y);
                }
            }
            Interlocked.Exchange(ref _updatingTag, CASHelper.LockFree);
            local.Clear();
        }
    }
}