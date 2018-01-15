using BrainNetwork.BrainDeviceProtocol;
using SciChart.Examples.Examples.CreateRealtimeChart.EEGChannelsDemo;
using System.Threading.Tasks;
using System.Windows;

namespace SciChart_50ChannelEEG
{
    /// <summary>
    /// SettingViewWin.xaml 的交互逻辑
    /// </summary>
    public partial class SettingViewWin : Window
    {
        public SettingViewWin()
        {
            InitializeComponent();
        }

        private void SetSampleRate(SampleRateEnum rate)
        {
            var eEGExampleViewModel = this.DataContext as EEGExampleViewModel;
            if (eEGExampleViewModel == null) return;
            var aresult = eEGExampleViewModel.SetSampleRate(rate);
            if (aresult == null) return;
            aresult.ContinueWith(result => ShowSetSampleResult(result,rate));
        }

        private void ShowSetSampleResult(Task<CommandError> result, SampleRateEnum rate)
        {
            var r = result.Result;
            if (r != CommandError.Success)
            {
                Window tmp = new Window()
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                };
                tmp.Content = $"Set Sample Rate {rate} failed: {r}";
                tmp.ShowDialog();
            }
            var eEGExampleViewModel = this.DataContext as EEGExampleViewModel;
            if (eEGExampleViewModel == null) return;
            eEGExampleViewModel.UpdateRuningStates();
        }

        private void SmapleRate2kBtn_Checked(object sender, RoutedEventArgs e)
        {
            SetSampleRate(SampleRateEnum.SPS_2k);
        }

        private void SmapleRate1kBtn_Checked(object sender, RoutedEventArgs e)
        {
            SetSampleRate(SampleRateEnum.SPS_1k);
        }

        private void SmapleRate500Btn_Checked(object sender, RoutedEventArgs e)
        {
            SetSampleRate(SampleRateEnum.SPS_500);
        }

        private void SmapleRate250Btn_Checked(object sender, RoutedEventArgs e)
        {
            SetSampleRate(SampleRateEnum.SPS_250);
        }
    }
}
