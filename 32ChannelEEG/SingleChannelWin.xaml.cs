﻿using System;
using System.ComponentModel;
using System.Windows;
using SciChart.Examples.Examples.SeeFeaturedApplication.ECGMonitor;
using static SciChart.Examples.Examples.CreateRealtimeChart.EEGChannelsDemo.EEGExampleViewModel;

namespace SciChart_50ChannelEEG
{
    /// <summary>
    /// SingleChannelWin.xaml 的交互逻辑
    /// </summary>
    public partial class SingleChannelWin : Window
    {
        public SingleChannelWin(IObservable<(double, float)> ds, 
            IObservable<(ChannelViewState, int)> ss)
        {
            InitializeComponent();
            SingleChannelView.DataContext = new ECGMonitorViewModel(ds, ss);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            var model = SingleChannelView.DataContext as ECGMonitorViewModel;
            model?.OnClosing();
            
            base.OnClosing(e);
        }
    }
}