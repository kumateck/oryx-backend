using System.Text.RegularExpressions;
using APP.Services.QcWorksheets.WorksheetDocxImport;
using DOMAIN.Entities.QcWorksheets;
using Xunit;
using Xunit.Abstractions;

namespace APP.Tests.QcWorksheets.WorksheetImport;

/// <summary>The 7 raw-material worksheet / Specification pairs (brief 09). Runs only with <c>QC_RM_CORPUS_DIR</c> set.</summary>
public class RmCorpusTests(ITestOutputHelper output)
{
    /// <summary>Per worksheet: numbered tests, Instrument fields, W1/W2/W3 results, titration, peak areas.</summary>
    private static readonly Dictionary<string, (int Tests, int Instruments, string[] Crucible, bool Titration, bool PeakAreas)> Worksheets = new()
    {
        ["012"] = (7, 7, ["Sulfated Ash"], false, true),
        ["017"] = (11, 7, ["Sulfated Ash"], true, false),
        ["021"] = (11, 5, [], false, false),
        ["024"] = (9, 4, ["Loss on Drying", "Sulfated Ash"], false, false),
        ["167"] = (7, 7, ["Sulfated Ash"], false, true),
        ["179"] = (6, 5, ["Sulfated Ash", "Loss on Drying"], true, false),
        ["193"] = (8, 2, ["Loss on Drying"], false, false)
    };

    /// <summary>Per Specification: characteristics, MICROBIAL ones, and sections added to the worksheet for it.</summary>
    private static readonly Dictionary<string, (int Characteristics, int Microbial, int Added)> Specifications = new()
    {
        ["012"] = (12, 0, 2),
        ["017"] = (13, 0, 1),
        ["021"] = (11, 0, 0),
        ["024"] = (13, 4, 1),
        ["167"] = (21, 0, 3),
        ["179"] = (9, 0, 1),
        ["193"] = (15, 6, 2)
    };

    private static RmFile Worksheet(List<RmFile> files, string key) => files.Single(file => file.Key == key && !file.IsSpecification);
    private static RmFile Specification(List<RmFile> files, string key) => files.Single(file => file.Key == key && file.IsSpecification);

    [RmCorpusFact]
    public void All_fourteen_files_are_classified()
    {
        var files = RmCorpus.Load();

        Assert.Equal(14, files.Count);
        foreach (var file in files)
        {
            Assert.Equal(file.IsSpecification ? ArdFamily.RawMaterialSpecification : ArdFamily.RawMaterialChemical, file.Proposal.Family);
            Assert.Equal(file.Key, file.Proposal.RawMaterial.PairingKey);
            Assert.DoesNotContain(file.Proposal.Flags, flag => flag.Code is WorksheetImportFlagCodes.UnknownFamily or WorksheetImportFlagCodes.MissingMetadata);
        }
    }

    [RmCorpusFact]
    public void Each_worksheet_has_its_tests_instruments_and_formulas()
    {
        // Worksheets alone: nothing is added for a Specification.
        var files = RmCorpus.Load(include: name => !name.Contains("spec", StringComparison.OrdinalIgnoreCase));

        foreach (var (key, expected) in Worksheets)
        {
            var proposal = Worksheet(files, key).Proposal;
            var template = proposal.Template;
            var fields = RmCorpus.Fields(proposal);

            Assert.Equal(($"RM-{key}", WorksheetCategory.Chemical), (template.Code, template.Category));
            Assert.Equal(expected.Tests, template.Sections.Count);
            Assert.Equal(expected.Instruments, fields.Count(field => field.Type == WorksheetFieldType.Instrument));
            Assert.Equal(expected.Instruments, proposal.EquipmentMatches.Count);

            foreach (var name in expected.Crucible)
            {
                var section = template.Sections.Single(item => item.Name == name);
                var weights = section.Fields.Where(field => Regex.IsMatch(field.FieldKey, "_w[123]$")).ToList();
                Assert.Equal(3, weights.Count);
                Assert.All(weights, field => Assert.Equal((WorksheetFieldType.Number, WorksheetFieldMode.Entry), (field.Type, field.Mode)));

                var result = section.Fields.Single(field => field.Type == WorksheetFieldType.Result);
                Assert.Equal((WorksheetFieldMode.Calculated, "%"), (result.Mode, result.Unit));
                var (w1, w2, w3) = (weights[0].FieldKey, weights[1].FieldKey, weights[2].FieldKey);
                Assert.Equal($"(({{{w2}}} - {{{w3}}}) * 100) / ({{{w2}}} - {{{w1}}})", result.FormulaExpression);
                Assert.True(APP.Services.QcWorksheets.QcFormulaEvaluator.Analyze(result.FormulaExpression).IsValid);
            }

            // Brief 12: a loss on drying takes its definition's formula; every Sulfated Ash sheet prints
            // the loss arrangement (W2 − W3) instead of the residue, so there the printed formula wins.
            foreach (var name in expected.Crucible)
            {
                var code = name == "Loss on Drying" ? WorksheetImportFlagCodes.FormulaFromDefinition : WorksheetImportFlagCodes.FormulaFromPrint;
                Assert.Contains(proposal.Flags, flag => flag.Code == code && flag.Message.StartsWith($"{name} /"));
            }

            var titration = fields.SingleOrDefault(field => field.FieldKey.EndsWith("_titration"));
            Assert.Equal(expected.Titration, titration is not null);
            if (titration is not null)
            {
                // One row per sample; the blank's readings are fields, because each row's assay subtracts the same blank titre.
                Assert.Contains("\"fixedValues\":[\"Sample 1\",\"Sample 2\"]", titration.ColumnDefinitions);
                Assert.Contains("\"formula\":\"{finalVolume} - {initialVolume}\"", titration.ColumnDefinitions);
                Assert.Contains("\"key\":\"assay\"", titration.ColumnDefinitions);
                Assert.Contains(fields, field => field.FieldKey.EndsWith("_blank_titre") && field.Mode == WorksheetFieldMode.Calculated);
                Assert.Contains(fields, field => field.FieldKey.EndsWith("_factor") && field.Type == WorksheetFieldType.Number);
                Assert.Contains(fields, field => field.Label == "Equivalence" && field.Mode == WorksheetFieldMode.Constant);
                var assay = fields.Single(field => field.FieldKey.EndsWith("titration_result"));
                Assert.Equal((WorksheetFieldMode.Calculated, $"AVG({{{titration.FieldKey}.assay}})"), (assay.Mode, assay.FormulaExpression));
            }

            Assert.Equal(expected.PeakAreas, fields.Any(field => field.FieldKey.EndsWith("_peak_areas")));

            // Every test of these seven sheets has a definition, so no formula is left for review.
            Assert.DoesNotContain(proposal.Flags, flag => flag.Code == WorksheetImportFlagCodes.FormulaNeedsReview);
            Assert.All(fields.Where(field => field.Mode == WorksheetFieldMode.Calculated),
                field => Assert.True(APP.Services.QcWorksheets.QcFormulaEvaluator.Analyze(field.FormulaExpression).IsValid, field.FieldKey));
            Assert.Empty(proposal.SpecificationProposals);

            // Every test has something a Specification characteristic can bind to.
            Assert.All(template.Sections, section => Assert.NotNull(RawMaterialResultField.Choose(section)));
        }
    }

    [RmCorpusFact]
    public void No_header_run_data_reaches_the_template()
    {
        var labels = new Regex(@"(Batch\s*No\.?|Quantity\s+Received|A\.R\.\s*No\.|Quantity\s+Sampled|Mfg\.\s*Date|Sampled\s+on|Issue\s+No\.|"
                               + @"Exp\.\s*Date|Sampled\s+by|Issued\s+by|GRN\s+No\.|Analysed\s+Date|Issue\s+Date|Supplier/Manufacturer|Spec\.\s*No\.|STP\s+No\.)\s*:",
            RegexOptions.IgnoreCase);

        foreach (var file in RmCorpus.Load().Where(file => !file.IsSpecification))
        {
            var header = file.Document.HeaderText;
            var starts = labels.Matches(header).ToList();
            var values = starts.Select((match, index) =>
                    ImportText.Normalize(header[(match.Index + match.Length)..(index + 1 < starts.Count ? starts[index + 1].Index : header.Length)]))
                .Where(value => value.Length >= 4)
                .ToList();

            var fields = RmCorpus.Fields(file.Proposal);
            var texts = fields.Where(field => field.Mode == WorksheetFieldMode.Constant).Select(field => field.ConstantValue!)
                .Concat(fields.Select(field => field.Label))
                .Append(file.Proposal.Template.Name).Append(file.Proposal.Template.Code).ToList();

            Assert.True(values.Count >= 10, $"{file.FileName}: only {values.Count} header values");
            foreach (var value in values)
                Assert.DoesNotContain(texts, text => text.Contains(value, StringComparison.OrdinalIgnoreCase));

            // Sign-off rows are dropped.
            Assert.DoesNotContain(fields, field => ImportText.Canonical(field.Label).Contains("analysedby"));
        }
    }

    [RmCorpusFact]
    public void Every_specification_is_parsed_with_its_references_and_groups()
    {
        var files = RmCorpus.Load();
        foreach (var (key, expected) in Specifications)
        {
            var proposal = Specification(files, key).Proposal;
            var rows = proposal.SpecificationProposals;

            Assert.Null(proposal.Template);
            Assert.Equal(expected.Characteristics, rows.Count);
            Assert.Equal(expected.Microbial, rows.Count(row => row.GroupName == "MICROBIAL"));
            Assert.All(rows, row =>
            {
                Assert.Contains(row.GroupName, new[] { "MICROBIAL", "CHEMICAL" });
                Assert.False(string.IsNullOrWhiteSpace(row.Reference));
                Assert.Equal(row.AcceptanceCriteria, row.ActionLimit);
                Assert.Null(row.Stage);
            });
            Assert.StartsWith("QCD/SPC/RM/" + key, proposal.RawMaterial.SpecificationCode);
            Assert.NotNull(proposal.RawMaterial.Revision);
        }

        var cipro = Specification(files, "012").Proposal;
        Assert.Contains(cipro.SpecificationProposals, row => (row.TestName, row.Analyte, row.AcceptanceCriteria) == ("Related Substances", "Impurity E", "NMT 0.30%"));
        Assert.Single(cipro.Flags, flag => flag.Code == WorksheetImportFlagCodes.UnrecognizedContent);

        var levofloxacin = Specification(files, "167").Proposal.SpecificationProposals;
        Assert.Contains(levofloxacin, row => (row.TestName, row.Analyte, row.AcceptanceCriteria) == ("Organic Impurities", "N-Desmethyl levofloxacin (Procedure 1)", "0.30%"));
        Assert.Contains(levofloxacin, row => (row.Analyte, row.AcceptanceCriteria) == ("D-Isomer (Procedure 1)", "0.80%"));
        Assert.Contains(levofloxacin, row => (row.Analyte, row.AcceptanceCriteria) == ("Levofloxacin related compound B (Procedure 2)", "0.13%"));
        Assert.Contains(levofloxacin, row => (row.Analyte, row.AcceptanceCriteria) == ("Procedure 3", "Not more than 0.10%"));
        Assert.Equal("USP", levofloxacin[0].Reference);

        var maize = Specification(files, "024").Proposal.SpecificationProposals.Where(row => row.GroupName == "MICROBIAL").ToList();
        Assert.Equal(["TAMC", "TYMC", "Escherichia coli", "Salmonella spp"], maize.Select(row => row.Analyte));
        Assert.All(maize, row => Assert.Equal("Microbial Contamination", row.TestName));
        Assert.Equal("Not more than 10^3 CFU/g", maize[0].AcceptanceCriteria);

        var xmox = Specification(files, "193").Proposal;
        Assert.Equal(("QCD/SPC/RM/193", "03"), (xmox.RawMaterial.SpecificationCode, xmox.RawMaterial.Revision));
        Assert.Equal(["Description", "Cap Colour", "Body Colour", "Odour", "Disintegration Time", "Loss on drying", "Average weight",
            "Closed Joined Length", "Printing Details"], xmox.SpecificationProposals.Where(row => row.GroupName == "CHEMICAL").Select(row => row.TestName));
        Assert.All(xmox.SpecificationProposals.Where(row => row.GroupName == "MICROBIAL"), row => Assert.Equal("Microbial Test", row.TestName));
        Assert.Equal("In-House", xmox.SpecificationProposals[0].Reference);
    }

    [RmCorpusFact]
    public void Each_specification_binds_to_its_worksheet_and_adds_sections_for_missing_tests()
    {
        var files = RmCorpus.Load();
        foreach (var (key, expected) in Specifications)
        {
            var specification = Specification(files, key).Proposal;
            var worksheet = Worksheet(files, key).Proposal;
            var keys = RmCorpus.Fields(worksheet).ToDictionary(field => field.FieldKey);

            Assert.Equal(RawMaterialPairingStatus.InUpload, specification.RawMaterial.Pairing);
            Assert.Equal(Worksheet(files, key).FileName, specification.RawMaterial.PairedFileName);
            Assert.All(specification.SpecificationProposals, row =>
            {
                Assert.NotNull(row.SourceFieldKey);
                Assert.True(keys.ContainsKey(row.SourceFieldKey!), $"{key}: {row.TestName} → {row.SourceFieldKey}");
                Assert.Null(row.SourceWorksheetTemplateId);
            });
            Assert.DoesNotContain(specification.Flags, flag => flag.Code is WorksheetImportFlagCodes.WorksheetNotFound or WorksheetImportFlagCodes.FieldNotOnTemplate);

            var added = worksheet.Flags.Count(flag => flag.Code == WorksheetImportFlagCodes.AddedForSpecification);
            Assert.Equal(expected.Added, added);
            Assert.Equal(expected.Added, specification.Flags.Count(flag => flag.Code == WorksheetImportFlagCodes.AddedForSpecification));
            Assert.Equal(Worksheets[key].Tests + expected.Added, worksheet.Template.Sections.Count);
            foreach (var section in worksheet.Template.Sections.Skip(Worksheets[key].Tests))
            {
                Assert.Equal((WorksheetFieldType.FileUpload, "Attach print out"), (section.Fields[^1].Type, section.Fields[^1].Label));
                Assert.All(section.Fields.SkipLast(1), field => Assert.Equal((WorksheetFieldType.LongText, WorksheetFieldMode.Entry), (field.Type, field.Mode)));
            }

            // Microbial rows bind to the added sections of the same chemical template.
            Assert.All(specification.SpecificationProposals.Where(row => row.GroupName == "MICROBIAL"),
                row => Assert.Contains(worksheet.Template.Sections.Skip(Worksheets[key].Tests), section => section.Fields.Any(field => field.FieldKey == row.SourceFieldKey)));
        }

        // Spot checks of the name matching: synonyms, sub-tests, and a broader section name.
        Assert.Equal("sulfated_ash_result", Bound(files, "024", "Sulphated Ash"));
        Assert.Equal("sulfated_ash_result", Bound(files, "167", "Residue on Ignition"));
        Assert.Equal("identity_test_ir_observation", Bound(files, "017", "Identification Tests", "IR"));
        Assert.Equal("specific_optical_rotation_result", Bound(files, "017", "Identification Tests", "Specific Optical Rotation"));
        Assert.Equal("description_appearance_result", Bound(files, "193", "Description"));
        Assert.Equal("assay_titration_result", Bound(files, "179", "Assay"));
    }

    [RmCorpusFact]
    public void A_saved_worksheet_binds_the_specification_without_being_modified()
    {
        var sections = new List<CatalogSection> { new("Description / Appearance", "description_appearance_result"), new("Sulfated Ash", "sulfated_ash_result") };
        var saved = new CatalogTemplate(Guid.NewGuid(), "RM-012", "saved", ["description_appearance_result", "sulfated_ash_result"], sections);
        var catalog = new InMemoryWorksheetImportCatalog([], [], [], null, [saved]);

        var specification = RmCorpus.Load(catalog, name => name.StartsWith("012") && name.Contains("spec")).Single().Proposal;

        Assert.Equal((RawMaterialPairingStatus.ExistingTemplate, saved.Id, "RM-012"),
            (specification.RawMaterial.Pairing, specification.RawMaterial.PairedTemplateId, specification.RawMaterial.PairedTemplateCode));
        Assert.All(specification.SpecificationProposals, row => Assert.Equal(saved.Id, row.SourceWorksheetTemplateId));
        Assert.Equal("sulfated_ash_result", specification.SpecificationProposals.Single(row => row.TestName == "Sulfated Ash").SourceFieldKey);
        var unbound = specification.SpecificationProposals.Count(row => row.SourceFieldKey is null);
        Assert.Equal(10, unbound);
        Assert.Equal(unbound, specification.Flags.Count(flag => flag.Code == WorksheetImportFlagCodes.FieldNotOnTemplate));
        Assert.DoesNotContain(specification.Flags, flag => flag.Code == WorksheetImportFlagCodes.AddedForSpecification);
    }

    [RmCorpusFact]
    public void A_specification_alone_asks_for_its_worksheet_first()
    {
        var specification = RmCorpus.Load(include: name => name.StartsWith("179") && name.Contains("spec")).Single().Proposal;

        Assert.Equal(RawMaterialPairingStatus.Missing, specification.RawMaterial.Pairing);
        Assert.Contains("import 179's worksheet first", Assert.Single(specification.Flags, flag => flag.Code == WorksheetImportFlagCodes.WorksheetNotFound).Message);
        Assert.All(specification.SpecificationProposals, row => Assert.Null(row.SourceFieldKey));
    }

    [RmCorpusFact]
    public void Coverage_report()
    {
        foreach (var file in RmCorpus.Load())
        {
            var proposal = file.Proposal;
            if (file.IsSpecification)
            {
                output.WriteLine($"{file.FileName}: {proposal.SpecificationProposals.Count} characteristics, "
                                 + $"{proposal.SpecificationProposals.Count(row => row.SourceFieldKey is not null)} bound, "
                                 + $"{proposal.Flags.Count(flag => flag.Code == WorksheetImportFlagCodes.AddedForSpecification)} sections added, "
                                 + $"flags: {string.Join(", ", proposal.Flags.GroupBy(flag => flag.Code).Select(group => $"{group.Key}×{group.Count()}"))}");
                continue;
            }

            var provenance = proposal.FieldProvenance.Where(item => item.ColumnKey is null).ToList();
            var high = provenance.Count(item => item.Confidence == ImportConfidence.High);
            output.WriteLine($"{file.FileName}: {proposal.Template.Sections.Count} sections, {provenance.Count} fields, "
                             + $"{high} High ({100 * high / Math.Max(1, provenance.Count)}%), "
                             + $"flags: {string.Join(", ", proposal.Flags.GroupBy(flag => flag.Code).Select(group => $"{group.Key}×{group.Count()}"))}");
        }
    }

    private static string? Bound(List<RmFile> files, string key, string testName, string? analyte = null) =>
        Specification(files, key).Proposal.SpecificationProposals.First(row => row.TestName == testName && row.Analyte == analyte).SourceFieldKey;
}
