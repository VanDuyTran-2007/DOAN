using System.ComponentModel.DataAnnotations;
using CineBook.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CineBook.ViewModels;

/// <summary>Form đăng ký + validate: email, SĐT VN 10 số, mật khẩu ≥ 8 ký tự, 2 mật khẩu khớp nhau.</summary>
public partial class SignUpFormViewModel : ObservableValidator
{
    public SignUpFormViewModel() => ErrorsChanged += (_, _) => OnPropertyChanged(nameof(CanSubmit));

    [ObservableProperty, NotifyDataErrorInfo, NotifyPropertyChangedFor(nameof(CanSubmit))]
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [MinLength(2, ErrorMessage = "Họ tên quá ngắn")]
    private string _fullName = "";

    [ObservableProperty, NotifyDataErrorInfo, NotifyPropertyChangedFor(nameof(CanSubmit))]
    [Required(ErrorMessage = "Vui lòng nhập email")]
    [RegularExpression(ValidationRules.EmailPattern, ErrorMessage = "Email không đúng định dạng")]
    private string _email = "";

    [ObservableProperty, NotifyDataErrorInfo, NotifyPropertyChangedFor(nameof(CanSubmit))]
    [Required(ErrorMessage = "Vui lòng nhập SĐT")]
    [RegularExpression(ValidationRules.PhonePattern, ErrorMessage = "SĐT phải gồm 10 số, bắt đầu bằng 0")]
    private string _phone = "";

    [ObservableProperty, NotifyDataErrorInfo, NotifyPropertyChangedFor(nameof(CanSubmit))]
    [Required(ErrorMessage = "Vui lòng nhập ngày sinh")]
    [CustomValidation(typeof(ValidationRules), nameof(ValidationRules.BirthDate))]
    private string _birthDate = "";

    [ObservableProperty, NotifyDataErrorInfo, NotifyPropertyChangedFor(nameof(CanSubmit))]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [MinLength(8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự")]
    private string _password = "";

    [ObservableProperty, NotifyDataErrorInfo, NotifyPropertyChangedFor(nameof(CanSubmit))]
    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu")]
    [CustomValidation(typeof(SignUpFormViewModel), nameof(ValidateConfirm))]
    private string _confirmPassword = "";

    [ObservableProperty, NotifyDataErrorInfo, NotifyPropertyChangedFor(nameof(CanSubmit))]
    [CustomValidation(typeof(ValidationRules), nameof(ValidationRules.AcceptTerms))]
    private bool _acceptTerms;

    [ObservableProperty] private bool _showPassword;

    public bool CanSubmit => !HasErrors && AcceptTerms && !string.IsNullOrWhiteSpace(FullName)
        && !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Phone)
        && !string.IsNullOrWhiteSpace(BirthDate) && Password.Length >= 8 && ConfirmPassword == Password;

    public static ValidationResult? ValidateConfirm(string? value, ValidationContext ctx)
        => ctx.ObjectInstance is SignUpFormViewModel vm && value != vm.Password
            ? new ValidationResult("Hai mật khẩu không khớp")
            : ValidationResult.Success;

    // Đổi mật khẩu thì kiểm tra lại ô nhập lại (nếu đã nhập).
    partial void OnPasswordChanged(string value)
    {
        if (!string.IsNullOrEmpty(ConfirmPassword)) ValidateProperty(ConfirmPassword, nameof(ConfirmPassword));
    }

    public bool Validate() { ValidateAllProperties(); return !HasErrors; }
}
