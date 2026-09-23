namespace SHARED.Requests;

public enum VerifiableEntity
{
    Product = 0,
    MaterialSpecification = 1,
    ProductSpecification = 2,
    MaterialAnalyticalRawData = 3,
    ProductAnalyticalRawData = 4,
    RoutineArd = 5,
    MicrobialRequirement = 6,
}

public class VerifyRequest
{
    public VerifiableEntity ModelType { get; set; }
    public Guid ModelId { get; set; }
}
