namespace DBM.POS.API.DTOs.Auth;

public class LoginRequest
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}