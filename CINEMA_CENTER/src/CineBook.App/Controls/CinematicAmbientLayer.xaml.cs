using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CineBook.Models;

namespace CineBook.Controls;

public partial class CinematicAmbientLayer : UserControl
{
    public CinematicAmbientLayer() => InitializeComponent();

    internal void ShowMovie(Movie? movie)
    {
        if (movie is null)
        {
            Fade(Previous, Previous.Opacity, 0);
            Fade(Current, Current.Opacity, 0);
            return;
        }

        if (Current.Opacity > .01)
        {
            Previous.Background = Current.Background;
            Fade(Previous, Current.Opacity, 0);
        }
        Current.Background = CreateGlow(MovieCinematicMedia.For(movie));
        Fade(Current, 0, 1);
    }

    private static void Fade(Border layer, double from, double to)
    {
        layer.BeginAnimation(OpacityProperty, null);
        layer.Opacity = to;
        if (!SystemParameters.ClientAreaAnimation || from == to) return;

        layer.BeginAnimation(OpacityProperty, new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(750))
        {
            FillBehavior = FillBehavior.Stop,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
        });
    }

    internal static RadialGradientBrush CreateGlow(MovieCinematicMedia media)
    {
        var glow = new RadialGradientBrush
        {
            Center = new Point(.48, .30),
            GradientOrigin = new Point(.37, .20),
            RadiusX = .78,
            RadiusY = .79,
        };
        glow.GradientStops.Add(new GradientStop(WithAlpha(media.AccentColor, 106), 0));
        glow.GradientStops.Add(new GradientStop(WithAlpha(media.PrimaryColor, 104), .24));
        glow.GradientStops.Add(new GradientStop(WithAlpha(media.SecondaryColor, 85), .61));
        glow.GradientStops.Add(new GradientStop(Colors.Transparent, 1));
        glow.Freeze();
        return glow;
    }

    private static Color WithAlpha(string hex, byte alpha)
    {
        Color color;
        try
        {
            color = (Color)ColorConverter.ConvertFromString(hex);
        }
        catch (FormatException)
        {
            color = Colors.SlateBlue;
        }
        color.A = alpha;
        return color;
    }
}
