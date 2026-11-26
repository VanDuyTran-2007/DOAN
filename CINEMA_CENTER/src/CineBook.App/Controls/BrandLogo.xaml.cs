using System.Windows;
using System.Windows.Controls;

namespace CineBook.Controls;

/// <summary>Display-only brand logo. Bind BrandId to Brand.Id and BrandName to Brand.Name.</summary>
public partial class BrandLogo : UserControl
{
    public BrandLogo() => InitializeComponent();

    public static readonly DependencyProperty BrandIdProperty =
        DependencyProperty.Register(nameof(BrandId), typeof(string), typeof(BrandLogo));
    public static readonly DependencyProperty BrandNameProperty =
        DependencyProperty.Register(nameof(BrandName), typeof(string), typeof(BrandLogo));

    public string? BrandId { get => (string?)GetValue(BrandIdProperty); set => SetValue(BrandIdProperty, value); }
    public string? BrandName { get => (string?)GetValue(BrandNameProperty); set => SetValue(BrandNameProperty, value); }
}
