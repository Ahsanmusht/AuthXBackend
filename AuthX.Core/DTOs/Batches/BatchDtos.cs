namespace AuthX.Core.DTOs.Batches;

public class BatchListDto
{
    public long    BatchId       { get; set; }
    public string  BatchNo       { get; set; } = null!;
    public string  ProductName   { get; set; } = null!;
    public string  CategoryName  { get; set; } = null!;
    public DateOnly ProductionDate { get; set; }
    public int     Quantity      { get; set; }
    public string  Status        { get; set; } = null!;
    public DateTime CreatedAt    { get; set; }
    public string? ColorName   { get; set; }
public string? ColorHexCode { get; set; }
public string? ModelNo       { get; set; }
}

public class BatchDetailDto : BatchListDto
{
    public int  ProductId    { get; set; }
    public long GeneratedQty { get; set; }
    public long PrintedQty   { get; set; }
    public long DispatchedQty { get; set; }
}

public class CreateBatchDto
{
    public int      ProductId      { get; set; }
    public string   BatchNo        { get; set; } = null!;
    public DateOnly ProductionDate { get; set; }
    public int      Quantity       { get; set; }
    public int? ColorId { get; set; }
}

public class BatchProgressDto
{
    public long   BatchId      { get; set; }
    public string BatchNo      { get; set; } = null!;
    public int    Total        { get; set; }
    public int    Generated    { get; set; }
    public int    Printed      { get; set; }
    public int    Dispatched   { get; set; }
    public int    PercentDone  { get; set; }
}