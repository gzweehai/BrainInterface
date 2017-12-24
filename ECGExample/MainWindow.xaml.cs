using System.Windows;
using Abt.Controls.SciChart.Example.Examples.IWantTo.SeeFeaturedApplication.ECGMonitor;

namespace ECGExample
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var viewModel = new ECGMonitorViewModel();
            ecgView.DataContext = viewModel;
            viewModel.OnExampleEnter();
        }
    }
}
