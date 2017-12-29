using System;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;
using Abt.Controls.SciChart.Example.Data;

namespace Abt.Controls.SciChart.Example.Examples.IWantTo.CreateRealtimeChart.EEGChannelsDemo
{
    /// <summary>
    /// Interaction logic for EEGExampleView.xaml
    /// </summary>
    public partial class EEGExampleView : UserControl
    {
        private Stopwatch _stopWatch;
        private double _lastFrameTime;
        private MovingAverage _fpsAverage;

        public EEGExampleView()
        {
            InitializeComponent();

            _stopWatch = Stopwatch.StartNew();
            _fpsAverage = new MovingAverage(50);
            CompositionTarget.Rendering += CompositionTarget_Rendering;            
        }

        /// <summary>
        /// Purely for stats reporting (FPS). Not needed for SciChart rendering
        /// </summary>
        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            // Compute the render time
            double frameTime = _stopWatch.ElapsedMilliseconds;
            double delta = frameTime - _lastFrameTime;
            double fps = 1000.0 / delta;

            // Push the fps to the movingaverage, we want to average the FPS to get a more reliable reading
            _fpsAverage.Push(fps);

            // Render the fps to the screen
            fpsCounter.Text = _fpsAverage.Current == double.NaN ? "-" : string.Format("{0:0}", _fpsAverage.Current);

            // Render the total point count (all series) to the screen
            var eegExampleViewModel = (DataContext as EEGExampleViewModel);
            pointCount.Text = eegExampleViewModel != null ? eegExampleViewModel.PointCount.ToString() : "Na";

            _lastFrameTime = frameTime;
        }
    }
}
