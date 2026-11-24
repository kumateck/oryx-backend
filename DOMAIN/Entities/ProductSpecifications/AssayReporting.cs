namespace DOMAIN.Entities.ProductSpecifications;

/// <summary>One explicitly selected percentage-of-label-claim output reported in % w/v for this product specification only.</summary>
public sealed record AssayReportingRule(Guid FormFieldId, string ResultKey, string DefinitionHash,
    decimal NominalConcentration, decimal Minimum, decimal Maximum, int DecimalPlaces = 4);
public sealed record SaveAssayReportingRequest(string ExpectedToken, string Reason, List<AssayReportingRule> Rules);
public sealed record AssayOutputDto(string Key, string Label, string Unit = null, bool CanConvert = true, string UnavailableReason = null);
public sealed record AssayReportingFieldDto(Guid FormFieldId, string TestName, string Label, string Specification,
    string DefinitionHash, List<AssayOutputDto> Outputs, string UnavailableReason);
public sealed record AssayReportingConfigurationDto(Guid SpecificationId, Guid ProductId, string ProductName, int TestStage,
    string SpecificationNumber, string Token, List<AssayReportingFieldDto> Fields, List<AssayReportingRule> Rules);
/// <summary>Source and converted values are captured at certificate submission, independently of later master changes.</summary>
public sealed record AssayReportedResultDto(string Key, string Label, decimal SourceValue, decimal Value, string DisplayValue,
    string Unit, decimal Minimum, decimal Maximum, bool? Complies, decimal NominalConcentration, string DefinitionHash, bool Converted = true, int DecimalPlaces = 4);
public sealed record AssayReportingFieldSnapshot(Guid FormFieldId, Guid SpecificationId, Guid ProductId, string SpecificationRevision,
    List<AssayReportedResultDto> Results, string Error, Guid? AnswerId = null, string AnswerHash = "", string DefinitionJson = "");
public sealed record AssayReportingSnapshot(Guid ActorId, DateTime CapturedAt, List<AssayReportingFieldSnapshot> Fields);
