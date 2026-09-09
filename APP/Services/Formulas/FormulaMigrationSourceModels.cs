namespace APP.Services.Formulas;

#nullable enable

internal sealed record ArtifactKey(Guid QuestionId, Guid? OptionId, string Path);
internal sealed record SourceArtifact(Guid QuestionId, Guid? QuestionOptionId,
    string LegacyPath, string SourceHash, bool IsActive, int ActivePlacements, int ResponseRows)
{
    public ArtifactKey Key => new(QuestionId, QuestionOptionId, LegacyPath);
}
internal sealed record QuestionRow(Guid Id, DateTime? DeletedAt);
internal sealed record OptionRow(Guid Id, Guid QuestionId, string Name, DateTime CreatedAt,
    DateTime? UpdatedAt, DateTime? DeletedAt);
internal sealed record FieldRow(Guid Id, Guid QuestionId, Guid FormSectionId, DateTime? DeletedAt);
internal sealed record SectionRow(Guid Id, Guid FormId, DateTime? DeletedAt);
internal sealed record FormRow(Guid Id, DateTime? DeletedAt);
internal sealed record ResponseRow(Guid Id, Guid ResponseId, Guid FormFieldId, string Value,
    DateTime CreatedAt, DateTime? DeletedAt);
internal sealed record SourceInventory(IReadOnlyList<SourceArtifact> Artifacts,
    IReadOnlyList<QuestionRow> Questions, IReadOnlyList<OptionRow> Options,
    IReadOnlyList<FieldRow> Fields, IReadOnlyList<SectionRow> Sections,
    IReadOnlyList<FormRow> Forms, IReadOnlyList<ResponseRow> Responses);
