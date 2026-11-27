using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CineBook.ViewModels;

namespace CineBook.Controls;

public partial class SignInForm : UserControl
{
    private SignInFormViewModel? _form;

    public SignInForm()
    {
        InitializeComponent();
        Loaded += (_, _) => WatchForm();
        Unloaded += (_, _) => WatchForm(active: false);
        DataContextChanged += (_, _) => WatchForm(IsLoaded);
    }

    private void WatchForm(bool active = true)
    {
        if (_form is not null) _form.PropertyChanged -= OnFormChanged;
        _form = active ? (DataContext as AuthViewModel)?.SignIn : null;
        if (_form is not null) _form.PropertyChanged += OnFormChanged;
    }

    private void OnFormChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SignInFormViewModel.CredentialsRejected) ||
            _form?.CredentialsRejected != true || !SystemParameters.ClientAreaAnimation) return;

        var shake = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.Stop };
        shake.KeyFrames.Add(new LinearDoubleKeyFrame(-5, TimeSpan.FromMilliseconds(40)));
        shake.KeyFrames.Add(new LinearDoubleKeyFrame(5, TimeSpan.FromMilliseconds(95)));
        shake.KeyFrames.Add(new LinearDoubleKeyFrame(-3, TimeSpan.FromMilliseconds(145)));
        shake.KeyFrames.Add(new LinearDoubleKeyFrame(3, TimeSpan.FromMilliseconds(195)));
        shake.KeyFrames.Add(new LinearDoubleKeyFrame(0, TimeSpan.FromMilliseconds(245)));
        IdentityOffset.BeginAnimation(TranslateTransform.XProperty, shake);
        PasswordOffset.BeginAnimation(TranslateTransform.XProperty, shake);
    }
}
