using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SHARED;

#nullable enable

namespace APP.Services.Formulas;

public sealed class FormulaCalculationClient(
    HttpClient client,
    FormulaCalculationSettings settings) : IFormulaCalculationClient
{
    private const string ResponseSchema = "oryx-formula-service-response-v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<Result<FormulaServiceResponse>> ValidateAsync(
        JsonElement request,
        CancellationToken cancellationToken = default) =>
        SendAsync("internal/formula/v1/validate", request, cancellationToken);

    public Task<Result<FormulaServiceResponse>> EvaluateAsync(
        JsonElement request,
        CancellationToken cancellationToken = default) =>
        SendAsync("internal/formula/v1/evaluate", request, cancellationToken);

    public async Task<Result<bool>> IsReadyAsync(
        CancellationToken cancellationToken = default)
    {
        if (!settings.Enabled)
            return Result.Failure<bool>(FormulaCalculationErrors.Disabled);
        try
        {
            using var response = await client.GetAsync("ready", cancellationToken);
            return Result.Success(response.IsSuccessStatusCode);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<bool>(FormulaCalculationErrors.Unavailable);
        }
        catch (HttpRequestException)
        {
            return Result.Failure<bool>(FormulaCalculationErrors.Unavailable);
        }
    }

    private async Task<Result<FormulaServiceResponse>> SendAsync(
        string path,
        JsonElement request,
        CancellationToken cancellationToken)
    {
        if (!settings.Enabled)
            return Result.Failure<FormulaServiceResponse>(FormulaCalculationErrors.Disabled);
        try
        {
            using var response = await client.PostAsJsonAsync(
                path, request, JsonOptions, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Result.Failure<FormulaServiceResponse>(
                    FormulaCalculationErrors.Unavailable);

            var body = await response.Content.ReadFromJsonAsync<FormulaServiceResponse>(
                JsonOptions, cancellationToken);
            if (body is null || body.SchemaVersion != ResponseSchema ||
                body.TestOutcomes is null ||
                string.IsNullOrWhiteSpace(body.EngineBuildHash) ||
                body.EngineBuildHash.Length != 64)
                return Result.Failure<FormulaServiceResponse>(
                    FormulaCalculationErrors.InvalidResponse);

            return Result.Success(body);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<FormulaServiceResponse>(FormulaCalculationErrors.Unavailable);
        }
        catch (HttpRequestException)
        {
            return Result.Failure<FormulaServiceResponse>(FormulaCalculationErrors.Unavailable);
        }
        catch (JsonException)
        {
            return Result.Failure<FormulaServiceResponse>(FormulaCalculationErrors.InvalidResponse);
        }
    }
}

public sealed class FormulaCalculationClientAuthHandler(
    FormulaCalculationSettings settings) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (settings.Enabled)
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer", settings.WorkloadToken);
        return base.SendAsync(request, cancellationToken);
    }
}
