using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CineBook.Helpers;

/// <summary>Các luật validate phía client (WARNING-BACKEND: server PHẢI validate lại, không tin client).</summary>
public static partial class ValidationRules
{
    public const string EmailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    /// <summary>SĐT Việt Nam 10 số, bắt đầu bằng 0.</summary>
    public const string PhonePattern = @"^0\d{9}$";

    [GeneratedRegex(EmailPattern)] private static partial Regex EmailRegex();
    [GeneratedRegex(PhonePattern)] private static partial Regex PhoneRegex();

    public static bool IsEmail(string? s) => s is not null && EmailRegex().IsMatch(s);
    public static bool IsPhone(string? s) => s is not null && PhoneRegex().IsMatch(s);

    public static ValidationResult? EmailOrPhone(string? value, ValidationContext _)
        => string.IsNullOrWhiteSpace(value) || IsEmail(value.Trim()) || IsPhone(value.Trim())
            ? ValidationResult.Success
            : new ValidationResult("Nhập email hợp lệ hoặc SĐT 10 số");

    public static ValidationResult? BirthDate(string? value, ValidationContext _)
    {
        if (string.IsNullOrWhiteSpace(value)) return ValidationResult.Success; // [Required] lo phần trống
        if (!DateTime.TryParseExact(value.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return new ValidationResult("Ngày sinh theo dạng dd/MM/yyyy");
        return d > DateTime.Today || d < DateTime.Today.AddYears(-120) ? new ValidationResult("Ngày sinh không hợp lệ") : ValidationResult.Success;
    }

    public static ValidationResult? AcceptTerms(bool value, ValidationContext _)
        => value ? ValidationResult.Success : new ValidationResult("Bạn cần đồng ý điều khoản");
}
