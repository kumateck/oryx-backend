#nullable enable

using System.Text.Json;
using System.Text.Json.Nodes;

namespace APP.Services.QcWorksheets.WorksheetDocxImport.AiExtraction;

/// <summary>
/// Adapts the shared extraction shape to OpenAI's strict structured-output rules.
/// Optional source fields remain nullable so the model need not invent QC values.
/// </summary>
public static class OpenAiWorksheetSchema
{
    public static readonly JsonElement Schema = Build();

    private static JsonElement Build()
    {
        var schema = JsonSerializer.SerializeToNode(WorksheetExtractionSchema.Schema)!;
        MakeStrict(schema);
        return JsonSerializer.SerializeToElement(schema);
    }

    private static void MakeStrict(JsonNode node)
    {
        if (node is not JsonObject obj) return;

        if (obj["properties"] is JsonObject properties)
        {
            var originallyRequired = obj["required"] is JsonArray required
                ? required.Select(item => item!.GetValue<string>()).ToHashSet(StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);

            foreach (var property in properties)
            {
                if (property.Value is not JsonObject child) continue;
                if (!originallyRequired.Contains(property.Key) && child["type"] is JsonValue type)
                    child["type"] = new JsonArray(
                        JsonValue.Create(type.GetValue<string>()), JsonValue.Create("null"));
                MakeStrict(child);
            }

            obj["required"] = new JsonArray(properties.Select(property =>
                (JsonNode?)JsonValue.Create(property.Key)).ToArray());
            obj["additionalProperties"] = false;
        }

        if (obj["items"] is JsonNode items)
            MakeStrict(items);
    }
}
