namespace AuthX.Core.DTOs.Users;

public class UserListDto
{
    public int         UserId    { get; set; }
    public string      Name      { get; set; } = null!;
    public string      Email     { get; set; } = null!;
    public string?     Phone     { get; set; }
    public bool        IsActive  { get; set; }
    public DateTime    CreatedAt { get; set; }
    public List<string> Roles   { get; set; } = new();
}

public class UserDetailDto : UserListDto { }

public class CreateUserDto
{
    public string      Name      { get; set; } = null!;
    public string      Email     { get; set; } = null!;
    public string?     Phone     { get; set; }
    public string      Password  { get; set; } = null!;
    public List<int>   RoleIds   { get; set; } = new();
}

public class UpdateUserDto
{
    public string      Name     { get; set; } = null!;
    public string?     Phone    { get; set; }
    public List<int>   RoleIds  { get; set; } = new();
}

public class RoleDto
{
    public int    RoleId   { get; set; }
    public string RoleName { get; set; } = null!;
    public bool   IsActive { get; set; }
}