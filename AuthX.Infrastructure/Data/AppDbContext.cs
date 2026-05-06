using Microsoft.EntityFrameworkCore;
using AuthX.Core.Entities;

namespace AuthX.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ─── DbSets ────────────────────────────────────────────
    public DbSet<Company> Companies { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductionBatch> Batches { get; set; }
    public DbSet<ProductItem> ProductItems { get; set; }
    public DbSet<QRGeneration> QRGenerations { get; set; }
    public DbSet<PrintJob> PrintJobs { get; set; }
    public DbSet<Dispatch> Dispatches { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Claim> Claims { get; set; }
    public DbSet<ClaimStatusHistory> ClaimHistories { get; set; }
    public DbSet<ScanLog> ScanLogs { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Color> Colors { get; set; }
    public DbSet<ProductColor> ProductColors { get; set; }
    public DbSet<PrintSettings> PrintSettings { get; set; }
    public DbSet<CompanySettings> CompanySettings { get; set; }
    public DbSet<ReturnReason> ReturnReasons { get; set; }
    public DbSet<ProductCondition> ProductConditions { get; set; }
     public DbSet<MenuItem>       MenuItems       { get; set; }
     public DbSet<MenuPermission> MenuPermissions { get; set; }
     public DbSet<PromotionSetup> Promotions      { get; set; }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // ── Company ────────────────────────────────────────
        mb.Entity<Company>(e =>
        {
            e.ToTable("Company");
            e.HasKey(x => x.CompanyId);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Domain).HasMaxLength(200);
            e.Property(x => x.LogoUrl).HasMaxLength(500);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
        });

        // ── Role ───────────────────────────────────────────
        mb.Entity<Role>(e =>
        {
            e.ToTable("Roles");
            e.HasKey(x => x.RoleId);
            e.Property(x => x.RoleName).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.Company)
             .WithMany(c => c.Roles)
             .HasForeignKey(x => x.CompanyId);
        });

        // ── User ───────────────────────────────────────────
        mb.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.UserId);
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            e.Property(x => x.RefreshToken).HasMaxLength(500);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne(x => x.Company)
             .WithMany(c => c.Users)
             .HasForeignKey(x => x.CompanyId);
        });

        // ── UserRole ───────────────────────────────────────
        mb.Entity<UserRole>(e =>
        {
            e.ToTable("UserRoles");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.User)
             .WithMany(u => u.UserRoles)
             .HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Role)
             .WithMany(r => r.UserRoles)
             .HasForeignKey(x => x.RoleId);
        });

        // ── Category ───────────────────────────────────────
        mb.Entity<Category>(e =>
        {
            e.ToTable("Category");
            e.HasKey(x => x.CategoryId);
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.Company)
             .WithMany(c => c.Categories)
             .HasForeignKey(x => x.CompanyId);
            e.HasOne(x => x.Parent)
   .WithMany(c => c.SubCategories)
   .HasForeignKey(x => x.ParentId)
   .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Product ────────────────────────────────────────
        mb.Entity<Product>(e =>
        {
            e.ToTable("Product");
            e.HasKey(x => x.ProductId);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.SKU).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.HasIndex(x => new { x.SKU, x.CompanyId }).IsUnique();
            e.HasOne(x => x.Company)
             .WithMany(c => c.Products)
             .HasForeignKey(x => x.CompanyId);
            e.HasOne(x => x.Category)
             .WithMany(c => c.Products)
             .HasForeignKey(x => x.CategoryId);
            e.Property(x => x.ModelNo).HasMaxLength(100);
            e.Property(x => x.ImageUrl).HasMaxLength(2000);
        });

        // ── ProductionBatch ────────────────────────────────
        mb.Entity<ProductionBatch>(e =>
        {
            e.ToTable("ProductionBatch");
            e.HasKey(x => x.BatchId);
            e.Property(x => x.BatchNo).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.BatchNo).IsUnique();
            e.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("Draft");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.Company)
             .WithMany(c => c.Batches)
             .HasForeignKey(x => x.CompanyId);
            e.HasOne(x => x.Product)
             .WithMany(p => p.Batches)
             .HasForeignKey(x => x.ProductId);
            e.HasOne(x => x.Color).WithMany().HasForeignKey(x => x.ColorId).IsRequired(false);
        });

        // ── ProductItem ────────────────────────────────────
        mb.Entity<ProductItem>(e =>
        {
            e.ToTable("ProductItem");
            e.HasKey(x => x.ItemId);
            e.Property(x => x.SerialNo).HasMaxLength(200).IsRequired();
            e.Property(x => x.QRCode).HasMaxLength(500).IsRequired();
            e.Property(x => x.QRImagePath).HasMaxLength(500);
            e.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("Generated");
            e.Property(x => x.PrintStatus).HasMaxLength(50).HasDefaultValue("Pending");
            e.Property(x => x.FirstScanType).HasMaxLength(50);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.HasIndex(x => x.SerialNo).IsUnique();
            e.HasIndex(x => x.QRCode).IsUnique();
            e.HasIndex(x => x.BatchId);
            e.HasIndex(x => x.Status);
            e.HasOne(x => x.Company)
             .WithMany()
             .HasForeignKey(x => x.CompanyId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Product)
             .WithMany()
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Batch)
             .WithMany(b => b.Items)
             .HasForeignKey(x => x.BatchId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── QRGeneration ───────────────────────────────────
        mb.Entity<QRGeneration>(e =>
        {
            e.ToTable("QRGeneration");
            e.HasKey(x => x.GenerationId);
            e.Property(x => x.GeneratedAt).HasDefaultValueSql("GETDATE()");
        });

        // ── PrintJob ───────────────────────────────────────
        mb.Entity<PrintJob>(e =>
        {
            e.ToTable("PrintJob");
            e.HasKey(x => x.PrintJobId);
            e.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("Pending");
            e.Property(x => x.FileUrl).HasMaxLength(500);
            e.Property(x => x.FileFormat).HasMaxLength(20).HasDefaultValue("PDF");
            e.Property(x => x.ErrorMessage).HasMaxLength(500);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.Company)
             .WithMany()
             .HasForeignKey(x => x.CompanyId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Batch)
             .WithMany(b => b.PrintJobs)
             .HasForeignKey(x => x.BatchId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Dispatch ───────────────────────────────────────
        mb.Entity<Dispatch>(e =>
        {
            e.ToTable("Dispatch");
            e.HasKey(x => x.DispatchId);
            e.Property(x => x.Location).HasMaxLength(200);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.Property(x => x.SapInvoiceNo).HasMaxLength(100);
            e.Property(x => x.DispatchDate).HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.Item)
             .WithMany(i => i.Dispatches)
             .HasForeignKey(x => x.ItemId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Customer ───────────────────────────────────────
        mb.Entity<Customer>(e =>
        {
            e.ToTable("Customer");
            e.HasKey(x => x.CustomerId);
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(50).IsRequired();
            e.Property(x => x.Address).HasMaxLength(300);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.HasIndex(x => x.Phone);
        });

        // ── Claim ──────────────────────────────────────────
        mb.Entity<Claim>(e =>
        {
            e.ToTable("Claim");
            e.HasKey(x => x.ClaimId);
            e.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("Open");
            e.Property(x => x.LastStatus).HasMaxLength(100);
            e.Property(x => x.Remarks).HasMaxLength(500);
            e.Property(x => x.ClaimDate).HasDefaultValueSql("GETDATE()");
            e.HasIndex(x => x.ItemId);
            e.HasIndex(x => new { x.CompanyId, x.Status });
            e.HasOne(x => x.Item)
             .WithMany(i => i.Claims)
             .HasForeignKey(x => x.ItemId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Customer)
             .WithMany(c => c.Claims)
             .HasForeignKey(x => x.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ClaimStatusHistory ─────────────────────────────
        mb.Entity<ClaimStatusHistory>(e =>
        {
            e.ToTable("ClaimStatusHistory");
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasMaxLength(100).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(500);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.Claim)
             .WithMany(c => c.StatusHistory)
             .HasForeignKey(x => x.ClaimId);
        });

        // ── ScanLog (no FK — write-optimized) ─────────────
        mb.Entity<ScanLog>(e =>
        {
            e.ToTable("ScanLog");
            e.HasKey(x => x.ScanId);
            e.Property(x => x.QRCode).HasMaxLength(500).IsRequired();
            e.Property(x => x.ScanType).HasMaxLength(50).IsRequired();
            e.Property(x => x.IPAddress).HasMaxLength(50);
            e.Property(x => x.DeviceInfo).HasMaxLength(300);
            e.Property(x => x.ResponseStatus).HasMaxLength(50);
            e.Property(x => x.Country).HasMaxLength(100);
            e.Property(x => x.City).HasMaxLength(100);
            e.Property(x => x.ScanTime).HasDefaultValueSql("GETDATE()");
            e.Property(x => x.Latitude).HasColumnType("decimal(10,6)");
            e.Property(x => x.Longitude).HasColumnType("decimal(10,6)");
        });

        // ── Notification ───────────────────────────────────
        mb.Entity<Notification>(e =>
        {
            e.ToTable("Notification");
            e.HasKey(x => x.NotificationId);
            e.Property(x => x.Type).HasMaxLength(50).IsRequired();
            e.Property(x => x.Message).HasMaxLength(500).IsRequired();
            e.Property(x => x.ActionUrl).HasMaxLength(300);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.HasIndex(x => new { x.TargetUserId, x.IsRead });
            e.HasIndex(x => new { x.CompanyId, x.CreatedAt });
        });
        // ── Color ──────────────────────────────────────────────────
        mb.Entity<Color>(e =>
        {
            e.ToTable("Color");
            e.HasKey(x => x.ColorId);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.HexCode).HasMaxLength(10).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId);
        });

        // ── ProductColor ───────────────────────────────────────────
        mb.Entity<ProductColor>(e =>
        {
            e.ToTable("ProductColor");
            e.HasKey(x => new { x.ProductId, x.ColorId });
            e.HasOne(x => x.Product).WithMany(p => p.ProductColors).HasForeignKey(x => x.ProductId);
            e.HasOne(x => x.Color).WithMany(c => c.ProductColors).HasForeignKey(x => x.ColorId);
        });

        // ── PrintSettings ──────────────────────────────────────────
        mb.Entity<PrintSettings>(e =>
        {
            e.ToTable("PrintSettings");
            e.HasKey(x => x.Id);
            e.Property(x => x.LabelWidthMm).HasColumnType("decimal(6,2)");
            e.Property(x => x.LabelHeightMm).HasColumnType("decimal(6,2)");
            e.Property(x => x.QRSizeMm).HasColumnType("decimal(6,2)");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId);
            e.HasIndex(x => x.CompanyId).IsUnique();
        });

        // ── CompanySettings ────────────────────────────────────────
        mb.Entity<CompanySettings>(e =>
        {
            e.ToTable("CompanySettings");
            e.HasKey(x => x.Id);
            e.Property(x => x.ColorMode).HasMaxLength(10).HasDefaultValue("Multi");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId);
            e.HasIndex(x => x.CompanyId).IsUnique();
        });
        // ── ReturnReason ────────────────────────────────────────
        mb.Entity<ReturnReason>(e =>
        {
            e.ToTable("ReturnReason");
            e.HasKey(x => x.ReturnReasonId);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(300);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId);
        });
        // ── ProductCondition ────────────────────────────────────────
        mb.Entity<ProductCondition>(e =>
        {
            e.ToTable("ProductCondition");
            e.HasKey(x => x.ProductConditionId);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(300);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId);
        });
        mb.Entity<MenuItem>(e =>
    {
        e.ToTable("MenuItem");
        e.HasKey(x => x.MenuItemId);
        e.Property(x => x.Title).HasMaxLength(100).IsRequired();
        e.Property(x => x.Url).HasMaxLength(200);
        e.Property(x => x.Icon).HasMaxLength(50);
        e.Property(x => x.Type).HasMaxLength(20).HasDefaultValue("item");
        e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
        e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId);
        e.HasOne(x => x.Parent).WithMany(m => m.Children)
            .HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
    });
 
    mb.Entity<MenuPermission>(e =>
    {
        e.ToTable("MenuPermission");
        e.HasKey(x => x.Id);
        e.HasIndex(x => new { x.MenuItemId, x.RoleId }).IsUnique();
        e.HasOne(x => x.MenuItem).WithMany(m => m.Permissions)
            .HasForeignKey(x => x.MenuItemId).OnDelete(DeleteBehavior.Cascade);
        e.HasOne(x => x.Role).WithMany()
            .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
    });
 
    mb.Entity<PromotionSetup>(e =>
    {
        e.ToTable("PromotionSetup");
        e.HasKey(x => x.PromotionId);
        e.Property(x => x.Title).HasMaxLength(200);
        e.Property(x => x.ImageUrl).HasMaxLength(1000).IsRequired();
        e.Property(x => x.ForwardUrl).HasMaxLength(500);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
        e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId);
    });
    }
}