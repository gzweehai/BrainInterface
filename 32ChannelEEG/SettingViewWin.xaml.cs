using BrainNetwork.BrainDeviceProtocol;
using SciChart.Examples.Examples.CreateRealtimeChart.EEGChannelsDemo;
using System.Windows;
using System.Windows.Threading;
using System;
using System.Threading.Tasks;
using BrainCommon;

namespace SciChart_50ChannelEEG
{
    /// <summary>
    /// SettingViewWin.xaml 的交互逻辑
    /// </summary>
    public partial class SettingViewWin : Window
    {
        private bool _requesting;
        private readonly Dispatcher _uithread;
        private IDisposable _unsubscriber;
        private BrainDevState _currentState;

        /// <summary>
        /// 显示/修改基本设置
        /// </summary>
        /// <param name="state"></param>
        public SettingViewWin(BrainDevState state)
        {
            BrainDeviceManager.OnConnected += OnReconnect;
            InitializeComponent();
            _uithread = Dispatcher.CurrentDispatcher;
            _unsubscriber = BrainDeviceManager.BrainDeviceState.Subscribe(OnDevStateChanged, () =>
            {
                _uithread.InvokeAsync(OnDisconnect);
            });
            OnDevStateChanged(state);
            var cfglocal = ClientConfig.GetConfig();
            IpTextBox.Text = cfglocal.Ip;
            PortTextBox.Text = cfglocal.Port.ToString();
            EnableTimeoutCheckBox.IsChecked = cfglocal.EnableCommandTimeout;
            TimeoutTextBox.Text = cfglocal.TimeoutMilliseconds.ToString();
            AutoStartCheckBox.IsChecked = cfglocal.IsAutoStart;
        }

        protected override void OnClosed(EventArgs e)
        {
            _unsubscriber?.Dispose();
            BrainDeviceManager.OnConnected -= OnReconnect;
            var cfglocal = ClientConfig.GetConfig();
            cfglocal.Ip = IpTextBox.Text;
            cfglocal.Port = PortTextBox.Text.ToInt();
            cfglocal.IsAutoStart = AutoStartCheckBox.IsChecked ?? cfglocal.IsAutoStart;
            ClientConfig.ChangeTimeout(EnableTimeoutCheckBox.IsChecked,TimeoutTextBox.Text);
            base.OnClosed(e);
        }

        private void OnDevStateChanged(BrainDevState ss)
        {
            _currentState = ss;
            _uithread.InvokeAsync(RefreshDevStateUI);
        }

        private void RefreshDevStateUI()
        {
            switch (_currentState.SampleRate)
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

        private void OnDisconnect()
        {
            _unsubscriber?.Dispose();
            _unsubscriber = null;
        }

        private void OnReconnect()
        {
            _unsubscriber?.Dispose();
            _unsubscriber = BrainDeviceManager.BrainDeviceState.Subscribe(OnDevStateChanged, () =>
            {
                _uithread.InvokeAsync(OnDisconnect);
            });
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
                _requesting = false;
                ShowSetSampleResult(result.Result, rate);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void ShowSetSampleResult(CommandError result, SampleRateEnum rate)
        {
            var r = result;
            if (r != CommandError.Success)
            {
                ViewWinUtils.ShowDefaultDialog($"Set Sample Rate {rate} failed: {r}");
            }
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
