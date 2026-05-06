namespace AuthX.Core.Entities;

// ─── Company ───────────────────────────────────────────────
public class Company
{
    public int       CompanyId  { get; set; }
    public string    Name       { get; set; } = null!;
    public string?   Domain     { get; set; }
    public string?   LogoUrl    { get; set; }
    public bool      IsActive   { get; set; } = true;
    public DateTime  CreatedAt  { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt  { get; set; }

    public ICollection<User>            Users      { get; set; } = new List<User>();
    public ICollection<Role>            Roles      { get; set; } = new List<Role>();
    public ICollection<Category>        Categories { get; set; } = new List<Category>();
    public ICollection<Product>         Products   { get; set; } = new List<Product>();
    public ICollection<ProductionBatch> Batches    { get; set; } = new List<ProductionBatch>();
}

// ─── Role ──────────────────────────────────────────────────
public class Role
{
    public int    RoleId    { get; set; }
    public int    CompanyId { get; set; }
    public string RoleName  { get; set; } = null!;
    public bool   IsActive  { get; set; } = true;

    public Company           Company   { get; set; } = null!;
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

// ─── User ──────────────────────────────────────────────────
public class User
{
    public int       UserId               { get; set; }
    public int       CompanyId            { get; set; }
    public string    Name                 { get; set; } = null!;
    public string    Email                { get; set; } = null!;
    public string?   Phone                { get; set; }
    public string    PasswordHash         { get; set; } = null!;
    public string?   RefreshToken         { get; set; }
    public DateTime? RefreshTokenExpiry   { get; set; }
    public bool      IsActive             { get; set; } = true;
    public DateTime  CreatedAt            { get; set; } = DateTime.UtcNow;

    public Company             Company   { get; set; } = null!;
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

// ─── UserRole ──────────────────────────────────────────────
public class UserRole
{
    public int Id     { get; set; }
    public int UserId { get; set; }
    public int RoleId { get; set; }

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}

// ─── Category ──────────────────────────────────────────────
public class Category
{
    public int      CategoryId  { get; set; }
    public int      CompanyId   { get; set; }
    public int?     ParentId    { get; set; }
    public string   Name        { get; set; } = null!;
    public string?  Description { get; set; }
    public bool     IsActive    { get; set; } = true;
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;

    public Company              Company  { get; set; } = null!;
    public Category?            Parent   { get; set; }
    public ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

// ─── Product ───────────────────────────────────────────────
public class Product
{
    public int      ProductId    { get; set; }
    public int      CompanyId    { get; set; }
    public int      CategoryId   { get; set; }
    public string   Name         { get; set; } = null!;
    public string   SKU          { get; set; } = null!;
    public int      WarrantyDays { get; set; } = 365;
    public string?  Description  { get; set; }
    public bool     IsActive     { get; set; } = true;
    public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;

    public Company              Company  { get; set; } = null!;
    public Category             Category { get; set; } = null!;
    public ICollection<ProductionBatch> Batches { get; set; } = new List<ProductionBatch>();
    public string? ModelNo   { get; set; }
public string? ImageUrl  { get; set; }
public ICollection<ProductColor> ProductColors { get; set; } = new List<ProductColor>();
}

// ─── ProductionBatch ───────────────────────────────────────
public class ProductionBatch
{
    public long     BatchId        { get; set; }
    public int      CompanyId      { get; set; }
    public int      ProductId      { get; set; }
    public string   BatchNo        { get; set; } = null!;
    public DateOnly ProductionDate { get; set; }
    public int      Quantity       { get; set; }
    public string   Status         { get; set; } = "Draft";
    public int      CreatedBy      { get; set; }
    public DateTime CreatedAt      { get; set; } = DateTime.UtcNow;
    public int? ColorId { get; set; }
public Color? Color { get; set; }


    public Company                  Company  { get; set; } = null!;
    public Product                  Product  { get; set; } = null!;
    public ICollection<ProductItem> Items    { get; set; } = new List<ProductItem>();
    public ICollection<PrintJob>    PrintJobs { get; set; } = new List<PrintJob>();
}

// ─── ProductItem ───────────────────────────────────────────
public class ProductItem
{
    public long      ItemId            { get; set; }
    public int       CompanyId         { get; set; }
    public int       ProductId         { get; set; }
    public long      BatchId           { get; set; }
    public string    SerialNo          { get; set; } = null!;
    public string    QRCode            { get; set; } = null!;
    public string?   QRImagePath       { get; set; }
    public string    Status            { get; set; } = "Generated";
    public DateTime? WarrantyStartDate { get; set; }
    public DateTime? WarrantyEndDate   { get; set; }
    public bool      IsFirstScan       { get; set; } = false;
    public DateTime? FirstScanDate     { get; set; }
    public string?   FirstScanType     { get; set; }
    public string    PrintStatus       { get; set; } = "Pending";
    public bool      IsActive          { get; set; } = true;
    public DateTime  CreatedAt         { get; set; } = DateTime.UtcNow;

    public Company         Company  { get; set; } = null!;
    public Product         Product  { get; set; } = null!;
    public ProductionBatch Batch    { get; set; } = null!;
    public ICollection<Claim>    Claims    { get; set; } = new List<Claim>();
    public ICollection<Dispatch> Dispatches { get; set; } = new List<Dispatch>();
}

// ─── QRGeneration ──────────────────────────────────────────
public class QRGeneration
{
    public long     GenerationId   { get; set; }
    public int      CompanyId      { get; set; }
    public long     BatchId        { get; set; }
    public int      TotalGenerated { get; set; }
    public int      GeneratedBy    { get; set; }
    public DateTime GeneratedAt    { get; set; } = DateTime.UtcNow;
}

// ─── PrintJob ──────────────────────────────────────────────
public class PrintJob
{
    public long      PrintJobId   { get; set; }
    public int       CompanyId    { get; set; }
    public long      BatchId      { get; set; }
    public int       TotalItems   { get; set; }
    public int       PrintedCount { get; set; } = 0;
    public string    Status       { get; set; } = "Pending";
    public string?   FileUrl      { get; set; }
    public string    FileFormat   { get; set; } = "PDF";
    public string?   ErrorMessage { get; set; }
    public int       CreatedBy    { get; set; }
    public DateTime  CreatedAt    { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt  { get; set; }

    public Company         Company { get; set; } = null!;
    public ProductionBatch Batch   { get; set; } = null!;
}

// ─── Dispatch ──────────────────────────────────────────────
public class Dispatch
{
    public long      DispatchId   { get; set; }
    public int       CompanyId    { get; set; }
    public long      ItemId       { get; set; }
    public int       ScannedBy    { get; set; }
    public DateTime  DispatchDate { get; set; } = DateTime.UtcNow;
    public string?   Location     { get; set; }
    public string?   Notes        { get; set; }
    public string?   SapInvoiceNo { get; set; }

    public ProductItem Item { get; set; } = null!;
}

// ─── Customer ──────────────────────────────────────────────
public class Customer
{
    public long     CustomerId { get; set; }
    public string   Name       { get; set; } = null!;
    public string   Phone      { get; set; } = null!;
    public string?  Address    { get; set; }
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;

    public ICollection<Claim> Claims { get; set; } = new List<Claim>();
}

// ─── Claim ─────────────────────────────────────────────────
public class Claim
{
    public long      ClaimId    { get; set; }
    public int       CompanyId  { get; set; }
    public long      ItemId     { get; set; }
    public long      CustomerId { get; set; }
    public DateTime  ClaimDate  { get; set; } = DateTime.UtcNow;
    public string    Status     { get; set; } = "Open";
    public string?   LastStatus { get; set; }
    public string?   Remarks    { get; set; }
    public int?      AssignedTo { get; set; }

    public ProductItem            Item           { get; set; } = null!;
    public Customer               Customer       { get; set; } = null!;
    public ICollection<ClaimStatusHistory> StatusHistory { get; set; } = new List<ClaimStatusHistory>();
}

// ─── ClaimStatusHistory ────────────────────────────────────
public class ClaimStatusHistory
{
    public long     Id        { get; set; }
    public long     ClaimId   { get; set; }
    public string   Status    { get; set; } = null!;
    public int      UpdatedBy { get; set; }
    public string?  Notes     { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Claim Claim { get; set; } = null!;
}

// ─── ScanLog ───────────────────────────────────────────────
public class ScanLog
{
    public long      ScanId         { get; set; }
    public string    QRCode         { get; set; } = null!;
    public string    ScanType       { get; set; } = null!;
    public DateTime  ScanTime       { get; set; } = DateTime.UtcNow;
    public decimal?  Latitude       { get; set; }
    public decimal?  Longitude      { get; set; }
    public string?   IPAddress      { get; set; }
    public string?   DeviceInfo     { get; set; }
    public string?   ResponseStatus { get; set; }
    public string?   Country        { get; set; }
    public string?   City           { get; set; }
}

// ─── Notification ──────────────────────────────────────────
public class Notification
{
    public long      NotificationId { get; set; }
    public int       CompanyId      { get; set; }
    public string    Type           { get; set; } = null!;
    public long?     ReferenceId    { get; set; }
    public string    Message        { get; set; } = null!;
    public int?      TargetUserId   { get; set; }
    public int?      TargetRoleId   { get; set; }
    public string?   ActionUrl      { get; set; }
    public bool      IsRead         { get; set; } = false;
    public DateTime  CreatedAt      { get; set; } = DateTime.UtcNow;
}

public class Color
{
    public int      ColorId   { get; set; }
    public int      CompanyId { get; set; }
    public string   Name      { get; set; } = null!;
    public string   HexCode   { get; set; } = null!;
    public bool     IsActive  { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Company Company { get; set; } = null!;
    public ICollection<ProductColor> ProductColors { get; set; } = new List<ProductColor>();
}

// ─── ProductColor ───────────────────────────────────────────
public class ProductColor
{
    public int ProductId { get; set; }
    public int ColorId   { get; set; }
    public Product Product { get; set; } = null!;
    public Color   Color   { get; set; } = null!;
}

// ─── PrintSettings ─────────────────────────────────────────
public class PrintSettings
{
    public int     Id              { get; set; }
    public int     CompanyId       { get; set; }
    public decimal LabelWidthMm    { get; set; } = 17;
    public decimal LabelHeightMm   { get; set; } = 30;
    public decimal QRSizeMm        { get; set; } = 17;
    public int     ColumnsPerRow   { get; set; } = 1;
    public bool    ShowProductName { get; set; } = true;
    public bool    ShowSerialNo    { get; set; } = true;
    public bool    ShowBatchNo     { get; set; } = true;
    public bool    ShowColorName   { get; set; } = false;
    public bool    ShowModelNo     { get; set; } = false;
    public bool    ShowCompanyName { get; set; } = false;
    public int     WarrantyDelayDays { get; set; } = 60;
    public DateTime UpdatedAt      { get; set; } = DateTime.UtcNow;

    public Company Company { get; set; } = null!;
}

// ─── CompanySettings ───────────────────────────────────────
public class CompanySettings
{
    public int      Id                 { get; set; }
    public int      CompanyId          { get; set; }
    public int      WarrantyDelayDays  { get; set; } = 60;
    public DateTime UpdatedAt          { get; set; } = DateTime.UtcNow;

    public Company Company { get; set; } = null!;
}

// ─── ReturnReason ───────────────────────────────────────
public class ReturnReason
{
    public int      ReturnReasonId { get; set; }
    public int      CompanyId      { get; set; }
    public string   Name           { get; set; } = null!;
    public string?  Description    { get; set; }
    public bool     IsActive       { get; set; } = true;
    public DateTime CreatedAt      { get; set; } = DateTime.UtcNow;

    public Company Company { get; set; } = null!;
}

// ─── ProductCondition ───────────────────────────────────────

public class ProductCondition
{
    public int      ProductConditionId { get; set; }
    public int      CompanyId          { get; set; }
    public string   Name               { get; set; } = null!;
    public string?  Description        { get; set; }
    public bool     IsActive           { get; set; } = true;
    public DateTime CreatedAt          { get; set; } = DateTime.UtcNow;

    public Company Company { get; set; } = null!;
}