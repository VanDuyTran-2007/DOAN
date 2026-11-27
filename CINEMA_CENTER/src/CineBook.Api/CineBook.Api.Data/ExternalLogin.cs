namespace CineBook.Api.Data;

public class ExternalLogin
{
    public string Provider { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;
}
