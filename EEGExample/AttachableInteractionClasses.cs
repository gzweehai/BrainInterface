using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Interactivity;

namespace Abt.Controls.SciChart.Example.Helpers
{
    public class AttachableInteractionClasses
    {
        public static readonly DependencyProperty BehaviorsProperty = DependencyProperty.RegisterAttached("Behaviors",
                                                                                                          typeof (
                                                                                                              BehaviorsCollection
                                                                                                              ),
                                                                                                          typeof (
                                                                                                              AttachableInteractionClasses
                                                                                                              ),
                                                                                                          new PropertyMetadata
                                                                                                              (null,
                                                                                                               OnBehaviorsCollectionChanged));

        public static Collection<Behavior> GetBehaviors(DependencyObject o)
        {
            return (BehaviorsCollection) o.GetValue(BehaviorsProperty);
        }

        public static void SetBehaviors(DependencyObject o, BehaviorsCollection value)
        {
            o.SetValue(BehaviorsProperty, value);
        }

        private static void OnBehaviorsCollectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var newBehaviors = (BehaviorsCollection) e.NewValue;

            if (newBehaviors != null)
            {
                BehaviorCollection behaviors = Interaction.GetBehaviors(d);

                foreach (Behavior behavior in newBehaviors)
                {
                    behaviors.Add((Behavior)behavior.Clone());
                }
            }
        }
    }

    public class BehaviorsCollection : Collection<Behavior>
    {
    }
}