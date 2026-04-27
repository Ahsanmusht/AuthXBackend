namespace AuthX.Core.Constants;

public static class AppRoles
{
    public const string Admin       = "Admin";
    public const string Manager     = "Manager";
    public const string Production  = "Production";
    public const string Warehouse   = "Warehouse";
    public const string Support     = "Support";
    public const string Viewer      = "Viewer";
}

public static class CacheKeys
{
    public static string QRItem(string qrCode)    => $"qr:{qrCode}";
    public static string BatchProgress(string id) => $"batch_progress:{id}";
    public static string DashboardStats(int cId)  => $"dashboard:{cId}";
    public static string UserInfo(int userId)      => $"user:{userId}";
}

public static class ItemStatuses
{
    public const string Generated  = "Generated";
    public const string Printed    = "Printed";
    public const string Dispatched = "Dispatched";
    public const string Claimed    = "Claimed";
}

public static class ClaimStatuses
{
    public const string Open          = "Open";
    public const string InProgress    = "InProgress";
    public const string ProductSent   = "ProductSent";
    public const string WithTechnician = "WithTechnician";
    public const string ResolvedPhone = "ResolvedPhone";
    public const string Delivered     = "Delivered";
}

public static class BatchStatuses
{
    public const string Draft        = "Draft";
    public const string QRGenerated  = "QRGenerated";
    public const string Printed      = "Printed";
    public const string Dispatched   = "Dispatched";
}

public static class NotificationTypes
{
    public const string NewClaim      = "NewClaim";
    public const string ClaimUpdated  = "ClaimUpdated";
    public const string QRGenerated   = "QRGenerated";
    public const string PrintDone     = "PrintDone";
}

public static class SignalRGroups
{
    public const string SupportTeam = "SupportTeam";
    public const string Admins      = "Admins";
    public const string Managers    = "Managers";
}