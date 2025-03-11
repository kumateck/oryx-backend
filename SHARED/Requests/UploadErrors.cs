namespace SHARED.Requests;

public static class UploadErrors
{
    public static Error MissingRequiredHeader(string header) =>
        Error.Validation("Upload.MissingHeader", $"The required header '{header}' is missing from the uploaded file.");

    public static Error CategoryNotFound(string categoryName) =>
        Error.NotFound("Upload.CategoryNotFound", $"Category '{categoryName}' does not exist in the database.");

    public static Error EmptyFile =>
        Error.Validation("Upload.EmptyFile", "The uploaded file is empty or invalid.");

    public static Error InvalidFileFormat =>
        Error.Validation("Upload.InvalidFormat", "The uploaded file is not a valid Excel file.");

    public static Error WorksheetNotFound =>
        Error.Validation("Upload.WorksheetNotFound", "The uploaded Excel file does not contain any worksheets.");
}
