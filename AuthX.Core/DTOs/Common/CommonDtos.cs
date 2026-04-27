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