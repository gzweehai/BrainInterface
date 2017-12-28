
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace Abt.Controls.SciChart.Example.Behaviors
{
    class HyperlinkButtonBehavior : Behavior<Button>
    {
        public static readonly DependencyProperty UriProperty = DependencyProperty.Register("Uri", typeof(string), typeof(HyperlinkButtonBehavior));

        public string Uri
        {
            get { return (string) GetValue(UriProperty); }
            set { SetValue(UriProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.Click += OnHyperlinkClick;
            AssociatedObject.Cursor = Cursors.Hand;
        }

        private void OnHyperlinkClick(object sender, RoutedEventArgs e)
        {
            if(!string.IsNullOrWhiteSpace(Uri))
            {
                Process.Start(Uri);
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.Click -= OnHyperlinkClick;
            AssociatedObject.Cursor = Cursors.Arrow;
        }
    }
}
