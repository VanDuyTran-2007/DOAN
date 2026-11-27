using System.Windows;
using System.Windows.Controls;
using CineBook.ViewModels;
using Microsoft.Win32;

namespace CineBook.Views;

public partial class AccountView : UserControl
{
    public AccountView() => InitializeComponent();

    private async void OnUploadAvatar(object sender, RoutedEventArgs e)
    {
        if (DataContext is not AccountViewModel vm || !vm.CanChangeAvatar) return;
        var dialog = new OpenFileDialog { Title = "Chọn ảnh đại diện", Filter = "Ảnh PNG và JPEG|*.png;*.jpg;*.jpeg" };
        if (dialog.ShowDialog(Window.GetWindow(this)) == true) await vm.UploadAsync(dialog.FileName);
    }

    private void OnVerifyPassword(object sender, RoutedEventArgs e) => OpenPasswordWindow(change: false);
    private void OnChangePassword(object sender, RoutedEventArgs e) => OpenPasswordWindow(change: true);

    private async void OnSetInitialPassword(object sender, RoutedEventArgs e)
    {
        if (DataContext is AccountViewModel account &&
            await account.SetInitialPasswordAsync(InitialPassword.Password, InitialConfirmation.Password))
        {
            InitialPassword.Clear();
            InitialConfirmation.Clear();
        }
    }

    private void OpenPasswordWindow(bool change)
    {
        if (DataContext is not AccountViewModel vm) return;
        new AccountPasswordWindow(vm, change) { Owner = Window.GetWindow(this) }.ShowDialog();
    }
}
