using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Abt.Controls.SciChart.ChartModifiers;
using Abt.Controls.SciChart.Visuals;
using Abt.Controls.SciChart.Visuals.Axes;

namespace Abt.Controls.SciChart.Example.Common
{
    /// <summary>
    /// A toolbar used in examples to simplify zoom, pan, zoom extents, rollover, cursor etc... This also helps us with testing ;-)
    /// </summary>
    [TemplatePart(Name = "PART_Container", Type = typeof(Border))]
    public class SciChartInteractionToolbar : ContentControl
    {
        public static readonly DependencyProperty ToolbarIdProperty =
            DependencyProperty.Register("ToolbarId", typeof(string), typeof(SciChartInteractionToolbar), new PropertyMetadata(default(string)));

        public static readonly DependencyProperty TargetSurfaceProperty =
            DependencyProperty.Register("TargetSurface", typeof(ISciChartSurface), typeof(SciChartInteractionToolbar), new PropertyMetadata(default(ISciChartSurface), OnTargetSurfaceDependencyPropertyChanged));

        public static readonly DependencyProperty IsZoomPanEnabledProperty =
            DependencyProperty.Register("IsZoomPanEnabled", typeof(bool), typeof(SciChartInteractionToolbar), new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty IsRubberBandZoomEnabledProperty =
            DependencyProperty.Register("IsRubberBandZoomEnabled", typeof(bool), typeof(SciChartInteractionToolbar), new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty IsAxesSwappedProperty =
            DependencyProperty.Register("IsAxesSwapped", typeof(bool), typeof(SciChartInteractionToolbar),
                                        new PropertyMetadata(default(bool), OnIsAxesSwapppedChanged));

        public static readonly DependencyProperty IsXAxisFlippedProperty =
    DependencyProperty.Register("IsXAxisFlipped", typeof(bool), typeof(SciChartInteractionToolbar),
                                new PropertyMetadata(default(bool), OnXAxisFlippedChanged));

        public static readonly DependencyProperty IsYAxisFlippedProperty =
    DependencyProperty.Register("IsYAxisFlipped", typeof(bool), typeof(SciChartInteractionToolbar),
                                new PropertyMetadata(default(bool), OnYAxisFlippedChanged));

        public SciChartInteractionToolbar()
        {
            DefaultStyleKey = typeof(SciChartInteractionToolbar);
            ToolbarId = Guid.NewGuid().ToString();
        }

        public string ToolbarId
        {
            get { return (string)GetValue(ToolbarIdProperty); }
            set { SetValue(ToolbarIdProperty, value); }
        }

        public ISciChartSurface TargetSurface
        {
            get { return (ISciChartSurface)GetValue(TargetSurfaceProperty); }
            set { SetValue(TargetSurfaceProperty, value); }
        }

        public bool IsRubberBandZoomEnabled
        {
            get { return (bool)GetValue(IsRubberBandZoomEnabledProperty); }
            set { SetValue(IsRubberBandZoomEnabledProperty, value); }
        }

        public bool IsZoomPanEnabled
        {
            get { return (bool)GetValue(IsZoomPanEnabledProperty); }
            set { SetValue(IsZoomPanEnabledProperty, value); }
        }

        public bool IsAxesSwapped
        {
            get { return (bool)GetValue(IsAxesSwappedProperty); }
            set { SetValue(IsAxesSwappedProperty, value); }
        }

        public bool IsXAxisFlipped
        {
            get { return (bool)GetValue(IsXAxisFlippedProperty); }
            set { SetValue(IsXAxisFlippedProperty, value); }
        }

        public bool IsYAxisFlipped
        {
            get { return (bool)GetValue(IsYAxisFlippedProperty); }
            set { SetValue(IsYAxisFlippedProperty, value); }
        }

        public ICommand RotateChartCommand
        {
            get
            {
                return new ActionCommand(() =>
                {
                    var scs = TargetSurface;

                    using (scs.SuspendUpdates())
                    {
                        foreach (var xAxis in scs.XAxes)
                        {
                            RotateClockwise(xAxis);
                        }

                        foreach (var yAxis in scs.YAxes)
                        {
                            RotateClockwise(yAxis);
                        }
                    }
                });
            }
        }

        private static void OnTargetSurfaceDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var toolbar = (SciChartInteractionToolbar)d;
            var scs = e.NewValue as ISciChartSurface;

            if (scs == null)
                return;

            var zpm = new ZoomPanModifier();
            var rbzm = new RubberBandXyZoomModifier { IsXAxisOnly = true };
            var yAxisDragModifier = new YAxisDragModifier();
            var xAxisDragModifier = new XAxisDragModifier();
            var mouseWheelZoomPanModifier = new MouseWheelZoomModifier();

            scs.ChartModifier = new ModifierGroup(zpm, rbzm, mouseWheelZoomPanModifier, yAxisDragModifier, xAxisDragModifier, new ZoomExtentsModifier());

            zpm.SetBinding(ChartModifierBase.IsEnabledProperty, new Binding("IsZoomPanEnabled") { Source = toolbar, Mode = BindingMode.TwoWay });
            rbzm.SetBinding(ChartModifierBase.IsEnabledProperty, new Binding("IsRubberBandZoomEnabled") { Source = toolbar, Mode = BindingMode.TwoWay });
        }

        private static void OnXAxisFlippedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var toolbar = (SciChartInteractionToolbar)d;
            var scs = toolbar.TargetSurface;

            using (scs.SuspendUpdates())
            {
                foreach(var axis in scs.XAxes)
                    axis.FlipCoordinates = (bool)e.NewValue;
            }
        }

        private static void OnYAxisFlippedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var toolbar = (SciChartInteractionToolbar)d;
            var scs = toolbar.TargetSurface;

            using (scs.SuspendUpdates())
            {
                foreach(var axis in scs.YAxes)
                    axis.FlipCoordinates = (bool)e.NewValue;
            }
        }

        private static void OnIsAxesSwapppedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var toolbar = (SciChartInteractionToolbar)d;
            var scs = toolbar.TargetSurface;

            using (scs.SuspendUpdates())
            {
                foreach (var xAxis in scs.XAxes)
                {
                    ApplyAlignment(xAxis);
                }

                foreach (var yAxis in scs.YAxes)
                {
                    ApplyAlignment(yAxis);
                }
            }
        }

        private static void ApplyAlignment(IAxis axis)
        {
            switch (axis.AxisAlignment)
            {
                case AxisAlignment.Right:
                    axis.AxisAlignment = AxisAlignment.Bottom;
                    break;
                case AxisAlignment.Bottom:
                    axis.AxisAlignment = AxisAlignment.Right;
                    break;
                case AxisAlignment.Top:
                    axis.AxisAlignment = AxisAlignment.Left;
                    break;
                case AxisAlignment.Left:
                    axis.AxisAlignment = AxisAlignment.Top;
                    break;
            }
        }

        private static void RotateClockwise(IAxis axis)
        {
            switch (axis.AxisAlignment)
            {
                case AxisAlignment.Right:
                    axis.AxisAlignment = AxisAlignment.Bottom;
                    break;
                case AxisAlignment.Bottom:
                    axis.AxisAlignment = AxisAlignment.Left;
                    break;
                case AxisAlignment.Top:
                    axis.AxisAlignment = AxisAlignment.Right;
                    break;
                case AxisAlignment.Left:
                    axis.AxisAlignment = AxisAlignment.Top;
                    break;
            }
        }
    }
}
