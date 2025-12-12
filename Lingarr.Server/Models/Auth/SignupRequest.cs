namespace Lingarr.Server.Models.Auth;

public class SignupRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}
