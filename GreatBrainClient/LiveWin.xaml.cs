using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
//using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Abt.Controls.SciChart.Example.Examples.IWantTo.CreateRealtimeChart.EEGChannelsDemo;
using GreatBrainClient.MonitorViews;

namespace GreatBrainClient
{
    /// <summary>
    /// LiveWin.xaml 的交互逻辑
    /// </summary>
    public partial class LiveWin : Window
    {
        public LiveWin()
        {
            InitializeComponent();
            //初始化通道信息
           //InitialChanels();
            ShowEcgView();
        }

        public void ShowEcgView()
        {
            /*
            var viewModel = new ECGMonitorViewModel();
            EcgView.DataContext = viewModel;
            viewModel.OnExampleEnter();
            */
            var eegExampleViewModel = new EEGExampleViewModel();
            EEGView.DataContext = eegExampleViewModel;
            eegExampleViewModel.OnExampleEnter();
        }
        
/*
        private void InitialChanels()
        {
            for (int i = 0; i <10; i++){
                var plotter = new Figure();
               //lotter.Margin.Bottom = 5.0;
                plotter.Height = 50;
                //var xPlotAxis = new PlotAxis();
                var yPlotAxis = new PlotAxis();
                yPlotAxis.AxisOrientation = AxisOrientation.Left;
                
                Figure.SetPlacement(yPlotAxis, Placement.Left);
                var xPlotAxis = new PlotAxis();
                xPlotAxis.AxisOrientation = AxisOrientation.Bottom;
                Figure.SetPlacement(xPlotAxis, Placement.Bottom);
                var line = new LineGraph();
                line.Stroke = new SolidColorBrush(Colors.Black);
                line.StrokeThickness =0.3;
                var x = Enumerable.Range(0, 1001).Select(j => j / 10.0).ToArray();
                var y = x.Select(v => Math.Abs(v) < 1e-10 ? 1 : Math.Sin(v) / v).ToArray();
                line.Plot(x, y);

                plotter.Children.Add(yPlotAxis);
                if (i == 9)
                {
                    plotter.Children.Add(xPlotAxis);
                }
                plotter.Children.Add(line);
                Chanels.Children.Add(plotter);
            }
        }
        */

    }


}
