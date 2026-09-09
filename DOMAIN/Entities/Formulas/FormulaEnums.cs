namespace DOMAIN.Entities.Formulas;

public enum FormulaRevisionStatus
{
    Draft = 0,
    InReview = 1,
    Approved = 2,
    Retired = 3
}

public enum FormRevisionStatus
{
    Draft = 0,
    InReview = 1,
    Approved = 2,
    Retired = 3
}

public enum FormulaMigrationClass
{
    Exact = 0,
    Normalized = 1,
    Corrective = 2,
    Unrecoverable = 3
}

public enum FormulaSnapshotReason
{
    Initial = 0,
    ApprovedRebase = 1,
    HistoricalCorrection = 2
}

public enum FormulaExecutionAuthority
{
    ProvisionalClient = 0,
    AuthoritativeServer = 1
}

public enum FormulaExecutionTrigger
{
    DraftEdit = 0,
    FinalSubmission = 1,
    ApprovedRevalidation = 2,
    Correction = 3
}

public enum FormulaExecutionStatus
{
    Valid = 0,
    MissingInput = 1,
    InvalidInput = 2,
    DivideByZero = 3,
    DomainError = 4,
    ResourceLimit = 5,
    PolicyMismatch = 6,
    EngineError = 7
}

public enum FormulaMigrationRunMode
{
    DryRun = 0,
    Apply = 1
}

public enum FormulaMigrationRunStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3
}

public enum FormulaMigrationItemStatus
{
    Pending = 0,
    Applied = 1,
    Skipped = 2,
    Failed = 3
}

public enum FormulaMigrationApprovalScope
{
    Batch = 0,
    Individual = 1
}
