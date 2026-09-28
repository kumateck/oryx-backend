namespace APP.Services.QcWorksheets.WorksheetDocxImport.AiExtraction;

/// <summary>
/// Build brief 11: the one JSON schema both <see cref="AnthropicWorksheetExtractor"/> (a
/// tool-use <c>input_schema</c>) and <see cref="OpenAiWorksheetExtractor"/> (a structured-output
/// <c>json_schema</c>) send, so the two providers are held to exactly the same shape — including
/// a required <c>sourceQuote</c> per field — rather than two hand-maintained copies that could
/// drift. Extracted unchanged from the schema build brief 10's <c>AnthropicWorksheetExtractor</c>
/// already used.
/// </summary>
public static class WorksheetExtractionSchema
{
    public const string ToolName = "propose_worksheet_template";

    public const string Instructions =
        "You extract a QC worksheet template from an already-redacted analytical worksheet document. " +
        "Every field you propose MUST include a sourceQuote: the exact text (verbatim, character for " +
        "character) from the document below that the field was read from. Never invent a sourceQuote. " +
        "A choice field's options must be exactly two mutually exclusive phrases quoted verbatim from " +
        "the document — never invented options. If you recognize a run-data label or choice phrase not " +
        "in the standard dictionary but are confident it is one, add it to dictionarySuggestions as free " +
        "text; never apply it yourself.";

    public static readonly object Schema = new
    {
        type = "object",
        required = new[] { "template" },
        properties = new
        {
            template = new
            {
                type = "object",
                required = new[] { "name", "sections" },
                properties = new
                {
                    code = new { type = "string" },
                    name = new { type = "string" },
                    department = new { type = "string" },
                    category = new { type = "string" },
                    sections = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "object",
                            required = new[] { "name", "fields" },
                            properties = new
                            {
                                name = new { type = "string" },
                                fields = new
                                {
                                    type = "array",
                                    items = new
                                    {
                                        type = "object",
                                        required = new[] { "label", "type", "sourceQuote" },
                                        properties = new
                                        {
                                            fieldKey = new { type = "string" },
                                            label = new { type = "string" },
                                            type = new { type = "string" },
                                            mode = new { type = "string" },
                                            unit = new { type = "string" },
                                            analyte = new { type = "string" },
                                            constantValue = new { type = "string" },
                                            formulaExpression = new { type = "string" },
                                            options = new { type = "array", items = new { type = "string" } },
                                            sourceQuote = new { type = "string" },
                                            confidence = new { type = "string" }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            },
            specificationProposals = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    required = new[] { "testName", "sourceQuote" },
                    properties = new
                    {
                        testName = new { type = "string" },
                        analyte = new { type = "string" },
                        acceptanceCriteria = new { type = "string" },
                        printedCriteria = new { type = "string" },
                        alertLimit = new { type = "string" },
                        actionLimit = new { type = "string" },
                        reference = new { type = "string" },
                        sourceQuote = new { type = "string" },
                        confidence = new { type = "string" }
                    }
                }
            },
            dictionarySuggestions = new { type = "array", items = new { type = "string" } }
        }
    };
}
