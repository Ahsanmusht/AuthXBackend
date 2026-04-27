namespace AuthX.Core.DTOs.Dashboard;

public class DashboardStatsDto
{
    public int  TotalProducts    { get; set; }
    public int  TotalBatches     { get; set; }
    public long TotalQRGenerated { get; set; }
    public long TotalDispatched  { get; set; }
    public long TotalScans       { get; set; }
    public long TotalClaims      { get; set; }
    public long OpenClaims       { get; set; }
    public long ResolvedClaims   { get; set; }
    public long TodayScans       { get; set; }
    public long TodayClaims      { get; set; }
}

public class ScanTrendDto
{
    public string Date  { get; set; } = null!;
    public long   Count { get; set; }
}

public class ClaimTrendDto
{
    public string Date   { get; set; } = null!;
    public long   Count  { get; set; }
    public string Status { get; set; } = null!;
}