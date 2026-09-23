using AutoMapper;
using DOMAIN.Entities.QcWorksheets;
using DOMAIN.Entities.Users;

namespace APP.Repository.QcWorksheets;

/// <summary>
/// The one worksheet-summary projection, shared by the round's detail view and the Test Room
/// queues so a worksheet reads identically wherever it appears.
/// </summary>
internal static class QcWorksheetInstanceMapper
{
    internal static WorksheetInstanceSummaryDto ToSummaryDto(WorksheetInstance instance, IMapper mapper) =>
        Fill(new WorksheetInstanceSummaryDto(), instance, mapper);

    internal static TDto Fill<TDto>(TDto dto, WorksheetInstance instance, IMapper mapper)
        where TDto : WorksheetInstanceSummaryDto
    {
        dto.Id = instance.Id;
        dto.TestRequestSubjectId = instance.TestRequestSubjectId;
        dto.WorksheetTemplateId = instance.WorksheetTemplateId;

        // The pin as stored, not as re-read from the template row: a drifted pin should be
        // visible rather than quietly corrected on the way out.
        dto.WorksheetTemplateVersion = instance.WorksheetTemplateVersion;
        dto.WorksheetTemplateCode = instance.WorksheetTemplate?.Code;
        dto.WorksheetTemplateName = instance.WorksheetTemplate?.Name;
        dto.AnalysisType = instance.AnalysisType;
        dto.AssignedToId = instance.AssignedToId;
        dto.AssignedTo = instance.AssignedTo is null ? null : mapper.Map<UserDto>(instance.AssignedTo);
        dto.AssignedById = instance.AssignedById;
        dto.AssignedAt = instance.AssignedAt;
        dto.Status = instance.Status;
        dto.Approved = instance.Approved;
        dto.SubmittedAt = instance.SubmittedAt;
        dto.RetestOfInstanceId = instance.RetestOfInstanceId;
        dto.CreatedAt = instance.CreatedAt;
        dto.CreatedBy = instance.CreatedBy is null ? null : mapper.Map<UserDto>(instance.CreatedBy);
        return dto;
    }
}
