namespace BffAgenda.Application.Auth.DTOs;

public class AuthResponseDto
{
    public string AccessToken { get; set; }
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public AuthUserDto User { get; set; }
}

public class AuthUserDto
{
    public string? Username { get; set; }
}
