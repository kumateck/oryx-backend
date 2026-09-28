namespace DOMAIN.Entities.QcWorksheets;

/// <summary>The reader's normalized model, returned so a review screen can show the source.</summary>
public class ImportSourceDocument
{
    public string HeaderText { get; set; }
    public List<ImportSourceBlock> Blocks { get; set; } = [];
}

public class ImportSourceBlock
{
    public int Index { get; set; }

    /// <summary>Heading, Paragraph or Table.</summary>
    public string Kind { get; set; }

    public string Text { get; set; }
    public int? Table { get; set; }
    public List<List<string>> Rows { get; set; }
}

/// <summary>
/// One template shared by several files of a family (every EM area sheet proposes the same
/// "Environmental Monitoring – Airborne Viables" template).
/// <list type="bullet">
/// <item>In an upload of several such files, the template is proposed once, on the file named by
/// <see cref="CarriedBy"/>; the others carry only their sampling-point, group and specification
/// proposals and the flag <c>SharedTemplateInBatch</c>.</item>
/// <item>When the template is already saved, no file proposes it: <see cref="ExistingTemplateId"/> /
/// <see cref="ExistingTemplateCode"/> name it, every file carries <c>SharedTemplateExists</c>, and
/// the specification proposals point at that template.</item>
/// </list>
/// </summary>
public class SharedTemplateReference
{
    /// <summary>Stable identity of the shared template (its code), e.g. "EM-AIRBORNE-VIABLES".</summary>
    public string Key { get; set; }

    /// <summary>The file in this upload whose proposal carries the template; null when it already exists.</summary>
    public string CarriedBy { get; set; }

    public Guid? ExistingTemplateId { get; set; }
    public string ExistingTemplateCode { get; set; }
}
