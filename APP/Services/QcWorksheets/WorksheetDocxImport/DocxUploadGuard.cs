using Microsoft.AspNetCore.Http;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>
/// Upload validation shared by the DOCX importers (STP and worksheet): non-empty, 20 MB cap,
/// .docx only (macro-enabled .docm refused).
/// </summary>
public static class DocxUploadGuard
{
    public const long MaxBytes = 20 * 1024 * 1024;

    /// <summary>Why the file is refused, or null when it is acceptable.</summary>
    public static string Validate(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return "The uploaded file is empty.";

        if (file.Length > MaxBytes)
            return $"The file exceeds the maximum allowed size of {MaxBytes / (1024 * 1024)}MB.";

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        return extension switch
        {
            ".docx" => null,
            ".docm" => "Macro-enabled documents (.docm) are not supported.",
            _ => "Only .docx files are supported."
        };
    }
}
