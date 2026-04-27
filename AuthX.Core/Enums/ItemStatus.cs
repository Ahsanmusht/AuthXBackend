namespace AuthX.Core.Enums;

public enum ItemStatus
{
    Generated,
    Printed,
    Dispatched,
    Claimed
}

public enum ClaimStatus
{
    Open,
    InProgress,
    ProductSent,
    WithTechnician,
    ResolvedPhone,
    Delivered
}

public enum BatchStatus
{
    Draft,
    QRGenerated,
    Printed,
    Dispatched
}

public enum PrintJobStatus
{
    Pending,
    Processing,
    Done,
    Failed
}

public enum PrintFileFormat
{
    PDF,
    ZPL,
    CSV
}

public enum ScanType
{
    CustomerScan,
    DispatchScan,
    AdminScan
}

public enum ScanResponseStatus
{
    Genuine,
    Fake,
    NotFound,
    AlreadyClaimed,
    InProcess
}

public enum NotificationType
{
    NewClaim,
    ClaimUpdated,
    QRGenerated,
    PrintDone
}