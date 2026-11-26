using System;

namespace CineBook.Api.Controllers;

public record SignUpDto(string FullName, string Email, string Phone, string Password, DateTime? BirthDate = null);
