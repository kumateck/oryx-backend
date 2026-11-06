using Microsoft.Extensions.DependencyInjection;

namespace APP.Services.Formulas;

public static class FormulaCalculationRegistration
{
    // Shared by the API and its read-only deployment probe.
    public static IServiceCollection AddFormulaCalculationClient(this IServiceCollection services)
    {
        services.AddSingleton(_ => FormulaCalculationSettings.Load());
        services.AddTransient<FormulaCalculationClientAuthHandler>();
        services.AddHttpClient<IFormulaCalculationClient, FormulaCalculationClient>((provider, client) =>
            {
                var settings = provider.GetRequiredService<FormulaCalculationSettings>();
                client.BaseAddress = settings.BaseUri;
                client.Timeout = settings.Timeout;
            })
            .AddHttpMessageHandler<FormulaCalculationClientAuthHandler>();
        return services;
    }
}
