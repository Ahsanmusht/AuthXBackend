namespace AuthX.Core.DTOs.Claims;

public class ScanResultDto
{
    public string   Status        { get; set; } = null!; // Genuine | Fake | NotFound | AlreadyClaimed | InProcess
    public string   Message       { get; set; } = null!;
    public string?  ProductName   { get; set; }
    public string?  CategoryName  { get; set; }
    public string?  SerialNo      { get; set; }
    public string?  BatchNo       { get; set; }
    public DateTime? WarrantyStart { get; set; }
    public DateTime? WarrantyEnd   { get; set; }
    public bool     CanClaim      { get; set; }
    public bool     IsUnderWarranty { get; set; }
    public string?  ClaimStatus   { get; set; } // if already claimed
    public string? ColorName    { get; set; }
public string? ColorHexCode { get; set; }
public string? ProductImageUrl { get; set; }
public string? ModelNo      { get; set; }
}

public class SubmitClaimDto
{
    public string  QRCode    { get; set; } = null!;
    public string  Name      { get; set; } = null!;
    public string  Phone     { get; set; } = null!;
    public string? Address   { get; set; }
    public string? Remarks   { get; set; }
}

public class ClaimDto
{
    public long    ClaimId     { get; set; }
    public string  Status      { get; set; } = null!;
    public string  Message     { get; set; } = null!;
}

public class ClaimListDto
{
    public long     ClaimId      { get; set; }
    public string   CustomerName { get; set; } = null!;
    public string   Phone        { get; set; } = null!;
    public string   SerialNo     { get; set; } = null!;
    public string   ProductName  { get; set; } = null!;
    public string   Status       { get; set; } = null!;
    public DateTime ClaimDate    { get; set; }
    public string?  AssignedTo   { get; set; }
}

public class ClaimDetailDto : ClaimListDto
{
    public string?  Address   { get; set; }
    public string?  Remarks   { get; set; }
    public string?  BatchNo   { get; set; }
    public string?  QRCode    { get; set; }
    public DateTime? WarrantyStart { get; set; }
    public DateTime? WarrantyEnd   { get; set; }
    public List<ClaimHistoryDto> History { get; set; } = new();
}

public class ClaimHistoryDto
{
    public string   Status    { get; set; } = null!;
    public string   UpdatedBy { get; set; } = null!;
    public string?  Notes     { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateClaimStatusDto
{
    public string  Status  { get; set; } = null!;
    public string? Notes   { get; set; }
    public int?    AssignTo { get; set; }
}

public class ClaimFilterDto
{
    public string?   Status   { get; set; }
    public DateTime? From     { get; set; }
    public DateTime? To       { get; set; }
    public int?      AssignedTo { get; set; }
}