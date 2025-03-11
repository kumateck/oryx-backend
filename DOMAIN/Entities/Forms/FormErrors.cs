using SHARED;

namespace DOMAIN.Entities.Forms;

public static class FormErrors
{
    public static Error NotFound(Guid formId) =>
        Error.NotFound("Form.NotFound", $"Form with ID {formId} not found.");

    public static Error MissingField(string questionLabel) =>
        Error.Validation("FormResponse.MissingField", $"The question '{questionLabel}' is required.");
    
    public static Error SectionMissing() =>
        Error.Validation("Form.Section", "Form must have at least one section.");

    public static Error SectionWithoutQuestions(string sectionTitle) =>
        Error.Validation("Form.Question", $"Section '{sectionTitle}' must have at least one question.");

    public static Error InvalidQuestionOptions(string questionLabel, string questionType) =>
        Error.Validation("Form.Question.Options", $"Question '{questionLabel}' of type '{questionType}' should not have options.");

    public static Error MissingQuestionOptions(string questionLabel, string questionType) =>
        Error.Validation("Form.Question.Options", $"Question '{questionLabel}' of type '{questionType}' must have at least one option.");
    
    public static Error InvalidQuestionType(string questionType) =>
        Error.Validation("FormResponse.InvalidQuestionType", $"The question type '{questionType}' is invalid.");

    public static Error EmptyTextResponse(string questionLabel) =>
        Error.Validation("FormResponse.Text", $"The response for question '{questionLabel}' cannot be empty.");

    public static Error InvalidOptionResponse(string questionLabel) =>
        Error.Validation("FormResponse.Option", $"Invalid selection for question '{questionLabel}'.");

    public static Error InvalidFileResponse(string questionLabel) =>
        Error.Validation("FormResponse.File", $"The uploaded file for question '{questionLabel}' is not a valid base64 string.");

    public static Error InvalidSignatureResponse(string questionLabel) =>
        Error.Validation("FormResponse.Signature", $"The signature for question '{questionLabel}' is not valid.");

    public static Error InvalidDateResponse(string questionLabel) =>
        Error.Validation("FormResponse.Date", $"The date for question '{questionLabel}' is not valid.");
    
    public static Error PaymentComplete =>
        Error.NotFound("Form.PaymentComplete", $"Form response has already been paid for.");
    
    public static Error InvalidAlphanumericResponse(string questionLabel) =>
        Error.Validation("FormResponse.Text.Alphanumeric", $"The response for question '{questionLabel}' must be alphanumeric.");

    public static Error InvalidEmailResponse(string questionLabel) =>
        Error.Validation("FormResponse.Text.Email", $"The response for question '{questionLabel}' must be a valid email address.");

    public static Error InvalidNumericResponse(string questionLabel) =>
        Error.Validation("FormResponse.Text.Numeric", $"The response for question '{questionLabel}' must be a valid number.");

    public static Error InvalidResponseType(string questionLabel) =>
        Error.Validation("FormResponse.Text.InvalidType", $"The response type for question '{questionLabel}' is invalid.");

}
