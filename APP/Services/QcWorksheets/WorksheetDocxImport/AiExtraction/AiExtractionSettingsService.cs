using System.Security.Cryptography;
using DOMAIN.Entities.QcWorksheets;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.QcWorksheets.WorksheetDocxImport.AiExtraction;

/// <summary>
/// Build brief 11. Encrypts/decrypts provider API keys with ASP.NET Core Data Protection
/// (<see cref="IDataProtectionProvider"/>) rather than the app's unused hand-rolled AES helper
/// (<c>APP/Extensions/StringExtensions.cs</c>) — Data Protection is the framework-native
/// mechanism and manages key rotation itself, per the brief's locked decision. The protector is
/// purpose-named so a key encrypted for this purpose can never be decrypted by any other
/// consumer of Data Protection in this app.
/// </summary>
public sealed class AiExtractionSettingsService : IAiExtractionSettingsService
{
    private const string ProtectorPurpose = "QcAiExtraction.ApiKey";

    private readonly ApplicationDbContext context;
    private readonly IDataProtector protector;

    public AiExtractionSettingsService(ApplicationDbContext context, IDataProtectionProvider dataProtectionProvider)
    {
        this.context = context;
        protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
    }

    public async Task<Result<AiExtractionSettingsDto>> GetAsync(CancellationToken cancellationToken)
    {
        var dto = await BuildDtoAsync(cancellationToken);
        return Result.Success(dto);
    }

    public async Task<Result<AiExtractionSettingsDto>> SaveProviderKeyAsync(
        AiExtractionProvider provider, string model, string apiKey, Guid actorId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return Result.Failure<AiExtractionSettingsDto>(WorksheetImportErrors.AiExtractionKeyRequired);

        // Computed before encryption, per the brief: the last 4 characters of the real key,
        // never more.
        var trimmedKey = apiKey.Trim();
        var keyPreview = trimmedKey.Length <= 4 ? trimmedKey : trimmedKey[^4..];
        var encryptedKey = protector.Protect(trimmedKey);

        var row = await context.QcAiExtractionSettings
            .FirstOrDefaultAsync(item => item.Provider == provider, cancellationToken);

        if (row is null)
        {
            row = new AiExtractionSettings { Provider = provider };
            context.QcAiExtractionSettings.Add(row);
        }

        row.Model = model;
        row.EncryptedApiKey = encryptedKey;
        row.KeyPreview = keyPreview;
        row.UpdatedById = actorId;
        row.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        var dto = await BuildDtoAsync(cancellationToken);
        return Result.Success(dto);
    }

    public async Task<Result<AiExtractionSettingsDto>> SetActiveProviderAsync(
        AiExtractionProvider provider, Guid actorId, CancellationToken cancellationToken)
    {
        var hasKey = await context.QcAiExtractionSettings.AnyAsync(
            item => item.Provider == provider && item.EncryptedApiKey != null, cancellationToken);
        if (!hasKey)
            return Result.Failure<AiExtractionSettingsDto>(WorksheetImportErrors.AiExtractionUnavailable);

        var active = await context.QcAiExtractionActiveProvider.FirstOrDefaultAsync(cancellationToken);
        if (active is null)
        {
            active = new AiExtractionActiveProvider();
            context.QcAiExtractionActiveProvider.Add(active);
        }

        active.Provider = provider;
        active.UpdatedById = actorId;
        active.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        var dto = await BuildDtoAsync(cancellationToken);
        return Result.Success(dto);
    }

    public async Task<Result<(AiExtractionProvider Provider, string Model, string ApiKey)>> ResolveActiveAsync(
        CancellationToken cancellationToken)
    {
        var active = await context.QcAiExtractionActiveProvider.FirstOrDefaultAsync(cancellationToken);
        if (active is null)
            return Result.Failure<(AiExtractionProvider, string, string)>(WorksheetImportErrors.AiExtractionUnavailable);

        var row = await context.QcAiExtractionSettings
            .FirstOrDefaultAsync(item => item.Provider == active.Provider, cancellationToken);
        if (row?.EncryptedApiKey is null)
            return Result.Failure<(AiExtractionProvider, string, string)>(WorksheetImportErrors.AiExtractionUnavailable);

        string apiKey;
        try
        {
            apiKey = protector.Unprotect(row.EncryptedApiKey);
        }
        catch (CryptographicException)
        {
            // The key ring changed underneath a stored ciphertext (e.g. a restore onto a
            // different key ring) — treat exactly like "no key configured", never throw out to
            // the caller.
            return Result.Failure<(AiExtractionProvider, string, string)>(WorksheetImportErrors.AiExtractionUnavailable);
        }

        return Result.Success((row.Provider, row.Model, apiKey));
    }

    private async Task<AiExtractionSettingsDto> BuildDtoAsync(CancellationToken cancellationToken)
    {
        var rows = await context.QcAiExtractionSettings.ToListAsync(cancellationToken);
        var active = await context.QcAiExtractionActiveProvider.FirstOrDefaultAsync(cancellationToken);

        var updaterIds = rows.Select(row => row.UpdatedById).Distinct().ToList();
        var updaters = await context.Users
            .Where(user => updaterIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => $"{user.FirstName} {user.LastName}".Trim(), cancellationToken);

        var statuses = Enum.GetValues<AiExtractionProvider>()
            .Select(provider =>
            {
                var row = rows.FirstOrDefault(item => item.Provider == provider);
                var hasKey = row?.EncryptedApiKey is not null;
                return new AiExtractionProviderStatusDto(
                    provider,
                    row?.Model,
                    hasKey,
                    hasKey ? row.KeyPreview : null,
                    row?.UpdatedById,
                    row is null ? null : updaters.GetValueOrDefault(row.UpdatedById),
                    row?.UpdatedAt);
            })
            .ToList();

        return new AiExtractionSettingsDto(active?.Provider ?? default, statuses);
    }
}
