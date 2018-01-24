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
            LowPassRateTextBox.Text = cfg.LowRate.ToString();
            HighPassRateTextBox.Text = cfg.HighRate.ToString();
        }

        private void ApplyFilterBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var viewModle = DataContext as ECGMonitorViewModel;
            if (viewModle == null) return;
            var lowRate = LowPassRateTextBox.IntNum;
            var highRate = HighPassRateTextBox.IntNum;
            viewModle.SetBandwith(lowRate, highRate);
        }
    }
}
