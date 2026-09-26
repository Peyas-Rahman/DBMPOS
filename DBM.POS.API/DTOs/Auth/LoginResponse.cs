namespace DBM.POS.API.DTOs.Auth;

public class LoginResponse
{
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }

    public Guid UserId { get; set; }
    public string Username { get; set; } = null!;
    public string FullName { get; set; } = null!;

    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}