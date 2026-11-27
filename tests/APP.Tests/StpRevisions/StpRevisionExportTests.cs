using System.Reflection;
using System.Security.Cryptography;
using API.Authorization;
using API.Controllers;
using APP.Services.Authorization;
using APP.Services.QcWorksheets;
using APP.Services.QcWorksheets.WorksheetDocxImport;
using APP.Services.StpRevisions;
using APP.Utils;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.ProductSpecifications;
using DOMAIN.Entities.StpRevisions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SHARED;
using Xunit;

namespace APP.Tests.StpRevisions;

/// <summary>Step 4: the approved Word file, printing, and specification tests citing STP tests.</summary>
public partial class StpRevisionTests
{
    private StpTestLinkService Links() => new(db);

    [Fact]
    public void Export_print_and_link_endpoints_name_their_keys()
    {
        StpRevisionTask Task(string name) => Assert.Single(typeof(StpRevisionController).GetMethod(name)!.GetCustomAttributes<StpRevisionPermissionAttribute>()).Task;
        Assert.Equal(StpRevisionTask.Export, Task(nameof(StpRevisionController.GetStpRevisionDocx)));
        Assert.Equal(StpRevisionTask.Print, Task(nameof(StpRevisionController.GetStpRevisionPrint)));
        foreach (var (name, write) in new[] { (nameof(StpTestLinkController.GetMaterialSpecificationStpTestLinks), false), (nameof(StpTestLinkController.GetProductSpecificationStpTestLinks), false),
                     (nameof(StpTestLinkController.SaveMaterialSpecificationStpTestLink), true), (nameof(StpTestLinkController.SaveProductSpecificationStpTestLink), true) })
        {
            var method = typeof(StpTestLinkController).GetMethod(name)!;
            Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>());
            Assert.Equal(write, Assert.Single(method.GetCustomAttributes<StpTestLinkPermissionAttribute>()).Write);
        }
        Assert.Equal(PermissionKeys.CanEditProductSpecification, StpTestLinkPermissions.Key(StpRevisionKind.Product, true));
        Assert.Equal(PermissionKeys.CanViewPackagingMaterialSpecifications, StpTestLinkPermissions.Key(StpRevisionKind.PackagingMaterial, false));
        var registered = PermissionUtils.GeneratePermissions().Select(permission => permission.Key).ToList();
        Assert.Equal(42, StpRevisionPermissions.All.Distinct().Count());
        foreach (var key in StpRevisionPermissions.All) Assert.Single(registered, item => item == key);
    }

    [Fact]
    public async Task Approval_stores_the_exact_file_that_export_returns_and_a_draft_exports_marked_as_such()
    {
        var preparer = await Person("Preparer", [.. PreparerKeys, PermissionKeys.CanExportProductStpRevision, PermissionKeys.CanPrintProductStpRevision]);
        var checker = await Person("Deputy", PermissionKeys.CanReviewProductStpRevision);
        var viewer = await Person("Viewer", PermissionKeys.CanViewProductStpRevisions);
        actor.Current = preparer.User;
        var draftId = (await Service().Create(First(), preparer.User)).Value;
        var draft = (await Service().Export(draftId, preparer.User)).Value;
        Assert.False(draft.Approved);
        Assert.Contains("DRAFT", draft.FileName);
        Assert.Equal(ErrorType.Forbidden, (await Service().Export(draftId, viewer.User)).Errors[0].Type);
        Assert.Equal(ErrorType.Forbidden, (await Service().Get(draftId, viewer.User, StpRevisionTask.Print)).Errors[0].Type);
        Assert.True((await Service().Get(draftId, preparer.User, StpRevisionTask.Print)).IsSuccess);
        Assert.True((await Service().Delete(draftId, (await Person("Head", PermissionKeys.CanDeleteProductStpRevision)).User, "test")).IsSuccess);

        var id = await Approved(preparer, checker);
        var stored = await db.StpRevisionDocuments.AsNoTracking().SingleAsync(item => item.StpRevisionId == id);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(stored.Content)).ToLowerInvariant(), stored.Sha256);
        var first = (await Service().Export(id, preparer.User)).Value;
        var second = (await Service().Export(id, preparer.User)).Value;
        Assert.True(first.Approved);
        Assert.Equal(stored.Content, first.Content);
        Assert.Equal(first.Content, second.Content);
        Assert.Equal(stored.Sha256, (await Service().Get(id, preparer.User)).Value.ApprovedFileSha256);
        // The approved file names its preparer and checker and reads back as the approved content.
        using var stream = new MemoryStream(first.Content);
        var document = DocxDocumentReader.Read(stream);
        Assert.DoesNotContain("DRAFT", document.HeaderText);
        stream.Position = 0;
        var proposal = new StpDocxImportService().Propose(stream, first.FileName, StpRevisionKind.Product);
        Assert.Equal(Number, proposal.StpNumber.Value);
        Assert.Equal(["Description", "Assay"], proposal.Tests.Select(test => test.Title));
        Assert.Equal("Chromatographic Conditions", proposal.Tests[1].Steps[0].Heading);
    }

    private async Task<(Guid Spec, Guid Assay, Guid Ph)> ProductSpecification()
    {
        var formId = Guid.NewGuid();
        var spec = new ProductSpecification { Id = Guid.NewGuid(), FormId = formId, ProductId = db.Products.Single().Id, SpecificationNumber = "NMP/B/SPC/009" };
        var assay = new FormSection { Id = Guid.NewGuid(), FormId = formId, Name = "Assay", Order = 1 };
        var ph = new FormSection { Id = Guid.NewGuid(), FormId = formId, Name = "pH", Order = 2 };
        var other = new FormSection { Id = Guid.NewGuid(), FormId = formId, Name = "Other spec's test", Order = 3, ProductSpecificationId = Guid.NewGuid() };
        db.ProductSpecifications.Add(spec);
        db.FormSections.AddRange(assay, ph, other);
        await db.SaveChangesAsync();
        return (spec.Id, assay.Id, ph.Id);
    }

    [Fact]
    public async Task A_specification_test_cites_a_test_of_the_approved_stp_with_the_specification_edit_key()
    {
        var preparer = await Person("Preparer", PreparerKeys);
        var checker = await Person("Deputy", PermissionKeys.CanReviewProductStpRevision);
        var viewer = await Person("SpecViewer", PermissionKeys.CanViewProductSpecifications);
        var editor = await Person("SpecEditor", PermissionKeys.CanViewProductSpecifications, PermissionKeys.CanEditProductSpecification);
        var rawEditor = await Person("RawSpecEditor", PermissionKeys.CanEditRawMaterialSpecification);
        var (spec, assay, ph) = await ProductSpecification();

        // Before any approved revision there is nothing to cite.
        var empty = (await Links().Get(true, spec, viewer.User)).Value;
        Assert.Equal([Number], empty.StpNumbers);
        Assert.Empty(empty.Options);
        Assert.Equal(["Assay", "pH"], empty.Tests.Select(test => test.SectionName));
        Assert.Equal(ErrorType.Validation, (await Links().Save(true, spec, assay, new() { StpNumber = Number, TestKey = "assay" }, editor.User)).Errors[0].Type);

        await Approved(preparer, checker);
        Assert.Equal(ErrorType.Forbidden, (await Links().Save(true, spec, assay, new() { StpNumber = Number, TestKey = "assay" }, viewer.User)).Errors[0].Type);
        Assert.Equal(ErrorType.Forbidden, (await Links().Save(true, spec, assay, new() { StpNumber = Number, TestKey = "assay" }, rawEditor.User)).Errors[0].Type);
        Assert.Equal(ErrorType.Forbidden, (await Links().Get(true, spec, rawEditor.User)).Errors[0].Type);
        Assert.Equal(ErrorType.Validation, (await Links().Save(true, spec, assay, new() { StpNumber = Number, TestKey = "ph" }, editor.User)).Errors[0].Type);
        Assert.Equal(ErrorType.Validation, (await Links().Save(true, spec, assay, new() { StpNumber = Number }, editor.User)).Errors[0].Type);
        Assert.True((await Links().Save(true, spec, assay, new() { StpNumber = Number, TestKey = "assay" }, editor.User)).IsSuccess);

        var linked = (await Links().Get(true, spec, viewer.User)).Value;
        Assert.Equal(["description", "assay"], linked.Options.Select(option => option.TestKey));
        var cited = Assert.Single(linked.Tests, test => test.FormSectionId == assay);
        Assert.True(cited.Resolved);
        Assert.Equal(("5.2", "Assay", 2), (cited.TestNumber, cited.TestTitle, cited.RevisionNumber));
        Assert.Null(linked.Tests.Single(test => test.FormSectionId == ph).TestKey);

        // A section of another specification's scope is not this specification's test.
        var foreign = await db.FormSections.Where(item => item.ProductSpecificationId != null).Select(item => item.Id).SingleAsync();
        Assert.Equal(ErrorType.NotFound, (await Links().Save(true, spec, foreign, new() { StpNumber = Number, TestKey = "assay" }, editor.User)).Errors[0].Type);

        Assert.True((await Links().Save(true, spec, assay, new(), editor.User)).IsSuccess);
        Assert.Null((await Links().Get(true, spec, viewer.User)).Value.Tests.Single(test => test.FormSectionId == assay).TestKey);
    }
}
