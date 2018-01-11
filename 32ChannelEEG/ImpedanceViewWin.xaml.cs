using BrainNetwork.BrainDeviceProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
                ListBoxItem item = new ListViewItem();
                var code = _currentState.GetChannelImpedance(i);
                item.Content = $"Channel {i+1}: {code}";
                ImpedanceListBox.Items.Add(item);
            }
            _uithread = Dispatcher.CurrentDispatcher;

            _unsubscriber = BrainDeviceManager.BrainDeviceState.Subscribe(OnStateChanged, () =>
            {
                _uithread.InvokeAsync(OnDisconnect);
            });
        }

        private void OnReconnect()
        {
            _unsubscriber?.Dispose();
            _unsubscriber = BrainDeviceManager.BrainDeviceState.Subscribe(OnStateChanged, () =>
            {
                _uithread.InvokeAsync(OnDisconnect);
            });
        }

        private void OnStateChanged(BrainDevState ss)
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
