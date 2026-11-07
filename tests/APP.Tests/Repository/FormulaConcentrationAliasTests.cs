using System.Text.Json;
using System.Text.Json.Nodes;
using APP.Services.Formulas;
using Xunit;

namespace APP.Tests.Repository;

public sealed class FormulaConcentrationAliasTests
{
    private static string Configuration(string expression, string unit = "mg/mL", string inputKey = "wt") => JsonSerializer.Serialize(new
    {
        type = 3, variableKeyFormat = 1, concentrationUnit = unit, expression,
        standardInTable = true, noOfSamples = 2, decimalPlaces = 2,
        standardInputs = new[] { new { key = inputKey, label = "Weight (g)" } },
        sampleInputs = new[] { new[] { new { key = inputKey } }, new[] { new { key = inputKey } } }
    });
    private const string Values = """
        {"standardInputs":{"wt":"8"},"sampleInputs":[{"wt":"2"},{"wt":"4"}],
        "manualVars":{"standardConc":{"raw":"999"},"sampleConc":{"raw":"999"}}}
        """;

    [Theory]
    [InlineData(":standardConc/:sampleConc", ":standardWeight/:sampleWeight")]
    [InlineData("(:standardConc+:standardWeight)/(:sampleConc+:sampleWeight)", "(:standardWeight+:standardWeight)/(:sampleWeight+:sampleWeight)")]
    [InlineData(":standardConc/:sample2Conc", ":standardWeight/:sample2Weight")]
    public void Compiles_aliases_to_the_same_executable_definition_and_hash(string expression, string weightExpression)
    {
        var raw = Configuration(expression);
        Assert.Null(LegacyFormulaRepairDefinition.Validate(raw, raw));
        var alias = LegacyFormulaRepairDefinition.Compile(raw);
        var weight = LegacyFormulaRepairDefinition.Compile(Configuration(weightExpression));
        Assert.Equal(weight.Hash, alias.Hash);
        Assert.Equal(weight.Definition.GetRawText(), alias.Definition.GetRawText());
        Assert.DoesNotContain("Conc", alias.Definition.GetRawText());
        var variables = alias.Definition.GetProperty("variables").EnumerateArray().Select(v => v.GetProperty("key").GetString()).ToArray();
        Assert.Equal(variables.Distinct().Count(), variables.Length);
    }

    [Theory]
    [InlineData("mg/mL", "wt")]
    [InlineData("%", "wt")]
    [InlineData("mg/mL", "weight")]
    [InlineData("%", "weight")]
    public void Reads_one_standard_value_and_each_samples_own_value(string unit, string inputKey)
    {
        var raw = Configuration("(:standardConc+:standardWeight)/(:sampleConc+:sampleWeight)", unit, inputKey);
        var values = Values.Replace("\"wt\":", "\"" + inputKey + "\":", StringComparison.Ordinal);
        var inputs = LegacyFormulaRepairDefinition.ResolveInputs(raw, values);
        Assert.Equal(3, inputs.Count);
        Assert.Equal("8", inputs["standardWeight"]);
        Assert.Equal("2", inputs["sample1Weight"]);
        Assert.Equal("4", inputs["sample2Weight"]);
    }

    [Fact]
    public void Preserves_old_manual_variables_without_the_explicit_marker()
    {
        var config = JsonNode.Parse(Configuration(":standardConc/:sampleConc"))!.AsObject();
        config.Remove("concentrationUnit");
        config["manualVarLabels"] = new JsonObject { ["standardConc"] = "Custom standard", ["sampleConc"] = "Custom sample" };
        var raw = config.ToJsonString();
        Assert.Null(LegacyFormulaRepairDefinition.Validate(raw, raw));
        Assert.Equal("999", LegacyFormulaRepairDefinition.ResolveInputs(raw, Values)["sampleConc"]);
        Assert.Contains(":standardConc/:sampleConc", LegacyFormulaRepairDefinition.Compile(raw).Definition.GetRawText());
    }

    [Fact]
    public void Configuration_hash_keeps_expression_and_unit_changes_visible_to_stale_save_checks()
    {
        var weight = Configuration(":standardWeight/:sampleWeight");
        var alias = Configuration(":standardConc/:sampleConc");
        var percent = Configuration(":standardConc/:sampleConc", "%");
        Assert.NotEqual(LegacyFormulaRepairDefinition.Hash(weight), LegacyFormulaRepairDefinition.Hash(alias));
        Assert.NotEqual(LegacyFormulaRepairDefinition.Hash(alias), LegacyFormulaRepairDefinition.Hash(percent));
        Assert.Equal(LegacyFormulaRepairDefinition.Compile(alias).Hash, LegacyFormulaRepairDefinition.Compile(percent).Hash);
        // The bounded repair workflow still prohibits unit/configuration changes.
        Assert.NotNull(LegacyFormulaRepairDefinition.Validate(alias, percent));
    }

    [Fact]
    public void Does_not_treat_unconfigured_or_similarly_named_variables_as_aliases()
    {
        var raw = Configuration(":standardConcExtra/:sampleConc", inputKey: "amount");
        Assert.NotNull(LegacyFormulaRepairDefinition.Validate(raw, raw));
        Assert.Contains("standardConcExtra", LegacyFormulaRepairDefinition.Compile(raw).Definition.GetRawText());
    }

    private sealed class EngineTheoryAttribute : TheoryAttribute
    {
        public EngineTheoryAttribute()
        {
            if (Environment.GetEnvironmentVariable("ORYX_FORMULA_REPAIR_SERVICE_URL") is null)
                Skip = "Set ORYX_FORMULA_REPAIR_SERVICE_URL to an isolated formula engine.";
        }
    }

    [EngineTheory]
    [InlineData(":standardConc/:sampleConc")]
    [InlineData("(:standardConc+:standardWeight)/(:sampleConc+:sampleWeight)")]
    [InlineData(":standardWeight/:sampleWeight")]
    public async Task Real_service_validates_and_evaluates_distinct_samples(string expression)
    {
        var raw = Configuration(expression);
        var compiled = LegacyFormulaRepairDefinition.Compile(raw);
        var url = new Uri(Environment.GetEnvironmentVariable("ORYX_FORMULA_REPAIR_SERVICE_URL")!);
        using var http = new HttpClient { BaseAddress = url };
        http.DefaultRequestHeaders.Authorization = new("Bearer", "test-only");
        var client = new FormulaCalculationClient(http, new(true, url, "test-only", TimeSpan.FromSeconds(5)));
        var validation = await client.ValidateAsync(LegacyFormulaRepairDefinition.ValidationRequest(raw));
        Assert.True(validation.IsSuccess);
        Assert.Equal(5, validation.Value.Status);
        Assert.Equal(compiled.Hash, validation.Value.ComputedDefinitionHash);
        var configuration = JsonSerializer.SerializeToElement(new
        {
            placementKey = "concentration-test", definitionHash = compiled.Hash, methodReference = (string?)null,
            displayPolicy = new { policyVersion = "oryx-formula-display-v1", showResult = true, showUnit = true, showTrace = false },
            bindings = compiled.Definition.GetProperty("variables").EnumerateArray().Select(v => new
            {
                variableKey = v.GetProperty("key").GetString(),
                source = new { sourceType = 0, reference = "local:" + v.GetProperty("key").GetString(), dataType = 0, valueShape = 0, scope = 0, unit = (string?)null }
            }).ToArray(),
            resultTargets = compiled.Definition.GetProperty("results").EnumerateArray().Select(r => new
            { resultKey = r.GetProperty("key").GetString(), targetKey = "concentration-test:" + r.GetProperty("key").GetString(), dataType = 0, valueShape = 0, unit = (string?)null }).ToArray()
        });
        var projection = JsonNode.Parse(configuration.GetRawText())!.AsObject();
        projection["configurationVersion"] = "oryx-formula-configuration-v1";
        var configurationHash = FormulaCanonicalJson.HashJson("oryx:formula-configuration:v1", projection.ToJsonString(), 262144);
        var result = await client.EvaluateAsync(JsonSerializer.SerializeToElement(new
        {
            schemaVersion = "oryx-formula-service-evaluate-request-v1", requestId = "concentration-alias-test",
            formulaLanguageVersion = "oryx-formula-v1", numericPolicyVersion = LegacyFormulaRepairDefinition.Policy,
            definitionHash = compiled.Hash, definition = compiled.Definition,
            configuration, configurationHash,
            resolvedInputs = LegacyFormulaRepairDefinition.ResolveInputs(raw, Values)
        }));
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Status);
        Assert.Equal(configurationHash, result.Value.ComputedConfigurationHash);
        Assert.NotNull(result.Value.Evaluation);
        Assert.Equal(new[] { "4.00", "2.00" }, result.Value.Evaluation.Value.GetProperty("results")
            .EnumerateArray().Select(item => item.GetProperty("roundedResult").GetString()));
    }
}
