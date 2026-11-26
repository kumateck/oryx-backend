using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.Base;

namespace APP.Utils;

/// <summary>
/// One page of a material's store bin card, newest first, with the totals of the whole card in the department's unit.
/// Received − issued = closing; closing − shelf stock = difference, which is zero when every movement is posted.
/// </summary>
public class BinCardPage : Paginateable<IEnumerable<BinCardInformationDto>>
{
    public UnitOfMeasureDto Unit { get; set; }
    public decimal Received { get; set; }
    public decimal Issued { get; set; }
    public decimal Closing { get; set; }
    public decimal ShelfStock { get; set; }
    public decimal Difference { get; set; }
}
