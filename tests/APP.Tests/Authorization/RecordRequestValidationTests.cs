using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Xunit;

namespace APP.Tests.Authorization;

/// <summary>
/// ASP.NET refuses to bind a record whose validation attributes sit on its properties (for example
/// <c>[property: Required]</c> on a primary-constructor parameter): every request to the endpoint fails with a 500
/// before the action runs. Saving a pending certificate failed this way. Put the attributes on the parameters.
/// </summary>
public sealed class RecordRequestValidationTests
{
    [Fact]
    public void Records_keep_validation_on_their_constructor_parameters()
    {
        var assemblies = new[]
        {
            typeof(DOMAIN.Entities.Forms.SavePendingCertificate).Assembly,
            typeof(APP.Services.Forms.PendingCertificateService).Assembly,
            typeof(API.Controllers.PendingCertificateController).Assembly,
        };
        var offenders = assemblies.SelectMany(SafeTypes)
            .Where(type => type.GetMethod("<Clone>$") is not null)
            .SelectMany(type =>
            {
                var parameters = type.GetConstructors().SelectMany(constructor => constructor.GetParameters())
                    .Select(parameter => parameter.Name).ToHashSet(StringComparer.Ordinal);
                return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property => parameters.Contains(property.Name)
                        && property.GetCustomAttributes<ValidationAttribute>(inherit: true).Any())
                    .Select(property => $"{type.FullName}.{property.Name}");
            })
            .Order(StringComparer.Ordinal).ToList();
        Assert.True(offenders.Count == 0,
            "Move these validation attributes from [property: …] to the constructor parameter: " + string.Join(", ", offenders));
    }

    [Fact]
    public void Pending_certificate_requests_still_validate_their_fields()
    {
        var reason = typeof(DOMAIN.Entities.Forms.SavePendingCertificate).GetConstructors().Single()
            .GetParameters().Single(parameter => parameter.Name == "Reason");
        Assert.NotNull(reason.GetCustomAttribute<RequiredAttribute>());
        Assert.Equal(2000, reason.GetCustomAttribute<StringLengthAttribute>()!.MaximumLength);
    }

    private static IEnumerable<Type> SafeTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException error) { return error.Types.OfType<Type>(); }
    }
}
