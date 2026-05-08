using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.ProductAnalyticalRawData;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductStandardTestProcedures;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ProductAnalyticalRawDataRepository(ApplicationDbContext context, IMapper mapper)
    : IProductAnalyticalRawDataRepository
{
    public async Task<Result<Guid>> CreateAnalyticalRawData(
        CreateProductAnalyticalRawDataRequest request
    )
    {
        var existingAnalyticalRawData = await context
            .ProductAnalyticalRawData.IgnoreQueryFilters()
            .Where(ad => !ad.DeletedAt.HasValue)
            .AnyAsync(ad => ad.SpecNumber == request.SpecNumber);
        if (existingAnalyticalRawData)
        {
            return Error.Validation(
                "ProductAnalyticalRawData.Exists",
                "Analytical raw data already exists."
            );
        }

        var form = await context.Forms.FirstOrDefaultAsync(f => f.Id == request.FormId);

        if (form is null)
        {
            return Error.Validation("Form.Invalid", "Form is invalid.");
        }

        var stpNumber = await context.ProductStandardTestProcedures.AnyAsync(mstp =>
            mstp.Id == request.StpId
        );

        if (!stpNumber)
        {
            return Error.Validation(
                "ProductAnalyticalRawData.StpNumberNotFound",
                "Stp number not found."
            );
        }

        var analyticalRawData = mapper.Map<ProductAnalyticalRawData>(request);

        await context.ProductAnalyticalRawData.AddAsync(analyticalRawData);
        await context.SaveChangesAsync();

        return analyticalRawData.Id;
    }

    public async Task<
        Result<Paginateable<IEnumerable<ProductAnalyticalRawDataDto>>>
    > GetAnalyticalRawData(int page, int pageSize, string searchQuery, bool? isVerified = null)
    {
        var query = context
            .ProductAnalyticalRawData.AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(ad => ad.ProductStandardTestProcedure)
                .ThenInclude(p => p.Product)
            .Where(ad => !ad.DeletedAt.HasValue)
            .AsQueryable();

        if (isVerified.HasValue)
        {
            query = query.Where(p => p.IsVerified == isVerified.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(
                searchQuery,
                ad => ad.SpecNumber,
                ad => ad.ProductStandardTestProcedure.StpNumber,
                ad => ad.ProductStandardTestProcedure.Product.Name
            );
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            if (Enum.TryParse<TestStage>(searchQuery, true, out var stage))
                query = query.Where(ad => ad.Stage == stage);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            entity =>
                mapper.Map<ProductAnalyticalRawDataDto>(
                    entity,
                    opts =>
                    {
                        opts.Items[AppConstants.ModelType] = nameof(ProductAnalyticalRawData);
                    }
                )
        );
    }

    public async Task<Result<ProductAnalyticalRawDataDto>> GetAnalyticalRawData(Guid id)
    {
        var analyticalRawData = await context
            .ProductAnalyticalRawData.AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(ad => ad.Form)
            .Include(ad => ad.ProductStandardTestProcedure)
                .ThenInclude(ad => ad.Product)
            .Include(ad => ad.Form)
                .ThenInclude(f => f.Sections.OrderBy(s => s.Order))
                    .ThenInclude(f => f.Instrument)
                        .ThenInclude(f => f.QcEquipmentCategory)
            .Where(ad => !ad.DeletedAt.HasValue)
            .FirstOrDefaultAsync(ad => ad.Id == id);

        return analyticalRawData is null
            ? Error.NotFound(
                "ProductAnalyticalRawData.NotFound",
                "Product analytical raw data not found"
            )
            : mapper.Map<ProductAnalyticalRawDataDto>(
                analyticalRawData,
                opts =>
                {
                    opts.Items[AppConstants.ModelType] = nameof(ProductAnalyticalRawData);
                }
            );
    }

    public async Task<Result<List<ProductAnalyticalRawDataDto>>> GetAnalyticalRawDataByProduct(
        Guid id
    )
    {
        var analyticalRawData = await context
            .ProductAnalyticalRawData.AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(ad => ad.Form)
                .ThenInclude(f => f.Sections.OrderBy(s => s.Order))
                    .ThenInclude(f => f.Fields.OrderBy(fi => fi.Rank))
                        .ThenInclude(f => f.Question)
            .Include(ad => ad.Form)
                .ThenInclude(f => f.Sections.OrderBy(s => s.Order))
                    .ThenInclude(f => f.Instrument)
                        .ThenInclude(f => f.QcEquipmentCategory)
            .Include(ad => ad.ProductStandardTestProcedure)
                .ThenInclude(ad => ad.Product)
            .Where(ad => ad.ProductStandardTestProcedure.ProductId == id && !ad.DeletedAt.HasValue)
            .ToListAsync();

        return mapper.Map<List<ProductAnalyticalRawDataDto>>(
            analyticalRawData,
            opt =>
            {
                opt.Items[AppConstants.ModelType] = nameof(ProductAnalyticalRawData);
            }
        );
    }

    public async Task<Result<ProductBatchArd>> GetRelevantProductInfoForArd(
        Guid batchManufacturingRecordId,
        TestStage? testStage
    )
    {
        var bmr = await context
            .BatchManufacturingRecords.AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(batchManufacturingRecord => batchManufacturingRecord.IssuedBy)
            .Include(b => b.ProductionScheduleProduct)
            .Where(b => !b.DeletedAt.HasValue)
            .FirstOrDefaultAsync(m => m.Id == batchManufacturingRecordId);

        if (bmr is null)
            return Error.NotFound("Bmr.NotFound", "Bmr not found.");

        var atr = await context
            .AnalyticalTestRequests.AsSplitQuery()
            .Include(a => a.SampledBy)
            .Include(a => a.TestedBy)
            .Include(a => a.IssuedBy)
            .FirstOrDefaultAsync(a => a.BatchManufacturingRecordId == batchManufacturingRecordId);

        var productArd = await context
            .ProductAnalyticalRawData.AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(p => p.ProductStandardTestProcedure)
            .Where(p => !p.DeletedAt.HasValue)
            .FirstOrDefaultAsync(p =>
                p.ProductStandardTestProcedure.ProductId == bmr.ProductionScheduleProduct.ProductId
                && (!testStage.HasValue || p.Stage == testStage.Value)
            );

        var stps = await context
            .ProductStandardTestProcedures.AsSplitQuery()
            .Include(s => s.Product)
            .Where(s => s.ProductId == bmr.ProductionScheduleProduct.ProductId)
            .ToListAsync();

        return new ProductBatchArd
        {
            BatchManufacturingRecord = mapper.Map<BatchManufacturingRecordDto>(bmr),
            ArNumber = atr?.ArNumber,
            SpecNumber = productArd?.SpecNumber,
            ProductStandardTestProcedures = mapper.Map<List<ProductStandardTestProcedureDto>>(stps),
            SampledDate = atr?.SampledAt,
            SampledBy = mapper.Map<UserDto>(atr?.SampledBy),
            SampledQuantity = decimal.TryParse(atr?.SampledQuantity, out var sq) ? sq : null,
            IssueDate = atr?.IssuedAt,
            IssueNumber = atr?.IssueNumber,
            IssuedBy = mapper.Map<UserDto>(atr?.IssuedBy),
            AnalysedDate = atr?.TestedAt,
            AnalysedBy = mapper.Map<UserDto>(atr?.TestedBy),
        };
    }

    public async Task<Result> UpdateAnalyticalRawData(
        Guid id,
        CreateProductAnalyticalRawDataRequest request
    )
    {
        var analyticalRawData = await context.ProductAnalyticalRawData.FirstOrDefaultAsync(ad =>
            ad.Id == id
        );

        if (analyticalRawData is null)
        {
            return Error.NotFound(
                "ProductAnalyticalRawData.NotFound",
                "Product analytical raw data not found"
            );
        }

        mapper.Map(request, analyticalRawData);

        context.ProductAnalyticalRawData.Update(analyticalRawData);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteAnalyticalRawData(Guid id, Guid userId)
    {
        var analyticalRawData = await context.ProductAnalyticalRawData.FirstOrDefaultAsync(ad =>
            ad.Id == id
        );
        if (analyticalRawData is null)
        {
            return Error.NotFound(
                "ProductAnalyticalRawData.NotFound",
                "Product analytical raw data not found"
            );
        }

        analyticalRawData.DeletedAt = DateTime.UtcNow;
        analyticalRawData.LastDeletedById = userId;

        context.ProductAnalyticalRawData.Update(analyticalRawData);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> StartTestForBatchManufacturingRecord(Guid id)
    {
        var batchManufacturingRecord = await context.BatchManufacturingRecords.FirstOrDefaultAsync(
            b => b.Id == id
        );
        if (batchManufacturingRecord is null)
            return Error.NotFound("BMR.NotFound", "BMR not found");

        batchManufacturingRecord.Status = BatchManufacturingStatus.Testing;
        context.BatchManufacturingRecords.Update(batchManufacturingRecord);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
