using System.Windows.Controls;

namespace CineBook.Controls;

public partial class SignUpForm : UserControl
{
    public SignUpForm() => InitializeComponent();

    private void OnBirthDateChanged(object sender, TextChangedEventArgs e)
    {
        var input = (TextBox)sender;
        var digitsBeforeCaret = input.Text.Take(input.CaretIndex).Count(char.IsAsciiDigit);
        var digits = new string(input.Text.Where(char.IsAsciiDigit).Take(8).ToArray());
        var formatted = digits.Length switch
        {
            > 4 => $"{digits[..2]}/{digits[2..4]}/{digits[4..]}",
            > 2 => $"{digits[..2]}/{digits[2..]}",
            _ => digits
        };
        if (input.Text == formatted) return;

        input.SetCurrentValue(TextBox.TextProperty, formatted);
        input.CaretIndex = Math.Min(formatted.Length, digitsBeforeCaret + (digitsBeforeCaret > 2 ? 1 : 0) + (digitsBeforeCaret > 4 ? 1 : 0));
    }
}
