using System.Text.Json;
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
            var client = provider.GetRequiredService<IFormulaCalculationClient>();
            var result = await client.IsReadyAsync();
            if (result.IsFailure || !result.Value)
            {
                Console.Error.WriteLine(result.IsFailure
                    ? $"Formula readiness failed: {result.Error.Code}."
                    : "Formula readiness failed: service returned not ready.");
                return 1;
            }
            // /ready is unauthenticated and does not run a worker. An empty validate request is
            // rejected by the service as an invalid request (HTTP 200) only after the workload token
            // is accepted and a worker has answered, so it proves the path real calls use.
            using var probe = JsonDocument.Parse("{}");
            var call = await client.ValidateAsync(probe.RootElement);
            if (call.IsSuccess)
            {
                Console.WriteLine("Authoritative formula calculation service is ready and accepts the workload token.");
                return 0;
            }
            Console.Error.WriteLine($"Formula readiness failed: authenticated call rejected ({call.Error.Code}).");
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            // Never echo exception details: configuration and secret paths are private.
            Console.Error.WriteLine("Formula readiness failed: invalid or unreadable runtime configuration.");
        }
        return 1;
    }
}
