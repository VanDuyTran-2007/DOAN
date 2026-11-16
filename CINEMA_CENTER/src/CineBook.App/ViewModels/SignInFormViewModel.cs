using System.ComponentModel.DataAnnotations;
using CineBook.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CineBook.ViewModels;

/// <summary>Form đăng nhập. Kế thừa ObservableValidator (INotifyDataErrorInfo) để hiện lỗi dưới từng ô.</summary>
public partial class SignInFormViewModel : ObservableValidator
{
    [ObservableProperty, NotifyDataErrorInfo]
    [Required(ErrorMessage = "Vui lòng nhập email hoặc SĐT")]
    [CustomValidation(typeof(ValidationRules), nameof(ValidationRules.EmailOrPhone))]
    private string _identity = "";

    [ObservableProperty, NotifyDataErrorInfo]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    private string _password = "";

    [ObservableProperty] private bool _rememberMe;
    [ObservableProperty] private bool _showPassword;

    public bool Validate() { ValidateAllProperties(); return !HasErrors; }
}
