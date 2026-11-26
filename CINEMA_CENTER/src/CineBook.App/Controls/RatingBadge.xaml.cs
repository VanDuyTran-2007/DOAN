using System.Windows;
using System.Windows.Controls;

namespace CineBook.Controls;

/// <summary>Display-only score badge. Bind Score to Movie.Rating.</summary>
public partial class RatingBadge : UserControl
{
    public RatingBadge() => InitializeComponent();

    public static readonly DependencyProperty ScoreProperty =
        DependencyProperty.Register(nameof(Score), typeof(double), typeof(RatingBadge));

    public double Score { get => (double)GetValue(ScoreProperty); set => SetValue(ScoreProperty, value); }
}
