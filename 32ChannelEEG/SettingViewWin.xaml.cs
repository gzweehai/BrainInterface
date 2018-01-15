using BrainNetwork.BrainDeviceProtocol;
using SciChart.Examples.Examples.CreateRealtimeChart.EEGChannelsDemo;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SciChart_50ChannelEEG
{
    /// <summary>
    /// SettingViewWin.xaml 的交互逻辑
    /// </summary>
    public partial class SettingViewWin : Window
    {
        public SettingViewWin(EEGExampleViewModel eEGExampleViewModel)
        {
            InitializeComponent();
            _uithread = Dispatcher.CurrentDispatcher;
            if (eEGExampleViewModel == null)
            {
                return;
            }
            switch (eEGExampleViewModel.CurrentSampleRate)
            {
                case SampleRateEnum.SPS_2k:
                    SampleRate2kBtn.IsChecked = true;
                    break;
                case SampleRateEnum.SPS_1k:
                    SampleRate1kBtn.IsChecked = true;
                    break;
                case SampleRateEnum.SPS_500:
                    SampleRate500Btn.IsChecked = true;
                    break;
                case SampleRateEnum.SPS_250:
                    SampleRate250Btn.IsChecked = true;
                    break;
            }
        }

        private void SetSampleRate(SampleRateEnum rate)
        {
            if (_requesting) return;
            _requesting = true;
            var eEGExampleViewModel = this.DataContext as EEGExampleViewModel;
            if (eEGExampleViewModel == null)
            {
                _requesting = false;
                return;
            }
            var aresult = eEGExampleViewModel.SetSampleRate(rate);
            eEGExampleViewModel = null;
            if (aresult == null)
            {
                _requesting = false;
                return;
            }
            aresult.ContinueWith(result =>
            {
                _uithread.InvokeAsync(() =>
                {
                    _requesting = false;
                    ShowSetSampleResult(result.Result, rate);
                });
            });
        }

        private bool _requesting;
        private readonly Dispatcher _uithread;

        private void ShowSetSampleResult(CommandError result, SampleRateEnum rate)
        {
            var r = result;
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

        private void SampleRate2kBtn_Checked(object sender, RoutedEventArgs e)
        {
            SetSampleRate(SampleRateEnum.SPS_2k);
        }

        private void SampleRate1kBtn_Checked(object sender, RoutedEventArgs e)
        {
            SetSampleRate(SampleRateEnum.SPS_1k);
        }

        private void SampleRate500Btn_Checked(object sender, RoutedEventArgs e)
        {
            SetSampleRate(SampleRateEnum.SPS_500);
        }

        private void SampleRate250Btn_Checked(object sender, RoutedEventArgs e)
        {
            SetSampleRate(SampleRateEnum.SPS_250);
        }
    }
}
