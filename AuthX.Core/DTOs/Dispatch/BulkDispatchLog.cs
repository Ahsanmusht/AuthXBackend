namespace AuthX.Core.Entities;

public class BulkDispatchLog
{
    public long    LogId           { get; set; }
    public int     CompanyId       { get; set; }
    public int     DispatchedBy    { get; set; }
    public string  BatchIds        { get; set; } = null!;  // JSON array
    public int     TotalDispatched { get; set; }
    public int     TotalSkipped    { get; set; }
    public string? Location        { get; set; }
    public string? SapInvoiceNo    { get; set; }
    public DateTime  StartedAt    { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt  { get; set; }
    public string  Status         { get; set; } = "Processing"; // Processing | Done | Failed
}