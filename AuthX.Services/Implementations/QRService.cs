using AuthX.Core.Constants;
using AuthX.Core.DTOs.Batches;
using AuthX.Core.DTOs.QR;
using AuthX.Core.Entities;
using AuthX.Core.Interfaces;
using AuthX.Infrastructure.Cache;
using AuthX.Services.BackgroundJobs;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace AuthX.Services.Implementations;

public class QRService : IQRService
{
    private readonly IUnitOfWork        _uow;
    private readonly IRedisCacheService _cache;

    public QRService(IUnitOfWork uow, IRedisCacheService cache)
    {
        _uow   = uow;
        _cache = cache;
    }

    public async Task<QRGenerationResultDto> GenerateAsync(
        int companyId, int userId, GenerateQRDto dto)
    {
        var batch = await _uow.Batches.FindOneAsync(b =>
            b.CompanyId == companyId && b.BatchId == dto.BatchId)
            ?? throw new KeyNotFoundException("Batch not found.");

        if (batch.Status != BatchStatuses.Draft)
            throw new InvalidOperationException("QR codes already generated for this batch.");

        // Enqueue background job — returns immediately
        BackgroundJob.Enqueue<QRGenerationJob>(
            queue: "qr_generation",
            job => job.GenerateAsync(companyId, userId, batch.BatchId));

        return new QRGenerationResultDto
        {
            GenerationId   = 0,
            BatchId        = batch.BatchId,
            TotalGenerated = batch.Quantity,
            Message        = $"QR generation queued for {batch.Quantity:N0} items. " +
                             "You will be notified when complete."
        };
    }

    public async Task<PrintJobDto> CreatePrintJobAsync(
        int companyId, int userId, CreatePrintJobDto dto)
    {
        var batch = await _uow.Batches.FindOneAsync(b =>
            b.CompanyId == companyId && b.BatchId == dto.BatchId)
            ?? throw new KeyNotFoundException("Batch not found.");

        var totalItems = await _uow.ProductItems.CountAsync(i =>
            i.BatchId == dto.BatchId &&
            i.PrintStatus == "Pending" &&
            i.CompanyId == companyId);

        if (totalItems == 0)
            throw new InvalidOperationException("No pending items to print.");

        var printJob  = new Core.Entities.PrintJob
        {
            CompanyId  = companyId,
            BatchId    = dto.BatchId,
            TotalItems = totalItems,
            FileFormat = dto.FileFormat,
            CreatedBy  = userId
        };

        await _uow.PrintJobs.AddAsync(printJob);
        await _uow.SaveChangesAsync();

        // Enqueue background processing
        BackgroundJob.Enqueue<PrintProcessingJob>(
            queue: "print_jobs",
            job => job.ProcessAsync(companyId, userId, printJob.PrintJobId));

        return MapPrintJob(printJob);
    }

    public async Task<PrintJobDto> GetPrintJobAsync(int companyId, long printJobId)
    {
        var job = await _uow.PrintJobs.FindOneAsync(j =>
            j.CompanyId == companyId && j.PrintJobId == printJobId)
            ?? throw new KeyNotFoundException("Print job not found.");

        return MapPrintJob(job);
    }

    public async Task<byte[]> ExportQRsAsync(int companyId, long batchId, string format)
    {
        var items = await _uow.ProductItems.Query()
            .Where(i => i.BatchId == batchId && i.CompanyId == companyId)
            .Select(i => new { i.SerialNo, i.QRCode })
            .ToListAsync();

        if (format.ToUpper() == "CSV")
        {
            var csv = "SerialNo,QRCode\n" +
                string.Join("\n", items.Select(i => $"{i.SerialNo},{i.QRCode}"));
            return System.Text.Encoding.UTF8.GetBytes(csv);
        }

        throw new InvalidOperationException($"Format '{format}' not supported for direct export.");
    }

    public async Task<BatchProgressDto> GetBatchProgressAsync(long batchId)
    {
        var cacheKey = CacheKeys.BatchProgress(batchId.ToString());
        var cached   = await _cache.GetAsync<BatchProgressDto>(cacheKey);
        if (cached != null) return cached;

        var batch = await _uow.Batches.Query()
            .Where(b => b.BatchId == batchId)
            .Select(b => new BatchProgressDto
            {
                BatchId     = b.BatchId,
                BatchNo     = b.BatchNo,
                Total       = b.Quantity,
                Generated   = b.Items.Count(),
                Printed     = b.Items.Count(i => i.PrintStatus == "Printed"),
                Dispatched  = b.Items.Count(i => i.Status == "Dispatched"),
                PercentDone = b.Quantity == 0 ? 0 :
                    (int)(b.Items.Count(i => i.Status == "Dispatched") * 100.0 / b.Quantity)
            })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Batch not found.");

        await _cache.SetAsync(cacheKey, batch, TimeSpan.FromSeconds(30));
        return batch;
    }

    private static PrintJobDto MapPrintJob(Core.Entities.PrintJob job) => new()
    {
        PrintJobId   = job.PrintJobId,
        BatchId      = job.BatchId,
        TotalItems   = job.TotalItems,
        PrintedCount = job.PrintedCount,
        Status       = job.Status,
        FileUrl      = job.FileUrl,
        FileFormat   = job.FileFormat,
        ErrorMessage = job.ErrorMessage,
        CreatedAt    = job.CreatedAt,
        CompletedAt  = job.CompletedAt
    };
}