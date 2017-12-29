using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;
using Abt.Controls.SciChart.Common.Extensions;
using Abt.Controls.SciChart.Example.Helpers;

namespace Abt.Controls.SciChart.Example.Behaviors
{
    class ListBoxMouseWheelScrollingBehavior: Behavior<ListBox>
    {
        private ScrollViewer _scrollViewer;

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.PreviewMouseWheel += OnMouseWheel;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.PreviewMouseWheel -= OnMouseWheel;
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;

            if ((_scrollViewer ?? (_scrollViewer = AssociatedObject.FindVisualChild<ScrollViewer>())) != null)
            {
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta) { RoutedEvent = UIElement.MouseWheelEvent, Source = sender };

                _scrollViewer.RaiseEvent(eventArg);
            }
        }
    }
}
