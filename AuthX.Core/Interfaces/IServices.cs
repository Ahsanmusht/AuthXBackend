using AuthX.Core.DTOs.Auth;
using AuthX.Core.DTOs.Common;
using AuthX.Core.DTOs.Users;
using AuthX.Core.DTOs.Categories;
using AuthX.Core.DTOs.Products;
using AuthX.Core.DTOs.Batches;
using AuthX.Core.DTOs.QR;
using AuthX.Core.DTOs.Dispatch;
using AuthX.Core.DTOs.Claims;
using AuthX.Core.DTOs.Scan;
using AuthX.Core.DTOs.Notifications;
using AuthX.Core.DTOs.Dashboard;
using AuthX.Core.DTOs.ReturnReasons;
using AuthX.Core.DTOs.ProductConditions;

namespace AuthX.Core.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto dto);
    Task<LoginResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto);
    Task RevokeTokenAsync(int userId);
}

public interface IUserService
{
    Task<PagedResult<UserListDto>> GetUsersAsync(int companyId, PaginationParams p);
    Task<UserDetailDto> GetByIdAsync(int companyId, int userId);
    Task<UserDetailDto> CreateAsync(int companyId, CreateUserDto dto);
    Task<UserDetailDto> UpdateAsync(int companyId, int userId, UpdateUserDto dto);
    Task SetActiveAsync(int companyId, int userId, bool active);
    Task AssignRolesAsync(int companyId, int userId, List<int> roleIds);
}

public interface IRoleService
{
    Task<List<RoleDto>> GetRolesAsync(int companyId);
    Task<RoleDto> CreateAsync(int companyId, string roleName);
    Task DeleteAsync(int companyId, int roleId);
}

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync(int companyId);
    Task<CategoryDto> GetByIdAsync(int companyId, int categoryId);
    Task<CategoryDto> CreateAsync(int companyId, CreateCategoryDto dto);
    Task<CategoryDto> UpdateAsync(int companyId, int categoryId, UpdateCategoryDto dto);
    Task SetActiveAsync(int companyId, int categoryId, bool active);
}

public interface IProductService
{
    Task<PagedResult<ProductListDto>> GetAllAsync(int companyId, PaginationParams p);
    Task<ProductDetailDto> GetByIdAsync(int companyId, int productId);
    Task<ProductDetailDto> CreateAsync(int companyId, CreateProductDto dto);
    Task<ProductDetailDto> UpdateAsync(int companyId, int productId, UpdateProductDto dto);
    Task SetActiveAsync(int companyId, int productId, bool active);
}

public interface IBatchService
{
    Task<PagedResult<BatchListDto>> GetAllAsync(int companyId, PaginationParams p);
    Task<BatchDetailDto> GetByIdAsync(int companyId, long batchId);
    Task<BatchDetailDto> CreateAsync(int companyId, int createdBy, CreateBatchDto dto);
    Task UpdateStatusAsync(int companyId, long batchId, string status);
}

public interface IQRService
{
    Task<QRGenerationResultDto> GenerateAsync(int companyId, int userId, GenerateQRDto dto);
    Task<PrintJobDto> CreatePrintJobAsync(int companyId, int userId, CreatePrintJobDto dto);
    Task<PrintJobDto> GetPrintJobAsync(int companyId, long printJobId);
    Task<byte[]> ExportQRsAsync(int companyId, long batchId, string format);
    Task<BatchProgressDto> GetBatchProgressAsync(long batchId);
}

public interface IDispatchService
{
    Task<DispatchResultDto> ScanDispatchAsync(int companyId, int scannedBy, string qrCode, string? location,string? sapInvoiceNo);
    Task<PagedResult<DispatchListDto>> GetDispatchesAsync(int companyId, long? batchId, PaginationParams p);
}

public interface IClaimService
{
    Task<ScanResultDto> ScanQRAsync(string qrCode, string? ip, string? deviceInfo, decimal? lat, decimal? lon);
    Task<ClaimDto> SubmitClaimAsync(SubmitClaimDto dto);
    Task<PagedResult<ClaimListDto>> GetClaimsAsync(int companyId, ClaimFilterDto filter, PaginationParams p);
    Task<ClaimDetailDto> GetByIdAsync(int companyId, long claimId);
    Task<ClaimDetailDto> UpdateStatusAsync(int companyId, long claimId, int updatedBy, UpdateClaimStatusDto dto);
}

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetForUserAsync(int userId, PaginationParams p);
    Task MarkReadAsync(int userId, long notificationId);
    Task MarkAllReadAsync(int userId);
    Task PushAsync(int companyId, string type, long? referenceId,
                                                  string message, int? targetUserId = null,
                                                  int? targetRoleId = null, string? actionUrl = null);
}

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync(int companyId);
    Task<List<ScanTrendDto>> GetScanTrendAsync(int companyId, int days);
    Task<List<ClaimTrendDto>> GetClaimTrendAsync(int companyId, int days);
}
public interface IReturnReasonService
{
    Task<List<ReturnReasonDto>> GetAllAsync(int companyId);
    Task<ReturnReasonDto> GetByIdAsync(int companyId, int id);
    Task<ReturnReasonDto> CreateAsync(int companyId, CreateReturnReasonDto dto);
    Task<ReturnReasonDto> UpdateAsync(int companyId, int id, UpdateReturnReasonDto dto);
    Task SetActiveAsync(int companyId, int id, bool active);
}

public interface IProductConditionService
{
    Task<List<ProductConditionDto>> GetAllAsync(int companyId);
    Task<ProductConditionDto> GetByIdAsync(int companyId, int id);
    Task<ProductConditionDto> CreateAsync(int companyId, CreateProductConditionDto dto);
    Task<ProductConditionDto> UpdateAsync(int companyId, int id, UpdateProductConditionDto dto);
    Task SetActiveAsync(int companyId, int id, bool active);
}