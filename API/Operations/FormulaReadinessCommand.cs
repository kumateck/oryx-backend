using APP.Services.Formulas;
using Microsoft.Extensions.DependencyInjection;

namespace API.Operations;

public static class FormulaReadinessCommand
{
    public static async Task<int> RunAsync()
    {
        try
        {
            var services = new ServiceCollection();
            services.AddFormulaCalculationClient();
            await using var provider = services.BuildServiceProvider();
            var result = await provider.GetRequiredService<IFormulaCalculationClient>().IsReadyAsync();
            if (result.IsSuccess && result.Value)
            {
                Console.WriteLine("Authoritative formula calculation service is ready.");
                return 0;
            }
            Console.Error.WriteLine(result.IsFailure
                ? $"Formula readiness failed: {result.Error.Code}."
                : "Formula readiness failed: service returned not ready.");
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            // Never echo exception details: configuration and secret paths are private.
            Console.Error.WriteLine("Formula readiness failed: invalid or unreadable runtime configuration.");
        }
        return 1;
    }
}
