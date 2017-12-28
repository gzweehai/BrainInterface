using System.Windows.Media;
using Abt.Controls.SciChart.Model.DataSeries;
using GreatBrainClient.MonitorViews;

namespace Abt.Controls.SciChart.Example.Examples.IWantTo.CreateRealtimeChart.EEGChannelsDemo
{
    public class EEGChannelViewModel : BaseViewModel
    {
        private readonly int _size;
        private Color _color;
        private IXyDataSeries<double, double> _channelDataSeries;

        public EEGChannelViewModel(int size, Color color)
        {
            _size = size;
            SeriesColor = color;

            // Add an empty First In First Out series. When the data reaches capacity (int size) then old samples
            // will be pushed out of the series and new appended to the end. This gives the appearance of 
            // a scrolling chart window
            ChannelDataSeries = new XyDataSeries<double, double>() {FifoCapacity = _size};                
        }

        public string ChannelName { get; set; }

        public Color SeriesColor 
        { 
            get { return _color; }
            set 
            { 
                _color = value;
                OnPropertyChanged("SeriesColor");
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
    }
}