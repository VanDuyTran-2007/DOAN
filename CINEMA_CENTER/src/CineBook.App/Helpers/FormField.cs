using System.Windows;

namespace CineBook.Helpers;

public static class FormField
{
    public static readonly DependencyProperty HasInputProperty = DependencyProperty.RegisterAttached(
        "HasInput", typeof(bool), typeof(FormField), new PropertyMetadata(false));

    public static bool GetHasInput(DependencyObject element) => (bool)element.GetValue(HasInputProperty);
    public static void SetHasInput(DependencyObject element, bool value) => element.SetValue(HasInputProperty, value);
}
