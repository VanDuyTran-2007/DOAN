using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CineBook.Models;

namespace CineBook.Controls;

public partial class CinematicTrailerPlayer : UserControl
{
    private readonly DispatcherTimer _progress = new() { Interval = TimeSpan.FromMilliseconds(200) };
    private readonly DispatcherTimer _loadTimeout = new() { Interval = TimeSpan.FromSeconds(12) };
    private MediaElement? _media;
    private Uri? _source;
    private Window? _fullscreen;
    private bool _preview;
    private bool _playing;
    private bool _opened;
    private bool _updatingProgress;

    public CinematicTrailerPlayer()
    {
        InitializeComponent();
        _progress.Tick += RefreshProgress;
        _loadTimeout.Tick += (_, _) => ShowCover("Trailer tải quá lâu. Bạn có thể thử lại.");
        Unloaded += (_, _) => Release();
    }

    internal void Prepare(Movie movie, bool preview = false)
    {
        Release();
        _preview = preview;
        var cinematic = MovieCinematicMedia.For(movie);
        _source = MovieCinematicMedia.ResolveUri(preview ? cinematic.PreviewVideoUrl : movie.TrailerUrl);
        ControlsPanel.Visibility = preview ? Visibility.Collapsed : Visibility.Visible;
        PlaybackButton.IsEnabled = _source is not null;
        FullscreenButton.IsEnabled = _source is not null;
        CoverTitle.Text = movie.Title;
        CoverGlow.Background = CinematicAmbientLayer.CreateGlow(cinematic);
        ShowBackdrop(movie);
        SetStatus(_source is null ? "Trailer chưa có sẵn" : "");
    }

    private void ShowBackdrop(Movie movie)
    {
        Backdrop.Source = null;
        var uri = MovieCinematicMedia.ResolveUri(movie.BackdropUrl);
        if (uri is null) return;
        try
        {
            Backdrop.Source = new BitmapImage(uri);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or IOException or NotSupportedException)
        {
            Backdrop.Source = null;
        }
    }

    private void OnImageFailed(object sender, ExceptionRoutedEventArgs e) => Backdrop.Source = null;

    internal void Play()
    {
        if (_source is null) return;
        var media = _media ?? CreateMedia();
        try
        {
            if (media.Source is null)
            {
                SetStatus("Đang tải trailer…");
                _loadTimeout.Start();
                media.Source = _source;
                if (!ReferenceEquals(_media, media)) return;
            }
            media.Play();
            if (!ReferenceEquals(_media, media)) return;
            _playing = true;
            PlaybackButton.Content = "Tạm dừng";
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or ArgumentException)
        {
            ShowCover("Không phát được trailer. Bạn có thể thử lại.");
        }
    }

    private MediaElement CreateMedia()
    {
        var media = new MediaElement
        {
            LoadedBehavior = MediaState.Manual,
            UnloadedBehavior = MediaState.Manual,
            Stretch = Stretch.Uniform,
            Volume = Volume.Value,
            IsMuted = _preview,
            Opacity = 0,
        };
        _media = media;
        media.MediaOpened += OnMediaOpened;
        media.MediaEnded += OnMediaEnded;
        media.MediaFailed += OnMediaFailed;
        media.BufferingStarted += OnBufferingStarted;
        media.BufferingEnded += OnBufferingEnded;
        VideoSurface.Children.Add(media);
        return media;
    }

    internal void Release()
    {
        _source = null;
        ShowCover("");
        Backdrop.Source = null;
        PlaybackButton.IsEnabled = false;
        FullscreenButton.IsEnabled = false;
        _fullscreen?.Close();
    }

    private void ShowCover(string message)
    {
        _loadTimeout.Stop();
        _progress.Stop();
        var media = _media;
        _media = null;
        _playing = false;
        _opened = false;
        if (media is not null)
        {
            media.MediaOpened -= OnMediaOpened;
            media.MediaEnded -= OnMediaEnded;
            media.MediaFailed -= OnMediaFailed;
            media.BufferingStarted -= OnBufferingStarted;
            media.BufferingEnded -= OnBufferingEnded;
            media.Close();
            media.Source = null;
            VideoSurface.Children.Remove(media);
        }
        Cover.Visibility = Visibility.Visible;
        Seek.IsEnabled = false;
        _updatingProgress = true;
        Seek.Value = 0;
        Seek.Maximum = 1;
        _updatingProgress = false;
        TimeText.Text = "0:00 / --:--";
        PlaybackButton.Content = "Phát";
        SetStatus(message);
    }

    private void SetStatus(string message)
    {
        StatusText.Text = message;
        StatusText.Visibility = message.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnMediaOpened(object sender, RoutedEventArgs e)
    {
        if (sender is not MediaElement media || !ReferenceEquals(media, _media)) return;
        _loadTimeout.Stop();
        _opened = true;
        var duration = media.NaturalDuration;
        if (duration.HasTimeSpan && duration.TimeSpan > TimeSpan.Zero)
        {
            Seek.Maximum = duration.TimeSpan.TotalSeconds;
            Seek.IsEnabled = !_preview;
        }
        media.Opacity = 1;
        Cover.Visibility = Visibility.Hidden;
        SetStatus("");
        _progress.Start();
        RefreshProgress(this, EventArgs.Empty);
    }

    private void OnMediaEnded(object sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(sender, _media)) ShowCover("");
    }

    private void OnMediaFailed(object? sender, ExceptionRoutedEventArgs e)
    {
        if (ReferenceEquals(sender, _media)) ShowCover("Không phát được trailer. Bạn có thể thử lại.");
    }

    private void OnBufferingStarted(object sender, RoutedEventArgs e)
    {
        if (!ReferenceEquals(sender, _media)) return;
        SetStatus("Đang tải trailer…");
        _loadTimeout.Start();
    }

    private void OnBufferingEnded(object sender, RoutedEventArgs e)
    {
        if (!_opened || !ReferenceEquals(sender, _media)) return;
        _loadTimeout.Stop();
        SetStatus("");
    }

    private void RefreshProgress(object? sender, EventArgs e)
    {
        if (_media is not { } media || !_opened) return;
        var position = media.Position;
        if (_preview && position >= TimeSpan.FromSeconds(10))
        {
            ShowCover("");
            return;
        }
        if (_preview) return;

        var duration = media.NaturalDuration;
        _updatingProgress = true;
        if (duration.HasTimeSpan && !Seek.IsMouseCaptureWithin)
            Seek.Value = Math.Clamp(position.TotalSeconds, 0, Seek.Maximum);
        _updatingProgress = false;
        TimeText.Text = $"{FormatTime(position)} / {(duration.HasTimeSpan ? FormatTime(duration.TimeSpan) : "--:--")}";
    }

    private static string FormatTime(TimeSpan time) =>
        time.TotalHours >= 1 ? time.ToString(@"h\:mm\:ss") : $"{(int)time.TotalMinutes}:{time.Seconds:00}";

    private void OnSeek(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_media is { } media && _opened && !_updatingProgress)
            media.Position = TimeSpan.FromSeconds(e.NewValue);
    }

    private void OnVolume(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_media is { } media) media.Volume = e.NewValue;
    }

    private void OnPlayback(object sender, RoutedEventArgs e)
    {
        if (_playing && _media is { } media)
        {
            media.Pause();
            _playing = false;
            PlaybackButton.Content = "Phát";
        }
        else
        {
            Play();
        }
    }

    private void OnFullscreen(object sender, RoutedEventArgs e)
    {
        if (_fullscreen is { } existing)
        {
            existing.Close();
            return;
        }

        var window = new Window
        {
            Owner = Window.GetWindow(this),
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            WindowState = WindowState.Maximized,
            Background = Brushes.Black,
            ShowInTaskbar = false,
            Title = "Trailer CineBook",
        };
        _fullscreen = window;
        InlineSurface.Content = null;
        window.Content = PlayerSurface;
        AutomationProperties.SetName(FullscreenButton, "Thoát toàn màn hình");
        window.PreviewKeyDown += (_, args) =>
        {
            if (args.Key == Key.Escape) window.Close();
        };
        window.Closed += (_, _) =>
        {
            window.Content = null;
            InlineSurface.Content = PlayerSurface;
            _fullscreen = null;
            AutomationProperties.SetName(FullscreenButton, "Toàn màn hình");
            if (IsLoaded) FullscreenButton.Focus();
        };
        window.Show();
        FullscreenButton.Focus();
    }
}
