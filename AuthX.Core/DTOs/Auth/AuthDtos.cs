namespace AuthX.Core.DTOs.Auth;

public class LoginRequestDto
{
    public string Email    { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class LoginResponseDto
{
    public string      AccessToken         { get; set; } = null!;
    public string      RefreshToken        { get; set; } = null!;
    public DateTime    AccessTokenExpiry   { get; set; }
    public UserInfoDto User                { get; set; } = null!;
}

public class RefreshTokenRequestDto
{
    public string AccessToken  { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
}

public class UserInfoDto
{
    public int          UserId    { get; set; }
    public int          CompanyId { get; set; }
    public string       Name      { get; set; } = null!;
    public string       Email     { get; set; } = null!;
    public List<string> Roles     { get; set; } = new();
    public string?      CompanyName { get; set; }
    public string?      CompanyLogo { get; set; }
}