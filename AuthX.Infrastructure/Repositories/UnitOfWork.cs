using AuthX.Core.Entities;
using AuthX.Core.Interfaces;
using AuthX.Infrastructure.Data;

namespace AuthX.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _ctx;

    public UnitOfWork(AppDbContext ctx)
    {
        _ctx       = ctx;
        Companies      = new Repository<Company>(_ctx);
        Roles          = new Repository<Role>(_ctx);
        Users          = new Repository<User>(_ctx);
        UserRoles      = new Repository<UserRole>(_ctx);
        Categories     = new Repository<Category>(_ctx);
        Products       = new Repository<Product>(_ctx);
        Batches        = new Repository<ProductionBatch>(_ctx);
        ProductItems   = new Repository<ProductItem>(_ctx);
        QRGenerations  = new Repository<QRGeneration>(_ctx);
        PrintJobs      = new Repository<PrintJob>(_ctx);
        Dispatches     = new Repository<Dispatch>(_ctx);
        Customers      = new Repository<Customer>(_ctx);
        Claims         = new Repository<Claim>(_ctx);
        ClaimHistories = new Repository<ClaimStatusHistory>(_ctx);
        ScanLogs       = new Repository<ScanLog>(_ctx);
        Notifications  = new Repository<Notification>(_ctx);
        Colors          = new Repository<Color>(_ctx);
ProductColors   = new Repository<ProductColor>(_ctx);
PrintSettings   = new Repository<PrintSettings>(_ctx);
CompanySettings = new Repository<CompanySettings>(_ctx);
    }

    public IRepository<Company>            Companies      { get; }
    public IRepository<Role>               Roles          { get; }
    public IRepository<User>               Users          { get; }
    public IRepository<UserRole>           UserRoles      { get; }
    public IRepository<Category>           Categories     { get; }
    public IRepository<Product>            Products       { get; }
    public IRepository<ProductionBatch>    Batches        { get; }
    public IRepository<ProductItem>        ProductItems   { get; }
    public IRepository<QRGeneration>       QRGenerations  { get; }
    public IRepository<PrintJob>           PrintJobs      { get; }
    public IRepository<Dispatch>           Dispatches     { get; }
    public IRepository<Customer>           Customers      { get; }
    public IRepository<Claim>              Claims         { get; }
    public IRepository<ClaimStatusHistory> ClaimHistories { get; }
    public IRepository<ScanLog>            ScanLogs       { get; }
    public IRepository<Notification>       Notifications  { get; }
    public IRepository<Color>           Colors          { get; }
public IRepository<ProductColor>    ProductColors   { get; }
public IRepository<PrintSettings>   PrintSettings   { get; }
public IRepository<CompanySettings> CompanySettings { get; }

    public async Task<int> SaveChangesAsync()
        => await _ctx.SaveChangesAsync();

    public void Dispose()
        => _ctx.Dispose();
}