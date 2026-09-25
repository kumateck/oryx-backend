using DOMAIN.Entities.QcWorksheets;
using DOMAIN.Entities.Users;

namespace APP.Tests.QcWorksheets;

/// <summary>Shared set-up for the brief 07 execution tests.</summary>
internal static class QcWorksheetRuns
{
    /// <summary>A round with one subject and one started worksheet, assigned to a fresh analyst.</summary>
    internal static async Task<(Guid InstanceId, User Analyst)> StartedWorksheet(
        QcWorksheetTestContext harness, WorksheetTemplate template)
    {
        var analyst = await harness.SeedUser("analyst.a");
        var specification = await harness.SeedEffectiveSpecification(
            SpecificationAppliesTo.RoutineEnvironmental, (template, SpecificationAnalysisType.Microbial));

        var created = await harness.TestRequests.CreateTestRequest(
            new CreateTestRequestRequest
            {
                Type = TestRequestType.RoutineEnvironmental,
                SpecificationId = specification.Id,
                ScheduleOrigin = TestRequestScheduleOrigin.Scheduled,
                ArNumber = "ARD-BRIEF07",
                Subjects = [new CreateTestRequestSubjectRequest { SubjectRef = "PW-01", SubjectLabel = "Point 1" }]
            },
            Guid.NewGuid());

        await harness.TestRequests.RecordSample(created.Value.Id, new RecordTestRequestSampleRequest(), Guid.NewGuid());
        var instanceId = created.Value.Subjects.Single().WorksheetInstances.Single().Id;
        await harness.WorksheetInstances.Assign(
            instanceId, new AssignWorksheetInstanceRequest { AssignedToId = analyst.Id }, Guid.NewGuid());
        await harness.WorksheetInstances.Start(instanceId, analyst.Id);
        return (instanceId, analyst);
    }

    internal static SaveWorksheetValuesRequest Values(params WorksheetFieldValueEntry[] entries) =>
        new() { FieldValues = entries.ToList() };

    internal static WorksheetFieldValueEntry Cell(string table, string column, int row, string value) =>
        new() { FieldKey = table, ColumnKey = column, RowIndex = row, Value = value };
}
