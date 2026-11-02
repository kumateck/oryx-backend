namespace APP.Services.Materials;

/// <summary>Missing supplier batches never identify the same stock lot.</summary>
public static class OpeningBalanceBatches
{
    public static (string Batch, Guid UnbatchedLine) Identity(Guid lineId, string batchNumber) =>
        string.IsNullOrWhiteSpace(batchNumber)
            ? (string.Empty, lineId)
            : (batchNumber.Trim().ToUpperInvariant(), Guid.Empty);
}
