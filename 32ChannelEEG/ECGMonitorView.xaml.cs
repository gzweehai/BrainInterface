using BrainCommon;
using System.Windows.Controls;

namespace SciChart.Examples.Examples.SeeFeaturedApplication.ECGMonitor
{
    /// <summary>
    /// Interaction logic for ECGMonitorView.xaml
    /// </summary>
    public partial class ECGMonitorView : UserControl
    {
        public ECGMonitorView()
        {
            InitializeComponent();
            var cfg = ClientConfig.GetConfig();
            LowPassRateTextBox.IntNum = cfg.LowRate;
            HighPassRateTextBox.IntNum = cfg.HighRate;
            FilterHalfOrderTextBox.IntNum = cfg.FilterHalfOrder;
        }

        private void ApplyFilterBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var viewModle = DataContext as ECGMonitorViewModel;
            if (viewModle == null) return;
            var lowRate = LowPassRateTextBox.IntNum;
            var highRate = HighPassRateTextBox.IntNum;
            var filterHalfOrder = FilterHalfOrderTextBox.IntNum;
            viewModle.SetBandwith(lowRate, highRate, filterHalfOrder);
        }

        private void ChannelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
