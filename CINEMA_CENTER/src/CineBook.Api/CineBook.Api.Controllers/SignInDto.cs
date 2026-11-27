using System.ComponentModel.DataAnnotations;

namespace CineBook.Api.Controllers;

public record SignInDto(
    [Required, StringLength(254)] string Identity,
    [Required, StringLength(256)] string Password);
