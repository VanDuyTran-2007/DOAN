using System.ComponentModel.DataAnnotations;

namespace CineBook.Api.Controllers;

public record SignUpDto(
    [Required, StringLength(100, MinimumLength = 2)] string FullName,
    [Required, StringLength(254), EmailAddress] string Email,
    [Required, RegularExpression("^0[0-9]{9}$")] string Phone,
    [Required, StringLength(256, MinimumLength = 8)] string Password,
    DateTime? BirthDate = null);
