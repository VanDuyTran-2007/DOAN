using System.Windows;
using System.Windows.Controls;

namespace CineBook.Helpers;

/// <summary>
/// PasswordBox.Password KHÔNG phải DependencyProperty (cố ý, để tránh giữ mật khẩu dạng chuỗi trong binding),
/// nên không bind trực tiếp được. Attached property này đồng bộ 2 chiều giữa PasswordBox và ViewModel.
/// Dùng: helpers:PasswordHelper.Attach="True" helpers:PasswordHelper.Password="{Binding Password, Mode=TwoWay}"
/// </summary>
public static class PasswordHelper
{
    public static readonly DependencyProperty PasswordProperty = DependencyProperty.RegisterAttached(
        "Password", typeof(string), typeof(PasswordHelper),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPasswordPropertyChanged));

    public static readonly DependencyProperty AttachProperty = DependencyProperty.RegisterAttached(
        "Attach", typeof(bool), typeof(PasswordHelper), new PropertyMetadata(false, OnAttachChanged));

    // Cờ chống vòng lặp: ViewModel đổi → PasswordBox đổi → bắn PasswordChanged → ghi ngược lại ViewModel...
    private static readonly DependencyProperty IsUpdatingProperty = DependencyProperty.RegisterAttached(
        "IsUpdating", typeof(bool), typeof(PasswordHelper));

    public static string GetPassword(DependencyObject d) => (string)d.GetValue(PasswordProperty);
    public static void SetPassword(DependencyObject d, string value) => d.SetValue(PasswordProperty, value);
    public static bool GetAttach(DependencyObject d) => (bool)d.GetValue(AttachProperty);
    public static void SetAttach(DependencyObject d, bool value) => d.SetValue(AttachProperty, value);

    private static void OnPasswordPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox box || (bool)d.GetValue(IsUpdatingProperty)) return;
        box.PasswordChanged -= OnPasswordChanged;
        box.Password = (string?)e.NewValue ?? string.Empty;
        box.PasswordChanged += OnPasswordChanged;
    }

    private static void OnAttachChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox box) return;
        if ((bool)e.OldValue) box.PasswordChanged -= OnPasswordChanged;
        if ((bool)e.NewValue) box.PasswordChanged += OnPasswordChanged;
    }

    private static void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        var box = (PasswordBox)sender;
        box.SetValue(IsUpdatingProperty, true);
        SetPassword(box, box.Password);
        box.SetValue(IsUpdatingProperty, false);
    }
}
