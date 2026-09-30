using System.Reflection;
using APP.Utils;
using Xunit;

namespace APP.Tests.FullProcedures;

public class FullProcedurePermissionCatalogTests
{
    [Fact]
    public void Generate_registers_every_declared_key_once()
    {
        var declared = DeclaredKeys();
        var generated = FullProcedurePermissionCatalog.Generate();

        Assert.Equal(declared.Count, generated.Count);
        Assert.Equal(declared, generated.Select(item => item.Key).ToHashSet());
        Assert.All(generated, item => Assert.Equal(FullProcedurePermissionCatalog.Module, item.Module));
        Assert.All(generated, item => Assert.False(string.IsNullOrWhiteSpace(item.SubModule)));
    }

    [Fact]
    public void Main_catalog_contains_new_keys_without_collisions()
    {
        var generated = PermissionUtils.GeneratePermissions().ToList();
        var fullProcedureKeys = DeclaredKeys();

        Assert.All(fullProcedureKeys, key => Assert.Single(generated, item => item.Key == key));
        Assert.Empty(fullProcedureKeys.Intersect(LegacyKeys()));
    }

    private static HashSet<string> DeclaredKeys() => typeof(FullProcedurePermissionKeys)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(field => (string)field.GetRawConstantValue()!)
        .ToHashSet();

    private static HashSet<string> LegacyKeys() => typeof(PermissionKeys)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(field => (string)field.GetRawConstantValue()!)
        .ToHashSet();
}
