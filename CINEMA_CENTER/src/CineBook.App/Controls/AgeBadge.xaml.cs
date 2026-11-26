using System.Windows;
using System.Windows.Controls;

namespace CineBook.Controls;

/// <summary>Display-only age rating badge. Bind Code to Movie.AgeRating (enum or string).</summary>
public partial class AgeBadge : UserControl
{
    public AgeBadge() => InitializeComponent();

    public static readonly DependencyProperty CodeProperty =
        DependencyProperty.Register(nameof(Code), typeof(object), typeof(AgeBadge));

    public object? Code { get => GetValue(CodeProperty); set => SetValue(CodeProperty, value); }
}
