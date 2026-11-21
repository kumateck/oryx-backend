using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Reviews;

namespace DOMAIN.Entities.Costing;

public class PrepareCostingPriceListRequest
{
    public Division Division { get; set; }
    [Required, StringLength(100)] public string DocumentNumber { get; set; }
    [Range(0, 100000)] public int RevisionNumber { get; set; }
    public DateTime EffectiveFrom { get; set; }
    [Required, StringLength(2000)] public string RevisionReason { get; set; }
    public Guid? PreviousRevisionId { get; set; }
}

public class UpdateCostingPriceListRequest : PrepareCostingPriceListRequest
{
    public int Version { get; set; }
}

public class SaveCostingPriceListLineRequest
{
    public int Version { get; set; }
    public Guid ProductId { get; set; }
    public Guid ProductPackingId { get; set; }
    public decimal? Price { get; set; }
    [StringLength(100)] public string PriceUoM { get; set; }
    public Guid? CurrencyId { get; set; }
}

public class SubmitCostingPriceListRequest { public int Version { get; set; } }

public class CostingPackingOptionDto
{
    public Guid ProductId { get; set; }
    public Guid ProductPackingId { get; set; }
    public string ProductCode { get; set; }
    public string ProductName { get; set; }
    public string PackingStyle { get; set; }
}

public class CostingPriceListSummaryDto
{
    public Guid Id { get; set; }
    public Division Division { get; set; }
    public string DocumentNumber { get; set; }
    public int RevisionNumber { get; set; }
    public Guid? PreviousRevisionId { get; set; }
    public int? SupersedesRevision { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public string RevisionReason { get; set; }
    public CostingPriceListStatus Status { get; set; }
    public string Stage { get; set; }
    public DocumentReviewStatus? ReviewStatus { get; set; }
    public int Version { get; set; }
    public int LineCount { get; set; }
    public string PreparedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

public class CostingPriceListSignatoryDto
{
    public string Name { get; set; }
    public string Designation { get; set; }
    public string Signature { get; set; }
    public DateTime? SignedAt { get; set; }
}

public class CostingPriceListDetailDto : CostingPriceListSummaryDto
{
    public CostingPriceListSignatoryDto PreparedBySignatory { get; set; }
    public CostingPriceListSignatoryDto CheckedBy { get; set; }
    public CostingPriceListSignatoryDto ApprovedBy { get; set; }
}

public class CostingPriceListLineDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid ProductPackingId { get; set; }
    public string PackingStyle { get; set; }
    public string ProductCode { get; set; }
    public string ProductName { get; set; }
    public decimal? Price { get; set; }
    public decimal? PreviousPrice { get; set; }
    public string PreviousCurrencySymbol { get; set; }
    public string PreviousPriceUoM { get; set; }
    public Guid? PreviousCurrencyId { get; set; }
    public string PriceUoM { get; set; }
    public Guid? CurrencyId { get; set; }
    public string CurrencyName { get; set; }
    public string CurrencySymbol { get; set; }
}

public class CostingPriceListPrintDto
{
    public CostingPriceListDetailDto Revision { get; set; }
    public List<CostingPriceListLineDto> Lines { get; set; } = [];
}

public class CostingPriceListPageDto
{
    public IEnumerable<CostingPriceListSummaryDto> Data { get; set; } = [];
    public int PageIndex { get; set; }
    public int PageCount { get; set; }
    public int TotalRecordCount { get; set; }
    public int NumberOfPagesToShow { get; set; }
    public int StartPageIndex { get; set; }
    public int StopPageIndex { get; set; }
    public CostingPriceListSummaryDto LatestApproved { get; set; }
    public Guid? OpenRevisionId { get; set; }
    public int NextRevisionNumber { get; set; }
}
