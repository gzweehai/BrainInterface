using BrainCommon;
using BrainNetwork.BrainDeviceProtocol;
using SciChart.Examples.Examples.CreateRealtimeChart.EEGChannelsDemo;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SciChart_50ChannelEEG
{
    /// <summary>
    /// ImpedanceViewWin.xaml 的交互逻辑
    /// </summary>
    public partial class ImpedanceViewWin : Window
    {
        private BrainDevState _currentState;
        private Dispatcher _uithread;
        private IDisposable _unsubscriber;

        public ImpedanceViewWin(BrainDevState state)
        {
            BrainDeviceManager.OnConnected += OnReconnect;
            InitializeComponent();
            _currentState = state;
            for (int i = 0; i < _currentState.ChannelCount; i++)
            {
                var code = _currentState.GetChannelImpedance(i);
                ListBoxItem item = new ListViewItem
                {
                    Content = $"Channel {i + 1}: {code}",
                    TabIndex = i + 1
                };
                ImpedanceListBox.Items.Add(item);
                item.Selected += OnItemSelected;
            }
            _uithread = Dispatcher.CurrentDispatcher;

            _unsubscriber = BrainDeviceManager.BrainDeviceState.Subscribe(OnDevStateChanged, () =>
            {
                _uithread.InvokeAsync(OnDisconnect);
            });
        }

        private void OnItemSelected(object sender, RoutedEventArgs e)
        {
            var item = sender as ListViewItem;
            if (item == null) return;
            AppLogger.Debug($"{item.Content},{item.TabIndex}");
            var viewmodel = DataContext as EEGExampleViewModel;
            if (viewmodel == null) return;
            var aresult = viewmodel.TestSingleImpedance(item.TabIndex);

            aresult?.ContinueWith(ar => {
                item.IsSelected = false;
                if (!ar.IsFaulted)
                {
                    var single = _currentState.LastSelectedSingleImpedanceChannel;
                    if (1 <= single && single <= _currentState.ChannelCount)
                    {
                        var ritem = ImpedanceListBox.Items[single - 1] as ListBoxItem;
                        if (ritem != null)
                        {
                            ritem.Content = $"Channel {single}: {_currentState.LastSingleImpedanceCode}";
                        }
                    }
                    var tmp = ViewWinUtils.CreateDefaultDialog($"Test Single Channel Impedance #{single}: {_currentState.LastSingleImpedanceCode}");
                    tmp.ShowDialog();
                }
                else
                {
                    var tmp = ViewWinUtils.CreateDefaultDialog(ar.Exception);
                    tmp.ShowDialog();
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void OnReconnect()
        {
            _unsubscriber?.Dispose();
            _unsubscriber = BrainDeviceManager.BrainDeviceState.Subscribe(OnDevStateChanged, () =>
            {
                _uithread.InvokeAsync(OnDisconnect);
            });
        }

        private void OnDevStateChanged(BrainDevState ss)
        {
            _currentState = ss;
            _uithread.InvokeAsync(RefreshImpedanceCodes);
        }

        private void RefreshImpedanceCodes()
        {
            for (var i = 0; i < ImpedanceListBox.Items.Count; i++)
            {
                var item = ImpedanceListBox.Items[i] as ListBoxItem;
                if (item != null)
                {
                    var code = _currentState.GetChannelImpedance(i);
                    item.Content = $"Channel {i+1}: {code}";
                }
            }
        }
        
        private void OnDisconnect()
        {
            _unsubscriber?.Dispose();
            _unsubscriber = null;
            for (var i = 0; i < ImpedanceListBox.Items.Count; i++)
            {
                var item = ImpedanceListBox.Items[i] as ListBoxItem;
                if (item != null)
                {
                    item.Content = $"Channel {i+1}: Disconnected";
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _unsubscriber?.Dispose();
            BrainDeviceManager.OnConnected -= OnReconnect;
            base.OnClosed(e);
        }
    }
}
