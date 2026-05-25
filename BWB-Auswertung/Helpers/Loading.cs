using System.Windows;
using System.Windows.Controls;

namespace BWB_Auswertung.Helpers
{
    public static class Loading
    {
        public static readonly DependencyProperty IsBusyProperty =
            DependencyProperty.RegisterAttached(
                "IsBusy",
                typeof(bool),
                typeof(Loading),
                new PropertyMetadata(false, OnIsBusyChanged));

        public static bool GetIsBusy(DependencyObject d) => (bool)d.GetValue(IsBusyProperty);

        public static void SetIsBusy(DependencyObject d, bool value) => d.SetValue(IsBusyProperty, value);

        private static void OnIsBusyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Control c)
            {
                c.IsEnabled = !(bool)e.NewValue;
            }
        }
    }
}
