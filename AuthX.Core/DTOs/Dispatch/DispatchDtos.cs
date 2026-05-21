namespace AuthX.Core.DTOs.Dispatch;


public class DispatchResultDto
{
    public long     DispatchId   { get; set; }
    public string   SerialNo     { get; set; } = null!;
    public string   ProductName  { get; set; } = null!;
    public DateTime DispatchDate { get; set; }
    public DateTime WarrantyEnd  { get; set; }
    public string?  SapInvoiceNo { get; set; }
}

public class DispatchListDto
{
    public long      DispatchId   { get; set; }
    public string    SerialNo     { get; set; } = null!;
    public string    QRCode       { get; set; } = null!;
    public string    ProductName  { get; set; } = null!;
    public string?   Location     { get; set; }
    public DateTime  DispatchDate { get; set; }
    public string    ScannedBy    { get; set; } = null!;
    public DateTime? WarrantyEnd  { get; set; }
    public string?   SapInvoiceNo { get; set; }
}

/// <summary>Request body for bulk batch dispatch</summary>
public class BulkBatchDispatchDto
{
    /// <summary>List of batch IDs to dispatch (max 50)</summary>
    public List<long> BatchIds     { get; set; } = new();

    /// <summary>Optional dispatch location for all items</summary>
    public string?    Location     { get; set; }

    /// <summary>Optional SAP invoice number for all items</summary>
    public string?    SapInvoiceNo { get; set; }
}

/// <summary>Result of bulk batch dispatch operation</summary>
public class BulkDispatchResultDto
{
    public long   LogId           { get; set; }
    public int    TotalDispatched { get; set; }
    public int    TotalSkipped    { get; set; }
    public string Status          { get; set; } = null!;

    /// <summary>Per-batch breakdown</summary>
    public List<BatchDispatchSummary> Batches { get; set; } = new();
}

/// <summary>Per-batch result summary</summary>
public class BatchDispatchSummary
{
    public long   BatchId     { get; set; }
    public string BatchNo     { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public int    Dispatched  { get; set; }
    public int    Skipped     { get; set; }
    public string Status      { get; set; } = null!;
}

/// <summary>Batch available for dispatch — shown in bulk dispatch UI</summary>
public class AvailableBatchDto
{
    public long     BatchId         { get; set; }
    public string   BatchNo         { get; set; } = null!;
    public string   ProductName     { get; set; } = null!;
    public string   CategoryName    { get; set; } = null!;
    public DateOnly ProductionDate  { get; set; }
    public string   Status          { get; set; } = null!;

    /// <summary>Total QR codes in batch</summary>
    public int      TotalItems      { get; set; }

    /// <summary>Items ready to dispatch (Generated + Printed)</summary>
    public int      PendingItems    { get; set; }

    /// <summary>Already dispatched items</summary>
    public int      DispatchedItems { get; set; }

    public string?  ColorName       { get; set; }
    public string?  ColorHexCode    { get; set; }
    public string?  ModelNo         { get; set; }
    public DateTime CreatedAt       { get; set; }
}