namespace AuthX.Core.DTOs.QR;

public class GenerateQRDto
{
    public long BatchId { get; set; }
}

public class QRGenerationResultDto
{
    public long   GenerationId   { get; set; }
    public long   BatchId        { get; set; }
    public int    TotalGenerated { get; set; }
    public string Message        { get; set; } = null!;
}

public class CreatePrintJobDto
{
    public long   BatchId    { get; set; }
    public string FileFormat { get; set; } = "PDF"; // PDF | ZPL | CSV
}

public class PrintJobDto
{
    public long      PrintJobId   { get; set; }
    public long      BatchId      { get; set; }
    public int       TotalItems   { get; set; }
    public int       PrintedCount { get; set; }
    public string    Status       { get; set; } = null!;
    public string?   FileUrl      { get; set; }
    public string    FileFormat   { get; set; } = null!;
    public string?   ErrorMessage { get; set; }
    public DateTime  CreatedAt    { get; set; }
    public DateTime? CompletedAt  { get; set; }
}

public class ProductItemDto
{
    public long    ItemId    { get; set; }
    public string  SerialNo  { get; set; } = null!;
    public string  QRCode    { get; set; } = null!;
    public string  Status    { get; set; } = null!;
    public string  PrintStatus { get; set; } = null!;
}