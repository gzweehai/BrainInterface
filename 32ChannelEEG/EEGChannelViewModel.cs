using System.Collections.Generic;
using System.Windows.Media;
using SciChart.Charting.Model.DataSeries;
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
                ChannelDataSeries.AcceptsUnsortedData = true;
            }
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
            _channelDataSeries.Clear();         
        }

        public void BufferChannelData(float passTimes, double voltage)
        {
            xBuffer.Add(passTimes);
            yBuffer.Add(voltage);
        }

        public void FlushBuf()
        {
            if (xBuffer.Count > 0)
            {
                _channelDataSeries.Append(xBuffer,yBuffer);
                xBuffer.Clear();
                yBuffer.Clear();
            }
        }
    }
}