namespace AuthX.Core.DTOs.Dispatch;

public class DispatchResultDto
{
    public long    DispatchId    { get; set; }
    public string  SerialNo      { get; set; } = null!;
    public string  ProductName   { get; set; } = null!;
    public DateTime DispatchDate { get; set; }
    public DateTime WarrantyEnd  { get; set; }
}

public class DispatchListDto
{
    public long     DispatchId   { get; set; }
    public string   SerialNo     { get; set; } = null!;
    public string   QRCode       { get; set; } = null!;
    public string   ProductName  { get; set; } = null!;
    public string?  Location     { get; set; }
    public DateTime DispatchDate { get; set; }
    public string   ScannedBy    { get; set; } = null!;
}