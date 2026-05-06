namespace AuthX.Core.DTOs.Menu;
 
public class MenuItemDto
{
    public int          MenuItemId { get; set; }
    public int?         ParentId   { get; set; }
    public string       Title      { get; set; } = null!;
    public string?      Url        { get; set; }
    public string?      Icon       { get; set; }
    public string       Type       { get; set; } = "item";
    public int          SortOrder  { get; set; }
    public bool         IsActive   { get; set; }
    public List<MenuItemDto> Children { get; set; } = new();
    public List<int>    AllowedRoleIds { get; set; } = new();
}
 
public class CreateMenuItemDto
{
    public int?    ParentId  { get; set; }
    public string  Title     { get; set; } = null!;
    public string? Url       { get; set; }
    public string? Icon      { get; set; }
    public string  Type      { get; set; } = "item";
    public int     SortOrder { get; set; } = 0;
    public List<int> RoleIds { get; set; } = new();
}
 
public class UpdateMenuItemDto : CreateMenuItemDto { }
 
public class MenuPermissionUpdateDto
{
    public int       MenuItemId { get; set; }
    public List<int> RoleIds    { get; set; } = new();
}
 
// What the frontend receives — role-filtered flat list that frontend builds tree from
public class UserMenuDto
{
    public int      MenuItemId { get; set; }
    public int?     ParentId   { get; set; }
    public string   Title      { get; set; } = null!;
    public string?  Url        { get; set; }
    public string?  Icon       { get; set; }
    public string   Type       { get; set; } = "item";
    public int      SortOrder  { get; set; }
    public List<UserMenuDto> Children { get; set; } = new();
}