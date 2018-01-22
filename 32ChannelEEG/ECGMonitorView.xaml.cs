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
        }

        private void LowPassRateTextBox_TextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            //var viewModle = DataContext as ECGMonitorViewModel;
            //if (viewModle == null) return;
            //LowPassRateTextBox.Text;
            //viewModle.SetLowPassFilterRate(10);
        }

        private void LowPassRateTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var viewModle = DataContext as ECGMonitorViewModel;
            if (viewModle == null) return;
            var rateBox = sender as TextBox;
            if (rateBox == null) return;
            var nRate = rateBox.Text.ToInt();
            if (nRate == 0) return;
            //viewModle.SetLowPassFilterRate(10);
        }
    }
}
