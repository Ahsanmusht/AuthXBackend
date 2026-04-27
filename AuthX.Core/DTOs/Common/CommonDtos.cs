namespace AuthX.Core.DTOs.Common;

// ─── Pagination ───────────────────────────────────────────
public class PaginationParams
{
    private int _page     = 1;
    private int _pageSize = 20;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > 100 ? 100 : value < 1 ? 1 : value;
    }

    public string? Search    { get; set; }
    public string? SortBy    { get; set; }
    public bool    SortDesc  { get; set; } = false;
}

// ─── Date Range Filter ────────────────────────────────────
public class DateRangeFilter
{
    public DateTime? From { get; set; }
    public DateTime? To   { get; set; }
}

// ─── Lookup (Dropdown data) ───────────────────────────────
public class LookupDto
{
    public int    Id   { get; set; }
    public string Name { get; set; } = null!;
}

// ─── Dashboard Stats ──────────────────────────────────────
public class DashboardStatsDto
{
    public int  TotalProducts     { get; set; }
    public int  TotalBatches      { get; set; }
    public long TotalQRGenerated  { get; set; }
    public long TotalDispatched   { get; set; }
    public long TotalScans        { get; set; }
    public long TotalClaims       { get; set; }
    public long OpenClaims        { get; set; }
    public long ResolvedClaims    { get; set; }
    public long TodayScans        { get; set; }
    public long TodayClaims       { get; set; }
}

// ─── Auth DTOs ────────────────────────────────────────────
public class LoginRequestDto
{
    public string Email    { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class LoginResponseDto
{
    public string AccessToken         { get; set; } = null!;
    public string RefreshToken        { get; set; } = null!;
    public DateTime AccessTokenExpiry { get; set; }
    public UserInfoDto User           { get; set; } = null!;
}

public class RefreshTokenRequestDto
{
    public string AccessToken  { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
}

public class UserInfoDto
{
    public int           UserId    { get; set; }
    public int           CompanyId { get; set; }
    public string        Name      { get; set; } = null!;
    public string        Email     { get; set; } = null!;
    public List<string>  Roles     { get; set; } = new();
}

// ─── Notification DTO ─────────────────────────────────────
public class NotificationDto
{
    public long     NotificationId { get; set; }
    public string   Type           { get; set; } = null!;
    public string   Message        { get; set; } = null!;
    public long?    ReferenceId    { get; set; }
    public string?  ActionUrl      { get; set; }
    public bool     IsRead         { get; set; }
    public DateTime CreatedAt      { get; set; }
}