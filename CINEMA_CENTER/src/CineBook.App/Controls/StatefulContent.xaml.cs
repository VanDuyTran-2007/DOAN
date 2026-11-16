using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace CineBook.Controls;

/// <summary>Khung hiển thị 4 trạng thái cho một section: Loading / Empty / Error / Nội dung.</summary>
[ContentProperty(nameof(Body))]
public partial class StatefulContent : UserControl
{
    public StatefulContent() => InitializeComponent();

    public static readonly DependencyProperty BodyProperty = DependencyProperty.Register(nameof(Body), typeof(object), typeof(StatefulContent));
    public static readonly DependencyProperty SkeletonProperty = DependencyProperty.Register(nameof(Skeleton), typeof(object), typeof(StatefulContent));
    public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(StatefulContent), new PropertyMetadata(false, OnStateChanged));
    public static readonly DependencyProperty IsEmptyProperty = DependencyProperty.Register(nameof(IsEmpty), typeof(bool), typeof(StatefulContent), new PropertyMetadata(false, OnStateChanged));
    public static readonly DependencyProperty IsErrorProperty = DependencyProperty.Register(nameof(IsError), typeof(bool), typeof(StatefulContent), new PropertyMetadata(false, OnStateChanged));
    public static readonly DependencyProperty ShowBodyProperty = DependencyProperty.Register(nameof(ShowBody), typeof(bool), typeof(StatefulContent), new PropertyMetadata(true));
    public static readonly DependencyProperty EmptyTextProperty = DependencyProperty.Register(nameof(EmptyText), typeof(string), typeof(StatefulContent), new PropertyMetadata("Chưa có dữ liệu"));
    public static readonly DependencyProperty ErrorTextProperty = DependencyProperty.Register(nameof(ErrorText), typeof(string), typeof(StatefulContent));
    public static readonly DependencyProperty RetryCommandProperty = DependencyProperty.Register(nameof(RetryCommand), typeof(ICommand), typeof(StatefulContent));

    public object? Body { get => GetValue(BodyProperty); set => SetValue(BodyProperty, value); }
    public object? Skeleton { get => GetValue(SkeletonProperty); set => SetValue(SkeletonProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value); }
    public bool IsEmpty { get => (bool)GetValue(IsEmptyProperty); set => SetValue(IsEmptyProperty, value); }
    public bool IsError { get => (bool)GetValue(IsErrorProperty); set => SetValue(IsErrorProperty, value); }
    public bool ShowBody { get => (bool)GetValue(ShowBodyProperty); private set => SetValue(ShowBodyProperty, value); }
    public string EmptyText { get => (string)GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }
    public string? ErrorText { get => (string?)GetValue(ErrorTextProperty); set => SetValue(ErrorTextProperty, value); }
    public ICommand? RetryCommand { get => (ICommand?)GetValue(RetryCommandProperty); set => SetValue(RetryCommandProperty, value); }

    // Nội dung chỉ hiện khi không ở trạng thái loading/empty/error.
    private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var c = (StatefulContent)d;
        c.ShowBody = !(c.IsLoading || c.IsEmpty || c.IsError);
    }
}
