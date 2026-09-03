namespace APP.Repository;

internal static class ApprovalDocumentPolicy
{
    internal const string StockRequisition = "StockRequisition";

    internal static string ConfigurationTypeFor(string modelType) => modelType switch
    {
        "RawStockRequisition" or "PackageStockRequisition" => StockRequisition,
        _ => modelType,
    };
}
