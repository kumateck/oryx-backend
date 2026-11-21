using System.Reflection;
using API.Controllers;
using APP.Repository;
using APP.Services.Costing;
using APP.Services.Reviews;
using APP.Tests.ApprovedVendorLists;
using APP.Tests.Repository;
using APP.Utils;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Costing;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED;
using Xunit;

namespace APP.Tests.Costing;

public class CostingPriceListTests
{
    private readonly AvlActor actor = new();
    private readonly ApplicationDbContext db;
    private int claim;
    private static readonly DateTime Today = DateTime.UtcNow.Date;
    public CostingPriceListTests() => db = new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, actor);
    private ApprovalRepository Approvals() => new(db, null!, null!, null!, NullLogger<ApprovalRepository>.Instance, null!, new NoOpProductionActivityStepEventPublisher());
    private CostingPriceListService Lists() => new(db, Approvals());
    private DocumentReviewService Reviews() => new(db, Approvals(), null!);
    private async Task<Guid> Person(params string[] keys)
    {
        var id = Guid.NewGuid(); var role = Guid.NewGuid();
        db.Users.Add(new User { Id = id, UserName = id.ToString(), FirstName = "Price", LastName = "Officer" });
        db.Roles.Add(new Role { Id = role, Name = role.ToString() });
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = id, RoleId = role });
        foreach (var key in keys) {
            var cid = ++claim;
            db.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = cid, RoleId = role, ClaimType = AppConstants.Permission, ClaimValue = key });
            db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = cid, Key = key, Type = "View" });
        }
        await db.SaveChangesAsync(); actor.Current = id; return id;
    }
    private async Task<(Product Product, ProductPacking A, ProductPacking B, Guid Currency)> Masters()
    {
        var product = new Product { Id = Guid.NewGuid(), Code = "PR001", Name = "Suspension", Division = (Division)0 };
        var a = new ProductPacking { Id = Guid.NewGuid(), ProductId = product.Id, Name = "100 ml bottle" };
        var b = new ProductPacking { Id = Guid.NewGuid(), ProductId = product.Id, Name = "24 x 100 ml carton" };
        var currency = new Currency { Id = Guid.NewGuid(), Name = "Cedi", Symbol = "GHS", IsBaseCurrency = true };
        db.Products.Add(product); db.ProductPackings.AddRange(a,b); db.Currencies.Add(currency);
        await db.SaveChangesAsync(); return (product,a,b,currency.Id);
    }
    private static PrepareCostingPriceListRequest Header(int revision = 0, Guid? previous = null) => new() {
        Division = (Division)0, DocumentNumber = "FIN/PL/001", RevisionNumber = revision,
        PreviousRevisionId = previous, EffectiveFrom = Today.AddDays(revision), RevisionReason = "Updated pack prices" };
    private async Task<Guid> Draft(Guid user) => (await Lists().Prepare(Header(), user, false)).Value;

    [Fact]
    public void Endpoints_name_specific_registered_keys()
    {
        var expected = new Dictionary<string,string> {
            ["GetPriceListProducts"] = PermissionKeys.CanViewCostingPriceList, ["GetPriceLists"] = PermissionKeys.CanViewCostingPriceList,
            ["GetPriceList"] = PermissionKeys.CanViewCostingPriceList, ["GetPriceListLines"] = PermissionKeys.CanViewCostingPriceList,
            ["GetPriceListPrint"] = PermissionKeys.CanPrintCostingPriceList, ["PreparePriceList"] = PermissionKeys.CanCreateCostingPriceList,
            ["RevisePriceList"] = PermissionKeys.CanReviseCostingPriceList, ["UpdatePriceList"] = PermissionKeys.CanEditCostingPriceList,
            ["SavePriceListLine"] = PermissionKeys.CanEditCostingPriceList, ["SubmitPriceList"] = PermissionKeys.CanSubmitCostingPriceList };
        foreach (var (name,key) in expected) {
            var method = typeof(CostingPriceListController).GetMethod(name)!;
            Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>());
            Assert.Equal(key, Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        }
        var catalog = PermissionUtils.GeneratePermissions().Select(item => item.Key).ToList();
        foreach (var key in typeof(PermissionKeys).GetFields().Where(field => field.Name.EndsWith("CostingPriceList")).Select(field => (string)field.GetRawConstantValue()!))
            Assert.Single(catalog, key);
    }

    [Fact]
    public async Task Draft_seeds_each_pack_separately_and_writes_require_fresh_specific_grants()
    {
        var f = await Masters(); var outsider = await Person(PermissionKeys.CanManageCosting);
        Assert.Equal(ErrorType.Forbidden, (await Lists().Prepare(Header(), outsider, false)).Errors[0].Type);
        var author = await Person(PermissionKeys.CanCreateCostingPriceList, PermissionKeys.CanEditCostingPriceList);
        var id = await Draft(author);
        var lines = (await Lists().Lines(id,1,30,null)).Value.Data.ToList();
        Assert.Equal(2, lines.Count); Assert.All(lines,line => Assert.Null(line.Price));
        var save = new SaveCostingPriceListLineRequest { Version = 1, ProductId = f.Product.Id, ProductPackingId = f.A.Id, Price = 12m, CurrencyId = f.Currency };
        Assert.Equal(ErrorType.Forbidden, (await Lists().SaveLine(id,save,outsider)).Errors[0].Type);
        Assert.True((await Lists().SaveLine(id,save,author)).IsSuccess);
        Assert.Equal(ErrorType.Conflict, (await Lists().SaveLine(id,save,author)).Errors[0].Type);
        var grant = await db.RoleClaims.SingleAsync(item => item.ClaimValue == PermissionKeys.CanEditCostingPriceList);
        db.RoleClaims.Remove(grant); await db.SaveChangesAsync(); save.Version=2;
        Assert.Equal(ErrorType.Forbidden,(await Lists().SaveLine(id,save,author)).Errors[0].Type);
        Assert.Null((await Lists().Lines(id,1,30,null)).Value.Data.Single(line => line.ProductPackingId == f.B.Id).Price);
        Assert.Equal(ErrorType.Conflict,(await Lists().Prepare(Header(1),author,false)).Errors[0].Type);
    }

    [Fact]
    public async Task Submission_requires_a_human_approval_workflow_and_review_before_decision()
    {
        var f = await Masters(); var author = await Person(PermissionKeys.CanCreateCostingPriceList,PermissionKeys.CanEditCostingPriceList,PermissionKeys.CanSubmitCostingPriceList);
        var id = await Draft(author);
        await Lists().SaveLine(id,new() { Version=1,ProductId=f.Product.Id,ProductPackingId=f.A.Id,Price=12m,CurrencyId=f.Currency },author);
        Assert.Equal("CostingPriceList.Workflow",(await Lists().Submit(id,new() { Version=2 },author)).Errors[0].Code);
        var manager = await Person(PermissionKeys.CanApproveCostingPriceList,PermissionKeys.CanRejectCostingPriceList);
        db.Approvals.Add(new Approval { Id=Guid.NewGuid(),ItemType="CostingPriceList",ApprovalStages=[new ApprovalStage { Id=Guid.NewGuid(),UserId=manager,Order=1,Required=true }] });
        await db.SaveChangesAsync(); actor.Current=author;
        Assert.Equal(ErrorType.Forbidden,(await Lists().Submit(id,new() { Version=2 },manager)).Errors[0].Type);
        Assert.True((await Lists().Submit(id,new() { Version=2 },author)).IsSuccess);
        Assert.Equal("CostingPriceList.Review",(await Approvals().ApproveItem("CostingPriceList",id,manager,[],"OK",null)).Errors[0].Code);
        var checker=await Person(PermissionKeys.CanReviewCostingPriceList,PermissionKeys.CanReturnCostingPriceList);
        Assert.True((await Reviews().Confirm("CostingPriceList",id,new(),checker)).IsSuccess);
        actor.Current=manager;
        Assert.True((await Approvals().ApproveItem("CostingPriceList",id,manager,[],"OK",null)).IsSuccess);
        db.ChangeTracker.Clear();
        var revision=(await Lists().Get(id)).Value; Assert.Equal("Current",revision.Stage);
        Assert.Equal("Price Officer",revision.CheckedBy.Name); Assert.Equal("Price Officer",revision.ApprovedBy.Name);
        Assert.Equal(ErrorType.Conflict,(await Lists().SaveLine(id,new() { Version=revision.Version,ProductId=f.Product.Id,ProductPackingId=f.A.Id,Price=20m,CurrencyId=f.Currency },author)).Errors[0].Type);
        var print=(await Lists().Print(id)).Value; Assert.Equal(2,print.Lines.Count);
        Assert.Equal("100 ml bottle",print.Lines.Single(line => line.Price.HasValue).PackingStyle);
    }

    [Fact]
    public async Task Returned_revision_requires_correct_task_grants_and_rejection_is_final()
    {
        var f=await Masters();var author=await Person(PermissionKeys.CanCreateCostingPriceList,PermissionKeys.CanEditCostingPriceList,PermissionKeys.CanSubmitCostingPriceList);
        var id=await Draft(author);await Lists().SaveLine(id,new() {Version=1,ProductId=f.Product.Id,ProductPackingId=f.A.Id,Price=10m,CurrencyId=f.Currency},author);
        var checker=await Person(PermissionKeys.CanReviewCostingPriceList,PermissionKeys.CanReturnCostingPriceList);
        var manager=await Person(PermissionKeys.CanApproveCostingPriceList,PermissionKeys.CanRejectCostingPriceList);
        db.Approvals.Add(new() {Id=Guid.NewGuid(),ItemType="CostingPriceList",ApprovalStages=[new() {Id=Guid.NewGuid(),UserId=manager,Order=1,Required=true}]});
        await db.SaveChangesAsync();actor.Current=author;await Lists().Submit(id,new() {Version=2},author);
        Assert.Equal(ErrorType.Forbidden,(await Reviews().Confirm("CostingPriceList",id,new(),author)).Errors[0].Type);
        Assert.Equal(ErrorType.Forbidden,(await Reviews().Return("CostingPriceList",id,new() {Comments="Correction"},author)).Errors[0].Type);
        actor.Current=checker;Assert.True((await Reviews().Return("CostingPriceList",id,new() {Comments="Check the currency"},checker)).IsSuccess);
        Assert.Equal(ErrorType.Forbidden,(await Reviews().Resubmit("CostingPriceList",id,new(),checker)).Errors[0].Type);
        actor.Current=author;Assert.True((await Reviews().Resubmit("CostingPriceList",id,new(),author)).IsSuccess);
        actor.Current=checker;Assert.True((await Reviews().Confirm("CostingPriceList",id,new(),checker)).IsSuccess);
        Assert.Equal(ErrorType.Forbidden,(await Approvals().RejectItem("CostingPriceList",id,checker,[],"Refused",null)).Errors[0].Type);
        actor.Current=manager;Assert.True((await Approvals().RejectItem("CostingPriceList",id,manager,[],"Revise the prices",null)).IsSuccess);
        db.ChangeTracker.Clear();var closed=(await Lists().Get(id)).Value;
        Assert.Equal(CostingPriceListStatus.Rejected,closed.Status);
        Assert.Equal(ErrorType.Conflict,(await Lists().Update(id,new() {Version=closed.Version,Division=0,DocumentNumber="FIN/PL",RevisionReason="Change",EffectiveFrom=Today},author)).Errors[0].Type);
    }

    [Fact]
    public async Task Currency_is_required_snapshot_per_pack_and_not_borrowed_from_another_style()
    {
        var f=await Masters(); var author=await Person(PermissionKeys.CanCreateCostingPriceList,PermissionKeys.CanEditCostingPriceList);
        var id=await Draft(author); var dollars=new Currency {Id=Guid.NewGuid(),Name="US Dollar",Symbol="$"};
        var removed=new Currency {Id=Guid.NewGuid(),Name="Retired",Symbol="OLD",DeletedAt=DateTime.UtcNow};
        db.Currencies.AddRange(dollars,removed);await db.SaveChangesAsync();
        var line=new SaveCostingPriceListLineRequest {Version=1,ProductId=f.Product.Id,ProductPackingId=f.A.Id,Price=12m};
        Assert.Equal("CostingPriceList.Price",(await Lists().SaveLine(id,line,author)).Errors[0].Code);
        line.CurrencyId=removed.Id;
        Assert.Equal("CostingPriceList.Currency",(await Lists().SaveLine(id,line,author)).Errors[0].Code);
        line.CurrencyId=f.Currency;Assert.True((await Lists().SaveLine(id,line,author)).IsSuccess);
        line.Version=2;line.ProductPackingId=f.B.Id;line.Price=8m;line.CurrencyId=dollars.Id;
        Assert.True((await Lists().SaveLine(id,line,author)).IsSuccess);
        dollars.Symbol="Changed after save";await db.SaveChangesAsync();
        var printed=(await Lists().Print(id)).Value.Lines;
        Assert.Equal("$",printed.Single(item=>item.ProductPackingId==f.B.Id).CurrencySymbol);
        Assert.Equal("GHS",printed.Single(item=>item.ProductPackingId==f.A.Id).CurrencySymbol);
        var stored=await db.CostingPriceLists.SingleAsync(item=>item.Id==id); stored.Status=CostingPriceListStatus.Approved;stored.ApprovedAt=Today;
        await db.SaveChangesAsync();
        var selected=await EffectiveCostingPrice.Resolve(db,f.Product.Id,f.B.Id,Today.AddHours(1));
        Assert.Equal(8m,selected.Price);Assert.Equal(dollars.Id,selected.CurrencyId);
    }

    [Fact]
    public async Task Revision_source_and_open_state_are_independent_of_search_and_pagination()
    {
        await Masters();var author=await Person(PermissionKeys.CanCreateCostingPriceList,PermissionKeys.CanReviseCostingPriceList);
        var first=await Draft(author);var stored=await db.CostingPriceLists.SingleAsync(item=>item.Id==first);
        stored.Status=CostingPriceListStatus.Approved;stored.ApprovedAt=Today;await db.SaveChangesAsync();
        var page=await Lists().List((Division)0,2,1,"nothing matches");
        Assert.Empty(page.Data);Assert.Equal(first,page.LatestApproved.Id);Assert.Equal("Current",page.LatestApproved.Stage);Assert.Equal(1,page.NextRevisionNumber);
        var outsider=await Person(PermissionKeys.CanCreateCostingPriceList);
        Assert.Equal(ErrorType.Forbidden,(await Lists().Prepare(Header(1,first),outsider,true)).Errors[0].Type);
        actor.Current=author;
        Assert.True((await Lists().Prepare(Header(1,first),author,true)).IsSuccess);
        var filtered=await Lists().List((Division)0,1,20,"nothing matches");
        Assert.NotNull(filtered.OpenRevisionId);Assert.Equal(2,filtered.NextRevisionNumber);
    }

    [Fact]
    public async Task Effective_dates_and_exact_pack_style_control_resolution_without_repricing_saved_values()
    {
        var f=await Masters(); var asOf=Today.AddHours(12);
        var first=new CostingPriceList { Id=Guid.NewGuid(),Division=(Division)0,DocumentNumber="FIN/PL/001",RevisionReason="Initial",RevisionNumber=0,
            EffectiveFrom=Today,ApprovedAt=Today.AddDays(-1),Status=CostingPriceListStatus.Approved,
            Lines=[new() { Id=Guid.NewGuid(),ProductId=f.Product.Id,ProductPackingId=f.A.Id,ProductName="Snapshot",PackingStyle=f.A.Name,Price=12m,CurrencyId=f.Currency },
                   new() { Id=Guid.NewGuid(),ProductId=f.Product.Id,ProductPackingId=f.B.Id,ProductName="Snapshot",PackingStyle=f.B.Name,Price=240m,CurrencyId=f.Currency }] };
        var next=new CostingPriceList { Id=Guid.NewGuid(),Division=(Division)0,DocumentNumber="FIN/PL/001",RevisionReason="Increase",RevisionNumber=1,
            PreviousRevisionId=first.Id,EffectiveFrom=Today.AddDays(2),ApprovedAt=Today,Status=CostingPriceListStatus.Approved,
            Lines=[new() { Id=Guid.NewGuid(),ProductId=f.Product.Id,ProductPackingId=f.A.Id,ProductName="Snapshot",PackingStyle=f.A.Name,Price=15m,CurrencyId=f.Currency }] };
        db.CostingPriceLists.AddRange(first,next); await db.SaveChangesAsync();
        Assert.False((await EffectiveCostingPrice.Resolve(db,f.Product.Id,f.A.Id,Today.AddTicks(-1))).HasRevision);
        Assert.Equal(12m,(await EffectiveCostingPrice.Resolve(db,f.Product.Id,f.A.Id,asOf)).Price);
        Assert.Equal(240m,(await EffectiveCostingPrice.Resolve(db,f.Product.Id,f.B.Id,asOf)).Price);
        Assert.Equal(15m,(await EffectiveCostingPrice.Resolve(db,f.Product.Id,f.A.Id,next.EffectiveFrom)).Price);
        Assert.Equal(0m,(await EffectiveCostingPrice.Resolve(db,f.Product.Id,f.B.Id,next.EffectiveFrom)).Price);
        Assert.Equal(0m,(await EffectiveCostingPrice.Resolve(db,f.Product.Id,null,asOf)).Price);
        var customer=Guid.NewGuid(); db.Customers.Add(new Customer { Id=customer,Name="Buyer" }); db.CustomerPricingAgreements.Add(new() { Id=Guid.NewGuid(),CustomerId=customer,ProductId=f.Product.Id,ProductPackingId=f.B.Id,CurrencyId=f.Currency,AgreedPrice=200m,EffectiveFrom=Today });
        await db.SaveChangesAsync();
        Assert.Equal(200m,(await CustomerPricingResolver.ResolveAsync(db,customer,f.Product.Id,f.B.Id,99m,f.Currency,asOf)).UnitPrice);
        Assert.Equal(99m,(await CustomerPricingResolver.ResolveAsync(db,customer,f.Product.Id,f.A.Id,99m,f.Currency,Today.AddDays(-2))).UnitPrice);
        next.ApprovedAt=Today.AddDays(3); await db.SaveChangesAsync();
        Assert.Equal(12m,(await EffectiveCostingPrice.Resolve(db,f.Product.Id,f.A.Id,Today.AddDays(2))).Price);
        Assert.Equal(15m,(await EffectiveCostingPrice.Resolve(db,f.Product.Id,f.A.Id,Today.AddDays(3))).Price);
        Assert.Equal(12m,first.Lines[0].Price);
    }
}
