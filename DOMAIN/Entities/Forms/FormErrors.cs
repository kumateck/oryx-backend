using System.Net;
using System.Text.RegularExpressions;
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

    public static Error SectionWithoutQuestions(string formName, string sectionTitle, int position)
    {
        static string PlainText(string value) =>
            WebUtility.HtmlDecode(Regex.Replace(value ?? "", "<[^<>]*>", " ")).Replace('\u00a0', ' ').Trim();

        var title = PlainText(sectionTitle);
        var name = PlainText(formName);
        var section = title.Length > 0 ? $"Section {position} (‘{title}’)" : $"Section {position}";
        return Error.Validation("Form.Question",
            $"{section} in template ‘{name}’ has no active saved questions. Open the template linked to this specification, add or replace a question in that section, and save before creating the ARD.");
    }

    public static Error InvalidQuestionOptions(string questionLabel, string questionType) =>
        Error.Validation("Form.Question.Options", $"Question '{questionLabel}' of type '{questionType}' should not have options.");

    public static Error MissingQuestionOptions(string questionLabel, string questionType) =>
        Error.Validation("Form.Question.Options", $"Question '{questionLabel}' of type '{questionType}' must have at least one option.");

    public static Error InvalidQuestionType(string questionType) =>
        Error.Validation("FormResponse.InvalidQuestionType", $"The question type '{questionType}' is invalid.");

    public static Error FieldsInUse(Guid formId) =>
        Error.Conflict(
            "Form.FieldsInUse",
            $"Form {formId} already has saved test answers, specification values or CoA items. "
                + "Replacing its sections and fields would delete them. Edit, add or remove individual "
                + "sections and fields instead, or create a new form.");

    public static readonly Error FormulaGovernanceRequired = Error.Conflict(
        "Form.Question.FormulaGovernanceRequired",
        "Formula questions must be created or edited through the governed formula workflow.");

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
