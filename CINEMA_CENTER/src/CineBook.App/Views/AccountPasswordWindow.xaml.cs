using System.Windows;
using CineBook.Services;
using CineBook.ViewModels;

namespace CineBook.Views;

public partial class AccountPasswordWindow : Window
{
    private readonly AccountViewModel _account;
    private readonly bool _change;
    private readonly CancellationTokenSource _lifetime = new();

    public AccountPasswordWindow(AccountViewModel account, bool change)
    {
        InitializeComponent();
        _account = account;
        _change = change;
        Title = Heading.Text = change ? "Đổi mật khẩu" : "Xác thực mật khẩu";
        ChangeInputs.Visibility = change ? Visibility.Visible : Visibility.Collapsed;
        Submit.Content = change ? "Đổi mật khẩu" : "Xác thực";
        Loaded += (_, _) => Current.Focus();
    }

    private async void OnSubmit(object sender, RoutedEventArgs e)
    {
        Error.Text = "";
        if (Current.Password.Length == 0)
        {
            Error.Text = "Hãy nhập mật khẩu hiện tại.";
            return;
        }
        if (_change && (NewPassword.Password.Length < 8 || NewPassword.Password != Confirmation.Password))
        {
            Error.Text = "Mật khẩu mới cần ít nhất 8 ký tự và khớp ô xác nhận.";
            return;
        }

        Submit.IsEnabled = PasswordInputs.IsEnabled = false;
        var entered = Current.Password;
        var ct = _lifetime.Token;
        try
        {
            if (_change)
            {
                await _account.ChangePasswordAsync(entered, NewPassword.Password, Confirmation.Password, ct);
                if (!ct.IsCancellationRequested) Close();
            }
            else
            {
                await _account.VerifyAsync(entered, ct);
                if (ct.IsCancellationRequested) return;
                Current.Clear();
                PasswordInputs.Visibility = Submit.Visibility = Visibility.Collapsed;
                VerifiedText.Text = entered;
                Verified.Visibility = Visibility.Visible;
                CloseButton.Focus();
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex)
        {
            if (ct.IsCancellationRequested) return;
            Error.Text = ex is AccountApiException ? ex.Message : "Không thể kết nối API. Vui lòng thử lại.";
            Current.Clear();
            NewPassword.Clear();
            Confirmation.Clear();
            Submit.IsEnabled = PasswordInputs.IsEnabled = _change || _account.CanVerify;
            Current.Focus();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _lifetime.Cancel();
        _lifetime.Dispose();
        Current.Clear();
        NewPassword.Clear();
        Confirmation.Clear();
        VerifiedText.Text = "";
        base.OnClosed(e);
    }
}
