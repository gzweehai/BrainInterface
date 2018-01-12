using System.ComponentModel;
using System.Windows;

namespace SciChartExport
{
    /// <summary>
    /// Interaction logic for Shell.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            MainView.OnClsing(e);
            base.OnClosing(e);
        }
    }
}
