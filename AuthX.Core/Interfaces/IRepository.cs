using System.Linq.Expressions;
using AuthX.Core.Entities;

namespace AuthX.Core.Interfaces;

// ─── Generic Repository Interface ──────────────────────────
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(object id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    void Update(T entity);
    void Remove(T entity);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
    IQueryable<T> Query();
}

// ─── Unit of Work ──────────────────────────────────────────
public interface IUnitOfWork : IDisposable
{
    IRepository<Company>            Companies     { get; }
    IRepository<Role>               Roles         { get; }
    IRepository<User>               Users         { get; }
    IRepository<UserRole>           UserRoles     { get; }
    IRepository<Category>           Categories    { get; }
    IRepository<Product>            Products       { get; }
    IRepository<ProductionBatch>    Batches       { get; }
    IRepository<ProductItem>        ProductItems  { get; }
    IRepository<QRGeneration>       QRGenerations { get; }
    IRepository<PrintJob>           PrintJobs     { get; }
    IRepository<Dispatch>           Dispatches    { get; }
    IRepository<Customer>           Customers     { get; }
    IRepository<Claim>              Claims        { get; }
    IRepository<ClaimStatusHistory> ClaimHistories { get; }
    IRepository<ScanLog>            ScanLogs      { get; }
    IRepository<Notification>       Notifications { get; }

    Task<int> SaveChangesAsync();
}

// ─── Paginated Result ──────────────────────────────────────
public class PagedResult<T>
{
    public IEnumerable<T> Items       { get; set; } = Enumerable.Empty<T>();
    public int            TotalCount  { get; set; }
    public int            Page        { get; set; }
    public int            PageSize    { get; set; }
    public int            TotalPages  => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool           HasPrevious => Page > 1;
    public bool           HasNext     => Page < TotalPages;
}

// ─── API Response Wrapper ──────────────────────────────────
public class ApiResponse<T>
{
    public bool    Success { get; set; }
    public string? Message { get; set; }
    public T?      Data    { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message, List<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors ?? new() };
}

public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Ok(string message) =>
        new() { Success = true, Message = message };

    public new static ApiResponse Fail(string message, List<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors ?? new() };
}